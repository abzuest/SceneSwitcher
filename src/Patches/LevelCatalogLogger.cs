using System.Linq;
using HarmonyLib;

namespace SceneSwitcher.Patches
{
    /// <summary>
    /// Discovery aid: once a level has finished generating, dump every level
    /// RunManager knows about (vanilla + modded) to the BepInEx log, once per session.
    /// </summary>
    [HarmonyPatch(typeof(LevelGenerator), "GenerateDoneApply")]
    internal static class LevelCatalogLogger
    {
        private static bool _logged;

        [HarmonyPostfix]
        private static void Postfix()
        {
            if (_logged || RunManager.instance == null)
            {
                return;
            }
            _logged = true;
            CrashReporter.Guard("level catalog", LogCatalog);
        }

        private static void LogCatalog()
        {
            RunManager rm = RunManager.instance;
            Plugin.Log.LogInfo($"Current level: {rm.levelCurrent?.name} | levelsCompleted: {rm.levelsCompleted}");
            Plugin.Log.LogInfo($"Lobby: {rm.levelLobby?.name} | Lobby menu: {rm.levelLobbyMenu?.name}");
            Plugin.Log.LogInfo("Arenas: " + string.Join(", ", rm.levelArena.Where(l => l).Select(l => l.name)));
            Plugin.Log.LogInfo("Shops: " + string.Join(", ", rm.levelShop.Where(l => l).Select(l => l.name)));
            Plugin.Log.LogInfo("Run levels: " + string.Join(", ", rm.levels.Where(l => l).Select(l => l.name)));
        }
    }
}
