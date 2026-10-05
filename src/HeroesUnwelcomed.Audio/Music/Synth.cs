using HeroesUnwelcomed.Core.Randomness;

namespace HeroesUnwelcomed.Audio.Music;

/// <summary>
/// Minimal instrument sounds built from maths: sine waves, noise and decay curves.
/// Used only to make placeholder music; real music comes from the composer.
/// </summary>
/// <remarks>
/// Every sound is written *into* a loop buffer by adding samples. Notes near the
/// end of the loop wrap around to the start (index modulo length), so their
/// tails ring into the next repetition and the loop has no seam.
/// </remarks>
internal sealed class Synth
{
    private readonly int _sampleRate;
    private readonly DeterministicRandom _noise;

    public Synth(int sampleRate, DeterministicRandom noise)
    {
        _sampleRate = sampleRate;
        _noise = noise;
    }

    /// <summary>Frequency in Hz of a MIDI note number (69 = A4 = 440 Hz; +12 = one octave up).</summary>
    public static double NoteFrequency(int midiNote) => 440.0 * Math.Pow(2.0, (midiNote - 69) / 12.0);

    /// <summary>
    /// Kick drum: a sine wave whose pitch drops fast from 120 Hz to 45 Hz
    /// ("the thump"), fading out over a third of a second.
    /// </summary>
    public void Kick(float[] buffer, int start, float amplitude)
    {
        int length = Frames(0.35);
        double phase = 0;
        for (int i = 0; i < length; i++)
        {
            double t = (double)i / _sampleRate;
            double frequency = 45 + 75 * Math.Exp(-t * 30);
            phase += 2 * Math.PI * frequency / _sampleRate;
            Add(buffer, start + i, (float)(Math.Sin(phase) * Math.Exp(-t * 9)) * amplitude);
        }
    }

    /// <summary>Snare: a burst of noise (the rattle) plus a short 180 Hz tone (the body).</summary>
    public void Snare(float[] buffer, int start, float amplitude)
    {
        int length = Frames(0.2);
        for (int i = 0; i < length; i++)
        {
            double t = (double)i / _sampleRate;
            double rattle = NextNoise() * Math.Exp(-t * 22) * 0.7;
            double body = Math.Sin(2 * Math.PI * 180 * t) * Math.Exp(-t * 30) * 0.4;
            Add(buffer, start + i, (float)(rattle + body) * amplitude);
        }
    }

    /// <summary>
    /// Hi-hat: very short noise. Subtracting each noise sample from the previous
    /// one removes the low rumble and leaves the bright "tsk".
    /// </summary>
    public void HiHat(float[] buffer, int start, float amplitude, double decaySeconds = 0.05)
    {
        int length = Frames(decaySeconds * 1.5);
        double previous = 0;
        for (int i = 0; i < length; i++)
        {
            double t = (double)i / _sampleRate;
            double noise = NextNoise();
            double bright = noise - previous;
            previous = noise;
            Add(buffer, start + i, (float)(bright * 0.5 * Math.Exp(-t / decaySeconds * 3)) * amplitude);
        }
    }

    /// <summary>
    /// A pitched note. <paramref name="harmonics"/> adds quieter copies at 2× and 3×
    /// the frequency, which makes a plain sine sound more like an instrument.
    /// The envelope fades in over <paramref name="attackSeconds"/> and out over the
    /// final <paramref name="releaseSeconds"/>, so notes don't click.
    /// </summary>
    public void Tone(float[] buffer, int start, double frequency, double seconds, float amplitude,
        double harmonics = 0.3, double attackSeconds = 0.005, double releaseSeconds = 0.05)
    {
        int length = Frames(seconds);
        int attack = Math.Max(1, Frames(attackSeconds));
        int release = Math.Max(1, Frames(releaseSeconds));

        for (int i = 0; i < length; i++)
        {
            double t = (double)i / _sampleRate;
            double envelope = Math.Min(1.0, (double)i / attack) * Math.Min(1.0, (double)(length - i) / release);
            double wave = Math.Sin(2 * Math.PI * frequency * t)
                + harmonics * 0.5 * Math.Sin(2 * Math.PI * frequency * 2 * t)
                + harmonics * 0.25 * Math.Sin(2 * Math.PI * frequency * 3 * t);
            Add(buffer, start + i, (float)(wave * envelope) * amplitude);
        }
    }

    /// <summary>Plucked note: like <see cref="Tone"/> but fading away naturally after the attack.</summary>
    public void Pluck(float[] buffer, int start, double frequency, double seconds, float amplitude, double harmonics = 0.4)
    {
        int length = Frames(seconds);
        for (int i = 0; i < length; i++)
        {
            double t = (double)i / _sampleRate;
            double envelope = Math.Min(1.0, i / (0.003 * _sampleRate)) * Math.Exp(-t * 6);
            double wave = Math.Sin(2 * Math.PI * frequency * t)
                + harmonics * 0.5 * Math.Sin(2 * Math.PI * frequency * 2 * t);
            Add(buffer, start + i, (float)(wave * envelope) * amplitude);
        }
    }

    /// <summary>Metronome click: a 30 ms beep, higher on the first beat of each bar.</summary>
    public void Click(float[] buffer, int start, bool downbeat, float amplitude)
    {
        double frequency = downbeat ? 1500 : 1000;
        int length = Frames(0.03);
        for (int i = 0; i < length; i++)
        {
            double t = (double)i / _sampleRate;
            Add(buffer, start + i, (float)(Math.Sin(2 * Math.PI * frequency * t) * Math.Exp(-t * 120)) * amplitude);
        }
    }

    private int Frames(double seconds) => (int)(seconds * _sampleRate);

    private double NextNoise() => _noise.NextFloat() * 2.0 - 1.0;

    private static void Add(float[] buffer, int index, float value)
    {
        int wrapped = index % buffer.Length;
        if (wrapped < 0)
        {
            wrapped += buffer.Length;
        }
        buffer[wrapped] += value;
    }
}
