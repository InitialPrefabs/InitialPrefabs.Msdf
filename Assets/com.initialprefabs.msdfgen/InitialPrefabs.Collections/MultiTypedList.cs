using System;
using System.Runtime.InteropServices;

namespace InitialPrefabs.Collections {

    public unsafe struct MultiTypedList : IDisposable {
        /// <summary>
        /// Effectively an array of bytes to store unmanaged types.
        /// </summary>
        internal IntPtr ListData;

        /// <summary>
        /// The TypeSizeData stores the size per element.
        /// </summary>
        internal IntPtr TypeSizeData;

        public MultiTypedList(int totalElements, Type[] types) {
            TypeSizeData = Marshal.AllocHGlobal(totalElements * TypeHelper.MaxSize(types));
            ListData = Marshal.AllocHGlobal(totalElements * Marshal.SizeOf<TypeSpan>());
        }

        public void Dispose() {
            Marshal.FreeHGlobal(ListData);
            Marshal.FreeHGlobal(TypeSizeData);
        }
    }
}
