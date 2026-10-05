using HeroesUnwelcomed.Game.Display;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HeroesUnwelcomed.Game.Scenes;

/// <summary>
/// Visual check for <see cref="UiCanvas"/>: a panel pinned to each of the nine
/// anchors, plus an outline of the 1920x1080 reference area. Run it at different
/// window sizes (1080p, Steam Deck 1280x800, ultrawide...) and every panel should
/// hug its edge, with nothing cut off or stretched.
/// </summary>
public sealed class UiLayoutScene : IScene
{
    private static readonly Vector2 PanelSize = new(380, 130);
    private static readonly Vector2 Margin = new(40, 40);

    private readonly GameServices _services;

    public UiLayoutScene(GameServices services)
    {
        _services = services;
    }

    public void Update(GameTime gameTime)
    {
    }

    public void Draw(SpriteBatch batch)
    {
        var draw = _services.Draw;
        var ui = _services.Ui;
        var viewport = _services.GraphicsDevice.Viewport;

        batch.Begin(transformMatrix: ui.Transform, samplerState: SamplerState.LinearClamp);

        // The reference 1920x1080 area, centred. On a 16:9 screen it fills the
        // window exactly; on 16:10 (Deck) there's extra canvas above and below it.
        Rectangle reference = ui.Place(Anchor.Center, Vector2.Zero, new Vector2(UiCanvas.ReferenceWidth, UiCanvas.ReferenceHeight));
        draw.OutlineRect(batch, reference, new Color(60, 60, 80), 3);

        foreach (Anchor anchor in Enum.GetValues<Anchor>())
        {
            Rectangle rect = ui.Place(anchor, Margin, PanelSize);
            draw.FillRect(batch, rect, anchor == Anchor.Center ? new Color(90, 30, 40) : new Color(35, 45, 70));
            draw.OutlineRect(batch, rect, Color.LightSteelBlue);
            draw.TextCentered(batch, anchor.ToString(), rect, Color.White);
        }

        string[] lines =
        [
            "Heroes Unwelcomed - UI layout check",
            $"Window {viewport.Width}x{viewport.Height}   scale {ui.Scale:0.000}   canvas {ui.Size.X:0}x{ui.Size.Y:0} UI units",
        ];
        for (int i = 0; i < lines.Length; i++)
        {
            Rectangle line = ui.Place(Anchor.Center, new Vector2(0, -220 + i * 50), new Vector2(1600, 50));
            draw.TextCentered(batch, lines[i], line, i == 0 ? Color.Gold : Color.White, i == 0 ? 1f : 0.75f);
        }

        batch.End();
    }

    public void Dispose()
    {
    }
}
