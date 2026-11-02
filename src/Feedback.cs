using UnityEngine;

namespace SceneSwitcher
{
    /// <summary>Local-only feedback: log + menu sound + on-screen focus text.</summary>
    internal static class Feedback
    {
        public static void Ok(string message) => Show(message, MenuManager.MenuClickEffectType.Confirm, Color.green);

        public static void Info(string message, float time = 3f) => Show(message, MenuManager.MenuClickEffectType.Confirm, Color.white, time);

        public static void Fail(string message) => Show(message, MenuManager.MenuClickEffectType.Deny, Color.red);

        private static void Show(string message, MenuManager.MenuClickEffectType effect, Color flash, float time = 3f)
        {
            Plugin.Log.LogInfo("/scene: " + message);
            if (MenuManager.instance)
            {
                MenuManager.instance.MenuEffectClick(effect, null, 1f, 1f, soundOnly: true);
            }
            if (MissionUI.instance)
            {
                SemiFunc.UIFocusText(message, Color.white, flash, time);
            }
        }
    }
}
