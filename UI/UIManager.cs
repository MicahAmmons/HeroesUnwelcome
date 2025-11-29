using Heroes_UnWelcomed.InputTracker;
using Heroes_UnWelcomed.UI.UIEncounter;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;


namespace Heroes_UnWelcomed.UI
{
    public static class UIManager
    {

        private static UIEncounterController _encUIController;
        private static UIInput _input;

        public static void Initialize()
        {
             _encUIController = new UIEncounterController();
            _encUIController.UIButtonPressed += UIElementClickedThisFrame;
        }
        public static void Draw(SpriteBatch s)
        {
            _encUIController?.Draw(s);
        }
        public static void Update(GameTime gameTime)
        {
            UpdateInput();
            _encUIController?.Update(gameTime, _input);
        }
        private static void UIElementClickedThisFrame()
        {
            TapTap.UIElementClicked();
        }
        private static void UpdateInput()
        {
            _input = new UIInput
            {
                MousePos = TapTap.Position.ToVector2(),          // Vector2
                LeftPressed = TapTap.IsLeftPressed(),
                LeftJustReleased = TapTap.IsLeftReleased(),
                EscapePressed = TapTap.IsKeyPressed(Keys.Escape),
                SpacePressed = TapTap.IsKeyPressed(Keys.Space)
            };
        }
        internal static void ResetSpecificEncounter()
        {
            _encUIController.ResetCurrentlySelectedSpecificEnc();
        }

        internal static void ResetEncounterCategory()
        {
            _encUIController.UpdateSelectedEncounterCategory(EncounterType.None);
        }
    }
    public readonly struct UIInput
    {
        public Vector2 MousePos { get; init; }
        public bool LeftPressed { get; init; }
        public bool LeftJustReleased { get; init; }
        public bool EscapePressed { get; init; }
        public bool SpacePressed { get; init; }
    }
}
