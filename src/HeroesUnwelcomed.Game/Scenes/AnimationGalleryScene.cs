using HeroesUnwelcomed.Game.Animation;
using HeroesUnwelcomed.Game.Display;
using HeroesUnwelcomed.Game.Input;
using HeroesUnwelcomed.Game.Sound;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HeroesUnwelcomed.Game.Scenes;

/// <summary>
/// Every placeholder entity side by side, animating to the music. Pick an
/// entity (left/right) and a clip (up/down), Confirm to play it. Change speed or
/// pause to watch how music-clock and sim-clock animations behave.
/// Reloads automatically when data/animations/placeholders.json is saved.
/// </summary>
public sealed class AnimationGalleryScene : IScene
{
    private const float ClipButtonWidth = 210;
    private const float ClipButtonHeight = 64;
    private const float ClipButtonGap = 14;
    private const float SpriteScale = 0.75f;
    private const int MaxPerRow = 4;

    private readonly GameServices _services;
    private readonly List<AnimationPlayer> _players = [];
    private int _selectedEntity;
    private int _selectedClip;
    private string? _reloadMessage;
    private double _reloadMessageUntil;

    public AnimationGalleryScene(GameServices services)
    {
        _services = services;
        AnimationLibrary library = AnimationLibrary.Load(services.Data);
        foreach (EntityDefinition entity in library.Entities)
        {
            _players.Add(new AnimationPlayer(entity));
        }
        services.Data.FileChanged += OnDataFileChanged;
    }

    private AnimationPlayer SelectedPlayer => _players[_selectedEntity];
    private IReadOnlyList<ClipDefinition> SelectedClips => SelectedPlayer.Entity.Clips;

    public void Update(GameTime gameTime)
    {
        InputService input = _services.Input;

        if (input.WasPressed(GameAction.NavigateLeft)) SelectEntity(_selectedEntity - 1);
        if (input.WasPressed(GameAction.NavigateRight)) SelectEntity(_selectedEntity + 1);
        if (input.WasPressed(GameAction.NavigateUp)) _selectedClip = Wrap(_selectedClip - 1, SelectedClips.Count);
        if (input.WasPressed(GameAction.NavigateDown)) _selectedClip = Wrap(_selectedClip + 1, SelectedClips.Count);

        if (input.WasPressed(GameAction.Confirm))
        {
            SelectedPlayer.Play(SelectedClips[_selectedClip].Name);
        }
        if (input.WasPressed(GameAction.Cancel))
        {
            foreach (AnimationPlayer player in _players)
            {
                player.Play(EntityDefinition.IdleClip);
            }
        }

        HandlePointer();

        BeatTime time = _services.Audio.Time;
        foreach (AnimationPlayer player in _players)
        {
            player.Update(time);
        }
    }

    private void HandlePointer()
    {
        InputService input = _services.Input;
        if (!input.PointerPrimaryPressed)
        {
            return;
        }

        Vector2 pointer = _services.Ui.ScreenToUi(input.PointerPosition);

        for (int i = 0; i < _players.Count; i++)
        {
            if (EntityBox(i).Contains(pointer))
            {
                SelectEntity(i);
                return;
            }
        }

        for (int i = 0; i < SelectedClips.Count; i++)
        {
            if (ClipButton(i).Contains(pointer))
            {
                _selectedClip = i;
                SelectedPlayer.Play(SelectedClips[i].Name);
                return;
            }
        }
    }

