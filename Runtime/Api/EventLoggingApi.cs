using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Xprees.EventLogging.Api.Model;
using Xprees.EventLogging.Extensions;

namespace Xprees.EventLogging.Api
{
    public class EventLoggingApi : IEventLoggingApi
    {
        public const string DefaultEndpoint = "https://cf-collector.xprees.com";

        private readonly string _baseUrl;
        private string LogsUri => $"{_baseUrl}/logs";
        private string LogsBatchUri => $"{_baseUrl}/logs/batch";
        private string ScenariosUri => $"{_baseUrl}/logs/scenarios";

        /// Seconds before a request is aborted. Browsers impose no default timeout, so WebGL relies on these.
        private const int timeoutSeconds = 15;

        private const int batchTimeoutSeconds = 30;

        public EventLoggingApi(string baseUrl = DefaultEndpoint)
        {
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
            {
                Debug.LogError($"{nameof(EventLoggingApi)} - Created with invalid URL: \"{baseUrl}\"\n"
                               + $"Fallback to default Endpoint: \"{DefaultEndpoint}\"");
                baseUrl = DefaultEndpoint; // Fall back to default Endpoint
            }

            _baseUrl = baseUrl;
        }

        public async UniTask<bool> Warmup(CancellationToken cancellationToken = default)
        {
            try
            {
                using var request = UnityWebRequest.Get(_baseUrl);
                request.timeout = timeoutSeconds;
                await request.SendWebRequestAsync(cancellationToken);
                return request.result == UnityWebRequest.Result.Success;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return false;
        }

        public async UniTask<bool> SendEventLog(EventLog log, CancellationToken cancellationToken = default)
        {
            try
            {
                using var request = new UnityWebRequest(LogsUri, UnityWebRequest.kHttpVerbPOST);
                request.timeout = timeoutSeconds;
                request.AddJsonBody(log);
                await request.SendWebRequestAsync(cancellationToken);
                return request.result == UnityWebRequest.Result.Success;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return false;
        }

        public async UniTask<bool> SendEventLogsBatch(
            List<EventLog> logs,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                using var batchRequest = new UnityWebRequest(LogsBatchUri, UnityWebRequest.kHttpVerbPOST);
                batchRequest.timeout = batchTimeoutSeconds;
                batchRequest.AddJsonBody(logs);
                await batchRequest.SendWebRequestAsync(cancellationToken);
                return batchRequest.result == UnityWebRequest.Result.Success;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return false;
        }

        public async UniTask<string[]> GetScenarioNames(CancellationToken cancellationToken = default)
        {
            try
            {
                var downloadHandlerBuffer = new DownloadHandlerBuffer();
                var uploadHandlerRaw = new UploadHandlerRaw(Array.Empty<byte>());
                using var request = new UnityWebRequest(ScenariosUri, UnityWebRequest.kHttpVerbGET, downloadHandlerBuffer,
                    uploadHandlerRaw);
                request.timeout = timeoutSeconds;
                request.SetRequestHeader("Accept", "application/json");
                await request.SendWebRequestAsync(cancellationToken);
                var result = request.result;
                if (result != UnityWebRequest.Result.Success) return Array.Empty<string>();
                var rawString = request.downloadHandler.text;

                return JsonUtilityExtensions.FromJsonArray<string>(rawString);
            }
            catch (Exception e)
            {
                Debug.LogError($"API: {e}");
                return Array.Empty<string>();
            }
        }
    }
}