using HeroesUnwelcomed.Audio.Music;

namespace HeroesUnwelcomed.Audio.Tests.Music;

public class PlaceholderMusicTests
{
    private const int Rate = 48_000;

    private static readonly SpeedLevel[] Levels =
    [
        new("Paused", 70, 0, "calm"),
        new("1x", 100, 1, "normal"),
        new("2x", 100, 2, "driving"),
        new("3x", 150, 2, "driving"),
    ];

    [Fact]
    public void CreatesOneVersionPerSpeedLevel_WithAllStems()
    {
        MusicTrack track = PlaceholderMusic.Create(Levels, Rate);

        Assert.Equal(Levels.Select(l => l.Name), track.Versions.Keys);
        foreach (MusicVersion version in track.Versions.Values)
        {
            Assert.Equal(PlaceholderMusic.StemNames, version.Stems.Keys);
        }
    }

    [Fact]
    public void LoopLength_IsFourBarsAtThatTempo()
    {
        MusicTrack track = PlaceholderMusic.Create(Levels, Rate);

        // 100 bpm: 16 beats × 28,800 frames = 460,800.
        Assert.Equal(460_800, track.Versions["1x"].LoopFrames);
        Assert.Equal(16, track.Versions["1x"].LoopBeats);
    }

    [Fact]
    public void KickDrum_HitsOnTheBeat()
    {
        MusicTrack track = PlaceholderMusic.Create(Levels, Rate);
        float[] drums = track.Versions["1x"].Stems["drums"];

        // Loud in the first 20 ms after beat 1 (a kick), close to silent just before
        // beat 2 (the kick has died away and the snare hasn't started yet).
        float atKick = Peak(drums, 0, 960);
        float beforeSnare = Peak(drums, 28_800 - 960, 28_800 - 1);

        Assert.True(atKick > 0.5f, $"kick peak {atKick}");
        Assert.True(beforeSnare < 0.05f, $"pre-snare level {beforeSnare}");
    }

    [Fact]
    public void CalmVersion_HasNoDrums()
    {
        MusicTrack track = PlaceholderMusic.Create(Levels, Rate);

        Assert.Equal(0f, Peak(track.Versions["Paused"].Stems["drums"], 0, track.Versions["Paused"].LoopFrames - 1));
    }

    [Fact]
    public void SameSeed_GivesIdenticalMusic()
    {
        MusicTrack a = PlaceholderMusic.Create(Levels, Rate, seed: 5);
        MusicTrack b = PlaceholderMusic.Create(Levels, Rate, seed: 5);

        Assert.Equal(a.Versions["1x"].Stems["hats"], b.Versions["1x"].Stems["hats"]);
    }

    private static float Peak(float[] samples, int from, int to)
    {
        float peak = 0f;
        for (int i = from; i <= to; i++)
        {
            peak = Math.Max(peak, Math.Abs(samples[i]));
        }
        return peak;
    }
}
