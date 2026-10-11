using System;
using Xprees.EventLogging.Api.Legacy.Model;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1.Model;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1.Util;
using Xprees.EventLogging.ScriptableObjects;

namespace Xprees.EventLogging.Extensions
{
    public static class EventLoggingApiExtension
    {
        /// Creates a ready to queue ingest event with a fresh UUIDv7 id. The caller is expected to validate it
        /// (<see cref="OGValidation.ValidateEvent"/>) before queueing.
        public static OGIngestEvent ToIngestEvent(
            this EventSO @event,
            string playerId,
            string group,
            string scenarioSlug,
            DateTime timestamp
        ) => new()
        {
            id = Uuid7.New(),
            timestamp = timestamp.ToUniversalTime().ToString("O"),
            playerId = playerId,
            group = string.IsNullOrWhiteSpace(group) ? null : group.Trim(),
            @event = @event.eventName?.Trim(),
            eventData = @event.eventData?.Trim(),
            scenarioSlug = scenarioSlug,
        };

        [Obsolete("Legacy CF-Bucket API. Use ToIngestEvent (Open Game Analytics v1).")]
        public static EventLog GenerateEventLog(this EventSO @event, User user, DateTime timestamp) => new()
        {
            timestamp = timestamp.ToString("O"),
            user = user,
            scenario = @event.scenario?.Trim(),
            @event = @event.eventName.Trim(),
            eventData = @event.eventData?.Trim(),
        };
    }
}