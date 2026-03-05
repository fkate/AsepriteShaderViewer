// Aseprite Shader Viewer source
// Copyright (c) 2026 Felix Kate
// Licensed under the MIT license. Check LICENSE.txt for defails

using System.Drawing;

namespace AsepriteShaderViewer {
    public struct RawImage {
        public ushort Width;
        public ushort Height;
        public ushort Channel;
        public ushort Frame;
        public byte[] Data;

        public RawImage(ushort w, ushort h) {
            Width = w;
            Height = h;
            Data = new byte[w * h * 4];

            Channel = 0;
            Frame = 0;
        }

        public RawImage(ushort w, ushort h, Color color) {
            Width = w;
            Height = h;

            int j = 0;

            Data = new byte[w * h * 4];

            for(int i = 0; i < (w * h); i++) {
                Data[j++] = color.R;
                Data[j++] = color.G;
                Data[j++] = color.B;
                Data[j++] = color.A;
            }

            Channel = 0;
            Frame = 0;
        }

        public bool IsInitialized => Data != null;

        public override string ToString() {
            return string.Format("Image: Width:{0}, Height:{1}, Channel:{2}, Frame:{3}, byte[{4}]", Width, Height, Channel, Frame, Data.Length);
        }

    }
}
