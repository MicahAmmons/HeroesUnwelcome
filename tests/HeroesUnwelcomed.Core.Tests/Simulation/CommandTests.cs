using HeroesUnwelcomed.Core.Definitions;
using HeroesUnwelcomed.Core.Dungeons;
using HeroesUnwelcomed.Core.Simulation;

namespace HeroesUnwelcomed.Core.Tests.Simulation;

public class CommandTests
{
    [Fact]
    public void Building_IsRefused_UnlessTheDoorIsLocked()
    {
        var (run, _) = TestWorld.ScriptedRun(TestWorld.Setup(souls: 100));

        CommandResult result = run.Execute(new PlaceRoom("Goblin", new GridPoint(5, 1)));

        Assert.False(result.Accepted);
        Assert.Contains("Lock the door", result.Reason);
    }

    [Fact]
    public void LockingWithAPartyInside_WaitsForItToFinish()
    {
        var (run, dice) = TestWorld.ScriptedRun(TestWorld.Setup(souls: 100));
        dice.Default = false; // the party will be wiped

        run.Execute(new SendNextParty());
        run.Execute(new LockDoor());
        Assert.Equal(DoorState.Locking, run.Door);
        Assert.False(run.Execute(new SendNextParty()).Accepted);

        run.AdvanceTo(20);
        Assert.Equal(DoorState.Locked, run.Door);
        Assert.True(run.Execute(new PlaceRoom("Goblin", new GridPoint(5, 1))).Accepted);
    }

    [Fact]
    public void NewRoom_GoesJustBeforeTreasure_CostsSouls_AndConnectsByPortal()
    {
        var (run, _) = TestWorld.ScriptedRun(TestWorld.Setup(goblins: 2, souls: 25));
        run.Execute(new LockDoor());

        Assert.True(run.Execute(new PlaceRoom("Spikes", new GridPoint(6, 2))).Accepted);

        var rooms = run.Dungeon.Rooms;
        Assert.Equal("Spikes", rooms[^2].Kind.Id);
        Assert.Equal("Lich", rooms[^1].Kind.Id);
        Assert.Equal(17, run.Souls.Balance);                                         // 25 − 8
        Assert.Equal(ConnectionKind.Portal, run.Dungeon.ConnectionAfter(rooms.Count - 3)); // goblin → far-away spikes
        Assert.Equal(ConnectionKind.Portal, run.Dungeon.ConnectionAfter(rooms.Count - 2)); // spikes → lich
    }

    [Fact]
    public void Placement_ExplainsWhyItFailed()
    {
        var (run, _) = TestWorld.ScriptedRun(TestWorld.Setup(souls: 5));
        run.Execute(new LockDoor());

        Assert.Contains("outside", run.Execute(new PlaceRoom("Goblin", new GridPoint(99, 0))).Reason);
        Assert.Contains("already has", run.Execute(new PlaceRoom("Goblin", new GridPoint(1, 0))).Reason);
        Assert.Contains("costs 10 souls", run.Execute(new PlaceRoom("Goblin", new GridPoint(5, 1))).Reason);
        Assert.Contains("can't be built", run.Execute(new PlaceRoom("Lich", new GridPoint(5, 1))).Reason);
        Assert.Contains("no room called", run.Execute(new PlaceRoom("Dragon", new GridPoint(5, 1))).Reason);
    }

    [Fact]
    public void Upgrade_RaisesDifficulty_CostsMoreEachLevel_AndStopsAtMax()
    {
        var (run, _) = TestWorld.ScriptedRun(TestWorld.Setup(souls: 100));
        run.Execute(new LockDoor());
        Room goblin = run.Dungeon.Rooms[1];

        Assert.True(run.Execute(new UpgradeRoom(goblin.Id)).Accepted);  // costs 5
        Assert.True(run.Execute(new UpgradeRoom(goblin.Id)).Accepted);  // costs 10
        CommandResult third = run.Execute(new UpgradeRoom(goblin.Id));

        Assert.Equal(2, goblin.Level);
        Assert.Equal(3.0, goblin.Difficulty);
        Assert.Equal(85, run.Souls.Balance);
        Assert.Contains("maximum level", third.Reason);
    }

    [Fact]
    public void Flip_ChangesFacing_AndTurnsAHallwayIntoAPortal()
    {
        var (run, _) = TestWorld.ScriptedRun();
        run.Execute(new LockDoor());
        Assert.Equal(ConnectionKind.Hallway, run.Dungeon.ConnectionAfter(1));

        run.Execute(new FlipRoom(run.Dungeon.Rooms[1].Id));

        Assert.Equal(Facing.Left, run.Dungeon.Rooms[1].Facing);
        Assert.Equal(ConnectionKind.Portal, run.Dungeon.ConnectionAfter(1));
    }

    [Fact]
    public void Unlock_ReopensTheDoor()
    {
        var (run, _) = TestWorld.ScriptedRun();
        run.Execute(new LockDoor());

        Assert.True(run.Execute(new UnlockDoor()).Accepted);
        Assert.Equal(DoorState.Open, run.Door);
        Assert.True(run.Execute(new SendNextParty()).Accepted);
    }

    [Fact]
    public void OnlyAcceptedCommands_AreRecordedForReplay()
    {
        var (run, _) = TestWorld.ScriptedRun();

        run.Execute(new PlaceRoom("Goblin", new GridPoint(5, 1))); // refused: door open
        run.Execute(new SendNextParty());

        RecordedCommand recorded = Assert.Single(run.Commands);
        Assert.IsType<SendNextParty>(recorded.Command);
    }
}
