using HeroesUnwelcomed.Core.Definitions;
using HeroesUnwelcomed.Core.Simulation;

namespace HeroesUnwelcomed.Core.Tests.Simulation;

public class DeterminismAndDataTests
{
    // A readable fingerprint of a run's history, for comparing two runs.
    private static List<string> Fingerprint(Run run) =>
        run.Events.Select(e => e switch
        {
            HeroDied d => $"{d.Beat}:died:{d.HeroId}@{d.RoomId}",
            RoomCleared c => $"{c.Beat}:cleared:{c.HeroId}@{c.RoomId}",
            PartyReordered r => $"{r.Beat}:order:{string.Join(",", r.HeroIds)}",
            PartySpawned s => $"{s.Beat}:spawn:{string.Join(",", s.HeroIds)}",
            _ => $"{e.Beat}:{e.GetType().Name}",
        }).ToList();

    [Fact]
    public void SameSeed_GivesIdenticalRuns()
    {
        Run a = Run.Create(TestWorld.Definitions(), TestWorld.Setup(goblins: 3, pool: 6, spawn: "auto", seed: 42));
        Run b = Run.Create(TestWorld.Definitions(), TestWorld.Setup(goblins: 3, pool: 6, spawn: "auto", seed: 42));

        a.AdvanceTo(500);
        b.AdvanceTo(500);

        Assert.True(a.Events.Count > 50);
        Assert.Equal(Fingerprint(a), Fingerprint(b));
    }

    [Fact]
    public void DifferentSeeds_GiveDifferentRuns()
    {
        Run a = Run.Create(TestWorld.Definitions(), TestWorld.Setup(goblins: 3, pool: 6, spawn: "auto", seed: 1));
        Run b = Run.Create(TestWorld.Definitions(), TestWorld.Setup(goblins: 3, pool: 6, spawn: "auto", seed: 2));

        a.AdvanceTo(500);
        b.AdvanceTo(500);

        Assert.NotEqual(Fingerprint(a), Fingerprint(b));
    }

    [Fact]
    public void ReplayingRecordedCommands_ReproducesTheRun()
    {
        RunSetup setup = TestWorld.Setup(goblins: 2, pool: 4, souls: 50, seed: 9);
        Run original = Run.Create(TestWorld.Definitions(), setup);

        original.Execute(new SendNextParty());
        original.AdvanceTo(30);
        original.Execute(new LockDoor());
        original.AdvanceTo(80);
        original.Execute(new PlaceRoom("Spikes", new GridPoint(5, 2)));
        original.Execute(new UnlockDoor());
        original.Execute(new SendNextParty());
        original.AdvanceTo(200);

        // Replay: same seed, same commands at the same beats.
        Run replay = Run.Create(TestWorld.Definitions(), setup);
        foreach (RecordedCommand recorded in original.Commands)
        {
            replay.AdvanceTo(recorded.Beat);
            Assert.True(replay.Execute(recorded.Command).Accepted);
        }
        replay.AdvanceTo(200);

        Assert.Equal(Fingerprint(original), Fingerprint(replay));
    }

    [Fact]
    public void Metrics_AreCountedFromTheEventLog()
    {
        var (run, dice) = TestWorld.ScriptedRun();
        dice.Then(false, true, false); // party of 2: first hero dies, second clears room 1, then dies in room 2

        run.Execute(new SendNextParty());
        run.AdvanceTo(100);
        RunMetrics metrics = RunMetrics.From(run);

        Assert.Equal(1, metrics.PartiesSent);
        Assert.Equal(1, metrics.PartiesWiped);
        Assert.Equal(2, metrics.HeroDeaths);
        Assert.Equal(6, metrics.SoulsEarned);
        Assert.Equal(3, metrics.ByRoomKind["Goblin"].Attempts);
        Assert.Equal(2 / 3.0, metrics.ByRoomKind["Goblin"].KillRate, 9);
    }

    [Fact]
    public void ShippedDataFiles_LoadAndMakeAPlayableRun()
    {
        string data = Path.Combine(RepoRoot(), "data", "game");
        GameDefinitions defs = GameDefinitions.Parse(
            File.ReadAllText(Path.Combine(data, "rooms.json")),
            File.ReadAllText(Path.Combine(data, "tuning.json")),
            File.ReadAllText(Path.Combine(data, "heroes.json")));
        RunSetup setup = GameDefinitions.ParseSetup(File.ReadAllText(Path.Combine(data, "run-setup.json")));

        Run run = Run.Create(defs, setup);
        run.Execute(new SendNextParty());
        run.AdvanceTo(200);

        Assert.Equal(5, run.Dungeon.Rooms.Count);
        Assert.Equal(3, run.Dungeon.Rooms.Count(r => r.Kind.Id == "Goblin"));
        Assert.NotEmpty(run.Of<AttemptStarted>());
    }

    [Fact]
    public void BrokenData_ExplainsTheProblem()
    {
        var error = Assert.Throws<InvalidDataException>(() =>
            GameDefinitions.Parse("""{ "rooms": [ { "id": "X", "category": "Combat" } ] }""", "{}", "{}"));

        Assert.Contains("difficulty", error.Message);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "HeroesUnwelcomed.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("Couldn't find the repository root.");
    }
}
