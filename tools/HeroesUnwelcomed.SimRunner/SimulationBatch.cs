using HeroesUnwelcomed.Core.Definitions;
using HeroesUnwelcomed.Core.Heroes;
using HeroesUnwelcomed.Core.Simulation;

namespace HeroesUnwelcomed.SimRunner;

/// <summary>A snapshot of one run at one moment.</summary>
public readonly record struct Sample(double Beat, double CombatSkill, double TrapSkill, double BossSkill, int Souls, int Specialized);

/// <summary>What one simulated run produced.</summary>
public sealed record RunResult(
    string Rule,
    ulong Seed,
    bool HeroesWon,
    double LengthBeats,
    int Parties,
    int Deaths,
    int SoulsEarned,
    int Huddles,
    int HuddlesPickingBest,
    IReadOnlyList<Sample> Samples,
    IReadOnlyDictionary<string, RoomKindStats> ByRoomKind,
    int Specializations,
    int SpecializationsLost,
    double? FirstSpecializationBeat)
{
    /// <summary>
    /// Share of huddles where the hero sent first really was the party's most
    /// skilled at that room's category: a measure of how "smart" the ordering is.
    /// </summary>
    public double BestPickRate => Huddles == 0 ? 0 : HuddlesPickingBest / (double)Huddles;
}

/// <summary>Plays complete runs, one per seed, and collects the results.</summary>
public sealed class SimulationBatch(GameDefinitions definitions, RunSetup baseSetup, double maxBeats, double sampleEveryBeats)
{
    public RunResult Play(string orderingRule, ulong seed)
    {
        RunSetup setup = baseSetup with { Seed = seed, OrderingRule = orderingRule, SpawnRule = "auto" };
        Run run = Run.Create(definitions, setup);
        var bot = new BotDungeonMaster();
        var samples = new List<Sample>();
        int huddles = 0;
        int bestPicks = 0;

        // Judge each huddle at the moment it happens, against actual skill.
        run.EventRaised += e =>
        {
            if (e is not PartyReordered reordered || reordered.HeroIds.Count < 2)
            {
                return;
            }
            RoomCategory category = run.Dungeon.RoomById(reordered.RoomId)!.Kind.Category;
            var members = reordered.HeroIds.Select(run.Heroes.ById).ToList();
            double bestSkill = members.Max(h => h.Skill(category));
            huddles++;
            if (members[0].Skill(category) >= bestSkill - 1e-9)
            {
                bestPicks++;
            }
        };

        for (double beat = 0; beat <= maxBeats && !run.IsOver; beat += 1)
        {
            run.AdvanceTo(beat);
            bot.Act(run);
            if (beat % sampleEveryBeats == 0)
            {
                samples.Add(Snapshot(run));
            }
        }

        RunMetrics metrics = RunMetrics.From(run);
        return new RunResult(orderingRule, seed, metrics.HeroesWon, metrics.LengthBeats, metrics.PartiesSent,
            metrics.HeroDeaths, metrics.SoulsEarned, huddles, bestPicks, samples, metrics.ByRoomKind,
            metrics.Specializations, metrics.SpecializationsLost, metrics.FirstSpecializationBeat);
    }

    private static Sample Snapshot(Run run)
    {
        IReadOnlyList<Hero> heroes = run.Heroes.All;
        return new Sample(
            run.Beat,
            heroes.Average(h => h.BaseSkill(RoomCategory.Combat)),
            heroes.Average(h => h.BaseSkill(RoomCategory.Trap)),
            heroes.Average(h => h.BaseSkill(RoomCategory.Boss)),
            run.Souls.Balance,
            heroes.Count(h => h.Class is not null));
    }
}
