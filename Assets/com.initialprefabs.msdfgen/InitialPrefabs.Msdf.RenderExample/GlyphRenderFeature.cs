using InitialPrefabs.Msdf.Runtime;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace InitialPrefabs.Msdf.RenderExample {

    public class GlyphRenderFeature : ScriptableRendererFeature {

        /// <summary>
        /// Basic font settings.
        /// </summary>
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

        /// <summary>
        /// Compares a Glyph's unicode with another Glyph.
        /// </summary>
        private struct GlyphComparer : IComparer<RuntimeGlyphData> {
            public readonly int Compare(RuntimeGlyphData x, RuntimeGlyphData y) {
                return x.Unicode.CompareTo(y.Unicode);
            }
        }

        /// <summary>
        /// A basic glyph render pass.
        /// <remarks>
        /// This is by no means an optimal way to render text with Unity's API and is simply an example.
        /// This also does not handle text layout in any way whatsoever.
        /// </remarks>
        /// </summary>
        private class GlyphRenderPass : ScriptableRenderPass {

            public GlyphSettings Settings;

            private MaterialPropertyBlock propertyBlock;

            private const int VertexCapacity = 26 * 4;
            private const int IndexCapacity = 26 * 6;
            private static readonly int _BaseMap = Shader.PropertyToID(nameof(_BaseMap));

            private Mesh mesh;
            private readonly List<Vector3> vertexPositions = new List<Vector3>(VertexCapacity);
            private readonly List<Vector2> vertexUvs0 = new List<Vector2>(VertexCapacity);
            private readonly List<Color> vertexColors = new List<Color>(VertexCapacity);
            private readonly List<ushort> indices = new List<ushort>(IndexCapacity);

            public GlyphRenderPass() {
                mesh = new Mesh {
                    name = "Glyph Mesh"
                };
            }

            public void Init() {
                propertyBlock = new MaterialPropertyBlock();
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
                if (!Settings.IsValid || propertyBlock == null) {
                    return;
                }

                if (mesh == null) {
                    mesh = new Mesh {
                        name = "Glyph Mesh"
                    };
                }
                propertyBlock.SetTexture(_BaseMap, Settings.FontTexture);

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
                        Debug.Log($"Skipped {c} | {(int)c}");
                        continue;
                    }

                    var s = glyphs[idx].Scale(fontScale);
                    var metrics = s.Metrics;
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

                    PushTriangle(idxOffset, idxOffset + 1, idxOffset + 2);
                    PushTriangle(idxOffset, idxOffset + 2, idxOffset + 3);

                    startPos.x += s.Advance - s.Bearings.x;
                }
                SetMesh();

                cmd.DrawMesh(mesh, Matrix4x4.identity, Settings.Material, 0, 0, propertyBlock);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            private void ResetInternalBuffers() {
                vertexPositions.Clear();
                vertexUvs0.Clear();
                vertexColors.Clear();
                indices.Clear();
                mesh.Clear();
            }

            private void PushVertex(float3 position, float2 uv, Color color) {
                vertexPositions.Add(position);
                vertexUvs0.Add(uv);
                vertexColors.Add(color);
            }

            private void PushTriangle(int a, int b, int c) {
                indices.Add((ushort)a);
                indices.Add((ushort)b);
                indices.Add((ushort)c);
            }

            private void SetMesh() {
                mesh.SetVertices(vertexPositions);
                mesh.SetUVs(0, vertexUvs0);
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
            glyphRenderPass.Init();
            renderer.EnqueuePass(glyphRenderPass);
        }
    }
}
