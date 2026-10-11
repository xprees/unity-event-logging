using System;
using UnityEditor;
using UnityEngine;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1.Util;

namespace Xprees.EventLogging.Api.OpenGameAnalytics.V1
{
    /// Connection settings of the Open Game Analytics game app (copy them from the dashboard's key snippet).
    /// Edit it in <c>Project Settings > Open Game Analytics</c>.
    [CreateAssetMenu(menuName = "Settings/Open Game Analytics Config", fileName = "OGAnalyticsConfig")]
    public class OGAnalyticsConfigSO : ScriptableObject
    {
        [Header("Server settings")]
        [Tooltip("API root starting with http:// or https://, e.g. https://analytics.example.com (no trailing path).")]
        public string apiBaseUrl = "";

        [Tooltip("Organization slug (the {org} part of the URL).")]
        public string orgSlug = "";

        [Tooltip("Game app slug (the {app} part of the URL).")]
        public string appSlug = "";

        [Tooltip(
            "Publishable per-GameApp key (X-Api-Key). Safe to ship in a build. Keys always expire (max 1 year) - ship a new one before that.")]
        public string anonKey = "";

        [Header("Defaults")]
        [Tooltip("Optional group slug (a-z 0-9 . _ -) stamped on events when the player has none set, e.g. control-a.")]
        public string defaultGroup = "";

        [Header("Upload settings")]
        [Tooltip("Log upload progress and logged events into the console.")]
        public bool debugLogsEnabled = true;

        [Tooltip("Upload the collected events every N seconds. 0 = no periodic upload.")]
        [Min(0f)]
        public float uploadIntervalSeconds = 30f;

        [Tooltip("Upload as soon as N events are waiting, without waiting for the interval. 0 = no threshold.")]
        [Min(0)]
        public int uploadThreshold;

        [Tooltip("Upload the remaining events when the application quits.")]
        public bool uploadOnApplicationQuit = true;

        [Tooltip("Upload the remaining events when the sender is disabled.")]
        public bool uploadOnDisable = true;

        [Tooltip("Don't upload anything while running in the Unity Editor (events are discarded).")]
        public bool dontSendInEditor = false;

        [Tooltip("Seconds to pause uploads after a 429 that carries no readable Retry-After header.")]
        [Min(1f)]
        public float rateLimitFallbackPauseSeconds = 5f;

        [Header("Connection Test")]
        [Tooltip(
            "Scenario slug the 'Test connection' button sends its sample event to. Create it in the dashboard first (or allow auto-creation).")]
        public string validationScenarioSlug = "connection-test";

        [Tooltip("Player id used by the 'Test connection' sample event.")]
        public string validationPlayerId = "settings-test";

        /// The global config always lives here. Any other config asset is only usable as an explicit override (e.g. a component's field).
        public const string DefaultAssetPath = "Assets/Settings/OpenGameAnalytics/OGAnalyticsConfig.asset";

        private static OGAnalyticsConfigSO instance;

        /// The global config. In the Editor it is the asset at <see cref="DefaultAssetPath"/> (so builds and CI agree on it); at runtime it is
        /// the first config loaded at startup, which is the one registered in Preloaded Assets (scene-referenced overrides load later).
        /// Null when no config exists.
        public static OGAnalyticsConfigSO Instance
        {
            get
            {
                if (instance) return instance;
#if UNITY_EDITOR
                instance = FindInEditor();
#else
                var loaded = Resources.FindObjectsOfTypeAll<OGAnalyticsConfigSO>();
                if (loaded.Length > 0) instance = loaded[0];
#endif
                return instance;
            }
        }

#if UNITY_EDITOR
        private static OGAnalyticsConfigSO FindInEditor()
        {
            var config = AssetDatabase.LoadAssetAtPath<OGAnalyticsConfigSO>(DefaultAssetPath);
            if (config) return config;

            // Config moved away from the default path - still usable, but the settings page and the build warn about it.
            var guids = AssetDatabase.FindAssets($"t:{nameof(OGAnalyticsConfigSO)}");
            Array.Sort(guids, (x, y) => string.CompareOrdinal(AssetDatabase.GUIDToAssetPath(x),
                AssetDatabase.GUIDToAssetPath(y)));
            return guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<OGAnalyticsConfigSO>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }

        private void OnDisable() => instance = instance == this ? null : instance;
#else
        private void OnEnable()
        {
            if (!instance) instance = this; // the preloaded config is loaded before any scene, so it wins over scene-referenced overrides
        }
#endif

        /// All connection fields are valid, so requests can be sent.
        public bool IsConfigured => Validate(nameof(apiBaseUrl)) == null
                                    && Validate(nameof(orgSlug)) == null
                                    && Validate(nameof(appSlug)) == null
                                    && Validate(nameof(anonKey)) == null;

        /// Everything the connection test needs is valid (connection + test scenario/player).
        public bool IsValidForTest => IsConfigured
                                      && Validate(nameof(validationScenarioSlug)) == null
                                      && Validate(nameof(validationPlayerId)) == null;

        /// Expiry of the anon key (read from the JWT payload), false when the key is not a readable JWT.
        public bool TryGetKeyExpiry(out DateTimeOffset expiry) => OGJwt.TryGetExpiry(anonKey, out expiry);

        /// Returns the problem of the field (use <c>nameof(field)</c>), or null when the value is fine.
        public string Validate(string fieldName) => fieldName switch
        {
            nameof(apiBaseUrl) => ValidateBaseUrl(apiBaseUrl),
            nameof(orgSlug) => ValidateRequiredSlug(orgSlug, "Org slug"),
            nameof(appSlug) => ValidateRequiredSlug(appSlug, "App slug"),
            nameof(anonKey) => ValidateKey(anonKey),
            nameof(defaultGroup) => string.IsNullOrWhiteSpace(defaultGroup) ? null : ValidateRequiredSlug(defaultGroup, "Group"),
            nameof(validationScenarioSlug) => ValidateRequiredSlug(validationScenarioSlug, "Test scenario slug"),
            nameof(validationPlayerId) => OGValidation.IsValidPlayerId(validationPlayerId?.Trim())
                ? null
                : "Test player id must be 1-128 chars of A-Z a-z 0-9 . _ : -",
            _ => null,
        };

        private static string ValidateBaseUrl(string value)
        {
            var url = value?.Trim();
            if (string.IsNullOrEmpty(url)) return "Base URL is required.";
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return "Base URL has to start with http:// or https://";
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host)) return "Base URL is not a valid URL.";
            if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
                return "Base URL must not contain a query or fragment.";
            return null;
        }

        private static string ValidateRequiredSlug(string value, string label)
        {
            var slug = value?.Trim();
            if (string.IsNullOrEmpty(slug)) return $"{label} is required.";
            return OGValidation.IsValidSlug(slug) ? null : $"{label} must be a slug: a-z 0-9 . _ - (lower case, max 100 chars).";
        }

        private static string ValidateKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Anon key is required (create it in the dashboard).";
            foreach (var c in value.Trim())
            {
                if (char.IsWhiteSpace(c)) return "Anon key must not contain whitespace.";
            }

            return null;
        }
    }
}