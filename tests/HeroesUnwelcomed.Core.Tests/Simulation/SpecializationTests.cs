using HeroesUnwelcomed.Core.Definitions;
using HeroesUnwelcomed.Core.Heroes;
using HeroesUnwelcomed.Core.Simulation;

namespace HeroesUnwelcomed.Core.Tests.Simulation;

public class SpecializationTests
{
    private static readonly HeroClass Warrior = new("Warrior", RoomCategory.Combat, Threshold: 3, SkillBonus: 0.5);
    private static readonly HeroClass Thief = new("Thief", RoomCategory.Trap, Threshold: 3, SkillBonus: 0.5);

    private static Run NewRun(double rustGrace = 10, double rustPerBeat = 0.1)
    {
        GameDefinitions basic = TestWorld.Definitions(TestWorld.Tuning with { SkillRustGraceBeats = rustGrace, SkillRustPerBeat = rustPerBeat });
        var defs = new GameDefinitions(basic.RoomKinds, basic.Tuning, basic.Heroes, [Warrior, Thief]);
        return Run.Create(defs, TestWorld.Setup());
    }

    private static Hero FirstHero(Run run) => run.Heroes.All[0];

    [Fact]
    public void ReachingTheThreshold_GrantsTheClass_AndItsBonus()
    {
        Run run = NewRun();
        Hero hero = FirstHero(run);
        hero.AddSkill(RoomCategory.Combat, 3 - hero.BaseSkill(RoomCategory.Combat)); // exactly at the threshold

        run.AdvanceTo(0.125); // classes are reviewed every tick

        Assert.Equal("Warrior", hero.Class?.Id);
        Assert.Equal(3.5, hero.Skill(RoomCategory.Combat), 9);  // bonus applies to effective skill
        Assert.Equal(3.0, hero.BaseSkill(RoomCategory.Combat), 9);
        HeroSpecialized promoted = Assert.Single(run.Of<HeroSpecialized>());
        Assert.Equal((hero.Id, "Warrior"), (promoted.HeroId, promoted.ClassId));
    }

    [Fact]
    public void OnlyOneClass_TheFirstReachedKeepsIt()
    {
        Run run = NewRun(rustGrace: 10_000);
        Hero hero = FirstHero(run);
        hero.AddSkill(RoomCategory.Trap, 5);
        run.AdvanceTo(1);
        hero.AddSkill(RoomCategory.Combat, 5); // now qualifies for Warrior too
        run.AdvanceTo(2);

        Assert.Equal("Thief", hero.Class?.Id);
        Assert.Single(run.Of<HeroSpecialized>());
        Assert.Equal(hero.BaseSkill(RoomCategory.Combat), hero.Skill(RoomCategory.Combat)); // no Warrior bonus
    }

    [Fact]
    public void UnpractisedSkills_Rust_ButNeverBelowStartingValue()
    {
        Run run = NewRun(rustGrace: 10, rustPerBeat: 0.1);
        Hero hero = FirstHero(run);
        double start = hero.StartingSkill(RoomCategory.Combat);
        hero.AddSkill(RoomCategory.Combat, 1.0);

        run.AdvanceTo(10);
        Assert.Equal(start + 1.0, hero.BaseSkill(RoomCategory.Combat), 6); // still within the grace period

        run.AdvanceTo(15);
        Assert.Equal(start + 0.5, hero.BaseSkill(RoomCategory.Combat), 2); // 5 beats × 0.1

        run.AdvanceTo(100);
        Assert.Equal(start, hero.BaseSkill(RoomCategory.Combat), 9);       // floored at the start
    }

    [Fact]
    public void RustingBelowTheThreshold_LosesTheClass_TheBonusDoesntSaveIt()
    {
        Run run = NewRun(rustGrace: 10, rustPerBeat: 0.1);
        Hero hero = FirstHero(run);
        hero.AddSkill(RoomCategory.Combat, 3.2 - hero.BaseSkill(RoomCategory.Combat));
        run.AdvanceTo(1);
        Assert.Equal("Warrior", hero.Class?.Id);

        run.AdvanceTo(13); // 3 beats of rust: base 2.9 (< 3), even though base + bonus would be 3.4

        Assert.Null(hero.Class);
        HeroLostSpecialization lost = Assert.Single(run.Of<HeroLostSpecialization>());
        Assert.Equal("Warrior", lost.ClassId);
    }

    [Fact]
    public void AfterLosingAClass_AnotherAlreadyEarnedTakesOver()
    {
        Run run = NewRun(rustGrace: 10, rustPerBeat: 0.1);
        Hero hero = FirstHero(run);
        hero.AddSkill(RoomCategory.Combat, 3.2 - hero.BaseSkill(RoomCategory.Combat));
        run.AdvanceTo(1);                       // Warrior first
        hero.AddSkill(RoomCategory.Trap, 10);   // way above the Thief threshold; rust won't get it below in time
        run.AdvanceTo(13);                      // Combat rusts below 3 → loses Warrior → Thief takes over

        Assert.Equal("Thief", hero.Class?.Id);
        Assert.Equal(["Warrior", "Thief"], run.Of<HeroSpecialized>().Select(s => s.ClassId));
    }

    [Fact]
    public void Attempts_CountAsPractice()
    {
        var (run, _) = TestWorld.ScriptedRun();
        run.Execute(new SendNextParty());

        run.AdvanceTo(12); // first attempt resolves at 11

        int attemptedBy = run.Of<RoomCleared>().Single().HeroId;
        Assert.Equal(11, run.Heroes.ById(attemptedBy).LastPractised(RoomCategory.Combat));
    }

    [Fact]
    public void Metrics_CountSpecializations()
    {
        Run run = NewRun(rustGrace: 10, rustPerBeat: 0.1);
        Hero hero = FirstHero(run);
        hero.AddSkill(RoomCategory.Combat, 3.2 - hero.BaseSkill(RoomCategory.Combat));
        run.AdvanceTo(20);

        RunMetrics metrics = RunMetrics.From(run);

        Assert.Equal(1, metrics.Specializations);
        Assert.Equal(1, metrics.SpecializationsLost);
        Assert.Equal(0.125, metrics.FirstSpecializationBeat);
    }
}
