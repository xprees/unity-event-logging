using System;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1.Model;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1.Util;
using Object = UnityEngine.Object;

namespace Xprees.EventLogging.Tests
{
    public class OGConfigAndModelTests
    {
        private OGAnalyticsConfigSO _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<OGAnalyticsConfigSO>();
            _config.apiBaseUrl = "https://analytics.example.com";
            _config.orgSlug = "my-org";
            _config.appSlug = "my-app";
            _config.anonKey = "key";
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void ValidConfig_IsConfigured() => Assert.IsTrue(_config.IsConfigured);

        [TestCase("")]
        [TestCase("analytics.example.com")]
        [TestCase("ftp://analytics.example.com")]
        [TestCase("https://analytics.example.com?x=1")]
        [TestCase("https://analytics.example.com#frag")]
        public void InvalidBaseUrl_IsReported(string url)
        {
            _config.apiBaseUrl = url;

            Assert.IsNotNull(_config.Validate(nameof(OGAnalyticsConfigSO.apiBaseUrl)));
            Assert.IsFalse(_config.IsConfigured);
        }

        [TestCase("http://localhost:5245")]
        [TestCase("HTTPS://analytics.example.com/")]
        public void ValidBaseUrl_IsAccepted(string url)
        {
            _config.apiBaseUrl = url;

            Assert.IsNull(_config.Validate(nameof(OGAnalyticsConfigSO.apiBaseUrl)));
        }

        [Test]
        public void SlugsAndKey_AreValidated()
        {
            _config.orgSlug = "Bad Org";
            _config.appSlug = "";
            _config.anonKey = "has space";

            Assert.IsNotNull(_config.Validate(nameof(OGAnalyticsConfigSO.orgSlug)));
            Assert.IsNotNull(_config.Validate(nameof(OGAnalyticsConfigSO.appSlug)));
            Assert.IsNotNull(_config.Validate(nameof(OGAnalyticsConfigSO.anonKey)));
        }

        [Test]
        public void DefaultGroup_IsOptionalButMustBeASlug()
        {
            _config.defaultGroup = "";
            Assert.IsNull(_config.Validate(nameof(OGAnalyticsConfigSO.defaultGroup)));

            _config.defaultGroup = "Not A Slug";
            Assert.IsNotNull(_config.Validate(nameof(OGAnalyticsConfigSO.defaultGroup)));
        }

        [Test]
        public void IsValidForTest_NeedsTheTestFieldsToo()
        {
            Assert.IsTrue(_config.IsValidForTest);

            _config.validationPlayerId = "bad id";

            Assert.IsTrue(_config.IsConfigured);
            Assert.IsFalse(_config.IsValidForTest);
        }

        [Test]
        public void KeyExpiry_IsReadFromAJwt()
        {
            var expiry = new DateTimeOffset(2027, 1, 8, 0, 0, 0, TimeSpan.Zero);
            _config.anonKey = Jwt($"{{\"exp\":{expiry.ToUnixTimeSeconds()}}}");

            Assert.IsTrue(_config.TryGetKeyExpiry(out var read));
            Assert.AreEqual(expiry, read);
        }

        [TestCase("not-a-jwt")]
        [TestCase("a.b")]
        [TestCase("")]
        public void KeyExpiry_IsUnknownForOtherKeys(string key)
        {
            _config.anonKey = key;

            Assert.IsFalse(_config.TryGetKeyExpiry(out _));
        }

        [Test]
        public void KeyExpiry_IsUnknownWithoutAnExpClaim()
        {
            Assert.IsFalse(OGJwt.TryGetExpiry(Jwt("{\"sub\":\"x\"}"), out _));
        }

        [Test]
        public void IngestResponse_ParsesGroupErrorsAndRejected()
        {
            const string body =
                "{\"accepted\":3,\"duplicates\":1,\"groupErrors\":[{\"group\":\"g1\",\"code\":\"Group.NotFound\"}],"
                + "\"rejected\":[{\"id\":\"01900000-0000-7000-8000-000000000001\",\"code\":\"EventLog.TimestampInFuture\"}]}";

            var result = new OGApiResult { Success = true, StatusCode = 202, Body = body }.Parse<OGIngestResponse>();

            Assert.AreEqual(3, result.accepted);
            Assert.AreEqual(1, result.duplicates);
            Assert.AreEqual("g1", result.groupErrors[0].group);
            Assert.AreEqual("Group.NotFound", result.groupErrors[0].code);
            Assert.AreEqual(OGProblemDetails.TimestampInFuture, result.rejected[0].code);
        }

        [Test]
        public void ApiResult_ParseReturnsNullForBadBodies()
        {
            Assert.IsNull(new OGApiResult { Body = "" }.Parse<OGIngestResponse>());
            Assert.IsNull(new OGApiResult { Body = "<html>" }.Parse<OGIngestResponse>());
        }

        [Test]
        public void ApiResult_DecidesByProblemCode()
        {
            var result = new OGApiResult { StatusCode = 404, Problem = new OGProblemDetails { code = OGProblemDetails.ScenarioNotFound } };

            Assert.IsTrue(result.HasProblem(OGProblemDetails.ScenarioNotFound));
            Assert.IsFalse(result.HasProblem(OGProblemDetails.GameAppNotFound));
        }

        [Test]
        public void ApiResult_RetryableStatuses()
        {
            Assert.IsTrue(new OGApiResult { StatusCode = 429 }.IsRetryable);
            Assert.IsTrue(new OGApiResult { StatusCode = 503 }.IsRetryable);
            Assert.IsTrue(new OGApiResult { NetworkError = true }.IsRetryable);
            Assert.IsFalse(new OGApiResult { StatusCode = 400 }.IsRetryable);
            Assert.IsFalse(new OGApiResult { StatusCode = 401 }.IsRetryable);
        }

        private static string Jwt(string payloadJson)
        {
            var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return $"header.{payload}.signature";
        }
    }
}