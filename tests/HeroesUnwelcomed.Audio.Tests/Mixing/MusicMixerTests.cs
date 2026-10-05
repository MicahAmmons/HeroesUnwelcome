using HeroesUnwelcomed.Audio.Mixing;
using HeroesUnwelcomed.Audio.Music;

namespace HeroesUnwelcomed.Audio.Tests.Mixing;

public class MusicMixerTests
{
    private const int Rate = 48_000;

    // Fake music: each stem is a constant value, so any change in the output
    // can be pinned to the exact sample where it happened.
    private static MusicTrack ConstantTrack(params (string Version, double Bpm, float Value)[] versions)
    {
        var map = versions.ToDictionary(
            v => v.Version,
            v =>
            {
                int frames = (int)Math.Round(4 * 60.0 * Rate / v.Bpm); // one bar of 4 beats
                var stem = Enumerable.Repeat(v.Value, frames).ToArray();
                return new MusicVersion(v.Version, v.Bpm, 4, 1, Rate, new Dictionary<string, float[]> { ["lead"] = stem });
            });
        return new MusicTrack("test", map);
    }

    private static float[] Render(MusicMixer mixer, int frames)
    {
        var output = new float[frames * 2];
        mixer.Render(output);
        return output;
    }

    // Left channel of a frame (centre-panned, so left = value × 0.707 × master gain).
    private static float Left(float[] stereo, int frame) => stereo[frame * 2];

    [Fact]
    public void Mute_StartsExactlyOnTheScheduledBeat()
    {
        var mixer = new MusicMixer(ConstantTrack(("a", 120, 0.5f)), "a", 1) { MasterGain = 1f };

        mixer.ScheduleStemGain("lead", 0f, atBeat: 2, rampBeats: 0);
        float[] output = Render(mixer, 60_000);

        const int beatTwo = 48_000; // 120 bpm → 24,000 frames per beat
        const int fiveMs = 240;
        Assert.True(Left(output, beatTwo - 1) > 0.3f);           // still playing just before the beat
        Assert.True(Left(output, beatTwo) < Left(output, beatTwo - 1)); // fade begins on the beat
        Assert.Equal(0f, Left(output, beatTwo + fiveMs + 1));     // silent after the 5 ms anti-click ramp
    }

    [Fact]
    public void MutedStem_ComesBackInTime()
    {
        // The lead's position must keep advancing while muted. Build a stem
        // that counts upward, mute it, unmute later, and check the value.
        int frames = 4 * 24_000;
        float[] ramp = Enumerable.Range(0, frames).Select(i => i / (float)frames).ToArray();
        var version = new MusicVersion("a", 120, 4, 1, Rate, new Dictionary<string, float[]> { ["lead"] = ramp });
        var mixer = new MusicMixer(new MusicTrack("t", new Dictionary<string, MusicVersion> { ["a"] = version }), "a", 1) { MasterGain = 1f };

        mixer.ScheduleStemGain("lead", 0f, atBeat: 1);
        mixer.ScheduleStemGain("lead", 1f, atBeat: 3);
        float[] output = Render(mixer, frames);

        int checkFrame = 3 * 24_000 + 1000; // shortly after it comes back
        float expected = ramp[checkFrame] * MathF.Cos(MathF.PI / 4f);
        Assert.Equal(expected, Left(output, checkFrame), 3);
    }

    [Fact]
    public void VersionSwitch_HappensOnTheBeat_AndChangesTempo()
    {
        var mixer = new MusicMixer(ConstantTrack(("slow", 120, 0.25f), ("fast", 240, 0.5f)), "slow", 1) { MasterGain = 1f };

        mixer.ScheduleVersion("fast", atBeat: 1, simRate: 1);
        float[] output = Render(mixer, 30_000);

        Assert.Equal(0.25f * MathF.Cos(MathF.PI / 4f), Left(output, 23_999), 4);
        Assert.Equal(0.5f * MathF.Cos(MathF.PI / 4f), Left(output, 24_000), 4);
        Assert.Equal(240, mixer.Tempo.Latest.Bpm);
        Assert.Equal(1.0, mixer.Tempo.Latest.StartBeat);
        // After the switch a beat is 12,000 frames: frame 36,000 is beat 2.
        Assert.Equal(2.0, mixer.Tempo.BeatAt(36_000), 9);
    }

    [Fact]
    public void SimRateChange_AppliesImmediatelyAtRenderHead()
    {
        var mixer = new MusicMixer(ConstantTrack(("a", 120, 0.1f)), "a", 1);
        Render(mixer, 24_000); // play one beat

        mixer.SetSimRateNow(0);
        Render(mixer, 48_000);

        Assert.Equal(1.0, mixer.Tempo.SimBeatAt(mixer.RenderFrame), 9); // frozen at 1
        Assert.Equal(3.0, mixer.Tempo.BeatAt(mixer.RenderFrame), 9);    // music carried on
    }

    [Fact]
    public void NextGridBeat_IsAheadOfRenderHead()
    {
        var mixer = new MusicMixer(ConstantTrack(("a", 120, 0.1f)), "a", 1);
        Render(mixer, 30_000); // render head at beat 1.25

        Assert.Equal(2.0, mixer.NextGridBeat(1), 9);
        Assert.Equal(4.0, mixer.NextGridBeat(4), 9);
    }

    [Fact]
    public void UnknownNames_FailWhenScheduled_NotDuringPlayback()
    {
        var mixer = new MusicMixer(ConstantTrack(("a", 120, 0.1f)), "a", 1);

        Assert.Throws<ArgumentException>(() => mixer.ScheduleStemGain("nope", 0f, 1));
        Assert.Throws<ArgumentException>(() => mixer.ScheduleVersion("nope", 1, 1));
    }

    [Fact]
    public void LoudSignals_AreLimited_NotClipped()
    {
        var mixer = new MusicMixer(ConstantTrack(("a", 120, 5f)), "a", 1) { MasterGain = 1f };

        float[] output = Render(mixer, 1000);

        Assert.All(output, sample => Assert.InRange(sample, -1f, 1f));
    }
}
