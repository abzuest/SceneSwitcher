using System;
using System.Collections.Generic;
using System.Linq;

namespace SceneSwitcher.Core
{
    public enum SceneAction
    {
        Help,
        List,
        LobbyMenu,
        Truck,
        Arena,
        Map,
        RandomMap,
        Shop,
        RandomShop,
        Crimson
    }

    public sealed class SceneRequest
    {
        public SceneAction Action { get; set; }

        /// <summary>Level query for Arena/Map/Shop, without the trailing level number.</summary>
        public string Query { get; set; }

        /// <summary>Full level query including a trailing number, used when a map name itself ends in a number.</summary>
        public string RawQuery { get; set; }

        /// <summary>1-based level number, if given.</summary>
        public int? LevelNumber { get; set; }

        /// <summary>Set when the input could not be parsed; the request must not be executed.</summary>
        public string Error { get; set; }
    }

    /// <summary>
    /// Parses the arguments of the "/scene" chat command. Pure logic, no Unity types.
    /// </summary>
    public static class SceneCommandParser
    {
        public const string Usage = "/scene lobby | truck [level] | shop [zone|random] [level] | map <name|random> [level] | arena <fight|race> | list";

        private const string ShopPrefix = "Shop ";

        private static readonly Dictionary<string, string> ArenaAliases = new Dictionary<string, string>
        {
            { "fight", "Arena Fight" },
            { "disposal", "Arena Fight" },
            { "race", "Arena Race" },
            { "cars", "Arena Race" },
            { "race cars", "Arena Race" },
            { "racecars", "Arena Race" }
        };

        public static SceneRequest Parse(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return new SceneRequest { Action = SceneAction.Help };
            }

            string sub = args[0].ToLowerInvariant();
            string[] rest = args.Skip(1).ToArray();

            switch (sub)
            {
                case "help":
                    return new SceneRequest { Action = SceneAction.Help };
                case "crimson":
                    // Easter egg, intentionally left out of Usage.
                    return new SceneRequest { Action = SceneAction.Crimson };
                case "list":
                    return new SceneRequest { Action = SceneAction.List };
                case "lobby":
                case "menu":
                    return new SceneRequest { Action = SceneAction.LobbyMenu };
                case "truck":
                    return ParseTruck(rest);
                case "arena":
                    return ParseArena(rest);
                case "map":
                    return ParseMap(rest);
                case "shop":
                    return ParseShop(rest);
                default:
                    return new SceneRequest { Action = SceneAction.Help, Error = $"Unknown option '{args[0]}'." };
            }
        }

        private static SceneRequest ParseArena(string[] rest)
        {
            if (rest.Length == 0)
            {
                return new SceneRequest { Action = SceneAction.Arena, Error = "Missing arena: fight or race." };
            }
            string query = string.Join(" ", rest);
            if (ArenaAliases.TryGetValue(query.ToLowerInvariant(), out string alias))
            {
                query = alias;
            }
            return new SceneRequest { Action = SceneAction.Arena, Query = query, RawQuery = query };
        }

        private static SceneRequest ParseTruck(string[] rest)
        {
            var request = new SceneRequest { Action = SceneAction.Truck };
            if (rest.Length > 1 || (rest.Length == 1 && !int.TryParse(rest[0], out _)))
            {
                request.Error = "Usage: /scene truck [level].";
                return request;
            }
            if (rest.Length == 1)
            {
                SetLevelNumber(request, int.Parse(rest[0]));
            }
            return request;
        }

        private static SceneRequest ParseMap(string[] rest)
        {
            if (rest.Length == 0)
            {
                return new SceneRequest { Action = SceneAction.Map, Error = "Missing map name." };
            }
            return ParseNamedTarget(rest, SceneAction.Map, SceneAction.RandomMap);
        }

        private static SceneRequest ParseShop(string[] rest)
        {
            // "/scene shop" and "/scene shop 3": random zone. Shop names never are plain numbers.
            if (rest.Length == 0)
            {
                return new SceneRequest { Action = SceneAction.RandomShop };
            }
            if (rest.Length == 1 && int.TryParse(rest[0], out int number))
            {
                var randomShop = new SceneRequest { Action = SceneAction.RandomShop };
                SetLevelNumber(randomShop, number);
                return randomShop;
            }

            SceneRequest request = ParseNamedTarget(rest, SceneAction.Shop, SceneAction.RandomShop);
            if (request.Error == null && request.Action == SceneAction.Shop)
            {
                // Zone names ("forest", "dead forest") are matched as "Shop <zone>", so "forest"
                // is an exact match and not ambiguous with "Shop Dead Forest".
                request.Query = WithShopPrefix(request.Query);
                request.RawQuery = WithShopPrefix(request.RawQuery);
            }
            return request;
        }

        /// <summary>Parses "<name words...> [level]" where the name may also be "random".</summary>
        private static SceneRequest ParseNamedTarget(string[] rest, SceneAction action, SceneAction randomAction)
        {
            var request = new SceneRequest { Action = action, RawQuery = string.Join(" ", rest) };
            string[] nameTokens = rest;

            if (rest.Length > 1 && int.TryParse(rest[rest.Length - 1], out int number))
            {
                if (!SetLevelNumber(request, number))
                {
                    return request;
                }
                nameTokens = rest.Take(rest.Length - 1).ToArray();
            }

            request.Query = string.Join(" ", nameTokens);
            if (request.Query.Equals("random", StringComparison.OrdinalIgnoreCase))
            {
                request.Action = randomAction;
            }
            return request;
        }

        private static bool SetLevelNumber(SceneRequest request, int number)
        {
            if (number < 1)
            {
                request.Error = "Level number must be 1 or higher.";
                return false;
            }
            request.LevelNumber = number;
            return true;
        }

        private static string WithShopPrefix(string query)
        {
            string displayName = LevelNameResolver.DisplayName(query.Trim());
            return displayName.StartsWith(ShopPrefix, StringComparison.OrdinalIgnoreCase) ? displayName : ShopPrefix + displayName;
        }

        /// <summary>
        /// Resolves an Arena/Map request against level names. A trailing number is normally the
        /// level number, unless the full input is exactly a level name (e.g. "Facility 2") or the
        /// name without the number matches nothing.
        /// </summary>
        public static ResolveResult ResolveTarget(SceneRequest request, IEnumerable<string> levelNames, out int? levelNumber)
        {
            List<string> names = levelNames.ToList();
            levelNumber = request.LevelNumber;

            if (request.LevelNumber.HasValue)
            {
                string fullQuery = LevelNameResolver.Normalize(request.RawQuery);
                List<string> exactFull = names.Where(n => LevelNameResolver.Normalize(n) == fullQuery).ToList();
                if (exactFull.Count == 1)
                {
                    levelNumber = null;
                    return ResolveResult.Found(exactFull[0]);
                }
            }

            ResolveResult result = LevelNameResolver.Resolve(request.Query, names);
            if (result.Status == ResolveStatus.NotFound && request.LevelNumber.HasValue)
            {
                ResolveResult fullName = LevelNameResolver.Resolve(request.RawQuery, names);
                if (fullName.Status != ResolveStatus.NotFound)
                {
                    levelNumber = null;
                    return fullName;
                }
            }
            return result;
        }
    }
}
