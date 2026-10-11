using System;
using System.Collections.Generic;
using UnityEngine;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1.Model;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1.Util;
using Xprees.EventLogging.Extensions;
using Xprees.EventLogging.ScriptableObjects;
using Object = UnityEngine.Object;

namespace Xprees.EventLogging.Samples.Scripts
{
    /// Collects events and sends them to the Open Game Analytics API. Starting point for your own implementation.
    [DisallowMultipleComponent]
    public partial class EventLogManager : MonoBehaviour, IEventSenderService
    {
        [Header("Event Logging")]
        [Tooltip("Scenario slug (a-z 0-9 . _ -) events are logged to when the event has no scenario. The scenario has to exist in the dashboard.")]
        [SerializeField] private string defaultScenario = "my-scenario";

        [Tooltip("Reference to the user manager that provides the player for the logs.")]
        [SerializeField] private EventUserManager userManager;

        private readonly List<OGIngestEvent> _collectedEvents = new();

        private void Start()
        {
            StartPeriodicUpload();
        }

        private void OnEnable() => CreateApiInstance();

        private async void OnDisable()
        {
            StopPeriodicUpload();
            if (Config is not { uploadOnDisable: false }) await UploadEvents(destroyCancellationToken);
        }

        private async void OnApplicationQuit()
        {
            if (Config is not { uploadOnApplicationQuit: false }) await UploadEvents(destroyCancellationToken);
        }

        public void StartPeriodicUpload() => InvokeRepeating(nameof(Upload), sendInterval, sendInterval);

        public void StopPeriodicUpload() => CancelInvoke(nameof(Upload));

        public void LogEvent(EventSO loggedEvent)
        {
            if (loggedEvent == null) return;

            if (userManager == null || userManager.CurrentPlayer == null)
            {
                ConsoleLog($"Dropped event \"{loggedEvent.eventName}\": no player (assign the {nameof(EventUserManager)}).", this);
                return;
            }

            var scenario = OGValidation.Slugify(string.IsNullOrEmpty(loggedEvent.scenario) ? defaultScenario : loggedEvent.scenario);
            var group = string.IsNullOrEmpty(userManager.Group) || _invalidGroups.Contains(userManager.Group) ? null : userManager.Group;
            var ingestEvent = loggedEvent.ToIngestEvent(userManager.CurrentPlayer.id, group, scenario, DateTime.UtcNow);

            var error = OGValidation.ValidateEvent(ingestEvent.playerId, ingestEvent.@event, ingestEvent.eventData, ingestEvent.group);
            if (error != null || !OGValidation.IsValidSlug(scenario))
            {
                ConsoleLog($"Dropped event \"{loggedEvent.eventName}\": {error ?? "invalid scenario slug"}", this);
                return;
            }

            lock (_collectedEvents)
            {
                if (_collectedEvents.Count >= maxQueuedEvents)
                {
                    ConsoleLog($"Event queue is full ({maxQueuedEvents}), dropping \"{loggedEvent.eventName}\".", this);
                    return;
                }

                _collectedEvents.Add(ingestEvent);
            }

            ConsoleLog($"Logged event: {ingestEvent.@event}", this);
        }

        private void ConsoleLog(string message, Object context = null)
        {
            if (Config is { debugLogsEnabled: false }) return;
            Debug.Log(message, context);
        }
    }
}
