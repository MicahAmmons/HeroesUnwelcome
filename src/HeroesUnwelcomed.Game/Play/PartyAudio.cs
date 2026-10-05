using HeroesUnwelcomed.Core.Simulation;
using HeroesUnwelcomed.Game.Display;
using HeroesUnwelcomed.Game.Sound;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NumVector2 = System.Numerics.Vector2;

namespace HeroesUnwelcomed.Game.Play;

/// <summary>
/// The parties' voice in the soundtrack: their influence over the music
/// (from where the camera is looking) and the sounds of their big moments.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every frame</b> each party's influence is worked out from its front
/// hero's position on screen and the camera zoom (<see cref="Proximity"/>) and
/// handed to the mixer. A reaction already in progress gets louder or softer
/// as you move the camera toward or away from it.
/// </para>
/// <para>
/// <b>On events:</b> a death plays the hit, cuts the music (the "heroDied"
/// reaction: cut on the next half-beat, hats keep ticking, a drum fill leads
/// back in on the bar line) and the teammates' gasp lands with the cut. A
/// cleared room cheers; the banner whooshes down. All of this comes from
/// data/audio/reactions.json.
/// </para>
/// </remarks>
public sealed class PartyAudio(Choreographer choreographer, Func<int, HeroView> heroView, AudioEngine audio)
{
    private readonly Dictionary<int, (float Influence, NumVector2 Offset)> _parties = [];

    /// <summary>Latest influence per party (for the debug panel).</summary>
    public IReadOnlyDictionary<int, float> Influence => _parties.ToDictionary(p => p.Key, p => p.Value.Influence);

    public void Update(Camera2D camera, Viewport viewport, double gameBeat)
    {
        ProximitySettings settings = audio.Reactions.Proximity;
        var seen = new HashSet<int>();

        foreach (PartyView party in choreographer.Parties)
        {
            if (party.Line.Count == 0) continue;

            // Measure from the middle of the front hero's body, not their feet.
            HeroView front = heroView(party.Line[0]);
            float halfHeight = front.Animator.Entity.Size.Y * WorldLayout.SpriteScale / 2f;
            Vector2 world = front.PositionAt(gameBeat) - new Vector2(0, halfHeight);
            NumVector2 offset = ScreenOffset(camera, viewport, world);
            float influence = Proximity.Influence(camera.Zoom, offset, settings);

            _parties[party.Id] = (influence, offset);
            audio.Mixer.Augments.SetInfluence(party.Id, influence);
            seen.Add(party.Id);
        }

        // Parties that are gone no longer influence anything.
        foreach (int gone in _parties.Keys.Where(id => !seen.Contains(id)).ToList())
        {
            _parties.Remove(gone);
            audio.Mixer.Augments.RemoveSource(gone);
        }
    }

    public void Handle(GameEvent gameEvent)
    {
        switch (gameEvent)
        {
            case HeroDied died:
            {
                var (volume, pan) = SoundPlacement(died.PartyId);
                Play("heroDied", volume, pan);

                var placed = audio.React(died.PartyId, "heroDied", volume, pan);
                // The teammates gasp exactly as the music cuts.
                Play("deathReact", volume, pan, placed?.StartBeat);
                break;
            }

            case RoomCleared cleared:
            {
                var (volume, pan) = SoundPlacement(cleared.PartyId);
                Play("roomCleared", volume, pan);
                break;
            }

            case PartyStepStarted { Step: PartyPhase.BannerDown } step:
            {
                var (volume, pan) = SoundPlacement(step.PartyId);
                Play("bannerDown", volume, pan);
                break;
            }
        }
    }

    private void Play(string moment, float volume, float pan, double? atMusicBeat = null)
    {
        if (audio.Reactions.Sounds.TryGetValue(moment, out string? sound))
        {
            audio.PlaySound(sound, volume, pan, atMusicBeat);
        }
    }

    private (float Volume, float Pan) SoundPlacement(int partyId)
    {
        ProximitySettings settings = audio.Reactions.Proximity;
        return _parties.TryGetValue(partyId, out var party)
            ? (Proximity.SoundVolume(party.Influence, settings), Proximity.Pan(party.Offset, settings))
            : (settings.MinSoundVolume, 0f);
    }

    // Position relative to the screen centre, as a fraction of half the screen (±1 = the edge).
    private static NumVector2 ScreenOffset(Camera2D camera, Viewport viewport, Vector2 world)
    {
        Vector2 screen = camera.WorldToScreen(world, viewport);
        return new NumVector2(
            (screen.X - viewport.Width / 2f) / (viewport.Width / 2f),
            (screen.Y - viewport.Height / 2f) / (viewport.Height / 2f));
    }
}
