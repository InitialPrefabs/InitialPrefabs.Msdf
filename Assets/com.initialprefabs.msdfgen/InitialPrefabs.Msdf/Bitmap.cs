using System;

namespace InitialPrefabs.Msdf {
    public class Bitmap<T> where T : struct {
        public readonly int Width;
        public readonly int Height;
        public readonly T[] Data;

        public Bitmap(int width, int height) {
            Width = width;
            Height = height;
            Data = new T[width * height];
        }

        public Bitmap(int width, int height, T[] data) {
            if (data.Length != width * height) {
                throw new ArgumentException("The data length must be equal to the width * height");
            }

            Width = width;
            Height = height;
            Data = data;
        }

        public int GetIndex(int x, int y) => x + y * Width;

        public T this[int x, int y] {
            get => Data[GetIndex(x, y)];
            set => Data[GetIndex(x, y)] = value;
        }
    }
}
