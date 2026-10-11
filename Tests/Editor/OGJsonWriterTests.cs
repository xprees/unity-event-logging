using System;
using System.Collections.Generic;
using NUnit.Framework;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1.Model;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1.Util;

namespace Xprees.EventLogging.Tests
{
    public class OGJsonWriterTests
    {
        private static OGIngestEvent Event(
            string id = "01900000-0000-7000-8000-000000000001",
            string group = "control-a",
            string data = "{\"a\":1}"
        ) =>
            new()
            {
                id = id,
                timestamp = "2026-10-11T12:00:00.0000000Z",
                playerId = "player-1",
                group = group,
                @event = "level_done",
                eventData = data,
            };

        [Test]
        public void IngestRequest_WrapsEventsInAnArray()
        {
            var json = OGJsonWriter.IngestRequest(new List<OGIngestEvent> { Event(), Event(id: "x") });

            Assert.IsTrue(json.StartsWith("{\"events\":["));
            Assert.IsTrue(json.EndsWith("]}"));
            Assert.AreEqual(2, CountOccurrences(json, "\"playerId\""));
        }

        [Test]
        public void IngestRequest_WritesAllFieldsOfAnEvent()
        {
            var json = OGJsonWriter.IngestRequest(new List<OGIngestEvent> { Event() });

            StringAssert.Contains("\"id\":\"01900000-0000-7000-8000-000000000001\"", json);
            StringAssert.Contains("\"timestamp\":\"2026-10-11T12:00:00.0000000Z\"", json);
            StringAssert.Contains("\"playerId\":\"player-1\"", json);
            StringAssert.Contains("\"group\":\"control-a\"", json);
            StringAssert.Contains("\"event\":\"level_done\"", json);
        }

        [Test]
        public void IngestRequest_SendsEventDataAsAJsonString()
        {
            var json = OGJsonWriter.IngestRequest(new List<OGIngestEvent> { Event(data: "{\"a\":1}") });

            StringAssert.Contains("\"eventData\":\"{\\\"a\\\":1}\"", json);
        }

        [Test]
        public void IngestRequest_OmitsEmptyOptionalFields()
        {
            var json = OGJsonWriter.IngestRequest(new List<OGIngestEvent> { Event(id: "", group: "", data: null) });

            StringAssert.DoesNotContain("\"id\"", json);
            StringAssert.DoesNotContain("\"group\"", json);
            StringAssert.DoesNotContain("\"eventData\"", json);
        }

        [Test]
        public void IngestRequest_EscapesSpecialCharacters()
        {
            var e = Event();
            e.@event = "quote\" back\\slash\nnewline\ttab";

            var json = OGJsonWriter.IngestRequest(new List<OGIngestEvent> { e });

            StringAssert.Contains("quote\\\" back\\\\slash\\nnewline\\ttab", json);
            StringAssert.DoesNotContain("\n", json);
        }

        [Test]
        public void PlayerRequest_OmitsAnEmptyName()
        {
            Assert.AreEqual("{}", OGJsonWriter.PlayerRequest(null));
            Assert.AreEqual("{\"name\":\"Ann\"}", OGJsonWriter.PlayerRequest("Ann"));
        }

        [Test]
        public void GroupRequest_WritesTheGroup() => Assert.AreEqual("{\"group\":\"control-a\"}", OGJsonWriter.GroupRequest("control-a"));

        private static int CountOccurrences(string text, string value)
        {
            var count = 0;
            for (var i = text.IndexOf(value, StringComparison.Ordinal); i >= 0; i = text.IndexOf(value, i + 1, StringComparison.Ordinal))
            {
                count++;
            }

            return count;
        }
    }
}