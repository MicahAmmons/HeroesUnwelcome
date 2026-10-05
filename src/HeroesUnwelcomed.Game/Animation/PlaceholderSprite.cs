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
    public static void Draw(SpriteBatch batch, Primitives draw, Vector2 feet, EntityDefinition entity,
        ClipDefinition clip, int frame)
    {
        Point size = entity.Size;
        var box = new Rectangle((int)(feet.X - size.X / 2f), (int)(feet.Y - size.Y), size.X, size.Y);

        draw.FillRect(batch, box, entity.Color * 0.25f);
        draw.OutlineRect(batch, box, entity.Color, 3);

        string label = $"{entity.Name} - {clip.Name}";
        Vector2 labelSize = draw.Font.MeasureString(label) * LabelScale;

        // 0 for the first frame, 1 for the last: slide the label down the box.
        float t = clip.Frames > 1 ? frame / (float)(clip.Frames - 1) : 0.5f;
        float top = box.Top + Padding;
        float bottom = box.Bottom - Padding - labelSize.Y;
        var position = new Vector2(feet.X - labelSize.X / 2f, MathHelper.Lerp(top, bottom, t));
        draw.Text(batch, label, Vector2.Round(position), Color.White, LabelScale);

        // Frame counter just above the box's top-right corner, e.g. "2/3" (outside, so it never covers the label).
        string counter = $"{frame + 1}/{clip.Frames}";
        Vector2 counterSize = draw.Font.MeasureString(counter) * 0.45f;
        draw.Text(batch, counter, new Vector2(box.Right - counterSize.X, box.Top - counterSize.Y - 4), entity.Color, 0.45f);
    }
}
