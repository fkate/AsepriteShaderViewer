// Aseprite Shader Viewer source
// Copyright (c) 2026 Felix Kate
// Licensed under the MIT license. Check LICENSE.txt for defails

using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
//using System.Reflection;

namespace AsepriteShaderViewer {
    public class App {

        public readonly Color Background = Color.DimGray;

        private struct QueuedEvent {
            public AppEvent eventType;
            public object data;
        }

        private IWindow Window;
        private GL Graphics;
        private WebSocketServer Server;
        private MouseInput Input;

        private Camera _camera;
        private Mesh _mesh;
        private Shader _shader;
        private Texture2D[] _textures;

        private int _inputOwner;
        private double _elapsedTime;
        private int _frame;

        // Queue events since websocket loop does not have the GL context
        private Queue<QueuedEvent> _eventQueue;

        public App(IWindow window, WebSocketServer server) {
            Window = window;
            Server = server;
        }

        /// <summary> Create objects when window is ready </summary>
        public void Load() {
            // Initialize components
            Graphics = Window.CreateOpenGL();
            Input = new MouseInput(Window);

            _mesh = new Mesh(Graphics);
            _mesh.AssignFromQuad();

            _camera = new Camera(Camera.MappingMode.Plane);

            _shader = new Shader(Graphics);

            _textures = new Texture2D[4];
            for(int i = 0; i < 4 ; i++) _textures[i] = new Texture2D(Graphics);

            // Set the window icon
            RawImage rawIcon = BitmapUtility.Read(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("AsepriteShaderViewer.icon.Icon_x32.bmp"));
            Silk.NET.Core.RawImage icon = new Silk.NET.Core.RawImage((int) rawIcon.Width, (int) rawIcon.Height, rawIcon.Data);
            Window.SetWindowIcon(ref icon);

            // Create an event queue for socket events and start listening
            _eventQueue = new Queue<QueuedEvent>(16);
            Server.RecievedEvent += (ev, data) => _eventQueue.Enqueue(new QueuedEvent { eventType = ev, data = data });

            // Set defaults
            Graphics.Enable(EnableCap.Blend);
            Graphics.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            Graphics.Enable(EnableCap.DepthTest);
            Graphics.DepthFunc(DepthFunction.Lequal);
            Graphics.Disable(EnableCap.CullFace);
            Graphics.FrontFace(FrontFaceDirection.Ccw);

            _elapsedTime = 0.0;
            _frame = 0;
        }

        /// <summary> Resize viewport with window </summary>
        public void OnResize(Vector2D<int> size) {
            Graphics.Viewport(size);
        }

        /// <summary> Update the input </summary>
        public void Update(double deltaTime) {
            Input.Tick();

            // Handle camera transforms
            if(Input.MiddleButton.IsPressed) _camera.DoDrag(Input.ClipPosition, true);
            else if(Input.MiddleButton.IsDown) _camera.DoDrag(Input.ClipPosition);
            
            _camera.DoZoom(Input.ScrollDelta * (float) deltaTime);

            // Get hovered coordinates
            Ray ray = _camera.GetRay(Input.ClipPosition);
            bool insideImage = _mesh.Raycast(ray, out RayHit hit);

            float px = insideImage ? (hit.uv.X * _textures[0].Width) : 0.0f;
            float py = insideImage ? _textures[0].Height - (hit.uv.Y * _textures[0].Height) : 0.0f;

            // Set shader mouse position
            _shader.SetMouse(insideImage ? hit.uv.X : -1.0f, insideImage ? hit.uv.Y : -1.0f, Input.LeftButton.IsDown, Input.RightButton.IsDown);

            // Handle input
            if (_inputOwner == 0 && insideImage) {
                // Left button pressed
                if(Input.LeftButton.IsPressed) {
                    _inputOwner = 1;
                // Right button pressed
                } else if(Input.RightButton.IsPressed) {
                    _inputOwner = 2;
                }
            // Left button released
            } else if(_inputOwner == 1 && Input.LeftButton.IsReleased) {
                _inputOwner = -1;
            }
            // Right button released
            else if (_inputOwner == 2 && Input.RightButton.IsReleased) {
                _inputOwner = -2;
            }

            // Send event (1 left button, 2 right button, negative id button release)
            if (_inputOwner != 0) {
                if(insideImage || _inputOwner < 0) Server.SendMessage(string.Format("TOOL {0}, {1}, {2}", _inputOwner, px.ToString(CultureInfo.InvariantCulture), py.ToString(CultureInfo.InvariantCulture)));
                if(_inputOwner < 0) _inputOwner = 0;
            }
        }

        /// <summary> Render the current frame </summary>
        public void Render(double deltaTime) {
            _elapsedTime += deltaTime;

            // Go through event queue
            while(_eventQueue.Count > 0) {
                if(UseEvent(_eventQueue.Dequeue())) return;
            }

            // Clear the view
            Graphics.ClearColor(Background);
            Graphics.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Bind current values and draw
            _camera.AspectFromResolution(Window.Size.X, Window.Size.Y, _textures[0].Width, _textures[0].Height);

            _shader.Apply();
            _shader.SetTransform(_camera);
            _shader.SetTime((float) _elapsedTime, (float) deltaTime, _frame);

            _textures[0].Bind(TextureUnit.Texture0);
            _textures[1].Bind(TextureUnit.Texture1);
            _textures[2].Bind(TextureUnit.Texture2);
            _textures[3].Bind(TextureUnit.Texture3);

            _mesh.Draw();
        }

