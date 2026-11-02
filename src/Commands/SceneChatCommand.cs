using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SceneSwitcher.Core;
using UnityEngine;

namespace SceneSwitcher.Commands
{
    /// <summary>
    /// Registers "/scene" in the vanilla command handler. With debugOnly: false the game
    /// runs it from chat: any chat message starting with "/" is tried as a command first
    /// (PlayerAvatar.ChatMessageSend) and is not sent to other players when it matches.
    /// </summary>
    [HarmonyPatch(typeof(DebugCommandHandler), "Start")]
    internal static class SceneChatCommand
    {
        private const string CommandName = "scene";

        [HarmonyPostfix]
        private static void Postfix(DebugCommandHandler __instance)
        {
            // Duplicate handlers are destroyed in Awake; only register on the surviving one.
            if (__instance != DebugCommandHandler.instance)
            {
                return;
            }
            __instance.Register(new DebugCommandHandler.ChatCommand(
                CommandName,
                "Host only. Switch scene: " + SceneCommandParser.Usage,
                Execute,
                Suggest,
                isEnabled: null,
                debugOnly: false));
            Plugin.Log.LogInfo($"Registered chat command /{CommandName}.");
        }

        private static void Execute(bool isDebugConsole, string[] args)
        {
            SceneRequest request = SceneCommandParser.Parse(args);
            if (request.Error != null)
            {
                Feedback.Fail(request.Error + " Usage: " + SceneCommandParser.Usage);
                return;
            }

            switch (request.Action)
            {
                case SceneAction.Help:
                    Feedback.Info(SceneCommandParser.Usage);
                    return;
                case SceneAction.List:
                    ListLevels();
                    return;
            }

            // Checked before resolving names, so non-hosts get "Only the host..." instead of a lookup error.
            if (!SceneSwitchService.CanSwitch(out string reason))
            {
                Feedback.Fail(reason);
                return;
            }

            switch (request.Action)
            {
                case SceneAction.LobbyMenu:
                    SceneLauncher.ReturnToLobbyMenu();
                    break;
                case SceneAction.Truck:
                    SceneLauncher.Launch(SceneSwitchService.Truck, request.LevelNumber, "the truck");
                    break;
                case SceneAction.Arena:
                    LaunchResolved(request, SceneSwitchService.Arenas.ToList(), "arena");
                    break;
                case SceneAction.Map:
                    LaunchResolved(request, SceneSwitchService.Maps.ToList(), "map");
                    break;
                case SceneAction.RandomMap:
                    SceneLauncher.LaunchRandom(SceneSwitchService.PickRandomMap(), request.LevelNumber, "maps");
                    break;
                case SceneAction.Shop:
                    LaunchResolved(request, SceneSwitchService.Shops.ToList(), "shop");
                    break;
                case SceneAction.RandomShop:
                    SceneLauncher.LaunchRandom(SceneSwitchService.PickRandomShop(), request.LevelNumber, "shops");
                    break;
            }
        }

        private static void LaunchResolved(SceneRequest request, List<Level> candidates, string kind)
        {
            ResolveResult result = SceneCommandParser.ResolveTarget(request, candidates.Select(l => l.name), out int? levelNumber);
            switch (result.Status)
            {
                case ResolveStatus.NotFound:
                    Feedback.Fail($"No {kind} matches '{request.Query}'. Try /scene list.");
                    return;
                case ResolveStatus.Ambiguous:
                    Feedback.Fail($"'{request.Query}' matches: " + string.Join(", ", result.Candidates.Select(LevelNameResolver.DisplayName)));
                    return;
            }
            SceneLauncher.Launch(candidates.First(l => l.name == result.Match), levelNumber);
        }

        private static void ListLevels()
        {
            if (!RunManager.instance)
            {
                Feedback.Fail("Game is not ready.");
                return;
            }
            string maps = string.Join(", ", SceneSwitchService.Maps.Select(l => LevelNameResolver.DisplayName(l.name)));
            string arenas = string.Join(", ", SceneSwitchService.Arenas.Select(l => LevelNameResolver.DisplayName(l.name)));
            string shops = string.Join(", ", SceneSwitchService.Shops.Select(l => ShopZone(l.name)));
            Feedback.Info($"Maps: {maps}\nShops: {shops}\nArenas: {arenas}", 8f);
        }

        /// <summary>"Level - Shop Dead Forest" -> "dead forest", the form typed after "/scene shop".</summary>
        internal static string ShopZone(string levelName)
        {
            string normalized = LevelNameResolver.Normalize(levelName);
            return normalized.StartsWith("shop ") ? normalized.Substring("shop ".Length) : normalized;
        }

        // Only used by the debug console UI; chat has no autocomplete.
        private static List<string> Suggest(bool isDebugConsole, string partial, string[] args)
        {
            IEnumerable<string> options;
            if (args.Length <= 1)
            {
                options = new[] { "lobby", "truck", "shop", "map", "arena", "list", "help" };
            }
            else if (args[0].ToLowerInvariant() == "shop" && RunManager.instance)
            {
                options = SceneSwitchService.Shops.Select(l => ShopZone(l.name)).Prepend("random");
            }
            else if (args[0].ToLowerInvariant() == "arena")
            {
                options = new[] { "fight", "race" };
            }
            else if (args[0].ToLowerInvariant() == "map" && RunManager.instance)
            {
                options = SceneSwitchService.Maps.Select(l => LevelNameResolver.Normalize(l.name)).Prepend("random");
            }
            else
            {
                options = Enumerable.Empty<string>();
            }
            return options.Where(o => o.StartsWith(partial.ToLowerInvariant())).ToList();
        }
    }
}
