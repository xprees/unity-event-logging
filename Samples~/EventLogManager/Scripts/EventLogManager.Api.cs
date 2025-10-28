using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Xprees.EventLogging.Api;
using Xprees.EventLogging.Api.Model;

namespace Xprees.EventLogging.Samples.Scripts
{
    // Partial class to handle API related functionality of the Event Log Manager.
    public partial class EventLogManager
    {
        [Header("API Settings")]
        [Tooltip("Event logging API URL to send the events to.")]
        [SerializeField] private string apiUrl = "https://cf-collector.xprees.com/";

        [Tooltip("How often to send the events to the API in seconds.")]
        [Range(1, 300)]
        [SerializeField] private int sendInterval = 10;

        [Tooltip("Whether to warm up the API connection on start with a dummy call, to wake up the service.")]
        [SerializeField] private bool warmupApiOnStart = true;

        [Tooltip("Whenever to upload remaining event logs on application quit.")]
        [SerializeField] private bool uploadOnDisable = true;

        [Tooltip("Whenever to upload remaining event logs on application quit.")]
        [SerializeField] private bool uploadOnQuit = true;

        [Tooltip("Whenever the logs should be upload from editor.")]
        [SerializeField] private bool dontSendInEditor = false;

        [Tooltip("Whenever the manager should log into the console.")]
        [SerializeField] private bool logInConsole = true;

        private EventLoggingApi _api;

        private void CreateApiInstance()
        {
            if (IsInvalidUrl(apiUrl))
            {
                Debug.LogError(
                    $"{nameof(Xprees.EventLogging.Samples.Scripts.EventLogManager)} - Invalid API Url found!\n Using default one! {EventLoggingApi.DefaultEndpoint}",
                    this);
                _api = new EventLoggingApi();
                return;
            }

            _api = new EventLoggingApi(apiUrl);
        }

        private void StartupApi(CancellationToken cancellationToken = default)
        {
            if (Application.isEditor && dontSendInEditor) return;
            if (!warmupApiOnStart) return;

            ConsoleLog("Warming up API...");
            _api.Warmup(cancellationToken).Forget();
        }

        private bool IsInvalidUrl(string apiEndpoint) =>
            string.IsNullOrEmpty(apiEndpoint) || !Uri.TryCreate(apiEndpoint, UriKind.Absolute, out _);

        public async void Upload() => await UploadEvents(destroyCancellationToken);

        public async UniTask<bool> UploadEvents(CancellationToken cancellationToken = default)
        {
            List<EventLog> logs;
            lock (_collectedEventLogs)
            {
                logs = _collectedEventLogs.ToList();
                ClearCollectedLogs();
                TryToFixLogsUserWithCurrentIfMissing(logs);
                if (logs.Count <= 0) return true; // nothing to upload skip it
            }

            if (Application.isEditor && dontSendInEditor)
            {
                ConsoleLog("Disabled upload in Editor, skipping upload.", this);
                return true;
            }

            ConsoleLog($"Uploading {logs.Count.ToString()} event logs...", this);
            var result = await _api.SendEventLogsBatch(logs, cancellationToken);

            if (!result) // if upload failed, add logs back to the queue
            {
                lock (_collectedEventLogs) logs.ForEach(l => _collectedEventLogs.Add(l));
            }

            ConsoleLog($"Uploading {(result ? "Success" : "Failed")}.", this);
            return result;
        }

        private void TryToFixLogsUserWithCurrentIfMissing(IEnumerable<EventLog> logs)
        {
            var currentUser = EventLoggingUser;
            foreach (var log in logs)
            {
                if (log.user is null || string.IsNullOrEmpty(log.user.formId))
                {
                    log.user = currentUser;
                }
            }
        }
    }
}