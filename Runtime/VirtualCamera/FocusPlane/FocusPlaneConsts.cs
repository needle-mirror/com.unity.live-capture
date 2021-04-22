using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    static class FocusPlaneConsts
    {
        internal const string k_RenderProfilingSamplerLabel = "Focus Plane Render";
        internal const string k_ComposePlaneProfilingSamplerLabel = "Focus Plane Compose";
        internal static readonly int k_InputTextureProperty = Shader.PropertyToID("_InputTexture");
    }
}
