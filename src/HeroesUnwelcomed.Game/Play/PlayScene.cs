using HeroesUnwelcomed.Core.Definitions;
using HeroesUnwelcomed.Core.Dungeons;
using HeroesUnwelcomed.Core.Simulation;
using HeroesUnwelcomed.Game.Animation;
using HeroesUnwelcomed.Game.Data;
using HeroesUnwelcomed.Game.Display;
using HeroesUnwelcomed.Game.Input;
using HeroesUnwelcomed.Game.Scenes;
using HeroesUnwelcomed.Game.Sound;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HeroesUnwelcomed.Game.Play;

/// <summary>
/// The game itself: runs the simulation in time with the music and draws it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Each frame:</b> read input (camera, selection, menus, door, send
/// party) → advance the <see cref="Run"/> to the current game beat → draw.
/// </para>
/// <para>
/// <b>Events in, pictures out.</b> The run's events go to the
/// <see cref="Choreographer"/>, which turns every choreography step into exact
/// "path + clip from beat A to beat B" instructions for each hero and
/// encounter. Positions and frames are then calculated from the game beat
/// while drawing, so nothing accumulates and nothing drifts. The simulation
/// never knows any of this is happening.
/// </para>
/// <para>
/// <b>Scenario parameters:</b> <c>setup</c> (data file for the starting run,
/// default game/run-setup.json), <c>startLocked</c> ("true" begins in build
/// mode), <c>focusRoom</c> (order index to centre the camera on) and
/// <c>zoom</c> (starting zoom, e.g. "0.6").
/// </para>
/// </remarks>
public sealed class PlayScene : IScene
{
    private const float PanPixelsPerSecond = 1400f;
    private const float ZoomPerClick = 0.2f;

    private readonly GameServices _services;
    private readonly IReadOnlyDictionary<string, string> _parameters;
    private readonly Camera2D _camera = new() { MinZoom = 0.05f, MaxZoom = 1.5f };
    private readonly DungeonRenderer _renderer;
    private readonly PlayHud _hud;
    private readonly Dictionary<int, HeroView> _heroViews = [];
    private readonly Dictionary<int, ScriptedAnimator> _encounters = [];
    private readonly GameDefinitions _definitions;
    private readonly RunSetup _setup;
    private AnimationLibrary _animations;
    private RoomLayout _layout;
    private Run _run = null!;
    private BuildController _build = null!;
    private Choreographer _choreographer = null!;
    private PartyAudio _partyAudio = null!;
    private double _startSimBeat;
    private double _gameBeat;
    private double _seconds;
    private bool _cameraPlaced;

    public PlayScene(GameServices services, IReadOnlyDictionary<string, string> parameters)
    {
        _services = services;
        _parameters = parameters;
        _renderer = new DungeonRenderer(services.Draw);
        _hud = new PlayHud(services);
        _animations = AnimationLibrary.Load(services.Data);
        _layout = RoomLayout.Load(services.Data);
        _definitions = GameDataLoader.LoadDefinitions(services.Data);
        _setup = GameDataLoader.LoadSetup(services.Data, parameters.GetValueOrDefault("setup") ?? GameDataLoader.DefaultSetup);
        services.Data.FileChanged += OnDataFileChanged;
        StartRun();
    }

    /// <summary>The current run (read by the dev tools' inspector).</summary>
    public Run Run => _run;

    /// <summary>Each party's current influence over the music (shown in the dev tools).</summary>
    public IReadOnlyDictionary<int, float> PartyInfluence => _partyAudio.Influence;

    /// <summary>Animations too long for their choreography step (shown in the dev tools).</summary>
    public IReadOnlyList<string> ChoreographyProblems => ChoreographyCheck.Problems(_animations, _definitions.Tuning);

