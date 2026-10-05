using HeroesUnwelcomed.Core.Definitions;
using HeroesUnwelcomed.Core.Dungeons;
using HeroesUnwelcomed.Core.Simulation;
using HeroesUnwelcomed.Game.Animation;
using HeroesUnwelcomed.Game.Rendering;
using HeroesUnwelcomed.Game.Sound;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HeroesUnwelcomed.Game.Play;

/// <summary>
/// Draws the dungeon in world space: cells, rooms, monsters, and the
/// connection between each room and the next (hallway door or portal).
/// </summary>
/// <remarks>
/// Connections are worked out fresh from the simulation every frame
/// (<see cref="Dungeon.ConnectionAfter"/>), so a room's entrance sprite
/// changes the moment the layout or order changes, with nothing to keep in sync.
/// </remarks>
public sealed class DungeonRenderer(Primitives draw)
{
    private static readonly Color Background = new(20, 18, 28);
    private static readonly Color RoomHalfColor = new(30, 27, 42);
    private static readonly Color TravelHalfColor = new(24, 22, 33);
    private static readonly Color FloorColor = new(70, 64, 90);
    private static readonly Color EntryPortalColor = new(90, 200, 255);
    private static readonly Color ExitPortalColor = new(185, 120, 255);
    private static readonly Color DoorColor = new(150, 110, 70);

    public void Draw(SpriteBatch batch, Run run, RoomLayout layout, IReadOnlyDictionary<int, ScriptedAnimator> encounters,
        double gameBeat, BeatTime time, GridPoint selected, bool showSelection, float zoom)
    {
        Dungeon dungeon = run.Dungeon;
        int line = (int)Math.Max(2, 2 / zoom); // outlines stay ~2 px wide at any zoom

        DrawEmptyCells(batch, run, line);

        for (int i = 0; i < dungeon.Rooms.Count; i++)
        {
            Room room = dungeon.Rooms[i];
            DrawRoom(batch, run, layout, room, i, line, zoom);
            if (encounters.TryGetValue(room.Id, out ScriptedAnimator? encounter))
            {
                var (clip, frame) = encounter.FrameAt(gameBeat, time);
                PlaceholderSprite.Draw(batch, draw, layout.Encounter(room), encounter.Entity, clip, frame, scale: WorldLayout.SpriteScale);
            }
        }

        if (showSelection)
        {
            Rectangle cell = WorldLayout.CellRect(selected);
            cell.Inflate(-line * 2, -line * 2);
            draw.OutlineRect(batch, cell, Color.Gold, line * 4);
        }
    }

    private void DrawEmptyCells(SpriteBatch batch, Run run, int line)
    {
        bool building = run.Door == DoorState.Locked;
        for (int y = 0; y < run.Dungeon.Height; y++)
        {
            for (int x = 0; x < run.Dungeon.Width; x++)
            {
                var cell = new GridPoint(x, y);
                if (run.Dungeon.RoomAt(cell) is not null) continue;

                Rectangle rect = WorldLayout.CellRect(cell);
                rect.Inflate(-line * 6, -line * 6);
                draw.OutlineRect(batch, rect, building ? new Color(90, 90, 120) : new Color(38, 36, 50), line);
                if (building)
                {
                    draw.TextCentered(batch, "+ build", rect, new Color(110, 110, 140), 3f);
                }
            }
        }
    }

