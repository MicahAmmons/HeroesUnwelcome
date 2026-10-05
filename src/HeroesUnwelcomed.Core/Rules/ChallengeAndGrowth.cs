using HeroesUnwelcomed.Core.Definitions;
using HeroesUnwelcomed.Core.Dungeons;
using HeroesUnwelcomed.Core.Heroes;
using HeroesUnwelcomed.Core.Randomness;

namespace HeroesUnwelcomed.Core.Rules;

/// <summary>The outcome of one hero attempting one room.</summary>
/// <param name="Chance">The success chance that was rolled against (0..1), kept for metrics and debugging.</param>
public readonly record struct ChallengeResult(bool Success, double Chance);

/// <summary>Decides whether a hero beats a room.</summary>
public interface IChallengeRule
{
    ChallengeResult Resolve(Hero hero, Room room, DeterministicRandom random);
}

/// <summary>
/// Success chance from skill minus difficulty on an S-shaped ("logistic") curve.
/// </summary>
/// <remarks>
/// chance = 1 / (1 + e^(−(skill − difficulty) / steepness))
/// <list type="bullet">
///   <item>skill = difficulty → exactly 50%.</item>
///   <item>A big edge either way approaches 100% or 0% but never reaches it: there's always a little luck.</item>
///   <item>Steepness sets how much luck: 0.8 means a 1-point edge gives about 78%.</item>
/// </list>
/// </remarks>
public sealed class LogisticChallengeRule(double steepness) : IChallengeRule
{
    public static double Chance(double skill, double difficulty, double steepness) =>
        1.0 / (1.0 + Math.Exp(-(skill - difficulty) / steepness));

    public ChallengeResult Resolve(Hero hero, Room room, DeterministicRandom random)
    {
        double chance = Chance(hero.Skill(room.Kind.Category), room.Difficulty, steepness);
        return new ChallengeResult(random.NextFloat() < chance, chance);
    }
}

/// <summary>How a hero's actual skill changes after an attempt.</summary>
public interface IGrowthRule
{
    void Apply(Hero hero, Room room, bool success);
}

/// <summary>
/// Heroes learn mostly from failure: dying to a room type teaches a lot,
/// winning teaches a little. So whatever the player uses most, the heroes get
/// good at. That's the core tension.
/// </summary>
public sealed class PracticeGrowthRule(Tuning tuning) : IGrowthRule
{
    public void Apply(Hero hero, Room room, bool success) =>
        hero.AddSkill(room.Kind.Category, success ? tuning.SkillOnWin : tuning.SkillOnLoss);
}
