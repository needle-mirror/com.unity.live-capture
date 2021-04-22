using UnityEngine;

#if URP_10_2_OR_NEWER
using UnityEngine.Rendering.Universal;
#endif

namespace Unity.LiveCapture.Rendering
{
    /// <summary>
    /// Provides access to a set of render-pipeline related editor-side features.
    /// </summary>
    /// <remarks>Primarily introduced so that runtime code can request render features directly.</remarks>
    public class RenderPipelineBridge
    {
        static IRenderPipelineBridge s_Impl = RenderPipelineBridgeDefaultImpl.s_Instance;

        internal static void SetImplementation(IRenderPipelineBridge impl)
        {
            if (s_Impl != RenderPipelineBridgeDefaultImpl.s_Instance && s_Impl != impl)
                Debug.LogWarning($"Overriding non-default implementation of {nameof(RenderPipelineBridge)}, override is expected to occur once.");

            s_Impl = impl;
        }

#if URP_10_2_OR_NEWER
        /// <summary>
        /// Returns a reference to a render feature and adds this reference it if not already active.
        /// </summary>
        public static T RequestRenderFeature<T>() where T : ScriptableRendererFeature
        {
            return s_Impl.RequestRenderFeature<T>();
        }

        /// <summary>
        /// Whether or not a render feature is currently active on the renderer.
        /// </summary>
        /// <param name="result">The render feature if active.</param>
        public static bool HasRenderFeature<T>(out T result) where T : ScriptableRendererFeature
        {
            return s_Impl.HasRenderFeature<T>(out result);
        }

#endif
    }
}
