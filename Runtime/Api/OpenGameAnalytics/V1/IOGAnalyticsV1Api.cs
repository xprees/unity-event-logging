using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1.Model;

namespace Xprees.EventLogging.Api.OpenGameAnalytics.V1
{
    /// Client for the game-facing part of the Open Game Analytics v1 API (everything that works with the publishable anon key).
    public interface IOGAnalyticsV1Api
    {
        /// <c>POST /scenarios/{scenario}/events</c> - 1..500 events of ONE scenario, answered with <c>202</c> (<see cref="OGIngestResponse"/>).
        /// Duplicates (same event id) are counted, not errors. Group problems and rejected events are reported inside the <c>202</c>.
        UniTask<OGApiResult> IngestEvents(
            string scenarioSlug,
            IReadOnlyList<OGIngestEvent> events,
            CancellationToken cancellationToken = default
        );

        /// <c>GET /players/{playerId}</c> - reads the player profile.
        UniTask<OGApiResult> GetPlayer(string playerId, CancellationToken cancellationToken = default);

        /// <c>PUT /players/{playerId}</c> - idempotent create/update of the player profile.
        UniTask<OGApiResult> UpsertPlayer(string playerId, string name, CancellationToken cancellationToken = default);

        /// <c>PUT /scenarios/{scenario}/players/{playerId}/group</c> - the group is immutable once set (409 Group.Immutable).
        UniTask<OGApiResult> SetPlayerGroup(
            string scenarioSlug,
            string playerId,
            string group,
            CancellationToken cancellationToken = default
        );

        /// <c>GET /events/{id}</c>
        UniTask<OGApiResult> GetEventById(string eventId, CancellationToken cancellationToken = default);
    }
}