using System;
using Xprees.EventLogging.Api.Model;
using Xprees.EventLogging.ScriptableObjects;

namespace Xprees.EventLogging.Extensions
{
    public static class EventLoggingApiExtension
    {
        public static EventLog GenerateEventLog(this EventSO @event, User user, DateTime timestamp) => new()
        {
            timestamp = timestamp,
            user = user,
            scenario = @event.scenario?.Trim(),
            @event = @event.eventName.Trim(),
            eventData = @event.eventData?.Trim(),
        };
    }
}