using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace InitialPrefabs.Collections {
    public static class TypeHelper {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MaxSize<T0, T1, T2>()
            where T0 : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged {
            return Math.Max(Marshal.SizeOf<T2>(), Math.Max(Marshal.SizeOf<T0>(), Marshal.SizeOf<T1>()));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MaxSize(Type[] types) {
            int size = Marshal.SizeOf(types[0]);

            for (int i = 1; i < types.Length; i++) {
                size = Math.Max(Marshal.SizeOf(types[i]), size);
            }
            return size;
        }
    }
}

