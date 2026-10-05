using HeroesUnwelcomed.Core.Definitions;
using HeroesUnwelcomed.Core.Simulation;
using HeroesUnwelcomed.Game.Play;

namespace HeroesUnwelcomed.Game.Tests.Play;

public class BuildControllerTests
{
    private static GameDefinitions Definitions() => new(
        [
            new RoomKind("Entrance", RoomCategory.Entrance, Buildable: false),
            new RoomKind("Goblin", RoomCategory.Combat, Difficulty: 2, MaxLevel: 3, Souls: 3, PlaceCost: 10, UpgradeCost: 5),
            new RoomKind("Lich", RoomCategory.Boss, Difficulty: 4, Buildable: false),
        ],
        new Tuning(),
        new HeroGeneration { Names = ["A", "B", "C"], StartingSkill = new Dictionary<RoomCategory, double> { [RoomCategory.Combat] = 1 } });

    private static (Run Run, BuildController Build, List<string> Messages) Setup(int souls = 100)
    {
        var setup = new RunSetup
        {
            GridWidth = 5,
            GridHeight = 3,
            Rooms =
            [
                new RoomPlacement("Entrance", new GridPoint(0, 1)),
                new RoomPlacement("Goblin", new GridPoint(1, 1)),
                new RoomPlacement("Lich", new GridPoint(2, 1)),
            ],
            HeroPoolSize = 3,
            PartySize = 2,
            StartingSouls = souls,
        };
        Run run = Run.Create(Definitions(), setup);
        var build = new BuildController(run);
        var messages = new List<string>();
        build.Message += messages.Add;
        return (run, build, messages);
    }

    [Fact]
    public void StartsOnTheFirstRoomAfterTheEntrance()
    {
        var (_, build, _) = Setup();

        Assert.Equal(new GridPoint(1, 1), build.Selected);
    }

    [Fact]
    public void Move_StaysInsideTheGrid()
    {
        var (_, build, _) = Setup();

        for (int i = 0; i < 10; i++) build.Move(1, 1);

        Assert.Equal(new GridPoint(4, 2), build.Selected);
    }

    [Fact]
    public void ConfirmOnEmptyCell_WhileOpen_ExplainsTheDoorRule()
    {
        var (_, build, messages) = Setup();
        build.Move(0, 1); // (1,2) is empty

        build.Confirm();

        Assert.Null(build.OpenMenu);
        Assert.Contains("Lock the door", Assert.Single(messages));
    }

    [Fact]
    public void BuildingThroughTheMenu_PlacesTheRoom_AndClosesTheMenu()
    {
        var (run, build, messages) = Setup();
        run.Execute(new LockDoor());
        build.PointAt(new GridPoint(3, 0));

        build.Confirm();                                     // opens the build menu
        Assert.Equal("Goblin", build.OpenMenu!.Items[0].Label);
        build.Confirm();                                     // chooses Goblin

        Assert.Null(build.OpenMenu);
        Assert.Equal("Goblin", run.Dungeon.RoomAt(new GridPoint(3, 0))!.Kind.Id);
        Assert.Equal(90, run.Souls.Balance);
        Assert.StartsWith("Built Goblin", messages.Last());
    }

    [Fact]
    public void RoomMenu_UpgradesAndRefreshes()
    {
        var (run, build, _) = Setup();
        run.Execute(new LockDoor());

        build.Confirm(); // room menu for the goblin at (1,1)
        Assert.Equal("Upgrade", build.OpenMenu!.Items[0].Label);
        build.Confirm(); // upgrade

        Assert.Equal(1, run.Dungeon.RoomAt(new GridPoint(1, 1))!.Level);
        Assert.Contains("Level 1 → 2", build.OpenMenu!.Items[0].Detail);
    }

    [Fact]
    public void ChoosingWithoutTheDoorLocked_ShowsTheGamesReason()
    {
        var (_, build, messages) = Setup();

        build.Confirm();              // room menu opens even with the door open (for information)
        build.Confirm();              // try "Upgrade"

        Assert.Contains("Lock the door", messages.Last());
    }

    [Fact]
    public void WhileAMenuIsOpen_UpDownMovesTheMenu_NotTheGrid()
    {
        var (run, build, _) = Setup();
        run.Execute(new LockDoor());
        build.Confirm();
        GridPoint before = build.Selected;

        build.Move(0, 1);
        build.PointAt(new GridPoint(4, 2));

        Assert.Equal(1, build.OpenMenu!.Selected);
        Assert.Equal(before, build.Selected);
    }

    [Fact]
    public void Cancel_ClosesTheMenu()
    {
        var (_, build, _) = Setup();
        build.Confirm();

        build.Cancel();

        Assert.Null(build.OpenMenu);
    }
}
