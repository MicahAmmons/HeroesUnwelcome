namespace HeroesUnwelcomed.Audio.Mixing;

/// <summary>
/// One mixer channel: a stem's volume and left/right position.
/// </summary>
/// <remarks>
/// Volume never jumps instantly. A sudden jump from full to silent cuts the
/// waveform mid-wiggle, and speakers reproduce that as an audible click. Instead
/// the gain glides to its target over at least a few milliseconds (a "ramp").
/// </remarks>
public sealed class StemChannel
{
    private float _step;
    private float _pan;

    public StemChannel(string name, float gain = 1f, float pan = 0f)
    {
        Name = name;
        Gain = gain;
        TargetGain = gain;
        Pan = pan;
    }

    public string Name { get; }

    /// <summary>Volume right now (0 = silent, 1 = full).</summary>
    public float Gain { get; private set; }

    /// <summary>Volume the channel is gliding toward.</summary>
    public float TargetGain { get; private set; }

    /// <summary>-1 = hard left, 0 = centre, 1 = hard right.</summary>
    public float Pan
    {
        get => _pan;
        set
        {
            _pan = Math.Clamp(value, -1f, 1f);
            // Worked out once here rather than for every sample.
            float angle = (_pan + 1f) * MathF.PI / 4f; // 0 (left) .. π/2 (right)
            LeftGain = MathF.Cos(angle);
            RightGain = MathF.Sin(angle);
        }
    }

    /// <summary>
    /// "Equal-power" panning: at centre each side gets ~0.707, not 0.5, so a
    /// sound doesn't seem to get quieter as it moves through the middle.
    /// </summary>
    internal float LeftGain { get; private set; }

    internal float RightGain { get; private set; }

    /// <summary>The current version's samples for this stem, or null if that version doesn't use it.</summary>
    internal float[]? Buffer { get; set; }

    public bool IsSilent => Gain == 0f && TargetGain == 0f;

    /// <summary>Start gliding to <paramref name="target"/> over <paramref name="frames"/> frames.</summary>
    public void RampTo(float target, int frames)
    {
        TargetGain = Math.Clamp(target, 0f, 2f);
        _step = frames <= 0 ? float.MaxValue : Math.Abs(TargetGain - Gain) / frames;
        if (_step == 0f)
        {
            Gain = TargetGain;
        }
    }

    /// <summary>Advances the glide by one frame and returns the volume to use for it.</summary>
    internal float NextGain()
    {
        if (Gain != TargetGain)
        {
            Gain = Gain < TargetGain
                ? Math.Min(Gain + _step, TargetGain)
                : Math.Max(Gain - _step, TargetGain);
        }
        return Gain;
    }
}
