using System;

namespace Xprees.EventLogging.Api.OpenGameAnalytics.V1.Model
{
    /// Single event queued for ingestion. The <see cref="id"/> is the idempotency key, so a retried batch is deduplicated by the server.
    [Serializable]
    public class OGIngestEvent
    {
        /// Client-generated UUIDv7 - the idempotency key.
        public string id;

        /// ISO-8601 UTC time the event happened.
        public string timestamp;

        public string playerId;

        /// Optional group slug of the player in the scenario (empty = not sent).
        public string group;

        public string @event;

        /// Event payload sent as JSON string (server auto-cures bare strings). Empty = not sent.
        public string eventData;

        /// Scenario slug the event belongs to (part of the request URL, not of the body).
        public string scenarioSlug;
    }
}