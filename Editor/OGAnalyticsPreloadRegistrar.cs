using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1;

namespace Xprees.EventLogging.Editor
{
    /// Keeps the global config in PlayerSettings > Preloaded Assets, so builds load it at startup and <see cref="OGAnalyticsConfigSO.Instance"/>
    /// works without any scene reference.
    public class OGAnalyticsPreloadRegistrar : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var config = OGAnalyticsConfigSO.Instance;
            Register(config);

            var others = FindOtherConfigPaths(config);
            if (config && others.Length > 0)
            {
                Debug.LogWarning(
                    $"Open Game Analytics: the build uses the global config \"{AssetDatabase.GetAssetPath(config)}\"; ignored configs: {string.Join(", ", others)}.");
            }
        }

        /// Paths of every other config asset in the project (these are never the global config).
        public static string[] FindOtherConfigPaths(OGAnalyticsConfigSO global)
        {
            var globalPath = global ? AssetDatabase.GetAssetPath(global) : null;
            return AssetDatabase.FindAssets($"t:{nameof(OGAnalyticsConfigSO)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path != globalPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        [InitializeOnLoadMethod]
        private static void RegisterOnLoad() => EditorApplication.delayCall += () => Register(OGAnalyticsConfigSO.Instance);

        /// Registers the config as the only preloaded <see cref="OGAnalyticsConfigSO"/>.
        public static void Register(OGAnalyticsConfigSO config)
        {
            if (!config || !EditorUtility.IsPersistent(config)) return;

            var preloaded = PlayerSettings.GetPreloadedAssets().ToList();
            var changed = preloaded.RemoveAll(a => a is OGAnalyticsConfigSO other && other != config) > 0;
            changed |= preloaded.RemoveAll(a => !a) > 0;
            if (!preloaded.Contains(config))
            {
                preloaded.Add(config);
                changed = true;
            }

            if (changed) PlayerSettings.SetPreloadedAssets(preloaded.ToArray());
        }
    }
}