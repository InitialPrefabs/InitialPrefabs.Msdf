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

        private int listCountInBytes;
        private int count;

        public MultiTypedList(int totalElements, Type[] types) {
            TypeSizeData = Marshal.AllocHGlobal(totalElements * TypeHelper.MaxSize(types));
            ListData = Marshal.AllocHGlobal(totalElements * Marshal.SizeOf<TypeSpan>());

            listCountInBytes = 0;
            count = 0;
        }

        public void Add<T>(T value) {
            // Add the to the list of typespans.
            TypeSpan* typeHeader = (TypeSpan*)TypeSizeData.ToPointer();

            (int offset, int size) typeSpan = (listCountInBytes, Marshal.SizeOf<T>());
            *(typeHeader + count) = typeSpan;
            // Now we need to add to the ListData.

            count++;
            listCountInBytes += typeSpan.size;
        }

        public void Dispose() {
            Marshal.FreeHGlobal(ListData);
            Marshal.FreeHGlobal(TypeSizeData);
        }
    }
}
