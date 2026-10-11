using System.Collections.Generic;
using System.Text;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1.Model;

namespace Xprees.EventLogging.Api.OpenGameAnalytics.V1.Util
{
    /// Minimal JSON writer for the v1 request bodies. JsonUtility can't omit empty fields (an empty <c>id</c>/<c>group</c> fails the server validation).
    public static class OGJsonWriter
    {
        public static string IngestRequest(IReadOnlyList<OGIngestEvent> events)
        {
            var sb = new StringBuilder(events.Count * 160 + 16);
            sb.Append("{\"events\":[");
            for (var i = 0; i < events.Count; i++)
            {
                if (i > 0) sb.Append(',');
                AppendEvent(sb, events[i]);
            }

            return sb.Append("]}").ToString();
        }

        public static string PlayerRequest(string name)
        {
            var sb = new StringBuilder("{");
            if (!string.IsNullOrEmpty(name)) AppendString(sb.Append("\"name\":"), name);
            return sb.Append('}').ToString();
        }

        public static string GroupRequest(string group) => AppendString(new StringBuilder("{\"group\":"), group).Append('}').ToString();

        private static void AppendEvent(StringBuilder sb, OGIngestEvent e)
        {
            sb.Append('{');
            if (!string.IsNullOrEmpty(e.id)) AppendString(sb.Append("\"id\":"), e.id).Append(',');
            AppendString(sb.Append("\"timestamp\":"), e.timestamp).Append(',');
            AppendString(sb.Append("\"playerId\":"), e.playerId).Append(',');
            if (!string.IsNullOrEmpty(e.group)) AppendString(sb.Append("\"group\":"), e.group).Append(',');
            AppendString(sb.Append("\"event\":"), e.@event);
            if (!string.IsNullOrEmpty(e.eventData)) AppendString(sb.Append(",\"eventData\":"), e.eventData);
            sb.Append('}');
        }

        private static StringBuilder AppendString(StringBuilder sb, string value)
        {
            sb.Append('"');
            foreach (var c in value ?? string.Empty)
            {
                switch (c)
                {
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    case < ' ':
                        sb.Append("\\u").Append(((int) c).ToString("x4"));
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return sb.Append('"');
        }
    }
}