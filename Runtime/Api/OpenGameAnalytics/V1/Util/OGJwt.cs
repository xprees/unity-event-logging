using System;
using System.Text;
using UnityEngine;

namespace Xprees.EventLogging.Api.OpenGameAnalytics.V1.Util
{
    /// Anon keys are JWTs. The expiry is readable from the payload, no API call needed.
    public static class OGJwt
    {
        [Serializable]
        private class Claims
        {
            public long exp;
        }

        public static bool TryGetExpiry(string jwt, out DateTimeOffset expiry)
        {
            expiry = default;
            var parts = jwt?.Trim().Split('.');
            if (parts is not { Length: 3 }) return false;

            try
            {
                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                var claims = JsonUtility.FromJson<Claims>(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
                if (claims == null || claims.exp <= 0) return false;

                expiry = DateTimeOffset.FromUnixTimeSeconds(claims.exp);
                return true;
            }
            catch (Exception)
            {
                return false; // not a JWT - the server decides whether the key is valid
            }
        }
    }
}