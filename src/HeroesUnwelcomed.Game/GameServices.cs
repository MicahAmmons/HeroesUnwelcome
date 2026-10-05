using HeroesUnwelcomed.Game.Data;
using HeroesUnwelcomed.Game.Display;
using HeroesUnwelcomed.Game.Input;
using HeroesUnwelcomed.Game.Rendering;
using HeroesUnwelcomed.Game.Sound;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace HeroesUnwelcomed.Game;

/// <summary>
/// The shared tools every scene needs, handed to each scene when it's created.
/// </summary>
/// <remarks>
/// The old project made each manager a static class that anything could reach.
/// Here a scene receives exactly what it needs as a parameter. It's the same
/// convenience, but the dependencies are visible in the code, and two scenes (or
/// a test) can each get their own copies.
/// </remarks>
public sealed record GameServices(
    GraphicsDevice GraphicsDevice,
    ContentManager Content,
    InputService Input,
    UiCanvas Ui,
    Primitives Draw,
    AudioEngine Audio,
    DataSource Data);
