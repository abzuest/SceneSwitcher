using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace SceneSwitcher
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "abzu.SceneSwitcher";
        public const string PluginName = "SceneSwitcher";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(Plugin).Assembly);
            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }
    }
}
