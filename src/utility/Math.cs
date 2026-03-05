// Aseprite Shader Viewer source
// Copyright (c) 2026 Felix Kate
// Licensed under the MIT license. Check LICENSE.txt for defails

global using mat4 = System.Numerics.Matrix4x4;
global using vec2 = System.Numerics.Vector2;
global using vec3 = System.Numerics.Vector3;
global using vec4 = System.Numerics.Vector4;
using MathF = System.MathF;

namespace AsepriteShaderViewer {
    public static class math {

        public const float PI = 3.14159265f;
        public const float PI2 = 6.28318530f;
        public const float RadToDeg = 57.29577951f;
        public const float DegToRad = 0.01745329f;


        /// <summary> Clamp value between min and max </summary>
        public static ushort clamp(ushort value, ushort min, ushort max) => value <= min ? min : (value >= max ? max : value);
        /// <summary> Clamp value between min and max </summary>
        public static int clamp(int value, int min, int max) => value <= min ? min : (value >= max ? max : value);
        /// <summary> Clamp value between min and max </summary>
        public static float clamp(float value, float min, float max) => value <= min ? min : (value >= max ? max : value);


        /// <summary> Clamp value to a minimum </summary>
        public static ushort min(ushort value, ushort limit) => value <= limit ? limit : value;
        /// <summary> Clamp value to a minimum </summary>
        public static int min(int value, int limit) => value <= limit ? limit : value;
        /// <summary> Clamp value to a minimum </summary>
        public static float min(float value, float limit) => value <= limit ? limit : value;


        /// <summary> Clamp value to a maximum </summary>
        public static ushort max(ushort value, ushort limit) => value >= limit ? limit : value;
        /// <summary> Clamp value to a maximum </summary>
        public static int max(int value, int limit) => value >= limit ? limit : value;
        /// <summary> Clamp value to a maximum </summary>
        public static float max(float value, float limit) => value >= limit ? limit : value;


        /// <summary> Get the absolute value </summary>
        public static int abs(int value) => value < 0 ? -value : value;
        /// <summary> Get the absolute value </summary>
        public static float abs(float value) => value < 0 ? -value : value;


        /// <summary> Returns the sinus of the radians value </summary>
        public static float sin(float value) => MathF.Sin(value);
        /// <summary> Returns the cosinus of the radians value </summary>
        public static float cos(float value) => MathF.Cos(value);
        /// <summary> Returns the tangents of the radians value </summary>
        public static float tan(float value) => MathF.Tan(value);
        /// <summary> Returns the sinus and cosinus of the radians value </summary>
        public static vec2 sincos(float value) => new vec2(MathF.Sin(value), MathF.Cos(value));


        /// <summary> Get the dot product between vector a and b </summary>
        public static float dot(vec2 a, vec2 b) => (a.X * b.X) + (a.Y * b.Y);
        /// <summary> Get the dot product between vector a and b </summary>
        public static float dot(vec3 a, vec3 b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);
        /// <summary> Get the dot product between vector a and b </summary>
        public static float dot(vec4 a, vec4 b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z) + (a.W * b.W);


        /// <summary> Get the cross product between vector a and b </summary>
        public static vec3 cross(vec3 a, vec3 b) => new vec3((a.Y * b.Z) - (a.Z * b.Y), (a.Z * b.X) - (a.X * b.Z), (a.X * b.Y) - (a.Y * b.X));


        /// <summary> Get square length of vector </summary>
        public static float lengthSq(vec2 a) => dot(a, a);
        /// <summary> Get square length of vector </summary>
        public static float lengthSq(vec3 a) => dot(a, a);
        /// <summary> Get square length of vector </summary>
        public static float lengthSq(vec4 a) => dot(a, a);


        /// <summary> Get length of vector </summary>
        public static float length(vec2 a) => MathF.Sqrt(dot(a, a));
        /// <summary> Get length of vector </summary>
        public static float length(vec3 a) => MathF.Sqrt(dot(a, a));
        /// <summary> Get length of vector </summary>
        public static float length(vec4 a) => MathF.Sqrt(dot(a, a));


        /// <summary> Bring vector to a length of one </summary>
        public static vec2 normalize(vec2 a) => a / length(a);
        /// <summary> Bring vector to a length of one </summary>
        public static vec3 normalize(vec3 a) => a / length(a);
        /// <summary> Bring vector to a length of one </summary>
        public static vec4 normalize(vec4 a) => a / length(a);


        /// <summary> Inverse matrix. Return identity if invalid </summary>
        public static mat4 inverse(mat4 a) => mat4.Invert(a, out mat4 res) ? res : mat4.Identity;


        /// <summary> Multiply two matrices </summary>
        public static mat4 multiply(mat4 a, mat4 b) => a * b;
        /// <summary> Multiply matrix and a four dimensional vector </summary>
        public static vec4 multiply(mat4 a, vec4 b) => new vec4(a.M11, a.M12, a.M13, a.M14) * b.X + new vec4(a.M21, a.M22, a.M23, a.M24) * b.Y + new vec4(a.M31, a.M32, a.M33, a.M34) * b.Z + new vec4(a.M41, a.M42, a.M43, a.M44) * b.W;


        /// <summary> Transform a three dimensional position </summary>
        public static vec3 multiplyPosition(mat4 a, vec3 b) => multiply(a, new vec4(b.X, b.Y, b.Z, 1.0f)).toVec3();
        /// <summary> Transform a three dimensional direction </summary>
        public static vec3 multiplyDirection(mat4 a, vec3 b) => multiply(a, new vec4(b.X, b.Y, b.Z, 0.0f)).toVec3();
        
        
        /// <summary> Create a direction from yaw and pitch in radians </summary>
        public static vec3 createDirection(float yaw, float pitch) {
            float yawSin = MathF.Sin(yaw), yawCos = MathF.Cos(yaw), pitchSin = MathF.Sin(pitch), pitchCos = MathF.Cos(pitch);
            return new vec3(yawSin * pitchCos, pitchSin, yawCos * pitchCos);
        }


        // Vector extensions
        /// <summary> Truncate xyzw to xyz </summary>
        public static vec3 toVec3(this vec4 v) => new vec3(v.X, v.Y, v.Z);
        /// <summary> Truncate xyzw to xy </summary>
        public static vec2 toVec2(this vec4 v) => new vec2(v.X, v.Y);
        /// <summary> Truncate xyz to xy </summary>
        public static vec2 toVec2(this vec3 v) => new vec2(v.X, v.Y);

    }
}