    private void DrawRoom(SpriteBatch batch, Run run, RoomLayout layout, Room room, int order, int line, float zoom)
    {
        Dungeon dungeon = run.Dungeon;
        Rectangle cell = WorldLayout.CellRect(room.Position);
        draw.FillRect(batch, cell, Background);
        draw.FillRect(batch, WorldLayout.TravelHalf(room.Position, room.Facing), TravelHalfColor);
        draw.FillRect(batch, WorldLayout.RoomHalf(room.Position, room.Facing), RoomHalfColor);

        int floorY = cell.Y + (int)WorldLayout.FloorY;
        draw.FillRect(batch, new Rectangle(cell.X, floorY, cell.Width, 8), FloorColor);

        // How heroes get in: a door at the end of a hallway, or an arrival portal.
        if (order > 0)
        {
            if (dungeon.ConnectionAfter(order - 1) == ConnectionKind.Hallway)
            {
                Vector2 door = layout.Door(room);
                var frame = new Rectangle((int)door.X - 60, floorY - 380, 120, 380);
                draw.FillRect(batch, frame, DoorColor * 0.35f);
                draw.OutlineRect(batch, frame, DoorColor, 8);
            }
            else
            {
                Vector2 portal = layout.EntryPortal(room);
                draw.Ring(batch, new Rectangle((int)portal.X - 110, floorY - 420, 220, 420), EntryPortalColor);
            }
        }

        // How heroes leave: an exit portal if the next room isn't straight ahead.
        if (order < dungeon.Rooms.Count - 1 && dungeon.ConnectionAfter(order) == ConnectionKind.Portal)
        {
            Vector2 portal = layout.ExitPortal(room);
            draw.Ring(batch, new Rectangle((int)portal.X - 100, floorY - 400, 200, 400), ExitPortalColor);
        }

        if (order == 0)
        {
            DrawEntrance(batch, run, layout.Encounter(room), floorY);
        }
        else if (dungeon.IsTreasure(order))
        {
            Vector2 chest = layout.At(room, layout.For(room.Kind.Id).Encounter, behind: -260);
            var box = new Rectangle((int)chest.X - 90, floorY - 110, 180, 110);
            draw.FillRect(batch, box, new Color(200, 160, 40) * 0.6f);
            draw.OutlineRect(batch, box, new Color(240, 200, 70), 6);
            draw.TextCentered(batch, "TREASURE", new Rectangle(box.X - 60, box.Y - 70, box.Width + 120, 50), new Color(240, 200, 70), 1.8f);
        }

        DrawLabels(batch, room, order, cell, zoom);
        draw.OutlineRect(batch, cell, new Color(55, 50, 75), line);
    }

    private void DrawEntrance(SpriteBatch batch, Run run, Vector2 center, int floorY)
    {
        var gate = new Rectangle((int)center.X - 170, floorY - 520, 340, 520);
        draw.FillRect(batch, gate, new Color(40, 36, 30));
        draw.OutlineRect(batch, gate, new Color(160, 140, 110), 10);

        // Bars across the gate show the door state at a glance.
        Color? bars = run.Door switch
        {
            DoorState.Locked => new Color(220, 60, 60),
            DoorState.Locking => new Color(230, 150, 50),
            _ => null,
        };
        if (bars is { } barColor)
        {
            for (int i = 1; i <= 4; i++)
            {
                draw.FillRect(batch, new Rectangle(gate.X + i * gate.Width / 5 - 8, gate.Y, 16, gate.Height), barColor);
            }
        }

        string label = run.Door switch
        {
            DoorState.Locked => "LOCKED",
            DoorState.Locking => "LOCKING...",
            _ => "OPEN",
        };
        draw.TextCentered(batch, label, new Rectangle(gate.X - 100, gate.Y - 110, gate.Width + 200, 70), bars ?? new Color(140, 220, 140), 2.6f);
    }

    /// <summary>
    /// Text size in world units that looks about <paramref name="screenScale"/> on screen
    /// (so labels stay readable when zoomed in), but never bigger than <paramref name="maxWorldScale"/>
    /// (so they still fit the room when zoomed far out).
    /// </summary>
    public static float ScreenSteadyScale(float screenScale, float zoom, float maxWorldScale) =>
        Math.Min(maxWorldScale, screenScale / zoom);

    private void DrawLabels(SpriteBatch batch, Room room, int order, Rectangle cell, float zoom)
    {
        float titleScale = ScreenSteadyScale(0.9f, zoom, 3.6f);
        string title = order == 0 ? "Entrance" : $"#{order}  {room.Kind.Id}";
        draw.Text(batch, title, new Vector2(cell.X + 40, cell.Y + 30), Color.White, titleScale);

        if (room.Kind.IsChallenge)
        {
            float detailScale = ScreenSteadyScale(0.55f, zoom, 2.2f);
            string details = $"{room.Kind.Category} · level {room.Level} · difficulty {room.Difficulty:0.0}";
            float titleHeight = draw.Font.LineSpacing * titleScale;
            draw.Text(batch, details, new Vector2(cell.X + 40, cell.Y + 30 + titleHeight), new Color(170, 165, 195), detailScale);
        }

        // Direction of travel through this room.
        string arrow = room.Facing == Facing.Right ? ">>  >>  >>" : "<<  <<  <<";
        draw.TextCentered(batch, arrow, new Rectangle(cell.X, cell.Bottom - 150, cell.Width, 80), new Color(80, 75, 105), 2.4f);
    }
}
