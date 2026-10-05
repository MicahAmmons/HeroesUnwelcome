using HeroesUnwelcomed.Core.Dungeons;
using HeroesUnwelcomed.Core.Heroes;
using HeroesUnwelcomed.Core.Rules;
using HeroesUnwelcomed.Core.Simulation;
using HeroesUnwelcomed.Game.Display;
using HeroesUnwelcomed.Game.Input;
using HeroesUnwelcomed.Game.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HeroesUnwelcomed.Game.Play;

/// <summary>
/// Everything drawn on top of the world, in UI units: status, the selected
/// room's rankings, menus, button prompts and messages.
/// </summary>
public sealed class PlayHud(GameServices services)
{
    private const float MenuWidth = 700;
    private const float MenuRowHeight = 78;
    private const float MenuTitleHeight = 70;
    private const double ToastSeconds = 4;

    private static readonly Color PanelColor = new(10, 9, 16, 210);
    private static readonly Color Muted = new(170, 168, 190);

    private string? _toast;
    private double _toastUntil;

    private Primitives Draw => services.Draw;
    private UiCanvas Ui => services.Ui;

    public void Toast(string message, double nowSeconds)
    {
        _toast = message;
        _toastUntil = nowSeconds + ToastSeconds;
    }

    public void Render(SpriteBatch batch, Run run, BuildController build, double nowSeconds)
    {
        DrawStatus(batch, run);
        DrawPool(batch, run);
        if (build.SelectedRoom is { } room && room.Kind.IsChallenge)
        {
            DrawRoomRankings(batch, run, room);
        }
        if (build.OpenMenu is { } menu)
        {
            DrawMenu(batch, menu);
        }
        DrawPrompts(batch, run, build);

        if (_toast is not null && nowSeconds < _toastUntil)
        {
            Rectangle area = Ui.Place(Anchor.Top, new Vector2(0, 250), new Vector2(1300, 64));
            Draw.FillRect(batch, area, PanelColor);
            Draw.TextCentered(batch, _toast, area, Color.White, 0.6f);
        }

        if (run.Outcome is { } outcome)
        {
            DrawRunOver(batch, run, outcome);
        }
    }

    public Rectangle MenuRect(Menu menu) =>
        Ui.Place(Anchor.Right, new Vector2(40, 0), new Vector2(MenuWidth, MenuTitleHeight + menu.Items.Count * MenuRowHeight + 16));

    /// <summary>Which menu item is under a point (UI units), if any.</summary>
    public int? MenuItemAt(Menu menu, Vector2 uiPoint)
    {
        for (int i = 0; i < menu.Items.Count; i++)
        {
            if (MenuItemRect(menu, i).Contains(uiPoint)) return i;
        }
        return null;
    }

    private Rectangle MenuItemRect(Menu menu, int index)
    {
        Rectangle rect = MenuRect(menu);
        return new Rectangle(rect.X + 8, (int)(rect.Y + MenuTitleHeight + index * MenuRowHeight), rect.Width - 16, (int)MenuRowHeight - 6);
    }

    private void DrawStatus(SpriteBatch batch, Run run)
    {
        var audio = services.Audio;
        Rectangle panel = Ui.Place(Anchor.TopLeft, new Vector2(40, 30), new Vector2(560, 200));
        Draw.FillRect(batch, panel, PanelColor);

        string door = run.Door switch
        {
            DoorState.Locked => "LOCKED (build mode)",
            DoorState.Locking => "LOCKING… waiting for heroes inside",
            _ => "OPEN",
        };
        Line(batch, panel, 0, $"Souls  {run.Souls.Balance}", Color.Gold, 0.85f);
        Line(batch, panel, 1, $"Door  {door}", Color.White, 0.6f);
        Line(batch, panel, 2, audio.IsPaused ? "PAUSED" : $"Speed {audio.CurrentLevel.Name}   ·   beat {run.Beat:0}", Muted, 0.6f);
    }

    private void DrawPool(SpriteBatch batch, Run run)
    {
        Rectangle panel = Ui.Place(Anchor.TopRight, new Vector2(40, 30), new Vector2(520, 150));
        Draw.FillRect(batch, panel, PanelColor);

        int ready = run.Heroes.All.Count(h => h.State == HeroState.Waiting);
        int inside = run.Heroes.All.Count(h => h.State == HeroState.InParty);
        int dead = run.Heroes.All.Count(h => h.State == HeroState.Dead);
        Line(batch, panel, 0, $"Parties inside  {run.ActiveParties.Count()}", Color.White, 0.65f);
        Line(batch, panel, 1, $"Heroes  {ready} ready · {inside} inside · {dead} dead", Muted, 0.55f);
    }

