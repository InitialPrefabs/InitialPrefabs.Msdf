using System;
using Unity.Mathematics;

namespace InitialPrefabs.Msdf {
    public struct CubicSegment : ISegment, IDivider<CubicSegment>, ICopy<CubicSegment> {
        public EdgeColor Color { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public CubicSegment Clone() {
            throw new NotImplementedException();
        }

        public void GetBounds(ref float4 points) {
            throw new NotImplementedException();
        }

        public float2 GetDirection(Single t) {
            throw new NotImplementedException();
        }

        public float2 GetPoint(Single t) {
            throw new NotImplementedException();
        }

        public SignedDistance GetSignedDistance(float2 origin, out Single t) {
            throw new NotImplementedException();
        }

        public void MoveEndPoint(float2 dst) {
            throw new NotImplementedException();
        }

        public void MoveStartPoint(float2 dst) {
            throw new NotImplementedException();
        }

        public void SplitInThirds(out CubicSegment p1, out CubicSegment p2, out CubicSegment p3) {
            throw new NotImplementedException();
        }
    }
}
