using System;
using System.Collections.Generic;
using System.Linq;

namespace SceneSwitcher.Core
{
    public enum ResolveStatus
    {
        Found,
        NotFound,
        Ambiguous
    }

    public sealed class ResolveResult
    {
        public ResolveStatus Status { get; }

        /// <summary>The matched internal level name (e.g. "Level - Arctic") when Found.</summary>
        public string Match { get; }

        /// <summary>The competing level names when Ambiguous.</summary>
        public IReadOnlyList<string> Candidates { get; }

        private ResolveResult(ResolveStatus status, string match, IReadOnlyList<string> candidates)
        {
            Status = status;
            Match = match;
            Candidates = candidates;
        }

        public static ResolveResult Found(string match) => new ResolveResult(ResolveStatus.Found, match, Array.Empty<string>());

        public static ResolveResult NotFound() => new ResolveResult(ResolveStatus.NotFound, null, Array.Empty<string>());

        public static ResolveResult Ambiguous(IReadOnlyList<string> candidates) => new ResolveResult(ResolveStatus.Ambiguous, null, candidates);
    }

    /// <summary>
    /// Matches user input against level names. Pure string logic, no Unity types, so it is unit-testable.
    /// Matching order: exact, then unique prefix, then unique substring. Case-insensitive,
    /// the "Level - " prefix is optional.
    /// </summary>
    public static class LevelNameResolver
    {
        public const string LevelPrefix = "Level - ";

        public static string DisplayName(string levelName)
        {
            if (string.IsNullOrEmpty(levelName))
            {
                return string.Empty;
            }
            return levelName.StartsWith(LevelPrefix, StringComparison.OrdinalIgnoreCase)
                ? levelName.Substring(LevelPrefix.Length)
                : levelName;
        }

        public static string Normalize(string name)
        {
            string[] words = DisplayName(name?.Trim()).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", words).ToLowerInvariant();
        }

        public static ResolveResult Resolve(string query, IEnumerable<string> levelNames)
        {
            string normalizedQuery = Normalize(query);
            if (normalizedQuery.Length == 0)
            {
                return ResolveResult.NotFound();
            }

            List<string> names = levelNames.Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();

            List<string> exact = names.Where(n => Normalize(n) == normalizedQuery).ToList();
            if (exact.Count > 0)
            {
                return exact.Count == 1 ? ResolveResult.Found(exact[0]) : ResolveResult.Ambiguous(exact);
            }

            List<string> prefix = names.Where(n => Normalize(n).StartsWith(normalizedQuery, StringComparison.Ordinal)).ToList();
            if (prefix.Count > 0)
            {
                return prefix.Count == 1 ? ResolveResult.Found(prefix[0]) : ResolveResult.Ambiguous(prefix);
            }

            List<string> contains = names.Where(n => Normalize(n).Contains(normalizedQuery)).ToList();
            if (contains.Count > 0)
            {
                return contains.Count == 1 ? ResolveResult.Found(contains[0]) : ResolveResult.Ambiguous(contains);
            }

            return ResolveResult.NotFound();
        }
    }
}
