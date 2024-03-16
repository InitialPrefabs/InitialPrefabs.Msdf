using Unity.Mathematics;
using UnityEngine;

namespace InitialPrefabs.Msdf {
    public struct RectInt {
        public int X {
            get => Position.x;
            set => Position.x = value;
        }

        public int Y {
            get => Position.y;
            set => Position.y = value;
        }

        public int2 Position;
        public int Width;
        public int Height;

        public int Top => Y;
        public int Bottom => Y + Height;
        public int Left => X;
        public int Right => X + Width;

        public RectInt(Rect rect) {
            Position = new int2(rect.Position);
            Width = (int)rect.Width;
            Height = (int)rect.Height;
        }

        public bool Intersects(RectInt other) =>
            (X < other.X + other.Width) &&
            (X + Width > other.X) &&
            (Y < other.Y + other.Height) &&
            (Y + Height > other.Y);

        public bool Intersects(Rect other) {
            Rect rect = new Rect(this);
            return rect.Intersects(other);
        }
    }
}
