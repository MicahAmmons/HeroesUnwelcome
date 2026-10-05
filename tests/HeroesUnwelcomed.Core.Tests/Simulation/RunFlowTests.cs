using HeroesUnwelcomed.Core.Dungeons;
using HeroesUnwelcomed.Core.Heroes;
using HeroesUnwelcomed.Core.Simulation;

namespace HeroesUnwelcomed.Core.Tests.Simulation;

public class RunFlowTests
{
    [Fact]
    public void Party_WalksHuddlesAndAttempts_OnSchedule()
    {
        var (run, _) = TestWorld.ScriptedRun();

        Assert.True(run.Execute(new SendNextParty()).Accepted);
        run.AdvanceTo(4);  // hallway = 4 beats
        Assert.Single(run.Of<PartyArrived>());
        Assert.Single(run.Of<PartyHuddleStarted>());

        run.AdvanceTo(6);  // huddle = 2 beats
        Assert.Single(run.Of<PartyReordered>());
        Assert.Single(run.Of<AttemptStarted>());

        run.AdvanceTo(8);  // attempt = 2 beats → scripted default: success
        RoomCleared cleared = Assert.Single(run.Of<RoomCleared>());
        Assert.Equal(8, cleared.Beat);
        Assert.Equal(2, run.Of<PartyDeparted>().Count()); // entrance → room 1, room 1 → room 2
    }

    [Fact]
    public void Failure_KillsAttempter_PaysSouls_AndNextHeroTriesWithoutAHuddle()
    {
        var (run, dice) = TestWorld.ScriptedRun();
        dice.Then(false, true);

        run.Execute(new SendNextParty());
        run.AdvanceTo(10);

        HeroDied death = Assert.Single(run.Of<HeroDied>());
        Assert.Equal(8, death.Beat);
        Assert.Equal(3, run.Souls.Balance);                   // goblin pays 3
        Assert.Single(run.Of<PartyHuddleStarted>());          // still only one huddle
        Assert.Equal(2, run.Of<AttemptStarted>().Count());    // second hero stepped up at once
        Assert.Single(run.Of<RoomCleared>());
        Assert.Equal(HeroState.Dead, run.Heroes.ById(death.HeroId).State);
    }

    [Fact]
    public void Wipe_EndsParty_AndHeroesRespawnLater()
    {
        var (run, dice) = TestWorld.ScriptedRun();
        dice.Default = false;

        run.Execute(new SendNextParty());
        run.AdvanceTo(10);

        Assert.Single(run.Of<PartyWiped>());
        Assert.Empty(run.ActiveParties);
        Assert.Equal(2, run.Heroes.All.Count(h => h.State == HeroState.Dead));

        run.AdvanceTo(20); // deaths at 8 and 10, respawn 10 beats later
        Assert.Equal(2, run.Of<HeroRespawned>().Count());
        Assert.All(run.Heroes.All, h => Assert.Equal(HeroState.Waiting, h.State));
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

        // Party 1: arrive 4, huddle to 6, attempt to 8, clears and moves on. Party 2 follows that same tick.
        run.AdvanceTo(8);
        PartyDeparted follow = run.Of<PartyDeparted>().Single(d => d.PartyId == 2);
        Assert.Equal(8, follow.Beat);
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
