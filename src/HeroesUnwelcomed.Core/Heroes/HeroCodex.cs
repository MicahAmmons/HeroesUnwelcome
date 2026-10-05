namespace HeroesUnwelcomed.Core.Heroes;

/// <summary>What the heroes know about one hero against one room kind.</summary>
public sealed class CodexEntry
{
    public int Attempts { get; internal set; }
    public int Wins { get; internal set; }
    public int Losses => Attempts - Wins;

    /// <summary>The numeric reputation (used by the "reputation" ordering rule).</summary>
    public double Reputation { get; internal set; }
}

/// <summary>
/// The heroes' shared knowledge: who is good against what. Belongs to neither
/// heroes nor rooms. Either side can ask it questions.
/// </summary>
/// <remarks>
/// <para>
/// A <b>room</b> asks "rank the heroes for me" (the party arrives, the room
/// tells them the order; later, the room inspector shows this). A <b>hero</b>
/// asks "what am I known for?" (the hero inspector).
/// </para>
/// <para>
/// Knowledge is per room <i>kind</i> (Goblin, Pit Fall), shared by every room
/// of that kind. It holds both styles of reputation, a number per hero and a
/// ladder (ranked list) per room kind. The active ordering rule decides which
/// one it reads and updates.
/// </para>
/// </remarks>
public sealed class HeroCodex
{
    private readonly Dictionary<(int Hero, string Kind), CodexEntry> _entries = [];
    private readonly Dictionary<string, List<int>> _ladders = [];

    public CodexEntry? Get(int heroId, string kind) => _entries.GetValueOrDefault((heroId, kind));

    /// <summary>Every room kind this hero has faced, with what's known.</summary>
    public IEnumerable<(string Kind, CodexEntry Entry)> EntriesFor(int heroId) =>
        _entries.Where(e => e.Key.Hero == heroId).Select(e => (e.Key.Kind, e.Value));

    /// <summary>Ranked hero ids for a room kind, best first (used by the ladder rule).</summary>
    public IReadOnlyList<int> Ladder(string kind) =>
        _ladders.TryGetValue(kind, out var ladder) ? ladder : [];

    internal CodexEntry Entry(int heroId, string kind)
    {
        if (!_entries.TryGetValue((heroId, kind), out var entry))
        {
            entry = new CodexEntry();
            _entries[(heroId, kind)] = entry;
        }
        return entry;
    }

    internal List<int> MutableLadder(string kind)
    {
        if (!_ladders.TryGetValue(kind, out var ladder))
        {
            ladder = [];
            _ladders[kind] = ladder;
        }
        return ladder;
    }

    internal void RecordAttempt(int heroId, string kind, bool success)
    {
        CodexEntry entry = Entry(heroId, kind);
        entry.Attempts++;
        if (success)
        {
            entry.Wins++;
        }
    }
}
