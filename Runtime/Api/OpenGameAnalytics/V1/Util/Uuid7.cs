using System;
using System.Text;

namespace Xprees.EventLogging.Api.OpenGameAnalytics.V1.Util
{
    /// UUIDv7 (RFC 9562) generator: 48-bit unix-ms timestamp + random. Formatted straight from the bytes - going through
    /// <see cref="Guid"/>'s byte[] constructor would swap byte order and break the sortability.
    public static class Uuid7
    {
        private const string hex = "0123456789abcdef";

        public static string New() => New(DateTimeOffset.UtcNow);

        public static string New(DateTimeOffset time)
        {
            var ms = time.ToUnixTimeMilliseconds();
            var random = Guid.NewGuid().ToByteArray(); // bytes 0-5 and 9-15 carry no version/variant bits
            var b = new byte[16];

            for (var i = 0; i < 6; i++) b[i] = (byte) (ms >> (8 * (5 - i)));
            Array.Copy(random, 0, b, 6, 6);
            Array.Copy(random, 9, b, 12, 4);

            b[6] = (byte) ((b[6] & 0x0F) | 0x70); // version 7
            b[8] = (byte) ((b[8] & 0x3F) | 0x80); // variant 10xx

            var sb = new StringBuilder(36);
            for (var i = 0; i < 16; i++)
            {
                if (i is 4 or 6 or 8 or 10) sb.Append('-');
                sb.Append(hex[b[i] >> 4]).Append(hex[b[i] & 0x0F]);
            }

            return sb.ToString();
        }
    }
}