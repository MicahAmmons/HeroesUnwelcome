using System.Globalization;

namespace HeroesUnwelcomed.Game;

/// <summary>
/// Settings passed on the command line when the game starts. Visual Studio's
/// launch profiles (the dropdown next to the Run button) fill these in.
/// </summary>
/// <remarks>
/// Supported arguments:
/// <code>
/// --scenario &lt;name&gt;           start inside data/scenarios/&lt;name&gt;.json (debug builds)
/// --window &lt;W&gt;x&lt;H&gt;            initial window size, e.g. 1280x800
/// --fullscreen                  start in borderless fullscreen
/// --screenshot-frames 5,60      save a PNG after drawing these frame numbers (debug builds)
/// --screenshot-dir &lt;path&gt;      where screenshots go (default: Screenshots next to the exe)
/// --exit-after-screenshots      quit once the last requested screenshot is saved
/// --screenshot-devtools         include the dev tools panel in screenshots (debug builds)
/// </code>
/// </remarks>
public sealed record LaunchOptions
{
    public string? Scenario { get; init; }
    public int WindowWidth { get; init; } = 1600;
    public int WindowHeight { get; init; } = 900;
    public bool StartFullscreen { get; init; }
    public IReadOnlyList<int> ScreenshotFrames { get; init; } = [];
    public string? ScreenshotDirectory { get; init; }
    public bool ExitAfterScreenshots { get; init; }
    public bool ScreenshotIncludesDevTools { get; init; }

    public static LaunchOptions Parse(IReadOnlyList<string> args)
    {
        var options = new LaunchOptions();

        for (int i = 0; i < args.Count; i++)
        {
            string arg = args[i];
            options = arg switch
            {
                "--scenario" => options with { Scenario = ValueAfter(args, ref i) },
                "--window" => ParseWindow(options, ValueAfter(args, ref i)),
                "--fullscreen" => options with { StartFullscreen = true },
                "--screenshot-frames" => options with { ScreenshotFrames = ParseFrames(ValueAfter(args, ref i)) },
                "--screenshot-dir" => options with { ScreenshotDirectory = ValueAfter(args, ref i) },
                "--exit-after-screenshots" => options with { ExitAfterScreenshots = true },
                "--screenshot-devtools" => options with { ScreenshotIncludesDevTools = true },
                _ => throw new ArgumentException($"Unknown launch argument '{arg}'."),
            };
        }

        return options;
    }

    private static string ValueAfter(IReadOnlyList<string> args, ref int i)
    {
        if (i + 1 >= args.Count)
        {
            throw new ArgumentException($"Launch argument '{args[i]}' needs a value after it.");
        }
        i++;
        return args[i];
    }

    private static LaunchOptions ParseWindow(LaunchOptions options, string value)
    {
        string[] parts = value.ToLowerInvariant().Split('x');
        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int width)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int height))
        {
            throw new ArgumentException($"--window expects a size like 1280x800, got '{value}'.");
        }
        return options with { WindowWidth = width, WindowHeight = height };
    }

    private static int[] ParseFrames(string value)
    {
        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => int.Parse(part, CultureInfo.InvariantCulture))
            .Order()
            .ToArray();
    }
}
