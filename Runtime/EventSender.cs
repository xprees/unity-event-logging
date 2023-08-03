using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Xprees.EventLogging;
using Xprees.EventLogging.Api;
using Xprees.EventLogging.Api.Model;
using Xprees.EventLogging.Extensions;
using Xprees.EventLogging.ScriptableObjects;
using Xprees.Events.ScriptableObjects.Base;
using Xprees.Events.ScriptableObjects.EventLogging;
using Xprees.Variables.Primitive;

namespace Xprees.EventLogging
{
    [RequireComponent(typeof(IEventUserSetupService))]
    public class EventSender : MonoBehaviour, IEventSenderService
    {
        public const string PlayerPrefsBaseUrlKey = "EventLoggingBaseUrl";
        private const string backupFileExtension = "eventlogs";
        private readonly ConcurrentBag<EventLog> _collectedEventLogs = new();
        private static string backupDirPath;

        private IEventUserSetupService _eventUserSetupService;
        private EventLoggingApi _api;

        [Header("Settings")]
        public bool debugLogsEnabled = true;

        public bool doApiWarmups = true;

        public bool uploadOnApplicationQuit = true;

        public bool uploadOnDisable = true;

        [Tooltip("On upload file will place logs into local file.")]
        public bool backupToFileOnFail = false;

        [Header("Variables")]
        [Tooltip("Required for uploading events.")]
        [SerializeField] private StringVariable currentScenarioName;

        [Header("Listens to")]
        [SerializeField] private EventLoggingEventChannel addLogEvent;

        [SerializeField] private VoidEventChannelSO[] uploadEvents;

        [SerializeField] private VoidEventChannelSO settingsChangedEvent;


        private void Awake()
        {
            backupDirPath = Path.Combine(Application.dataPath, "LogsBackup");
            _eventUserSetupService = GetComponent<IEventUserSetupService>();
            CreateApiInstance();
        }

        private async void Start() => await StartupApi();

        private void OnEnable()
        {
            if (settingsChangedEvent != null) settingsChangedEvent.onEventRaised += OnSettingsChanged;
            if (addLogEvent != null) addLogEvent.onEventRaised += LogEvent;
            if (uploadEvents != null)
            {
                foreach (var uploadEvent in uploadEvents) uploadEvent.onEventRaised += Upload;
            }
        }

        private async void OnDisable()
        {
            if (settingsChangedEvent != null) settingsChangedEvent.onEventRaised -= OnSettingsChanged;
            if (addLogEvent != null) addLogEvent.onEventRaised -= LogEvent;
            if (uploadEvents != null)
            {
                foreach (var uploadEvent in uploadEvents) uploadEvent.onEventRaised -= Upload;
            }

            if (uploadOnDisable) await UploadEvents();
        }

        private async void OnApplicationQuit()
        {
            if (uploadOnApplicationQuit) await UploadEvents();
        }

        private void OnDestroy() => _api?.Dispose();

        private async void OnSettingsChanged()
        {
            CreateApiInstance();
            await StartupApi();
        }

        private void CreateApiInstance()
        {
            var apiEndpoint = GetApiEndpoint();
            if (IsInvalidUrl(apiEndpoint))
            {
                SavePredefinedEndpointToPlayerPrefsIfDoesNotExist();
                apiEndpoint = GetApiEndpoint();
            }

            _api = new EventLoggingApi(apiEndpoint);
        }

        private async UniTask StartupApi()
        {
            if (!doApiWarmups) return;
            Log("Warming up API...");
            await _api.Warmup();
            await TryToUploadLocalBackupFromFile();
        }

        private async void Upload() => await UploadEvents();

        private bool IsInvalidUrl(string apiEndpoint) =>
            string.IsNullOrEmpty(apiEndpoint) || !Uri.TryCreate(apiEndpoint, UriKind.Absolute, out _);

        private string GetApiEndpoint() => PlayerPrefs.GetString(PlayerPrefsBaseUrlKey, null);

        private void SavePredefinedEndpointToPlayerPrefsIfDoesNotExist()
        {
            var savedValue = PlayerPrefs.GetString(PlayerPrefsBaseUrlKey, null);
            if (!string.IsNullOrEmpty(savedValue)) return;

            PlayerPrefs.SetString(PlayerPrefsBaseUrlKey, EventLoggingApi.DefaultEndpoint);
        }

