using HeroesUnwelcomed.Audio.Mixing;
using HeroesUnwelcomed.Audio.Music;

namespace HeroesUnwelcomed.Audio.Tests.Mixing;

public class AugmentTests
{
    private const int Rate = 48_000;

    private static readonly AugmentDefinition CutDrums = new(
        new Dictionary<string, float> { ["drums"] = 1f, ["lead"] = 0.5f },
        StartGrid: 0.5, HoldBeats: 2, ReturnOnBar: true, ReleaseBeats: 0.5);

    [Fact]
    public void Reaction_StartsOnTheGrid_AndReturnsOnABarLine()
    {
        var augments = new AugmentController();
        augments.SetInfluence(1, 1f);

        ActiveAugment placed = augments.Add(1, CutDrums, earliestBeat: 9.2, beatsPerBar: 4);

        Assert.Equal(9.5, placed.StartBeat);    // next half-beat
        Assert.Equal(12, placed.ReturnBeat);    // hold 2 → 11.5 → next bar line is 12
    }

    [Fact]
    public void Multipliers_FollowTheEnvelope()
    {
        var augments = new AugmentController();
        augments.SetInfluence(1, 1f);
        augments.Add(1, CutDrums, 9.5, 4);

        Assert.Empty(augments.Multipliers(9.4));                     // not yet
        Assert.Equal(0f, augments.Multipliers(10)["drums"]);        // fully cut
        Assert.Equal(0.5f, augments.Multipliers(10)["lead"]);       // half cut
        Assert.Equal(0.5f, augments.Multipliers(12.25)["drums"], 3); // halfway back during the release
        Assert.Empty(augments.Multipliers(13));                      // gone
        Assert.Empty(augments.Active);
    }

    [Fact]
    public void Influence_ScalesTheCut()
    {
        var augments = new AugmentController();
        augments.SetInfluence(1, 0.2f); // zoomed out / far away
        augments.Add(1, CutDrums, 0, 4);

        Assert.Equal(0.8f, augments.Multipliers(1)["drums"], 3);
    }

    [Fact]
    public void StrongestRequestWins_TheyDontAddUp()
    {
        var augments = new AugmentController();
        augments.SetInfluence(1, 0.6f);
        augments.SetInfluence(2, 0.3f);
        augments.Add(1, CutDrums, 0, 4);
        augments.Add(2, CutDrums, 0, 4);

        Assert.Equal(0.4f, augments.Multipliers(1)["drums"], 3); // 1 − max(0.6, 0.3), not 1 − 0.9
    }

    [Fact]
    public void GlobalMode_IgnoresPartyReactions()
    {
        var augments = new AugmentController { Enabled = false };
        augments.SetInfluence(1, 1f);
        augments.Add(1, CutDrums, 0, 4);

        Assert.Empty(augments.Multipliers(1));
    }

    [Fact]
    public void Mixer_CutsTheStemOnTheBeat()
    {
        var version = new MusicVersion("a", 120, 4, 4, Rate, new Dictionary<string, float[]>
        {
            ["drums"] = Enumerable.Repeat(0.5f, 16 * 24_000).ToArray(),
        });
        var mixer = new MusicMixer(new MusicTrack("t", new Dictionary<string, MusicVersion> { ["a"] = version }), "a", 1) { MasterGain = 1f };
        mixer.Augments.SetInfluence(1, 1f);
        mixer.Augments.Add(1, new AugmentDefinition(new Dictionary<string, float> { ["drums"] = 1f }, StartGrid: 1, HoldBeats: 4), 1.5, 4);

        var output = new float[5 * 24_000 * 2];
        mixer.Render(output);

        // Beat 2 = frame 48,000. Full volume just before; silent ~40 ms after (8 ms glide, checked every 2.7 ms).
        Assert.True(output[(48_000 - 200) * 2] > 0.3f);
        Assert.True(Math.Abs(output[(48_000 + 2_000) * 2]) < 0.01f);
    }

    [Fact]
    public void Sound_StartsOnItsBeat_AndIsRemovedWhenDone()
    {
        var version = new MusicVersion("a", 120, 4, 1, Rate, new Dictionary<string, float[]> { ["x"] = new float[4 * 24_000] });
        var mixer = new MusicMixer(new MusicTrack("t", new Dictionary<string, MusicVersion> { ["a"] = version }), "a", 1) { MasterGain = 1f };
        float[] click = Enumerable.Repeat(0.5f, 100).ToArray();

        mixer.PlaySound(click, gain: 1f, pan: 0f, atBeat: 1);
        var output = new float[30_000 * 2];
        mixer.Render(output);

        Assert.Equal(0f, output[(24_000 - 1) * 2]);                      // silent before beat 1
        Assert.Equal(0.5f * MathF.Cos(MathF.PI / 4), output[24_000 * 2], 4); // plays from exactly beat 1
        Assert.Equal(0, mixer.ActiveSounds);                             // finished and cleaned up
    }

    [Fact]
    public void Sound_PanningPutsItOnOneSide()
    {
        var version = new MusicVersion("a", 120, 4, 1, Rate, new Dictionary<string, float[]> { ["x"] = new float[4 * 24_000] });
        var mixer = new MusicMixer(new MusicTrack("t", new Dictionary<string, MusicVersion> { ["a"] = version }), "a", 1) { MasterGain = 1f };

        mixer.PlaySound(Enumerable.Repeat(0.5f, 10).ToArray(), 1f, pan: -1f);
        var output = new float[20];
        mixer.Render(output);

        Assert.Equal(0.5f, output[0], 4);   // left
        Assert.Equal(0f, output[1], 4);     // right
    }

    [Fact]
    public void PlaceholderSounds_ExistAndAreAudible()
    {
        var sounds = PlaceholderSounds.Create(Rate, fillBpm: 100);

        Assert.Equal(["gasp", "hit", "cheer", "whoosh", "fill"], sounds.Keys);
        Assert.All(sounds.Values, s => Assert.True(s.Max(MathF.Abs) > 0.05f));
        Assert.Equal(Rate * 60 / 100, sounds["fill"].Length); // exactly one beat long
    }
}
