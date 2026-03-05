// Aseprite Shader Viewer source
// Copyright (c) 2026 Felix Kate
// Licensed under the MIT license. Check LICENSE.txt for defails

using Silk.NET.OpenGL;
using System;

namespace AsepriteShaderViewer {
    public class Shader : IDisposable {        

        public uint ID { get; private set; }

        public bool Is3D { get; private set; }

        private GL _context;

        // Uniform locations
        private int _viewLocation;
        private int _inverseViewLocation;
        private int _projectionLocation;
        private int _inverseProjectionLocation;
        private int _timeLocation;
        private int _mouseLocation;

        public Shader(GL graphics) : this(graphics, ShaderUtility.VertexTemplate(), ShaderUtility.FragmentTemplate()) { }

        public Shader(GL graphics, string fragment) : this(graphics, ShaderUtility.VertexTemplate(), ShaderUtility.FragmentTemplate(fragment)) {
            Is3D = fragment.StartsWith("#define USE3D");
        }

        public unsafe Shader(GL graphics, string vertex, string fragment) {
            _context = graphics;

            ID = _context.CompileShader(vertex, fragment);

            _context.UseProgram(ID);

            // Get texture channels
            int tex0 = _context.GetUniformLocation(ID, "iChannel0");
            if(tex0 >= 0) _context.Uniform1(tex0, 0);

            int tex1 = _context.GetUniformLocation(ID, "iChannel1");
            if (tex1 >= 0) _context.Uniform1(tex1, 1);

            int tex2 = _context.GetUniformLocation(ID, "iChannel2");
            if (tex2 >= 0) _context.Uniform1(tex2, 2);

            int tex3 = _context.GetUniformLocation(ID, "iChannel3");
            if (tex3 >= 0) _context.Uniform1(tex3, 3);

            // Get other uniforms
            _viewLocation = _context.GetUniformLocation(ID, "uView");
            _inverseViewLocation = _context.GetUniformLocation(ID, "uInverseView");
            _projectionLocation = _context.GetUniformLocation(ID, "uProjection");
            _inverseProjectionLocation = _context.GetUniformLocation(ID, "uInverseProjection");

            _timeLocation = _context.GetUniformLocation(ID, "uTime");
            _mouseLocation = _context.GetUniformLocation(ID, "uMouse");

            uint stride = Vertex.stride * sizeof(float);

            _context.EnableVertexAttribArray(0);
            _context.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*) 0);

            _context.EnableVertexAttribArray(1);
            _context.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, (void*) (3 * sizeof(float)));

            _context.EnableVertexAttribArray(2);
            _context.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, stride, (void*) (6 * sizeof(float)));
        }

        /// <summary> Set the transform matrices from a camera </summary>
        public unsafe void SetTransform(Camera camera) {
            mat4 view = camera.View;
            mat4 invView = camera.InverseView;
            mat4 proj = camera.Projection;
            mat4 invProj = camera.InverseProjection;

            if (_viewLocation >= 0) _context.UniformMatrix4(_viewLocation, 1, false, (float*) &view);
            if (_inverseViewLocation >= 0) _context.UniformMatrix4(_inverseViewLocation, 1, false, (float*) &invView);
            if (_projectionLocation >= 0) _context.UniformMatrix4(_projectionLocation, 1, false, (float*) &proj);
            if (_inverseProjectionLocation >= 0) _context.UniformMatrix4(_inverseProjectionLocation, 1, false, (float*) &invProj);
        }

        /// <summary> Set the current time values </summary>
        public void SetTime(float time, float deltaTime, int frame) {
            if(_timeLocation >= 0) _context.Uniform4(_timeLocation, time, deltaTime, frame, 1.0f / deltaTime);
        }

        /// <summary> Set the mouse position in 0-1 space and set left and right input </summary>
        public void SetMouse(float x, float y, bool left, bool right) {
            if(_mouseLocation >= 0) _context.Uniform4(_mouseLocation, x, y, left ? 1 : 0, right ? 1 : 0);
        }

        /// <summary> Apply the shader program </summary>
        public void Apply() {
            _context.UseProgram(ID);
        }

        /// <summary> Free up the shaders resources </summary>
        public void Dispose() {
            _context.DeleteProgram(ID);
        }

    }
}
