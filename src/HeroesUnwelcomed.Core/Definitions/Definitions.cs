namespace HeroesUnwelcomed.Core.Definitions;

/// <summary>What kind of challenge a room is. Heroes have a separate skill for each.</summary>
public enum RoomCategory
{
    /// <summary>Where heroes appear. Not a challenge.</summary>
    Entrance,
    Combat,
    Trap,
    /// <summary>The treasure room: the player's avatar (lich lord, dragon...). Beating it ends the run.</summary>
    Boss,
}

/// <summary>Which way a room faces: the direction heroes travel through it.</summary>
public enum Facing
{
    Right,
    Left,
}

/// <summary>A cell on the dungeon grid. X grows to the right, Y grows downward.</summary>
public readonly record struct GridPoint(int X, int Y)
{
    public GridPoint Step(Facing facing) => this with { X = X + (facing == Facing.Right ? 1 : -1) };

    public override string ToString() => $"({X},{Y})";
}

/// <summary>
/// A kind of room the player can build, e.g. "Goblin" or "Pit Fall Trap".
/// Loaded from data/game/rooms.json.
/// </summary>
/// <param name="Id">Unique name, also used to look up its animations later.</param>
/// <param name="Difficulty">How hard it is at level 0. Compared against a hero's skill in <paramref name="Category"/>.</param>
/// <param name="DifficultyPerLevel">Added for each upgrade level.</param>
/// <param name="Souls">Souls the player earns each time a hero dies here.</param>
/// <param name="PlaceCost">Souls to build one.</param>
/// <param name="UpgradeCost">Souls for the first upgrade; each later upgrade costs this × the level it reaches.</param>
/// <param name="Buildable">False for the entrance and treasure room, which every dungeon has exactly one of.</param>
public sealed record RoomKind(
    string Id,
    RoomCategory Category,
    double Difficulty = 0,
    double DifficultyPerLevel = 0,
    int MaxLevel = 0,
    int Souls = 0,
    int PlaceCost = 0,
    int UpgradeCost = 0,
    bool Buildable = true)
{
    public bool IsChallenge => Category != RoomCategory.Entrance;

    public double DifficultyAt(int level) => Difficulty + DifficultyPerLevel * level;

    /// <summary>Souls needed to upgrade from <paramref name="currentLevel"/> to the next level.</summary>
    public int UpgradeCostFrom(int currentLevel) => UpgradeCost * (currentLevel + 1);
}

/// <summary>
/// Every tunable number in the simulation. Loaded from data/game/tuning.json so
/// balance can be changed without touching code.
/// </summary>
public sealed record Tuning
{
    // How long things take, in game beats.
    public double HallwayBeats { get; init; } = 4;
    public double PortalBeats { get; init; } = 3;
    public double RespawnBeats { get; init; } = 32;

    // The encounter choreography: every step has a fixed length, in game beats.
    // The whole party (and the encounter) acts in lockstep through these.

    /// <summary>The line stops at the border; a short beat before the banner.</summary>
    public double ArrivePauseBeats { get; init; } = 1;

    /// <summary>The banner unfurls over the line. The order swaps the instant it's fully down.</summary>
    public double BannerDownBeats { get; init; } = 1;

    /// <summary>The banner stays down.</summary>
    public double BannerHoldBeats { get; init; } = 2;

    /// <summary>The banner furls back up.</summary>
    public double BannerUpBeats { get; init; } = 1;

    /// <summary>The front hero crosses from the border to the room's hero spot.</summary>
    public double EntranceBeats { get; init; } = 2;

    /// <summary>Face-off: the hero readies (sword raised), the encounter postures.</summary>
    public double ReadyBeats { get; init; } = 1;

    /// <summary>The attempt itself (fight, disarm...). The outcome is rolled at its end.</summary>
    public double ActionBeats { get; init; } = 2;

    /// <summary>Victory or death, with the party's reaction, for exactly this long.</summary>
    public double ResultBeats { get; init; } = 2;

    /// <summary>A beat of stillness after the result (a dead hero is gone by now).</summary>
    public double RecoverBeats { get; init; } = 1;

    /// <summary>After a death: the line moves up one space; the new front hero walks on to the hero spot.</summary>
    public double StepUpBeats { get; init; } = 2;

    /// <summary>After a win: the line walks up behind the victor before moving on together.</summary>
    public double CatchUpBeats { get; init; } = 2;

    /// <summary>Debugging aid: extra stillness after every step, to check everything starts and ends together. 0 in normal play.</summary>
    public double DebugGapBeats { get; init; }

    /// <summary>
    /// How sharply skill − difficulty turns into a success chance. Smaller =
    /// more decisive (a small edge almost guarantees success); larger = luckier.
    /// </summary>
    public double ChallengeSteepness { get; init; } = 0.8;

    // Growth: how much a hero's skill in a category rises after an attempt.
    public double SkillOnWin { get; init; } = 0.05;
    public double SkillOnLoss { get; init; } = 0.3;

    // Numeric reputation rule: change per result.
    public double ReputationOnWin { get; init; } = 1.0;
    public double ReputationOnLoss { get; init; } = 0.5;

    /// <summary>Automatic spawning: the newest party must be at least this many rooms in before the next one starts.</summary>
    public int SpawnGapRooms { get; init; } = 2;
}

/// <summary>How heroes are created. Loaded from data/game/heroes.json.</summary>
public sealed record HeroGeneration
{
    public IReadOnlyList<string> Names { get; init; } = [];

    /// <summary>Starting skill per category, before random variation.</summary>
    public IReadOnlyDictionary<RoomCategory, double> StartingSkill { get; init; } = new Dictionary<RoomCategory, double>();

    /// <summary>Each starting skill is randomly nudged by up to ± this much, so heroes differ from the start.</summary>
    public double SkillVariation { get; init; } = 0.3;
}

/// <summary>One room in a run's starting layout.</summary>
public sealed record RoomPlacement(string Kind, GridPoint Position, Facing Facing = Facing.Right);

/// <summary>
/// How a run begins: grid size, starting rooms, heroes, souls and which rules to use.
/// Loaded from data/game/run-setup.json; scenarios and the SimRunner can override parts.
/// </summary>
public sealed record RunSetup
{
    public ulong Seed { get; init; } = 1;
    public int GridWidth { get; init; } = 8;
    public int GridHeight { get; init; } = 5;

    /// <summary>In order: the entrance first, the treasure room last.</summary>
    public IReadOnlyList<RoomPlacement> Rooms { get; init; } = [];

    public int HeroPoolSize { get; init; } = 5;
    public int PartySize { get; init; } = 4;
    public int StartingSouls { get; init; }

    /// <summary>"reputation" (the chosen default) or "ladder". See the ordering rules.</summary>
    public string OrderingRule { get; init; } = "reputation";

    /// <summary>"manual" (only when told to) or "auto" (pool + gap rule).</summary>
    public string SpawnRule { get; init; } = "manual";
}
