using UnityEngine;

namespace Xprees.EventLogging.Api.OpenGameAnalytics.V1.Model
{
    /// Outcome of an API call. Carries the status + problem so callers can decide by error code instead of just success/fail.
    public class OGApiResult
    {
        public bool Success { get; set; }
        public long StatusCode { get; set; }

        /// Connection error, DNS failure, timeout... (no HTTP response).
        public bool NetworkError { get; set; }

        public bool Cancelled { get; set; }
        public string Body { get; set; }
        public OGProblemDetails Problem { get; set; }

        /// Seconds from the <c>Retry-After</c> header (429), 0 when missing/unreadable.
        public float RetryAfterSeconds { get; set; }

        public bool IsRateLimited => StatusCode == 429;
        public bool IsServerError => StatusCode >= 500;
        public bool IsRetryable => NetworkError || Cancelled || IsServerError || IsRateLimited;

        public bool HasProblem(string code) => Problem != null && Problem.Is(code);

        public string Describe() => NetworkError
            ? "network error"
            : $"HTTP {StatusCode} {Problem?.Identifier} {Problem?.detail}".TrimEnd();

        public T Parse<T>() where T : class
        {
            if (string.IsNullOrEmpty(Body)) return null;
            try
            {
                return JsonUtility.FromJson<T>(Body);
            }
            catch
            {
                return null;
            }
        }
    }
}