using HeroesUnwelcomed.Core.Definitions;
using HeroesUnwelcomed.Core.Dungeons;
using HeroesUnwelcomed.Core.Heroes;
using HeroesUnwelcomed.Core.Randomness;
using HeroesUnwelcomed.Core.Rules;
using HeroesUnwelcomed.Core.Simulation;

namespace HeroesUnwelcomed.Core.Tests.Simulation;

/// <summary>A challenge rule the test controls: outcomes come from a script instead of dice.</summary>
internal sealed class ScriptedChallengeRule : IChallengeRule
{
    private readonly Queue<bool> _script = new();

    /// <summary>What to return once the script runs out.</summary>
    public bool Default { get; set; } = true;

    public ScriptedChallengeRule Then(params bool[] outcomes)
    {
        foreach (bool outcome in outcomes) _script.Enqueue(outcome);
        return this;
    }

    public ChallengeResult Resolve(Hero hero, Room room, DeterministicRandom random) =>
        new(_script.Count > 0 ? _script.Dequeue() : Default, 0.5);
}

/// <summary>Builders for small, predictable runs.</summary>
internal static class TestWorld
{
    public static readonly Tuning Tuning = new()
    {
        HallwayBeats = 4,
        PortalBeats = 3,
        HuddleBeats = 2,
        AttemptBeats = 2,
        RespawnBeats = 10,
        SpawnGapRooms = 2,
    };

    public static GameDefinitions Definitions(Tuning? tuning = null) => new(
        [
            new RoomKind("Entrance", RoomCategory.Entrance, Buildable: false),
            new RoomKind("Goblin", RoomCategory.Combat, Difficulty: 2, DifficultyPerLevel: 0.5, MaxLevel: 2, Souls: 3, PlaceCost: 10, UpgradeCost: 5),
            new RoomKind("Spikes", RoomCategory.Trap, Difficulty: 2, Souls: 2, PlaceCost: 8),
            new RoomKind("Lich", RoomCategory.Boss, Difficulty: 4, Souls: 10, Buildable: false),
        ],
        tuning ?? Tuning,
        new HeroGeneration
        {
            Names = ["A", "B", "C", "D", "E", "F"],
            StartingSkill = new Dictionary<RoomCategory, double> { [RoomCategory.Combat] = 1, [RoomCategory.Trap] = 1, [RoomCategory.Boss] = 0.5 },
            SkillVariation = 0.3,
        });

    /// <summary>Entrance, <paramref name="goblins"/> goblin rooms in a straight line, then the Lich.</summary>
    public static RunSetup Setup(int goblins = 2, int pool = 4, int partySize = 2, int souls = 0,
        string ordering = "ladder", string spawn = "manual", ulong seed = 7)
    {
        var rooms = new List<RoomPlacement> { new("Entrance", new GridPoint(0, 0)) };
        for (int i = 0; i < goblins; i++) rooms.Add(new RoomPlacement("Goblin", new GridPoint(i + 1, 0)));
        rooms.Add(new RoomPlacement("Lich", new GridPoint(goblins + 1, 0)));

        return new RunSetup
        {
            Seed = seed,
            GridWidth = 8,
            GridHeight = 3,
            Rooms = rooms,
            HeroPoolSize = pool,
            PartySize = partySize,
            StartingSouls = souls,
            OrderingRule = ordering,
            SpawnRule = spawn,
        };
    }

    /// <summary>A run whose challenge outcomes the test scripts.</summary>
    public static (Run Run, ScriptedChallengeRule Dice) ScriptedRun(RunSetup? setup = null)
    {
        setup ??= Setup();
        var defs = Definitions();
        var dice = new ScriptedChallengeRule();
        RunRules rules = RunRules.FromSetup(setup, defs.Tuning) with { Challenge = dice };
        return (Run.Create(defs, setup, rules), dice);
    }

    public static IEnumerable<T> Of<T>(this Run run) => run.Events.OfType<T>();
}