    public void StartRun()
    {
        if (_run is not null)
        {
            _run.EventRaised -= OnRunEvent;
        }

        _run = Run.Create(_definitions, _setup);
        _heroViews.Clear();
        _encounters.Clear();
        _choreographer = new Choreographer(_run, _layout, ViewFor, EncounterFor);
        _services.Audio.Mixer.Augments.Clear();
        _partyAudio = new PartyAudio(_choreographer, ViewFor, _services.Audio);
        _run.EventRaised += OnRunEvent;
        _build = new BuildController(_run);
        _build.Message += message => _hud.Toast(message, _seconds);
        _startSimBeat = _services.Audio.Time.SimBeat;
        _gameBeat = 0;
        _cameraPlaced = false;

        if (_parameters.GetValueOrDefault("startLocked") == "true")
        {
            _run.Execute(new LockDoor());
        }
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _seconds += dt;
        Viewport viewport = _services.GraphicsDevice.Viewport;

        if (!_cameraPlaced)
        {
            PlaceCamera(viewport);
        }

        HandleInput(dt, viewport);

        BeatTime time = _services.Audio.Time;
        _gameBeat = Math.Max(_gameBeat, time.SimBeat - _startSimBeat);
        _run.AdvanceTo(_gameBeat);

        SyncEncounters();
        _partyAudio.Update(_camera, viewport, _gameBeat);
    }

    public void Draw(SpriteBatch batch)
    {
        Viewport viewport = _services.GraphicsDevice.Viewport;

        batch.Begin(transformMatrix: _camera.Transform(viewport), samplerState: SamplerState.LinearClamp);
        BeatTime time = _services.Audio.Time;
        _renderer.Draw(batch, _run, _layout, _encounters, _gameBeat, time, _build.Selected, showSelection: !_run.IsOver, _camera.Zoom);
        DrawHeroes(batch, time);
        DrawBanners(batch);
        batch.End();

        batch.Begin(transformMatrix: _services.Ui.Transform, samplerState: SamplerState.LinearClamp);
        _hud.Render(batch, _run, _build, _seconds);
        batch.End();
    }

    // ---- Input ------------------------------------------------------------

    private void HandleInput(float dt, Viewport viewport)
    {
        InputService input = _services.Input;
        UpdateCamera(input, dt, viewport);

        if (_run.IsOver)
        {
            if (input.WasPressed(GameAction.Confirm) || input.PointerPrimaryPressed) StartRun();
            return;
        }

        if (input.WasPressed(GameAction.NavigateLeft)) _build.Move(-1, 0);
        if (input.WasPressed(GameAction.NavigateRight)) _build.Move(1, 0);
        if (input.WasPressed(GameAction.NavigateUp)) _build.Move(0, -1);
        if (input.WasPressed(GameAction.NavigateDown)) _build.Move(0, 1);

        HandlePointer(input, viewport);

        if (input.WasPressed(GameAction.Confirm)) _build.Confirm();
        if (input.WasPressed(GameAction.Cancel)) _build.Cancel();

        if (input.WasPressed(GameAction.ToggleDoor))
        {
            bool opening = _run.Door != DoorState.Open;
            CommandResult result = _run.Execute(opening ? new UnlockDoor() : new LockDoor());
            _hud.Toast(!result.Accepted ? result.Reason!
                : opening ? "Door open: heroes may enter."
                : _run.Door == DoorState.Locked ? "Door locked: build and upgrade freely."
                : "Door locking: no new parties. You can build once the heroes inside are done.", _seconds);
        }

        if (input.WasPressed(GameAction.SendParty))
        {
            CommandResult result = _run.Execute(new SendNextParty());
            _hud.Toast(result.Accepted ? "A party enters the dungeon!" : result.Reason!, _seconds);
        }
    }

    private void HandlePointer(InputService input, Viewport viewport)
    {
        bool pointerActive = input.PointerDelta != Point.Zero || input.PointerPrimaryPressed;
        if (!pointerActive || input.ActiveDevice != InputDevice.KeyboardMouse)
        {
            return;
        }

        Vector2 uiPoint = _services.Ui.ScreenToUi(input.PointerPosition);
        if (_build.OpenMenu is { } menu)
        {
            int? item = _hud.MenuItemAt(menu, uiPoint);
            if (item is { } index) _build.HighlightMenuItem(index);
            if (input.PointerPrimaryPressed)
            {
                if (item is not null) _build.Confirm();
                else if (!_hud.MenuRect(menu).Contains(uiPoint)) _build.Cancel();
            }
            return;
        }

        GridPoint cell = WorldLayout.CellAt(_camera.ScreenToWorld(input.PointerPosition.ToVector2(), viewport));
        _build.PointAt(cell);
        if (input.PointerPrimaryPressed && _run.Dungeon.InBounds(cell))
        {
            _build.Confirm();
        }
    }

