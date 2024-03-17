﻿using Unity.Mathematics;

namespace InitialPrefabs.Msdf {

    public struct SignedDistance {
        public float Distance;
        public float Dot;

        public SignedDistance(float distance, float dot) {
            Distance = distance;
            Dot = dot;
        }

        public static SignedDistance NegativeInfinite { get; } = new SignedDistance {
            Distance = float.NegativeInfinity,
            Dot = 1.0f
        };

        public static SignedDistance PositiveInfinite { get; } = new SignedDistance {
            Distance = float.PositiveInfinity,
            Dot = 1.0f
        };

        public static bool operator <(SignedDistance lhs, SignedDistance rhs) =>
            math.abs(lhs.Distance) > math.abs(rhs.Distance) ||
            (math.abs(lhs.Distance) == math.abs(rhs.Distance) && lhs.Dot < rhs.Dot);

        public static bool operator >(SignedDistance lhs, SignedDistance rhs) =>
            math.abs(lhs.Distance) > math.abs(rhs.Distance) ||
            (math.abs(lhs.Distance) == math.abs(rhs.Distance) && lhs.Dot <= rhs.Dot);

        public static bool operator <=(SignedDistance lhs, SignedDistance rhs) =>
            math.abs(lhs.Distance) < math.abs(rhs.Distance) ||
            (math.abs(lhs.Distance) == math.abs(rhs.Distance) && lhs.Dot <= rhs.Dot);

        public static bool operator >=(SignedDistance lhs, SignedDistance rhs) =>
            math.abs(lhs.Distance) < math.abs(rhs.Distance) ||
            (math.abs(lhs.Distance) == math.abs(rhs.Distance) && lhs.Dot >= rhs.Dot);
    }
}
