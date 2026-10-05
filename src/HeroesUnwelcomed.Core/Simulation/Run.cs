using HeroesUnwelcomed.Core.Definitions;
using HeroesUnwelcomed.Core.Dungeons;
using HeroesUnwelcomed.Core.Heroes;
using HeroesUnwelcomed.Core.Randomness;
using HeroesUnwelcomed.Core.Rules;

namespace HeroesUnwelcomed.Core.Simulation;

/// <summary>
/// One play-through: the dungeon, the heroes, the parties inside, souls, and
/// the complete history. This is the whole game's logic, with no graphics.
/// </summary>
/// <remarks>
/// <para>
/// <b>Time.</b> The run moves forward in fixed steps ("ticks"), 8 per game beat.
/// <see cref="AdvanceTo"/> is called with the current game beat from the
/// audio clock and runs however many ticks are due. Fixed steps mean the same
/// seed and the same commands always produce exactly the same run.
/// </para>
/// <para>
/// <b>Each tick:</b> respawn heroes whose time is up, let the spawn rule send
/// a party, step every active party (oldest first), then finish locking the
/// door if the dungeon has emptied.
/// </para>
/// <para>
/// <b>A party's life:</b> spawn at the entrance → travel to the next room →
/// huddle → reorder → attempt, one hero at a time. A success clears the room
/// for everyone and they move on. A failure kills that hero (souls for the
/// player) and the next one tries. If the next room is held by a party ahead,
/// they wait. Clearing the treasure room ends the run.
/// </para>
/// <para>
/// <b>Holding rooms.</b> A party holds the room it's at or traveling to (its
/// <see cref="Party.RoomIndex"/>). No other party may enter a held room, so
/// parties never overlap and later ones queue behind.
/// </para>
/// </remarks>
public sealed class Run
{
    public const int TicksPerBeat = 8;
    private const double Epsilon = 1e-9;

    private readonly List<GameEvent> _events = [];
    private readonly List<RecordedCommand> _commands = [];
    private readonly List<Party> _parties = [];
    private readonly DeterministicRandom _challengeRandom;
    private readonly DeterministicRandom _partyRandom;
    private long _tick;
    private int _nextPartyId = 1;

    private Run(GameDefinitions definitions, RunSetup setup, RunRules rules, Dungeon dungeon, HeroPool heroes, DeterministicRandom random)
    {
        Definitions = definitions;
        Setup = setup;
        Rules = rules;
        Dungeon = dungeon;
        Heroes = heroes;
        _challengeRandom = random.Fork("challenge");
        _partyRandom = random.Fork("party-selection");
    }

    /// <summary>Builds a run from its definitions and starting setup.</summary>
    /// <param name="rules">Leave out to use the rules named in the setup.</param>
    public static Run Create(GameDefinitions definitions, RunSetup setup, RunRules? rules = null)
    {
        ValidateLayout(definitions, setup);

        var random = new DeterministicRandom(setup.Seed);
        var dungeon = new Dungeon(setup.GridWidth, setup.GridHeight);
        for (int i = 0; i < setup.Rooms.Count; i++)
        {
            RoomPlacement placement = setup.Rooms[i];
            dungeon.Insert(i, definitions.Room(placement.Kind), placement.Position, placement.Facing);
        }

        HeroPool heroes = HeroPool.Generate(setup.HeroPoolSize, definitions.Heroes, random.Fork("heroes"));
        var run = new Run(definitions, setup, rules ?? RunRules.FromSetup(setup, definitions.Tuning), dungeon, heroes, random);

        if (setup.StartingSouls > 0)
        {
            run.Souls.Add(new SoulTransaction(0, setup.StartingSouls, "Starting souls", null));
        }
        return run;
    }

    public GameDefinitions Definitions { get; }
    public RunSetup Setup { get; }
    public RunRules Rules { get; }
    public Dungeon Dungeon { get; }
    public HeroPool Heroes { get; }
    public HeroCodex Codex { get; } = new();
    public SoulLedger Souls { get; } = new();
    public DoorState Door { get; private set; } = DoorState.Open;

    /// <summary>Current game beat.</summary>
    public double Beat => _tick / (double)TicksPerBeat;

    /// <summary>Set when the run ends.</summary>
    public RunEnded? Outcome { get; private set; }
    public bool IsOver => Outcome is not null;

    /// <summary>Everything that has happened, in order.</summary>
    public IReadOnlyList<GameEvent> Events => _events;

