using Heroes_UnWelcomed.DebugBugger;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace Heroes_UnWelcomed.InputTracker
{
    public static class TapTap
    {
        private static KeyboardState _currentKeyboard;
        private static KeyboardState _previousKeyboard;

        private static MouseState _currentMouse;
        private static MouseState _previousMouse;

        private static double _lastLeftClickMs;
        private static Vector2 _lastLeftClickPos;
        private const int DoubleClickTimeMs = 300;
        private const int DoubleClickMaxDistSq = 16;

        private static bool _isDragging;
        private static Point _dragStartPos;
        private const int DragStartThresholdSq = 9;

        private static bool _uIElementClickedThisFrame = false;
        public static bool UIElementClickedThisFrame => _uIElementClickedThisFrame;

        public static void Update()
        {

            _uIElementClickedThisFrame = false;
            StartFrameDebug();
            _previousKeyboard = _currentKeyboard;
            _previousMouse = _currentMouse;


            _currentKeyboard = Keyboard.GetState();
            _currentMouse = Mouse.GetState();

            if (IsLeftPressed())
            {
                _dragStartPos = _currentMouse.Position;
                _isDragging = false;
            }
            if (IsLeftDown() && !_isDragging)
            {
                var dx = _currentMouse.X - _dragStartPos.X;
                var dy = _currentMouse.Y - _dragStartPos.Y;
                if (dx * dx + dy * dy >= DragStartThresholdSq)
                    _isDragging = true;
            }
            if (IsLeftReleased())
                _isDragging = false;
            EndFrameDebug();
        }
        private static void StartFrameDebug()
        {
            Debug.UpdateStartOfFrameInput(UIElementClickedThisFrame);
        }
        private static void EndFrameDebug()
        {
            Debug.UpdateEndOfFrameInput(UIElementClickedThisFrame);
        }
        public static bool IsKeyDown(Keys key) =>
            _currentKeyboard.IsKeyDown(key);

        public static bool IsKeyPressed(Keys key) =>
            _currentKeyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);

        public static bool IsKeyReleased(Keys key) =>
            !_currentKeyboard.IsKeyDown(key) && _previousKeyboard.IsKeyDown(key);

        public static bool AnyKeyPressed()
        {
            var now = _currentKeyboard.GetPressedKeys();
            foreach (var k in now)
                if (!_previousKeyboard.IsKeyDown(k))
                    return true;
            return false;
        }
        public static void UIElementClicked()
        {
            _uIElementClickedThisFrame = true;
        }
        public static Point Position => _currentMouse.Position;
        public static Vector2 WorldPositon => CameraManager.GetMouseWorldPosition(_currentMouse.Position);
        public static Point PreviousPosition => _previousMouse.Position;
       // public static Point Delta => new Point(Position.X - PreviousPosition.X, Position.Y - PreviousPosition.Y);

        public static int ScrollDelta => _currentMouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;


        public static bool IsLeftDown() => _currentMouse.LeftButton == ButtonState.Pressed;
        public static bool IsRightDown() => _currentMouse.RightButton == ButtonState.Pressed;
        public static bool IsMiddleDown() => _currentMouse.MiddleButton == ButtonState.Pressed;

        public static bool IsLeftPressed() =>
            _currentMouse.LeftButton == ButtonState.Pressed &&
            _previousMouse.LeftButton != ButtonState.Pressed;

        public static bool IsRightPressed() =>
            _currentMouse.RightButton == ButtonState.Pressed &&
            _previousMouse.RightButton != ButtonState.Pressed;

        public static bool IsMiddlePressed() =>
            _currentMouse.MiddleButton == ButtonState.Pressed &&
            _previousMouse.MiddleButton != ButtonState.Pressed;

        public static bool IsLeftReleased() =>
            _currentMouse.LeftButton != ButtonState.Pressed &&
            _previousMouse.LeftButton == ButtonState.Pressed;

        public static bool IsRightReleased() =>
            _currentMouse.RightButton != ButtonState.Pressed &&
            _previousMouse.RightButton == ButtonState.Pressed;

        public static bool IsMiddleReleased() =>
            _currentMouse.MiddleButton != ButtonState.Pressed &&
            _previousMouse.MiddleButton == ButtonState.Pressed;

        public static bool LeftClickIn(Rectangle rect) =>
            IsLeftPressed() && rect.Contains(Position);
        public static bool LeftClickInWorldView(Rectangle rect) =>
            IsLeftPressed() && rect.Contains(WorldPositon);
        public static bool LeftReleaseIn(Rectangle rect) =>
            IsLeftReleased() && rect.Contains(Position);

        public static bool IsUIElementClicked() =>_uIElementClickedThisFrame;

        public static bool IsDragging => _isDragging && IsLeftDown();
        public static Point DragStartPosition => _dragStartPos;

    }
}
