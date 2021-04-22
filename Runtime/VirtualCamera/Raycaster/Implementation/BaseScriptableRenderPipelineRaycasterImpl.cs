#if URP_10_2_OR_NEWER || HDRP_10_2_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Unity.LiveCapture.VirtualCamera.Raycasting
{
    /// <summary>
    /// A base class holding shared functionalities for SRP compatible graphics raycasters.
    /// </summary>
    class BaseScriptableRenderPipelineRaycasterImpl : IRaycasterImpl
    {
        // Object picking is regular graphics raycasting with extra steps.
        // This temporary object implements these extra steps, namely:
        // - Before raycasting: assign object ids to renderers, and build a map of ids to objects.
        // - After raycasting: clear the map.
        protected class PickingScope : IDisposable
        {
            const string k_SamplerName = "Set Graphics Picking Ids";
            static readonly int k_ObjectIdProp = Shader.PropertyToID("_ObjectID");

            // No concurrent access expected.
            static Dictionary<int, GameObject> s_IdToGameObjectMap = new Dictionary<int, GameObject>();
            static Dictionary<object, PickingScope> s_OwnerToInstanceMap = new Dictionary<object, PickingScope>();
            static MaterialPropertyBlock s_PropertyBlock = new MaterialPropertyBlock();
            static CustomSampler s_Sampler;

            object m_Owner;

            public static bool IsActive(object owner) => s_OwnerToInstanceMap.ContainsKey(owner);

            public PickingScope(object owner)
            {
                if (s_OwnerToInstanceMap.ContainsKey(owner))
                    throw new InvalidOperationException(
                        $"At most one {nameof(PickingScope)} is expected to be instantiated for a given owner at any time.");

                m_Owner = owner;
                s_OwnerToInstanceMap.Add(m_Owner, this);

                if (s_Sampler == null)
                    s_Sampler = CustomSampler.Create(k_SamplerName);

                s_Sampler.Begin();
                UpdateObjectIds();
                s_Sampler.End();
            }

            public void Dispose()
            {
                s_OwnerToInstanceMap.Remove(m_Owner);
                s_IdToGameObjectMap.Clear();
                m_Owner = null;
            }

            public bool TryGetObject(int id, out GameObject gameObject)
            {
                return s_IdToGameObjectMap.TryGetValue(id, out gameObject);
            }

            void UpdateObjectIds()
            {
                s_IdToGameObjectMap.Clear();

                foreach (var renderer in Resources.FindObjectsOfTypeAll<Renderer>())
                {
                    if (renderer.gameObject.scene.IsValid())
                    {
                        var id = renderer.gameObject.GetInstanceID();
                        var encodedId = EncodeId(id);
                        renderer.GetPropertyBlock(s_PropertyBlock);
                        s_PropertyBlock.SetVector(k_ObjectIdProp, encodedId);
                        renderer.SetPropertyBlock(s_PropertyBlock);
                        s_IdToGameObjectMap.Add(id, renderer.gameObject);
                    }
                }
            }
        }

        const string k_ProfilingSamplerName = "Graphics Raycast";
        const float k_MaxRawDepthFarPlaneError = 10e-6f;
        static readonly ShaderTagId[] k_ShaderTagsDepthOnly = { new ShaderTagId("DepthOnly") };

        protected GameObject m_Raycaster;
        protected Camera m_Camera;
        protected RenderTexture m_DepthTexture;
        protected RenderTexture m_DepthAsColorTexture;
        protected RenderTexture m_ColorTexture;

        Material m_PickingMaterial;
        ShaderTagId[] m_ShaderTagsObjectId;

        /// <inheritdoc/>
        public virtual void Initialize()
        {
            m_Raycaster = new GameObject("Graphics Raycaster")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            // Create a camera to render to the 1x1 depth buffer. Since the depth texture is just 1 texel,
            // the field of view can be set very small as allowed to achieve better culling.
            m_Camera = m_Raycaster.AddComponent<Camera>();
            m_Camera.pixelRect = new Rect(0, 0, 1, 1);
            m_Camera.enabled = false;
            m_Camera.allowHDR = false;
            m_Camera.allowMSAA = false;
            m_Camera.usePhysicalProperties = false;
            m_Camera.fieldOfView = 0.01f;
        }

        void AllocateDepthResourcesIfNeeded()
        {
            if (m_DepthTexture != null)
                return;

            m_DepthTexture = new RenderTexture(1, 1, 24, RenderTextureFormat.Depth)
            {
                name = "Graphics Raycast Depth Buffer",
                useMipMap = false,
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave,
            };

            m_DepthAsColorTexture = new RenderTexture(1, 1, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear)
            {
                name = "Graphics Raycast Depth Color Buffer",
                useMipMap = false,
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave,
            };
        }

        void AllocatePickingResourcesIfNeeded()
        {
            if (m_ColorTexture != null)
                return;

            m_ColorTexture = new RenderTexture(1, 1, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                name = "Graphics Raycast Color Buffer",
                useMipMap = false,
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave,
            };
        }

        /// <inheritdoc/>
        public virtual void Dispose()
        {
            DestroyIfNeeded(ref m_Raycaster);
            DestroyIfNeeded(ref m_DepthTexture);
            DestroyIfNeeded(ref m_DepthAsColorTexture);
            DestroyIfNeeded(ref m_ColorTexture);
            DestroyIfNeeded(ref m_PickingMaterial);
        }

        static void DestroyIfNeeded<T>(ref T o) where T : Object
        {
            if (o != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(o);
                else
                    Object.DestroyImmediate(o);

                o = null;
            }
        }

        /// <inheritdoc/>
        public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hit, float minDistance, float maxDistance, int layerMask)
        {
            AllocateDepthResourcesIfNeeded();

            // Place the camera at the ray origin, looking in its direction.
            m_Camera.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(direction));
            m_Camera.nearClipPlane = minDistance;
            m_Camera.farClipPlane = maxDistance;
            m_Camera.cullingMask = layerMask;

            m_Camera.Render();
            var rawDepth = ReadDepthSample();

            // Their may be *significant imprecisions* as depth gets closer to the far plane.
            // So we use the raw depth, which is zero at the far plane.
            if (rawDepth < k_MaxRawDepthFarPlaneError)
            {
                hit = default;
                return false;
            }

            var depth = LinearEyeDepth(rawDepth, minDistance, maxDistance);

            hit = new RaycastHit
            {
                distance = depth,
                point = origin + (depth * direction),
                normal = -direction,
            };

            return true;
        }

        public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hit, out GameObject gameObject, float minDistance, float maxDistance, int layerMask)
        {
            AllocatePickingResourcesIfNeeded();

            using (var pickingScope = new PickingScope(this))
            {
                if (Raycast(origin, direction, out hit, minDistance, maxDistance, layerMask))
                {
                    var color = ReadColorSample();
                    var id = DecodeId(color);
                    if (pickingScope.TryGetObject(id, out var result))
                    {
                        gameObject = result;
                        return true;
                    }
                }
            }

            hit = default;
            gameObject = null;
            return false;
        }

        float ReadDepthSample()
        {
            var readback = AsyncGPUReadback.Request(m_DepthAsColorTexture);
            readback.WaitForCompletion();
            if (readback.hasError)
                return 0;
            return readback.GetData<float>()[0];
        }

        Color32 ReadColorSample()
        {
            var readback = AsyncGPUReadback.Request(m_ColorTexture);
            readback.WaitForCompletion();
            if (readback.hasError)
                return Color.clear;
            return readback.GetData<Color32>()[0];
        }

        // Lazily instantiated since it will be invoked rarely if at all.
        Material GetPickingMaterial()
        {
            if (m_PickingMaterial == null)
            {
                var shader = Shader.Find("Hidden/LiveCapture/ObjectPicking");
                Assert.IsNotNull(shader);
                m_PickingMaterial = new Material(shader);
                m_PickingMaterial.hideFlags = HideFlags.HideAndDontSave;
            }

            return m_PickingMaterial;
        }

        // Lazily instantiated since it will be invoked rarely if at all.
        ShaderTagId[] GetShaderTagsObjectId()
        {
            // During the renderer collection process,
            // renderers are filtered based on the tagged passes in their materials,
            // so we aim at having a shaderTag collection covering SRPs built-in materials.
            // If an object cannot be picked,
            // chances are it's because its material does not have a pass whose tag is included here.
            // If the material is an SRP provided one, the tag should be added here,
            // if it's a custom material, it should be tagged properly.
            if (m_ShaderTagsObjectId == null)
                m_ShaderTagsObjectId = new[]
                {
                    new ShaderTagId("Forward"),
                    new ShaderTagId("ForwardOnly"),
                    new ShaderTagId("SRPDefaultUnlit"),
                    new ShaderTagId("GBuffer"),
                    new ShaderTagId("ForwardLit"),
                    new ShaderTagId("Unlit"),
                    new ShaderTagId("UniversalForward"),
                    new ShaderTagId(GetPickingMaterial().GetPassName(0))
                };
            return m_ShaderTagsObjectId;
        }

        static void DrawRenderers(ScriptableRenderContext context, Camera camera, ShaderTagId[] shaderTags, Material material = null, int pass = 0)
        {
            if (camera.TryGetCullingParameters(false, out var cullingParameters))
            {
                var cullingResults = context.Cull(ref cullingParameters);

                var rendererListDesc = new RendererListDesc(shaderTags, cullingResults, camera)
                {
                    rendererConfiguration = PerObjectData.None,
                    renderQueueRange = RenderQueueRange.all,
                    sortingCriteria = SortingCriteria.BackToFront,
                    excludeObjectMotionVectors = false,
                    layerMask = camera.cullingMask,
                    overrideMaterial = material,
                    overrideMaterialPassIndex = pass
                };

                var rendererList = RendererList.Create(rendererListDesc);

                if (rendererList.isValid)
                {
                    if (rendererList.stateBlock == null)
                    {
                        context.DrawRenderers(rendererList.cullingResult, ref rendererList.drawSettings, ref rendererList.filteringSettings);
                    }
                    else
                    {
                        var renderStateBlock = rendererList.stateBlock.Value;
                        context.DrawRenderers(rendererList.cullingResult, ref rendererList.drawSettings, ref rendererList.filteringSettings, ref renderStateBlock);
                    }
                }
                else
                {
                    Debug.LogError("Invalid renderer list!", camera.gameObject);
                }
            }
        }

        protected void DoRender(CommandBuffer cmd, ScriptableRenderContext context, Camera camera)
        {
            var isPickingActive = PickingScope.IsActive(this);

            var colorTexture = isPickingActive ? m_ColorTexture : m_DepthAsColorTexture;
            cmd.SetRenderTarget(colorTexture.colorBuffer, m_DepthTexture.colorBuffer);
            cmd.ClearRenderTarget(true, true, Color.clear);
            cmd.SetViewport(new Rect(0, 0, 1, 1));
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            // Render scene.
            if (isPickingActive)
                DrawRenderers(context, camera, GetShaderTagsObjectId(), GetPickingMaterial(), 0);
            else
                DrawRenderers(context, camera, k_ShaderTagsDepthOnly);

            cmd.Blit(m_DepthTexture.colorBuffer, m_DepthAsColorTexture.colorBuffer);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        static Vector4 EncodeId(int uid)
        {
            var x = ((uid >> 0) & 0xff) / 255.0f;
            var y = ((uid >> 8) & 0xff) / 255.0f;
            var z = ((uid >> 16) & 0xff) / 255.0f;
            var w = ((uid >> 24) & 0xff) / 255.0f;
            return (new Vector4(x, y, z, w));
        }

        static int DecodeId(Color32 color)
        {
            var x = (int)color.r;
            var y = (int)color.g;
            var z = (int)color.b;
            var w = (int)color.a;
            return ((w << 24) | (z << 16) | (y << 8) | x);
        }

        static float LinearEyeDepth(float rawDepth, float near, float far)
        {
            return 1.0f / ((far - near) * rawDepth / (near * far) + 1.0f / far);
        }
    }
}
#endif
