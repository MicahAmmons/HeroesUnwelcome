using HeroesUnwelcomed.Core.Randomness;

namespace HeroesUnwelcomed.Audio.Music;

/// <summary>
/// Stand-in sound effects made from maths, so party reactions can be heard
/// before real sounds exist. Each is a mono buffer played once.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><b>gasp</b>: a breathy rising whoosh of noise (teammates seeing a death).</item>
///   <item><b>hit</b>: a low thud (the death itself).</item>
///   <item><b>cheer</b>: a quick bright rising arpeggio (a room beaten).</item>
///   <item><b>whoosh</b>: a soft noise sweep (the banner unfurling).</item>
///   <item><b>fill</b>: a one-beat snare roll that crescendos (the music coming back in).</item>
/// </list>
/// </remarks>
public static class PlaceholderSounds
{
    public static IReadOnlyDictionary<string, float[]> Create(int sampleRate, double fillBpm, ulong seed = 3)
    {
        var random = new DeterministicRandom(seed);
        return new Dictionary<string, float[]>
        {
            ["gasp"] = Gasp(sampleRate, random.Fork("gasp")),
            ["hit"] = Hit(sampleRate),
            ["cheer"] = Cheer(sampleRate),
            ["whoosh"] = Whoosh(sampleRate, random.Fork("whoosh")),
            ["fill"] = Fill(sampleRate, fillBpm, random.Fork("fill")),
        };
    }

    private static float[] Gasp(int rate, DeterministicRandom random)
    {
        var buffer = new float[(int)(0.45 * rate)];
        double low = 0;
        for (int i = 0; i < buffer.Length; i++)
        {
            double t = (double)i / buffer.Length;
            double noise = random.NextFloat() * 2 - 1;
            low += (noise - low) * (0.05 + 0.25 * t); // the filter opens up: the gasp "rises"
            double envelope = Math.Sin(Math.PI * Math.Min(1, t * 1.6)) * (1 - t * 0.6);
            buffer[i] = (float)(low * envelope * 1.6);
        }
        return buffer;
    }

    private static float[] Hit(int rate)
    {
        var buffer = new float[(int)(0.5 * rate)];
        double phase = 0;
        for (int i = 0; i < buffer.Length; i++)
        {
            double t = (double)i / rate;
            phase += 2 * Math.PI * (40 + 80 * Math.Exp(-t * 25)) / rate;
            buffer[i] = (float)(Math.Sin(phase) * Math.Exp(-t * 7) * 0.9);
        }
        return buffer;
    }

    private static float[] Cheer(int rate)
    {
        int[] notes = [72, 76, 79, 84]; // C major arpeggio, rising
        double noteSeconds = 0.09;
        var buffer = new float[(int)((notes.Length * noteSeconds + 0.4) * rate)];
        for (int n = 0; n < notes.Length; n++)
        {
            double frequency = 440 * Math.Pow(2, (notes[n] - 69) / 12.0);
            int start = (int)(n * noteSeconds * rate);
            for (int i = 0; start + i < buffer.Length; i++)
            {
                double t = (double)i / rate;
                double envelope = Math.Min(1, t / 0.005) * Math.Exp(-t * 7);
                buffer[start + i] += (float)((Math.Sin(2 * Math.PI * frequency * t) + 0.3 * Math.Sin(4 * Math.PI * frequency * t)) * envelope * 0.25);
            }
        }
        return buffer;
    }

    private static float[] Whoosh(int rate, DeterministicRandom random)
    {
        var buffer = new float[(int)(0.6 * rate)];
        double low = 0;
        for (int i = 0; i < buffer.Length; i++)
        {
            double t = (double)i / buffer.Length;
            double noise = random.NextFloat() * 2 - 1;
            low += (noise - low) * (0.02 + 0.1 * Math.Sin(Math.PI * t));
            buffer[i] = (float)(low * Math.Sin(Math.PI * t) * 1.8);
        }
        return buffer;
    }

    // A beat-long roll of snare hits getting faster and louder, ending on the next downbeat.
    private static float[] Fill(int rate, double bpm, DeterministicRandom random)
    {
        double beatSeconds = 60.0 / bpm;
        var buffer = new float[(int)(beatSeconds * rate)];
        double[] hits = [0, 0.25, 0.5, 0.625, 0.75, 0.875];
        for (int h = 0; h < hits.Length; h++)
        {
            int start = (int)(hits[h] * beatSeconds * rate);
            double amplitude = 0.2 + 0.5 * h / (hits.Length - 1);
            for (int i = 0; start + i < buffer.Length && i < 0.12 * rate; i++)
            {
                double t = (double)i / rate;
                double noise = random.NextFloat() * 2 - 1;
                buffer[start + i] += (float)((noise * 0.7 + Math.Sin(2 * Math.PI * 190 * t) * 0.4) * Math.Exp(-t * 28) * amplitude);
            }
        }
        return buffer;
    }
}
