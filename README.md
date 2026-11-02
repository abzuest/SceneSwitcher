# SceneSwitcher

Host-only R.E.P.O. (v0.5.0) mod to switch scenes on demand. BepInEx 5 + HarmonyX.
No other mods required.

## Usage

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
- Like the vanilla `level` debug command, in saved (private) lobbies a scene change reverts
  run progress to the last save before switching. Exception: going from a map to a shop
  keeps the run's progress, as the vanilla `level shop` command does.
- Arenas follow their normal rules: when an arena ends, the game returns to the lobby menu.

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
In-game checks are listed in [docs/TEST_PLAN.md](docs/TEST_PLAN.md).

## Install (manual)

Copy the `dist/SceneSwitcher/` folder into `BepInEx/plugins/` of the game or mod profile.
Do not copy the whole project folder: BepInEx would find two DLLs (`bin/` and `obj/`).

## Docs

- [docs/LEVEL_FLOW.md](docs/LEVEL_FLOW.md) - how the game changes levels (decompilation notes)
- [docs/TEST_PLAN.md](docs/TEST_PLAN.md) - manual in-game test cases

## License

[MIT](LICENSE)
