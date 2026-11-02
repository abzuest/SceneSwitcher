# How R.E.P.O. v0.5.0 changes levels

Source: `Assembly-CSharp.dll` decompiled with ILSpy 8.2 into `../_decompiled/Assembly-CSharp/`
(read-only reference, not committed).

## Key types

| Type | Role |
|---|---|
| `RunManager` (singleton, `DontDestroyOnLoad`) | Owns the level catalog and the run state. All scene changes go through it. |
| `RunManagerPUN` | Photon RPC bridge. `UpdateLevelRPC(name, levelsCompleted, gameOver)` syncs clients. |
| `Level` (ScriptableObject) | One level definition. Identified across the network **by `name`**. |
| `DebugCommandHandler` | Vanilla console commands (`level`, `levelnum`, `arenaraceloop`). |
| `SemiFunc` | Static helpers (`RunIsArena`, `RunIsLobbyMenu`, `IsMasterClientOrSingleplayer`, ...). |

## Level catalog (`RunManager` fields)

- `levelLobby` (the truck), `levelLobbyMenu` (pre-game lobby menu), `levelMainMenu`,
  `levelTutorial`, `levelRecording`, `levelSplashScreen`
- `List<Level> levelShop`
- `List<Level> levelArena` - **a list** in v0.5.0.
- `List<Level> levels` - run maps. REPOLib registers modded levels here, so they are
  resolvable by name on clients too.

Confirmed in game (v0.5.0, vanilla, logged by `LevelCatalogLogger`):

| Field | Level names |
|---|---|
| `levelLobby` / `levelLobbyMenu` | `Level - Lobby` / `Level - Lobby Menu` |
| `levelArena` | `Level - Arena Fight` (disposal arena), `Level - Arena Race` (race cars) |
| `levelShop` | `Level - Shop Forest`, `Level - Shop Harbor`, `Level - Shop Wasteland`, `Level - Shop Suburb`, `Level - Shop Dead Forest` |
| `levels` | `Level - Arctic`, `Level - Manor`, `Level - Museum`, `Level - Wizard`, `Level - Desert` |

The catalog is already populated on the splash screen, before any run starts.
- `int levelsCompleted` - "level number - 1". Drives difficulty:
  `LevelGenerator` module count, `EnemyDirector` enemy gating, `SemiFunc` difficulty curves.

## The entry point: `RunManager.ChangeLevel(completed, failed, ChangeLevelType)`

1. Bails out unless master client / singleplayer (or in a menu level), or if already `restarting`.
2. If `failed` while in an arena -> `ResetProgress()`, `levelCurrent = levelLobbyMenu` (MP),
   `gameOver = true`. If `failed` in a run level -> `SetArenaLevel()` (random arena).
3. Otherwise picks the target from `ChangeLevelType`
   (`LobbyMenu`, `RunLevel` = random map, `Shop`, ...) or the normal run progression.
4. **Override hook:** for any target except main menu / tutorial / lobby menu / splash,
   if `RunManager.debugLevel` is set, `levelCurrent = debugLevel`.
5. Sends `UpdateLevelRPC(levelCurrent.name, levelsCompleted, gameOver)` to others (buffered).
6. Sets save slot (`saveLevel`), calls `RestartScene()` and `SemiFunc.OnSceneSwitch(...)`.

`RestartScene()` waits for every `PlayerAvatar.outroDone`, then the master calls
`PhotonNetwork.LoadLevel("Reload")` with `AutomaticallySyncScene = true`. The Photon room is
kept, so **players stay connected** across every change (including back to the lobby menu).

## How the vanilla console does it (`DebugCommandHandler`, `level` command)

```csharp
RunManager.instance.debugLevel = <Level>;   // any arena, shop, map, or levelLobby
ResetLevelProgress();
RunManager.instance.ChangeLevel(false, false);
RunManager.instance.debugLevel = null;
```

- `level lobby menu` -> `ChangeLevel(false, false, ChangeLevelType.LobbyMenu)` (after saving `loadLevel`).
- `level arena` picks a random arena; `level <name>` matches `levels + levelArena + levelShop`
  by name without the `"Level - "` prefix. So vanilla *can* target arenas by name.
- `levelnum N` sets `levelsCompleted = N - 1` + `SemiFunc.StatSetRunLevel(...)`, then reloads.
- **Why it is not usable normally:** `DebugConsoleUI` only opens when
  `SemiFunc.DebugTester()` (`Debug.isDebugBuild`) or `developerFlags.debug_console`.
  `DebugCommandHandler.Register(ChatCommand)` is public, though; commands with
  `debugOnly: false` are also accepted from chat.

## Implications for SceneSwitcher

| Feature | Approach |
|---|---|
| Return to lobby menu, keep players | `ChangeLevel(false, false, ChangeLevelType.LobbyMenu)`; mirror the `ResetLevelProgress` + `loadLevel` logic. |
| Arena (disposal) / Race Cars | Set `debugLevel` to the matching `levelArena` entry, `ChangeLevel(false, false)`, clear `debugLevel`. |
| Any map incl. modded | Same with an entry of `RunManager.levels`. |
| Level number | Set `levelsCompleted = N - 1` + `StatSetRunLevel` **before** `ChangeLevel` (it is sent in the RPC). |
| Host-only | Guard with `SemiFunc.IsMasterClientOrSingleplayer()`; `ChangeLevel` also self-guards. |

Open questions to verify in game:
- Exact disposal-arena `Level.name` (logged by `LevelCatalogLogger`).
- Arena end with `gameOver` from a "forced" arena: should the run reset or return to the truck?
- Interaction with LateJoinPlus while a forced change is in progress.
