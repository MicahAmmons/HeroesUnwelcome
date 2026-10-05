using HeroesUnwelcomed.Core.Definitions;
using Microsoft.Xna.Framework;

namespace HeroesUnwelcomed.Game.Play;

/// <summary>
/// Where things are in the world. Converts the simulation's grid cells into
/// world coordinates (the space the camera looks at).
/// </summary>
/// <remarks>
/// <para>
/// One grid cell is 1920×1080 world units, so at zoom 1 one room fills one
/// 1080p screen. Each cell is split in two along the direction of travel:
/// the <b>travel half</b> (hallway, door or arrival portal) and the <b>room half</b>
/// (the monster or trap).
/// </para>
/// <para>
/// <b>Local coordinates.</b> Positions inside a cell are given as a distance
/// <c>u</c> "along the way through": u = 0 is the side heroes enter from and
/// u = 1920 is the side they leave by. <see cref="ToWorld"/> turns that into a
/// world position, mirroring it for rooms that face left. Rooms can face either
/// way and every landmark (door, queue slots, monster) stays correct.
/// </para>
/// </remarks>
public static class WorldLayout
{
    public const float CellWidth = 1920f;
    public const float CellHeight = 1080f;

    /// <summary>Height of the floor line, measured from the top of the cell.</summary>
    public const float FloorY = CellHeight * 0.8f;

    // Anchor points (line, hero spot, encounter, door, portals) are data:
    // see RoomLayout and data/game/layout.json.

    /// <summary>
    /// Characters are drawn this much bigger than their size in the animation
    /// file, so they read well against a 1920×1080 room.
    /// </summary>
    public const float SpriteScale = 1.6f;

    public static Rectangle CellRect(GridPoint cell) =>
        new((int)(cell.X * CellWidth), (int)(cell.Y * CellHeight), (int)CellWidth, (int)CellHeight);

    /// <summary>A point <paramref name="u"/> units into the cell (along travel), on the floor.</summary>
    public static Vector2 ToWorld(GridPoint cell, Facing facing, float u, float y = FloorY)
    {
        float localX = facing == Facing.Right ? u : CellWidth - u;
        return new Vector2(cell.X * CellWidth + localX, cell.Y * CellHeight + y);
    }

    /// <summary>The rectangle covering the room half of a cell.</summary>
    public static Rectangle RoomHalf(GridPoint cell, Facing facing)
    {
        Rectangle rect = CellRect(cell);
        int half = rect.Width / 2;
        return facing == Facing.Right
            ? new Rectangle(rect.X + half, rect.Y, half, rect.Height)
            : new Rectangle(rect.X, rect.Y, half, rect.Height);
    }

    /// <summary>The rectangle covering the travel half of a cell.</summary>
    public static Rectangle TravelHalf(GridPoint cell, Facing facing)
    {
        Rectangle rect = CellRect(cell);
        int half = rect.Width / 2;
        return facing == Facing.Right
            ? new Rectangle(rect.X, rect.Y, half, rect.Height)
            : new Rectangle(rect.X + half, rect.Y, half, rect.Height);
    }

    /// <summary>The grid cell containing a world position.</summary>
    public static GridPoint CellAt(Vector2 world) =>
        new((int)MathF.Floor(world.X / CellWidth), (int)MathF.Floor(world.Y / CellHeight));

    /// <summary>The whole grid in world units.</summary>
    public static Rectangle GridBounds(int width, int height) =>
        new(0, 0, (int)(width * CellWidth), (int)(height * CellHeight));
}
