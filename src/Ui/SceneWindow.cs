using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using SceneSwitcher.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SceneSwitcher.Ui
{
    /// <summary>
    /// IMGUI window toggled with a hotkey (Insert by default). Same actions as the "/scene"
    /// chat command; no dependency on other mods.
    /// While open it frees the cursor and blocks player input using the game's own helpers,
    /// refreshed every frame, so everything returns to normal on its own once it closes.
    /// </summary>
    internal sealed class SceneWindow : MonoBehaviour
    {
        private const int MaxLevel = 50;

        // Layout is designed for 1080p and scaled to the actual screen height.
        private const float ReferenceHeight = 1080f;

        // Input blocks expire after this time unless refreshed; slightly longer than a frame.
        private const float BlockTime = 0.1f;

        // Empty "except" list: every game control is disabled.
        private static readonly List<InputKey> NoControls = new List<InputKey>();

        private ConfigEntry<Key> _toggleKey;
        private bool _visible;
        private Rect _windowRect = new Rect(40f, 40f, 340f, 640f);
        private Vector2 _scroll;
        private int _level; // 0 = keep current

        private int? SelectedLevel => _level == 0 ? (int?)null : _level;

        /// <summary>Creates the window on its own object so it survives scene changes.</summary>
        public static void Create(ConfigEntry<Key> toggleKey)
        {
            var go = new GameObject("SceneSwitcher.SceneWindow");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<SceneWindow>()._toggleKey = toggleKey;
        }

        private void Update()
        {
            if (!CrashReporter.Guard("window", UpdateWindow))
            {
                Shutdown();
            }
        }

        /// <summary>After a crash, close and disable the window so the error is not repeated every frame.</summary>
        private void Shutdown()
        {
            Close();
            enabled = false;
        }

        private void UpdateWindow()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            Key toggleKey = _toggleKey.Value;
            if (toggleKey != Key.None && keyboard[toggleKey].wasPressedThisFrame && SemiFunc.NoTextInputsActive())
            {
                Toggle();
            }
            if (!_visible)
            {
                return;
            }
            // Close on Escape, or if switching is no longer possible (host left, scene changed...).
            if (keyboard.escapeKey.wasPressedThisFrame || !SceneSwitchService.CanSwitch(out _))
            {
                Close();
                return;
            }
            BlockGame();
        }

        private void Toggle()
        {
            if (_visible)
            {
                Close();
                return;
            }
            if (!SceneSwitchService.CanSwitch(out string reason))
            {
                Feedback.Fail(reason);
                return;
            }
            _visible = true;
            BlockGame();
        }

        private void Close()
        {
            _visible = false;
            // The game re-locks the cursor by itself when CursorManager's unlock timer runs out.
            Cursor.visible = false;
        }

        private static void BlockGame()
        {
            if (CursorManager.instance)
            {
                CursorManager.instance.Unlock(BlockTime);
            }
            // CursorManager.Unlock hides the OS cursor and shows the game's own; IMGUI is drawn
            // above the game's cursor, so use the OS one instead.
            Cursor.visible = true;
            if (MenuCursor.instance)
            {
                MenuCursor.instance.OverrideHide(BlockTime);
            }
            if (InputManager.instance)
            {
                InputManager.instance.DisableMovement(BlockTime);
                InputManager.instance.DisableAiming(BlockTime);
                InputManager.instance.DisableControlsExcept(BlockTime, NoControls);
            }
        }

        private void OnGUI()
        {
            if (!_visible)
            {
                return;
            }
            float scale = Screen.height / ReferenceHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            _windowRect = GUILayout.Window(GetInstanceID(), _windowRect, DrawWindow, "Scene Switcher (host)");
        }

        // OnGUI runs several times per frame (layout, repaint, input events):
        // only the code inside a pressed button's "if" has side effects.
        private void DrawWindow(int windowId)
        {
            try
            {
                DrawWindowContents();
            }
            catch (Exception exception) when (!(exception is ExitGUIException))
            {
                // ExitGUIException is IMGUI's normal control flow and must propagate.
                CrashReporter.Report(exception, "window GUI");
                Shutdown();
            }
        }

        private void DrawWindowContents()
        {
            GUILayout.Label(_level == 0 ? "Level: keep current" : $"Level: {_level}");
            _level = Mathf.RoundToInt(GUILayout.HorizontalSlider(_level, 0f, MaxLevel));
            GUILayout.Space(6f);

            _scroll = GUILayout.BeginScrollView(_scroll);

            if (GUILayout.Button("Lobby menu"))
            {
                Run(SceneLauncher.ReturnToLobbyMenu);
            }
            if (GUILayout.Button("Truck"))
            {
                Run(() => SceneLauncher.Launch(SceneSwitchService.Truck, SelectedLevel, "the truck"));
            }

            Section("Shops");
            if (GUILayout.Button("Random shop"))
            {
                Run(() => SceneLauncher.LaunchRandom(SceneSwitchService.PickRandomShop(), SelectedLevel, "shops"));
            }
            foreach (Level shop in SceneSwitchService.Shops)
            {
                if (GUILayout.Button(LevelNameResolver.DisplayName(shop.name)))
                {
                    Run(() => SceneLauncher.Launch(shop, SelectedLevel));
                }
            }

            Section("Maps");
            if (GUILayout.Button("Random map"))
            {
                Run(() => SceneLauncher.LaunchRandom(SceneSwitchService.PickRandomMap(), SelectedLevel, "maps"));
            }
            foreach (Level map in SceneSwitchService.Maps)
            {
                if (GUILayout.Button(LevelNameResolver.DisplayName(map.name)))
                {
                    Run(() => SceneLauncher.Launch(map, SelectedLevel));
                }
            }

            Section("Arenas");
            foreach (Level arena in SceneSwitchService.Arenas)
            {
                // Arenas ignore the level number, like the "/scene arena" command.
                if (GUILayout.Button(LevelNameResolver.DisplayName(arena.name)))
                {
                    Run(() => SceneLauncher.Launch(arena, null));
                }
            }

            GUILayout.EndScrollView();

            if (GUILayout.Button("Close"))
            {
                Close();
            }
            GUI.DragWindow();
        }

        private static void Section(string title)
        {
            GUILayout.Space(8f);
            GUILayout.Label(title);
        }

        private void Run(Action action)
        {
            Close();
            CrashReporter.Guard("window button", action);
        }
    }
}
