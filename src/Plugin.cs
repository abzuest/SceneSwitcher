using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SceneSwitcher.Ui;

namespace SceneSwitcher
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "abzu.SceneSwitcher";
        public const string PluginName = "SceneSwitcher";
        public const string PluginVersion = "0.4.0";

        internal static ManualLogSource Log;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            bool started = CrashReporter.Guard("startup", () =>
            {
                _harmony = new Harmony(PluginGuid);
                _harmony.PatchAll(typeof(Plugin).Assembly);

                Settings.Bind(Config);
                SceneWindow.Create(Settings.WindowToggleKey);
            });
            if (!started)
            {
                return;
            }

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
            // Easter egg: intentionally in Spanish.
            Log.LogInfo("Todo inició correctamente, chupala Crimson");
        }
    }
}
