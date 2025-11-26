using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heroes_UnWelcomed.Assets
{
    public static class AssetManager
    {
        private static ContentManager _content;
        private static Dictionary<string, Texture2D> _textures = new();
        private static Dictionary<string, SpriteFont> _fonts = new();
        private static Dictionary<string, Song> _songs = new();
        private static Dictionary<string, Effect> _effects = new();
        public static void Initialize(ContentManager content, GraphicsDevice graphicsDevice)
        {
            _content = content;
            CreatePixel(graphicsDevice);
            LoadAllAssets();
        }
        internal static void LoadAllAssets()
        {
            AssetLoader.LoadAllTextures();
            AssetLoader.LoadShaders();
            AssetLoader.LoadCombatEncounters();
            AssetLoader.LoadFonts();
            AssetLoader.LoadCellAnimations();
            AssetLoader.LoadHallwayEncounter();

        }
        public static void LoadTexture(string key, string path)
        {
            Texture2D texture = _content.Load<Texture2D>(path);
            _textures[key] = texture;
        }
        public static void LoadTexture(Texture2D texture, string key)
        {
            _textures[key] = texture;
        }
        public static Texture2D GetTexture(string key)
        {
            if (_textures.TryGetValue(key, out var tex))
                return tex;

            return _textures["TurtleWalk"];
        }
        public static Song LoadSong(string path)
        {
            return _content.Load<Song>(path);
        }
        public static Song GetSong(string key)
        {
            return _songs[key];
        }
        public static void LoadFont(string key, string path) =>
       _fonts[key] = _content.Load<SpriteFont>(path);
        public static SpriteFont GetFont(string key)
        {
            if (_fonts.TryGetValue(key, out var font))
                return font;

            return _fonts["Default"];
        }

        public static void LoadEffect(string key, string path)
        {
            Effect effect = _content.Load<Effect>(path);
            _effects[key] = effect;
        }

        internal static Effect GetEffect(string v)
        {
            return _effects[v];
        }
        public static void CreatePixel(GraphicsDevice device)
        {
            Texture2D pixel = new Texture2D(device, 1, 1);
            pixel.SetData(new[] { Color.White });
            _textures["WhitePixel"] = pixel;
        }
        public static bool TryLoadRawTexture(string key, string path)
        {
            try
            {
                Texture2D texture = _content.Load<Texture2D>(path);
                _textures[key] = texture;
                return true;
            }
            catch
            {

                return false;
            }
        }

    }
}
