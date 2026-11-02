using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SceneSwitcher
{
    /// <summary>
    /// Performs the scene changes. Mirrors the vanilla "level" debug command
    /// (see docs/LEVEL_FLOW.md) so the game's own sync and save logic is reused.
    /// </summary>
    internal static class SceneSwitchService
    {
        // Private in DebugCommandHandler; reverts run progress to the last save in saved (private) lobbies.
        private static readonly MethodInfo ResetLevelProgressMethod =
            AccessTools.Method(typeof(DebugCommandHandler), "ResetLevelProgress");

        public static IEnumerable<Level> Maps => RunManager.instance.levels.Where(l => l);

        public static IEnumerable<Level> Arenas => RunManager.instance.levelArena.Where(l => l);

        public static IEnumerable<Level> Shops => RunManager.instance.levelShop.Where(l => l);

        public static Level Truck => RunManager.instance.levelLobby;

        /// <summary>Returns false with a user-facing reason when a scene change is not allowed right now.</summary>
        public static bool CanSwitch(out string reason)
        {
            RunManager rm = RunManager.instance;
            if (!rm)
            {
                reason = "Game is not ready.";
                return false;
            }
            if (!SemiFunc.IsMasterClientOrSingleplayer())
            {
                reason = "Only the host can change scenes.";
                return false;
            }
            if (SemiFunc.IsSplashScreen() || SemiFunc.IsMainMenu() || SemiFunc.RunIsTutorial())
            {
                reason = "Not available in this scene.";
                return false;
            }
            if (rm.restarting)
            {
                reason = "A scene change is already in progress.";
                return false;
            }
            reason = null;
            return true;
        }

        public static void ReturnToLobbyMenu()
        {
            RunManager rm = RunManager.instance;
            if (Settings.Progress.Value == ProgressMode.RevertToLastSave)
            {
                ResetLevelProgress();
            }
            if (!SemiFunc.RunIsLobbyMenu())
            {
                rm.loadLevel = rm.saveLevel;
            }
            // ChangeLevel skips its target selection while the current level is an arena,
            // which would just reload the arena. Pretend we are in the truck so LobbyMenu applies.
            if (SemiFunc.RunIsArena())
            {
                rm.levelCurrent = rm.levelLobby;
            }
            rm.ChangeLevel(_completedLevel: false, _levelFailed: false, RunManager.ChangeLevelType.LobbyMenu);
        }

        /// <param name="levelNumber">1-based level number; null keeps the current one.</param>
        public static void LaunchLevel(Level level, int? levelNumber)
        {
            RunManager rm = RunManager.instance;
            // Like the vanilla "level shop" command: going from a map to a shop always keeps the
            // run's progress (as if the level was completed); otherwise the setting decides.
            bool keepProgress = Settings.Progress.Value == ProgressMode.KeepCurrent
                || (SemiFunc.IsLevelShop(level) && SemiFunc.RunIsLevel());
            if (!keepProgress)
            {
                ResetLevelProgress();
            }
            if (levelNumber.HasValue)
            {
                // levelsCompleted drives difficulty and is sent to clients in UpdateLevelRPC.
                rm.levelsCompleted = levelNumber.Value - 1;
                SemiFunc.StatSetRunLevel(rm.levelsCompleted);
            }
            rm.debugLevel = level;
            try
            {
                rm.ChangeLevel(_completedLevel: false, _levelFailed: false);
            }
            finally
            {
                rm.debugLevel = null;
            }
        }

        public static Level PickRandomMap() => PickRandom(Maps);

        public static Level PickRandomShop() => PickRandom(Shops);

        /// <summary>Random level from the list, avoiding the current one when possible.</summary>
        private static Level PickRandom(IEnumerable<Level> levels)
        {
            List<Level> all = levels.ToList();
            List<Level> others = all.Where(l => l != RunManager.instance.levelCurrent).ToList();
            List<Level> pool = others.Count > 0 ? others : all;
            return pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : null;
        }

        private static void ResetLevelProgress()
        {
            if (DebugCommandHandler.instance && ResetLevelProgressMethod != null)
            {
                ResetLevelProgressMethod.Invoke(DebugCommandHandler.instance, null);
            }
            else
            {
                Plugin.Log.LogWarning("DebugCommandHandler.ResetLevelProgress not available; progress not reset.");
            }
        }
    }
}