    /// <summary>Every accepted command with its beat: enough to replay the run from its seed.</summary>
    public IReadOnlyList<RecordedCommand> Commands => _commands;

    public IReadOnlyList<Party> AllParties => _parties;
    public IEnumerable<Party> ActiveParties => _parties.Where(p => p.IsActive);

    /// <summary>Raised as each event happens (visuals, audio and metrics subscribe here).</summary>
    public event Action<GameEvent>? EventRaised;

    /// <summary>True if a party could enter right now.</summary>
    public bool CanSpawnParty => !IsOver && Door == DoorState.Open && PartyHolding(0) is null && Heroes.Waiting.Any();

    /// <summary>The active party holding the room at <paramref name="roomIndex"/>, if any.</summary>
    public Party? PartyHolding(int roomIndex, Party? except = null) =>
        _parties.FirstOrDefault(p => p.IsActive && p != except && p.RoomIndex == roomIndex);

    /// <summary>Runs every tick up to <paramref name="targetBeat"/>. Safe to call every frame.</summary>
    public void AdvanceTo(double targetBeat)
    {
        long targetTick = (long)Math.Floor(targetBeat * TicksPerBeat + Epsilon);
        while (_tick < targetTick && !IsOver)
        {
            _tick++;
            Tick();
        }
    }

    /// <summary>Applies a player command at the current beat, or refuses it with a reason.</summary>
    public CommandResult Execute(GameCommand command)
    {
        if (IsOver)
        {
            return CommandResult.Refused("The run is over.");
        }

        CommandResult result = command switch
        {
            PlaceRoom place => Place(place),
            UpgradeRoom upgrade => Upgrade(upgrade),
            FlipRoom flip => Flip(flip),
            LockDoor => Lock(),
            UnlockDoor => Unlock(),
            SendNextParty => SendParty(),
            _ => CommandResult.Refused($"Unknown command {command.GetType().Name}."),
        };

        if (result.Accepted)
        {
            _commands.Add(new RecordedCommand(Beat, command));
        }
        return result;
    }

    // ---- The tick ---------------------------------------------------------

    private void Tick()
    {
        foreach (Hero hero in Heroes.All.Where(h => h.State == HeroState.Dead && h.RespawnAtBeat <= Beat + Epsilon))
        {
            hero.State = HeroState.Waiting;
            Raise(new HeroRespawned(Beat, hero.Id));
        }

        RustAndReviewClasses();

        if (Door == DoorState.Open && Rules.Spawn.ShouldSpawn(this))
        {
            SpawnParty();
        }

        // Oldest party first, so the one in front always moves first.
        foreach (Party party in _parties.Where(p => p.IsActive).ToList())
        {
            Step(party);
            if (IsOver)
            {
                return;
            }
        }

        if (Door == DoorState.Locking && !ActiveParties.Any())
        {
            SetDoor(DoorState.Locked);
        }
    }

    // Moves a party on to its next choreography step once the current one is over.
    private void Step(Party party)
    {
        if (party.Phase == PartyPhase.Waiting)
        {
            Depart(party); // try again every tick
            return;
        }

        if (Beat < party.PhaseEndsAt - Epsilon)
        {
            return;
        }

        switch (party.Phase)
        {
            case PartyPhase.Traveling: Arrive(party); break;
            case PartyPhase.ArrivePause: Begin(party, PartyPhase.BannerDown); break;
            case PartyPhase.BannerDown: Reorder(party); Begin(party, PartyPhase.BannerHold); break;
            case PartyPhase.BannerHold: Begin(party, PartyPhase.BannerUp); break;
            case PartyPhase.BannerUp: StartAttempt(party, PartyPhase.Entrance); break;
            case PartyPhase.Entrance: Begin(party, PartyPhase.Ready); break;
            case PartyPhase.StepUp: Begin(party, PartyPhase.Ready); break;
            case PartyPhase.Ready: Begin(party, PartyPhase.Action); break;
            case PartyPhase.Action: Resolve(party); break;
            case PartyPhase.Result: Begin(party, PartyPhase.Recover); break;
            case PartyPhase.Recover: AfterRecover(party); break;
            case PartyPhase.CatchUp: Depart(party); break;
        }
    }

    /// <summary>
    /// Starts a choreography step: sets its end beat from the tuning and tells
    /// the world (visuals animate exactly this long). The optional debug gap
    /// adds stillness after the step without changing the step itself.
    /// </summary>
    private void Begin(Party party, PartyPhase step)
    {
        double duration = StepBeats(step);
        party.Phase = step;
        party.PhaseEndsAt = Beat + duration + Definitions.Tuning.DebugGapBeats;
        Raise(new PartyStepStarted(Beat, party.Id, Dungeon.Rooms[party.RoomIndex].Id, step, duration));
    }