    public void Draw(SpriteBatch batch)
    {
        var draw = _services.Draw;
        var ui = _services.Ui;
        BeatTime time = _services.Audio.Time;

        batch.Begin(transformMatrix: ui.Transform, samplerState: SamplerState.LinearClamp);

        DrawHeader(batch, time);
        DrawBeatIndicator(batch, time);

        // Floor lines the characters stand on (one per row).
        for (int row = 0; row < RowCount; row++)
        {
            draw.FillRect(batch, new Rectangle(0, (int)FloorY(row), (int)ui.Size.X, 3), new Color(60, 60, 80));
        }

        for (int i = 0; i < _players.Count; i++)
        {
            AnimationPlayer player = _players[i];
            Vector2 feet = FeetPosition(i);
            float floorY = feet.Y;
            PlaceholderSprite.Draw(batch, draw, feet, player.Entity, player.DisplayClip, player.Frame, scale: SpriteScale);

            string status = player.DisplayClip != player.Clip
                ? $"{player.Clip.Name} (paused: showing Idle)"
                : player.Clip.Loop ? player.Clip.Name : $"{player.Clip.Name} {player.Progress:P0}";
            var statusArea = new Rectangle((int)feet.X - 200, (int)floorY + 14, 400, 40);
            draw.TextCentered(batch, status, statusArea, new Color(190, 190, 210), 0.55f);

            if (i == _selectedEntity)
            {
                Rectangle box = EntityBox(i);
                box.Inflate(14, 14);
                draw.OutlineRect(batch, box, Color.Gold, _services.Input.ActiveDevice == InputDevice.Gamepad ? 6 : 3);
            }
        }

        DrawClipButtons(batch);
        DrawHelp(batch);

        if (_reloadMessage is not null && time.MusicBeat < _reloadMessageUntil)
        {
            Rectangle area = ui.Place(Anchor.Center, new Vector2(0, -330), new Vector2(1400, 50));
            draw.TextCentered(batch, _reloadMessage, area, _reloadMessage.StartsWith("Reloaded") ? Color.LightGreen : Color.OrangeRed, 0.6f);
        }

        batch.End();
    }

    private void DrawHeader(SpriteBatch batch, BeatTime time)
    {
        var draw = _services.Draw;
        var audio = _services.Audio;

        Rectangle title = _services.Ui.Place(Anchor.Top, new Vector2(0, 30), new Vector2(900, 50));
        draw.TextCentered(batch, "Animation Gallery", title, Color.White);

        string speed = audio.IsPaused ? "PAUSED" : $"Speed {audio.CurrentLevel.Name}";
        Rectangle speedBox = _services.Ui.Place(Anchor.TopRight, new Vector2(40, 30), new Vector2(280, 56));
        draw.FillRect(batch, speedBox, audio.IsPaused ? new Color(140, 30, 30) : new Color(30, 90, 50));
        draw.TextCentered(batch, speed, speedBox, Color.White, 0.8f);

        string[] lines =
        [
            $"Bar {time.Bar + 1}  Beat {time.BeatInBar + 1}   ({audio.Mixer.CurrentVersion.Bpm:0} bpm)",
            $"Music beat {time.MusicBeat:0.00}",
            $"Game beat  {time.SimBeat:0.00}",
        ];
        Rectangle panel = _services.Ui.Place(Anchor.TopLeft, new Vector2(40, 30), new Vector2(520, 150));
        draw.FillRect(batch, panel, new Color(0, 0, 0, 150));
        for (int i = 0; i < lines.Length; i++)
        {
            draw.Text(batch, lines[i], new Vector2(panel.X + 16, panel.Y + 12 + i * 44), Color.White, 0.7f);
        }
    }

    // Four boxes, one per beat of the bar. The current one lights up and fades
    // across the beat, so you can see the pulse even with the sound off.
    private void DrawBeatIndicator(SpriteBatch batch, BeatTime time)
    {
        var draw = _services.Draw;
        const int size = 56;
        const int gap = 14;
        int count = time.BeatsPerBar;
        Rectangle row = _services.Ui.Place(Anchor.Top, new Vector2(0, 110), new Vector2(count * size + (count - 1) * gap, size));
        float pulse = 1f - (float)(time.MusicBeat - Math.Floor(time.MusicBeat));

        for (int i = 0; i < count; i++)
        {
            var box = new Rectangle(row.X + i * (size + gap), row.Y, size, size);
            bool current = i == time.BeatInBar;
            Color baseColor = i == 0 ? new Color(200, 160, 40) : new Color(90, 120, 200);
            draw.FillRect(batch, box, current ? baseColor * (0.35f + 0.65f * pulse) : new Color(35, 35, 50));
            draw.OutlineRect(batch, box, baseColor, 2);
        }
    }

