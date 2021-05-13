#if HDRP_10_2_OR_NEWER
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Unity.LiveCapture.Rendering.Editor
{
    static class HDRPEditorUtilities
    {
        static HDRenderPipelineAsset GetAsset()
        {
            var asset = default(HDRenderPipelineAsset);
            var hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;

            // Can return null on domain reload
            if (hdPipeline != null)
            {
                var assetField = typeof(HDRenderPipeline).GetField("m_Asset", BindingFlags.NonPublic | BindingFlags.Instance);

                asset = assetField.GetValue(hdPipeline) as HDRenderPipelineAsset;
            }

            return asset;
        }

        /// <summary>
        /// Checks if a post effect is present in the HDRP Settings.
        /// </summary>
        /// <param name="injectionPoint">Injection point of the post effect in the rendering pipeline.</param>
        /// <typeparam name="T">Type of the post effect, subclass of CustomPostProcessVolumeComponent.</typeparam>
        /// <returns>Whether or not the post effect is present.</returns>
        public static bool ContainsPostEffect<T>(CustomPostProcessInjectionPoint injectionPoint) where T : CustomPostProcessVolumeComponent
        {
            var asset = GetAsset();

            if (asset != null)
            {
                var name = typeof(T).AssemblyQualifiedName;
                var serializedObject = new SerializedObject(asset);
                var postProcessProp = serializedObject.FindProperty(injectionPoint.GetPropertyName());

                while (postProcessProp.Next(true))
                {
                    if (postProcessProp.type.Equals("string") && postProcessProp.stringValue.Equals(name))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Adds a post effect to HDRP Settings if it is not already there.
        /// </summary>
        /// <param name="injectionPoint">Injection point of the post effect in the rendering pipeline.</param>
        /// <typeparam name="T">Type of the post effect, subclass of CustomPostProcessVolumeComponent.</typeparam>
        public static void AddPostEffect<T>(CustomPostProcessInjectionPoint injectionPoint) where T : CustomPostProcessVolumeComponent
        {
            var asset = GetAsset();

            if (asset != null && !ContainsPostEffect<T>(injectionPoint))
            {
                var name = typeof(T).AssemblyQualifiedName;
                var serializedObject = new SerializedObject(asset);
                var postProcessProp = serializedObject.FindProperty(injectionPoint.GetPropertyName());

                Debug.Assert(postProcessProp.isArray);

                serializedObject.Update();

                var arraySize = postProcessProp.arraySize;
                postProcessProp.arraySize = arraySize + 1;
                var elementProp = postProcessProp.GetArrayElementAtIndex(arraySize);

                elementProp.stringValue = typeof(T).AssemblyQualifiedName;

                serializedObject.ApplyModifiedProperties();
            }
        }

        static string GetPropertyName(this CustomPostProcessInjectionPoint injectionPoint)
        {
            switch (injectionPoint)
            {
                case CustomPostProcessInjectionPoint.AfterOpaqueAndSky:
                    return "beforeTransparentCustomPostProcesses";
                case CustomPostProcessInjectionPoint.BeforePostProcess:
                    return "beforePostProcessCustomPostProcesses";
                case CustomPostProcessInjectionPoint.AfterPostProcess:
                    return "afterPostProcessCustomPostProcesses";
            }
            return string.Empty;
        }
    }
}
#endif
