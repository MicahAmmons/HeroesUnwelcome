using HeroesUnwelcomed.Game.Input;
using Microsoft.Xna.Framework;

namespace HeroesUnwelcomed.Game.DevTools;

/// <summary>
/// Owns every developer tool (debug panel, scenarios, screenshots) and plugs
/// them into the game. Only compiled in Debug builds.
/// </summary>
/// <remarks>
/// Compare with the old static Debug class: there, gameplay code pushed values
/// into Debug (CellManager called Debug.UpdateHoveredCell...). Here the
/// dependency only points one way. Dev tools read the game and call the same
/// public methods real features use, and gameplay code never mentions dev
/// tools. Deleting this folder would not break the game.
/// </remarks>
public sealed class DevToolsHost : IDisposable
{
    // Size multiplier for the panel's text and spacing (readable on high-DPI laptops).
    private const float PanelScale = 1.25f;

    private readonly HeroesGame _game;
    private readonly ImGuiRenderer _imgui;
    private readonly DebugPanel _panel;
    private readonly bool _screenshotIncludesPanel;
    private readonly DataWatcher _watcher;

    public DevToolsHost(HeroesGame game, LaunchOptions options)
    {
        _game = game;
        Scenarios = ScenarioCatalog.Load(game.SharedServices.Data);
        _watcher = new DataWatcher(game.SharedServices.Data);
        game.SharedServices.Data.FileChanged += OnDataFileChanged;
        Screenshots = new ScreenshotCapture(options);
        _imgui = new ImGuiRenderer(game, PanelScale);
        _panel = new DebugPanel(this, game);

        if (options.Scenario is { } name)
        {
            CurrentScenario = Scenarios.Get(name);
            StartingScene = CurrentScenario.Scene;
        }

        // Show the panel by default in normal debug runs, but not in automated
        // screenshot runs (unless the screenshots are meant to include it).
        _screenshotIncludesPanel = options.ScreenshotIncludesDevTools;
        PanelVisible = options.ScreenshotFrames.Count == 0 || _screenshotIncludesPanel;
    }

    public ScenarioCatalog Scenarios { get; private set; }
    public DataWatcher Watcher => _watcher;
    public ScreenshotCapture Screenshots { get; }
    public Scenario? CurrentScenario { get; private set; }
    public bool PanelVisible { get; set; }

    /// <summary>The scene the launch scenario asked for, if any.</summary>
    public string? StartingScene { get; }

    /// <summary>Whether the panel is currently using the keyboard/mouse (so the game should ignore them).</summary>
    public (bool Keyboard, bool Mouse) InputCapture =>
        PanelVisible ? (_imgui.WantsKeyboard, _imgui.WantsMouse) : (false, false);

    public void Update(GameTime gameTime)
    {
        _watcher.Update();

        InputService input = _game.Input;

        if (input.WasPressed(GameAction.ToggleDebugPanel))
        {
            PanelVisible = !PanelVisible;
        }

        if (input.WasPressed(GameAction.Screenshot))
        {
            Screenshots.RequestManual();
        }
    }

    public void Draw(GameTime gameTime)
    {
        // Normally the screenshot is taken first, so it shows the game without the panel.
        if (!_screenshotIncludesPanel)
        {
            CaptureScreenshotIfDue();
        }

        if (PanelVisible)
        {
            _imgui.BeforeLayout(gameTime);
            _panel.Layout(gameTime);
            _imgui.AfterLayout();
        }

        if (_screenshotIncludesPanel)
        {
            CaptureScreenshotIfDue();
        }
    }

    private void CaptureScreenshotIfDue()
    {
        string label = CurrentScenario?.Name ?? _game.CurrentSceneName;
        if (Screenshots.AfterGameDrawn(_game.GraphicsDevice, label))
        {
            _game.Exit();
        }
    }

    public void LoadScenario(Scenario scenario)
    {
        CurrentScenario = scenario;
        _game.SwitchScene(scenario.Scene);
    }

    /// <summary>Starts the current scene over from scratch.</summary>
    public void Restart() => _game.SwitchScene(_game.CurrentSceneName);

    /// <summary>Re-reads the scenarios folder (picks up newly added files).</summary>
    public void RefreshScenarios()
    {
        Scenarios = ScenarioCatalog.Load(_game.SharedServices.Data);
        if (CurrentScenario is { } current)
        {
            CurrentScenario = Scenarios.All.FirstOrDefault(s => s.Name == current.Name);
        }
    }

    private void OnDataFileChanged(string path)
    {
        if (path.StartsWith(ScenarioCatalog.FolderName + "/", StringComparison.Ordinal))
        {
            RefreshScenarios();
        }
    }

    public void Dispose()
    {
        _game.SharedServices.Data.FileChanged -= OnDataFileChanged;
        _watcher.Dispose();
        _imgui.Dispose();
    }
}
