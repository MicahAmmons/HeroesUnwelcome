using HeroesUnwelcomed.Core.Definitions;
using HeroesUnwelcomed.Core.Randomness;

namespace HeroesUnwelcomed.Core.Heroes;

public enum HeroState
{
    /// <summary>In the pool, available for the next party.</summary>
    Waiting,
    InParty,
    /// <summary>Dead; returns to the pool at <see cref="Hero.RespawnAtBeat"/>.</summary>
    Dead,
}

/// <summary>
/// One hero. Lives for the whole run: dies, respawns, and keeps everything it learned.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Skill"/> is what the hero can actually do, per category. What the
/// heroes *believe* about each other is kept separately in the <see cref="HeroCodex"/>.
/// The gap between the two is where the "getting smarter" story lives.
/// </para>
/// <para>
/// <b>Base skill vs. skill.</b> <see cref="BaseSkill"/> is what practice has built.
/// <see cref="Skill"/> adds the bonus of the hero's class, if any. Earning and
/// losing a class is judged on base skill, so a class's own bonus can never
/// keep a hero in it.
/// </para>
/// </remarks>
public sealed class Hero
{
    private readonly Dictionary<RoomCategory, double> _skills;
    private readonly Dictionary<RoomCategory, double> _startingSkills;
    private readonly Dictionary<RoomCategory, double> _lastPractised = [];

    internal Hero(int id, string name, Dictionary<RoomCategory, double> skills)
    {
        Id = id;
        Name = name;
        _skills = skills;
        _startingSkills = new Dictionary<RoomCategory, double>(skills);
    }

    public int Id { get; }
    public string Name { get; }
    public HeroState State { get; internal set; } = HeroState.Waiting;
    public double RespawnAtBeat { get; internal set; }

    public int Deaths { get; internal set; }
    public int RoomsCleared { get; internal set; }

    /// <summary>Base skills (without any class bonus).</summary>
    public IReadOnlyDictionary<RoomCategory, double> Skills => _skills;

    /// <summary>The hero's class (Warrior, Thief...), or null if not specialized.</summary>
    public HeroClass? Class { get; internal set; }

    /// <summary>Skill built by practice, without any class bonus.</summary>
    public double BaseSkill(RoomCategory category) => _skills.GetValueOrDefault(category);

    /// <summary>Effective skill: base skill plus the class bonus if the class is about this category.</summary>
    public double Skill(RoomCategory category) =>
        BaseSkill(category) + (Class is { } c && c.Category == category ? c.SkillBonus : 0);

    /// <summary>The skill rust can't go below.</summary>
    public double StartingSkill(RoomCategory category) => _startingSkills.GetValueOrDefault(category);

    /// <summary>Game beat this hero last attempted a room of <paramref name="category"/> (0 if never).</summary>
    public double LastPractised(RoomCategory category) => _lastPractised.GetValueOrDefault(category);

    internal void AddSkill(RoomCategory category, double amount) => _skills[category] = BaseSkill(category) + amount;

    internal void MarkPractised(RoomCategory category, double beat) => _lastPractised[category] = beat;

    /// <summary>Lowers a skill by <paramref name="amount"/>, but never below the starting value.</summary>
    internal void Rust(RoomCategory category, double amount)
    {
        if (_skills.TryGetValue(category, out double skill))
        {
            _skills[category] = Math.Max(StartingSkill(category), skill - amount);
        }
    }

    public override string ToString() => $"{Name}#{Id}";
}

/// <summary>Every hero in the run.</summary>
public sealed class HeroPool
{
    private readonly List<Hero> _heroes = [];

    public IReadOnlyList<Hero> All => _heroes;

    public IEnumerable<Hero> Waiting => _heroes.Where(h => h.State == HeroState.Waiting);

    public Hero ById(int id) => _heroes.First(h => h.Id == id);

    /// <summary>
    /// Creates <paramref name="count"/> heroes with names and slightly varied
    /// starting skills, drawn from <paramref name="random"/> so a seed always
    /// gives the same heroes.
    /// </summary>
    internal static HeroPool Generate(int count, HeroGeneration generation, DeterministicRandom random)
    {
        var pool = new HeroPool();
        List<string> names = generation.Names.Count > 0 ? [.. generation.Names] : ["Hero"];

        for (int i = 0; i < count; i++)
        {
            // Draw names without repeats until the list runs out, then add numbers.
            string name;
            if (names.Count > 0)
            {
                int pick = random.Next(names.Count);
                name = names[pick];
                names.RemoveAt(pick);
            }
            else
            {
                name = $"Hero {i + 1}";
            }

            var skills = new Dictionary<RoomCategory, double>();
            // Fixed category order, so the random draws happen in the same sequence every time.
            foreach (RoomCategory category in Enum.GetValues<RoomCategory>())
            {
                if (!generation.StartingSkill.TryGetValue(category, out double baseSkill))
                {
                    continue;
                }
                double nudge = (random.NextFloat() * 2 - 1) * generation.SkillVariation;
                skills[category] = Math.Max(0, baseSkill + nudge);
            }

            pool._heroes.Add(new Hero(i + 1, name, skills));
        }

        return pool;
    }
}
