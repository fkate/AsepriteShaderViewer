// Aseprite Shader Viewer source
// Copyright (c) 2026 Felix Kate
// Licensed under the MIT license. Check LICENSE.txt for defails

using Silk.NET.OpenGL;
using System;
using System.Drawing;

namespace AsepriteShaderViewer {
    public class Texture2D : IDisposable {

        public uint ID;

        public uint Width;
        public uint Height;

        private GL _context;
                
        public Texture2D(GL graphics, RawImage image) {
            _context = graphics;

            ID = _context.GenTexture();

            OverwriteSettings(false, false);

            FromRawImage(image);
        }

        public Texture2D(GL graphics) : this(graphics, new RawImage(1, 1, Color.Black)) { }

        /// <summary> Set the textures pixels from an external image </summary>
        public unsafe void FromRawImage(RawImage image) {
            Width = image.Width;
            Height = image.Height;

            Bind(TextureUnit.Texture0);

            fixed (byte* ptr = image.Data) {
                _context.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint) image.Width, (uint) image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }

            Unbind();
        }

        /// <summary> Overwrite the textures filter and wrap mode </summary>
        public unsafe void OverwriteSettings(bool linear, bool repeat) {
            _context.ActiveTexture(TextureUnit.Texture0);
            _context.BindTexture(TextureTarget.Texture2D, ID);

            _context.TextureParameter(ID, TextureParameterName.TextureWrapS, repeat ? (int) TextureWrapMode.Repeat : (int) TextureWrapMode.ClampToEdge);
            _context.TextureParameter(ID, TextureParameterName.TextureWrapT, repeat ? (int) TextureWrapMode.Repeat : (int) TextureWrapMode.ClampToEdge);

            _context.TextureParameter(ID, TextureParameterName.TextureMinFilter, linear ? (int) TextureMinFilter.Linear : (int) TextureMinFilter.Nearest);
            _context.TextureParameter(ID, TextureParameterName.TextureMagFilter, linear ? (int) TextureMagFilter.Linear : (int) TextureMagFilter.Nearest);

            Unbind();
        }

        /// <summary> Bind the texture to a channel </summary>
        public void Bind(TextureUnit unit) {
            _context.ActiveTexture(unit);
            _context.BindTexture(TextureTarget.Texture2D, ID);
        }

        /// <summary> Unbind the texture </summary>
        public void Unbind() {
            _context.BindTexture(TextureTarget.Texture2D, 0);
        }

        /// <summary> Free up the texture resources </summary>
        public void Dispose() {
            _context.DeleteTexture(ID);
        }

    }
}
