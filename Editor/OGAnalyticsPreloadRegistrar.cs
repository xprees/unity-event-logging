using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1;

namespace Xprees.EventLogging.Editor
{
    /// Keeps the global config in PlayerSettings > Preloaded Assets, so builds load it at startup and <see cref="OGAnalyticsConfigSO.Instance"/>
    /// works without any scene reference.
    public class OGAnalyticsPreloadRegistrar : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) => Register(OGAnalyticsConfigSO.Instance);

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