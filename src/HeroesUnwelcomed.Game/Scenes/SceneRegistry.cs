namespace HeroesUnwelcomed.Game.Scenes;

/// <summary>
/// Every scene the game knows about, by name. Scenario files refer to scenes by
/// these names ("scene": "sandbox"), so adding a scene is one line here.
/// </summary>
public static class SceneRegistry
{
    public const string DefaultScene = "sandbox";

    private static readonly Dictionary<string, Func<GameServices, IScene>> Factories = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sandbox"] = services => new SandboxScene(services),
        ["ui-layout"] = services => new UiLayoutScene(services),
    };

    public static IEnumerable<string> Names => Factories.Keys;

    public static IScene Create(string name, GameServices services)
    {
        if (!Factories.TryGetValue(name, out var factory))
        {
            throw new ArgumentException($"No scene named '{name}'. Known scenes: {string.Join(", ", Names)}.");
        }
        return factory(services);
    }
}
