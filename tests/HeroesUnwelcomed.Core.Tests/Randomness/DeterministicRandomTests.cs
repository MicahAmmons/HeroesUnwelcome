using HeroesUnwelcomed.Core.Randomness;

namespace HeroesUnwelcomed.Core.Tests.Randomness;

public class DeterministicRandomTests
{
    [Fact]
    public void SameSeed_ProducesSameSequence()
    {
        var a = new DeterministicRandom(42);
        var b = new DeterministicRandom(42);

        for (int i = 0; i < 1000; i++)
        {
            Assert.Equal(a.NextUInt(), b.NextUInt());
        }
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentSequences()
    {
        var a = new DeterministicRandom(1);
        var b = new DeterministicRandom(2);

        int matches = Enumerable.Range(0, 100).Count(_ => a.NextUInt() == b.NextUInt());

        Assert.True(matches < 5);
    }

    [Fact]
    public void KnownSeed_ProducesKnownValues()
    {
        // "Golden" values from the official PCG32 reference program (pcg32-demo,
        // seed 42, stream 54). Passing proves we implemented PCG32 correctly; it
        // also locks the algorithm, so if this ever fails, the generator changed
        // and every existing seed/replay would break.
        var rng = new DeterministicRandom(seed: 42, stream: 54);

        uint[] actual = [rng.NextUInt(), rng.NextUInt(), rng.NextUInt(), rng.NextUInt(), rng.NextUInt(), rng.NextUInt()];

        Assert.Equal([0xa15c02b7, 0x7b47f409, 0xba1d3330, 0x83d2f293, 0xbfa4784b, 0xcbed606e], actual);
    }

    [Fact]
    public void TextSeed_IsStableAcrossRuns()
    {
        var a = DeterministicRandom.FromText("PIZZA");
        var b = DeterministicRandom.FromText("PIZZA");

        Assert.Equal(a.Seed, b.Seed);
        Assert.Equal(a.NextUInt(), b.NextUInt());
    }

    [Fact]
    public void Fork_IsIndependentOfHowMuchParentWasUsed()
    {
        // Drawing from the parent must not change what a forked stream produces.
        var fresh = new DeterministicRandom(7);
        var used = new DeterministicRandom(7);
        for (int i = 0; i < 500; i++)
        {
            used.NextUInt();
        }

        Assert.Equal(fresh.Fork("combat").NextUInt(), used.Fork("combat").NextUInt());
    }

    [Fact]
    public void Forks_WithDifferentNames_Differ()
    {
        var rng = new DeterministicRandom(7);
        var combat = rng.Fork("combat");
        var cosmetic = rng.Fork("cosmetic");

        int matches = Enumerable.Range(0, 100).Count(_ => combat.NextUInt() == cosmetic.NextUInt());

        Assert.True(matches < 5);
    }

    [Fact]
    public void Next_StaysWithinRange_AndHitsEveryValue()
    {
        var rng = new DeterministicRandom(123);
        var seen = new int[6];

        for (int i = 0; i < 6000; i++)
        {
            int roll = rng.Next(6);
            Assert.InRange(roll, 0, 5);
            seen[roll]++;
        }

        // Roughly 1000 each; a fair die shouldn't be wildly off.
        Assert.All(seen, count => Assert.InRange(count, 850, 1150));
    }

    [Fact]
    public void NextFloat_IsBetweenZeroAndOne()
    {
        var rng = new DeterministicRandom(9);

        for (int i = 0; i < 10_000; i++)
        {
            float value = rng.NextFloat();
            Assert.True(value >= 0f && value < 1f);
        }
    }

    [Fact]
    public void Next_RejectsEmptyRange()
    {
        var rng = new DeterministicRandom(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => rng.Next(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.Next(5, 5));
    }
}
