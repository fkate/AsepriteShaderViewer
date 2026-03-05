// Aseprite Shader Viewer source
// Copyright (c) 2026 Felix Kate
// Licensed under the MIT license. Check LICENSE.txt for defails

using Silk.NET.Input;
using Silk.NET.Windowing;

namespace AsepriteShaderViewer {
    public class MouseInput {

        public struct ButtonState {

            private int _state;

            public ButtonState(ButtonState last, bool input) {
                if(input) {
                    _state = math.clamp(last._state, -1, 0) - 1;
                } else {     
                    _state = math.clamp(last._state, 0, 1) + 1;
                }
            }

            public bool IsPressed => _state == -1;
            public bool IsDown => _state <= -1;
            public bool IsReleased => _state == 1;
            public bool IsUp => _state >= 1;

        }

        public ButtonState LeftButton;
        public ButtonState RightButton;
        public ButtonState MiddleButton;

        public vec2 PixelPosition;
        public vec2 ViewportPosition;
        public vec2 ClipPosition;
        public float ScrollDelta;

        private IWindow _window;
        private IInputContext _context;
        private IMouse _mouse;

        public MouseInput(IWindow window) {
            _window = window;
            _context = window.CreateInput();

            if(_context.Mice.Count >= 1) {
                _mouse = _context.Mice[0];
            }
        }

        /// <summary> Call each frame to update the input and prgress the button presses </summary>
        public void Tick() {
            if(_mouse == null) return;

            LeftButton = new ButtonState(LeftButton, _mouse.IsButtonPressed(MouseButton.Left));
            RightButton = new ButtonState(RightButton, _mouse.IsButtonPressed(MouseButton.Right));
            MiddleButton = new ButtonState(MiddleButton, _mouse.IsButtonPressed(MouseButton.Middle));

            // Mouse position starts top left. We need to start bottom left s
            vec2 wSize = new vec2(_window.Size.X, _window.Size.Y);
            vec2 mPos = _mouse.Position;

            PixelPosition = new vec2(mPos.X, wSize.Y - mPos.Y - 1.0f);
            ViewportPosition = PixelPosition / wSize;
            ClipPosition = ViewportPosition * 2.0f - new vec2(1.0f, 1.0f);

            ScrollDelta = _mouse.ScrollWheels[0].Y;
        }

    }
}
