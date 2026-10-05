using HeroesUnwelcomed.Core.Randomness;

namespace HeroesUnwelcomed.Audio.Music;

/// <summary>
/// Generates a stand-in music track (one version per speed level) so the whole
/// music system can be built and tested before real music exists.
/// </summary>
/// <remarks>
/// <para>
/// The output has exactly the shape real music will have: per speed level, a
/// 4-bar loop split into the same named stems. When the composer's files
/// arrive, only the loading changes; the mixer can't tell the difference.
/// </para>
/// <para>
/// Stems: <c>drums</c> (kick + snare), <c>hats</c>, <c>bass</c>, <c>lead</c>
/// (the melody), <c>pad</c> (sustained chords) and <c>click</c> (metronome).
/// The chord progression is Am - F - C - G, one chord per bar.
/// </para>
/// <para>
/// Styles: <b>calm</b> (pause: pad, soft hats, long bass notes, no drums),
/// <b>normal</b> (1x: kick on 1 and 3, eighth-note hats and arpeggio) and
/// <b>driving</b> (2x/3x: four-on-the-floor kick, sixteenth hats and arpeggio).
/// </para>
/// </remarks>
public static class PlaceholderMusic
{
    public const int BeatsPerBar = 4;
    public const int LoopBars = 4;

    public static readonly string[] StemNames = ["drums", "hats", "bass", "lead", "pad", "click"];

    // One chord per bar: (bass note, chord notes) as MIDI note numbers.
    private static readonly (int Bass, int[] Chord)[] Progression =
    [
        (45, [57, 60, 64]), // A minor
        (41, [53, 57, 60]), // F major
        (48, [60, 64, 67]), // C major
        (43, [55, 59, 62]), // G major
    ];

    public static MusicTrack Create(IEnumerable<SpeedLevel> levels, int sampleRate, ulong seed = 1)
    {
        var versions = new Dictionary<string, MusicVersion>();
        var random = new DeterministicRandom(seed);

        foreach (SpeedLevel level in levels)
        {
            // Each version gets its own noise stream, so adding a level never changes the others.
            var synth = new Synth(sampleRate, random.Fork(level.Name));
            versions[level.Name] = CreateVersion(level, sampleRate, synth);
        }

        return new MusicTrack("placeholder", versions);
    }

    private static MusicVersion CreateVersion(SpeedLevel level, int sampleRate, Synth synth)
    {
        double framesPerBeat = 60.0 * sampleRate / level.Bpm;
        double secondsPerBeat = 60.0 / level.Bpm;
        int loopFrames = (int)Math.Round(LoopBars * BeatsPerBar * framesPerBeat);
        var stems = StemNames.ToDictionary(name => name, _ => new float[loopFrames]);

        // Converts "bar 2, beat 1.5" into a sample position in the loop.
        int At(int bar, double beat) => (int)Math.Round((bar * BeatsPerBar + beat) * framesPerBeat);

        for (int bar = 0; bar < LoopBars; bar++)
        {
            var (bassNote, chord) = Progression[bar % Progression.Length];

            for (int beat = 0; beat < BeatsPerBar; beat++)
            {
                synth.Click(stems["click"], At(bar, beat), downbeat: beat == 0, amplitude: 0.5f);
            }

            switch (level.Style)
            {
                case "calm":
                    WriteCalm(synth, stems, bar, bassNote, chord, secondsPerBeat, At);
                    break;
                case "driving":
                    WriteDriving(synth, stems, bar, bassNote, chord, secondsPerBeat, At);
                    break;
                default:
                    WriteNormal(synth, stems, bar, bassNote, chord, secondsPerBeat, At);
                    break;
            }
        }

        return new MusicVersion(level.Name, level.Bpm, BeatsPerBar, LoopBars, sampleRate, stems);
    }

