using System;

namespace Xprees.EventLogging.Api.OpenGameAnalytics.V1.Model
{
    [Serializable]
    public class OGIngestResponse
    {
        /// Events stored.
        public int accepted;

        /// Ids that were already stored (retries) - counted, not an error.
        public int duplicates;

        /// Groups that could not be assigned. The events are still stored, just without a group - stop sending that group.
        public OGGroupError[] groupErrors;

        /// Events dropped for a reason a retry will not fix (e.g. a wrong device clock). Do not resend them.
        public OGRejectedEvent[] rejected;
    }

    [Serializable]
    public class OGGroupError
    {
        public string group;

        /// <c>Group.NotFound</c>, <c>Group.LimitReached</c> or <c>Group.InvalidSlug</c>.
        public string code;
    }

    [Serializable]
    public class OGRejectedEvent
    {
        public string id;

        /// e.g. <c>EventLog.TimestampInFuture</c> (more than 24 h ahead of the server clock).
        public string code;
    }
}