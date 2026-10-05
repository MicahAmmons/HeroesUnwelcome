using System.Reflection;
using System.Text.Json;

namespace HeroesUnwelcomed.Game.Data;

/// <summary>
/// Where the game's JSON data files are read from, plus a notification when one changes.
/// </summary>
/// <remarks>
/// <para>
/// Release builds read the copy of <c>data/</c> placed next to the exe.
/// Debug builds read straight from the repository's <c>data/</c> folder (its path
/// is baked in at build time), so editing a JSON file in Visual Studio affects
/// the running game without rebuilding.
/// </para>
/// <para>
/// <see cref="FileChanged"/> is raised by the dev tools' file watcher. Anything
/// that loaded a file can listen and reload it. In Release builds it never fires.
/// </para>
/// </remarks>
public sealed class DataSource
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public DataSource(string root)
    {
        Root = root;
    }

    /// <summary>Folder containing the data files.</summary>
    public string Root { get; }

    /// <summary>Raised with a path relative to <see cref="Root"/>, using forward slashes (e.g. "animations/placeholders.json").</summary>
    public event Action<string>? FileChanged;

    public static DataSource CreateDefault()
    {
        string? sourceFolder = Assembly.GetEntryAssembly()?
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "SourceDataDirectory")?.Value;

        if (sourceFolder is not null && Directory.Exists(sourceFolder))
        {
            return new DataSource(Path.GetFullPath(sourceFolder));
        }
        return new DataSource(Path.Combine(AppContext.BaseDirectory, "Data"));
    }

    public string PathOf(string relativePath) => Path.Combine(Root, relativePath);

    public bool Exists(string relativePath) => File.Exists(PathOf(relativePath));

    public string ReadText(string relativePath) => File.ReadAllText(PathOf(relativePath));

    public T ReadJson<T>(string relativePath) =>
        JsonSerializer.Deserialize<T>(ReadText(relativePath), JsonOptions)
        ?? throw new InvalidDataException($"Data file '{relativePath}' is empty.");

    /// <summary>Tells listeners a file changed. Called by the dev tools' watcher.</summary>
    public void NotifyChanged(string relativePath) => FileChanged?.Invoke(relativePath.Replace('\\', '/'));
}
