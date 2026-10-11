using System;

namespace Xprees.EventLogging.Api.Legacy.Model
{
    [Serializable]
    [Obsolete(
        "Legacy CF-Bucket API - the Open Game Analytics backend has no /logs/batch or /logs/scenarios. Use the v1 client. Removed in the next major version.")]
    public class EventLog
    {
        public long? id = null;
        public string scenario;
        public string timestamp;

        public string @event;
        public string eventData = null;

        public User user;
    }
}