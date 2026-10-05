namespace HeroesUnwelcomed.Core.Randomness;

/// <summary>
/// A seeded random number generator whose output is guaranteed never to change.
/// Same seed in, same sequence out, on every machine and every .NET version.
/// </summary>
/// <remarks>
/// <para>
/// Why not <see cref="System.Random"/>? Microsoft reserves the right to change its
/// algorithm between .NET versions. If that ever happened, every saved replay and
/// seed would silently produce a different game. Owning the algorithm (about 20
/// lines) removes that risk.
/// </para>
/// <para>
/// The algorithm is PCG32 (pcg-random.org): a small, fast, well-studied
/// generator. Each call advances a 64-bit internal state with one multiply and one
/// add, then scrambles the bits of that state into a 32-bit output.
/// </para>
/// <para>
/// Use <see cref="Fork"/> to give each game system (combat rolls, party selection,
/// cosmetics...) its own independent stream. That way, adding a random blink
/// animation can never shift the outcome of a fight.
/// </para>
/// </remarks>
public sealed class DeterministicRandom
{
    // PCG's standard multiplier. Changing it would change every sequence.
    private const ulong Multiplier = 6364136223846793005UL;

    private ulong _state;
    private readonly ulong _increment; // must be odd; selects one of 2^63 distinct streams

    /// <summary>The seed this generator was created from (handy for logging and replays).</summary>
    public ulong Seed { get; }

    /// <param name="seed">Starting point of the sequence.</param>
    /// <param name="stream">Which independent stream to use for that seed.</param>
    public DeterministicRandom(ulong seed, ulong stream = 0)
    {
        Seed = seed;
        _increment = (stream << 1) | 1UL;
        _state = 0;
        NextUInt();
        _state += seed;
        NextUInt();
    }

    /// <summary>Creates a generator from text, the way games accept seeds like "PIZZA".</summary>
    public static DeterministicRandom FromText(string seedText) => new(StableHash.Of(seedText));

    /// <summary>
    /// Creates an independent child generator for a named purpose, e.g. Fork("combat").
    /// The child depends only on this generator's seed and the name, NOT on how many
    /// numbers have been drawn so far, so systems can't disturb one another.
    /// </summary>
    public DeterministicRandom Fork(string purpose) => new(Seed, StableHash.Of(purpose));

    /// <summary>Next raw 32-bit value. Everything else is built on this.</summary>
    public uint NextUInt()
    {
        ulong old = _state;
        _state = unchecked(old * Multiplier + _increment);

        // Output scramble: xorshift the high bits down, then rotate by an amount
        // taken from the top 5 bits. This hides the weak low bits of the raw state.
        uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
        int rotation = (int)(old >> 59);
        return (xorShifted >> rotation) | (xorShifted << (-rotation & 31));
    }

    /// <summary>Integer in [0, maxExclusive). Unbiased: every value is equally likely.</summary>
    public int Next(int maxExclusive)
    {
        if (maxExclusive <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Must be greater than zero.");
        }

        // Plain "NextUInt() % max" slightly favours small numbers when 2^32 isn't a
        // multiple of max. Rejecting the few values in the uneven tail fixes that.
        uint bound = (uint)maxExclusive;
        uint threshold = (uint)(-bound % bound);
        while (true)
        {
            uint value = NextUInt();
            if (value >= threshold)
            {
                return (int)(value % bound);
            }
        }
    }

    /// <summary>Integer in [minInclusive, maxExclusive).</summary>
    public int Next(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Must be greater than minInclusive.");
        }
        return minInclusive + Next(maxExclusive - minInclusive);
    }

    /// <summary>Float in [0, 1). Uses 24 random bits, which is exactly a float's precision.</summary>
    public float NextFloat() => (NextUInt() >> 8) * (1f / (1 << 24));

    /// <summary>True with the given probability (0 = never, 1 = always).</summary>
    public bool Chance(float probability) => NextFloat() < probability;
}
