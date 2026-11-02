using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SceneSwitcher.Ui;
using UnityEngine.InputSystem;

namespace SceneSwitcher
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "abzu.SceneSwitcher";
        public const string PluginName = "SceneSwitcher";
        public const string PluginVersion = "0.3.0";

        internal static ManualLogSource Log;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(Plugin).Assembly);

            // Read live by the window, so changes made in game (e.g. with REPOConfig) apply at once.
            ConfigEntry<Key> windowKey = Config.Bind(
                "Window",
                "ToggleKey",
                Key.Insert,
                "Key that opens/closes the Scene Switcher window (host only). Set to None to disable the window.");
            SceneWindow.Create(windowKey);

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }
    }
}
