using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Xprees.EventLogging.Api.Model;
using Xprees.EventLogging.Extensions;

namespace Xprees.EventLogging.Api
{
    public class EventLoggingApi : IEventLoggingApi
    {
        public const string DefaultEndpoint = "https://eventlog-service-phkfk465ha-ew.a.run.app";

        private readonly string _baseUrl;
        private string LogsUri => $"{_baseUrl}/logs";
        private string LogsBatchUri => $"{_baseUrl}/logs/batch";
        private string ScenariosUri => $"{_baseUrl}/logs/scenarios";

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
                var request = UnityWebRequest.Get(_baseUrl);
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
                var request = new UnityWebRequest(LogsUri, UnityWebRequest.kHttpVerbPOST);
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

        public async UniTask<bool> SendEventLogsBatch(List<EventLog> logs, CancellationToken cancellationToken = default)
        {
            try
            {
                var batchRequest = new UnityWebRequest(LogsBatchUri, UnityWebRequest.kHttpVerbPOST);
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
                var request = new UnityWebRequest(ScenariosUri, UnityWebRequest.kHttpVerbGET, downloadHandlerBuffer, uploadHandlerRaw);
                request.SetRequestHeader("Content-Type", "application/json");
                await request.SendWebRequestAsync(cancellationToken);
                var result = request.result;
                if (result != UnityWebRequest.Result.Success) return Array.Empty<string>();
                var rawString = request.downloadHandler.text;

                return JsonConvert.DeserializeObject<string[]>(rawString);
            }
            catch (Exception e)
            {
                Debug.LogError($"API: {e}");
                return Array.Empty<string>();
            }
        }
    }
}