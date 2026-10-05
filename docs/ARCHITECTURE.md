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
| Room order | `BuildOrderRule`: new rooms go just before the treasure room | Preset grid path, player-defined |
| Party spawning | `ManualSpawnRule` (debug "send next party") and `PoolAndGapSpawnRule` (pool full + newest party ≥ 2 rooms in; all dead → send whoever's ready) | Anything |
| Run end | `TreasureDefeatedRule`: first hero to beat the treasure room | Treasure health, X heroes through |
| Challenge outcome | `LogisticChallengeRule`: chance = 1/(1+e^−(skill−difficulty)/steepness) | Anything |
| Growth | `PracticeGrowthRule`: skill +0.3 on a loss, +0.05 on a win, in the room's category | Anything |
| Huddle order | `LadderOrderingRule` (win = up one place, loss = down one) and `ReputationOrderingRule` (numeric score, category fallback, tie-break by skill → deaths → id) | Decided by SimRunner results |

## Design decisions so far

- **Linear path.** Each room has an order index. Connections are calculated from grid position + facing + order and are never stored: an adjacent room in the travel direction gives a hallway + door; anything else gives a portal. The connection is drawn as its own swappable sprite.
- **Room cells** are half travel area, half room. Rooms can be flipped so heroes travel left or right.
- **Door lock cycle:** Open → Locking (no new parties; current ones finish) → Locked (building allowed) → Open.
- **Pause** freezes Core (no ticks). Actors show idle animations and resume exactly where they were. Pause has calmer music.
- **Heroes:** *skill* per category (what they can actually do; grows with practice). Reputation (what the heroes *believe*) lives in the **Hero Codex**, per hero per room *kind* (Goblin, Pit Fall), shared by all rooms of that kind. Rooms ask the codex "rank the heroes for me"; heroes ask "what am I known for?".
- **Treasure room = the player's avatar** (lich lord, dragon…; skins later): a Boss-category challenge that can be upgraded. Beating it ends the run.
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
| `src/HeroesUnwelcomed.Audio` | Music system: tempo map/beat clock, mixer, music tracks, placeholder synth. No MonoGame, so it's testable sample by sample. |
| `src/HeroesUnwelcomed.Game` | Visuals, audio, input, UI, dev tools. Shared by all platforms. |
| `src/HeroesUnwelcomed.Desktop` | Windows/macOS/Linux/Deck launcher. |
| `tests/HeroesUnwelcomed.Core.Tests` | xUnit tests for Core. |
| `tests/HeroesUnwelcomed.Audio.Tests` | Sample-accurate tests for the music system. |
| `tests/HeroesUnwelcomed.Game.Tests` | Tests for Game logic that needs no window (animation timing, data parsing). |
| `tools/HeroesUnwelcomed.SimRunner` | Plays hundreds of runs without graphics (bot dungeon master) and writes CSVs + an HTML balance report to `sim-output/`. |
| `data/` | JSON: scenarios, speed levels, animations; rooms, tuning and costs later. Copied next to the exe as `Data/`. Debug builds read the repo's `data/` directly and live-reload it. |

Versions are pinned: .NET SDK in `global.json`, NuGet packages in `Directory.Packages.props`, MonoGame content tools in `.config/dotnet-tools.json` (keep these in sync with the MonoGame packages). Shared build settings are in `Directory.Build.props`.

Developer tools live in `src/HeroesUnwelcomed.Game/DevTools/` and are compiled **only in Debug builds**. Gameplay code never references them.

## Milestones

- **M0: Foundation** ✅. Solution, pinned versions, window that works at any resolution, input actions layer, UI canvas, ImGui dev panel, scenarios + launch profiles, automated screenshots, deterministic random generator + tests.
- **M1: Beat clock and audio prototype** ✅. Custom mixer that doubles as the master clock, generated placeholder stems per speed level (calm pause track included), speed changes on the beat, stem mute/unmute on the beat, animation system (music/sim clocks, one-shots, rate curves, pause shows idle), animation gallery, live JSON reload.
- **M2: Core game logic, no graphics** ✅. Data-driven room kinds/tuning/heroes/run setup (`data/game/`), dungeon grid + order + calculated connections + flipping, hero pool and codex, parties (travel → huddle → reorder → attempt → die/clear → wait/queue), souls ledger, door lock cycle, commands with refusal reasons, event log + replay, metrics, both ordering rules, SimRunner with bot DM and HTML report.
- **M3: First playable.** Draw Core's state: rooms, hallways/doors/portals, heroes, huddle reorder. Camera. Placing/upgrading rooms with controller and mouse. Debug hero inspector.
- **M4:** Multiple parties, room queues, waiting idles.
- **M5:** Music augments by influence, positional sound, Fear animation.
- **M6:** Specialization into classes, balance pass.
- **Later:** save mid-run, hero pool screen, costs, music brief for the composer, real art, Steam integration, Deck Verified, mobile port.

## How the game logic works (M2)

- **`Run`** owns everything for one play-through. `AdvanceTo(gameBeat)` steps in fixed ticks (8 per beat); each tick: respawns → spawn rule → step each active party (oldest first) → finish locking the door if empty. `Execute(command)` applies a player command at the current beat or refuses it with a reason.
- **A party holds** the room it's at or traveling to, so parties never overlap; a party whose next room is held waits where it is.
- **Determinism:** one seed → forked random streams (`heroes`, `challenge`, `party-selection`). The same seed plus the recorded commands at their beats reproduces a run exactly (tested).
- **Events** (`GameEvent` records) are the only output. Visuals, audio and metrics subscribe to `Run.EventRaised`; `RunMetrics` is calculated purely from `Run.Events`.
- **Data:** `data/game/rooms.json`, `tuning.json`, `heroes.json`, `run-setup.json`. Core parses JSON text but never touches files; callers load the text.

### First SimRunner findings (200 runs per rule, default data, bot DM)

| Rule | Heroes won | Median beats to win | Huddle sent the most skilled hero first |
|---|---|---|---|
| ladder | 98% | 3,344 | 66% |
| reputation | 100% | 3,545 | 81% |

The numeric reputation rule picks the truly best hero noticeably more often; the ladder adapts more slowly (one place per result). Both are kept; the choice is open. Balance numbers are placeholders.

## How time works (M1)

- **Audio frames are the master clock.** `AudioOutput` feeds 1024-frame buffers (48 kHz stereo, ~4 queued ≈ 85 ms) to MonoGame's `DynamicSoundEffectInstance`. Frames played = submitted − still queued.
- **`TempoMap`** (Audio project) converts frames ↔ beats using segments, each with its own tempo and *sim rate*. The *music beat* always moves; the *sim (game) beat* = music beats × sim rate, and stops when paused.
- **`AudioEngine`** smooths the "audible frame" estimate into a steady per-frame `BeatTime` (music beat, game beat, deltas, paused). Everything animates from this.
- **Speed levels** (`data/audio/speeds.json`): Paused 70 bpm × 0, 1x 100 bpm × 1, 2x 100 bpm × 2, 3x 150 bpm × 2. Effective game speed = bpm × sim rate, so they're exactly 1x/2x/3x and game beats always land on musical beats or half-beats. Changing speed: the sim rate changes immediately (pause feels instant); the music version switches on the next beat.
- **Stems** are muted/unmuted on a scheduled beat with a ≥5 ms ramp (no clicks). Muted stems keep their place, so they come back in time.
- **Animations** count beats, never seconds. Looping *music-clock* clips (idles) take their frame from the global beat, so they're always in unison. *Sim-clock* clips (actions) freeze while paused and show Idle instead, then resume exactly. One-shots can chain (`then`) and use playback-rate curves (Fear).
- **Known limitation:** audio is refilled from the game loop, so a stall longer than ~85 ms (e.g. dragging the window on Windows) briefly stutters the sound. A dedicated audio thread can fix this if needed. The first fill queues ~250 ms to cover slow startup frames.

## Known placeholders

- `Fonts/Ui.spritefont` uses Windows' Arial. It's fine for development, but it needs replacing with a properly licensed font before release.
- Music is generated by `PlaceholderMusic` (about 0.7 s at startup, ~45 MB of samples). Real music will be the composer's stems per speed level, loaded/streamed from files. The mixer won't change.
