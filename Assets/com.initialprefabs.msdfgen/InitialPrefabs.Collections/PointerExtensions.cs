namespace InitialPrefabs.Msdf.Collections {

    public static unsafe class PointerExtensions {

        public static ref T ElementAt<T>(T* ptr, int i) where T : unmanaged {
            return ref *(ptr + i);
        }
    }
}

