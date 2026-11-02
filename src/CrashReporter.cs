using System;

namespace SceneSwitcher
{
    /// <summary>
    /// Catches unexpected exceptions at SceneSwitcher's entry points and reports them
    /// with a single, recognizable error code (log + on-screen when possible).
    /// </summary>
    internal static class CrashReporter
    {
        public const string ErrorCode = "CRM001 - Crimson's hamster stopped running";

        public static void Report(Exception exception, string context)
        {
            Plugin.Log?.LogError($"{ErrorCode} [{context}]\n{exception}");
            try
            {
                Feedback.Fail(ErrorCode);
            }
            catch
            {
                // The UI may not exist yet (e.g. during startup); the log entry is enough.
            }
        }

        /// <summary>Runs the action, reporting any exception instead of letting it propagate.</summary>
        /// <returns>True if the action completed without throwing.</returns>
        public static bool Guard(string context, Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception exception)
            {
                Report(exception, context);
                return false;
            }
        }
    }
}
