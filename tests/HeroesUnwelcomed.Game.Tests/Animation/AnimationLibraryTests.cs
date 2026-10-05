using HeroesUnwelcomed.Game.Animation;

namespace HeroesUnwelcomed.Game.Tests.Animation;

public class AnimationLibraryTests
{
    private static string Entity(string clips, string color = "#112233") => $$"""
    { "entities": { "Bat": { "color": "{{color}}", "size": [100, 100], "clips": { {{clips}} } } } }
    """;

    [Fact]
    public void KeepsClipOrderFromFile()
    {
        var library = AnimationLibrary.Parse(Entity("""
            "Idle": {}, "Zap": { "loop": false }, "Angry": {}
            """));

        Assert.Equal(["Idle", "Zap", "Angry"], library.Entity("Bat").Clips.Select(c => c.Name));
    }

    [Fact]
    public void AppliesDefaults()
    {
        ClipDefinition idle = AnimationLibrary.Parse(Entity("\"Idle\": {}")).Entity("Bat").Idle;

        Assert.Equal(3, idle.Frames);
        Assert.Equal(1.0, idle.BeatsPerFrame);
        Assert.True(idle.Loop);
        Assert.Equal(AnimationClock.Music, idle.Clock);
    }

    [Theory]
    [InlineData("\"Walk\": {}", "needs an 'Idle'")]
    [InlineData("\"Idle\": { \"then\": \"Nope\" }", "doesn't exist")]
    [InlineData("\"Idle\": { \"frames\": 0 }", "at least 1")]
    [InlineData("\"Idle\": { \"clock\": \"wall\" }", "'music' or 'sim'")]
    [InlineData("\"Idle\": { \"rate\": [[2, 1], [1, 1]] }", "increasing")]
    public void BadData_GivesHelpfulMessage(string clips, string expectedMessagePart)
    {
        var error = Assert.Throws<InvalidDataException>(() => AnimationLibrary.Parse(Entity(clips)));

        Assert.Contains(expectedMessagePart, error.Message);
    }

    [Fact]
    public void BadColour_GivesHelpfulMessage()
    {
        var error = Assert.Throws<InvalidDataException>(() => AnimationLibrary.Parse(Entity("\"Idle\": {}", color: "red")));

        Assert.Contains("#3D7BD9", error.Message);
    }

    [Fact]
    public void RateCurve_InterpolatesBetweenPoints()
    {
        ClipDefinition clip = AnimationLibrary.Parse(Entity("""
            "Idle": { "rate": [[0, 0], [2, 1]] }
            """)).Entity("Bat").Idle;

        Assert.Equal(0.0, clip.RateAt(0));
        Assert.Equal(0.5, clip.RateAt(1), 9);
        Assert.Equal(1.0, clip.RateAt(5));
    }

    [Fact]
    public void ShippedPlaceholderFile_IsValid()
    {
        // Guards the real data file: a typo there would otherwise only show up in-game.
        string path = Path.Combine(RepoRoot(), "data", AnimationLibrary.FilePath);

        var library = AnimationLibrary.Parse(File.ReadAllText(path));

        Assert.Contains(library.Entities, e => e.Name == "Hero");
    }

    [Fact]
    public void ShippedAnimations_FitTheirChoreographySteps()
    {
        // Guards the real data: every step clip fills its step exactly, so it ends on
        // the beat its step ends (e.g. Death React finishes before the line steps up).
        string data = Path.Combine(RepoRoot(), "data");
        var library = AnimationLibrary.Parse(File.ReadAllText(Path.Combine(data, AnimationLibrary.FilePath)));
        var tuning = HeroesUnwelcomed.Core.Definitions.GameDefinitions.Parse(
            File.ReadAllText(Path.Combine(data, "game", "rooms.json")),
            File.ReadAllText(Path.Combine(data, "game", "tuning.json")),
            File.ReadAllText(Path.Combine(data, "game", "heroes.json"))).Tuning;

        Assert.Empty(HeroesUnwelcomed.Game.Play.ChoreographyCheck.Problems(library, tuning));
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
