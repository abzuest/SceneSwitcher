using SceneSwitcher.Core;
using Xunit;

namespace SceneSwitcher.Tests
{
    public class LevelNameResolverTests
    {
        private static readonly string[] Maps =
        {
            "Level - Arctic", "Level - Manor", "Level - Museum", "Level - Wizard", "Level - Desert"
        };

        [Theory]
        [InlineData("arctic", "Level - Arctic")]
        [InlineData("ARCTIC", "Level - Arctic")]
        [InlineData("Level - Arctic", "Level - Arctic")]
        [InlineData("  wizard  ", "Level - Wizard")]
        [InlineData("des", "Level - Desert")]
        [InlineData("zar", "Level - Wizard")]
        public void Resolves_unique_match(string query, string expected)
        {
            ResolveResult result = LevelNameResolver.Resolve(query, Maps);

            Assert.Equal(ResolveStatus.Found, result.Status);
            Assert.Equal(expected, result.Match);
        }

        [Fact]
        public void Reports_ambiguous_prefix()
        {
            ResolveResult result = LevelNameResolver.Resolve("m", Maps);

            Assert.Equal(ResolveStatus.Ambiguous, result.Status);
            Assert.Equal(new[] { "Level - Manor", "Level - Museum" }, result.Candidates);
        }

        [Fact]
        public void Exact_match_wins_over_prefix()
        {
            ResolveResult result = LevelNameResolver.Resolve("facility", new[] { "Level - Facility", "Level - Facility 2" });

            Assert.Equal(ResolveStatus.Found, result.Status);
            Assert.Equal("Level - Facility", result.Match);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("volcano")]
        public void Reports_not_found(string query)
        {
            Assert.Equal(ResolveStatus.NotFound, LevelNameResolver.Resolve(query, Maps).Status);
        }

        [Fact]
        public void Matches_modded_names_without_prefix()
        {
            ResolveResult result = LevelNameResolver.Resolve("backrooms", new[] { "Level - Arctic", "Backrooms Level" });

            Assert.Equal("Backrooms Level", result.Match);
        }

        [Fact]
        public void DisplayName_strips_prefix()
        {
            Assert.Equal("Arena Fight", LevelNameResolver.DisplayName("Level - Arena Fight"));
            Assert.Equal("Custom", LevelNameResolver.DisplayName("Custom"));
        }
    }
}
