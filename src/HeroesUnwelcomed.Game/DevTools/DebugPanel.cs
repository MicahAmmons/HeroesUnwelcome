using HeroesUnwelcomed.Game.Input;
using ImGuiNET;
using Microsoft.Xna.Framework;
using NumVector2 = System.Numerics.Vector2;

namespace HeroesUnwelcomed.Game.DevTools;

/// <summary>
/// The "Dev Tools" window. Each section is a plain method of ImGui calls;
/// adding a tool means adding a few lines here.
/// </summary>
public sealed class DebugPanel
{
    private static readonly (string Label, int Width, int Height)[] WindowPresets =
    [
        ("Steam Deck 1280x800", 1280, 800),
        ("720p", 1280, 720),
        ("1600x900", 1600, 900),
        ("1080p", 1920, 1080),
    ];

    private readonly DevToolsHost _host;
    private readonly HeroesGame _game;
    private float _smoothedFrameSeconds = 1f / 60f;

    public DebugPanel(DevToolsHost host, HeroesGame game)
    {
        _host = host;
        _game = game;
    }

    public void Layout(GameTime gameTime)
    {
        ImGui.SetNextWindowPos(new NumVector2(16, 16), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new NumVector2(460, 640), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Dev Tools  (F1 / Back to hide)"))
        {
            DrawStats(gameTime);
            DrawScenarios();
            DrawDisplay();
            DrawInput();
        }
        ImGui.End();
    }

    private void DrawStats(GameTime gameTime)
    {
        // Smooth the frame time so the number is readable instead of flickering.
        float seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _smoothedFrameSeconds += (seconds - _smoothedFrameSeconds) * 0.05f;

        ImGui.Text($"{1f / _smoothedFrameSeconds:0} FPS   {_smoothedFrameSeconds * 1000f:0.0} ms/frame");
        ImGui.Text($"Scene: {_game.CurrentSceneName}");
        if (_host.CurrentScenario is { } scenario)
        {
            ImGui.Text($"Scenario: {scenario.Name}");
            ImGui.TextWrapped(scenario.Description);
        }
        ImGui.Separator();
    }

    private void DrawScenarios()
    {
        if (!ImGui.CollapsingHeader("Scenarios", ImGuiTreeNodeFlags.DefaultOpen))
        {
            return;
        }

        foreach (Scenario scenario in _host.Scenarios.All)
        {
            if (ImGui.Selectable(scenario.Name, scenario == _host.CurrentScenario))
            {
                _host.LoadScenario(scenario);
            }
            if (ImGui.IsItemHovered() && scenario.Description.Length > 0)
            {
                ImGui.SetTooltip(scenario.Description);
            }
        }

        if (ImGui.Button("Restart scene"))
        {
            _host.Restart();
        }
    }

    private void DrawDisplay()
    {
        if (!ImGui.CollapsingHeader("Display", ImGuiTreeNodeFlags.DefaultOpen))
        {
            return;
        }

        var viewport = _game.GraphicsDevice.Viewport;
        ImGui.Text($"Window: {viewport.Width}x{viewport.Height}");
        ImGui.Text($"UI scale: {_game.Ui.Scale:0.000}   canvas: {_game.Ui.Size.X:0}x{_game.Ui.Size.Y:0}");

        foreach (var (label, width, height) in WindowPresets)
        {
            if (ImGui.Button(label))
            {
                _game.SetWindowSize(width, height);
            }
            ImGui.SameLine();
        }
        ImGui.NewLine();

        bool fullscreen = _game.IsFullscreen;
        if (ImGui.Checkbox("Fullscreen (F11)", ref fullscreen))
        {
            _game.SetFullscreen(fullscreen);
        }

        if (ImGui.Button("Screenshot (F12)"))
        {
            _host.Screenshots.RequestManual();
        }
        if (_host.Screenshots.LastSavedPath is { } path)
        {
            ImGui.TextWrapped($"Saved: {path}");
        }
    }

    private void DrawInput()
    {
        if (!ImGui.CollapsingHeader("Input"))
        {
            return;
        }

        InputService input = _game.Input;
        ImGui.Text($"Active device: {input.ActiveDevice}");
        ImGui.Text($"Pan: {input.Pan.X:0.00}, {input.Pan.Y:0.00}   Zoom: {input.Zoom:0.00}");
        ImGui.Text($"Pointer: {input.PointerPosition.X}, {input.PointerPosition.Y}");

        foreach (GameAction action in Enum.GetValues<GameAction>())
        {
            bool down = input.IsDown(action);
            ImGui.TextColored(
                down ? new System.Numerics.Vector4(0.4f, 1f, 0.4f, 1f) : new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1f),
                $"{action}: {(down ? "DOWN" : "-")}");
        }
    }
}
