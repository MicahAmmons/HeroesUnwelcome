using HeroesUnwelcomed.Game.Animation;
using HeroesUnwelcomed.Game.Sound;

namespace HeroesUnwelcomed.Game.Tests.Animation;

public class AnimationPlayerTests
{
    private const string Json = """
    {
      "entities": {
        "Hero": {
          "color": "#4A86E8",
          "size": [180, 260],
          "clips": {
            "Idle":   { "frames": 3, "beatsPerFrame": 1,   "loop": true,  "clock": "music" },
            "Walk":   { "frames": 3, "beatsPerFrame": 0.5, "loop": true,  "clock": "sim" },
            "Attack": { "frames": 3, "beatsPerFrame": 0.5, "loop": false, "clock": "sim", "then": "Idle" },
            "Death":  { "frames": 3, "beatsPerFrame": 1,   "loop": false, "clock": "sim" },
            "Fear":   { "frames": 3, "beatsPerFrame": 1,   "loop": false, "clock": "music", "then": "Idle",
                        "rate": [[0, 0], [2, 0], [2.01, 1]] }
          }
        }
      }
    }
    """;

    private static AnimationPlayer NewHero() => new(AnimationLibrary.Parse(Json).Entity("Hero"));

    // Advances music and game time together by `beats`, in small steps like real frames.
    private static double Advance(AnimationPlayer player, double fromBeat, double beats, bool paused = false, double step = 0.05)
    {
        double beat = fromBeat;
        double simBeat = paused ? 0 : fromBeat;
        for (double done = 0; done < beats - 1e-9; done += step)
        {
            beat += step;
            player.Update(new BeatTime(beat, step, simBeat, paused ? 0 : step, paused, 4));
        }
        return beat;
    }

    [Fact]
    public void MusicLoop_FrameFollowsTheGlobalBeat()
    {
        var player = NewHero();

        player.Update(new BeatTime(MusicBeat: 7.5, 0, 7.5, 0, false, 4));

        Assert.Equal("Idle", player.DisplayClip.Name);
        Assert.Equal(1, player.Frame); // beat 7 → frame 7 mod 3 = 1
    }

    [Fact]
    public void OneShot_ReturnsToItsThenClipWhenFinished()
    {
        var player = NewHero();
        player.Play("Attack"); // 3 frames × 0.5 beats = 1.5 beats

        Advance(player, 0, 1.0);
        Assert.Equal("Attack", player.Clip.Name);

        Advance(player, 1.0, 0.6);
        Assert.Equal("Idle", player.Clip.Name);
    }

    [Fact]
    public void OneShotWithoutThen_HoldsLastFrame()
    {
        var player = NewHero();
        player.Play("Death");

        Advance(player, 0, 10);

        Assert.Equal("Death", player.Clip.Name);
        Assert.Equal(2, player.Frame);
    }

    [Fact]
    public void Pause_ShowsIdle_AndSimClipResumesWhereItWas()
    {
        var player = NewHero();
        player.Play("Attack");
        Advance(player, 0, 0.75); // halfway: frame 1
        Assert.Equal(1, player.Frame);

        Advance(player, 0.75, 20, paused: true);
        Assert.Equal("Attack", player.Clip.Name);     // still logically attacking
        Assert.Equal("Idle", player.DisplayClip.Name); // but shown idling

        // Resume: the update carries a tiny game-time step, so still frame 1.
        player.Update(new BeatTime(20.80, 0.05, 0.80, 0.05, false, 4));
        Assert.Equal("Attack", player.DisplayClip.Name);
        Assert.Equal(1, player.Frame);
    }

    [Fact]
    public void RateCurve_FreezesThenPlays()
    {
        var player = NewHero();
        player.Play("Fear");

        Advance(player, 0, 1.9);
        Assert.Equal(0, player.Frame); // frozen for the first 2 beats
        Assert.Equal(0, player.Progress, 3);

        Advance(player, 1.9, 1.6);
        Assert.Equal(1, player.Frame); // ~1.5 beats of progress at full speed
    }

    [Fact]
    public void Rebind_KeepsCurrentClipIfItStillExists()
    {
        var player = NewHero();
        player.Play("Walk");

        player.Rebind(AnimationLibrary.Parse(Json).Entity("Hero"));

        Assert.Equal("Walk", player.Clip.Name);
    }
}
