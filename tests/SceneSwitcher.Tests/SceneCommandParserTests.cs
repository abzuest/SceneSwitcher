using SceneSwitcher.Core;
using Xunit;

namespace SceneSwitcher.Tests
{
    public class SceneCommandParserTests
    {
        private static readonly string[] Arenas = { "Level - Arena Fight", "Level - Arena Race" };

        private static SceneRequest Parse(string input) =>
            SceneCommandParser.Parse(input.Split(' ', System.StringSplitOptions.RemoveEmptyEntries));

        [Theory]
        [InlineData("", SceneAction.Help)]
        [InlineData("help", SceneAction.Help)]
        [InlineData("list", SceneAction.List)]
        [InlineData("lobby", SceneAction.LobbyMenu)]
        [InlineData("LOBBY", SceneAction.LobbyMenu)]
        [InlineData("menu", SceneAction.LobbyMenu)]
        public void Parses_simple_actions(string input, SceneAction expected)
        {
            SceneRequest request = Parse(input);

            Assert.Null(request.Error);
            Assert.Equal(expected, request.Action);
        }

        [Fact]
        public void Unknown_option_is_an_error()
        {
            Assert.NotNull(Parse("teleport").Error);
        }

        [Theory]
        [InlineData("arena fight", "Level - Arena Fight")]
        [InlineData("arena disposal", "Level - Arena Fight")]
        [InlineData("arena race", "Level - Arena Race")]
        [InlineData("arena race cars", "Level - Arena Race")]
        [InlineData("arena cars", "Level - Arena Race")]
        [InlineData("arena arena fight", "Level - Arena Fight")]
        public void Resolves_arena_aliases(string input, string expected)
        {
            SceneRequest request = Parse(input);
            ResolveResult result = SceneCommandParser.ResolveTarget(request, Arenas, out _);

            Assert.Equal(SceneAction.Arena, request.Action);
            Assert.Equal(expected, result.Match);
        }

        [Fact]
        public void Arena_without_name_is_an_error()
        {
            Assert.NotNull(Parse("arena").Error);
        }

        [Fact]
        public void Map_with_level_number()
        {
            SceneRequest request = Parse("map arctic 5");

            Assert.Null(request.Error);
            Assert.Equal(SceneAction.Map, request.Action);
            Assert.Equal("arctic", request.Query);
            Assert.Equal(5, request.LevelNumber);
        }

        [Fact]
        public void Map_without_level_number()
        {
            SceneRequest request = Parse("map shop dead forest");

            Assert.Equal("shop dead forest", request.Query);
            Assert.Null(request.LevelNumber);
        }

        [Theory]
        [InlineData("map arctic 0")]
        [InlineData("map arctic -3")]
        public void Level_number_below_one_is_an_error(string input)
        {
            Assert.NotNull(Parse(input).Error);
        }

        [Fact]
        public void Map_without_name_is_an_error()
        {
            Assert.NotNull(Parse("map").Error);
        }

        [Fact]
        public void Single_number_is_a_name_not_a_level()
        {
            SceneRequest request = Parse("map 2");

            Assert.Equal("2", request.Query);
            Assert.Null(request.LevelNumber);
        }

        [Theory]
        [InlineData("map random", null)]
        [InlineData("map RANDOM 7", 7)]
        public void Random_map(string input, int? expectedLevel)
        {
            SceneRequest request = Parse(input);

            Assert.Equal(SceneAction.RandomMap, request.Action);
            Assert.Equal(expectedLevel, request.LevelNumber);
        }

        [Theory]
        [InlineData("truck", null)]
        [InlineData("TRUCK 4", 4)]
        public void Truck_with_optional_level(string input, int? expectedLevel)
        {
            SceneRequest request = Parse(input);

            Assert.Null(request.Error);
            Assert.Equal(SceneAction.Truck, request.Action);
            Assert.Equal(expectedLevel, request.LevelNumber);
        }

        [Theory]
        [InlineData("truck forest")]
        [InlineData("truck 1 2")]
        [InlineData("truck 0")]
        public void Truck_invalid_arguments_are_errors(string input)
        {
            Assert.NotNull(Parse(input).Error);
        }

        private static readonly string[] Shops =
        {
            "Level - Shop Forest", "Level - Shop Harbor", "Level - Shop Wasteland", "Level - Shop Suburb", "Level - Shop Dead Forest"
        };

        [Theory]
        [InlineData("shop", null)]
        [InlineData("shop 3", 3)]
        [InlineData("shop random", null)]
        [InlineData("shop random 6", 6)]
        public void Random_shop(string input, int? expectedLevel)
        {
            SceneRequest request = Parse(input);

            Assert.Null(request.Error);
            Assert.Equal(SceneAction.RandomShop, request.Action);
            Assert.Equal(expectedLevel, request.LevelNumber);
        }

        [Theory]
        [InlineData("shop forest", "Level - Shop Forest", null)]
        [InlineData("shop dead forest", "Level - Shop Dead Forest", null)]
        [InlineData("shop dead", "Level - Shop Dead Forest", null)]
        [InlineData("shop harb 2", "Level - Shop Harbor", 2)]
        [InlineData("shop Shop Suburb", "Level - Shop Suburb", null)]
        [InlineData("shop Level - Shop Wasteland 5", "Level - Shop Wasteland", 5)]
        public void Resolves_shop_zone(string input, string expected, int? expectedLevel)
        {
            SceneRequest request = Parse(input);
            ResolveResult result = SceneCommandParser.ResolveTarget(request, Shops, out int? level);

            Assert.Equal(SceneAction.Shop, request.Action);
            Assert.Equal(ResolveStatus.Found, result.Status);
            Assert.Equal(expected, result.Match);
            Assert.Equal(expectedLevel, level);
        }

        [Fact]
        public void Unknown_shop_zone_is_not_found()
        {
            SceneRequest request = Parse("shop volcano");

            Assert.Equal(ResolveStatus.NotFound, SceneCommandParser.ResolveTarget(request, Shops, out _).Status);
        }

        [Fact]
        public void Shop_level_below_one_is_an_error()
        {
            Assert.NotNull(Parse("shop forest 0").Error);
            Assert.NotNull(Parse("shop 0").Error);
        }

        [Fact]
        public void Trailing_number_falls_back_to_full_name()
        {
            SceneRequest request = Parse("map facility 2");
            ResolveResult result = SceneCommandParser.ResolveTarget(request, new[] { "Level - Facility 2", "Level - Arctic" }, out int? level);

            Assert.Equal("Level - Facility 2", result.Match);
            Assert.Null(level);
        }

        [Fact]
        public void Trailing_number_is_level_when_full_input_is_not_a_name()
        {
            SceneRequest request = Parse("map facility 3");
            ResolveResult result = SceneCommandParser.ResolveTarget(request, new[] { "Level - Facility", "Level - Facility 2" }, out int? level);

            Assert.Equal("Level - Facility", result.Match);
            Assert.Equal(3, level);
        }

        [Fact]
        public void Exact_full_name_wins_over_level_number()
        {
            // Both readings are valid ("Facility" at level 2, or "Facility 2"): the exact name wins.
            SceneRequest request = Parse("map facility 2");
            ResolveResult result = SceneCommandParser.ResolveTarget(request, new[] { "Level - Facility", "Level - Facility 2" }, out int? level);

            Assert.Equal("Level - Facility 2", result.Match);
            Assert.Null(level);
        }
    }
}
