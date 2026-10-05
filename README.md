# Heroes Unwelcomed

You own the dungeon. Heroes keep raiding it, and every time they die they come back a little smarter.

A 2D reverse-dungeon auto-battler built in C# with MonoGame. Desktop and Steam Deck first (controller supported throughout), with iOS/Android later.

## Getting started

**Requirements:** Visual Studio Community 2026 with the ".NET desktop development" workload, the .NET 10 SDK, and the "MonoGame Framework C# project templates" extension.

1. Open `HeroesUnwelcomed.slnx` in Visual Studio.
2. Right-click **HeroesUnwelcomed.Desktop** → **Set as Startup Project** (only needed once).
3. Pick a launch profile from the dropdown next to the green ▶ button, then press **F5**.

| Launch profile | What it opens |
|---|---|
| Play | The game: 3 goblin rooms and the Lich Lord; heroes enter automatically |
| Play: build mode | Door already locked, 300 souls: try building, upgrading, flipping |
| Play: crowded | 12 heroes in parties of 3, a portal and a left-facing row: parties queue |
| Animation Gallery | Every placeholder entity animating to the music; play clips, change speed, pause |
| Sandbox | Input playground: one selection driven by keyboard, gamepad and mouse |
| UI Layout (1080p) | UI scaling check at 1920x1080 |
| UI Layout (Steam Deck 1280x800) | The same check at the Deck's resolution |
| Game (no scenario) | The game's normal start |

## Controls (so far)

| Action | Keyboard / mouse | Gamepad |
|---|---|---|
| Move selection | Arrow keys / hover | D-pad / left stick |
| Confirm / open menu | Enter, Space / click | A |
| Cancel / close menu | Esc, Backspace | B |
| Lock / open the dungeon door | L | Y |
| Send the next party | N | X |
| Pause | P | Start |
| Speed down / up | `[` `]` (or `-` `+`) | LB / RB |
| Pan camera | W A S D / right-drag | Right stick |
| Zoom | Mouse wheel | Triggers |
| Fullscreen | F11 or Alt+Enter | |
| Screenshot (debug) | F12 | |
| Dev tools panel (debug) | F1 or `~` | Back / View |

## Developer tools (Debug builds only)

- **Dev Tools panel (F1):** FPS, scenario switcher, run inspector (heroes' skills and states, parties, send party, door, new run), audio & beat readout (speed buttons, metronome, volume, per-stem mutes, a "fear cut" test, visual delay), window-size presets (including Steam Deck), fullscreen, screenshots, live input readout.
- **Live data reload:** in Debug builds the game reads JSON straight from the repo's `data/` folder. Save `data/animations/placeholders.json` while the Animation Gallery runs and the changes appear instantly (errors are shown on screen and the old data is kept).
- **Scenarios:** JSON files in `data/scenarios/`. Each one launches straight into a prepared situation. Add a file and it shows up in the panel.
- **Command-line options** (set in `src/HeroesUnwelcomed.Desktop/Properties/launchSettings.json`):

```
--scenario <name>          start in data/scenarios/<name>.json
--window 1280x800          window size
--fullscreen               borderless fullscreen
--screenshot-frames 30,90  save PNGs after these frames
--screenshot-dir <path>    where screenshots go
--exit-after-screenshots   quit after the last screenshot
--screenshot-devtools      include the dev panel in screenshots
```

## Balance simulations (SimRunner)

Plays hundreds of complete runs with no graphics, using a simple bot as the dungeon master, and compares the two huddle-ordering rules. Takes a few seconds.

```bash
dotnet run --project tools/HeroesUnwelcomed.SimRunner -- --runs 200
```

Open `sim-output/report.html` for the charts (CSVs are next to it). Options: `--runs`, `--max-beats`, `--rules ladder,reputation`, `--data`, `--out`. Edit `data/game/*.json` and run it again to see how balance changes.

## Running the tests

In Visual Studio: **Test → Run All Tests**. From a terminal:

```bash
dotnet test
```

## Documentation

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md): how the code is organised, and the milestone plan.

The original 2025 prototype is preserved at the git tag `prototype-2025`.
