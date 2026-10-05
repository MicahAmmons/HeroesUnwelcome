using HeroesUnwelcomed.Core.Dungeons;
using HeroesUnwelcomed.Core.Heroes;
using HeroesUnwelcomed.Core.Simulation;

namespace HeroesUnwelcomed.Core.Tests.Simulation;

public class RunFlowTests
{
    [Fact]
    public void Party_StepsThroughTheChoreography_OnSchedule()
    {
        var (run, _) = TestWorld.ScriptedRun();

        Assert.True(run.Execute(new SendNextParty()).Accepted);
        run.AdvanceTo(14);

        Assert.Equal(4, run.Of<PartyArrived>().Single().Beat);          // hallway = 4 beats
        Assert.Equal(6, run.Of<PartyReordered>().Single().Beat);        // the moment the banner is fully down
        Assert.Equal(8, run.Of<AttemptStarted>().Single().Beat);        // front hero heads in after the banner lifts
        Assert.Equal(11, run.Of<RoomCleared>().Single().Beat);          // rolled at the end of Action
        Assert.Equal(14, run.Of<PartyDeparted>().Last().Beat);          // after result, recover, catch up

        var steps = run.Of<PartyStepStarted>().Select(s => s.Step).ToList();
        Assert.Equal(
        [
            PartyPhase.ArrivePause, PartyPhase.BannerDown, PartyPhase.BannerHold, PartyPhase.BannerUp,
            PartyPhase.Entrance, PartyPhase.Ready, PartyPhase.Action, PartyPhase.Result, PartyPhase.Recover,
            PartyPhase.CatchUp,
        ], steps);
    }

    [Fact]
    public void Failure_KillsFrontHero_PaysSouls_ThenTheLineStepsUp()
    {
        var (run, dice) = TestWorld.ScriptedRun();
        dice.Then(false, true);

        run.Execute(new SendNextParty());
        run.AdvanceTo(16);

        HeroDied death = Assert.Single(run.Of<HeroDied>());
        Assert.Equal(11, death.Beat);
        Assert.Equal(3, run.Souls.Balance);                                  // goblin pays 3
        Assert.Single(run.Of<PartyReordered>());                             // no second banner for the same room
        Assert.Equal([8.0, 13.0], run.Of<AttemptStarted>().Select(a => a.Beat)); // next hero heads in after recovering
        Assert.Contains(run.Of<PartyStepStarted>(), s => s.Step == PartyPhase.StepUp && s.Beat == 13);
        Assert.Equal(16, run.Of<RoomCleared>().Single().Beat);              // step up 13, ready 14, action 15, roll 16
        Assert.Equal(HeroState.Dead, run.Heroes.ById(death.HeroId).State);
    }

    [Fact]
    public void Wipe_EndsPartyAfterTheLastResult_AndHeroesRespawnLater()
    {
        var (run, dice) = TestWorld.ScriptedRun();
        dice.Default = false;

        run.Execute(new SendNextParty());
        run.AdvanceTo(18);

        Assert.Equal([11.0, 16.0], run.Of<HeroDied>().Select(d => d.Beat));
        Assert.Equal(18, Assert.Single(run.Of<PartyWiped>()).Beat);          // after the last death's result + recover
        Assert.Empty(run.ActiveParties);

        run.AdvanceTo(26); // respawn 10 beats after each death
        Assert.Equal(2, run.Of<HeroRespawned>().Count());
        Assert.All(run.Heroes.All, h => Assert.Equal(HeroState.Waiting, h.State));
    }

    [Fact]
    public void DebugGap_AddsStillnessAfterEachStep_WithoutChangingStepLengths()
    {
        var defs = TestWorld.Definitions(TestWorld.Tuning with { DebugGapBeats = 1 });
        var run = Run.Create(defs, TestWorld.Setup());

        run.Execute(new SendNextParty());
        run.AdvanceTo(20);

        var steps = run.Of<PartyStepStarted>().ToList();
        Assert.All(steps, s => Assert.Equal(1, s.Duration));
        Assert.Equal([4.0, 6.0, 8.0], steps.Take(3).Select(s => s.Beat)); // each step starts 2 beats after the last
    }

    [Fact]
    public void BeatingTheTreasureRoom_EndsTheRun()
    {
        var (run, _) = TestWorld.ScriptedRun(TestWorld.Setup(goblins: 1));

        run.Execute(new SendNextParty());
        run.AdvanceTo(100);

        Assert.True(run.IsOver);
        Assert.Equal(run.Dungeon.Treasure.Id, run.Of<RoomCleared>().Last().RoomId);
        Assert.False(run.Execute(new SendNextParty()).Accepted);
        Assert.Equal(run.Outcome!.Beat, run.Beat); // time stops at the end
    }

    [Fact]
    public void SecondParty_WaitsForTheRoomAhead_ThenFollows()
    {
        var (run, _) = TestWorld.ScriptedRun(TestWorld.Setup(pool: 4, partySize: 2));

        run.Execute(new SendNextParty()); // party 1 leaves the entrance at once, holding room 1
        run.AdvanceTo(1);
        Assert.True(run.Execute(new SendNextParty()).Accepted);

        PartyWaiting waiting = Assert.Single(run.Of<PartyWaiting>());
        Assert.Equal((2, 1), (waiting.PartyId, waiting.BlockedByPartyId));
        Assert.False(run.Execute(new SendNextParty()).Accepted); // party 2 is still at the entrance

        // Party 1 clears room 1 and leaves it at beat 14 (see the timeline in TestWorld). Party 2 follows that same tick.
        run.AdvanceTo(14);
        PartyDeparted follow = run.Of<PartyDeparted>().Single(d => d.PartyId == 2);
        Assert.Equal(14, follow.Beat);
    }

    [Fact]
    public void TwoParties_NeverHoldTheSameRoom()
    {
        var (run, dice) = TestWorld.ScriptedRun(TestWorld.Setup(goblins: 4, pool: 6, partySize: 2, spawn: "auto"));
        for (int i = 0; i < 60; i++) dice.Then(i % 3 != 0); // every third attempt fails, so parties bunch up

        for (double beat = 0; beat < 300 && !run.IsOver; beat += 1.0 / Run.TicksPerBeat)
        {
            run.AdvanceTo(beat);
            var held = run.ActiveParties.Select(p => p.RoomIndex).ToList();
            Assert.Equal(held.Count, held.Distinct().Count());
        }
        Assert.True(run.AllParties.Count >= 2);
    }

    [Fact]
    public void ReachingTheNextRoom_UsesHallwayOrPortalTiming()
    {
        var (run, _) = TestWorld.ScriptedRun();

        run.Execute(new SendNextParty());

        PartyDeparted first = run.Of<PartyDeparted>().First();
        Assert.Equal(ConnectionKind.Hallway, first.Connection);
        Assert.Equal(4, first.ArriveBeat);
    }
}
