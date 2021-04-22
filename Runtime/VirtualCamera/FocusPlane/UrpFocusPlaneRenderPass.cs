#if URP_10_2_OR_NEWER
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// Pass that renders the focus plane to an intermediary render target.
    /// </summary>
    class UrpFocusPlaneRenderPass : ScriptableRenderPass
    {
        public RenderTargetIdentifier source;

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;
            if (camera.cameraType == CameraType.SceneView)
                return;

            if (FocusPlaneMap.instance.TryGetInstance(camera, out var focusPlane))
            {
                if (focusPlane.isActiveAndEnabled && focusPlane.TryGetRenderTarget(out RenderTexture target))
                {
                    CommandBuffer cmd = CommandBufferPool.Get(FocusPlaneConsts.k_RenderProfilingSamplerLabel);
                    Blit(cmd, source, target, focusPlane.renderMaterial);
                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }
            }
        }
    }
}
#endif
