# SceneSwitcher

Host-only scene switching for R.E.P.O. (v0.5.0). Jump straight to where you want to be, without
playing through a run:

- **Lobby menu** (not the truck), keeping every player connected
- **Truck**
- **Any shop**, by zone: Forest, Harbor, Wasteland, Suburb, Dead Forest
- **Any map**, at the level number you choose (difficulty, rooms and enemies scale with it)
- **Game Over arenas**: Arena Fight (disposal) and Arena Race (race cars)

No other mods required.

## Window (Insert)

Press **Insert** as the host to open the Scene Switcher window: pick a level number (or keep the
current one) and click a destination. Escape or Insert closes it. While it is open the cursor is
free and your character does not move.

## Chat command

Type in the in-game chat (host only):

| Command | Effect |
|---|---|
| `/scene lobby` | Lobby menu |
| `/scene truck [level]` | Truck |
| `/scene shop [zone] [level]` | Shop by zone (`forest`, `harbor`, `wasteland`, `suburb`, `dead forest`); no zone or `random` = random shop |
| `/scene map <name> [level]` | Map, e.g. `/scene map arctic 5` (names can be shortened: `/scene map wiz`) |
| `/scene map random [level]` | Random map |
| `/scene arena fight` / `/scene arena race` | Game Over arenas |
| `/scene list` | Available maps, shops and arenas |
| `/scene help` | Usage |

Commands are not posted to the chat. Other players get "Only the host can change scenes."

## Configuration

`BepInEx/config/abzu.SceneSwitcher.cfg` (or in game with REPOConfig, changes apply at once):

| Setting | Default | Description |
|---|---|---|
| `Window.ToggleKey` | `Insert` | Key that opens the window. `None` disables it. |
| `Saves.ProgressOnSceneChange` | `RevertToLastSave` | In saved (private) lobbies: `RevertToLastSave` goes back to the last save before switching; `KeepCurrent` keeps money, upgrades and level. Map to shop always keeps progress. |

## Good to know

- **Arenas are the Game Over screen.** In saved (private) lobbies, entering an arena deletes the
  save file, exactly like in vanilla, even if you leave it right away.
- Works with vanilla maps. Tested alongside LateJoinPlus, MenuLib and REPOConfig.

## Source

[github.com/abzuest/SceneSwitcher](https://github.com/abzuest/SceneSwitcher) (MIT license)
