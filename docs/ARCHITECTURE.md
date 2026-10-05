# Heroes Unwelcomed: Architecture

A living document. Update it whenever a decision changes.

## The game in one paragraph

The player owns a dungeon, a strictly linear sequence of rooms placed on a capped grid. Parties of heroes walk in single file. At each room they huddle, reorder themselves by who they believe is best at that room type, and attempt it one hero at a time. A failure kills that hero, which earns the player souls. Souls buy and upgrade rooms, but only while the dungeon door is locked. Heroes respawn, keep their growth, and get better at whatever kills them. The run ends when a party reaches the treasure room. The heart of the game is heroes who visibly learn and adapt, and the player out-thinking them.

## The big picture

```
 Player input ──► Commands ──► ┌────────────────────────┐ ──► Events ──► Visuals (animation, camera, UI)
 Debug panel  ──► Commands ──► │  CORE (the game logic) │ ──► Events ──► Audio (music augments, sounds)
 Replay file  ──► Commands ──► │  heroes, rooms, souls  │ ──► Events ──► Metrics (deaths, kills, souls…)
                               └────────────────────────┘
                                          ▲
                           Beat clock: "advance one tick"
```

1. **Core knows nothing about graphics or sound.** `HeroesUnwelcomed.Core` has no MonoGame reference, so it can't accidentally depend on visuals. That's what makes tests, balance simulations, saving and replays straightforward.
2. **Commands go in.** Every player action is a command object (`PlaceRoom`, `UpgradeRoom`, `LockDoor`, `SendNextParty`). Core validates it (e.g. "can't build while the door is open") and applies it. The debug panel sends the same commands. A replay is the list of commands with their beats.
3. **Events come out.** Core announces what happened (`HeroDied`, `PartyHuddled`, `RoomCleared`, `SoulsEarned`). Visuals, audio and metrics listen and react in their own ways. Core never says "play this animation".
4. **Time moves in beat ticks.** The audio clock tells Core "advance one tick" (several ticks per beat). Speed changes how fast ticks arrive; pause stops them. Core always runs in fixed steps, which keeps seeded replays exact.

## Swappable rules

Undecided design questions are isolated behind replaceable rules, so changing one never ripples through the code.

| Rule | First version | Planned alternatives |
|---|---|---|
| Room order | Build order | Preset grid path, player-defined |
| Party spawning | Manual "send next party" | Pool size + 2-room gap + all-dead fallback |
| Run end | First party reaches treasure | Treasure health, X heroes through |
| Challenge outcome | Skill vs room stat + luck | Anything |
| Growth | Skill up on a loss, reputation up on a win | Anything |

## Design decisions so far

- **Linear path.** Each room has an order index. Connections are calculated from grid position + facing + order and are never stored: an adjacent room in the travel direction gives a hallway + door; anything else gives a portal. The connection is drawn as its own swappable sprite.
- **Room cells** are half travel area, half room. Rooms can be flipped so heroes travel left or right.
- **Door lock cycle:** Open → Locking (no new parties; current ones finish) → Locked (building allowed) → Open.
- **Pause** freezes Core (no ticks). Actors show idle animations and resume exactly where they were. Pause has calmer music.
- **Heroes:** per-room-type *skill* (grows with practice) and per-room-type *reputation* (the party's belief about who's best, which orders the huddle; it can be wrong for a while).
- **Multiple parties:** each room holds one party at a time; later parties wait (idle) at the previous room.
- **Economy and metrics:** souls go through a ledger where every gain and spend is recorded. Costs live in data (zero at first). Metrics are an event log, so future prestige formulas are just maths over it. Runs are self-contained for now.
- **Music:** one global beat (from game speed) that never stops. One shared track made of stems. Parties request *augments* (cut stems, play stingers) weighted by their camera-based *influence*, and the strongest request wins. Party sounds play with volume and stereo pan by proximity. Global or Local mode is a player option.
- **Placeholder art** is generated at runtime: a rectangle + "Name - Animation" label that moves between 3 positions on the beat. Real art replaces it as it arrives.
- **Input:** game actions, not buttons. No hover-only interactions. One selection/focus model shared by mouse, gamepad and (later) touch. Target: Steam Deck Verified.
- **UI** is designed on a 1920x1080 reference canvas that scales to the window and grows in the spare direction. Anchor everything to edges.

## Solution layout

| Project | Purpose |
|---|---|
| `src/HeroesUnwelcomed.Core` | Game logic: no MonoGame. |
| `src/HeroesUnwelcomed.Game` | Visuals, audio, input, UI, dev tools. Shared by all platforms. |
| `src/HeroesUnwelcomed.Desktop` | Windows/macOS/Linux/Deck launcher. |
| `tests/HeroesUnwelcomed.Core.Tests` | xUnit tests for Core. |
| `tools/HeroesUnwelcomed.SimRunner` *(M2)* | Runs thousands of runs without graphics and outputs balance charts. |
| `data/` | JSON: scenarios now; rooms, tuning, costs later. Copied next to the exe as `Data/`. |

Versions are pinned: .NET SDK in `global.json`, NuGet packages in `Directory.Packages.props`, MonoGame content tools in `.config/dotnet-tools.json` (keep these in sync with the MonoGame packages). Shared build settings are in `Directory.Build.props`.

Developer tools live in `src/HeroesUnwelcomed.Game/DevTools/` and are compiled **only in Debug builds**. Gameplay code never references them.

## Milestones

- **M0: Foundation** ✅. Solution, pinned versions, window that works at any resolution, input actions layer, UI canvas, ImGui dev panel, scenarios + launch profiles, automated screenshots, deterministic random generator + tests.
- **M1: Beat clock and audio prototype.** Custom mixer (also the master clock), generated placeholder stems at 3 tempos plus a calm pause track, speed changes on bar lines, stem mute/unmute on the beat, animation gallery with beat-synced placeholder animations.
- **M2: Core game logic, no graphics.** Grid, build-order rule, connection calculation, flipping, one party: walk → huddle → reorder → attempt; death → souls → respawn; skill/reputation growth; run end; door lock cycle; commands, events, metrics; tests; first SimRunner chart.
- **M3: First playable.** Draw Core's state: rooms, hallways/doors/portals, heroes, huddle reorder. Camera. Placing/upgrading rooms with controller and mouse. Debug hero inspector.
- **M4:** Multiple parties, room queues, waiting idles.
- **M5:** Music augments by influence, positional sound, Fear animation.
- **M6:** Specialization into classes, balance pass.
- **Later:** save mid-run, hero pool screen, costs, music brief for the composer, real art, Steam integration, Deck Verified, mobile port.

## Known placeholders

- `Fonts/Ui.spritefont` uses Windows' Arial. It's fine for development, but it needs replacing with a properly licensed font before release.
