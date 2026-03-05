// Aseprite Shader Viewer source
// Copyright (c) 2026 Felix Kate
// Licensed under the MIT license. Check LICENSE.txt for defails

using Silk.NET.OpenGL;

namespace AsepriteShaderViewer {
    public static class ShaderUtility {

        private const string VERTEX_TEMPLATE =
@"#version 330 core
layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec3 vNormal;
layout (location = 2) in vec3 vTexcoord;

uniform mat4 uView;
uniform mat4 uProjection;

uniform vec4 uTime;
uniform vec4 uMouse;

out vec3 iPosition;
out vec3 iNormal;
out vec2 iTexcoord;
out float iMeshID;

out vec3 iResolution;
out float iTime;
out float iTimeDelta;
out float iFrame;
out float iFrameRate;
out float iChannelTime[4];
out vec3 iChannelResolution[4];
out vec4 iMouse;
        
uniform sampler2D iChannel0;
uniform sampler2D iChannel1;
uniform sampler2D iChannel2;
uniform sampler2D iChannel3;

void main() {
    iPosition = vPosition;
    iNormal = vNormal;
    iTexcoord = vec2(vTexcoord.x, 1.0 - vTexcoord.y);
    iMeshID = vTexcoord.z;

    gl_Position = uProjection * uView * vec4(vPosition, 1.0);    

    iTime = uTime.x;
    iTimeDelta = uTime.y;
    iFrame = uTime.z;
    iFrameRate = uTime.w;
            
    vec2 res0 = textureSize(iChannel0, 0);
    vec2 res1 = textureSize(iChannel1, 0);
    vec2 res2 = textureSize(iChannel2, 0);
    vec2 res3 = textureSize(iChannel3, 0);

    iChannelResolution[0] = vec3(res0.x, res0.y, res0.x / res0.y);
    iChannelResolution[1] = vec3(res1.x, res1.y, res1.x / res1.y);
    iChannelResolution[2] = vec3(res2.x, res2.y, res2.x / res2.y);
    iChannelResolution[3] = vec3(res3.x, res3.y, res3.x / res3.y);

    iChannelTime[0] = iTime;
    iChannelTime[1] = iTime;
    iChannelTime[2] = iTime;
    iChannelTime[3] = iTime;

    iResolution = iChannelResolution[0];
    iMouse = vec4(uMouse.x * iResolution.x, (1.0f - uMouse.y) * iResolution.y, uMouse.zw);
}
";

        private const string FRAGMENT_TEMPLATE =
@"#version 330 core
uniform mat4 uView;
uniform mat4 uInverseView;
uniform mat4 uProjection;
uniform mat4 uInverseProjection;

in vec3 iPosition;
in vec3 iNormal;
in vec2 iTexcoord;
in float iMeshID;

in vec3 iResolution;
in float iTime;
in float iTimeDelta;
in float iFrame;
in float iFrameRate;
in float iChannelTime[4];
in vec3 iChannelResolution[4];
in vec4 iMouse;

uniform sampler2D iChannel0;
uniform sampler2D iChannel1;
uniform sampler2D iChannel2;
uniform sampler2D iChannel3;

out vec4 FRAGMENT_COLOR;

<Main>

void main() {
    mainImage(FRAGMENT_COLOR, iTexcoord * iResolution.xy);
}
";

        public const string DEFAULT_FRAGMENT =
@"void mainImage(out vec4 fragColor, in vec2 fragCoord) {
    fragColor = texture(iChannel0, fragCoord / iResolution.xy);
}
";

        /// <summary> Helper for compiling shader programs </summary>
        public static uint CompileShaderFromString(this GL graphics, string source, ShaderType type) {
            uint id = graphics.CreateShader(type);
            graphics.ShaderSource(id, source);
            graphics.CompileShader(id);

            string infoLog = graphics.GetShaderInfoLog(id);
            if (!string.IsNullOrEmpty(infoLog)) {
                if(type == ShaderType.FragmentShader) infoLog = AdjustFragmentLineCount(infoLog, -27);
                Log.Print(string.Format("{0}: {1}", type, infoLog), Log.MessageType.Error);
            }

            return id;
        }

        /// <summary> Helper for compiling a full shader program consisting of a vertex and a fragment shader </summary>
        public static uint CompileShader(this GL graphics, string vertexSource, string fragmentSource) {
            uint vertexShader = graphics.CompileShaderFromString(vertexSource, ShaderType.VertexShader);
            uint fragmentShader = graphics.CompileShaderFromString(fragmentSource, ShaderType.FragmentShader);

            uint id = graphics.CreateProgram();
            graphics.AttachShader(id, vertexShader);
            graphics.AttachShader(id, fragmentShader);
            graphics.LinkProgram(id);

            graphics.GetProgram(id, GLEnum.LinkStatus, out int status);
            if(status == 0) Log.Print(string.Format("Linking shader {0}", graphics.GetProgramInfoLog(id)), Log.MessageType.Error);

            graphics.DetachShader(id, vertexShader);
            graphics.DetachShader(id, fragmentShader);

            graphics.DeleteShader(vertexShader);
            graphics.DeleteShader(fragmentShader);

            return id;
        }

        /// <summary> Get the standard vertex shader template </summary>
        public static string VertexTemplate() {
            return VERTEX_TEMPLATE;
        }

        /// <summary> Get the standard fragment shader template </summary>
        public static string FragmentTemplate() {
            return FragmentTemplate(DEFAULT_FRAGMENT);
        }

        /// <summary> Create a templated fragment shader with a custom main function </summary>
        public static string FragmentTemplate(string mainFunction) {
            return FRAGMENT_TEMPLATE.Replace("<Main>", mainFunction);
        }

        /// <summary> Rewrite error logs of custom fragment shaders to start within the main function </summary>
        public static string AdjustFragmentLineCount(string infoLog, int lineOffset) {
            // Adjustment of lines since user input does not include half of the fragment shader
            int readPos = 0;

            while(true) {
                // All affected line numbers seem to start with 0:
                int offset = infoLog.IndexOf("0:", readPos);
                if(offset == -1) break;

                offset += 2;
                int length = 0;

                for(; length < 3; length++) {
                    if (infoLog[offset + length].Equals(':')) break;
                }

                // Parse line number and adjust it. Remove old line number and paste in new one
                string first = infoLog.Substring(0, offset);
                string last = infoLog.Substring(offset + length, infoLog.Length - (offset + length));

                int num = int.Parse(infoLog.Substring(offset, length));
                string insert = (num + lineOffset).ToString();

                infoLog = first + insert + last;
                readPos = offset + insert.Length;
            }

            return infoLog;
        }

    }
}