        public void LogEvent(EventSO loggedEvent)
        {
            SetCurrentScenarioToLoggedEventIfMissing(loggedEvent);
            var user = GetUser();
            if (string.IsNullOrEmpty(user.formId))
            {
                Debug.LogError($"{nameof(EventSender)} - Current user formId is null or empty! Resulting corrupted logs.", this);
            }

            var eventLog = loggedEvent.GenerateEventLog(user, DateTime.Now);
            lock (_collectedEventLogs) _collectedEventLogs.Add(eventLog);

            Log($"Logged event: {eventLog.@event}");
        }

        private void SetCurrentScenarioToLoggedEventIfMissing(EventSO loggedEvent)
        {
            if (!string.IsNullOrEmpty(loggedEvent.scenario)) return;
            Debug.Assert(currentScenarioName.CurrentValue != null, $"{nameof(EventSender)} - Current scenario is null!");
            loggedEvent.scenario = currentScenarioName;
        }

        private User GetUser() => _eventUserSetupService.CurrentUser;

        public async UniTask<bool> UploadEvents()
        {
            List<EventLog> logs;
            lock (_collectedEventLogs)
            {
                logs = _collectedEventLogs.ToList();
                ClearCollectedLogs();
                TryToFixLogsUserWithCurrentIfMissing(logs);
                if (logs.Count <= 0)
                {
                    if (debugLogsEnabled) Debug.Log("No new event logs collected. Skipping upload.");
                    return true;
                }
            }

            Log($"Uploading {logs.Count.ToString()} event logs...");
            var result = await _api.SendEventLogsBatch(logs);

            if (!result) // if upload failed, add logs back to the queue
            {
                lock (_collectedEventLogs) logs.ForEach(l => _collectedEventLogs.Add(l));

                if (backupToFileOnFail) await DoLogsFileBackup(logs);
            }

            Log($"Uploading {(result ? "Success" : "Failed")}.");
            return result;
        }

        private void TryToFixLogsUserWithCurrentIfMissing(IEnumerable<EventLog> logs)
        {
            var currentUser = GetUser();
            foreach (var log in logs)
            {
                if (log.user is null || string.IsNullOrEmpty(log.user.formId))
                {
                    log.user = currentUser;
                }
            }
        }

        #region Upload Failure Handling

        private async UniTask DoLogsFileBackup(ICollection<EventLog> logs)
        {
            if (logs?.Count <= 0) return;
            var createDirTask = CreateLogsBackupsFolderIfDoesNotExist();

#if UNITY_WEBGL
            var serializedLogs = JsonConvert.SerializeObject(logs);
#else
            var serializedLogs = await UniTask.RunOnThreadPool(() => JsonConvert.SerializeObject(logs));
#endif
            var filePath = Path.Combine(backupDirPath, $"logs_backup_from_{DateTime.UtcNow:MM_dd_yy_hh_mm_ss}.{backupFileExtension}");

            await createDirTask;
            await File.WriteAllTextAsync(filePath, serializedLogs, Encoding.UTF8);
        }

        private UniTask CreateLogsBackupsFolderIfDoesNotExist()
        {
#if UNITY_WEBGL
            if (!Directory.Exists(backupDirPath))
            {
                Directory.CreateDirectory(backupDirPath);
            }

            return UniTask.CompletedTask;
#else
            return UniTask.RunOnThreadPool(() =>
            {
                if (Directory.Exists(backupDirPath)) return;
                Directory.CreateDirectory(backupDirPath);
            });
#endif
        }

        private async UniTask TryToUploadLocalBackupFromFile()
        {
            if (!Directory.Exists(backupDirPath)) return;

            var foundFiles = Directory.EnumerateFiles(backupDirPath, $"*.{backupFileExtension}");
            await foreach (var (success, filePath) in foundFiles
                               .ToUniTaskAsyncEnumerable()
                               .SelectAwait(UploadBackupFile))
            {
                if (success) File.Delete(filePath);
            }
        }

        private async UniTask<Tuple<bool, string>> UploadBackupFile(string filePath)
        {
            try
            {
                var fileContent = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                var deserializedLogs = await UniTask.RunOnThreadPool(() =>
                    JsonConvert.DeserializeObject<List<EventLog>>(fileContent));
                var result = await _api.SendEventLogsBatch(deserializedLogs);
                return new(result, filePath);
            }
            catch (Exception)
            {
                // ignored
            }

            return new(false, filePath);
        }

        #endregion

        private void ClearCollectedLogs() => _collectedEventLogs.Clear();

        private void Log(string message)
        {
            if (!debugLogsEnabled) return;
            Debug.Log(message, this);
        }

        private void OnValidate()
        {
            if (currentScenarioName == null)
                Debug.LogWarning($"{nameof(EventSender)} on {nameof(name)} - {nameof(currentScenarioName)} is not set.");
        }
    }
}