using HeroesUnwelcomed.Core.Dungeons;
using HeroesUnwelcomed.Core.Simulation;
using HeroesUnwelcomed.Game.Animation;

namespace HeroesUnwelcomed.Game.Play;

/// <summary>The banner covering a party during the reorder.</summary>
public sealed record BannerState(PartyPhase Step, double StartBeat, double Duration);

/// <summary>A party on screen: its line of heroes (front first) and its banner.</summary>
public sealed class PartyView(int id)
{
    public int Id { get; } = id;

    /// <summary>Hero ids, front of the line first. Matches the simulation's order.</summary>
    public List<int> Line { get; set; } = [];

    public int RoomId { get; set; }
    public BannerState? Banner { get; set; }
    public bool LastSucceeded { get; set; }
    public int? FallenHeroId { get; set; }
}

/// <summary>
/// Turns the simulation's choreography steps into exact instructions for every
/// hero and encounter: "move along this stretch of route from beat A to beat
/// B" and "play this clip from beat A".
/// </summary>
/// <remarks>
/// <para>
/// <b>The party is one object.</b> The line is a list of heroes, front first.
/// Every position comes from the room's anchor points (<see cref="RoomLayout"/>)
/// and the hero's place in the line, so heroes can never overlap: two heroes
/// never share a place.
/// </para>
/// <para>
/// <b>Travelling</b> uses one route for the whole party. It starts at the back
/// of the line, runs through each member to the leader, then on through the
/// door or portal to the next room. Every member moves the same distance along
/// it in the same beats: same pace, same spacing, single file.
/// </para>
/// <para>
/// <b>Every step</b> ends exactly when the simulation says, because each
/// instruction is given the step's start beat and length.
/// </para>
/// </remarks>
public sealed class Choreographer(Run run, RoomLayout layout, Func<int, HeroView> heroView, Func<int, ScriptedAnimator?> encounter)
{
    private readonly Dictionary<int, PartyView> _parties = [];

    public IEnumerable<PartyView> Parties => _parties.Values;

    public void Handle(GameEvent gameEvent)
    {
        switch (gameEvent)
        {
            case PartySpawned spawned: Spawn(spawned); break;
            case PartyDeparted departed: Depart(departed); break;
            case PartyArrived arrived: Arrive(arrived); break;
            case PartyWaiting waiting: AllPlay(Party(waiting.PartyId), "Idle", waiting.Beat); break;
            case PartyReordered reordered: Reorder(reordered); break;
            case RoomCleared cleared: Party(cleared.PartyId).LastSucceeded = true; break;
            case HeroDied died:
                PartyView party = Party(died.PartyId);
                party.LastSucceeded = false;
                party.FallenHeroId = died.HeroId;
                break;
            case PartyStepStarted step: Step(step); break;
            case PartyWiped wiped: _parties.Remove(wiped.PartyId); break;
        }
    }

    private void Spawn(PartySpawned spawned)
    {
        Room entrance = run.Dungeon.Entrance;
        var party = new PartyView(spawned.PartyId) { Line = [.. spawned.HeroIds], RoomId = entrance.Id };
        _parties[party.Id] = party;

        for (int i = 0; i < party.Line.Count; i++)
        {
            HeroView hero = heroView(party.Line[i]);
            hero.Visible = true;
            hero.PlaceAt(layout.LineSlot(entrance, i));
            hero.Animator.Play("Idle", spawned.Beat);
        }
    }

    private void Depart(PartyDeparted departed)
    {
        PartyView party = Party(departed.PartyId);
        Room from = run.Dungeon.RoomById(departed.FromRoomId)!;
        Room to = run.Dungeon.RoomById(departed.ToRoomId)!;
        party.RoomId = to.Id;

        // Build the shared route: back of the line → ... → leader → out → into the next room's line.
        var positions = party.Line.Select(id => heroView(id).RestingPosition).ToList();
        var path = new MovementPath(positions[^1]);
        var startDistance = new float[positions.Count];
        for (int i = positions.Count - 2; i >= 0; i--)
        {
            path.WalkTo(positions[i]);
            startDistance[i] = path.WalkedLength;
        }
        float leaderStart = path.WalkedLength;

        if (departed.Connection == ConnectionKind.Hallway)
        {
            path.WalkTo(RoomLayout.ExitEdge(from)).WalkTo(layout.LineSlot(to, 0));
        }
        else
        {
            path.WalkTo(layout.ExitPortal(from)).JumpTo(layout.EntryPortal(to)).WalkTo(layout.LineSlot(to, 0));
        }

        // Everyone advances the same distance: the leader ends at the front of the new line.
        float shift = path.WalkedLength - leaderStart;
        for (int i = 0; i < party.Line.Count; i++)
        {
            HeroView hero = heroView(party.Line[i]);
            hero.Move(path, startDistance[i], startDistance[i] + shift, departed.Beat, departed.ArriveBeat);
            hero.Animator.Play("Walk", departed.Beat);
        }
    }

