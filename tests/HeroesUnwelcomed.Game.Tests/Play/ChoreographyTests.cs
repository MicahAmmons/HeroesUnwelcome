using HeroesUnwelcomed.Core.Definitions;
using HeroesUnwelcomed.Core.Simulation;
using HeroesUnwelcomed.Game.Animation;
using HeroesUnwelcomed.Game.Play;
using HeroesUnwelcomed.Game.Sound;
using Microsoft.Xna.Framework;

namespace HeroesUnwelcomed.Game.Tests.Play;

public class ChoreographyTests
{
    private const string Layout = """
    {
      "default": { "lineFront": 0.44, "heroSpot": 0.667, "encounter": 0.833, "door": 0.5, "entryPortal": 0.05, "exitPortal": 0.94 },
      "spacing": 0.125,
      "actionClips": { "Combat": "Fight", "Trap": "Disarm", "Boss": "Fight" },
      "rooms": { "Spikes": { "heroSpot": 0.6 } }
    }
    """;

    private const string Animations = """
    { "entities": { "Hero": { "color": "#4A86E8", "size": [140, 260], "clips": { "Idle": {} } } } }
    """;

    // A hero is 140 × 1.6 = 224 world units wide: closer than that and two heroes overlap.
    private const float HeroWidth = 140 * WorldLayout.SpriteScale;

    private static GameDefinitions Definitions() => new(
        [
            new RoomKind("Entrance", RoomCategory.Entrance, Buildable: false),
            new RoomKind("Goblin", RoomCategory.Combat, Difficulty: 2.5, Souls: 3),
            new RoomKind("Spikes", RoomCategory.Trap, Difficulty: 2.5, Souls: 3),
            new RoomKind("Lich", RoomCategory.Boss, Difficulty: 4),
        ],
        new Tuning { RespawnBeats = 30 },
        new HeroGeneration
        {
            Names = ["A", "B", "C", "D", "E", "F", "G", "H"],
            StartingSkill = new Dictionary<RoomCategory, double> { [RoomCategory.Combat] = 2, [RoomCategory.Trap] = 2, [RoomCategory.Boss] = 1 },
        });

    // Entrance, goblin, (portal) spikes facing left on another row, then the lich.
    private static RunSetup Setup(ulong seed) => new()
    {
        Seed = seed,
        GridWidth = 6,
        GridHeight = 3,
        Rooms =
        [
            new RoomPlacement("Entrance", new GridPoint(0, 0)),
            new RoomPlacement("Goblin", new GridPoint(1, 0)),
            new RoomPlacement("Spikes", new GridPoint(4, 2), Facing.Left),
            new RoomPlacement("Goblin", new GridPoint(3, 2), Facing.Left),
            new RoomPlacement("Lich", new GridPoint(2, 2), Facing.Left),
        ],
        HeroPoolSize = 8,
        PartySize = 4,
        SpawnRule = "auto",
    };

    [Fact]
    public void Layout_LineStandsBehindTheFront_ForBothFacings()
    {
        RoomLayout layout = RoomLayout.Parse(Layout);
        Run run = Run.Create(Definitions(), Setup(1));
        var right = run.Dungeon.Rooms[1];   // faces right
        var left = run.Dungeon.Rooms[2];    // faces left

        Assert.True(layout.LineSlot(right, 1).X < layout.LineSlot(right, 0).X);
        Assert.True(layout.LineSlot(left, 1).X > layout.LineSlot(left, 0).X);
        Assert.Equal(240, Math.Abs(layout.LineSlot(right, 1).X - layout.LineSlot(right, 0).X), 3);
    }

    [Fact]
    public void Layout_RoomKindsCanOverrideAnchors()
    {
        RoomLayout layout = RoomLayout.Parse(Layout);

        Assert.Equal(0.6f, layout.For("Spikes").HeroSpot);
        Assert.Equal(0.667f, layout.For("Goblin").HeroSpot);
        Assert.Equal(0.44f, layout.For("Spikes").LineFront); // not overridden: from the defaults
        Assert.Equal("Disarm", layout.ActionClip(RoomCategory.Trap));
    }

