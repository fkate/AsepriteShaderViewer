// Aseprite Shader Viewer source
// Copyright (c) 2026 Felix Kate
// Licensed under the MIT license. Check LICENSE.txt for defails

using Silk.NET.OpenGL;
using System;

namespace AsepriteShaderViewer {
    public class Mesh : IDisposable {

        public uint VAO { get; private set; }
        public uint VBO { get; private set; }
        public uint EBO { get; private set; }

        public Vertex[] Vertices { get; private set; }
        public ushort[] Indices { get; private set; }

        public int VertexCount => Vertices.Length;
        public int IndexCount => Indices.Length;
        public int TriangleCount => Indices.Length / 3;

        protected GL _context;

        public Mesh(GL graphics) {
            _context = graphics;

            VAO = _context.GenVertexArray();
            _context.BindVertexArray(VAO);

            VBO = _context.GenBuffer();
            EBO = _context.GenBuffer();
        }

        /// <summary> Send a new vertex array to the gpu </summary>
        public unsafe void SetVertices(Vertex[] vertices) {
            _context.BindBuffer(BufferTargetARB.ArrayBuffer, VBO);

            Vertices = vertices;

            fixed (void* v = &vertices[0]) {
                _context.BufferData(BufferTargetARB.ArrayBuffer, (UIntPtr) (vertices.Length * Vertex.stride * sizeof(float)), v, BufferUsageARB.StaticDraw);
            }
        }

        /// <summary> Send a new index array to the gpu </summary>
        public unsafe void SetIndices(ushort[] indices) {            
            _context.BindBuffer(BufferTargetARB.ElementArrayBuffer, EBO);

            Indices = indices;

            fixed (void* i = &indices[0]) {
                _context.BufferData(BufferTargetARB.ElementArrayBuffer, (UIntPtr) (indices.Length * sizeof(ushort)), i, BufferUsageARB.StaticDraw);
            }
        }

        /// <summary> Bind the meshes VAO </summary>
        public void BindVAO() {
            _context.BindVertexArray(VAO);
        }

        /// <summary> Draw the meshes VAO </summary>
        public unsafe void Draw() {
            BindVAO();
            _context.DrawElements(PrimitiveType.Triangles, (uint) IndexCount, DrawElementsType.UnsignedShort, null);
        }

        /// <summary> Create a triangle from the mesh geometry </summary>
        public Triangle GetTriangle(int index) {
            int i = index * 3;
            
            return new Triangle {
                v0 = Vertices[Indices[i++]],
                v1 = Vertices[Indices[i++]],
                v2 = Vertices[Indices[i]],
            };
        }

        /// <summary> Free mesh resources </summary>
        public void Dispose() {
            _context.DeleteBuffer(VBO);
            _context.DeleteBuffer(EBO);
            _context.DeleteVertexArray(VAO);
        }

    }
}
