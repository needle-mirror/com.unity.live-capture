using UnityEditor;
using UnityEngine;

#if URP_10_2_OR_NEWER
using UnityEngine.Rendering.Universal;
#endif

namespace Unity.LiveCapture.Rendering.Editor
{
    internal class RenderPipelineBridgeImpl : IRenderPipelineBridge
    {
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            RenderPipelineBridge.SetImplementation(new RenderPipelineBridgeImpl());
        }

#if URP_10_2_OR_NEWER
        public T RequestRenderFeature<T>() where T : ScriptableRendererFeature
        {
            if (URPUtility.HasRenderFeature<T>(out var feature))
                return feature;

            return URPUtility.AddRenderFeature<T>();
        }

        public bool HasRenderFeature<T>(out T result) where T : ScriptableRendererFeature
        {
            return URPUtility.HasRenderFeature<T>(out result);
        }

#endif
    }
}
