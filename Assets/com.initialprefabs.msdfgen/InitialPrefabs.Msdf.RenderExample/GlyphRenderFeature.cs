using InitialPrefabs.Msdf.Runtime;
using Palmmedia.ReportGenerator.Core;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace InitialPrefabs.Msdf.RenderExample {

    public class GlyphRenderFeature : ScriptableRendererFeature {

        [Serializable]
        public class GlyphSettings {
            public ushort FontSize = 16;
            public string TextToRender = "ABCDefgHIJklmNOPqrsTUVwxyZ";
            public SerializedFontData FontData;
            public Material Material;
            public Texture2D FontTexture;
            public float2 StartPos = new float2(500);
            public Color Color = Color.white;

            public bool IsValid => Material != null & FontData != null && FontTexture != null;
        }

        private struct GlyphComparer : IComparer<RuntimeGlyphData> {
            public int Compare(RuntimeGlyphData x, RuntimeGlyphData y) {
                return x.Unicode.CompareTo(y.Unicode);
            }
        }

        private class GlyphRenderPass : ScriptableRenderPass {

            public GlyphSettings Settings;

            private const int VertexCapacity = 26 * 4;
            private const int IndexCapacity = 26 * 6;

            private Mesh mesh;
            private readonly List<Vector3> vertexPositions = new List<Vector3>(VertexCapacity);
            private readonly List<Vector2> vertexUvs0 = new List<Vector2>(VertexCapacity);
            private readonly List<Vector2> vertexUvs1 = new List<Vector2>(VertexCapacity);
            private readonly List<Color> vertexColors = new List<Color>(VertexCapacity);
            private readonly List<ushort> indices = new List<ushort>(IndexCapacity);

            public GlyphRenderPass() {
                mesh = new Mesh {
                    name = "Glyph Mesh"
                };
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
                if (!Settings.IsValid) {
                    return;
                }

                if (mesh == null) {
                    mesh = new Mesh {
                        name = "Glyph Mesh"
                    };
                }

                ref var cameraData = ref renderingData.cameraData;
                var width = cameraData.camera.pixelWidth;
                var height = cameraData.camera.pixelHeight;

                var cmd = CommandBufferPool.Get(nameof(GlyphRenderPass));
                cmd.Clear();
                ResetInternalBuffers();

                cmd.SetViewProjectionMatrices(
                    Matrix4x4.identity,
                    Matrix4x4.Ortho(0, width, 0, height, -1, 1));

                var faceData = Settings.FontData.FaceData;
                var glyphs = Settings.FontData.Glyphs;
                var fontScale = (float)Settings.FontSize / faceData.UnitsPerEm;
                var startPos = Settings.StartPos;

                // Now build the glyphs
                foreach (var c in Settings.TextToRender) {
                    var idx = Array.BinarySearch(
                        glyphs,
                        0,
                        glyphs.Length,
                        new RuntimeGlyphData { Unicode = c },
                        default(GlyphComparer));

                    if (idx < 0) {
                        continue;
                    }

                    var s = glyphs[idx].Scale(fontScale);
                    var metrics = s.Metrics;
                    // calculate the px range, assume the texture size is the metrics
                    // Ideally this should be put to a compute buffer, receive the vertex ID and flatten it to it's associated
                    // unitRange. Every 4 vertices = 1 element
                    // TODO: Write the Range into the RuntimeFontFace
                    var unitRange = 100 / metrics;

                    var localHeight = s.Metrics.y - s.Bearings.y;
                    var extrem = new float4(
                        startPos.x + s.Bearings.x,
                        startPos.y - localHeight,
                        startPos.x + s.Bearings.x + s.Metrics.x,
                        startPos.y - localHeight + s.Metrics.y);

                    var idxOffset = vertexPositions.Count;
                    PushVertex(new float3(extrem.xy, 0), s.Uvs.xy, Settings.Color);
                    PushVertex(new float3(extrem.xw, 0), s.Uvs.xw, Settings.Color);
                    PushVertex(new float3(extrem.zw, 0), s.Uvs.zw, Settings.Color);
                    PushVertex(new float3(extrem.zy, 0), s.Uvs.zy, Settings.Color);
                    PushUnitRange(unitRange);

                    PushTriangle(idxOffset, idxOffset + 1, idxOffset + 2);
                    PushTriangle(idxOffset, idxOffset + 2, idxOffset + 3);

                    startPos.x += s.Advance - s.Bearings.x;
                }
                SetMesh();

                cmd.DrawMesh(mesh, Matrix4x4.identity, Settings.Material, 0, 0);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            private void ResetInternalBuffers() {
                vertexPositions.Clear();
                vertexUvs0.Clear();
                vertexUvs1.Clear();
                vertexColors.Clear();
                indices.Clear();
                mesh.Clear();
            }

            private void PushVertex(float3 position, float2 uv, Color color) {
                vertexPositions.Add(position);
                vertexUvs0.Add(uv);
                vertexColors.Add(color);
            }

            private void PushUnitRange(float2 unitRange) {
                vertexUvs1.Add(unitRange);
                vertexUvs1.Add(unitRange);
                vertexUvs1.Add(unitRange);
                vertexUvs1.Add(unitRange);
            }

            private void PushTriangle(int a, int b, int c) {
                indices.Add((ushort)a);
                indices.Add((ushort)b);
                indices.Add((ushort)c);
            }

            private void SetMesh() {
                mesh.SetVertices(vertexPositions);
                mesh.SetUVs(0, vertexUvs0);
                mesh.SetUVs(1, vertexUvs1);
                mesh.SetColors(vertexColors, 0, vertexColors.Count);
                mesh.SetIndices(indices, MeshTopology.Triangles, 0);
                mesh.RecalculateBounds();
            }
        }

        private GlyphRenderPass glyphRenderPass;
        [SerializeField]
        private GlyphSettings settings = new GlyphSettings();

        /// <inheritdoc/>
        public override void Create() {
            glyphRenderPass = new GlyphRenderPass {
                Settings = settings,
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents
            };
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            renderer.EnqueuePass(glyphRenderPass);
        }
    }

}
