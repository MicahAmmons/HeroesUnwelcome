namespace HeroesUnwelcomed.Audio.Music;

/// <summary>
/// One game-speed setting (Paused, 1x, 2x, 3x) and the music that goes with it.
/// Loaded from data/audio/speeds.json.
/// </summary>
/// <param name="Name">Shown to the player; also the name of the music version that plays.</param>
/// <param name="Bpm">Music tempo at this speed.</param>
/// <param name="SimRate">
/// Game beats per music beat. 0 freezes the game (pause). Whole numbers keep game
/// events landing on musical beats: at 2, every music beat carries two game beats,
/// so actions land on half-beats (eighth notes).
/// </param>
/// <param name="Style">Which arrangement the placeholder music generator uses: calm, normal or driving.</param>
/// <remarks>
/// Effective game speed = Bpm × SimRate. The defaults give exact 1x/2x/3x:
/// 100 bpm × 1 = 100, 100 × 2 = 200, 150 × 2 = 300 game beats per minute.
/// </remarks>
public sealed record SpeedLevel(string Name, double Bpm, double SimRate, string Style)
{
    public bool IsPause => SimRate == 0;

    /// <summary>Game beats per minute: how fast the game actually runs.</summary>
    public double GameBeatsPerMinute => Bpm * SimRate;
}
