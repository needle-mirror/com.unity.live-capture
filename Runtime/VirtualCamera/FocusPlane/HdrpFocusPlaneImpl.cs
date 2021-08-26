#if HDRP_10_2_OR_NEWER
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Experimental.Rendering;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// HDRP specific focus plane implementation.
    /// </summary>
    class HdrpFocusPlaneImpl : IFocusPlaneImpl, IRenderTargetProvider<RTHandle>
    {
        CustomPassManager.Handle<HdrpFocusPlaneRenderPass> m_RenderPassHandle;
        CustomPassManager.Handle<HdrpFocusPlaneComposePass> m_ComposePassHandle;
        RTHandle m_Target;
        Material m_ComposeMaterial;
        bool m_InitializedTarget;

        public HdrpFocusPlaneImpl(Material composeMaterial)
        {
            m_ComposeMaterial = composeMaterial;
        }

        /// <inheritdoc/>
        public bool TryGetRenderTarget<T>(out T target)
        {
            if (this is IRenderTargetProvider<T> specialized)
            {
                return specialized.TryGetRenderTarget(out target);
            }

            target = default(T);
            return false;
        }

        /// <inheritdoc/>
        bool IRenderTargetProvider<RTHandle>.TryGetRenderTarget(out RTHandle target)
        {
            target = m_Target;
            return true;
        }

        /// <inheritdoc/>
        public void Initialize()
        {
            m_RenderPassHandle = new CustomPassManager.Handle<HdrpFocusPlaneRenderPass>(CustomPassInjectionPoint.BeforePostProcess);
            m_ComposePassHandle = new CustomPassManager.Handle<HdrpFocusPlaneComposePass>(CustomPassInjectionPoint.AfterPostProcess);
            m_RenderPassHandle.GetPass().name = FocusPlaneConsts.RenderProfilingSamplerLabel;
            m_ComposePassHandle.GetPass().name = FocusPlaneConsts.ComposePlaneProfilingSamplerLabel;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            m_RenderPassHandle.Dispose();
            m_ComposePassHandle.Dispose();
            m_ComposeMaterial = null;

            if (m_InitializedTarget)
            {
                m_Target.Release();
                m_InitializedTarget = false;
            }
        }

        /// <inheritdoc/>
        public bool AllocateTargetIfNeeded(int width, int height)
        {
            if (!m_InitializedTarget)
            {
                m_Target = RTHandles.Alloc(Vector2.one, TextureXR.slices,
                    dimension: TextureXR.dimension, colorFormat: GraphicsFormat.R8G8B8A8_SRGB,
                    useDynamicScale: true, name: "Focus Plane Buffer");
                m_InitializedTarget = true;

                m_ComposeMaterial.SetTexture(FocusPlaneConsts.InputTextureProperty, m_Target);

                return true;
            }

            return false;
        }

        // These 2 methods are only used with the Legacy Render Pipeline so far.

        /// <inheritdoc/>
        public void SetCamera(Camera camera) {}

        /// <inheritdoc/>
        public void Update() {}
    }
}
#endif
