namespace InitialPrefabs.Msdf {
    public interface ICopy<T> where T : ISegment {
        T Clone();
    }
}
