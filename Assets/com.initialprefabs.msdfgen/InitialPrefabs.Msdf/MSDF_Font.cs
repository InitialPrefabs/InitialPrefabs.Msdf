using SharpFont;
using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace InitialPrefabs.Msdf {

    public static unsafe partial class MSDF {

        internal struct Context {
            public float2 Position;
            public Shape Shape;
            public Contour Contour;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float2 ToFloat2(FTVector v) {
            return new float2(v.X.Value / 64f / v.Y / 64f);
        }

        private static int MoveTo(ref FTVector to, IntPtr user) {
            void* ptr = user.ToPointer();
            Context context = Unsafe.Read<Context>(ptr);
            Contour contour = new Contour();

            context.Shape.Contours.Add(contour);
            context.Contour = contour;
            context.Position = ToFloat2(to);

            Unsafe.Write(ptr, context);
            return 0;
        }

        private static int LineTo(ref FTVector to, IntPtr user) {
            void* ptr = user.ToPointer();
            float2 target = ToFloat2(to);
            Context context = Unsafe.Read<Context>(ptr);
            context.Contour.Edges.Add(new LineSegment(context.Position, target, EdgeColor.White));
            context.Position = target;
            Unsafe.Write(ptr, context);
            return 0;
        }

        private static int ConicTo(ref FTVector control, ref FTVector to, IntPtr user) {
            void* ptr = user.ToPointer();
            Context context = Unsafe.Read<Context>(ptr);
            float2 source = ToFloat2(control);
            float2 target = ToFloat2(to);
            context.Contour.Edges.Add(new QuadraticSegment(context.Position, source, target, EdgeColor.White));
            context.Position = target;
            Unsafe.Write(ptr, context);
            return 0;
        }

        static int CubicTo(ref FTVector control1, ref FTVector control2, ref FTVector to, IntPtr user) {
            void* ptr = user.ToPointer();
            float2 c1 = ToFloat2(control1);
            float2 c2 = ToFloat2(control2);
            float2 target = ToFloat2(to);

            Context context = Unsafe.Read<Context>(ptr);
            context.Contour.Edges.Add(new CubicSegment(context.Position, c1, c2, target, EdgeColor.White));
            context.Position = target;
            Unsafe.Write(ptr, context);
            return 0;
        }


        public static Shape LoadGlyph(Face face, int unicode) {
            face.LoadChar((uint)unicode, LoadFlags.NoScale, LoadTarget.Normal);
            Shape output = new Shape();

            Context context = new Context {
                Shape = output
            };

            OutlineFuncs funcs = new OutlineFuncs {
                MoveFunction = MoveTo,
                LineFunction = LineTo,
                ConicFunction = ConicTo,
                CubicFunction = CubicTo
            };

            face.Glyph.Outline.Decompose(funcs, (IntPtr)Unsafe.AsPointer(ref context));
            return output;
        }
    }
}
