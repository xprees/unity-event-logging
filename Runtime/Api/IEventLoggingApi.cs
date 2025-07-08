using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Xprees.EventLogging.Api.Model;

namespace Xprees.EventLogging.Api
{
    public interface IEventLoggingApi
    {
        /// Call this method before logging start to warm up the connection (otherwise the first log will be slow)
        UniTask<bool> Warmup(CancellationToken cancellationToken = default);

        /// Sends a single event log to API endpoint, which will return success flag. It's recommended to use <seealso cref="SendEventLogsBatch"/> for most cases and best performance.
        UniTask<bool> SendEventLog(EventLog log, CancellationToken cancellationToken = default);

        /// Sends a batch of event logs to API endpoint, which will return success flag. This is the recommended way to send logs for better performance and minimized network traffic.
        UniTask<bool> SendEventLogsBatch(List<EventLog> logs, CancellationToken cancellationToken = default);

        /// Returns a list of all known scenario names from the API endpoint.
        UniTask<string[]> GetScenarioNames(CancellationToken cancellationToken = default);
    }
}