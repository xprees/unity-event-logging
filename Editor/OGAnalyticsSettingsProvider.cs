using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1;

namespace Xprees.EventLogging.Editor
{
    /// <c>Project Settings > Open Game Analytics</c>: edit the connection config and verify it against the server.
    public class OGAnalyticsSettingsProvider : SettingsProvider
    {
        public const string DefaultConfigFolder = "Assets/Settings/OpenGameAnalytics";
        public const string DefaultConfigPath = OGAnalyticsConfigSO.DefaultAssetPath;
        private const string settingsPagePath = "Project/Open Game Analytics";

        private readonly static Color successColor = new(0.55f, 0.9f, 0.55f);
        private readonly static Color errorColor = new(1f, 0.6f, 0.6f);

        private OGAnalyticsConfigSO _config;
        private SerializedObject _serializedConfig;
        private bool _showKey;
        private CancellationTokenSource _cts;

        // Static: Unity may rebuild the provider (domain reload, settings refresh) while a test is running - the result has to survive that.
        private static bool _showTestSettings; // collapsed by default
        private static bool _testing;
        private static OGConnectionTestResult? _lastResult;

        private OGAnalyticsSettingsProvider() : base(settingsPagePath, SettingsScope.Project,
            new[] { "analytics", "event", "logging", "api key", "OG" })
        {
        }

        [SettingsProvider]
        public static SettingsProvider Create() => new OGAnalyticsSettingsProvider();

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            LoadConfig();
            _config ??= CreateDefaultConfig(); // the page is always usable, no manual "create" step
            OGAnalyticsPreloadRegistrar.Register(_config);
        }

        public override void OnDeactivate()
        {
            // Leaving the page must not throw away a running test - its result is shown when the page is opened again.
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUILayout.Space();

            if (!_config) return;

            // The global config is always the asset at the default path - other assets can only be used as explicit overrides.
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Config asset", _config, typeof(OGAnalyticsConfigSO), false);
            }

            DrawConfigWarnings();
            if (_serializedConfig == null || _serializedConfig.targetObject != _config) _serializedConfig = new SerializedObject(_config);
            _serializedConfig.Update();

            EditorGUI.BeginChangeCheck();

            // Connection
            EditorGUILayout.Space();
            DrawField(nameof(OGAnalyticsConfigSO.apiBaseUrl));
            DrawField(nameof(OGAnalyticsConfigSO.orgSlug));
            DrawField(nameof(OGAnalyticsConfigSO.appSlug));
            DrawKey();

            // Defaults
            EditorGUILayout.Space();
            DrawField(nameof(OGAnalyticsConfigSO.defaultGroup));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Upload settings", EditorStyles.boldLabel);
            DrawField(nameof(OGAnalyticsConfigSO.dontSendInEditor));
            DrawField(nameof(OGAnalyticsConfigSO.uploadIntervalSeconds));
            DrawField(nameof(OGAnalyticsConfigSO.uploadThreshold));
            DrawField(nameof(OGAnalyticsConfigSO.uploadOnApplicationQuit));
            DrawField(nameof(OGAnalyticsConfigSO.uploadOnDisable));
            DrawField(nameof(OGAnalyticsConfigSO.debugLogsEnabled));
            DrawField(nameof(OGAnalyticsConfigSO.rateLimitFallbackPauseSeconds));

            // Connection test
            EditorGUILayout.Space();
            _showTestSettings = EditorGUILayout.Foldout(_showTestSettings, "Connection test settings", true);
            if (_showTestSettings)
            {
                EditorGUI.indentLevel++;
                DrawField(nameof(OGAnalyticsConfigSO.validationScenarioSlug));
                DrawField(nameof(OGAnalyticsConfigSO.validationPlayerId));
                EditorGUI.indentLevel--;
            }
            else if (!_config.IsValidForTest && _config.IsConfigured)
            {
                // A collapsed foldout must not hide why the test button is disabled.
                DrawError(_config.Validate(nameof(OGAnalyticsConfigSO.validationScenarioSlug))
                          ?? _config.Validate(nameof(OGAnalyticsConfigSO.validationPlayerId)));
            }

            if (EditorGUI.EndChangeCheck())
            {
                _serializedConfig.ApplyModifiedProperties();
                EditorUtility.SetDirty(_config);
                _lastResult = null;
            }

