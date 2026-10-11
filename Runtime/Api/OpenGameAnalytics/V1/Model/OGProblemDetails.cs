using System;

namespace Xprees.EventLogging.Api.OpenGameAnalytics.V1.Model
{
    /// RFC 7807 problem body. Every error has a stable <c>code</c> (e.g. <c>ApiKey.Revoked</c>) - branch on it, never on <c>title</c>
    /// (that one is for humans). Validation problems list details in <c>errors</c>.
    [Serializable]
    public class OGProblemDetails
    {
        public string type;
        public string title;
        public int status;
        public string detail;
        public string code;

        public const string ValidationFailed = "Validation.Failed";
        public const string ApiKeyMissing = "ApiKey.Missing";
        public const string ApiKeyInvalid = "ApiKey.Invalid";
        public const string ApiKeyRevoked = "ApiKey.Revoked";
        public const string ApiKeyExpired = "ApiKey.Expired";
        public const string ApiKeyWrongApp = "ApiKey.WrongApp";
        public const string OrgNotFound = "Org.NotFound";
        public const string GameAppNotFound = "GameApp.NotFound";
        public const string ScenarioNotFound = "Scenario.NotFound";
        public const string GroupNotFound = "Group.NotFound";
        public const string GroupImmutable = "Group.Immutable";
        public const string GroupLimitReached = "Group.LimitReached";
        public const string GroupInvalidSlug = "Group.InvalidSlug";
        public const string PlayerInvalidId = "Player.InvalidId";
        public const string RateLimitExceeded = "RateLimit.Exceeded";
        public const string TimestampInFuture = "EventLog.TimestampInFuture";

        public bool Is(string errorCode) => code == errorCode;

        public string Identifier => code;
    }
}