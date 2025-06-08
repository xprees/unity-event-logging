using UnityEditor;
using UnityEngine;

namespace Xprees.EventLogging.Editor
{
    /// This class is responsible for setting up the package's XPREES_EVENT_LOGGING ScriptDefineSymbol in the Project Settings.
    public static class PackageSetup
    {
        private const string packageName = "cz.xprees.event-logging";
        private const string eventLoggingDefineSymbol = "XPREES_EVENT_LOGGING";

        /// This method checks shows the usage, by checking if the symbol is already defined in the project settings
        private static bool IsSymbolDefined()
        {
#if XPREES_EVENT_LOGGING
            return true;
#else
            return false;
#endif
        }


        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            var isAlreadyInitialized = IsSymbolDefined() || ScriptDefineUtility.ContainsDefineSymbol(eventLoggingDefineSymbol);
            if (isAlreadyInitialized) return;

            ScriptDefineUtility.AddDefineSymbol(eventLoggingDefineSymbol);
            Debug.Log($"Package {packageName} has been initialized. Define symbol '{eventLoggingDefineSymbol}' added to the project settings.");
        }

    }
}