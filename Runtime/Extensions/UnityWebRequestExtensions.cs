using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Xprees.EventLogging.Extensions
{
    public static class UnityWebRequestExtensions
    {
        public async static UniTask SendWebRequestAsync(
            this UnityWebRequest request,
            CancellationToken cancellationToken = default,
            IProgress<float> progress = null
        )
        {
            try
            {
                await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken, progress: progress);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (UnityWebRequestException e)
            {
                if (e.Result == UnityWebRequest.Result.ConnectionError)
                {
                    Debug.LogError($"Connection error: {e.Message} - URL: {e.UnityWebRequest.url}");
                    return;
                }

                Debug.LogError($"Web request failed: {e.Message} with code {e.ResponseCode}");
            }
        }

        public static void AddJsonBody(this UnityWebRequest request, object body)
        {
            var json = JsonConvert.SerializeObject(body);
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(jsonBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
        }

        /// Tries to Request neverssl.com to check that internet connection is working 
        public async static UniTask<UnityWebRequest.Result> TestInternetConnection(CancellationToken cancellationToken = default)
        {
            var request = UnityWebRequest.Get("http://neverssl.com");
            try
            {
                await request.SendWebRequestAsync(cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (UnityWebRequestException e)
            {
                Debug.LogError($"Webex: {e.Message}");
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return request.result;
        }
    }
}