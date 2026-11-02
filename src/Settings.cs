using BepInEx.Configuration;
using UnityEngine.InputSystem;

namespace SceneSwitcher
{
    internal enum ProgressMode
    {
        /// <summary>Revert the run to the last save before switching (vanilla "level" command behavior).</summary>
        RevertToLastSave,

        /// <summary>Keep the current run state (money, upgrades, level) and carry it to the new scene.</summary>
        KeepCurrent
    }

    /// <summary>
    /// BepInEx config entries. Values are read when used, so changes made in game
    /// (e.g. with REPOConfig) apply immediately.
    /// </summary>
    internal static class Settings
    {
        public static ConfigEntry<Key> WindowToggleKey { get; private set; }

        public static ConfigEntry<ProgressMode> Progress { get; private set; }

        public static void Bind(ConfigFile config)
        {
            WindowToggleKey = config.Bind(
                "Window",
                "ToggleKey",
                Key.Insert,
                "Key that opens/closes the Scene Switcher window (host only). Set to None to disable the window.");

            Progress = config.Bind(
                "Saves",
                "ProgressOnSceneChange",
                ProgressMode.RevertToLastSave,
                "What happens to the run's progress in saved (private) lobbies when you switch scene. " +
                "RevertToLastSave: go back to the last save first. KeepCurrent: keep money, upgrades and level as they are now. " +
                "Going from a map to a shop always keeps progress. " +
                "Arenas are the Game Over screen: entering one deletes the save file, as in vanilla.");
        }
    }
}
