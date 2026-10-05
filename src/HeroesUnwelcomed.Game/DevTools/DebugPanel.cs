using HeroesUnwelcomed.Audio.Mixing;
using HeroesUnwelcomed.Audio.Music;
using HeroesUnwelcomed.Audio.Timing;
using HeroesUnwelcomed.Core.Definitions;
using HeroesUnwelcomed.Core.Heroes;
using HeroesUnwelcomed.Core.Simulation;
using HeroesUnwelcomed.Game.Input;
using HeroesUnwelcomed.Game.Play;
using HeroesUnwelcomed.Game.Sound;
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
        ImGui.SetNextWindowSize(new NumVector2(500, 1000), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Dev Tools  (F1 / Back to hide)"))
        {
            DrawStats(gameTime);
            DrawScenarios();
            DrawAudio();
            DrawRun();
            DrawDisplay();
            DrawInput();
            DrawData();
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

    // Live view of the simulation: heroes, parties, door, souls.
    private void DrawRun()
    {
        if (_game.CurrentScene is not PlayScene play || !ImGui.CollapsingHeader("Run inspector", ImGuiTreeNodeFlags.DefaultOpen))
        {
            return;
        }

        Run run = play.Run;
        ImGui.Text($"Beat {run.Beat:0.0}   door {run.Door}   souls {run.Souls.Balance}   rule {run.Rules.Ordering.Name}");
        if (run.IsOver) ImGui.TextColored(new System.Numerics.Vector4(1f, 0.4f, 0.4f, 1f), "Run over: heroes won");

        if (ImGui.Button("Send party")) Report(run.Execute(new SendNextParty()));
        ImGui.SameLine();
        if (ImGui.Button(run.Door == DoorState.Open ? "Lock door" : "Unlock door"))
        {
            Report(run.Execute(run.Door == DoorState.Open ? new LockDoor() : new UnlockDoor()));
        }
        ImGui.SameLine();
        if (ImGui.Button("New run")) play.StartRun();
        if (_lastCommandResult is { } result) ImGui.TextWrapped(result);

        var problems = play.ChoreographyProblems;
        if (problems.Count > 0)
        {
            ImGui.SeparatorText("Choreography check");
            foreach (string problem in problems) ImGui.TextColored(new System.Numerics.Vector4(1f, 0.7f, 0.3f, 1f), problem);
        }

        ImGui.SeparatorText("Parties");
        foreach (Party party in run.ActiveParties)
        {
            string room = run.Dungeon.Rooms[party.RoomIndex].Kind.Id;
            ImGui.Text($"#{party.Id} {party.Phase} -> {room}: {string.Join(", ", party.Members.Select(h => h.Name))}");
        }

        ImGui.SeparatorText("Heroes");
        if (ImGui.BeginTable("heroes", 7, ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingStretchProp))
        {
            foreach (string column in new[] { "Name", "Class", "State", "Combat", "Trap", "Boss", "Deaths" })
            {
                ImGui.TableSetupColumn(column);
            }
            ImGui.TableHeadersRow();
            foreach (Hero hero in run.Heroes.All)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn(); ImGui.Text(hero.Name);
                ImGui.TableNextColumn(); ImGui.Text(hero.Class?.Id ?? "-");
                ImGui.TableNextColumn(); ImGui.Text(hero.State == HeroState.Dead ? $"Dead ({hero.RespawnAtBeat - run.Beat:0})" : hero.State.ToString());
                ImGui.TableNextColumn(); ImGui.Text($"{hero.BaseSkill(RoomCategory.Combat):0.00}");
                ImGui.TableNextColumn(); ImGui.Text($"{hero.BaseSkill(RoomCategory.Trap):0.00}");
                ImGui.TableNextColumn(); ImGui.Text($"{hero.BaseSkill(RoomCategory.Boss):0.00}");
                ImGui.TableNextColumn(); ImGui.Text(hero.Deaths.ToString());
            }
            ImGui.EndTable();
        }
    }

    private string? _lastCommandResult;

    private void Report(CommandResult result) => _lastCommandResult = result.ToString();

    private void DrawAudio()
    {
        if (!ImGui.CollapsingHeader("Audio & beat", ImGuiTreeNodeFlags.DefaultOpen))
        {
            return;
        }

        AudioEngine audio = _game.SharedServices.Audio;
        BeatTime time = audio.Time;
        AudioOutput output = audio.Output;

        ImGui.Text($"Bar {time.Bar + 1} beat {time.BeatInBar + 1}   music {time.MusicBeat:0.00}   game {time.SimBeat:0.00}");
        ImGui.Text($"Playing: {audio.Mixer.CurrentVersion.Name} @ {audio.Mixer.CurrentVersion.Bpm:0} bpm   pending changes: {audio.Mixer.PendingChanges}");
        ImGui.Text(output.HasDevice
            ? $"Device OK   queued {output.QueuedBuffers}/{AudioOutput.TargetQueuedBuffers}   buffer {AudioOutput.BufferedSeconds * 1000:0} ms   underruns {output.Underruns}"
            : $"NO AUDIO DEVICE (silent clock): {output.FailureReason}");

        // Speed buttons (same as P / [ ] / Start / LB / RB).
        foreach (SpeedLevel level in audio.Levels)
        {
            bool current = level == audio.CurrentLevel;
            if (current) ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.2f, 0.55f, 0.3f, 1f));
            if (ImGui.Button(level.Name)) audio.SetSpeed(level);
            if (current) ImGui.PopStyleColor();
            ImGui.SameLine();
        }
        ImGui.NewLine();

        bool local = audio.Mode == MusicMode.Local;
        if (ImGui.Checkbox("Local music (parties near the camera shape it)", ref local))
        {
            audio.Mode = local ? MusicMode.Local : MusicMode.Global;
        }
        if (_game.CurrentScene is PlayScene play && play.PartyInfluence.Count > 0)
        {
            foreach (var (partyId, influence) in play.PartyInfluence.OrderBy(p => p.Key))
            {
                ImGui.ProgressBar(influence, new NumVector2(220, 0), $"party {partyId}: {influence:P0}");
            }
        }
        ImGui.Text($"Active reactions: {audio.Mixer.Augments.Active.Count}   sounds playing: {audio.Mixer.ActiveSounds}");
        foreach (string sound in audio.Sounds.Keys)
        {
            if (ImGui.SmallButton(sound)) audio.PlaySound(sound, 1f, 0f);
            ImGui.SameLine();
        }
        ImGui.NewLine();

        bool metronome = audio.MetronomeOn;
        if (ImGui.Checkbox("Metronome", ref metronome))
        {
            audio.SetMetronome(metronome);
        }

        float volume = audio.Mixer.MasterGain;
        if (ImGui.SliderFloat("Master volume", ref volume, 0f, 1f))
        {
            audio.Mixer.MasterGain = volume;
        }

        float latency = audio.OutputLatencyMs;
        if (ImGui.SliderFloat("Visual delay (ms)", ref latency, 0f, 200f))
        {
            audio.OutputLatencyMs = latency;
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("If animations look ahead of the sound, raise this until they line up.");
        }

        ImGui.SeparatorText("Stems (mute/unmute on the next beat)");
        foreach (StemChannel channel in audio.Mixer.Channels)
        {
            if (channel.Name == AudioEngine.MetronomeStem)
            {
                continue;
            }
            bool on = channel.TargetGain > 0f;
            if (ImGui.Checkbox(channel.Name, ref on))
            {
                audio.ScheduleStemGain(channel.Name, on ? 1f : 0f, When.NextBeat);
            }
            ImGui.SameLine();
            float reaction = audio.Mixer.AugmentGainOf(channel.Name);
            ImGui.ProgressBar(channel.Gain * reaction, new NumVector2(-1, 0), reaction < 0.99f ? $"reaction {reaction:P0}" : "");
        }

        // Trigger the real "hero died" reaction at full strength, as if zoomed in on a party.
        if (ImGui.Button("Test: hero died reaction"))
        {
            const int testSource = -1;
            audio.Mixer.Augments.SetInfluence(testSource, 1f);
            var placed = audio.React(testSource, "heroDied");
            audio.PlaySound(audio.Reactions.Sounds.GetValueOrDefault("deathReact", "gasp"), 1f, 0f, placed?.StartBeat);
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("The heroDied reaction from data/audio/reactions.json at full strength:\ncut on the next half-beat (hats keep time), gasp, drum fill, return on the bar line.");
        }
    }

    private void DrawData()
    {
        if (!ImGui.CollapsingHeader("Data files"))
        {
            return;
        }

        ImGui.TextWrapped($"Reading from: {_game.SharedServices.Data.Root}");
        if (_host.Watcher.LastReloaded is { } path)
        {
            ImGui.Text($"Last change: {path} at {_host.Watcher.LastReloadedAt:HH:mm:ss}");
        }
        else
        {
            ImGui.TextDisabled("Save a JSON file in data/ and it reloads live.");
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
