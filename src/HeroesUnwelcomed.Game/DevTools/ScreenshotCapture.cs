using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HeroesUnwelcomed.Game.DevTools;

/// <summary>
/// Saves what's on screen to a PNG, either when F12 is pressed or automatically
/// on specific frame numbers (from --screenshot-frames).
/// </summary>
/// <remarks>
/// The automatic mode is what lets Claude check visuals without seeing your
/// screen: launch a scenario at a given window size, capture a few frames, quit,
/// then look at the images.
/// </remarks>
public sealed class ScreenshotCapture
{
    private readonly Queue<int> _scheduledFrames;
    private readonly bool _exitWhenDone;
    private readonly string _directory;
    private int _frame;
    private bool _manualRequested;

    public ScreenshotCapture(LaunchOptions options)
    {
        _scheduledFrames = new Queue<int>(options.ScreenshotFrames);
        _exitWhenDone = options.ExitAfterScreenshots && _scheduledFrames.Count > 0;
        _directory = options.ScreenshotDirectory ?? Path.Combine(AppContext.BaseDirectory, "Screenshots");
    }

    public string? LastSavedPath { get; private set; }

    public void RequestManual() => _manualRequested = true;

    /// <summary>
    /// Call once per frame, after the game has drawn but before the debug panel,
    /// so screenshots show what a player would see.
    /// </summary>
    /// <returns>True when all scheduled screenshots are done and the game should quit.</returns>
    public bool AfterGameDrawn(GraphicsDevice device, string label)
    {
        _frame++;

        bool scheduled = _scheduledFrames.Count > 0 && _scheduledFrames.Peek() <= _frame;
        if (scheduled)
        {
            _scheduledFrames.Dequeue();
        }

        if (scheduled || _manualRequested)
        {
            _manualRequested = false;
            Save(device, label);
        }

        return _exitWhenDone && _scheduledFrames.Count == 0;
    }

    private void Save(GraphicsDevice device, string label)
    {
        int width = device.PresentationParameters.BackBufferWidth;
        int height = device.PresentationParameters.BackBufferHeight;

        var pixels = new Color[width * height];
        device.GetBackBufferData(pixels);

        // The back buffer's alpha channel isn't meaningful; force it opaque so
        // image viewers don't show see-through patches.
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i].A = 255;
        }

        Directory.CreateDirectory(_directory);
        string path = Path.Combine(_directory, $"{label}_{width}x{height}_f{_frame:D5}.png");

        using var texture = new Texture2D(device, width, height, false, SurfaceFormat.Color);
        texture.SetData(pixels);
        using FileStream file = File.Create(path);
        texture.SaveAsPng(file, width, height);

        LastSavedPath = path;
    }
}