    private double StepBeats(PartyPhase step)
    {
        Tuning t = Definitions.Tuning;
        return step switch
        {
            PartyPhase.ArrivePause => t.ArrivePauseBeats,
            PartyPhase.BannerDown => t.BannerDownBeats,
            PartyPhase.BannerHold => t.BannerHoldBeats,
            PartyPhase.BannerUp => t.BannerUpBeats,
            PartyPhase.Entrance => t.EntranceBeats,
            PartyPhase.Ready => t.ReadyBeats,
            PartyPhase.Action => t.ActionBeats,
            PartyPhase.Result => t.ResultBeats,
            PartyPhase.Recover => t.RecoverBeats,
            PartyPhase.StepUp => t.StepUpBeats,
            PartyPhase.CatchUp => t.CatchUpBeats,
            _ => 0,
        };
    }

    // ---- A party's life ---------------------------------------------------

    private void SpawnParty()
    {
        var waiting = Heroes.Waiting.ToList();
        int size = Math.Min(Setup.PartySize, waiting.Count);

        // Random members (seeded), so different heroes end up adventuring together.
        var members = new List<Hero>(size);
        for (int i = 0; i < size; i++)
        {
            int pick = _partyRandom.Next(waiting.Count);
            members.Add(waiting[pick]);
            waiting.RemoveAt(pick);
        }

        var party = new Party(_nextPartyId++, members) { RoomIndex = 0 };
        foreach (Hero hero in members)
        {
            hero.State = HeroState.InParty;
        }
        _parties.Add(party);

        Raise(new PartySpawned(Beat, party.Id, members.Select(h => h.Id).ToList()));
        Depart(party);
    }

    private void Depart(Party party)
    {
        int next = party.RoomIndex + 1;
        Room from = Dungeon.Rooms[party.RoomIndex];

        if (PartyHolding(next, except: party) is { } blocker)
        {
            party.Phase = PartyPhase.Waiting;
            if (!party.ReportedWaiting)
            {
                party.ReportedWaiting = true;
                Raise(new PartyWaiting(Beat, party.Id, from.Id, blocker.Id));
            }
            return;
        }

        ConnectionKind connection = Dungeon.ConnectionAfter(party.RoomIndex);
        double travel = connection == ConnectionKind.Hallway ? Definitions.Tuning.HallwayBeats : Definitions.Tuning.PortalBeats;

        party.RoomIndex = next;
        party.Phase = PartyPhase.Traveling;
        party.PhaseEndsAt = Beat + travel;
        party.ReportedWaiting = false;

        Raise(new PartyDeparted(Beat, party.Id, from.Id, Dungeon.Rooms[next].Id, connection, party.PhaseEndsAt));
    }

    private void Arrive(Party party)
    {
        Room room = Dungeon.Rooms[party.RoomIndex];
        Raise(new PartyArrived(Beat, party.Id, room.Id));

        if (!room.Kind.IsChallenge)
        {
            Depart(party);
            return;
        }
        Begin(party, PartyPhase.ArrivePause);
    }

    // Happens the instant the banner is fully down: the order swaps unseen.
    private void Reorder(Party party)
    {
        Room room = Dungeon.Rooms[party.RoomIndex];
        party.Members = Rules.Ordering.Order(party.Members, room.Kind, Codex, Definitions).ToList();
        Raise(new PartyReordered(Beat, party.Id, room.Id, party.Members.Select(h => h.Id).ToList()));
    }

    // The front hero heads in: Entrance after the banner, or Step Up after a teammate died.
    private void StartAttempt(Party party, PartyPhase movement)
    {
        Raise(new AttemptStarted(Beat, party.Id, party.Members[0].Id, Dungeon.Rooms[party.RoomIndex].Id));
        Begin(party, movement);
    }

