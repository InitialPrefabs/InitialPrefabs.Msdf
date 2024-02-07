namespace InitialPrefabs.Msdf {
    public interface ICopy<T> where T : struct, ISegment {
        T Clone();
    }
}
