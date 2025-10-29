using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Heroes_UnWelcomed.InputTracker
{
    public static class TapTap
    {
        private static KeyboardState _currentKeyboard;
        private static KeyboardState _previousKeyboard;

        private static MouseState _currentMouse;
        private static MouseState _previousMouse;

        private static double _lastLeftClickMs;
        private static Point _lastLeftClickPos;
        private const int DoubleClickTimeMs = 300;
        private const int DoubleClickMaxDistSq = 16;

        private static bool _isDragging;
        private static Point _dragStartPos;
        private const int DragStartThresholdSq = 9; 

        private static double _timeMs;

        public static void Update(GameTime gameTime)
        {
            _previousKeyboard = _currentKeyboard;
            _previousMouse = _currentMouse;

            _currentKeyboard = Keyboard.GetState();
            _currentMouse = Mouse.GetState();

            _timeMs = gameTime.TotalGameTime.TotalMilliseconds;

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
        public static Point Position => _currentMouse.Position;
        public static Point PreviousPosition => _previousMouse.Position;
        public static Point Delta => new Point(Position.X - PreviousPosition.X, Position.Y - PreviousPosition.Y);

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

        public static bool LeftReleaseIn(Rectangle rect) =>
            IsLeftReleased() && rect.Contains(Position);

        public static bool HoverIn(Rectangle rect) => rect.Contains(Position);


        public static bool IsLeftDoubleClick()
        {
            if (!IsLeftPressed())
                return false;

            var dt = _timeMs - _lastLeftClickMs;
            var dx = Position.X - _lastLeftClickPos.X;
            var dy = Position.Y - _lastLeftClickPos.Y;

            bool isDouble = dt <= DoubleClickTimeMs && (dx * dx + dy * dy) <= DoubleClickMaxDistSq;

            _lastLeftClickMs = _timeMs;
            _lastLeftClickPos = Position;

            return isDouble;
        }
        public static bool IsDragging => _isDragging && IsLeftDown();
        public static Point DragStartPosition => _dragStartPos;
        public static Rectangle DragRectangle
        {
            get
            {
                var p0 = _dragStartPos;
                var p1 = Position;
                var x = System.Math.Min(p0.X, p1.X);
                var y = System.Math.Min(p0.Y, p1.Y);
                var w = System.Math.Abs(p1.X - p0.X);
                var h = System.Math.Abs(p1.Y - p0.Y);
                return new Rectangle(x, y, w, h);
            }
        }
    }
}
