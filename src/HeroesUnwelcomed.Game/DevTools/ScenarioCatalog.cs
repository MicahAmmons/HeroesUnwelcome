using System.Text.Json;
using HeroesUnwelcomed.Game.Scenes;

namespace HeroesUnwelcomed.Game.DevTools;

/// <summary>
/// A ready-made situation to launch straight into, defined by a JSON file in
/// data/scenarios. The file name is the scenario's name.
/// </summary>
/// <remarks>
/// For now a scenario only picks a scene. Once game logic exists (M2), scenarios
/// will also describe the dungeon layout, hero pool, seed and starting state:
/// "two parties queued at a trap room", "a hero one win from specializing"...
/// </remarks>
public sealed record Scenario(string Name, string Description, string Scene);

/// <summary>Loads every scenario file from the Data/scenarios folder next to the game.</summary>
public sealed class ScenarioCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private ScenarioCatalog(IReadOnlyList<Scenario> scenarios)
    {
        All = scenarios;
    }

    public IReadOnlyList<Scenario> All { get; }

    public static string Folder => Path.Combine(AppContext.BaseDirectory, "Data", "scenarios");

    public static ScenarioCatalog Load()
    {
        if (!Directory.Exists(Folder))
        {
            return new ScenarioCatalog([]);
        }

        var scenarios = Directory.GetFiles(Folder, "*.json")
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(LoadFile)
            .ToList();

        return new ScenarioCatalog(scenarios);
    }

    public Scenario Get(string name)
    {
        return All.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException(
                $"No scenario named '{name}' in {Folder}. Available: {string.Join(", ", All.Select(s => s.Name))}.");
    }

    private static Scenario LoadFile(string path)
    {
        string name = Path.GetFileNameWithoutExtension(path);
        ScenarioFile file = JsonSerializer.Deserialize<ScenarioFile>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidDataException($"Scenario file '{path}' is empty.");
        return new Scenario(name, file.Description, file.Scene);
    }

    // The shape of the JSON on disk.
    private sealed class ScenarioFile
    {
        public string Description { get; set; } = "";
        public string Scene { get; set; } = SceneRegistry.DefaultScene;
    }
}
