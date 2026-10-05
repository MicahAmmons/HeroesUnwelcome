using HeroesUnwelcomed.Core.Definitions;
using HeroesUnwelcomed.Game.Display;
using HeroesUnwelcomed.Game.Play;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HeroesUnwelcomed.Game.Tests.Play;

public class WorldAndCameraTests
{
    private static readonly Viewport Screen = new(0, 0, 1600, 900);

    [Fact]
    public void Path_AtStartAndEnd()
    {
        var path = new MovementPath(new Vector2(0, 0)).WalkTo(new Vector2(100, 0));

        Assert.Equal(new Vector2(0, 0), path.At(0));
        Assert.Equal(new Vector2(50, 0), path.At(0.5f));
        Assert.Equal(new Vector2(100, 0), path.At(1));
    }

    [Fact]
    public void Path_PortalJumpTakesNoTime()
    {
        // Walk 100, jump far away, walk 100 more: halfway is exactly the moment of the jump.
        var path = new MovementPath(new Vector2(0, 0))
            .WalkTo(new Vector2(100, 0))
            .JumpTo(new Vector2(5000, 0))
            .WalkTo(new Vector2(5100, 0));

        Assert.Equal(200, path.WalkedLength);
        Assert.Equal(new Vector2(5050, 0), path.At(0.75f));
    }

    [Fact]
    public void Layout_MirrorsForLeftFacingRooms()
    {
        var cell = new GridPoint(2, 1);

        Vector2 right = WorldLayout.ToWorld(cell, Facing.Right, 100);
        Vector2 left = WorldLayout.ToWorld(cell, Facing.Left, 100);

        Assert.Equal(2 * 1920 + 100, right.X);
        Assert.Equal(3 * 1920 - 100, left.X);
        Assert.Equal(right.Y, left.Y);
    }

    [Fact]
    public void Camera_ScreenAndWorldConversionsAreInverses()
    {
        var camera = new Camera2D { Position = new Vector2(3000, 1200) };
        camera.ZoomAt(0.4f / camera.Zoom, new Vector2(800, 450), Screen);

        Vector2 world = camera.ScreenToWorld(new Vector2(123, 456), Screen);
        Vector2 back = camera.WorldToScreen(world, Screen);

        Assert.Equal(123, back.X, 3);
        Assert.Equal(456, back.Y, 3);
    }

    [Fact]
    public void Camera_ZoomKeepsThePointUnderTheCursorFixed()
    {
        var camera = new Camera2D { Position = new Vector2(1000, 500) };
        var cursor = new Vector2(300, 200);
        Vector2 before = camera.ScreenToWorld(cursor, Screen);

        camera.ZoomAt(1.8f, cursor, Screen);

        Vector2 after = camera.ScreenToWorld(cursor, Screen);
        Assert.Equal(before.X, after.X, 2);
        Assert.Equal(before.Y, after.Y, 2);
    }

    [Fact]
    public void Camera_StaysInsideBounds()
    {
        var camera = new Camera2D { Bounds = new Rectangle(0, 0, 1000, 1000) };

        camera.PanByScreen(new Vector2(-100_000, 100_000));

        Assert.Equal(new Vector2(0, 1000), camera.Position);
    }
}
