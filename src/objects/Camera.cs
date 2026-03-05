// Aseprite Shader Viewer source
// Copyright (c) 2026 Felix Kate
// Licensed under the MIT license. Check LICENSE.txt for defails

namespace AsepriteShaderViewer {

    public class Camera {

        [System.Flags]
        public enum ResetFlags {
            None,
            Position,
            Rotation,
            Size
        }

        public enum MappingMode {
            Fixed,
            Plane,
            Orbit
        }

        public const float RotationIntensity = 1.0f;
        public const float ZoomIntensity = 8.0f;

        public vec2 Position;
        public vec2 Rotation;
        public float OrthographicSize;

        public mat4 View { get; private set; }
        public mat4 Projection { get; private set; }

        public mat4 InverseView { get; private set; }
        public mat4 InverseProjection { get; private set; }

        public MappingMode Mode {
            get => _mode;
            set {
                if(_mode != value) {
                    _mode = value;
                    Reset();
                }
            }
        }

        private MappingMode _mode;
        private vec2 _aspect;
        private vec2 _dragPos;
        private vec2 _dragOrg;

        public Camera(MappingMode mode = MappingMode.Fixed) {
            Position = new vec2(0.0f, 0.0f);
            Rotation = new vec2(0.0f, 0.0f);
            _aspect = new vec2(1.0f, 1.0f);
            OrthographicSize = 1.0f;

            _mode = mode;

            RecalculateView();
            RecalculateProjection();
        }

        /// <summary> Use the texture resolution and screen sizes to calculate the aspect ratio. Calculation depends on mapping mode </summary>
        public void AspectFromResolution(float screenW, float screenH, float texW, float texH) {
            float w, h;
            
            switch(_mode) {
                case MappingMode.Plane:
                    w = screenW / texW;
                    h = screenH / texH;
                    break;

                case MappingMode.Orbit:
                    w = screenW;
                    h = screenH;
                    break;

                default:
                    w = 1.0f;
                    h = 1.0f;
                    break;

            }

            _aspect = w > h ? new vec2(w / h, 1.0f) : new vec2(1.0f, h / w);

            RecalculateProjection();
        }

        /// <summary> Apply a mouse drag. Transform type depends on the mapping mode </summary>
        public void DoDrag(vec2 pos, bool begin = false) {
            switch(_mode) {
                // Do a planar drag
                case MappingMode.Plane:
                    if (begin) {
                        _dragPos = pos;
                        _dragOrg = Position;
                    }
                    else {
                        vec2 diff = (pos - _dragPos);

                        float inv = 1.0f / OrthographicSize;
                        Position = _dragOrg - diff * (_aspect / inv);
                    }
                    break;

                // Do a rotation drag
                case MappingMode.Orbit:
                    if (begin) {
                        _dragPos = pos;
                        _dragOrg = Rotation;
                    }
                    else {
                        vec2 diff = (pos - _dragPos) * RotationIntensity;
                        Rotation = new vec2(
                            _dragOrg.X - diff.X,
                            math.clamp(_dragOrg.Y - diff.Y, -math.PI * 0.4999f, math.PI * 0.4999f)
                        );
                    }
                    break;
            }

            ClampPosition();
            RecalculateView();
        }

        /// <summary> Apply the scrollwheel zoom </summary>
        public void DoZoom(float delta) {
            if(delta == 0.0f) return;

            OrthographicSize = math.clamp(OrthographicSize - delta * ZoomIntensity, 0.1f, 10.0f);
            ClampPosition();
            RecalculateView();
            RecalculateProjection();
        }

        /// <summary> Reset most values to their initial state </summary>
        public void Reset(ResetFlags flags = ResetFlags.Position | ResetFlags.Rotation | ResetFlags.Size) {
            if(flags.HasFlag(ResetFlags.Position)) {
                Position = new vec2(0.0f, 0.0f);
            }

            if(flags.HasFlag(ResetFlags.Rotation)) {
                Rotation = new vec2(0.0f, 0.0f);
            }

            if(flags.HasFlag(ResetFlags.Size)) {
                OrthographicSize = 1.0f;
            }            

            RecalculateView();
            RecalculateProjection();
        }

        /// <summary> Listen to external transform changes </summary>
        public void ApplyTransform(Transform2DEvent ev) {
            Position = new vec2(ev.positionX, ev.positionY);
            Rotation = new vec2(ev.rotationYaw, ev.rotationPitch);
            OrthographicSize = ev.size;

            RecalculateView();
            RecalculateProjection();
        }

        /// <summary> Calculate a camera to world ray from the mouse position </summary>
        public Ray GetRay(vec2 clipPos) {
            // Since we are using an orthographic camera we can skip the projection matrix
            vec3 p = new vec3(clipPos.X * _aspect.X * OrthographicSize, clipPos.Y * _aspect.Y * OrthographicSize, 0.0f);
            return new Ray(math.multiplyPosition(InverseView, p), math.multiplyDirection(InverseView, -vec3.UnitZ));
        }

        /// <summary> Clamp the maximum planar drag so that the image always stays within the bounds </summary>
        public void ClampPosition() {
            float limitX = math.abs(1.0f - _aspect.X * OrthographicSize);
            float limitY = math.abs(1.0f - _aspect.Y * OrthographicSize);

            Position = new vec2(math.clamp(Position.X, -limitX, limitX), math.clamp(Position.Y, -limitY, limitY));
        }

        /// <summary> Recalculate the view matrix </summary>
        public void RecalculateView() {
            vec3 target = new vec3(Position.X, Position.Y, 0.0f);
            vec3 direction = math.createDirection(Rotation.X, Rotation.Y);

            View = mat4.CreateLookAt(target + direction * 10.0f, target, vec3.UnitY);
            InverseView = math.inverse(View);
        }

        /// <summary> Recalculate the projection matrix </summary>
        public void RecalculateProjection() {
            Projection = mat4.CreateOrthographic(_aspect.X * 2.0f * OrthographicSize, _aspect.Y * 2.0f * OrthographicSize, 0.001f, 100.0f);
            InverseProjection = math.inverse(Projection);
        }

    }
}
