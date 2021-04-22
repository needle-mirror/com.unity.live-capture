#if URP_10_2_OR_NEWER
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Unity.LiveCapture.VirtualCamera
{
    internal class UrpFilmFormatPass : ScriptableRenderPass
    {
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;
            if (camera.cameraType == CameraType.SceneView)
                return;

            if (FilmFormatMap.instance.TryGetInstance(camera, out var filmFormat))
            {
                CommandBuffer cmd = CommandBufferPool.Get(FilmFormat.k_ProfilingSamplerLabel);
                filmFormat.Render(cmd);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }
    }
}
#endif
