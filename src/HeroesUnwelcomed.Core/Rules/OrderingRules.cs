using HeroesUnwelcomed.Core.Definitions;
using HeroesUnwelcomed.Core.Heroes;

namespace HeroesUnwelcomed.Core.Rules;

/// <summary>
/// How a party decides who goes first at a room (the huddle), and how that
/// belief is updated after each attempt.
/// </summary>
public interface IPartyOrderingRule
{
    string Name { get; }

    /// <summary>The party's members in the order they'll attempt <paramref name="kind"/>, best first.</summary>
    IReadOnlyList<Hero> Order(IReadOnlyList<Hero> members, RoomKind kind, HeroCodex codex, GameDefinitions definitions);

    /// <summary>Updates what the heroes believe after <paramref name="hero"/> succeeded or failed.</summary>
    void Record(Hero hero, RoomKind kind, bool success, HeroCodex codex);

    public static IPartyOrderingRule Create(string name, Tuning tuning) => name.ToLowerInvariant() switch
    {
        LadderOrderingRule.RuleName => new LadderOrderingRule(),
        ReputationOrderingRule.RuleName => new ReputationOrderingRule(tuning),
        _ => throw new InvalidDataException($"Unknown ordering rule '{name}'. Use 'ladder' or 'reputation'."),
    };
}

/// <summary>
/// Each room kind keeps a ranked list of heroes. Win → move up one place;
/// lose → move down one place. Heroes new to that room kind join at the bottom.
/// Ties are impossible: every hero has exactly one place.
/// </summary>
public sealed class LadderOrderingRule : IPartyOrderingRule
{
    public const string RuleName = "ladder";

    public string Name => RuleName;

    public IReadOnlyList<Hero> Order(IReadOnlyList<Hero> members, RoomKind kind, HeroCodex codex, GameDefinitions definitions)
    {
        List<int> ladder = codex.MutableLadder(kind.Id);

        // Newcomers join at the bottom, lowest id first (deterministic).
        foreach (Hero hero in members.Where(h => !ladder.Contains(h.Id)).OrderBy(h => h.Id))
        {
            ladder.Add(hero.Id);
        }

        return members.OrderBy(h => ladder.IndexOf(h.Id)).ToList();
    }

    public void Record(Hero hero, RoomKind kind, bool success, HeroCodex codex)
    {
        List<int> ladder = codex.MutableLadder(kind.Id);
        int index = ladder.IndexOf(hero.Id);
        if (index < 0)
        {
            ladder.Add(hero.Id);
            index = ladder.Count - 1;
        }

        int target = success ? index - 1 : index + 1;
        if (target >= 0 && target < ladder.Count)
        {
            (ladder[index], ladder[target]) = (ladder[target], ladder[index]);
        }
    }
}

/// <summary>
/// Each hero has a reputation number per room kind. Win → up, lose → down.
/// Highest goes first.
/// </summary>
/// <remarks>
/// <para>
/// <b>Never faced this kind?</b> Use the hero's average reputation across other
/// kinds in the same category ("never met a vampire, but great at combat"),
/// or 0 with no history at all.
/// </para>
/// <para>
/// <b>Ties</b> are broken by higher actual skill in the category, then fewer
/// deaths, then lower hero id, so the result never depends on luck or list order.
/// </para>
/// </remarks>
public sealed class ReputationOrderingRule(Tuning tuning) : IPartyOrderingRule
{
    public const string RuleName = "reputation";

    public string Name => RuleName;

    public IReadOnlyList<Hero> Order(IReadOnlyList<Hero> members, RoomKind kind, HeroCodex codex, GameDefinitions definitions)
    {
        return members
            .OrderByDescending(h => Score(h, kind, codex, definitions))
            .ThenByDescending(h => h.Skill(kind.Category))
            .ThenBy(h => h.Deaths)
            .ThenBy(h => h.Id)
            .ToList();
    }

    public void Record(Hero hero, RoomKind kind, bool success, HeroCodex codex)
    {
        CodexEntry entry = codex.Entry(hero.Id, kind.Id);
        entry.Reputation += success ? tuning.ReputationOnWin : -tuning.ReputationOnLoss;
    }

    public static double Score(Hero hero, RoomKind kind, HeroCodex codex, GameDefinitions definitions)
    {
        if (codex.Get(hero.Id, kind.Id) is { Attempts: > 0 } known)
        {
            return known.Reputation;
        }

        var sameCategory = codex.EntriesFor(hero.Id)
            .Where(e => e.Entry.Attempts > 0 && definitions.HasRoom(e.Kind) && definitions.Room(e.Kind).Category == kind.Category)
            .Select(e => e.Entry.Reputation)
            .ToList();

        return sameCategory.Count > 0 ? sameCategory.Average() : 0;
    }
}
