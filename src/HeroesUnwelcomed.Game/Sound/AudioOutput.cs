using HeroesUnwelcomed.Audio.Mixing;
using Microsoft.Xna.Framework.Audio;

namespace HeroesUnwelcomed.Game.Sound;

/// <summary>
/// Sends the mixer's samples to the speakers through MonoGame's
/// <see cref="DynamicSoundEffectInstance"/> (a "streaming" sound we feed ourselves).
/// </summary>
/// <remarks>
/// <para>
/// <b>Buffers.</b> We keep about four small buffers (~21 ms each) queued at all
/// times. The sound card plays them in order; each frame we top the queue back
/// up. More buffers = safer against hiccups but more delay between "changed"
/// and "heard"; ~85 ms is a comfortable middle for a game that isn't a rhythm game.
/// </para>
/// <para>
/// <b>Where playback is.</b> We know how many frames we've submitted and how many
/// buffers haven't finished playing. Submitted minus still-queued = roughly what
/// has been heard. The true position is somewhere inside the buffer that's
/// playing right now, so we add half a buffer as a best guess.
/// </para>
/// <para>
/// <b>No sound card?</b> Then a "virtual" clock advances by real time and the
/// game behaves exactly the same, just silently.
/// </para>
/// <para>
/// <b>Known limitation:</b> buffers are refilled from the game loop. If the
/// loop stalls for longer than the queue (~85 ms), e.g. while Windows is
/// dragging the window, the sound briefly stutters. A dedicated audio thread
/// can fix that later if it matters in practice.
/// </para>
/// </remarks>
public sealed class AudioOutput : IDisposable
{
    public const int SampleRate = 48_000;
    public const int BufferFrames = 1024;
    public const int TargetQueuedBuffers = 4;

    // The first few frames after launch are slow (window creation, shader and
    // font setup), long enough to drain a 4-buffer queue. So the very first fill
    // queues ~250 ms; after that we only top up to 4, and the extra drains away
    // naturally within the first second.
    public const int StartupQueuedBuffers = 12;

    private readonly DynamicSoundEffectInstance? _instance;
    private readonly float[] _mix = new float[BufferFrames * 2];
    private readonly byte[] _pcm = new byte[BufferFrames * 2 * sizeof(short)];
    private bool _started;
    private double _virtualFrames;

    public AudioOutput()
    {
        try
        {
            _instance = new DynamicSoundEffectInstance(SampleRate, AudioChannels.Stereo);
        }
        catch (Exception ex) when (ex is NoAudioHardwareException or InstancePlayLimitException or InvalidOperationException)
        {
            FailureReason = ex.Message;
        }
    }

    public bool HasDevice => _instance is not null;
    public string? FailureReason { get; }
    public long FramesSubmitted { get; private set; }

    /// <summary>Times the queue ran completely dry (an audible gap).</summary>
    public int Underruns { get; private set; }

    public int QueuedBuffers => _instance?.PendingBufferCount ?? 0;

    /// <summary>How far ahead of the speakers the mixer works.</summary>
    public static double BufferedSeconds => TargetQueuedBuffers * BufferFrames / (double)SampleRate;

    /// <summary>Best estimate of the frame currently coming out of the speakers.</summary>
    public double AudibleFrame
    {
        get
        {
            if (_instance is null)
            {
                return _virtualFrames;
            }
            double estimate = FramesSubmitted - _instance.PendingBufferCount * (double)BufferFrames + BufferFrames * 0.5;
            return Math.Max(0, estimate);
        }
    }

    /// <summary>Tops up the queue so playback never runs dry. Call once per frame.</summary>
    public void Pump(MusicMixer mixer, double deltaSeconds)
    {
        if (_instance is null)
        {
            _virtualFrames += deltaSeconds * SampleRate;
            while (FramesSubmitted < _virtualFrames + BufferFrames * TargetQueuedBuffers)
            {
                mixer.Render(_mix);
                FramesSubmitted += BufferFrames;
            }
            return;
        }

        if (_started && _instance.PendingBufferCount == 0)
        {
            Underruns++;
        }

        int target = _started ? TargetQueuedBuffers : StartupQueuedBuffers;
        while (_instance.PendingBufferCount < target)
        {
            mixer.Render(_mix);
            ConvertToPcm16();
            _instance.SubmitBuffer(_pcm);
            FramesSubmitted += BufferFrames;
        }

        if (!_started)
        {
            _instance.Play();
            _started = true;
        }
    }

    // Sound cards take 16-bit whole numbers (-32768..32767), not -1..1 floats.
    // Each number is stored as two bytes, low byte first ("little-endian").
    private void ConvertToPcm16()
    {
        for (int i = 0; i < _mix.Length; i++)
        {
            short value = (short)(Math.Clamp(_mix[i], -1f, 1f) * short.MaxValue);
            _pcm[i * 2] = (byte)value;
            _pcm[i * 2 + 1] = (byte)(value >> 8);
        }
    }

    public void Dispose()
    {
        _instance?.Stop();
        _instance?.Dispose();
    }
}
