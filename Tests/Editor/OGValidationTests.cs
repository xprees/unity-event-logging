using NUnit.Framework;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1.Util;

namespace Xprees.EventLogging.Tests
{
    public class OGValidationTests
    {
        [TestCase("player-1", true)]
        [TestCase("playID_abc.DEF:1-2", true)]
        [TestCase("", false)]
        [TestCase(null, false)]
        [TestCase("has space", false)]
        [TestCase("slash/id", false)]
        [TestCase("diakritika-š", false)]
        public void IsValidPlayerId(string value, bool expected) => Assert.AreEqual(expected, OGValidation.IsValidPlayerId(value));

        [Test]
        public void IsValidPlayerId_EnforcesMaxLength()
        {
            Assert.IsTrue(OGValidation.IsValidPlayerId(new string('a', OGValidation.MaxPlayerIdLength)));
            Assert.IsFalse(OGValidation.IsValidPlayerId(new string('a', OGValidation.MaxPlayerIdLength + 1)));
        }

        [TestCase("day_one_ga26", true)]
        [TestCase("control-a", true)]
        [TestCase("a.b_c-d", true)]
        [TestCase("Upper", false)]
        [TestCase("with space", false)]
        [TestCase("colon:no", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void IsValidSlug(string value, bool expected) => Assert.AreEqual(expected, OGValidation.IsValidSlug(value));

        [Test]
        public void IsValidSlug_EnforcesMaxLength()
        {
            Assert.IsTrue(OGValidation.IsValidSlug(new string('a', OGValidation.MaxSlugLength)));
            Assert.IsFalse(OGValidation.IsValidSlug(new string('a', OGValidation.MaxSlugLength + 1)));
        }

        [TestCase("Day One", "day-one")]
        [TestCase("Matej_test", "matej_test")]
        [TestCase("  Scenario 2!  ", "scenario-2")]
        [TestCase("a.b_c", "a.b_c")]
        [TestCase("Třída_1", "trida_1")]
        [TestCase("Žluťoučký kůň", "zlutoucky-kun")]
        [TestCase("???", "")]
        [TestCase("", "")]
        [TestCase(null, "")]
        public void Slugify(string value, string expected) => Assert.AreEqual(expected, OGValidation.Slugify(value));

        [Test]
        public void Slugify_ResultIsAlwaysAValidSlugOrEmpty()
        {
            foreach (var input in new[] { "Hello World", "ÁČĎ test", "a/b\\c", new string('X', 300) })
            {
                var slug = OGValidation.Slugify(input);
                Assert.IsTrue(slug.Length == 0 || OGValidation.IsValidSlug(slug), $"'{input}' -> '{slug}'");
            }
        }

        [Test]
        public void SanitizePlayerId_ReplacesInvalidCharsAndTruncates()
        {
            Assert.AreEqual("a_b_c", OGValidation.SanitizePlayerId("a b/c"));
            Assert.AreEqual(string.Empty, OGValidation.SanitizePlayerId("  "));
            Assert.AreEqual(OGValidation.MaxPlayerIdLength, OGValidation.SanitizePlayerId(new string('a', 500)).Length);
        }

        [Test]
        public void ValidateEvent_AcceptsAValidEvent() =>
            Assert.IsNull(OGValidation.ValidateEvent("player-1", "level_done", "{}", "control-a"));

        [Test]
        public void ValidateEvent_AcceptsMissingGroupAndData() =>
            Assert.IsNull(OGValidation.ValidateEvent("player-1", "level_done", null, null));

        [Test]
        public void ValidateEvent_RejectsBadInput()
        {
            Assert.IsNotNull(OGValidation.ValidateEvent("player-1", " ", null, null), "empty name");
            Assert.IsNotNull(OGValidation.ValidateEvent("player-1", new string('e', OGValidation.MaxEventNameLength + 1), null, null),
                "long name");
            Assert.IsNotNull(OGValidation.ValidateEvent("bad id", "e", null, null), "player id");
            Assert.IsNotNull(OGValidation.ValidateEvent("player-1", "e", null, "Not A Slug"), "group");
            Assert.IsNotNull(OGValidation.ValidateEvent("player-1", "e", new string('d', OGValidation.MaxEventDataBytes + 1), null),
                "data size");
        }
    }
}