    private void UpdateCamera(InputService input, float dt, Viewport viewport)
    {
        if (input.Pan != Vector2.Zero) _camera.PanByScreen(input.Pan * PanPixelsPerSecond * dt);
        if (input.PointerDragPanDown) _camera.PanByScreen(-input.PointerDelta.ToVector2());

        if (input.Zoom != 0)
        {
            Vector2 anchor = input.ActiveDevice == InputDevice.KeyboardMouse
                ? input.PointerPosition.ToVector2()
                : new Vector2(viewport.Width / 2f, viewport.Height / 2f);
            _camera.ZoomAt(MathF.Exp(input.Zoom * ZoomPerClick), anchor, viewport);
        }

        // Controller players move a selection, not a cursor: keep it on screen.
        if (input.ActiveDevice == InputDevice.Gamepad)
        {
            Rectangle target = WorldLayout.CellRect(_build.Selected);
            _camera.EnsureVisible(target, viewport, 1f - MathF.Pow(0.002f, dt));
        }
    }

    private void PlaceCamera(Viewport viewport)
    {
        _camera.Bounds = WorldLayout.GridBounds(_run.Dungeon.Width, _run.Dungeon.Height);

        // Frame the rooms that exist, with a little breathing room.
        Rectangle area = _run.Dungeon.Rooms
            .Select(r => WorldLayout.CellRect(r.Position))
            .Aggregate(Rectangle.Union);
        area.Inflate((int)(WorldLayout.CellWidth * 0.1f), (int)(WorldLayout.CellHeight * 0.1f));
        _camera.FitTo(area, viewport, margin: 1f);

        // Scenarios can start somewhere specific, e.g. zoomed in on one room.
        if (int.TryParse(_parameters.GetValueOrDefault("focusRoom"), out int focus) && focus >= 0 && focus < _run.Dungeon.Rooms.Count)
        {
            _camera.Position = WorldLayout.CellRect(_run.Dungeon.Rooms[focus].Position).Center.ToVector2();
        }
        if (float.TryParse(_parameters.GetValueOrDefault("zoom"), System.Globalization.CultureInfo.InvariantCulture, out float zoom))
        {
            _camera.ZoomAt(zoom / _camera.Zoom, new Vector2(viewport.Width / 2f, viewport.Height / 2f), viewport);
        }
        _cameraPlaced = true;
    }

    // ---- Simulation → visuals ------------------------------------------------

    private void OnRunEvent(GameEvent gameEvent)
    {
        _choreographer.Handle(gameEvent);
        _partyAudio.Handle(gameEvent);

        if (gameEvent is RunEnded ended)
        {
            _hud.Toast($"{_run.Heroes.ById(ended.WinningHeroId).Name} reached the treasure!", _seconds);
        }
    }

    private void DrawHeroes(SpriteBatch batch, BeatTime time)
    {
        foreach (HeroView view in _heroViews.Values.Where(v => v.Visible))
        {
            Vector2 feet = view.PositionAt(_gameBeat);
            var (clip, frame) = view.FrameAt(_gameBeat, time);
            PlaceholderSprite.Draw(batch, _services.Draw, feet, view.Animator.Entity, clip, frame, scale: WorldLayout.SpriteScale);

            int top = (int)(feet.Y - view.Animator.Entity.Size.Y * WorldLayout.SpriteScale);
            var nameArea = new Rectangle((int)feet.X - 250, top - 120, 500, 70);
            float nameScale = DungeonRenderer.ScreenSteadyScale(0.75f, _camera.Zoom, 1.6f);
            _services.Draw.TextCentered(batch, view.Name, nameArea, Color.White, nameScale);
        }
    }