    private void DrawClipButtons(SpriteBatch batch)
    {
        var draw = _services.Draw;
        for (int i = 0; i < SelectedClips.Count; i++)
        {
            Rectangle button = ClipButton(i);
            bool selected = i == _selectedClip;
            bool playing = SelectedPlayer.Clip.Name == SelectedClips[i].Name;
            draw.FillRect(batch, button, playing ? new Color(60, 90, 60) : new Color(35, 35, 50));
            draw.OutlineRect(batch, button, selected ? Color.Gold : new Color(80, 80, 100), selected ? 4 : 2);
            draw.TextCentered(batch, SelectedClips[i].Name, button, Color.White, 0.6f);
        }

        Rectangle label = _services.Ui.Place(Anchor.Bottom, new Vector2(0, 188), new Vector2(900, 40));
        draw.TextCentered(batch, $"{SelectedPlayer.Entity.Name} animations", label, new Color(180, 180, 200), 0.6f);
    }

    private void DrawHelp(SpriteBatch batch)
    {
        bool pad = _services.Input.ActiveDevice == InputDevice.Gamepad;
        string help = pad
            ? "D-pad L/R: entity   U/D: clip   A: play   B: all to Idle   Start: pause   LB/RB: speed"
            : "Left/Right: entity   Up/Down: clip   Enter/click: play   Esc: all to Idle   P: pause   [ ]: speed";
        Rectangle area = _services.Ui.Place(Anchor.Bottom, new Vector2(0, 30), new Vector2(1800, 44));
        _services.Draw.TextCentered(batch, help, area, new Color(160, 160, 185), 0.6f);
    }

    private int RowCount => (_players.Count + MaxPerRow - 1) / MaxPerRow;

    // Two rows when there are more than four entities: floors at ~44% and ~74% of the canvas height.
    private float FloorY(int row) => RowCount == 1
        ? _services.Ui.Size.Y / 2f + 170
        : _services.Ui.Size.Y * (row == 0 ? 0.435f : 0.74f);

    private Vector2 FeetPosition(int index)
    {
        int row = index / MaxPerRow;
        int inRow = Math.Min(MaxPerRow, _players.Count - row * MaxPerRow);
        int column = index % MaxPerRow;
        return new Vector2(_services.Ui.Size.X * (column + 1) / (inRow + 1), FloorY(row));
    }

    private Rectangle EntityBox(int index)
    {
        Vector2 feet = FeetPosition(index);
        Point size = _players[index].Entity.Size;
        int width = (int)(size.X * SpriteScale);
        int height = (int)(size.Y * SpriteScale);
        return new Rectangle((int)(feet.X - width / 2f), (int)(feet.Y - height), width, height);
    }

    private Rectangle ClipButton(int index)
    {
        int count = SelectedClips.Count;
        float rowWidth = count * ClipButtonWidth + (count - 1) * ClipButtonGap;
        Rectangle row = _services.Ui.Place(Anchor.Bottom, new Vector2(0, 120), new Vector2(rowWidth, ClipButtonHeight));
        return new Rectangle((int)(row.X + index * (ClipButtonWidth + ClipButtonGap)), row.Y, (int)ClipButtonWidth, (int)ClipButtonHeight);
    }

    private void SelectEntity(int index)
    {
        _selectedEntity = Wrap(index, _players.Count);
        _selectedClip = 0;
    }

    private static int Wrap(int value, int count) => ((value % count) + count) % count;

    // Live reload: if the file is broken, keep the old animations and show the error.
    private void OnDataFileChanged(string path)
    {
        if (path != AnimationLibrary.FilePath)
        {
            return;
        }

        try
        {
            AnimationLibrary reloaded = AnimationLibrary.Load(_services.Data);
            var oldPlayers = _players.ToDictionary(p => p.Entity.Name);
            _players.Clear();
            foreach (EntityDefinition entity in reloaded.Entities)
            {
                if (oldPlayers.TryGetValue(entity.Name, out AnimationPlayer? existing))
                {
                    existing.Rebind(entity);
                    _players.Add(existing);
                }
                else
                {
                    _players.Add(new AnimationPlayer(entity));
                }
            }
            _selectedEntity = Math.Min(_selectedEntity, _players.Count - 1);
            _selectedClip = Math.Min(_selectedClip, SelectedClips.Count - 1);
            _reloadMessage = $"Reloaded {path}";
        }
        catch (Exception ex) when (ex is InvalidDataException or System.Text.Json.JsonException or KeyNotFoundException or InvalidOperationException or IOException)
        {
            _reloadMessage = $"Couldn't reload {path}: {ex.Message}";
        }
        _reloadMessageUntil = _services.Audio.Time.MusicBeat + 8;
    }

    public void Dispose() => _services.Data.FileChanged -= OnDataFileChanged;
}
