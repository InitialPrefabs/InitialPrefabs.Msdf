using Unity.Mathematics;

namespace InitialPrefabs.Msdf {
    internal struct MultiDistance {

        public float R {
            get => Values.x;
            set => Values.x = value;
        }

        public float G {
            get => Values.y;
            set => Values.y = value;
        }

        public float B {
            get => Values.z;
            set => Values.z = value;
        }

        public float Med {
            get => Values.w;
            set => Values.w = value;
        }

        public float4 Values;
    }
}

