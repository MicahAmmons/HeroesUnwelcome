using Heroes_UnWelcomed.Assets;
using Heroes_UnWelcomed.Heroes;
using Heroes_UnWelcomed.InputTracker;
using Heroes_UnWelcomed.Libraries;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Heroes_UnWelcomed
{
    public class Source : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        public Source()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            AssetManager.Initialize(Content);
            HeroLibrary.Initialize();
            AnimationLibrary.Initialize();
            HeroManager.Initialize();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

        }

        protected override void Update(GameTime gameTime)
        {
            TapTap.Update(gameTime);
            HeroManager.Update(gameTime);

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();


            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            HeroManager.DrawHeroes(_spriteBatch);
            base.Draw(gameTime);
        }
    }
}
