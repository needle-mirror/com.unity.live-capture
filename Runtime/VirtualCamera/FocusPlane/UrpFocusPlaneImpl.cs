#if URP_10_2_OR_NEWER
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// URP specific focus plane implementation.
    /// </summary>
    /// <remarks>
    /// We find ourselves supporting both RenderTexture and RTHandle,
    /// as URP migrated to RTHandle at v13.1.2
    /// </remarks>
#if URP_13_1_2_OR_NEWER
    class UrpFocusPlaneImpl : RTHandleFocusPlaneImpl
    {
        public UrpFocusPlaneImpl(Material composeMaterial) : base(composeMaterial)
        {
        }
    }
#else
    class UrpFocusPlaneImpl : IFocusPlaneImpl, IRenderTargetProvider<RenderTexture>
    {
        RenderTexture m_Target;
        Material m_ComposeMaterial;

        public UrpFocusPlaneImpl(Material composeMaterial)
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
        bool IRenderTargetProvider<RenderTexture>.TryGetRenderTarget(out RenderTexture target)
        {
            target = m_Target;
            return true;
        }

        /// <inheritdoc/>
        public void Initialize()
        {
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            m_ComposeMaterial = null;

            if (m_Target != null)
            {
                m_Target.Release();
                m_Target = null;
            }
        }

        /// <inheritdoc/>
        public bool AllocateTargetIfNeeded(int width, int height)
        {
            if (m_Target == null || m_Target.width != width || m_Target.height != height)
            {
                if (m_Target != null)
                    m_Target.Release();

                m_Target = new RenderTexture(width, height, 0, RenderTextureFormat.BGRA32)
                {
                    hideFlags = HideFlags.DontSave
                };

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
#endif
}
#endif
