using System.Linq;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace InitialPrefabs.Msdf.Runtime {

    public class Generation : MonoBehaviour {

        private struct Vertex {
            public float3 Position;
            public float2 Uvs;
        }

        public SerializedFontData FontData;
        public string Chars = "abcdefghijk";
        public float PointSize = 12.0f;
        public Material RenderMat;

        [SerializeField]
        private Mesh mesh;

        private void Awake() {
        }

        private void Start() {
            mesh = new Mesh();

            var vertices = new List<Vertex>();
            var indices = new List<ushort>();

            var startPos = new float2();

            var glyphs = FontData.Glyphs;
            for (var i = 0; i < Chars.Length; i++) {
                var glyphData = glyphs.First(x => (char)(x.Unicode) == Chars[i]);
                var s = glyphData; // glyphData.ScaleWithDPI(PointSize, FontData.FaceData);

                var localHeight = s.Metrics.y - s.Bearings.y;
                Debug.Log($"Local Height for {Chars[i]}: {localHeight}, {s.Metrics.y}, {s.Bearings.y}");
                var extrem = new float4(
                    startPos.x + s.Bearings.x,
                    startPos.y - localHeight,
                    startPos.x + s.Bearings.x + s.Metrics.x,
                    startPos.y - localHeight + s.Metrics.y);

                var idxOffset = (ushort)vertices.Count;

                Debug.Log($"{extrem.xy}, uv: {s.Uvs.xy}");
                Debug.Log($"{extrem.xw}, uv: {s.Uvs.xw}");
                Debug.Log($"{extrem.zw}, uv: {s.Uvs.zw}");
                Debug.Log($"{extrem.zy}, uv: {s.Uvs.zy}");

                vertices.Add(new Vertex {
                    Position = new float3(extrem.xy, 0),
                    Uvs = s.Uvs.xy
                });
                vertices.Add(new Vertex {
                    Position = new float3(extrem.xw, 0),
                    Uvs = s.Uvs.xw
                });
                vertices.Add(new Vertex {
                    Position = new float3(extrem.zw, 0),
                    Uvs = s.Uvs.zw
                });
                vertices.Add(new Vertex {
                    Position = new float3(extrem.zy, 0),
                    Uvs = s.Uvs.zy
                });

                indices.Add(idxOffset);
                indices.Add((ushort)(idxOffset + 1));
                indices.Add((ushort)(idxOffset + 2));
                indices.Add(idxOffset);
                indices.Add((ushort)(idxOffset + 2));
                indices.Add((ushort)(idxOffset + 3));

                startPos.x += s.Advance - s.Bearings.x;
            }

            mesh.SetVertices(vertices.Select(x => (Vector3)x.Position).ToArray());
            mesh.SetUVs(0, vertices.Select(x => (Vector2)x.Uvs).ToArray());
            mesh.SetColors(vertices.Select(_ => Color.white).ToArray(), 0, vertices.Count);

            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.RecalculateBounds();

            // var layout = new[] {
            //     new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            //     new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
            // };

            // mesh.SetVertexBufferParams(vertices.Count, layout);
            // mesh.SetVertexBufferData(vertices, 0, 0, vertices.Count);

            // mesh.SetIndexBufferParams(indices.Count, IndexFormat.UInt16);
            // mesh.SetIndexBufferData(indices, 0, 0, indices.Count);

            // mesh.SetSubMesh(0, new SubMeshDescriptor {
            //     baseVertex = 0,
            //     bounds = new Bounds(default, Vector3.one * 500),
            //     firstVertex = 0,
            //     indexCount = indices.Count,
            //     indexStart = 0,
            //     vertexCount = vertices.Count,
            //     topology = MeshTopology.Triangles
            // });

            var filter = GetComponent<MeshFilter>();
            filter.sharedMesh = mesh;
        }

        // private void Update() {
        //     var rp = new RenderParams(RenderMat);
        //     Graphics.RenderMesh(rp, mesh, 0, Matrix4x4.identity);
        // }
    }
}


