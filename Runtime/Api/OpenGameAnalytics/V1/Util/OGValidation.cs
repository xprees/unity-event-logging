using System.Text;

namespace Xprees.EventLogging.Api.OpenGameAnalytics.V1.Util
{
    /// Client side checks mirroring the API validation, so one bad event can't make the server reject a whole batch.
    public static class OGValidation
    {
        public const int MaxPlayerIdLength = 128;
        public const int MaxSlugLength = 100;
        public const int MaxEventNameLength = 200;
        public const int MaxEventDataBytes = 16 * 1024;
        public const int MaxBatchSize = 500;

        /// <c>^[A-Za-z0-9._:-]{1,128}$</c>
        public static bool IsValidPlayerId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > MaxPlayerIdLength) return false;
            foreach (var c in value)
            {
                if (!IsPlayerIdChar(c)) return false;
            }

            return true;
        }

        /// <c>^[a-z0-9._-]{1,100}$</c>
        public static bool IsValidSlug(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > MaxSlugLength) return false;
            foreach (var c in value)
            {
                if (!IsSlugChar(c)) return false;
            }

            return true;
        }

        /// Replaces every character outside the player id charset with <c>_</c> and truncates. Returns empty for empty input.
        public static string SanitizePlayerId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var sb = new StringBuilder(value.Length);
            foreach (var c in value.Trim())
            {
                if (sb.Length >= MaxPlayerIdLength) break;
                sb.Append(IsPlayerIdChar(c) ? c : '_');
            }

            return sb.ToString();
        }

        /// Lower-cases, turns whitespace into <c>-</c> and drops everything outside the slug charset. Keeps <c>_</c> and <c>.</c>.
        /// Returns empty when nothing usable is left.
        public static string Slugify(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var sb = new StringBuilder(value.Length);
            foreach (var raw in value.Trim())
            {
                if (sb.Length >= MaxSlugLength) break;
                var c = char.ToLowerInvariant(raw);
                if (char.IsWhiteSpace(c)) sb.Append('-');
                else if (IsSlugChar(c)) sb.Append(c);
            }

            return sb.ToString();
        }

        /// Returns null when the event is valid, otherwise the reason.
        public static string ValidateEvent(string playerId, string eventName, string eventData, string group)
        {
            if (string.IsNullOrWhiteSpace(eventName)) return "event name is empty";
            if (eventName.Trim().Length > MaxEventNameLength) return $"event name is longer than {MaxEventNameLength} chars";
            if (!IsValidPlayerId(playerId)) return $"playerId \"{playerId}\" is not 1-128 chars of A-Z a-z 0-9 . _ : -";
            if (!string.IsNullOrEmpty(group) && !IsValidSlug(group)) return $"group \"{group}\" is not a slug";
            if (!string.IsNullOrEmpty(eventData) && Encoding.UTF8.GetByteCount(eventData) > MaxEventDataBytes)
                return "eventData exceeds 16 KiB";
            return null;
        }

        private static bool IsPlayerIdChar(char c) =>
            c is >= 'A' and <= 'Z' || c is >= 'a' and <= 'z' || c is >= '0' and <= '9' || c is '.' or '_' or ':' or '-';

        private static bool IsSlugChar(char c) => c is >= 'a' and <= 'z' || c is >= '0' and <= '9' || c is '.' or '_' or '-';
    }
}