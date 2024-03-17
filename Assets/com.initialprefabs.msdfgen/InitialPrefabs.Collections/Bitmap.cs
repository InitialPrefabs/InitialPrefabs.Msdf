using System;
using System.Runtime.CompilerServices;

namespace InitialPrefabs.Msdf.Collections {

    public readonly ref struct Bitmap<T> where T : unmanaged {
        public readonly int Width;
        public readonly int Height;
        public readonly Span<T> Data;

        public Bitmap(int width, int height, Span<T> data) {
            Width = width;
            Height = height;
            Data = data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetIndex(int x, int y) => x + y * Width;

        public T this[int x, int y] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Data[GetIndex(x, y)];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Data[GetIndex(x, y)] = value;
        }
    }

    public class HeapBitmap<T> where T : struct {
        public readonly int Width;
        public readonly int Height;
        public readonly T[] Data;

        public HeapBitmap(int width, int height) {
            Width = width;
            Height = height;
            Data = new T[width * height];
        }

        public HeapBitmap(int width, int height, T[] data) {
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
