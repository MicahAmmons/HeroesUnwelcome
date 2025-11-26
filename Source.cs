using Heroes_UnWelcomed.Assets;
using Heroes_UnWelcomed.Cells;
using Heroes_UnWelcomed.Data.SaveData;
using Heroes_UnWelcomed.DebugBugger;
using Heroes_UnWelcomed.Encounters;
using Heroes_UnWelcomed.Heroes;
using Heroes_UnWelcomed.InputTracker;
using Heroes_UnWelcomed.Libraries;
using Heroes_UnWelcomed.Movement;
using Heroes_UnWelcomed.ScreenReso;
using Heroes_UnWelcomed.UI;
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


            EncounterLibrary.Initialize();
            AssetManager.Initialize(Content, GraphicsDevice);
            HeroLibrary.Initialize();
            AnimationLibrary.Initialize();
            HeroManager.Initialize();
            CameraManager.Intitialize();
            CellManager.Initialize();
            Debug.Initialize();

            SaveStateLibrary.Initialize();
            UIManager.Initialize();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

        }

        protected override void Update(GameTime gameTime)
        {
            TapTap.Update(gameTime);
            UIManager.Update(gameTime); // if UI calls after Cell Manager, we'll run into the issue of clicking through a UI elements onto teh world
            CameraManager.Update(gameTime, GraphicsDevice);
            CellManager.Update(gameTime);
            Debug.Update();
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            DrawZoomableLayers();
            DrawUINonZoomableLaters();
            
 
           // DrawScroll(gameTime);





            base.Draw(gameTime);
        }
        protected void DrawUINonZoomableLaters()
        {
            _spriteBatch.Begin();
            UIManager.Draw(_spriteBatch);
            Debug.Draw(_spriteBatch);
            _spriteBatch.End();
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

            CellManager.DrawCells(_spriteBatch);
            CellManager.DrawCellOutLine(_spriteBatch);
            CellManager.DrawParties(_spriteBatch);
            _spriteBatch.End();
            
        }

        protected void DrawScroll(GameTime gameTime)
        {
        
        }
    }
}