    private static void WriteNormal(Synth synth, Dictionary<string, float[]> stems, int bar, int bassNote, int[] chord,
        double secondsPerBeat, Func<int, double, int> at)
    {
        synth.Kick(stems["drums"], at(bar, 0), 0.9f);
        synth.Kick(stems["drums"], at(bar, 2), 0.9f);
        synth.Snare(stems["drums"], at(bar, 1), 0.45f);
        synth.Snare(stems["drums"], at(bar, 3), 0.45f);

        for (int eighth = 0; eighth < 8; eighth++)
        {
            synth.HiHat(stems["hats"], at(bar, eighth * 0.5), eighth % 2 == 0 ? 0.35f : 0.2f);

            int note = chord[eighth % chord.Length] + 12;
            synth.Pluck(stems["lead"], at(bar, eighth * 0.5), Synth.NoteFrequency(note), secondsPerBeat * 0.9, 0.12f);
        }

        for (int beat = 0; beat < BeatsPerBar; beat++)
        {
            synth.Tone(stems["bass"], at(bar, beat), Synth.NoteFrequency(bassNote), secondsPerBeat * 0.8, 0.3f, harmonics: 0.5);
        }

        WritePad(synth, stems, bar, chord, secondsPerBeat, at, 0.04f);
    }

    private static void WriteDriving(Synth synth, Dictionary<string, float[]> stems, int bar, int bassNote, int[] chord,
        double secondsPerBeat, Func<int, double, int> at)
    {
        for (int beat = 0; beat < BeatsPerBar; beat++)
        {
            synth.Kick(stems["drums"], at(bar, beat), 0.9f);
        }
        synth.Kick(stems["drums"], at(bar, 3.5), 0.6f);
        synth.Snare(stems["drums"], at(bar, 1), 0.5f);
        synth.Snare(stems["drums"], at(bar, 3), 0.5f);

        for (int sixteenth = 0; sixteenth < 16; sixteenth++)
        {
            synth.HiHat(stems["hats"], at(bar, sixteenth * 0.25), sixteenth % 2 == 0 ? 0.3f : 0.15f, decaySeconds: 0.03);

            int note = chord[sixteenth % chord.Length] + (sixteenth % 8 < 4 ? 12 : 24);
            synth.Pluck(stems["lead"], at(bar, sixteenth * 0.25), Synth.NoteFrequency(note), secondsPerBeat * 0.5, 0.09f);
        }

        for (int eighth = 0; eighth < 8; eighth++)
        {
            int note = eighth % 2 == 0 ? bassNote : bassNote + 12;
            synth.Tone(stems["bass"], at(bar, eighth * 0.5), Synth.NoteFrequency(note), secondsPerBeat * 0.45, 0.28f, harmonics: 0.6);
        }

        WritePad(synth, stems, bar, chord, secondsPerBeat, at, 0.04f);
    }

    private static void WriteCalm(Synth synth, Dictionary<string, float[]> stems, int bar, int bassNote, int[] chord,
        double secondsPerBeat, Func<int, double, int> at)
    {
        // Pause music: the "tsk... tsk... tsk..." that keeps time while everything waits.
        for (int beat = 0; beat < BeatsPerBar; beat++)
        {
            synth.HiHat(stems["hats"], at(bar, beat), 0.15f, decaySeconds: 0.04);
        }

        synth.Tone(stems["bass"], at(bar, 0), Synth.NoteFrequency(bassNote), secondsPerBeat * BeatsPerBar * 0.95, 0.2f,
            harmonics: 0.2, attackSeconds: 0.1, releaseSeconds: 0.5);

        WritePad(synth, stems, bar, chord, secondsPerBeat, at, 0.09f);
    }

    // Sustained chord across the whole bar, fading in and out softly.
    private static void WritePad(Synth synth, Dictionary<string, float[]> stems, int bar, int[] chord,
        double secondsPerBeat, Func<int, double, int> at, float amplitude)
    {
        foreach (int note in chord)
        {
            synth.Tone(stems["pad"], at(bar, 0), Synth.NoteFrequency(note), secondsPerBeat * BeatsPerBar, amplitude,
                harmonics: 0.1, attackSeconds: secondsPerBeat * 0.5, releaseSeconds: secondsPerBeat);
        }
    }
}
