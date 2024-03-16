namespace InitialPrefabs.Msdf {
    public interface IDivider<T> where T : ISegment {
        void SplitInThirds(out T p1, out T p2, out T p3);
    }
}
