using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Xprees.EventLogging.Api.Model;

namespace Xprees.EventLogging.Api
{
    public interface IEventLoggingApi
    {
        /// <summary>
        /// Call this method before logging start to warm up the connection (otherwise the first log will be slow)
        /// </summary>
        /// <returns>Success</returns>
        UniTask<bool> Warmup(CancellationToken cancellationToken = default);

        UniTask<bool> SendEventLog(EventLog log, CancellationToken cancellationToken = default);

        UniTask<bool> SendEventLogsBatch(List<EventLog> logs, CancellationToken cancellationToken = default);
    }
}