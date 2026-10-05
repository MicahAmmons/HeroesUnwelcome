using HeroesUnwelcomed.Audio.Timing;

namespace HeroesUnwelcomed.Audio.Tests.Timing;

public class TempoMapTests
{
    private const int Rate = 48_000;

    [Fact]
    public void ConstantTempo_BeatIsFramesTimesBpmOverSixtySampleRate()
    {
        var map = new TempoMap(Rate, bpm: 120, simRate: 1);

        // 120 bpm = 2 beats per second = 24,000 frames per beat.
        Assert.Equal(0.0, map.BeatAt(0));
        Assert.Equal(1.0, map.BeatAt(24_000), 9);
        Assert.Equal(10.0, map.BeatAt(240_000), 9);
    }

    [Fact]
    public void TempoChange_ContinuesBeatCountFromWhereItWas()
    {
        var map = new TempoMap(Rate, bpm: 120, simRate: 1);

        map.Change(frame: 96_000, bpm: 60); // beat 4, then half speed

        Assert.Equal(4.0, map.BeatAt(96_000), 9);
        Assert.Equal(5.0, map.BeatAt(96_000 + 48_000), 9); // at 60 bpm a beat is 48,000 frames
        Assert.Equal(3.0, map.BeatAt(72_000), 9);           // before the change: old tempo still applies
    }

    [Fact]
    public void SimBeat_StopsWhilePaused_AndResumesFromSameValue()
    {
        var map = new TempoMap(Rate, bpm: 120, simRate: 1);

        map.Change(frame: 48_000, simRate: 0);  // pause at beat 2
        map.Change(frame: 144_000, simRate: 1); // resume at music beat 6

        Assert.Equal(2.0, map.SimBeatAt(48_000), 9);
        Assert.Equal(2.0, map.SimBeatAt(100_000), 9); // frozen
        Assert.Equal(6.0, map.BeatAt(144_000), 9);    // ...while the music kept going
        Assert.Equal(3.0, map.SimBeatAt(144_000 + 24_000), 9);
    }

    [Fact]
    public void SimRateTwo_RunsGameAtDoubleTheMusicBeat()
    {
        var map = new TempoMap(Rate, bpm: 100, simRate: 2);

        Assert.Equal(1.0, map.BeatAt(28_800), 9);
        Assert.Equal(2.0, map.SimBeatAt(28_800), 9);
    }

    [Fact]
    public void SnapBeat_MakesSegmentStartOnExactWholeBeat()
    {
        var map = new TempoMap(Rate, bpm: 120, simRate: 1);

        map.Change(frame: 24_001, bpm: 90, snapBeat: 1.0);

        Assert.Equal(1.0, map.Latest.StartBeat);
    }

    [Fact]
    public void Change_InThePast_IsRejected()
    {
        var map = new TempoMap(Rate, bpm: 120, simRate: 1);
        map.Change(frame: 1000, bpm: 90);

        Assert.Throws<ArgumentOutOfRangeException>(() => map.Change(frame: 500, bpm: 100));
    }

    [Fact]
    public void ForgetBefore_KeepsLookupsAfterThatPointCorrect()
    {
        var map = new TempoMap(Rate, bpm: 120, simRate: 1);
        map.Change(frame: 24_000, bpm: 60);
        map.Change(frame: 72_000, bpm: 120);
        double expected = map.BeatAt(80_000);

        map.ForgetBefore(50_000);

        Assert.Equal(2, map.Segments.Count);
        Assert.Equal(expected, map.BeatAt(80_000), 9);
        Assert.Equal(1.5, map.BeatAt(48_000), 9);
    }

    [Theory]
    [InlineData(11.2, 1, 12)]
    [InlineData(12.0, 1, 12)]           // exactly on the grid: that beat counts
    [InlineData(11.9999999999, 1, 12)]  // floating-point fuzz just below a whole beat
    [InlineData(13.0, 4, 16)]           // next bar
    [InlineData(0.1, 0.5, 0.5)]         // next half-beat
    public void Quantize_RoundsUpToGrid(double beat, double grid, double expected)
    {
        Assert.Equal(expected, Quantize.NextMultiple(beat, grid), 9);
    }
}
