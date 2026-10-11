using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1.Model;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1.Util;

namespace Xprees.EventLogging.Api.OpenGameAnalytics.V1
{
    public class OGAnalyticsV1Api : IOGAnalyticsV1Api
    {
        private const string apiKeyHeader = "X-Api-Key";

        /// Seconds before a request is aborted. Browsers impose no default timeout, so WebGL relies on these.
        private const int timeoutSeconds = 15;

        private const int batchTimeoutSeconds = 30;

        private readonly string _appUrl;
        private readonly string _anonKey;

        /// <param name="baseUrlOverride">Replaces <see cref="OGAnalyticsConfigSO.apiBaseUrl"/> when it is a valid absolute URL (e.g. from PlayerPrefs).</param>
        public OGAnalyticsV1Api(OGAnalyticsConfigSO config, string baseUrlOverride = null)
        {
            if (!config) throw new ArgumentNullException(nameof(config));

            var url = Uri.TryCreate(baseUrlOverride, UriKind.Absolute, out _) ? baseUrlOverride : config.apiBaseUrl;
            var baseUrl = (url ?? string.Empty).Trim().TrimEnd('/');
            _anonKey = config.anonKey?.Trim();

            var orgSlug = Uri.EscapeDataString(config.orgSlug?.Trim() ?? string.Empty);
            var appSlug = Uri.EscapeDataString(config.appSlug?.Trim() ?? string.Empty);
            _appUrl = $"{baseUrl}/v1/orgs/{orgSlug}/apps/{appSlug}";
        }

        public UniTask<OGApiResult> IngestEvents(
            string scenarioSlug,
            IReadOnlyList<OGIngestEvent> events,
            CancellationToken cancellationToken = default
        ) => SendAsync(
            UnityWebRequest.kHttpVerbPOST,
            $"{_appUrl}/scenarios/{Uri.EscapeDataString(scenarioSlug)}/events",
            OGJsonWriter.IngestRequest(events),
            batchTimeoutSeconds,
            cancellationToken
        );

        public UniTask<OGApiResult> UpsertPlayer(string playerId, string name, CancellationToken cancellationToken = default) =>
            SendAsync(
                UnityWebRequest.kHttpVerbPUT,
                $"{_appUrl}/players/{Uri.EscapeDataString(playerId)}",
                OGJsonWriter.PlayerRequest(name),
                timeoutSeconds,
                cancellationToken
            );

        public UniTask<OGApiResult> SetPlayerGroup(
            string scenarioSlug,
            string playerId,
            string group,
            CancellationToken cancellationToken = default
        ) => SendAsync(
            UnityWebRequest.kHttpVerbPUT,
            $"{_appUrl}/scenarios/{Uri.EscapeDataString(scenarioSlug)}/players/{Uri.EscapeDataString(playerId)}/group",
            OGJsonWriter.GroupRequest(group),
            timeoutSeconds,
            cancellationToken
        );

        public UniTask<OGApiResult> GetPlayer(string playerId, CancellationToken cancellationToken = default) =>
            SendAsync(UnityWebRequest.kHttpVerbGET, $"{_appUrl}/players/{Uri.EscapeDataString(playerId)}", null, timeoutSeconds,
                cancellationToken);

        public UniTask<OGApiResult> GetEventById(string eventId, CancellationToken cancellationToken = default) =>
            SendAsync(UnityWebRequest.kHttpVerbGET, $"{_appUrl}/events/{Uri.EscapeDataString(eventId)}", null, timeoutSeconds,
                cancellationToken);

        /// Sends the request and returns status + body + parsed problem. Unlike the shared extension it never logs errors itself -
        /// callers decide what is worth reporting (a halted upload must be logged once, not on every retry).
        private async UniTask<OGApiResult> SendAsync(string method, string url, string json, int timeout, CancellationToken cancellationToken)
        {
            try
            {
                using var request = new UnityWebRequest(url, method);
                request.timeout = timeout;
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Accept", "application/json");
                if (!string.IsNullOrEmpty(_anonKey)) request.SetRequestHeader(apiKeyHeader, _anonKey);
                if (json != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                    request.SetRequestHeader("Content-Type", "application/json");
                }

                try
                {
                    await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
                }
                catch (UnityWebRequestException)
                {
                    // status/body are read from the request below
                }

                return ToResult(request);
            }
            catch (OperationCanceledException)
            {
                return new OGApiResult { Cancelled = true };
            }
            catch (Exception e)
            {
                Debug.LogError($"{nameof(OGAnalyticsV1Api)} - unexpected error: {e}");
                return new OGApiResult { NetworkError = true };
            }
        }

        private static OGApiResult ToResult(UnityWebRequest request)
        {
            var body = request.downloadHandler?.text;
            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                return new OGApiResult { NetworkError = true, Body = body };
            }

            var success = request.result == UnityWebRequest.Result.Success;
            var result = new OGApiResult
            {
                Success = success,
                StatusCode = request.responseCode,
                Body = body,
                // A response without any HTTP status (e.g. aborted) is not an API answer.
                NetworkError = request.responseCode == 0,
            };

            if (success) return result;

            result.Problem = TryParseProblem(body);
            if (request.responseCode == 429)
            {
                var retryAfter = request.GetResponseHeader("Retry-After"); // readable in WebGL only if the server exposes it (CORS)
                if (float.TryParse(retryAfter, NumberStyles.Float, CultureInfo.InvariantCulture,
                        out var seconds))
                {
                    result.RetryAfterSeconds = seconds;
                }
            }

            return result;
        }

        private static OGProblemDetails TryParseProblem(string body)
        {
            if (string.IsNullOrEmpty(body) || body[0] != '{') return null;
            try
            {
                return JsonUtility.FromJson<OGProblemDetails>(body);
            }
            catch
            {
                return null;
            }
        }
    }
}