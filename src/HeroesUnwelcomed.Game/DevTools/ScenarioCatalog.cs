using System.Text.Json;
using HeroesUnwelcomed.Game.Data;
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

/// <summary>Loads every scenario file from the data/scenarios folder.</summary>
public sealed class ScenarioCatalog
{
    public const string FolderName = "scenarios";

    private ScenarioCatalog(string folder, IReadOnlyList<Scenario> scenarios)
    {
        Folder = folder;
        All = scenarios;
    }

    public IReadOnlyList<Scenario> All { get; }

    public string Folder { get; }

    public static ScenarioCatalog Load(DataSource data)
    {
        string folder = data.PathOf(FolderName);
        if (!Directory.Exists(folder))
        {
            return new ScenarioCatalog(folder, []);
        }

        var scenarios = Directory.GetFiles(folder, "*.json")
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(LoadFile)
            .ToList();

        return new ScenarioCatalog(folder, scenarios);
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
        ScenarioFile file = JsonSerializer.Deserialize<ScenarioFile>(File.ReadAllText(path), DataSource.JsonOptions)
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