    // The room's "who goes first here" list: the heroes' shared belief, from the Hero Codex.
    // A slim bar along the bottom, where it doesn't cover the dungeon.
    private void DrawRoomRankings(SpriteBatch batch, Run run, Room room)
    {
        const int shown = 5;
        Rectangle panel = Ui.Place(Anchor.Bottom, new Vector2(0, 96), new Vector2(Math.Min(1700, Ui.Size.X - 80), 132));
        Draw.FillRect(batch, panel, PanelColor);

        int deaths = run.Events.OfType<HeroDied>().Count(d => d.RoomId == room.Id);
        int clears = run.Events.OfType<RoomCleared>().Count(c => c.RoomId == room.Id);
        string header = $"{room.Kind.Id} · level {room.Level} · difficulty {room.Difficulty:0.0} · killed {deaths} · beaten {clears}      Who goes first here:";
        Draw.Text(batch, header, new Vector2(panel.X + 20, panel.Y + 12), Color.White, 0.5f);

        var ranked = Ranking(run, room).Take(shown).ToList();
        float columnWidth = (panel.Width - 40) / (float)shown;
        for (int i = 0; i < ranked.Count; i++)
        {
            Hero hero = ranked[i];
            double reputation = run.Codex.Get(hero.Id, room.Kind.Id)?.Reputation ?? 0;
            bool dead = hero.State == HeroState.Dead;
            var at = new Vector2(panel.X + 20 + i * columnWidth, panel.Y + 54);
            Draw.Text(batch, $"{i + 1}. {hero.Name}{(dead ? " (dead)" : "")}", at, dead ? Muted : Color.White, 0.5f);
            Draw.Text(batch, $"rep {reputation:+0.0;-0.0;0}   skill {hero.Skill(room.Kind.Category):0.0}", at + new Vector2(22, 36), Muted, 0.42f);
        }
    }

    private static IEnumerable<Hero> Ranking(Run run, Room room)
    {
        // The reputation rule's ordering is a pure calculation, so it can be shown directly.
        if (run.Rules.Ordering is ReputationOrderingRule reputation)
        {
            return reputation.Order(run.Heroes.All, room.Kind, run.Codex, run.Definitions);
        }

        // Ladder: show the ladder as-is (heroes not on it yet go at the bottom).
        var ladder = run.Codex.Ladder(room.Kind.Id);
        return run.Heroes.All.OrderBy(h => ladder.Contains(h.Id) ? ladder.ToList().IndexOf(h.Id) : int.MaxValue).ThenBy(h => h.Id);
    }

    private void DrawMenu(SpriteBatch batch, Menu menu)
    {
        Rectangle rect = MenuRect(menu);
        Draw.FillRect(batch, rect, new Color(14, 12, 22, 240));
        Draw.OutlineRect(batch, rect, new Color(120, 110, 160), 2);
        Draw.Text(batch, menu.Title, new Vector2(rect.X + 20, rect.Y + 16), Color.Gold, 0.7f);

        for (int i = 0; i < menu.Items.Count; i++)
        {
            MenuItem item = menu.Items[i];
            Rectangle row = MenuItemRect(menu, i);
            bool selected = i == menu.Selected;
            if (selected)
            {
                Draw.FillRect(batch, row, new Color(60, 55, 90));
                Draw.OutlineRect(batch, row, Color.Gold, 3);
            }
            Color text = item.Available ? Color.White : new Color(120, 118, 135);
            Draw.Text(batch, item.Label, new Vector2(row.X + 16, row.Y + 6), text, 0.62f);
            if (item.Detail.Length > 0)
            {
                Draw.Text(batch, item.Detail, new Vector2(row.X + 16, row.Y + 40), item.Available ? Muted : new Color(100, 98, 115), 0.45f);
            }
        }
    }

    private void DrawPrompts(SpriteBatch batch, Run run, BuildController build)
    {
        bool pad = services.Input.ActiveDevice == InputDevice.Gamepad;
        string prompt;
        if (run.IsOver)
        {
            prompt = pad ? "A: new run" : "Enter / click: new run";
        }
        else if (build.OpenMenu is not null)
        {
            prompt = pad ? "D-pad: choose   A: confirm   B: close" : "Up/Down or mouse: choose   Enter/click: confirm   Esc: close";
        }
        else
        {
            string door = run.Door == DoorState.Open ? "lock door" : "open door";
            prompt = pad
                ? $"D-pad: select   A: open   Y: {door}   X: send party   Start: pause   LB/RB: speed   R-stick: pan   LT/RT: zoom"
                : $"Arrows/mouse: select   Enter/click: open   L: {door}   N: send party   P: pause   [ ]: speed   WASD/right-drag: pan   wheel: zoom";
        }

        Rectangle area = Ui.Place(Anchor.Bottom, new Vector2(0, 24), new Vector2(Ui.Size.X - 80, 56));
        Draw.FillRect(batch, area, PanelColor);
        Draw.TextCentered(batch, prompt, area, Muted, 0.5f);
    }

    private void DrawRunOver(SpriteBatch batch, Run run, RunEnded outcome)
    {
        Rectangle panel = Ui.Place(Anchor.Center, Vector2.Zero, new Vector2(1100, 330));
        Draw.FillRect(batch, panel, new Color(30, 8, 12, 235));
        Draw.OutlineRect(batch, panel, new Color(220, 70, 70), 4);

        Hero winner = run.Heroes.ById(outcome.WinningHeroId);
        RunMetrics metrics = RunMetrics.From(run);
        Draw.TextCentered(batch, "The heroes took your treasure!", new Rectangle(panel.X, panel.Y + 30, panel.Width, 80), new Color(240, 120, 120), 1.1f);
        Draw.TextCentered(batch, $"{winner.Name} defeated your {run.Dungeon.Treasure.Kind.Id} after {outcome.Beat:0} beats.",
            new Rectangle(panel.X, panel.Y + 130, panel.Width, 50), Color.White, 0.6f);
        Draw.TextCentered(batch, $"{metrics.HeroDeaths} heroes died · {metrics.SoulsEarned} souls earned · {metrics.PartiesSent} parties sent",
            new Rectangle(panel.X, panel.Y + 190, panel.Width, 50), Muted, 0.55f);
    }

    private void Line(SpriteBatch batch, Rectangle panel, int index, string text, Color color, float scale) =>
        Draw.Text(batch, text, new Vector2(panel.X + 20, panel.Y + 16 + index * 58), color, scale);
}
