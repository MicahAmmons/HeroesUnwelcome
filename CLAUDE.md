# Heroes Unwelcomed: notes for Claude

Read `docs/ARCHITECTURE.md` first: it holds the design decisions and milestone plan. Keep it updated when decisions change.

## Working with Micah

- Claude writes the systems; Micah keeps oversight and is learning. Explain what each system does and why it's built that way, including the maths. Expect and welcome questions.
- Talk designs through before building. Don't start a milestone without agreement.

## Build, run, test

```bash
dotnet build HeroesUnwelcomed.slnx
dotnet test
```

Run a scenario and capture screenshots for visual checks (Debug build):

```bash
src/HeroesUnwelcomed.Desktop/bin/Debug/net10.0/HeroesUnwelcomed.exe --scenario ui-layout --window 1280x800 --screenshot-frames 30 --screenshot-dir <scratch dir> --exit-after-screenshots
```

Add `--screenshot-devtools` to include the ImGui panel. Check visual changes at both 1920x1080 and Steam Deck 1280x800.

## Rules of the codebase

- `HeroesUnwelcomed.Core` and `HeroesUnwelcomed.Audio` must never reference MonoGame.
- Anything that moves is timed in beats from `AudioEngine.Time` (`BeatTime`), never in seconds or frames. Actions use the sim (game) beat; ambient loops use the music beat.
- Game logic lives in `Core` (`Run`): change state only through `GameCommand`s, report changes only through `GameEvent`s, keep it deterministic (fixed ticks, seeded forks, no wall-clock time, stable iteration order). Every tunable number belongs in `data/game/*.json`.
- After changing game logic or balance data, run the SimRunner (`dotnet run --project tools/HeroesUnwelcomed.SimRunner -- --runs 200`) and compare.
- No static managers for game state; pass dependencies in (see `GameServices`).
- Game code reads input only through `InputService` / `GameAction`. No hover-only interactions; everything must work with a gamepad.
- UI is laid out in UI units on `UiCanvas` and anchored with `UiCanvas.Place`.
- Randomness that affects gameplay comes from `DeterministicRandom`, with one `Fork` per purpose. Never use `System.Random` or `string.GetHashCode()` for gameplay.
- Dev tools live in `Game/DevTools/`, are excluded from Release builds, and gameplay code must not reference them (wrap the few hooks in `HeroesGame` with `#if DEBUG`).
- Package versions only in `Directory.Packages.props`; keep MonoGame and `.config/dotnet-tools.json` in sync.
- Style: file-scoped namespaces, explanatory comments on the *why*, `.editorconfig` conventions.