    // End of the Action step: roll the dice, then show the result.
    private void Resolve(Party party)
    {
        Room room = Dungeon.Rooms[party.RoomIndex];
        Hero hero = party.Members[0];

        ChallengeResult result = Rules.Challenge.Resolve(hero, room, _challengeRandom);
        Codex.RecordAttempt(hero.Id, room.Kind.Id, result.Success);
        Rules.Growth.Apply(hero, room, result.Success);
        Rules.Ordering.Record(hero, room.Kind, result.Success, Codex);
        hero.MarkPractised(room.Kind.Category, Beat);
        ReviewClass(hero);
        party.LastAttemptSucceeded = result.Success;

        if (result.Success)
        {
            hero.RoomsCleared++;
            var cleared = new RoomCleared(Beat, party.Id, hero.Id, room.Id, room.Kind.Id, result.Chance);
            Raise(cleared);
            Begin(party, PartyPhase.Result);

            if (Rules.RunEnd.EndsRun(this, cleared))
            {
                FinishParty(party);
                Outcome = new RunEnded(Beat, hero.Id, party.Id);
                Raise(Outcome);
            }
            return;
        }

        party.Members.RemoveAt(0);
        hero.State = HeroState.Dead;
        hero.Deaths++;
        hero.RespawnAtBeat = Beat + Definitions.Tuning.RespawnBeats;
        Raise(new HeroDied(Beat, party.Id, hero.Id, room.Id, room.Kind.Id, result.Chance, hero.RespawnAtBeat));

        if (room.Kind.Souls > 0)
        {
            ChangeSouls(room.Kind.Souls, $"{hero.Name} died at {room.Kind.Id}", room.Id);
        }
        Begin(party, PartyPhase.Result);
    }

    // After the result and a beat of stillness: catch up to the victor, step up for the next try, or end.
    private void AfterRecover(Party party)
    {
        Room room = Dungeon.Rooms[party.RoomIndex];

        if (party.LastAttemptSucceeded)
        {
            if (Dungeon.IsTreasure(party.RoomIndex))
            {
                FinishParty(party); // reached the end without ending the run (only with other end rules)
            }
            else
            {
                Begin(party, PartyPhase.CatchUp);
            }
            return;
        }

        if (party.Members.Count == 0)
        {
            party.Phase = PartyPhase.Finished;
            Raise(new PartyWiped(Beat, party.Id, room.Id));
            return;
        }

        StartAttempt(party, PartyPhase.StepUp);
    }

    // ---- Specialization -----------------------------------------------------

    // Every tick: skills a hero hasn't practised for a while slowly rust; then
    // anyone whose skills crossed a class threshold either way gets reviewed.
    private void RustAndReviewClasses()
    {
        Tuning tuning = Definitions.Tuning;
        double rustPerTick = tuning.SkillRustPerBeat / TicksPerBeat;

        foreach (Hero hero in Heroes.All)
        {
            if (rustPerTick > 0)
            {
                foreach (RoomCategory category in tuning.SkillRustCategories)
                {
                    if (Beat - hero.LastPractised(category) > tuning.SkillRustGraceBeats)
                    {
                        hero.Rust(category, rustPerTick);
                    }
                }
            }
            ReviewClass(hero);
        }
    }

    /// <summary>
    /// One class at a time. Below the threshold (base skill) → lose it. With no
    /// class → take the first class (in data order) whose threshold is met.
    /// </summary>
    private void ReviewClass(Hero hero)
    {
        if (hero.Class is { } current && hero.BaseSkill(current.Category) < current.Threshold)
        {
            hero.Class = null;
            Raise(new HeroLostSpecialization(Beat, hero.Id, current.Id));
        }

        if (hero.Class is null && Definitions.Classes.FirstOrDefault(c => hero.BaseSkill(c.Category) >= c.Threshold) is { } earned)
        {
            hero.Class = earned;
            Raise(new HeroSpecialized(Beat, hero.Id, earned.Id));
        }
    }

    // Survivors of a party that finished without being wiped go back to the pool.
    private void FinishParty(Party party)
    {
        party.Phase = PartyPhase.Finished;
        foreach (Hero hero in party.Members)
        {
            hero.State = HeroState.Waiting;
        }
    }

    // ---- Commands ---------------------------------------------------------

