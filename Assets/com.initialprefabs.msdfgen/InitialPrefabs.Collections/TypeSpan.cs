namespace InitialPrefabs.Collections {
    public struct TypeSpan {
        public ushort Offset;
        public ushort Size;

        public static implicit operator (ushort offset, ushort size)(TypeSpan value) => (value.Offset, value.Size);
        public static implicit operator TypeSpan((ushort offset, ushort size) v) => new TypeSpan {
            Offset = v.offset,
            Size = v.size
        };

        public static implicit operator TypeSpan((int offset, int size) v) => new TypeSpan {
            Offset = (ushort)v.offset,
            Size = (ushort)v.size
        };
    }
}

