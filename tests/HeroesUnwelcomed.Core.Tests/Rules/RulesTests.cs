using HeroesUnwelcomed.Core.Definitions;
using HeroesUnwelcomed.Core.Dungeons;
using HeroesUnwelcomed.Core.Heroes;
using HeroesUnwelcomed.Core.Randomness;
using HeroesUnwelcomed.Core.Rules;
using HeroesUnwelcomed.Core.Simulation;
using HeroesUnwelcomed.Core.Tests.Simulation;

namespace HeroesUnwelcomed.Core.Tests.Rules;

public class RulesTests
{
    private static readonly GameDefinitions Defs = TestWorld.Definitions();
    private static RoomKind Goblin => Defs.Room("Goblin");

    // Four heroes from a real run (ids 1..4).
    private static (Run Run, IReadOnlyList<Hero> Heroes) FourHeroes()
    {
        var (run, _) = TestWorld.ScriptedRun(TestWorld.Setup(pool: 4));
        return (run, run.Heroes.All);
    }

    [Fact]
    public void Challenge_IsFiftyFifty_WhenSkillEqualsDifficulty()
    {
        Assert.Equal(0.5, LogisticChallengeRule.Chance(2, 2, 0.8), 9);
        Assert.True(LogisticChallengeRule.Chance(3, 2, 0.8) > 0.75);
        Assert.True(LogisticChallengeRule.Chance(1, 2, 0.8) < 0.25);
    }

    [Fact]
    public void Challenge_RollsMatchTheChance_OverManyAttempts()
    {
        var (run, heroes) = FourHeroes();
        Room room = run.Dungeon.Rooms[1];
        var rule = new LogisticChallengeRule(0.8);
        var random = new DeterministicRandom(99);

        double expected = LogisticChallengeRule.Chance(heroes[0].Skill(RoomCategory.Combat), room.Difficulty, 0.8);
        int wins = Enumerable.Range(0, 20_000).Count(_ => rule.Resolve(heroes[0], room, random).Success);

        // Within ±0.01 (about 4 standard errors for 20,000 rolls).
        Assert.InRange(wins / 20_000.0, expected - 0.01, expected + 0.01);
    }

    [Fact]
    public void Growth_TeachesMoreFromDeathThanFromWinning()
    {
        var (run, heroes) = FourHeroes();
        var rule = new PracticeGrowthRule(new Tuning { SkillOnWin = 0.05, SkillOnLoss = 0.3 });
        double combatBefore = heroes[0].Skill(RoomCategory.Combat);
        double trapBefore = heroes[0].Skill(RoomCategory.Trap);

        rule.Apply(heroes[0], run.Dungeon.Rooms[1], success: false); // +0.3
        rule.Apply(heroes[0], run.Dungeon.Rooms[1], success: true);  // +0.05

        Assert.Equal(combatBefore + 0.35, heroes[0].Skill(RoomCategory.Combat), 9);
        Assert.Equal(trapBefore, heroes[0].Skill(RoomCategory.Trap)); // only the room's category grows
    }

    [Fact]
    public void Ladder_NewcomersJoinAtBottom_InIdOrder()
    {
        var (run, heroes) = FourHeroes();
        var rule = new LadderOrderingRule();

        var order = rule.Order([heroes[2], heroes[0], heroes[3]], Goblin, run.Codex, Defs);

        Assert.Equal([1, 3, 4], order.Select(h => h.Id));
    }

    [Fact]
    public void Ladder_WinMovesUpOne_LossMovesDownOne()
    {
        var (run, heroes) = FourHeroes();
        var rule = new LadderOrderingRule();
        rule.Order(heroes, Goblin, run.Codex, Defs); // ladder: 1,2,3,4

        rule.Record(heroes[2], Goblin, success: true, run.Codex);  // 3 passes 2 → 1,3,2,4
        rule.Record(heroes[0], Goblin, success: false, run.Codex); // 1 drops → 3,1,2,4
        rule.Record(heroes[2], Goblin, success: true, run.Codex);  // 3 already top → unchanged

        Assert.Equal([3, 1, 2, 4], run.Codex.Ladder("Goblin"));
    }

    [Fact]
    public void Reputation_HighestScoreGoesFirst()
    {
        var (run, heroes) = FourHeroes();
        var rule = new ReputationOrderingRule(TestWorld.Tuning with { ReputationOnWin = 1, ReputationOnLoss = 0.5 });

        Record(run, rule, heroes[3], true);   // +1
        Record(run, rule, heroes[1], false);  // −0.5

        var order = rule.Order(heroes, Goblin, run.Codex, Defs);

        Assert.Equal(4, order[0].Id);
        Assert.Equal(2, order[^1].Id);
    }

    [Fact]
    public void Reputation_FallsBackToSameCategoryKnowledge()
    {
        var (run, heroes) = FourHeroes();
        var rule = new ReputationOrderingRule(TestWorld.Tuning);
        var orc = new RoomKind("Orc", RoomCategory.Combat, Difficulty: 3);
        var defs = new GameDefinitions([.. Defs.RoomKinds, orc], Defs.Tuning, Defs.Heroes);

        Record(run, rule, heroes[2], true); // hero 3 has a goblin win, never fought an orc

        Assert.Equal(1.0, ReputationOrderingRule.Score(heroes[2], orc, run.Codex, defs));
        Assert.Equal(0.0, ReputationOrderingRule.Score(heroes[0], orc, run.Codex, defs));
        Assert.Equal(3, rule.Order(heroes, orc, run.Codex, defs)[0].Id);
    }

    [Fact]
    public void Reputation_TiesBreakByActualSkill()
    {
        var (run, heroes) = FourHeroes();
        var rule = new ReputationOrderingRule(TestWorld.Tuning);
        Hero best = heroes.MaxBy(h => h.Skill(RoomCategory.Combat))!;

        var order = rule.Order(heroes, Goblin, run.Codex, Defs); // nobody has history: all score 0

        Assert.Equal(best.Id, order[0].Id);
    }

    private static void Record(Run run, ReputationOrderingRule rule, Hero hero, bool success)
    {
        run.Codex.RecordAttempt(hero.Id, "Goblin", success);
        rule.Record(hero, Goblin, success, run.Codex);
    }
}