    private CommandResult Place(PlaceRoom command)
    {
        if (Door != DoorState.Locked) return CommandResult.Refused("Lock the door before building.");
        if (!Definitions.HasRoom(command.Kind)) return CommandResult.Refused($"There's no room called '{command.Kind}'.");

        RoomKind kind = Definitions.Room(command.Kind);
        if (!kind.Buildable) return CommandResult.Refused($"{kind.Id} can't be built.");
        if (Dungeon.WhyCantPlace(command.Position) is { } why) return CommandResult.Refused(why);
        if (!Souls.CanAfford(kind.PlaceCost)) return CommandResult.Refused($"{kind.Id} costs {kind.PlaceCost} souls; you have {Souls.Balance}.");

        // The entrance must stay first and the treasure room last.
        int index = Math.Clamp(Rules.RoomOrder.InsertIndex(Dungeon, command.Position), 1, Dungeon.Rooms.Count - 1);
        Room room = Dungeon.Insert(index, kind, command.Position, command.Facing);

        if (kind.PlaceCost > 0) ChangeSouls(-kind.PlaceCost, $"Built {kind.Id}", room.Id);
        Raise(new RoomPlaced(Beat, room.Id, kind.Id, index));
        return CommandResult.Ok;
    }

    private CommandResult Upgrade(UpgradeRoom command)
    {
        if (Door != DoorState.Locked) return CommandResult.Refused("Lock the door before upgrading.");
        if (Dungeon.RoomById(command.RoomId) is not { } room) return CommandResult.Refused($"No room #{command.RoomId}.");
        if (!room.Kind.IsChallenge) return CommandResult.Refused($"{room.Kind.Id} can't be upgraded.");
        if (room.Level >= room.Kind.MaxLevel) return CommandResult.Refused($"{room.Kind.Id} is already at its maximum level ({room.Kind.MaxLevel}).");

        int cost = room.Kind.UpgradeCostFrom(room.Level);
        if (!Souls.CanAfford(cost)) return CommandResult.Refused($"Upgrading costs {cost} souls; you have {Souls.Balance}.");

        room.Level++;
        if (cost > 0) ChangeSouls(-cost, $"Upgraded {room.Kind.Id} to level {room.Level}", room.Id);
        Raise(new RoomUpgraded(Beat, room.Id, room.Level));
        return CommandResult.Ok;
    }

    private CommandResult Flip(FlipRoom command)
    {
        if (Door != DoorState.Locked) return CommandResult.Refused("Lock the door before flipping rooms.");
        if (Dungeon.RoomById(command.RoomId) is not { } room) return CommandResult.Refused($"No room #{command.RoomId}.");

        room.Facing = room.Facing == Facing.Right ? Facing.Left : Facing.Right;
        Raise(new RoomFlipped(Beat, room.Id));
        return CommandResult.Ok;
    }

    private CommandResult Lock()
    {
        if (Door != DoorState.Open) return CommandResult.Refused($"The door is already {Door.ToString().ToLowerInvariant()}.");
        SetDoor(ActiveParties.Any() ? DoorState.Locking : DoorState.Locked);
        return CommandResult.Ok;
    }

    private CommandResult Unlock()
    {
        if (Door == DoorState.Open) return CommandResult.Refused("The door is already open.");
        SetDoor(DoorState.Open);
        return CommandResult.Ok;
    }

    private CommandResult SendParty()
    {
        if (Door != DoorState.Open) return CommandResult.Refused("The door is locked.");
        if (PartyHolding(0) is not null) return CommandResult.Refused("A party is still at the entrance.");
        if (!Heroes.Waiting.Any()) return CommandResult.Refused("No heroes are ready.");
        SpawnParty();
        return CommandResult.Ok;
    }

    // ---- Helpers ----------------------------------------------------------

    private void SetDoor(DoorState state)
    {
        Door = state;
        Raise(new DoorStateChanged(Beat, state));
    }

    private void ChangeSouls(int amount, string reason, int? roomId)
    {
        Souls.Add(new SoulTransaction(Beat, amount, reason, roomId));
        Raise(new SoulsChanged(Beat, amount, Souls.Balance, reason, roomId));
    }

    private void Raise(GameEvent gameEvent)
    {
        _events.Add(gameEvent);
        EventRaised?.Invoke(gameEvent);
    }

    private static void ValidateLayout(GameDefinitions definitions, RunSetup setup)
    {
        if (setup.Rooms.Count < 2)
        {
            throw new InvalidDataException("A dungeon needs at least an entrance and a treasure room.");
        }
        if (definitions.Room(setup.Rooms[0].Kind).Category != RoomCategory.Entrance)
        {
            throw new InvalidDataException("The first room must be an Entrance.");
        }
        if (definitions.Room(setup.Rooms[^1].Kind).Category != RoomCategory.Boss)
        {
            throw new InvalidDataException("The last room must be the treasure (Boss) room.");
        }
        if (setup.PartySize < 1 || setup.HeroPoolSize < 1)
        {
            throw new InvalidDataException("Party size and hero pool size must be at least 1.");
        }
    }
}
