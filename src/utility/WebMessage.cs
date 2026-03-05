// Aseprite Shader Viewer source
// Copyright (c) 2026 Felix Kate
// Licensed under the MIT license. Check LICENSE.txt for defails

using System.Globalization;

namespace AsepriteShaderViewer {
    public static class WebMessage {

        /// <summary> Parse a message into it's type and content /// </summary>
        public static bool ParseMessage(string message, out string messageType, out string content, int typeChars = 4) {
            messageType = "";
            content = "";
            if(string.IsNullOrEmpty(message) || message.Length < typeChars) return false;

            messageType = message.Substring(0, typeChars);
            if(message.Length > typeChars) content = message.Substring(typeChars, message.Length - typeChars);

            return true;
        }

        /// <summary> Parse a single float /// </summary>
        public static float ParseFloat(string element) {
            return float.TryParse(element, NumberStyles.Float, CultureInfo.InvariantCulture, out float val) ? val : 0.0f;
        }

        /// <summary> Parse multiple elements into a vector2. If there are not enought elements it will write zeros /// </summary>
        public static vec2 ParseVec2(string[] elements, int offset = 0) {
            float val;

            return new vec2(
                offset < elements.Length && float.TryParse(elements[offset++], NumberStyles.Float, CultureInfo.InvariantCulture, out val) ? val : 0.0f,
                offset < elements.Length && float.TryParse(elements[offset++], NumberStyles.Float, CultureInfo.InvariantCulture, out val) ? val : 0.0f
            );
        }

        /// <summary> Parse multiple elements into a vector3. If there are not enought elements it will write zeros /// </summary>
        public static vec3 ParseVec3(string[] elements, int offset = 0) {
            float val;

            return new vec3(
                offset < elements.Length && float.TryParse(elements[offset++], NumberStyles.Float, CultureInfo.InvariantCulture, out val) ? val : 0.0f,
                offset < elements.Length && float.TryParse(elements[offset++], NumberStyles.Float, CultureInfo.InvariantCulture, out val) ? val : 0.0f,
                offset < elements.Length && float.TryParse(elements[offset++], NumberStyles.Float, CultureInfo.InvariantCulture, out val) ? val : 0.0f
            );
        }

        /// <summary> Parse multiple elements into a vector4. If there are not enought elements it will write zeros /// </summary>
        public static vec4 ParseVec4(string[] elements, int offset = 0) {
            float val;

            return new vec4(
                offset < elements.Length && float.TryParse(elements[offset++], NumberStyles.Float, CultureInfo.InvariantCulture, out val) ? val : 0.0f,
                offset < elements.Length && float.TryParse(elements[offset++], NumberStyles.Float, CultureInfo.InvariantCulture, out val) ? val : 0.0f,
                offset < elements.Length && float.TryParse(elements[offset++], NumberStyles.Float, CultureInfo.InvariantCulture, out val) ? val : 0.0f,
                offset < elements.Length && float.TryParse(elements[offset++], NumberStyles.Float, CultureInfo.InvariantCulture, out val) ? val : 0.0f
            );
        }

    }
}