    // The reorder banner: drops from the ceiling over the whole line, holds, then rolls back up.
    private void DrawBanners(SpriteBatch batch)
    {
        foreach (PartyView party in _choreographer.Parties)
        {
            if (party.Banner is not { } banner || party.Line.Count == 0) continue;

            float progress = (float)Math.Clamp((_gameBeat - banner.StartBeat) / Math.Max(1e-6, banner.Duration), 0, 1);
            float drop = banner.Step switch
            {
                PartyPhase.BannerDown => progress,
                PartyPhase.BannerUp => 1 - progress,
                _ => 1,
            };
            if (drop <= 0) continue;

            Room room = _run.Dungeon.RoomById(party.RoomId)!;
            Vector2 front = _layout.LineSlot(room, 0);
            Vector2 back = _layout.LineSlot(room, party.Line.Count - 1);
            float halfHero = _animations.Entity("Hero").Size.X * WorldLayout.SpriteScale / 2f + 50;
            Rectangle cell = WorldLayout.CellRect(room.Position);
            int left = (int)(Math.Min(front.X, back.X) - halfHero);
            int right = (int)(Math.Max(front.X, back.X) + halfHero);
            int top = cell.Y + 40;
            int fullHeight = (int)(front.Y + 30 - top);
            var rect = new Rectangle(left, top, right - left, (int)(fullHeight * drop));

            string clip = banner.Step switch
            {
                PartyPhase.BannerDown => "Unfurl",
                PartyPhase.BannerUp => "Furl",
                _ => "Covered",
            };
            _services.Draw.FillRect(batch, rect, new Color(110, 20, 30));
            _services.Draw.OutlineRect(batch, rect, new Color(230, 190, 80), 10);
            float labelScale = DungeonRenderer.ScreenSteadyScale(0.8f, _camera.Zoom, 3f);
            _services.Draw.TextCentered(batch, $"Banner - {clip}", rect, new Color(240, 210, 120), labelScale);
        }
    }

    // ---- Helpers --------------------------------------------------------------

    private HeroView ViewFor(int heroId)
    {
        if (!_heroViews.TryGetValue(heroId, out HeroView? view))
        {
            view = new HeroView(heroId, _run.Heroes.ById(heroId).Name, _animations.Entity("Hero"));
            _heroViews[heroId] = view;
        }
        return view;
    }

    private ScriptedAnimator? EncounterFor(int roomId)
    {
        SyncEncounters();
        return _encounters.GetValueOrDefault(roomId);
    }

    // Every room with an animated entity (monster or trap) gets an animator; new rooms appear when built.
    private void SyncEncounters()
    {
        foreach (Room room in _run.Dungeon.Rooms)
        {
            if (!_encounters.ContainsKey(room.Id) && _animations.TryEntity(room.Kind.Id) is { } entity)
            {
                _encounters[room.Id] = new ScriptedAnimator(entity);
            }
        }
    }

    private void OnDataFileChanged(string path)
    {
        if (path == AnimationLibrary.FilePath)
        {
            try
            {
                _animations = AnimationLibrary.Load(_services.Data);
                foreach (HeroView view in _heroViews.Values) view.Animator.Rebind(_animations.Entity("Hero"));
                foreach (ScriptedAnimator encounter in _encounters.Values)
                {
                    if (_animations.TryEntity(encounter.Entity.Name) is { } entity) encounter.Rebind(entity);
                }
                _hud.Toast("Animations reloaded.", _seconds);
            }
            catch (Exception ex) when (ex is InvalidDataException or System.Text.Json.JsonException or IOException)
            {
                _hud.Toast($"Couldn't reload animations: {ex.Message}", _seconds);
            }
        }
        else if (path == ReactionSettings.FilePath)
        {
            try
            {
                _services.Audio.Reactions = ReactionSettings.Load(_services.Data);
                _hud.Toast("Music reactions reloaded.", _seconds);
            }
            catch (Exception ex) when (ex is InvalidDataException or System.Text.Json.JsonException or IOException)
            {
                _hud.Toast($"Couldn't reload reactions: {ex.Message}", _seconds);
            }
        }
        else if (path == RoomLayout.FilePath)
        {
            try
            {
                _layout = RoomLayout.Load(_services.Data);
                _hud.Toast("Room layout reloaded. Restart the scene to rebuild positions.", _seconds);
            }
            catch (Exception ex) when (ex is InvalidDataException or System.Text.Json.JsonException or IOException)
            {
                _hud.Toast($"Couldn't reload the layout: {ex.Message}", _seconds);
            }
        }
        else if (path.StartsWith("game/", StringComparison.Ordinal))
        {
            _hud.Toast($"{path} changed. Restart the scene (dev panel) to use it.", _seconds);
        }
    }

    public void Dispose()
    {
        _services.Data.FileChanged -= OnDataFileChanged;
        _run.EventRaised -= OnRunEvent;
        _services.Audio.Mixer.Augments.Clear();
    }
}
