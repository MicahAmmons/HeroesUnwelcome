namespace HeroesUnwelcomed.Core.Simulation;

/// <summary>Attempt results for one room kind.</summary>
public sealed record RoomKindStats(string Kind, int Attempts, int Kills, int Clears, int SoulsEarned)
{
    public double KillRate => Attempts == 0 ? 0 : Kills / (double)Attempts;
}

/// <summary>
/// Numbers worked out from a run's event log: the data a prestige system,
/// balance report or end-of-run screen would use.
/// </summary>
/// <remarks>
/// Everything here is calculated from <see cref="Run.Events"/>, never tracked
/// separately. Any new statistic can be added later and will work on old runs
/// (or replays) too.
/// </remarks>
public sealed record RunMetrics(
    double LengthBeats,
    bool HeroesWon,
    int PartiesSent,
    int PartiesWiped,
    int HeroDeaths,
    int SoulsEarned,
    int SoulsSpent,
    IReadOnlyDictionary<string, RoomKindStats> ByRoomKind,
    IReadOnlyDictionary<int, int> DeathsByRoomId,
    int Specializations,
    int SpecializationsLost,
    double? FirstSpecializationBeat)
{
    public static RunMetrics From(Run run)
    {
        var events = run.Events;
        var deaths = events.OfType<HeroDied>().ToList();
        var clears = events.OfType<RoomCleared>().ToList();
        var souls = events.OfType<SoulsChanged>().ToList();

        var kinds = deaths.Select(d => d.RoomKind).Concat(clears.Select(c => c.RoomKind)).Distinct();
        var byKind = kinds.ToDictionary(
            kind => kind,
            kind =>
            {
                int kills = deaths.Count(d => d.RoomKind == kind);
                int cleared = clears.Count(c => c.RoomKind == kind);
                var roomIds = deaths.Where(d => d.RoomKind == kind).Select(d => d.RoomId).ToHashSet();
                int earned = souls.Where(s => s.Amount > 0 && s.RoomId is { } id && roomIds.Contains(id)).Sum(s => s.Amount);
                return new RoomKindStats(kind, kills + cleared, kills, cleared, earned);
            });

        return new RunMetrics(
            run.Beat,
            run.Outcome is not null,
            events.OfType<PartySpawned>().Count(),
            events.OfType<PartyWiped>().Count(),
            deaths.Count,
            souls.Where(s => s.Amount > 0).Sum(s => s.Amount),
            -souls.Where(s => s.Amount < 0).Sum(s => s.Amount),
            byKind,
            deaths.GroupBy(d => d.RoomId).ToDictionary(g => g.Key, g => g.Count()),
            events.OfType<HeroSpecialized>().Count(),
            events.OfType<HeroLostSpecialization>().Count(),
            events.OfType<HeroSpecialized>().FirstOrDefault()?.Beat);
    }
}