        /// <summary> Dispose resources when window is closed </summary>
        public void Close() {
            _mesh.Dispose();
            _shader.Dispose();

            for(int i = 0; i < _textures.Length; i++) {
                _textures[i].Dispose();
            }
        }

        /// <summary> React to incoming events during the rendering. Return true to skip the rest of the frame </summary>
        private bool UseEvent(QueuedEvent ev) {
            switch(ev.eventType) {
                case AppEvent.LoadImage:
                    RawImage raw = (RawImage) ev.data;

                    uint prevW = _textures[raw.Channel].Width;
                    uint prevH = _textures[raw.Channel].Height;

                    _textures[raw.Channel].FromRawImage(raw);

                    if(raw.Channel == 0) {
                        _frame = raw.Frame;
                    } else {
                        // Reuse frame as filter and mode set for secondary textures
                        byte[] mask = BitConverter.GetBytes(raw.Frame);
                        _textures[raw.Channel].OverwriteSettings(mask[0] > 0, mask[1] > 0);
                    }

                    if(prevW != _textures[raw.Channel].Width || prevH != _textures[raw.Channel].Height) {
                        if(_camera.Mode == Camera.MappingMode.Plane) _camera.Reset();
                        else _camera.Reset(Camera.ResetFlags.Position);
                    }
                    return false;

                case AppEvent.LoadShader:
                    _shader.Dispose();
                    _shader = new Shader(Graphics, (string) ev.data);

                    // Always assign the default quad on shader load
                    _mesh.AssignFromQuad();

                    // If the shader defines USE3D swap to an orbit camera
                    if (!_shader.Is3D) {
                        _camera.Mode = Camera.MappingMode.Plane;
                    } else {
                        _camera.Mode = Camera.MappingMode.Orbit;
                    }

                    return false;

                case AppEvent.LoadMesh:
                    // Overwrite render with a 3D mesh
                    _mesh.AssignFromObj((string) ev.data);

                    return false;

                case AppEvent.ExportImage:
                    vec2 size = (vec2) ev.data;
                    Server.SendImage(RenderToTexture((ushort) size.X, (ushort) size.Y, _camera.Mode == Camera.MappingMode.Plane));
                    return true;

                case AppEvent.SetCamera:
                    _camera.ApplyTransform((Transform2DEvent) ev.data);
                    return false;

                case AppEvent.Exit:
                    Window.Close();
                    return true;

            }

            return false;
        }

        /// <summary> Create temporary render values to draw a single frame to an image </summary>
        private unsafe RawImage RenderToTexture(ushort width, ushort height, bool fixedTransform) {
            RawImage img = new RawImage(width, height);
            Texture2D texture = new Texture2D(Graphics, img);

            uint frameBuffer = Graphics.GenFramebuffer();
            Graphics.BindFramebuffer(GLEnum.Framebuffer, frameBuffer);

            uint depthBuffer = Graphics.GenRenderbuffer();
            Graphics.BindRenderbuffer(GLEnum.Renderbuffer, depthBuffer);
            Graphics.RenderbufferStorage(GLEnum.Renderbuffer, GLEnum.DepthComponent, img.Width, img.Height);
            Graphics.FramebufferRenderbuffer(GLEnum.Framebuffer, GLEnum.DepthAttachment, GLEnum.Renderbuffer, depthBuffer);

            if(Graphics.CheckFramebufferStatus(GLEnum.Framebuffer) == GLEnum.FramebufferComplete) {
                Graphics.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, texture.ID, 0);
                Graphics.Viewport(0, 0, texture.Width, texture.Height);

                Graphics.DrawBuffer(GLEnum.ColorAttachment0);

                Graphics.ClearColor(Color.Transparent);
                Graphics.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                Camera _tempCam = new Camera(fixedTransform ? Camera.MappingMode.Fixed : Camera.MappingMode.Orbit);
                if(!fixedTransform) {
                    _tempCam.Rotation = _camera.Rotation;
                    _tempCam.OrthographicSize = _camera.OrthographicSize;
                    _tempCam.RecalculateView();
                    _tempCam.RecalculateProjection();
                }

                _shader.Apply();
                _shader.SetTransform(_tempCam);
                _shader.SetMouse(0.0f, 0.0f, false, false);
                _shader.SetTime(0.0f, 0.0f, _frame);

                _textures[0].Bind(TextureUnit.Texture0);
                _textures[1].Bind(TextureUnit.Texture1);
                _textures[2].Bind(TextureUnit.Texture2);
                _textures[3].Bind(TextureUnit.Texture3);

                _mesh.Draw();

                fixed (byte* ptr = img.Data) {
                    Graphics.ReadPixels(0, 0, texture.Width, texture.Height, GLEnum.Rgba, GLEnum.UnsignedByte, ptr);
                }
            }

            Graphics.DeleteRenderbuffer(depthBuffer);
            Graphics.DeleteFramebuffer(frameBuffer);
            texture.Dispose();

            // Reset viewport;
            Graphics.Viewport(Window.Size);
            Graphics.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, 0, 0);
            Graphics.DrawBuffer(GLEnum.DrawBuffer);

            img.Data = BitmapUtility.Convert(img.Data, img.Width, img.Height, false, true);

            return img;
        }

    }
}
