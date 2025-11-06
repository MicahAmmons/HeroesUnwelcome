using Heroes_UnWelcomed.Assets;
using Heroes_UnWelcomed.Cells;
using Heroes_UnWelcomed.Heroes;
using Heroes_UnWelcomed.InputTracker;
using Heroes_UnWelcomed.Libraries;
using Heroes_UnWelcomed.Movement;
using Heroes_UnWelcomed.ScreenReso;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Heroes_UnWelcomed
{
    public class Source : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private int ScreenWidth = ScreenSize.Width;
        private int ScreenHeight = ScreenSize.Height;
        private Camera2D _camera => CameraManager.Camera;


        public Source()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth = ScreenWidth;
            _graphics.PreferredBackBufferHeight = ScreenHeight;
            _graphics.ApplyChanges();

        }

        protected override void Initialize()
        {

 
            AssetManager.Initialize(Content, GraphicsDevice);
            HeroLibrary.Initialize();
            AnimationLibrary.Initialize();
            HeroManager.Initialize();
            CameraManager.Intitialize();
            CellManager.Initialize();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

        }

        protected override void Update(GameTime gameTime)
        {
            TapTap.Update(gameTime);
            CameraManager.Update(gameTime, GraphicsDevice);
            HeroManager.Update(gameTime);

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Pink);
            DrawZoomableLayers();

 
           // DrawScroll(gameTime);





            base.Draw(gameTime);
        }
        protected void DrawZoomableLayers()
        {
                        _spriteBatch.Begin(
                 SpriteSortMode.Deferred,
                 BlendState.AlphaBlend,
                 SamplerState.LinearClamp,   // smoother scaling when zoomed
                 DepthStencilState.None,
                 RasterizerState.CullNone,
                 effect: null,
                 transformMatrix: _camera.GetViewMatrix(GraphicsDevice) // <-- camera applied here
             );
            HeroManager.DrawHeroes(_spriteBatch);
            CellManager.DrawCells(_spriteBatch);
            CellManager.DrawCellOutLine(_spriteBatch);
            _spriteBatch.End();
        }

        protected void DrawScroll(GameTime gameTime)
        {
        
        }
    }
}
