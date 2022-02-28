#if URP_10_2_OR_NEWER
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// Pass that renders the focus plane to an intermediary render target.
    /// </summary>
    /// We find ourselves supporting both RenderTexture and RTHandle,
    /// as URP migrated to RTHandle at v13.1.2
    /// </remarks>
#if URP_13_1_2_OR_NEWER
    class UrpFocusPlaneRenderPass : ScriptableRenderPass
    {
        public RTHandle Source;

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;
            if (camera.cameraType == CameraType.SceneView)
                return;

            if (FocusPlaneMap.Instance.TryGetInstance(camera, out var focusPlane))
            {
                if (focusPlane.isActiveAndEnabled && focusPlane.TryGetRenderTarget(out RTHandle target))
                {
                    // URP does not yet use scaling but it is upcoming so this will prevent us from missing the landing.
                    // We should try and lean on URP's built-in blitting utilities if possible.
                    if (!(Mathf.Approximately(target.scaleFactor.x, 1) && Mathf.Approximately(target.scaleFactor.y, 1)))
                    {
                        throw new InvalidOperationException("Scaling of renderTarget not supported yet.");
                    }

                    CommandBuffer cmd = CommandBufferPool.Get(FocusPlaneConsts.RenderProfilingSamplerLabel);
                    Blit(cmd, Source, target, focusPlane.RenderMaterial);
                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }
            }
        }
    }
#else
    class UrpFocusPlaneRenderPass : ScriptableRenderPass
    {
        public RenderTargetIdentifier Source;

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;
            if (camera.cameraType == CameraType.SceneView)
                return;

            if (FocusPlaneMap.Instance.TryGetInstance(camera, out var focusPlane))
            {
                if (focusPlane.isActiveAndEnabled && focusPlane.TryGetRenderTarget(out RenderTexture target))
                {
                    CommandBuffer cmd = CommandBufferPool.Get(FocusPlaneConsts.RenderProfilingSamplerLabel);
                    Blit(cmd, Source, target, focusPlane.RenderMaterial);
                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }
            }
        }
    }
#endif
}
#endif
