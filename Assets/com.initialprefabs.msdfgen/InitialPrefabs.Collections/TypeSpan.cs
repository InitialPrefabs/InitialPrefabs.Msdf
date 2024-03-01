namespace InitialPrefabs.Collections {
    public struct TypeSpan {
        public ushort Offset;
        public ushort Size;

        public static implicit operator (int offset, int size)(TypeSpan value) => (value.Offset, value.Size);
        public static implicit operator TypeSpan((int offset, int size) v) => new TypeSpan {
            Offset = (ushort)v.offset,
            Size = (ushort)v.size
        };
    }
}

