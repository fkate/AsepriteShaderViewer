// Aseprite Shader Viewer source
// Copyright (c) 2026 Felix Kate
// Licensed under the MIT license. Check LICENSE.txt for defails

using System.Numerics;

namespace AsepriteShaderViewer {
    public struct Ray {
        public vec3 origin;
        public vec3 direction;

        public Ray(vec3 org, vec3 dir) {
            origin = org;
            direction = math.normalize(dir);
        }

        public vec3 GetPoint(float distance) {
            return origin + direction * distance;
        }

        public override string ToString() {
            return string.Format("Ray(origin:{0}, direction:{1})", origin.ToString(), direction.ToString());
        }
    }

    public struct RayHit {
        public vec3 position;
        public vec3 normal;
        public vec3 uv;
        public float distance;

        public override string ToString() {
            return string.Format("Hit(position:{0}, normal:{1}, uv:{2}, distance:{3})", position.ToString(), normal.ToString(), uv.ToString(), distance.ToString());
        }
    }

}
