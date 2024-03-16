using Unity.Mathematics;

namespace InitialPrefabs.Msdf {
    public struct Rect {

        public float X {
            get => Position.x;
            set => Position.x = value;
        }

        public float Y {
            get => Position.y;
            set => Position.y = value;
        }
        public float Width { get; internal set; }
        public float Height { get; internal set; }

        public float2 Position;

        public float2 Size {
            get => new float2(Width, Height);
            set {
                Width = value.x;
                Height = value.y;
            }
        }

        public float Top => Y;
        public float Bottom => Y + Height;
        public float Left => X;
        public float Right => X + Height;

        public Rect(float x, float y, float width, float height) {
            Position = new float2(x, y);
            Width = width;
            Height = height;
        }

        public Rect(RectInt rect) {
            Position = new float2(rect.Position);
            Width = rect.Width;
            Height = rect.Height;
        }

        public bool Intersects(Rect other) {
            return (X < other.X + other.Width) &&
                (X + Width > other.X) &&
                (Y < other.Y + other.Height) &&
                (Y + Height > other.Y);
        }

        public bool Intersects(RectInt other) {
            Rect otherf = new Rect(other);
            return Intersects(otherf);
        }
    }
}
