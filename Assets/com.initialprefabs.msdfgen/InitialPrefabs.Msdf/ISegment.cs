using Unity.Mathematics;

namespace InitialPrefabs.Msdf {
    public interface ISegment {

        EdgeColor Color { get; set; }

        float2 GetPoint(float t);
        float2 GetDirection(float t);
        SignedDistance GetSignedDistance(float2 origin, out float t);

        /// <summary>
        /// Gets all points within a bound.
        /// </summary>
        /// <param name="points">Stores the left, bottom, right, top coordinates.</param>
        void GetBounds(ref float4 points);
        void MoveStartPoint(float2 dst);
        void MoveEndPoint(float2 dst);
    }
}
