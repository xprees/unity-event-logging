using System;
using System.Collections.Concurrent;
using UnityEngine;
using Xprees.EventLogging.Api.Model;
using Xprees.EventLogging.Extensions;
using Xprees.EventLogging.ScriptableObjects;
using Object = UnityEngine.Object;

namespace Xprees.EventLogging.Samples.Scripts
{
    /// Local event log manager for elevator that collects and sends events to the event log api server.
    [DisallowMultipleComponent]
    public partial class EventLogManager : MonoBehaviour, IEventSenderService
    {
        [Header("Event Logging")]
        [Tooltip(
            "This is unique URL-SAFE ID for your project logs. "
            + "Change it to your own project's ID, or you will mix logs with other projects using the same default ID. "
            + "aka. Scenario name.")]
        [SerializeField] private string projectId = "vr-mp-elevator";

        [Tooltip("Reference to the user manager that provides user data for the logs.")]
        [SerializeField] private EventUserManager userManager;

        private User EventLoggingUser => userManager.CurrentUser;

        private readonly ConcurrentBag<EventLog> _collectedEventLogs = new();

        private void Start()
        {
            StartupApi();
            StartPeriodicUpload();
        }

        private void OnEnable() => CreateApiInstance();

        private async void OnDisable()
        {
            StopPeriodicUpload();
            if (uploadOnDisable) await UploadEvents(destroyCancellationToken);
        }

        private async void OnApplicationQuit()
        {
            if (uploadOnQuit) await UploadEvents(destroyCancellationToken);
        }

        public void StartPeriodicUpload() => InvokeRepeating(nameof(Upload), sendInterval, sendInterval);

        public void StopPeriodicUpload() => CancelInvoke(nameof(Upload));

        public void LogEvent(EventSO loggedEvent)
        {
            SetCurrentScenarioToLoggedEventIfMissing(loggedEvent);
            var user = EventLoggingUser;
            if (string.IsNullOrEmpty(user.formId))
            {
                Debug.LogError($"{nameof(EventLogManager)} - Current user formId is null or empty! Resulting corrupted logs.", this);
            }

            if (loggedEvent == null || string.IsNullOrEmpty(loggedEvent.scenario))
            {
                ConsoleLog($"{nameof(EventLogManager)} - Scenario is null or empty! Throwing out the log. "
                           + $"Event: \"{(loggedEvent != null ? loggedEvent.eventName : "None")}\"");
                return;
            }

            var eventLog = loggedEvent.GenerateEventLog(user, DateTime.UtcNow);
            lock (_collectedEventLogs) _collectedEventLogs.Add(eventLog);

            ConsoleLog($"Logged event: {eventLog.@event}", this);
        }

        private void SetCurrentScenarioToLoggedEventIfMissing(EventSO loggedEvent)
        {
            if (!string.IsNullOrEmpty(loggedEvent.scenario)) return;
            // Try to fill scenario from current scenario name
            loggedEvent.scenario = projectId;
        }

        private void ClearCollectedLogs() => _collectedEventLogs.Clear();

        private void ConsoleLog(string message, Object context = null)
        {
            if (!logInConsole) return;
            Debug.Log(message, context);
        }
    }
}