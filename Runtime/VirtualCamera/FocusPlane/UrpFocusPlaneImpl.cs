#if URP_10_2_OR_NEWER
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// URP specific focus plane implementation.
    /// </summary>
    class UrpFocusPlaneImpl : IFocusPlaneImpl, IRenderTargetProvider<RenderTexture>
    {
        RenderTexture m_Target;
        Material m_RenderMaterial;
        Material m_ComposeMaterial;

        /// <inheritdoc/>
        public Material RenderMaterial => m_RenderMaterial;

        /// <inheritdoc/>
        public Material ComposeMaterial => m_ComposeMaterial;

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
            m_RenderMaterial = CoreUtils.CreateEngineMaterial("Hidden/LiveCapture/FocusPlane/Render/Urp");
            m_ComposeMaterial = CoreUtils.CreateEngineMaterial("Hidden/LiveCapture/FocusPlane/Compose/Urp");
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            CoreUtils.Destroy(m_RenderMaterial);
            CoreUtils.Destroy(m_ComposeMaterial);

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
}
#endif