    private void Arrive(PartyArrived arrived)
    {
        if (!_parties.TryGetValue(arrived.PartyId, out PartyView? party)) return;
        Room room = run.Dungeon.RoomById(arrived.RoomId)!;

        // Settle exactly into line slots (also catches anyone still behind a portal in a long line).
        for (int i = 0; i < party.Line.Count; i++)
        {
            HeroView hero = heroView(party.Line[i]);
            hero.PlaceAt(layout.LineSlot(room, i));
            hero.Animator.Play("Idle", arrived.Beat);
        }
        encounter(room.Id)?.Play("Idle", arrived.Beat); // the room resets for every party
    }

    // The banner is fully down: swap places instantly, unseen.
    private void Reorder(PartyReordered reordered)
    {
        PartyView party = Party(reordered.PartyId);
        Room room = run.Dungeon.RoomById(reordered.RoomId)!;
        party.Line = [.. reordered.HeroIds];
        for (int i = 0; i < party.Line.Count; i++)
        {
            heroView(party.Line[i]).PlaceAt(layout.LineSlot(room, i));
        }
    }

    private void Step(PartyStepStarted step)
    {
        PartyView party = Party(step.PartyId);
        Room room = run.Dungeon.RoomById(step.RoomId)!;
        double start = step.Beat;
        double end = step.Beat + step.Duration;
        ScriptedAnimator? encounterAnim = encounter(room.Id);
        HeroView front = heroView(party.Line[0]);

        switch (step.Step)
        {
            case PartyPhase.ArrivePause:
                AllPlay(party, "Idle", start);
                break;

            case PartyPhase.BannerDown or PartyPhase.BannerHold or PartyPhase.BannerUp:
                party.Banner = new BannerState(step.Step, start, step.Duration);
                break;

            case PartyPhase.Entrance:
                party.Banner = null;
                AllPlay(party, "Idle", start);
                front.MoveTo(layout.HeroSpot(room), start, end);
                front.Animator.Play("EncounterEntrance", start);
                encounterAnim?.Play("HeroEnter", start);
                break;

            case PartyPhase.Ready:
                AllPlay(party, "Idle", start);
                front.Animator.Play("Ready", start);
                encounterAnim?.Play("Ready", start);
                break;

            case PartyPhase.Action:
                front.Animator.Play(layout.ActionClip(room.Kind.Category), start);
                encounterAnim?.Play("Interact", start);
                break;

            case PartyPhase.Result:
                bool won = party.LastSucceeded;
                AllPlay(party, won ? "VictoryReact" : "DeathReact", start);
                front.Animator.Play(won ? "Victory" : "Death", start);
                encounterAnim?.Play(won ? "Defeat" : "Victory", start);
                break;

            case PartyPhase.Recover:
                if (party.FallenHeroId is { } fallen)
                {
                    heroView(fallen).Visible = false; // gone; the same hero returns with a later party
                    party.Line.Remove(fallen);
                    party.FallenHeroId = null;
                    encounterAnim?.Play("Idle", start);
                }
                foreach (int id in party.Line.Skip(party.LastSucceeded ? 1 : 0))
                {
                    heroView(id).Animator.Play("Idle", start); // a victor keeps celebrating
                }
                break;

            case PartyPhase.StepUp:
                // The whole line moves up one space; the new front hero carries on to the hero spot.
                front.MoveTo(layout.HeroSpot(room), start, end);
                front.Animator.Play("EncounterEntrance", start);
                for (int i = 1; i < party.Line.Count; i++)
                {
                    HeroView hero = heroView(party.Line[i]);
                    hero.MoveTo(layout.LineSlot(room, i), start, end);
                    hero.Animator.Play("Walk", start);
                }
                encounterAnim?.Play("HeroEnter", start);
                break;

            case PartyPhase.CatchUp:
                // Everyone walks up behind the victor, who keeps celebrating.
                for (int i = 1; i < party.Line.Count; i++)
                {
                    HeroView hero = heroView(party.Line[i]);
                    hero.MoveTo(layout.BehindHeroSpot(room, i), start, end);
                    hero.Animator.Play("Walk", start);
                }
                break;
        }
    }

    private void AllPlay(PartyView party, string clip, double beat)
    {
        foreach (int id in party.Line)
        {
            heroView(id).Animator.Play(clip, beat);
        }
    }

    private PartyView Party(int id) => _parties[id];
}
