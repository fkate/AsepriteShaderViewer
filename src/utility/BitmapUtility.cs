// Aseprite Shader Viewer source
// Copyright (c) 2026 Felix Kate
// Licensed under the MIT license. Check LICENSE.txt for defails

using System.IO;

namespace AsepriteShaderViewer {
    public static class BitmapUtility {

        /// <summary> Read a bitmap file. This is not a full features reader but focuses only on 32bit images </summary>
        public static RawImage Read(Stream stream) {
            RawImage img = new RawImage(1, 1);

            using(BinaryReader reader = new BinaryReader(stream)) {
                char[] head = reader.ReadChars(2);
                if (head[0] != 'B' || head[1] != 'M') return img;
                int size = reader.ReadInt32();
                reader.ReadInt32(); // Reserved
                int offset = reader.ReadInt32();

                reader.ReadInt32(); // InfoHeader size
                int width = reader.ReadInt32();
                int height = reader.ReadInt32();
                reader.ReadInt16(); // Unused planes
                int bpp = reader.ReadInt16();

                // In this case we only care for 32bit images
                if (bpp != 32) return img;

                // Skip rest of infoheader and go directly to data block
                reader.BaseStream.Position = offset;

                int pixelCount = width * height;
                byte[] rawData = reader.ReadBytes(pixelCount * 4);

                img = new RawImage { Width = (ushort) width, Height = (ushort) height, Data = Convert(rawData, width, height, false, true, 2, 1, 0, 3) };
            }

            return img;
        }

        /// <summary> Convert image data. Options include image flipping and channel swapping </summary>
        public static byte[] Convert(byte[] data, int width, int height, bool flipX, bool flipY, int r = 0, int g = 1, int b = 2, int a = 3) {
            byte[] output = new byte[width * height * 4];
            
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    int off = (y * width + x) * 4;
                    int invOff = ((flipY ? (height - y - 1) : y) * width + (flipX ? width - x - 1 : x)) * 4;

                    output[off] = data[invOff + r];
                    output[off + 1] = data[invOff + g];
                    output[off + 2] = data[invOff + b];
                    output[off + 3] = data[invOff + a];
                }
            }

            return output;
        }

    }
}
