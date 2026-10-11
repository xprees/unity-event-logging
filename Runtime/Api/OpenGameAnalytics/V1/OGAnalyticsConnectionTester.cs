using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1.Model;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1.Util;

namespace Xprees.EventLogging.Api.OpenGameAnalytics.V1
{
    public enum OGConnectionStatus
    {
        Ok,
        NotConfigured,
        InvalidSettings,
        KeyRejected,
        WrongApp,
        AppNotFound,
        ScenarioMissing,
        RateLimited,
        BadRequest,
        NetworkError,
        Unknown,
    }

    public readonly struct OGConnectionTestResult
    {
        public OGConnectionStatus Status { get; }
        public string Message { get; }
        public bool IsOk => Status == OGConnectionStatus.Ok;

        public OGConnectionTestResult(OGConnectionStatus status, string message)
        {
            Status = status;
            Message = message;
        }
    }

    /// Verifies the config end to end by sending one sample event. The event id is fixed, so repeated runs are reported by the
    /// server as duplicates and don't pollute the data.
    public static class OGAnalyticsConnectionTester
    {
        /// UUIDv7-shaped constant id of the sample event.
        public const string SampleEventId = "01900000-0000-7000-8000-000000000001";

        public const string SampleEventName = "connection_test";

        public async static UniTask<OGConnectionTestResult> Test(OGAnalyticsConfigSO config, CancellationToken cancellationToken = default)
        {
            if (!config || !config.IsConfigured)
            {
                return new OGConnectionTestResult(OGConnectionStatus.NotConfigured, "Fill in base URL, org slug, app slug and anon key first.");
            }

            var scenario = OGValidation.Slugify(config.validationScenarioSlug);
            var player = OGValidation.SanitizePlayerId(config.validationPlayerId);
            if (!OGValidation.IsValidSlug(scenario) || !OGValidation.IsValidPlayerId(player))
            {
                return new OGConnectionTestResult(OGConnectionStatus.InvalidSettings,
                    "Connection test scenario slug / player id are empty or contain invalid characters.");
            }

            var sample = new OGIngestEvent
            {
                id = SampleEventId,
                timestamp = DateTime.UtcNow.ToString("O"),
                playerId = player,
                @event = SampleEventName,
                scenarioSlug = scenario,
            };

            var api = new OGAnalyticsV1Api(config);
            var result = await api.IngestEvents(scenario, new List<OGIngestEvent> { sample }, cancellationToken);
            return Interpret(result, scenario);
        }

        private static OGConnectionTestResult Interpret(OGApiResult result, string scenario)
        {
            if (result.Cancelled)
            {
                return new OGConnectionTestResult(OGConnectionStatus.Unknown, "Cancelled.");
            }

            if (result.NetworkError)
            {
                return new OGConnectionTestResult(OGConnectionStatus.NetworkError,
                    "Could not reach the server. Check the base URL, your network and (WebGL) the server CORS settings.");
            }

            if (result.Success)
            {
                var response = result.Parse<OGIngestResponse>();
                var note = response is { duplicates: > 0 }
                    ? "sample event already existed (duplicate) - the connection was verified before"
                    : "sample event accepted";
                return new OGConnectionTestResult(OGConnectionStatus.Ok,
                    $"Connection OK - key valid, app and scenario \"{scenario}\" found, {note}.");
            }

            var detail = result.Describe();
            return result.StatusCode switch
            {
                401 => new OGConnectionTestResult(OGConnectionStatus.KeyRejected,
                    $"The anon key was rejected ({result.Problem?.Identifier ?? "unknown reason"}): {result.Problem?.detail ?? "invalid, revoked or expired"}. "
                    + "A key only works against the server that issued it: check the base URL, org and app, or create a new key in the dashboard "
                    + "(a local API without ANON_KEY_SIGNING_KEY issues new keys on every start)."),
                403 => new OGConnectionTestResult(OGConnectionStatus.WrongApp,
                    "The key belongs to a different app than the configured app slug."),
                404 when result.HasProblem(OGProblemDetails.ScenarioNotFound) => new OGConnectionTestResult(OGConnectionStatus.ScenarioMissing,
                    $"Key and app are OK, but scenario \"{scenario}\" doesn't exist. Create it in the dashboard (or change the connection test scenario)."),
                404 when result.HasProblem(OGProblemDetails.OrgNotFound) => new OGConnectionTestResult(OGConnectionStatus.AppNotFound,
                    "Unknown org. Check the org slug (a key used on another org's URL gives this too)."),
                404 when result.HasProblem(OGProblemDetails.GameAppNotFound) => new OGConnectionTestResult(OGConnectionStatus.AppNotFound,
                    "Unknown app. Check the org and app slug."),
                404 => new OGConnectionTestResult(OGConnectionStatus.AppNotFound,
                    $"Not found ({detail}). Check the base URL, org and app slug."),
                429 => new OGConnectionTestResult(OGConnectionStatus.RateLimited,
                    $"Rate limited (per key and IP, 60 s window), try again in {Math.Max(1, (int) result.RetryAfterSeconds)} s."),
                400 => new OGConnectionTestResult(OGConnectionStatus.BadRequest,
                    $"The server rejected the sample event: {detail}\n{result.Body}"),
                _ => new OGConnectionTestResult(OGConnectionStatus.Unknown, $"Unexpected response: {detail}"),
            };
        }
    }
}