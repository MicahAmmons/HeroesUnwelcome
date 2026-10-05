using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HeroesUnwelcomed.Game.Display;

/// <summary>Which point of the screen (and of the element) a UI element is pinned to.</summary>
public enum Anchor
{
    TopLeft, Top, TopRight,
    Left, Center, Right,
    BottomLeft, Bottom, BottomRight,
}

/// <summary>
/// The coordinate space all UI is designed in, so the UI looks right at any
/// window size or aspect ratio.
/// </summary>
/// <remarks>
/// <para>
/// UI is laid out in "UI units" against a 1920x1080 reference. Each frame we work
/// out a single <see cref="Scale"/> that makes the reference fit inside the real
/// window, e.g. 0.667 on a 1280x800 Steam Deck or 2.0 on a 4K monitor.
/// </para>
/// <para>
/// If the window's shape differs from 16:9, the canvas grows in the spare
/// direction instead of stretching. A 16:10 Deck screen gets a canvas 1920 wide
/// and 1200 tall. That's why UI should be anchored to edges and corners with
/// <see cref="Place"/>, not placed at fixed coordinates: an element pinned to
/// the bottom edge stays on the bottom edge whatever the screen shape.
/// </para>
/// <para>
/// Draw UI with <c>spriteBatch.Begin(transformMatrix: canvas.Transform)</c> and use
/// UI units everywhere; the matrix converts them to real pixels.
/// </para>
/// </remarks>
public sealed class UiCanvas
{
    public const float ReferenceWidth = 1920f;
    public const float ReferenceHeight = 1080f;

    /// <summary>Real pixels per UI unit.</summary>
    public float Scale { get; private set; } = 1f;

    /// <summary>Size of the whole window in UI units (at least 1920x1080).</summary>
    public Vector2 Size { get; private set; } = new(ReferenceWidth, ReferenceHeight);

    /// <summary>Converts UI units to screen pixels. Pass this to SpriteBatch.Begin.</summary>
    public Matrix Transform { get; private set; } = Matrix.Identity;

    /// <summary>Recalculates scale and size. Call once per frame (cheap).</summary>
    public void Update(Viewport viewport)
    {
        Scale = MathF.Min(viewport.Width / ReferenceWidth, viewport.Height / ReferenceHeight);
        Size = new Vector2(viewport.Width / Scale, viewport.Height / Scale);
        Transform = Matrix.CreateScale(Scale, Scale, 1f);
    }

    /// <summary>
    /// Returns the rectangle (in UI units) for an element of <paramref name="size"/>
    /// pinned to <paramref name="anchor"/>, kept <paramref name="margin"/> units in
    /// from the edges it is pinned to.
    /// </summary>
    /// <example>
    /// <c>Place(Anchor.BottomLeft, new(40, 40), new(300, 100))</c> puts a 300x100
    /// panel 40 units from the left edge and 40 units above the bottom edge.
    /// </example>
    public Rectangle Place(Anchor anchor, Vector2 margin, Vector2 size)
    {
        // How far across (0 = left, 0.5 = middle, 1 = right) and down the anchor is.
        (float fx, float fy) = AnchorFractions(anchor);

        // Inward direction for each axis: +1 pushes away from left/top, -1 away
        // from right/bottom, 0 means centred (margin then acts as a plain offset).
        float inwardX = fx == 0f ? 1f : fx == 1f ? -1f : 1f;
        float inwardY = fy == 0f ? 1f : fy == 1f ? -1f : 1f;

        float x = Size.X * fx - size.X * fx + margin.X * inwardX;
        float y = Size.Y * fy - size.Y * fy + margin.Y * inwardY;

        return new Rectangle((int)MathF.Round(x), (int)MathF.Round(y), (int)size.X, (int)size.Y);
    }

    /// <summary>Converts a screen pixel position (e.g. the mouse) into UI units.</summary>
    public Vector2 ScreenToUi(Point screen) => screen.ToVector2() / Scale;

    private static (float X, float Y) AnchorFractions(Anchor anchor) => anchor switch
    {
        Anchor.TopLeft => (0f, 0f),
        Anchor.Top => (0.5f, 0f),
        Anchor.TopRight => (1f, 0f),
        Anchor.Left => (0f, 0.5f),
        Anchor.Center => (0.5f, 0.5f),
        Anchor.Right => (1f, 0.5f),
        Anchor.BottomLeft => (0f, 1f),
        Anchor.Bottom => (0.5f, 1f),
        Anchor.BottomRight => (1f, 1f),
        _ => throw new ArgumentOutOfRangeException(nameof(anchor)),
    };
}
