namespace HeroesUnwelcomed.Audio.Timing;

/// <summary>
/// Rounding musical time to a grid, the way a music app "snaps to beat".
/// "Mute on the next beat" means: round up to the next multiple of 1.
/// "Change on the next bar" (4 beats) means: round up to the next multiple of 4.
/// </summary>
public static class Quantize
{
    // Doubles carry tiny rounding errors: 12.0 might be stored as 11.9999999998.
    // Without this tolerance, "next beat after 12" could wrongly return 12 or 13.
    private const double Tolerance = 1e-9;

    /// <summary>The first multiple of <paramref name="grid"/> at or after <paramref name="beat"/>.</summary>
    public static double NextMultiple(double beat, double grid)
    {
        if (grid <= 0) throw new ArgumentOutOfRangeException(nameof(grid));
        return Math.Ceiling(beat / grid - Tolerance) * grid;
    }
}
