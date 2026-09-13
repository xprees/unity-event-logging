using System;

namespace Xprees.EventLogging.Api.Model
{
    [Serializable]
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