using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HeroesUnwelcomed.Game.Scenes;

/// <summary>
/// One "screen" of the game: title, gameplay, a test sandbox... Exactly one
/// scene is active at a time; the game forwards Update and Draw to it.
/// </summary>
public interface IScene : IDisposable
{
    void Update(GameTime gameTime);

    void Draw(SpriteBatch batch);
}