            DrawTest();
        }

        private void DrawConfigWarnings()
        {
            if (AssetDatabase.GetAssetPath(_config) != DefaultConfigPath)
            {
                EditorGUILayout.HelpBox(
                    $"The global config should be at {DefaultConfigPath}. Move it there so every machine and CI use the same one.",
                    MessageType.Warning);
            }

            var others = OGAnalyticsPreloadRegistrar.FindOtherConfigPaths(_config);
            if (others.Length > 0)
            {
                EditorGUILayout.HelpBox(
                    "Other Open Game Analytics configs exist and are ignored as the global config (usable only as an override):\n" +
                    string.Join("\n", others), MessageType.Info);
            }
        }

        /// Draws the property followed by its validation error (if any).
        private void DrawField(string fieldName)
        {
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty(fieldName));
            DrawError(_config.Validate(fieldName));
        }

        private void DrawKey()
        {
            var key = _serializedConfig.FindProperty(nameof(OGAnalyticsConfigSO.anonKey));
            EditorGUILayout.BeginHorizontal();
            var label = new GUIContent("Anon key", key.tooltip);
            key.stringValue = _showKey
                ? EditorGUILayout.TextField(label, key.stringValue)
                : EditorGUILayout.PasswordField(label, key.stringValue);
            _showKey = GUILayout.Toggle(_showKey, "Show", "Button", GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
            DrawError(_config.Validate(nameof(OGAnalyticsConfigSO.anonKey)));
            DrawKeyExpiry();
        }

        /// Keys always expire (max 1 year). The expiry is read from the key itself (JWT <c>exp</c>), no API call needed.
        private void DrawKeyExpiry()
        {
            if (!_config.TryGetKeyExpiry(out var expiry)) return;

            var left = expiry - DateTimeOffset.UtcNow;
            var date = expiry.LocalDateTime.ToString("yyyy-MM-dd");
            if (left <= TimeSpan.Zero)
                EditorGUILayout.HelpBox($"The key expired on {date}. Create a new key in the dashboard and ship it.", MessageType.Error);
            else if (left.TotalDays < 30)
                EditorGUILayout.HelpBox($"The key expires on {date} ({(int) left.TotalDays} days left) - ship a new key before that.",
                    MessageType.Warning);
            else EditorGUILayout.HelpBox($"The key is valid until {date} ({(int) left.TotalDays} days left).", MessageType.None);
        }

        private static void DrawError(string error)
        {
            if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Error);
        }

        private void DrawTest()
        {
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(_testing || !_config.IsValidForTest))
            {
                if (GUILayout.Button(_testing ? "Testing..." : "Test connection", GUILayout.Height(26))) RunTest().Forget();
            }

            if (!_config.IsValidForTest) EditorGUILayout.HelpBox("Fix the settings above to test the connection.", MessageType.None);
            if (_lastResult is not { } result) return;

            var previousColor = GUI.color;
            GUI.color = result.IsOk ? successColor : errorColor;
            EditorGUILayout.HelpBox(result.IsOk ? $"SUCCESS - {result.Message}" : $"FAILED - {result.Message}",
                result.IsOk ? MessageType.Info : MessageType.Error);
            GUI.color = previousColor;
        }

        private async UniTaskVoid RunTest()
        {
            _testing = true;
            _lastResult = null;
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var config = _config;
            var token = _cts.Token;
            try
            {
                _lastResult = await OGAnalyticsConnectionTester.Test(config, token);
            }
            catch (Exception e)
            {
                _lastResult = new OGConnectionTestResult(OGConnectionStatus.Unknown, $"Test failed unexpectedly: {e.Message}");
                Debug.LogException(e);
            }
            finally
            {
                _testing = false;
                EditorApplication.delayCall += InternalEditorUtility.RepaintAllViews; // redraw the page, OnGUI is not called on its own
            }
        }

        private void LoadConfig() => _config = OGAnalyticsConfigSO.Instance;

        private static OGAnalyticsConfigSO CreateDefaultConfig()
        {
            EnsureFolder("Assets/Settings");
            EnsureFolder(DefaultConfigFolder);

            var config = ScriptableObject.CreateInstance<OGAnalyticsConfigSO>();
            AssetDatabase.CreateAsset(config, DefaultConfigPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created Open Game Analytics config at {DefaultConfigPath}.", config);
            return config;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path[..slash], path[(slash + 1)..]);
        }
    }
}