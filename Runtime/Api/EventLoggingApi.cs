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
    public class EventLoggingApi : IEventLoggingApi, IDisposable
    {
        public const string DefaultEndpoint = "https://eventlog-service-phkfk465ha-ew.a.run.app";
        private readonly CancellationTokenSource _cts = new();

        private readonly string _baseUrl;
        private string LogsUri => $"{_baseUrl}/logs";
        private string LogsBatchUri => $"{_baseUrl}/logs/batch";

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

        public async UniTask<bool> Warmup()
        {
            try
            {
                var request = UnityWebRequest.Get(_baseUrl);
                await request.SendWebRequestAsync(_cts.Token);
                return request.result == UnityWebRequest.Result.Success;
            }
            catch
            {
                // ignore
            }

            return false;
        }

        public async UniTask<bool> SendEventLog(EventLog log)
        {
            var request = new UnityWebRequest(LogsUri, UnityWebRequest.kHttpVerbPOST);
            request.AddJsonBody(log);
            await request.SendWebRequestAsync(_cts.Token);
            return request.result == UnityWebRequest.Result.Success;
        }

        public async UniTask<bool> SendEventLogsBatch(List<EventLog> logs)
        {
            var batchRequest = new UnityWebRequest(LogsBatchUri, UnityWebRequest.kHttpVerbPOST);
            batchRequest.AddJsonBody(logs);
            await batchRequest.SendWebRequestAsync(_cts.Token);
            return batchRequest.result == UnityWebRequest.Result.Success;
        }

        public void Dispose() => _cts?.Dispose();
    }
}