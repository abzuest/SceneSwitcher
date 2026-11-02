using SceneSwitcher.Core;

namespace SceneSwitcher
{
    /// <summary>
    /// User-facing entry points shared by the chat command and the menu:
    /// permission check + feedback + the actual switch.
    /// </summary>
    internal static class SceneLauncher
    {
        public static void ReturnToLobbyMenu()
        {
            if (!CheckCanSwitch())
            {
                return;
            }
            Feedback.Ok("Returning to the lobby menu...");
            SceneSwitchService.ReturnToLobbyMenu();
        }

        /// <param name="targetName">Display name for the feedback; defaults to the level name.</param>
        public static void Launch(Level level, int? levelNumber, string targetName = null)
        {
            if (!CheckCanSwitch())
            {
                return;
            }
            if (!level)
            {
                Feedback.Fail("Level not available.");
                return;
            }
            string target = targetName ?? LevelNameResolver.DisplayName(level.name);
            string suffix = levelNumber.HasValue ? $" (level {levelNumber.Value})" : string.Empty;
            Feedback.Ok($"Launching {target}{suffix}...");
            SceneSwitchService.LaunchLevel(level, levelNumber);
        }

        /// <param name="kind">Plural noun for the "none available" message, e.g. "maps".</param>
        public static void LaunchRandom(Level level, int? levelNumber, string kind)
        {
            if (!level)
            {
                Feedback.Fail($"No {kind} available.");
                return;
            }
            Launch(level, levelNumber);
        }

        private static bool CheckCanSwitch()
        {
            if (SceneSwitchService.CanSwitch(out string reason))
            {
                return true;
            }
            Feedback.Fail(reason);
            return false;
        }
    }
}
