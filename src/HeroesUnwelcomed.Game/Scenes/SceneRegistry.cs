using HeroesUnwelcomed.Game.Play;

namespace HeroesUnwelcomed.Game.Scenes;

/// <summary>
/// Every scene the game knows about, by name. Scenario files refer to scenes by
/// these names ("scene": "play"), so adding a scene is one line here. A scenario
/// can also pass the scene text parameters (e.g. which starting setup to use).
/// </summary>
public static class SceneRegistry
{
    public const string DefaultScene = "play";

    private static readonly IReadOnlyDictionary<string, string> NoParameters = new Dictionary<string, string>();

    private static readonly Dictionary<string, Func<GameServices, IReadOnlyDictionary<string, string>, IScene>> Factories = new(StringComparer.OrdinalIgnoreCase)
    {
        ["play"] = (services, parameters) => new PlayScene(services, parameters),
        ["sandbox"] = (services, _) => new SandboxScene(services),
        ["ui-layout"] = (services, _) => new UiLayoutScene(services),
        ["animation-gallery"] = (services, _) => new AnimationGalleryScene(services),
    };

    public static IEnumerable<string> Names => Factories.Keys;

    public static IScene Create(string name, GameServices services, IReadOnlyDictionary<string, string>? parameters = null)
    {
        if (!Factories.TryGetValue(name, out var factory))
        {
            throw new ArgumentException($"No scene named '{name}'. Known scenes: {string.Join(", ", Names)}.");
        }
        return factory(services, parameters ?? NoParameters);
    }
}
