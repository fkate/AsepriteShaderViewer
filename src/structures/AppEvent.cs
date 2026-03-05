// Aseprite Shader Viewer source
// Copyright (c) 2026 Felix Kate
// Licensed under the MIT license. Check LICENSE.txt for defails

namespace AsepriteShaderViewer {

    public enum AppEvent {
        None,
        LoadImage,
        LoadShader,
        LoadMesh,
        ExportImage,
        SetCamera,
        Exit
    }

    public struct Transform2DEvent {
        public float positionX;
        public float positionY;
        public float rotationYaw;
        public float rotationPitch;
        public float size;
    }

}
