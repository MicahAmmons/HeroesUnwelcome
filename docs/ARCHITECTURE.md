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
- **M3: First playable** ✅. Play scene (now the game's default start): world layout, camera (pan/zoom/fit/follow selection), dungeon renderer with calculated doors and portals, hero views that walk timed paths and glide to queue/fight spots, event-driven animations (huddle, attack, celebrate, death + teammates' fear, monster reactions), HUD (souls, door, pool, "who goes first here" rankings, prompts, toasts, run-over screen), build controller with build/upgrade/flip menus usable by controller, keyboard and mouse, door toggle and send-party actions, dev-panel run inspector, play scenarios.
- **M3.5: Choreography** ✅. Beat-scripted encounter flow (see below), party moves as one formation, banner reorder, room anchor points as data, the full animation vocabulary, choreography length check, no-overlap tests.
- **M4:** Multiple parties polish (most of it already works: queueing, waiting, auto spawning).
- **M5: Music reacting to parties** ✅. Influence-weighted stem reactions (strongest wins, on the beat), positional party sounds, Local/Global music mode, placeholder sound effects, reactions as data.
- **M6:** Specialization into classes, balance pass.
- **Later:** save mid-run, hero pool screen, costs, music brief for the composer, real art, Steam integration, Deck Verified, mobile port.

## How the game logic works (M2)

- **`Run`** owns everything for one play-through. `AdvanceTo(gameBeat)` steps in fixed ticks (8 per beat); each tick: respawns → spawn rule → step each active party (oldest first) → finish locking the door if empty. `Execute(command)` applies a player command at the current beat or refuses it with a reason.
- **A party holds** the room it's at or traveling to, so parties never overlap; a party whose next room is held waits where it is.
- **Determinism:** one seed → forked random streams (`heroes`, `challenge`, `party-selection`). The same seed plus the recorded commands at their beats reproduces a run exactly (tested).
- **Events** (`GameEvent` records) are the only output. Visuals, audio and metrics subscribe to `Run.EventRaised`; `RunMetrics` is calculated purely from `Run.Events`.
- **Data:** `data/game/rooms.json`, `tuning.json`, `heroes.json`, `run-setup.json`. Core parses JSON text but never touches files; callers load the text.

## Music reacting to parties (M5)

- **One shared track, shaped by parties.** A party event (e.g. a hero dying) asks for a *reaction* from `data/audio/reactions.json`: which stems to cut and how deeply, landing on the next half-beat, holding, returning on a bar line, with a stinger (drum fill) leading back in.
- **Influence** (`Proximity`): each party's reach over the music = zoom factor (0 when zoomed out to the dungeon, rising smoothly to 1 when zoomed in) × centre factor (1 in the middle of the screen, 0 just past the edge). `PartyAudio` updates it every frame from the front hero's position, so panning hands the music from one party to another.
- **`AugmentController`** (Audio project) combines reactions per stem: cut = depth × influence × envelope; the **strongest cut wins** (reactions never stack deeper). The mixer re-reads it every 128 frames (≈2.7 ms) at its render position, so cuts and returns land on their beats, with an ~8 ms glide to avoid clicks.
- **Party sounds** (hit, gasp timed to the cut, cheer, banner whoosh) are mixed on top of the music, with volume (never below `minSoundVolume`, so distant drama is faintly audible) and left/right pan from the party's place on screen. Sounds can start exactly on a music beat.
- **Music mode**: Local (the above) or Global (party reactions off; the track plays untouched; sounds still play). Default in the data file; toggle in the dev panel for now.
- Placeholder sound effects are synthesized (`PlaceholderSounds`), like the music.

## Encounter choreography (M3.5)

Every encounter is a fixed script. Each step has a length in beats (`data/game/tuning.json`), and the whole party and the encounter act in lockstep:

| Step | Front hero | Rest of the line | Encounter |
|---|---|---|---|
| Walk | Walk (Portal Entrance / Exit near portals) | Walk, same pace, fixed spacing | Idle |
| Arrive pause | stops at the border | Idle | Idle |
| Banner down / hold / up | covered; the order swaps the instant it's fully down | covered | Idle |
| Entrance | EncounterEntrance to the Hero Spot | Idle | HeroEnter |
| Ready | Ready (face-off) | Idle | Ready |
| Action | Fight / Disarm / Puzzle / Magic (by category) | Idle | Interact |
| Result | Victory / Death | VictoryReact / DeathReact | Defeat / Victory |
| Recover | (fallen hero removed) | Idle | Idle / stays defeated |
| Step up (loss) | new front walks on to the Hero Spot → Ready | line moves up one space | HeroEnter |
| Catch up (win) | victor keeps celebrating | walk up behind the victor → Walk on | Defeat |

- **Core** (`Run`) owns the timing: each step is a `PartyPhase`, announced with a `PartyStepStarted(beat, step, duration)` event. The dice roll at the end of Action. `debugGapBeats` adds stillness after every step for checking timing by eye.
- **`Choreographer`** turns each step into exact instructions per participant: a stretch of route from beat A to beat B, and a clip from beat A. **The party is one object**: when travelling, one route runs from the back of the line through every member to the leader and on; everyone moves the same distance, so spacing and pace are identical. Positions come from anchors plus line index, so heroes never share a place. A test plays whole runs and checks no two heroes ever overlap.
- **`ScriptedAnimator`** calculates frames from a clip's start beat (no running timers): one-shots hold their last frame until the next step. **`ChoreographyCheck`** lists clips longer than their step (shown in the dev panel; a test keeps the shipped data clean).
- **Anchors** (`data/game/layout.json`): as fractions of the cell width along travel. Line front 0.44 (just before the border/door at 0.5), Hero Spot ⅓ into the encounter half, encounter ⅔ in, portals, spacing. Room kinds can override any of them.

## How the playable game works (M3)

- **`PlayScene`** advances the `Run` to the current game beat each frame, passes events to the `Choreographer`, and draws positions and frames calculated from the game beat (so pause freezes everything exactly).
- **`WorldLayout`**: one grid cell = 1920×1080 world units, split into a travel half and an encounter half; positions inside a cell are measured "along the way through", so left-facing rooms are just mirrored.
- **`BuildController`** takes intentions (move, point at, confirm, cancel), not buttons, so keyboard, gamepad, mouse and later touch drive identical logic. Every change goes through a Core command, and refusals show Core's reason.
- **Readability:** characters draw 1.6× their data size; room labels and names keep a steady on-screen size whatever the zoom.
- **Scenario parameters** for the play scene: `setup` (run-setup file), `startLocked`, `focusRoom`, `zoom`.
- **Chosen huddle rule: reputation** (after the M2 simulations). Ladder remains available via `orderingRule`.

### First SimRunner findings (200 runs per rule, default data, bot DM)

| Rule | Heroes won | Median beats to win | Huddle sent the most skilled hero first |
|---|---|---|---|
| ladder | 98% | 3,344 | 66% |
| reputation | 100% | 3,545 | 81% |

After the M3.5 choreography (slower, more theatrical encounters): reputation 100% won, median 4,839 beats, best-pick 82%; ladder median 5,163 beats, best-pick 68%.

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
- Sound effects are generated by `PlaceholderSounds` (gasp, hit, cheer, whoosh, fill).
- Music is generated by `PlaceholderMusic` (about 0.7 s at startup, ~45 MB of samples). Real music will be the composer's stems per speed level, loaded/streamed from files. The mixer won't change.
