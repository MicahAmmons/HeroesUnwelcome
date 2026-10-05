using HeroesUnwelcomed.Game.Data;
using HeroesUnwelcomed.Game.Display;
using HeroesUnwelcomed.Game.Input;
using HeroesUnwelcomed.Game.Rendering;
using HeroesUnwelcomed.Game.Scenes;
using HeroesUnwelcomed.Game.Sound;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HeroesUnwelcomed.Game;

/// <summary>
/// The MonoGame entry point. Deliberately thin: it owns the window, reads
/// input once per frame, and forwards Update/Draw to the active scene.
/// Game-specific behaviour lives in scenes, not here.
/// </summary>
/// <remarks>
/// <b>Frame order:</b> read input, refill the audio and read the beat clock,
/// update the UI canvas, handle global keys (fullscreen, pause, speed), update
/// dev tools, update the scene. Then draw the scene, then the dev tools
/// on top. Screenshots are captured between those two draws, so they show
/// exactly what a player would see.
/// </remarks>
public sealed class HeroesGame : Microsoft.Xna.Framework.Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly LaunchOptions _options;
    private readonly InputService _input = new(InputBindings.CreateDefaults());
    private readonly UiCanvas _ui = new();
    private readonly DataSource _data = DataSource.CreateDefault();

    private SpriteBatch _spriteBatch = null!;
    private GameServices _services = null!;
    private AudioEngine _audio = null!;
    private IScene? _scene;

    // The windowed size to return to when leaving fullscreen.
    private Point _windowedSize;
    // Changing the back buffer can itself fire ClientSizeChanged; this guards against reacting to our own change.
    private bool _applyingSize;

#if DEBUG
    private DevTools.DevToolsHost _devTools = null!;
#endif

    public HeroesGame(LaunchOptions options)
    {
        _options = options;
        _windowedSize = new Point(options.WindowWidth, options.WindowHeight);

        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = options.WindowWidth,
            PreferredBackBufferHeight = options.WindowHeight,
            GraphicsProfile = GraphicsProfile.HiDef,   // allows large textures (4K art)
            SynchronizeWithVerticalRetrace = true,     // vsync: no tearing, no wasted frames
            HardwareModeSwitch = false,                // fullscreen = borderless window, instant alt-tab
        };

        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        // MonoGame defaults to a fixed 60 updates per second. We use variable
        // timing instead: one update per displayed frame, so 120/144 Hz screens
        // are smooth. Gameplay timing comes from the music's beat clock
        // (AudioEngine), not from counting frames, so frame rate can't affect the game.
        IsFixedTimeStep = false;

        Window.Title = "Heroes Unwelcomed";
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnClientSizeChanged;
    }

    public string CurrentSceneName { get; private set; } = "";
    public bool IsFullscreen => _graphics.IsFullScreen;
    public InputService Input => _input;
    public UiCanvas Ui => _ui;
    // Named to avoid clashing with MonoGame's own Game.Services container.
    public GameServices SharedServices => _services;

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        var font = Content.Load<SpriteFont>("Fonts/Ui");
        _audio = AudioEngine.CreateWithPlaceholderMusic(_data);
        _services = new GameServices(GraphicsDevice, Content, _input, _ui, new Primitives(GraphicsDevice, font), _audio, _data);

        if (_options.StartFullscreen)
        {
            SetFullscreen(true);
        }

        string startScene = SceneRegistry.DefaultScene;
#if DEBUG
        _devTools = new DevTools.DevToolsHost(this, _options);
        startScene = _devTools.StartingScene ?? startScene;
#endif
        SwitchScene(startScene);
    }

    protected override void Update(GameTime gameTime)
    {
        float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

        bool keyboardBlocked = false;
        bool mouseBlocked = false;
#if DEBUG
        (keyboardBlocked, mouseBlocked) = _devTools.InputCapture;
#endif
        _input.Update(deltaSeconds, IsActive, keyboardBlocked, mouseBlocked);
        _audio.Update(gameTime.ElapsedGameTime.TotalSeconds);
        _ui.Update(GraphicsDevice.Viewport);

        HandleGlobalActions();

#if DEBUG
        _devTools.Update(gameTime);
#endif
        _scene?.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(14, 12, 20));
        _scene?.Draw(_spriteBatch);

#if DEBUG
        _devTools.Draw(gameTime);
#endif

        base.Draw(gameTime);
    }

    // Controls that work the same in every scene.
    private void HandleGlobalActions()
    {
        if (_input.WasPressed(GameAction.ToggleFullscreen)) SetFullscreen(!IsFullscreen);
        if (_input.WasPressed(GameAction.Pause)) _audio.TogglePause();
        if (_input.WasPressed(GameAction.SpeedUp)) _audio.SpeedUp();
        if (_input.WasPressed(GameAction.SpeedDown)) _audio.SpeedDown();
    }

    /// <summary>Replaces the active scene with a fresh instance of the named one.</summary>
    public void SwitchScene(string name)
    {
        IScene next = SceneRegistry.Create(name, _services);
        _scene?.Dispose();
        _scene = next;
        CurrentSceneName = name;
    }

    /// <summary>Resizes the window (leaving fullscreen if needed).</summary>
    public void SetWindowSize(int width, int height)
    {
        _windowedSize = new Point(width, height);
        if (IsFullscreen)
        {
            SetFullscreen(false);
            return;
        }
        ApplyBackBuffer(width, height, fullscreen: false);
    }

    /// <summary>Switches between a window and borderless fullscreen at the monitor's resolution.</summary>
    public void SetFullscreen(bool fullscreen)
    {
        if (fullscreen == IsFullscreen)
        {
            return;
        }

        if (fullscreen)
        {
            DisplayMode monitor = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            ApplyBackBuffer(monitor.Width, monitor.Height, fullscreen: true);
        }
        else
        {
            ApplyBackBuffer(_windowedSize.X, _windowedSize.Y, fullscreen: false);
        }
    }

    private void ApplyBackBuffer(int width, int height, bool fullscreen)
    {
        _applyingSize = true;
        _graphics.PreferredBackBufferWidth = width;
        _graphics.PreferredBackBufferHeight = height;
        _graphics.IsFullScreen = fullscreen;
        _graphics.ApplyChanges();
        _applyingSize = false;
    }

    // The player dragged the window edge: match the drawing surface to the new size.
    private void OnClientSizeChanged(object? sender, EventArgs e)
    {
        if (_applyingSize || IsFullscreen)
        {
            return;
        }

        Rectangle bounds = Window.ClientBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return; // minimised
        }

        _windowedSize = new Point(bounds.Width, bounds.Height);
        ApplyBackBuffer(bounds.Width, bounds.Height, fullscreen: false);
    }

    protected override void UnloadContent()
    {
        _scene?.Dispose();
        _services.Draw.Dispose();
        _spriteBatch.Dispose();
        _audio.Dispose();
#if DEBUG
        _devTools.Dispose();
#endif
        base.UnloadContent();
    }
}
