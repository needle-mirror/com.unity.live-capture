#if HDRP_10_2_OR_NEWER
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// A Custom Pass to render the film format associated with a Camera.
    /// </summary>
    /// <remarks>
    /// Note the order attribute, the film format should be rendered after the focus plane.
    /// </remarks>
    [CustomPassOrder(10)]
    class HdrpFilmFormatPass : CustomPass
    {
        protected override void Execute(CustomPassContext ctx)
        {
            var baseCamera = ctx.hdCamera.camera;
            if (baseCamera.cameraType == CameraType.SceneView)
                return;

            if (FilmFormatMap.instance.TryGetInstance(baseCamera, out var filmFormat))
            {
                if (filmFormat.ShouldRender())
                {
                    CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ctx.cameraDepthBuffer, ClearFlag.None);
                    filmFormat.Render(ctx.cmd);
                }
            }
        }
    }
}
#endif
