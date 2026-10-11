using System;
using System.Linq;
using NUnit.Framework;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1.Util;

namespace Xprees.EventLogging.Tests
{
    public class Uuid7Tests
    {
        [Test]
        public void New_HasCanonicalFormat()
        {
            var id = Uuid7.New();

            Assert.AreEqual(36, id.Length);
            Assert.AreEqual('-', id[8]);
            Assert.AreEqual('-', id[13]);
            Assert.AreEqual('-', id[18]);
            Assert.AreEqual('-', id[23]);
            Assert.IsTrue(id.Replace("-", "").All(Uri.IsHexDigit));
            Assert.AreEqual(id.ToLowerInvariant(), id);
        }

        [Test]
        public void New_SetsVersion7AndRfcVariant()
        {
            for (var i = 0; i < 100; i++)
            {
                var id = Uuid7.New();
                Assert.AreEqual('7', id[14], id);
                Assert.Contains(id[19], new[] { '8', '9', 'a', 'b' }, id);
            }
        }

        [Test]
        public void New_EncodesTheTimestampInTheFirst48Bits()
        {
            var time = new DateTimeOffset(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);

            var id = Uuid7.New(time);

            var ms = Convert.ToInt64(id.Substring(0, 8) + id.Substring(9, 4), 16);
            Assert.AreEqual(time.ToUnixTimeMilliseconds(), ms);
        }

        [Test]
        public void New_IsSortableByTime()
        {
            var earlier = Uuid7.New(DateTimeOffset.UtcNow.AddMinutes(-1));
            var later = Uuid7.New(DateTimeOffset.UtcNow);

            Assert.Less(string.CompareOrdinal(earlier, later), 0);
        }

        [Test]
        public void New_IsUnique()
        {
            var ids = Enumerable.Range(0, 1000).Select(_ => Uuid7.New()).ToList();

            Assert.AreEqual(ids.Count, ids.Distinct().Count());
        }
    }
}