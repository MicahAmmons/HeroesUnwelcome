using System.Numerics;
using HeroesUnwelcomed.Audio.Music;
using HeroesUnwelcomed.Game.Data;
using HeroesUnwelcomed.Game.Sound;

namespace HeroesUnwelcomed.Game.Tests.Sound;

public class ProximityTests
{
    private static readonly ProximitySettings Settings = new(ZoomedOut: 0.15f, ZoomedIn: 0.45f, MinSoundVolume: 0.2f, PanWidth: 0.8f);

    [Fact]
    public void ZoomedOut_NoPartyHasInfluence()
    {
        Assert.Equal(0f, Proximity.Influence(0.12f, Vector2.Zero, Settings));
    }

    [Fact]
    public void ZoomedIn_CentredParty_HasFullInfluence()
    {
        Assert.Equal(1f, Proximity.Influence(0.6f, Vector2.Zero, Settings));
    }

    [Fact]
    public void InfluenceRisesSmoothlyWithZoom()
    {
        float halfway = Proximity.Influence(0.30f, Vector2.Zero, Settings);

        Assert.Equal(0.5f, halfway, 3); // smoothstep's midpoint
        Assert.True(Proximity.Influence(0.25f, Vector2.Zero, Settings) < halfway);
    }

    [Fact]
    public void FarFromCentre_LosesInfluence()
    {
        float centre = Proximity.Influence(0.6f, Vector2.Zero, Settings);
        float edge = Proximity.Influence(0.6f, new Vector2(1f, 0f), Settings);
        float offScreen = Proximity.Influence(0.6f, new Vector2(2f, 0f), Settings);

        Assert.True(edge < centre && edge > 0f);
        Assert.Equal(0f, offScreen);
    }

    [Fact]
    public void Sounds_NeverFallBelowTheMinimum_AndPanBySide()
    {
        Assert.Equal(0.2f, Proximity.SoundVolume(0f, Settings));
        Assert.Equal(1f, Proximity.SoundVolume(1f, Settings));
        Assert.Equal(-0.8f, Proximity.Pan(new Vector2(-3f, 0f), Settings));
        Assert.Equal(0.4f, Proximity.Pan(new Vector2(0.5f, 0f), Settings), 4);
    }

    [Fact]
    public void ShippedReactions_LoadAndPointAtRealSounds()
    {
        var data = new DataSource(Path.Combine(RepoRoot(), "data"));
        ReactionSettings reactions = ReactionSettings.Load(data);
        var sounds = PlaceholderSounds.Create(48_000, 100);

        Assert.Equal(MusicMode.Local, reactions.DefaultMode);
        Assert.True(reactions.MusicReactions["heroDied"].Stems["drums"] > 0);
        Assert.All(reactions.Sounds.Values, name => Assert.True(sounds.ContainsKey(name), $"No sound named '{name}'."));
        Assert.All(reactions.MusicReactions.Values.Where(r => r.Stinger is not null),
            r => Assert.True(sounds.ContainsKey(r.Stinger!), $"No stinger named '{r.Stinger}'."));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "HeroesUnwelcomed.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("Couldn't find the repository root.");
    }
}
