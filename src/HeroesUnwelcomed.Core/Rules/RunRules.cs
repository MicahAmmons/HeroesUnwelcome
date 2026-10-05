using HeroesUnwelcomed.Core.Definitions;
using HeroesUnwelcomed.Core.Dungeons;
using HeroesUnwelcomed.Core.Simulation;

namespace HeroesUnwelcomed.Core.Rules;

/// <summary>Where a newly built room goes in the order heroes visit rooms.</summary>
public interface IRoomOrderRule
{
    /// <summary>Order index for the new room (the entrance is 0; the treasure room is always last).</summary>
    int InsertIndex(Dungeon dungeon, GridPoint position);
}

/// <summary>Rooms are visited in the order they were built: each new room goes just before the treasure room.</summary>
public sealed class BuildOrderRule : IRoomOrderRule
{
    public int InsertIndex(Dungeon dungeon, GridPoint position) => dungeon.Rooms.Count - 1;
}

/// <summary>When a new party enters by itself (besides the player's "send next party").</summary>
public interface IPartySpawnRule
{
    bool ShouldSpawn(Run run);

    public static IPartySpawnRule Create(string name, Tuning tuning) => name.ToLowerInvariant() switch
    {
        "manual" => new ManualSpawnRule(),
        "auto" => new PoolAndGapSpawnRule(tuning.SpawnGapRooms),
        _ => throw new InvalidDataException($"Unknown spawn rule '{name}'. Use 'manual' or 'auto'."),
    };
}

/// <summary>Parties only enter when sent (the debug "send next party" button, for now).</summary>
public sealed class ManualSpawnRule : IPartySpawnRule
{
    public bool ShouldSpawn(Run run) => false;
}

/// <summary>
/// The rule Micah leaned toward: a new party enters once the pool holds a full
/// party AND the newest party is at least <paramref name="gapRooms"/> rooms in.
/// If every party is dead, the next one goes immediately with whoever is waiting.
/// </summary>
public sealed class PoolAndGapSpawnRule(int gapRooms) : IPartySpawnRule
{
    public bool ShouldSpawn(Run run)
    {
        int waiting = run.Heroes.Waiting.Count();
        if (waiting == 0 || !run.CanSpawnParty)
        {
            return false;
        }

        Party? newest = run.ActiveParties.LastOrDefault();
        if (newest is null)
        {
            return true; // nobody inside: send whoever is ready
        }

        return waiting >= run.Setup.PartySize && newest.RoomIndex >= gapRooms;
    }
}

/// <summary>Decides when a cleared room ends the run.</summary>
public interface IRunEndRule
{
    bool EndsRun(Run run, RoomCleared cleared);
}

/// <summary>The first hero to beat the treasure room wins the run for the heroes.</summary>
public sealed class TreasureDefeatedRule : IRunEndRule
{
    public bool EndsRun(Run run, RoomCleared cleared) => cleared.RoomId == run.Dungeon.Treasure.Id;
}

/// <summary>The full set of rules a run uses. Swap any one to try a different design.</summary>
public sealed record RunRules(
    IChallengeRule Challenge,
    IGrowthRule Growth,
    IPartyOrderingRule Ordering,
    IRoomOrderRule RoomOrder,
    IPartySpawnRule Spawn,
    IRunEndRule RunEnd)
{
    public static RunRules FromSetup(RunSetup setup, Tuning tuning) => new(
        new LogisticChallengeRule(tuning.ChallengeSteepness),
        new PracticeGrowthRule(tuning),
        IPartyOrderingRule.Create(setup.OrderingRule, tuning),
        new BuildOrderRule(),
        IPartySpawnRule.Create(setup.SpawnRule, tuning),
        new TreasureDefeatedRule());
}
