#if URP_10_2_OR_NEWER
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// A Scriptable Render Feature responsible for managing virtual camera related render passes,
    /// including the film format and focus plane.
    /// </summary>
    /// <remarks>
    /// The use of a single render feature that manages multiple passes gives a precise control over their submission order,
    /// which is important, for example, if you want to render the film format after the focus plane.
    /// </remarks>
    class VirtualCameraScriptableRenderFeature : ScriptableRendererFeature
    {
        UrpFilmFormatPass m_UrpFilmFormatPass;
        UrpFocusPlaneRenderPass m_UrpFocusPlaneRenderPass;
        UrpFocusPlaneComposePass m_UrpFocusPlaneComposePass;

        public override void Create()
        {
            m_UrpFilmFormatPass = new UrpFilmFormatPass { renderPassEvent = RenderPassEvent.AfterRendering };
            m_UrpFocusPlaneRenderPass = new UrpFocusPlaneRenderPass { renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing };
            m_UrpFocusPlaneComposePass = new UrpFocusPlaneComposePass { renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;

            if (FilmFormatMap.instance.TryGetInstance(camera, out var filmFormat))
            {
                if (filmFormat.ShouldRender())
                {
                    renderer.EnqueuePass(m_UrpFilmFormatPass);
                }
            }

            if (FocusPlaneMap.instance.TryGetInstance(camera, out var focusPlane))
            {
                if (focusPlane.isActiveAndEnabled)
                {
                    focusPlane.AllocateTargetIfNeeded(camera.pixelWidth, camera.pixelHeight);
                    m_UrpFocusPlaneRenderPass.source = renderer.cameraColorTarget;
                    renderer.EnqueuePass(m_UrpFocusPlaneRenderPass);
                    renderer.EnqueuePass(m_UrpFocusPlaneComposePass);
                }
            }
        }
    }
}
#endif
