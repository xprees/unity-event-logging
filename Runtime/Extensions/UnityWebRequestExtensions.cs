using System;
using System.Collections;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
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
            // JsonUtility can't serialize a root-level collection, so build the array manually for those.
            var json = body is IEnumerable items and not string
                ? JsonUtilityExtensions.ToJsonArray(items)
                : JsonUtility.ToJson(body);
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(jsonBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
        }
    }
}