    [Theory]
    [InlineData(1UL)]
    [InlineData(2UL)]
    [InlineData(3UL)]
    public void HeroesInAParty_NeverOverlap(ulong seed)
    {
        Run run = Run.Create(Definitions(), Setup(seed));
        RoomLayout layout = RoomLayout.Parse(Layout);
        EntityDefinition heroEntity = AnimationLibrary.Parse(Animations).Entity("Hero");
        var views = new Dictionary<int, HeroView>();
        HeroView ViewFor(int id) => views.TryGetValue(id, out var v) ? v : views[id] = new HeroView(id, $"H{id}", heroEntity);
        var choreographer = new Choreographer(run, layout, ViewFor, _ => null);
        run.EventRaised += choreographer.Handle;

        int checkedMoments = 0;
        for (double beat = 0; beat < 400 && !run.IsOver; beat += 1.0 / Run.TicksPerBeat)
        {
            run.AdvanceTo(beat);
            foreach (PartyView party in choreographer.Parties)
            {
                // While the banner is down the swap happens unseen, so only check visible moments.
                if (party.Banner is { Step: not PartyPhase.BannerUp }) continue;

                var positions = party.Line.Select(id => views[id]).Where(v => v.Visible).Select(v => v.PositionAt(beat)).ToList();
                for (int a = 0; a < positions.Count; a++)
                {
                    for (int b = a + 1; b < positions.Count; b++)
                    {
                        bool sameFloor = Math.Abs(positions[a].Y - positions[b].Y) < 1;
                        float gap = Math.Abs(positions[a].X - positions[b].X);
                        Assert.False(sameFloor && gap < HeroWidth - 0.5f,
                            $"Seed {seed}, beat {beat}: party {party.Id} heroes overlap ({gap:0} apart) at {positions[a]} and {positions[b]}.");
                    }
                }
                checkedMoments++;
            }
        }

        Assert.True(checkedMoments > 500);
    }

    [Fact]
    public void LineMovesTogether_SameStartAndEndForEveryone()
    {
        Run run = Run.Create(Definitions(), Setup(5));
        RoomLayout layout = RoomLayout.Parse(Layout);
        EntityDefinition heroEntity = AnimationLibrary.Parse(Animations).Entity("Hero");
        var views = new Dictionary<int, HeroView>();
        HeroView ViewFor(int id) => views.TryGetValue(id, out var v) ? v : views[id] = new HeroView(id, $"H{id}", heroEntity);
        var choreographer = new Choreographer(run, layout, ViewFor, _ => null);
        run.EventRaised += choreographer.Handle;

        run.AdvanceTo(0.125); // first party spawns and departs on the first tick
        PartyView party = choreographer.Parties.First();
        var starts = party.Line.Select(id => views[id].PositionAt(0)).ToList();
        PartyDeparted departed = run.Events.OfType<PartyDeparted>().First();
        var ends = party.Line.Select(id => views[id].PositionAt(departed.ArriveBeat)).ToList();

        // Same distance moved by everyone (same pace), and spacing unchanged at both ends.
        float moved = Vector2.Distance(starts[0], ends[0]);
        Assert.All(starts.Zip(ends), pair => Assert.Equal(moved, Vector2.Distance(pair.First, pair.Second), 1));
        for (int i = 1; i < ends.Count; i++)
        {
            Assert.Equal(240, Math.Abs(ends[i].X - ends[i - 1].X), 1);
        }
    }

    [Fact]
    public void ScriptedAnimator_FrameComesFromStartBeat_AndHoldsLastFrame()
    {
        EntityDefinition entity = AnimationLibrary.Parse("""
            { "entities": { "X": { "color": "#112233", "size": [10, 10], "clips": {
                "Idle": {}, "Fight": { "frames": 3, "beatsPerFrame": 0.5, "loop": false, "clock": "sim" } } } } }
            """).Entity("X");
        var animator = new ScriptedAnimator(entity);
        animator.Play("Fight", startBeat: 10);
        var time = new BeatTime(0, 0, 0, 0, false, 4);

        Assert.Equal(0, animator.FrameAt(10.2, time).Frame);
        Assert.Equal(1, animator.FrameAt(10.6, time).Frame);
        Assert.Equal(2, animator.FrameAt(99, time).Frame); // longer step than clip: hold the last frame
    }
}
