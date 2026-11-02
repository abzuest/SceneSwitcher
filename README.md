# SceneSwitcher

Host-only R.E.P.O. (v0.5.0) mod to switch scenes on demand. BepInEx 5 + HarmonyX.

No other mods required.

## Window (Insert key)

The host can press **Insert** to open the Scene Switcher window: a level slider (0 = keep current)
and one button per destination: lobby menu, truck, every shop, every map (including modded ones)
and both arenas. The level applies to the truck, shops and maps. Escape or Insert closes it.
While it is open the cursor is free and player controls are blocked. Other players cannot open it.

## Configuration

`BepInEx/config/abzu.SceneSwitcher.cfg`:

| Section | Key | Default | Description |
|---|---|---|---|
| `Window` | `ToggleKey` | `Insert` | Key that opens/closes the window. Any [Input System key name](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/api/UnityEngine.InputSystem.Key.html); `None` disables the window. |
| `Saves` | `ProgressOnSceneChange` | `RevertToLastSave` | In saved (private) lobbies: `RevertToLastSave` goes back to the last save before switching (like the vanilla `level` debug command); `KeepCurrent` keeps money, upgrades and level as they are. Going from a map to a shop always keeps progress. |

All settings are read when used. With [REPOConfig](https://thunderstore.io/c/repo/p/REPOModding/REPOConfig/)
installed, SceneSwitcher appears in its "Mods" list and changes made in game apply immediately.

## Chat command

Type the command in the in-game chat (host only; other players get "Only the host can change scenes."):

| Command | Effect |
|---|---|
| `/scene lobby` | Return to the lobby menu (not the truck), keeping all players connected |
| `/scene truck [level]` | Go to the truck |
| `/scene shop [zone] [level]` | Go to a shop: `forest`, `harbor`, `wasteland`, `suburb`, `dead forest` (no zone or `random`: random shop) |
| `/scene map <name> [level]` | Launch a map, optionally at a level number (e.g. `/scene map arctic 5`) |
| `/scene map random [level]` | Launch a random map |
| `/scene arena fight` | Launch the disposal arena (alias: `disposal`) |
| `/scene arena race` | Launch the race cars arena (aliases: `cars`, `race cars`) |
| `/scene list` | Show available maps (including modded ones), shops and arenas |
| `/scene` or `/scene help` | Show usage |

Map names are case-insensitive and can be shortened when unique (`/scene map wiz`).
If a map name itself ends in a number (e.g. a modded "Facility 2"), the exact name wins
over reading the number as a level.

Notes:
- What happens to the run's progress is set by `ProgressOnSceneChange` (see Configuration).
- Arenas are the Game Over screen and follow vanilla rules: in saved (private) lobbies,
  **entering an arena deletes the save file**, even if you leave it right away. When an arena
  ends, the game returns to the lobby menu.

## Build

Requirements: .NET SDK 6+.

```
dotnet build -c Release
```

Output: `bin/Release/netstandard2.1/SceneSwitcher.dll`, also staged as
`dist/SceneSwitcher/SceneSwitcher.dll` after every build.
The game path defaults to `E:\Program Files (x86)\Steam\steamapps\common\REPO`;
override it with `dotnet build -p:GameDir="<path>"`. The build only reads from the game folder
(`Assembly-CSharp` is publicized into a copy under `obj/`).

## Tests

```
dotnet test tests/SceneSwitcher.Tests
```

Unit tests cover command parsing and level name matching (no game needed).

## Install (manual)

Copy the `dist/SceneSwitcher/` folder into `BepInEx/plugins/` of the game or mod profile.
Do not copy the whole project folder: BepInEx would find two DLLs (`bin/` and `obj/`).

## Docs

- [docs/LEVEL_FLOW.md](docs/LEVEL_FLOW.md) - how the game changes levels (decompilation notes)

## License

[MIT](LICENSE)
