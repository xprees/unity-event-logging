using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace Xprees.EventLogging.Editor
{
    public static class ScriptDefineUtility
    {
        public static NamedBuildTarget[] AllTargets => new[]
        {
            NamedBuildTarget.Standalone,
            NamedBuildTarget.Android,
            NamedBuildTarget.WebGL,
            NamedBuildTarget.iOS,
            NamedBuildTarget.tvOS,
            NamedBuildTarget.XboxOne,
            NamedBuildTarget.PS4,
            NamedBuildTarget.PS5,
            NamedBuildTarget.NintendoSwitch,
            NamedBuildTarget.WindowsStoreApps,
            NamedBuildTarget.EmbeddedLinux,
            NamedBuildTarget.Server,
            NamedBuildTarget.LinuxHeadlessSimulation,
            NamedBuildTarget.Unknown,
            NamedBuildTarget.VisionOS,
            NamedBuildTarget.QNX,
        };

        /// Adds the given scripting define symbol to all named build targets.
        public static void AddDefineSymbol(string symbol)
        {
            foreach (var buildTarget in AllTargets)
            {
                PlayerSettings.GetScriptingDefineSymbols(buildTarget, out var defines);
                if (defines.Contains(symbol)) continue;

                var updatedDefines = defines.Concat(new[] { symbol }).ToArray();
                PlayerSettings.SetScriptingDefineSymbols(buildTarget, updatedDefines);
            }
        }

        /// Checks if the given scripting define symbol is present in all/selected named build targets.
        public static bool ContainsDefineSymbol(string symbol, NamedBuildTarget[] targets = null)
        {
            foreach (var buildTarget in targets ?? AllTargets)
            {
                PlayerSettings.GetScriptingDefineSymbols(buildTarget, out var defines);
                if (!defines.Contains(symbol)) return false;
            }

            return true;
        }

        /// Removes the given scripting define symbol from all named build targets.
        public static void RemoveDefineSymbol(string symbol)
        {
            foreach (var buildTarget in AllTargets)
            {
                PlayerSettings.GetScriptingDefineSymbols(buildTarget, out var defines);
                if (!defines.Contains(symbol)) continue;

                var updatedDefines = defines.Where(d => d != symbol).ToArray();
                PlayerSettings.SetScriptingDefineSymbols(buildTarget, updatedDefines);
            }
        }
    }
}