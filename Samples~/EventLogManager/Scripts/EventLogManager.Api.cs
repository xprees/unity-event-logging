using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1.Model;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1.Util;

namespace Xprees.EventLogging.Samples.Scripts
{
    // Partial class to handle API related functionality of the Event Log Manager.
    public partial class EventLogManager
    {
        private const int maxQueuedEvents = 10_000;

        [Header("API Settings")]
        [Tooltip("Optional. Leave empty to use the global settings (Project Settings > Open Game Analytics, with 'Test connection' there).")]
        [SerializeField] private OGAnalyticsConfigSO configOverride;

        [Tooltip("How often to send the events to the API in seconds.")]
        [Range(1, 300)]
        [SerializeField] private int sendInterval = 10;

        private readonly HashSet<string> _haltedScenarios = new();
        private readonly HashSet<string> _invalidGroups = new();

        private IOGAnalyticsV1Api _api;
        private bool _halted; // key rejected / app unknown - fix the settings and restart
        private float _pausedUntil; // rate limit

        private OGAnalyticsConfigSO Config => configOverride ? configOverride : OGAnalyticsConfigSO.Instance;

        private void CreateApiInstance()
        {
            var config = Config;
            if (config == null || !config.IsConfigured)
            {
                Debug.LogError(
                    $"{nameof(EventLogManager)} - Open Game Analytics config is missing or incomplete (Project Settings > Open Game Analytics).",
                    this);
                return;
            }

            _api = new OGAnalyticsV1Api(config);
        }

        public async void Upload() => await UploadEvents(destroyCancellationToken);

        public async UniTask<bool> UploadEvents(CancellationToken cancellationToken = default)
        {
            List<OGIngestEvent> events;
            lock (_collectedEvents)
            {
                if (_collectedEvents.Count <= 0) return true; // nothing to upload skip it
                events = _collectedEvents.ToList();
                _collectedEvents.Clear();
            }

            if (_api == null || (Application.isEditor && Config is { dontSendInEditor: true }))
            {
                ConsoleLog("Upload skipped.", this);
                return true;
            }

            var failed = new List<OGIngestEvent>();
            // One request per scenario, max 500 events each.
            foreach (var scenario in events.GroupBy(e => e.scenarioSlug))
            {
                var scenarioEvents = scenario.ToList();
                for (var i = 0; i < scenarioEvents.Count; i += OGValidation.MaxBatchSize)
                {
                    var chunk = scenarioEvents.GetRange(i, System.Math.Min(OGValidation.MaxBatchSize, scenarioEvents.Count - i));
                    var blocked = _halted || _haltedScenarios.Contains(scenario.Key) || Time.realtimeSinceStartup < _pausedUntil;
                    if (blocked || !await UploadChunk(scenario.Key, chunk, cancellationToken)) failed.AddRange(chunk);
                }
            }

            // Events keep their ids, so re-queueing and retrying is safe (the server counts duplicates).
            if (failed.Count > 0)
            {
                lock (_collectedEvents) _collectedEvents.InsertRange(0, failed);
            }

            ConsoleLog($"Uploading {(failed.Count == 0 ? "Success" : $"Failed ({failed.Count} events kept)")}.", this);
            return failed.Count == 0;
        }

        /// Returns true when the chunk is done (accepted, or dropped because a retry can never work), false when it should be retried.
        private async UniTask<bool> UploadChunk(string scenarioSlug, List<OGIngestEvent> chunk, CancellationToken cancellationToken)
        {
            var result = await _api.IngestEvents(scenarioSlug, chunk, cancellationToken);
            if (result.Success)
            {
                HandleAccepted(scenarioSlug, JsonUtility.FromJson<OGIngestResponse>(string.IsNullOrEmpty(result.Body) ? "{}" : result.Body));
                return true;
            }

            if (result.Cancelled) return false;

            switch (result.StatusCode)
            {
                case 401:
                case 403:
                    _halted = true; // bad key - retrying won't help, check the settings
                    Debug.LogError($"{nameof(EventLogManager)} - Upload stopped: {result.Describe()}", this);
                    return false;
                case 404 when result.HasProblem(OGProblemDetails.ScenarioNotFound):
                    _haltedScenarios.Add(scenarioSlug); // create the scenario in the dashboard; other scenarios still upload
                    Debug.LogError($"{nameof(EventLogManager)} - Scenario \"{scenarioSlug}\" doesn't exist: {result.Describe()}", this);
                    return false;
                case 404:
                    _halted = true; // unknown app
                    Debug.LogError($"{nameof(EventLogManager)} - Upload stopped: {result.Describe()}", this);
                    return false;
                case 400:
                    // The server rejects the batch itself - a retry would fail forever, so drop it.
                    Debug.LogError($"{nameof(EventLogManager)} - Batch of scenario \"{scenarioSlug}\" rejected and dropped: {result.Body}", this);
                    return true;
                case 429:
                    var pause = result.RetryAfterSeconds > 0 ? result.RetryAfterSeconds : Config?.rateLimitFallbackPauseSeconds ?? 5f;
                    _pausedUntil = Time.realtimeSinceStartup + pause;
                    ConsoleLog($"Rate limited, pausing uploads for {pause:F0}s.", this);
                    return false;
                default:
                    return false; // network / 5xx - retry on the next upload
            }
        }

        /// The 202 stores the batch but can still report groups that were not assigned and events dropped for good.
        private void HandleAccepted(string scenarioSlug, OGIngestResponse response)
        {
            if (response == null) return;
            ConsoleLog($"Scenario \"{scenarioSlug}\": accepted {response.accepted}, duplicates {response.duplicates}.", this);

            if (response.groupErrors != null)
            {
                foreach (var error in response.groupErrors)
                {
                    if (string.IsNullOrEmpty(error.group) || !_invalidGroups.Add(error.group)) continue;
                    Debug.LogWarning(
                        $"{nameof(EventLogManager)} - Group \"{error.group}\" was not assigned ({error.code}); events are stored without it.", this);
                }
            }

            if (response.rejected is { Length: > 0 })
            {
                Debug.LogWarning(
                    $"{nameof(EventLogManager)} - {response.rejected.Length} events were rejected ({response.rejected[0].code}) and dropped.", this);
            }
        }
    }
}
