// Aseprite Shader Viewer source
// Copyright (c) 2026 Felix Kate
// Licensed under the MIT license. Check LICENSE.txt for defails

namespace AsepriteShaderViewer {

    public struct Vertex {
        public const int stride = 9;

        public float x;
        public float y;
        public float z;
        public float nx;
        public float ny;
        public float nz;
        public float u;
        public float v;
        public float w;

        public vec3 Position => new vec3(x, y, z);
        public vec3 Normal => new vec3(nx, ny, nz);
        public vec3 UV => new vec3(u, v, w);

        public Vertex(vec3 position, vec3 normal, vec3 uv) {
            x = position.X;
            y = position.Y;
            z = position.Z;
            nx = normal.X;
            ny = normal.Y;
            nz = normal.Z;
            u = uv.X;
            v = uv.Y;
            w = uv.Z;
        }

        public override string ToString() {
            return string.Format("Vertex: pos({0},{1},{2}) norm({3},{4},{5}) uv({6},{7})", x, y, z, nx, ny, nz, u, v);
        }
    }
}
