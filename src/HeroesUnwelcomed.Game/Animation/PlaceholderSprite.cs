using HeroesUnwelcomed.Game.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HeroesUnwelcomed.Game.Animation;

/// <summary>
/// Stand-in art: a box where the real sprite will go, labelled "Vampire - Idle".
/// The label moves top → middle → bottom across the frames, so you can see the
/// animation advancing and its timing against the beat.
/// </summary>
public static class PlaceholderSprite
{
    private const float LabelScale = 0.5f;
    private const int Padding = 10;

    /// <param name="feet">Bottom-centre of the sprite: where the character stands.</param>
    /// <param name="opacity">1 = solid; lower fades the whole sprite (e.g. a dead hero).</param>
    /// <param name="scale">Size multiplier for the box (the label keeps its size).</param>
    public static void Draw(SpriteBatch batch, Primitives draw, Vector2 feet, EntityDefinition entity,
        ClipDefinition clip, int frame, float opacity = 1f, float scale = 1f)
    {
        var size = new Point((int)(entity.Size.X * scale), (int)(entity.Size.Y * scale));
        var box = new Rectangle((int)(feet.X - size.X / 2f), (int)(feet.Y - size.Y), size.X, size.Y);

        draw.FillRect(batch, box, entity.Color * (0.25f * opacity));
        draw.OutlineRect(batch, box, entity.Color * opacity, 3);

        string label = $"{entity.Name} - {clip.Name}";
        float labelScale = LabelScale * scale;
        Vector2 labelSize = draw.Font.MeasureString(label) * labelScale;

        // 0 for the first frame, 1 for the last: slide the label down the box.
        float t = clip.Frames > 1 ? frame / (float)(clip.Frames - 1) : 0.5f;
        float top = box.Top + Padding;
        float bottom = box.Bottom - Padding - labelSize.Y;
        var position = new Vector2(feet.X - labelSize.X / 2f, MathHelper.Lerp(top, bottom, t));
        draw.Text(batch, label, Vector2.Round(position), Color.White * opacity, labelScale);

        // Frame counter just above the box's top-right corner, e.g. "2/3" (outside, so it never covers the label).
        string counter = $"{frame + 1}/{clip.Frames}";
        float counterScale = 0.45f * scale;
        Vector2 counterSize = draw.Font.MeasureString(counter) * counterScale;
        draw.Text(batch, counter, new Vector2(box.Right - counterSize.X, box.Top - counterSize.Y - 4), entity.Color * opacity, counterScale);
    }
}
