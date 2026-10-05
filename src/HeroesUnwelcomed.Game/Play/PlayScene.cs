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
/// party) → advance the <see cref="Run"/> to the current game beat → update
/// where every hero should stand → animate → draw.
/// </para>
/// <para>
/// <b>Events in, pictures out.</b> The scene listens to the run's events and
/// turns them into visuals: a departure becomes a walk (or portal jump) timed
/// to arrive exactly when the simulation says; a death becomes the Death
/// animation plus Fear on the dead hero's teammates. The simulation never
/// knows any of this is happening.
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
    private readonly Dictionary<int, AnimationPlayer> _monsters = [];
    private readonly GameDefinitions _definitions;
    private readonly RunSetup _setup;
    private AnimationLibrary _animations;
    private Run _run = null!;
    private BuildController _build = null!;
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
        _definitions = GameDataLoader.LoadDefinitions(services.Data);
        _setup = GameDataLoader.LoadSetup(services.Data, parameters.GetValueOrDefault("setup") ?? GameDataLoader.DefaultSetup);
        services.Data.FileChanged += OnDataFileChanged;
        StartRun();
    }

    /// <summary>The current run (read by the dev tools' inspector).</summary>
    public Run Run => _run;

    public void StartRun()
    {
        if (_run is not null)
        {
            _run.EventRaised -= OnRunEvent;
        }

        _run = Run.Create(_definitions, _setup);
        _run.EventRaised += OnRunEvent;
        _build = new BuildController(_run);
        _build.Message += message => _hud.Toast(message, _seconds);
        _heroViews.Clear();
        _monsters.Clear();
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

        SyncMonsters();
        UpdateHeroTargets();
        foreach (HeroView view in _heroViews.Values) view.Update(time, _gameBeat);
        foreach (AnimationPlayer monster in _monsters.Values) monster.Update(time);
    }

    public void Draw(SpriteBatch batch)
    {
        Viewport viewport = _services.GraphicsDevice.Viewport;

        batch.Begin(transformMatrix: _camera.Transform(viewport), samplerState: SamplerState.LinearClamp);
        _renderer.Draw(batch, _run, _monsters, _build.Selected, showSelection: !_run.IsOver, _camera.Zoom);
        DrawHeroes(batch);
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
        switch (gameEvent)
        {
            case PartySpawned spawned:
                for (int i = 0; i < spawned.HeroIds.Count; i++)
                {
                    HeroView view = ViewFor(spawned.HeroIds[i]);
                    view.Appear(WorldLayout.QueueSlot(_run.Dungeon.Entrance.Position, _run.Dungeon.Entrance.Facing, i));
                }
                break;

            case PartyDeparted departed:
                StartTravel(departed);
                break;

            case PartyArrived arrived:
                PlayMonster(arrived.RoomId, "Idle"); // the room resets for every party
                break;

            case PartyHuddleStarted huddle:
                foreach (int heroId in MembersOf(huddle.PartyId)) ViewFor(heroId).PlayOneShot("Huddle");
                break;

            case AttemptStarted attempt:
                ViewFor(attempt.HeroId).PlayOneShot("Attack");
                PlayMonster(attempt.RoomId, "Prepare");
                break;

            case RoomCleared cleared:
                ViewFor(cleared.HeroId).PlayOneShot("Celebrate");
                PlayMonster(cleared.RoomId, "Death", "Disarmed");
                break;

            case HeroDied died:
                ViewFor(died.HeroId).Die(died.Beat);
                foreach (int heroId in MembersOf(died.PartyId)) ViewFor(heroId).PlayOneShot("Fear");
                PlayMonster(died.RoomId, "Attack", "Trigger");
                break;

            case RunEnded ended:
                _hud.Toast($"{_run.Heroes.ById(ended.WinningHeroId).Name} reached the treasure!", _seconds);
                break;
        }
    }

    private void StartTravel(PartyDeparted departed)
    {
        Room from = _run.Dungeon.RoomById(departed.FromRoomId)!;
        Room to = _run.Dungeon.RoomById(departed.ToRoomId)!;
        var members = MembersOf(departed.PartyId);

        for (int i = 0; i < members.Count; i++)
        {
            HeroView view = ViewFor(members[i]);
            Vector2 end = WorldLayout.QueueSlot(to.Position, to.Facing, i);
            var path = new MovementPath(view.Position);

            if (departed.Connection == ConnectionKind.Hallway)
            {
                // Out the far side, along the hallway, through the next room's door, into the queue.
                path.WalkTo(WorldLayout.ToWorld(from.Position, from.Facing, WorldLayout.CellWidth))
                    .WalkTo(WorldLayout.ToWorld(to.Position, to.Facing, WorldLayout.DoorU))
                    .WalkTo(end);
            }
            else
            {
                // Into this room's exit portal, out of the next room's entry portal.
                path.WalkTo(WorldLayout.ToWorld(from.Position, from.Facing, WorldLayout.ExitPortalU))
                    .JumpTo(WorldLayout.ToWorld(to.Position, to.Facing, WorldLayout.EntryPortalU))
                    .WalkTo(end);
            }

            view.FollowPath(path, departed.Beat, departed.ArriveBeat);
        }
    }

    // Where each hero in a room should stand right now.
    private void UpdateHeroTargets()
    {
        foreach (Party party in _run.ActiveParties)
        {
            if (party.Phase == PartyPhase.Traveling) continue;

            Room room = _run.Dungeon.Rooms[party.RoomIndex];
            float spacing = party.Phase == PartyPhase.Huddling ? WorldLayout.HuddleSpacing : WorldLayout.SlotSpacing;

            for (int i = 0; i < party.Members.Count; i++)
            {
                HeroView view = ViewFor(party.Members[i].Id);
                bool fighting = party.Phase == PartyPhase.Attempting && i == 0;
                view.Target = fighting
                    ? WorldLayout.ToWorld(room.Position, room.Facing, WorldLayout.FightSpotU)
                    : WorldLayout.QueueSlot(room.Position, room.Facing, i, spacing);
            }
        }
    }

    private void DrawHeroes(SpriteBatch batch)
    {
        // The fallen are drawn first (behind the living), and without name tags, to keep fights readable.
        foreach (HeroView view in _heroViews.Values.Where(v => v.Visible).OrderByDescending(v => v.IsDead).ThenBy(v => v.Position.Y).ThenBy(v => v.Position.X))
        {
            AnimationPlayer anim = view.Animation;
            PlaceholderSprite.Draw(batch, _services.Draw, view.Position, anim.Entity, anim.DisplayClip, anim.Frame, view.Opacity, WorldLayout.SpriteScale);

            if (view.IsDead) continue;
            int top = (int)(view.Position.Y - anim.Entity.Size.Y * WorldLayout.SpriteScale);
            var nameArea = new Rectangle((int)view.Position.X - 250, top - 120, 500, 70);
            float nameScale = DungeonRenderer.ScreenSteadyScale(0.75f, _camera.Zoom, 1.6f);
            _services.Draw.TextCentered(batch, view.Name, nameArea, Color.White * view.Opacity, nameScale);
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

    private List<int> MembersOf(int partyId) =>
        _run.AllParties.FirstOrDefault(p => p.Id == partyId)?.Members.Select(h => h.Id).ToList() ?? [];

    // Every room with an animated entity (monster or trap) gets a player; new rooms appear when built.
    private void SyncMonsters()
    {
        foreach (Room room in _run.Dungeon.Rooms)
        {
            if (!_monsters.ContainsKey(room.Id) && _animations.TryEntity(room.Kind.Id) is { } entity)
            {
                _monsters[room.Id] = new AnimationPlayer(entity);
            }
        }
    }

    /// <summary>Plays the first of <paramref name="clips"/> the room's entity has.</summary>
    private void PlayMonster(int roomId, params string[] clips)
    {
        if (!_monsters.TryGetValue(roomId, out AnimationPlayer? monster)) return;
        string? clip = clips.FirstOrDefault(monster.Entity.HasClip);
        if (clip is not null) monster.Play(clip);
    }

    private void OnDataFileChanged(string path)
    {
        if (path == AnimationLibrary.FilePath)
        {
            try
            {
                _animations = AnimationLibrary.Load(_services.Data);
                foreach (HeroView view in _heroViews.Values) view.Animation.Rebind(_animations.Entity("Hero"));
                foreach (var (roomId, monster) in _monsters)
                {
                    if (_animations.TryEntity(monster.Entity.Name) is { } entity) monster.Rebind(entity);
                }
                _hud.Toast("Animations reloaded.", _seconds);
            }
            catch (Exception ex) when (ex is InvalidDataException or System.Text.Json.JsonException or IOException)
            {
                _hud.Toast($"Couldn't reload animations: {ex.Message}", _seconds);
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
    }
}
