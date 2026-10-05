using System.Text;

namespace HeroesUnwelcomed.Core.Randomness;

/// <summary>
/// Turns text into a number that is the same every time the program runs.
/// </summary>
/// <remarks>
/// C#'s built-in <c>string.GetHashCode()</c> is deliberately randomized each time
/// the program starts (a security feature), so "PIZZA" would give a different seed
/// on every launch. This uses FNV-1a, a simple hash with a fixed definition.
/// </remarks>
public static class StableHash
{
    private const ulong OffsetBasis = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    public static ulong Of(string text)
    {
        ulong hash = OffsetBasis;
        foreach (byte b in Encoding.UTF8.GetBytes(text))
        {
            hash ^= b;
            hash = unchecked(hash * Prime);
        }
        return hash;
    }
}
