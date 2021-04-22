using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// Replicates some functionalities of SRP CoreUtils.
    /// </summary>
    /// <remarks>
    /// Introduced since SRP is an optional dependency.
    /// </remarks>
    static class AdditionalCoreUtils
    {
        /// <summary>
        /// Creates and returns a reference to an empty GameObject.
        /// </summary>
        /// <remarks>
        /// This is a temporary workaround method. You might fail to create GameObjects via the `new GameObject()` method in some circumstances,
        /// for example when you invoke it in OnEnable through a component that you just added manually in the Inspector window,
        /// depending on the Editor configuration.
        /// See https://fogbugz.unity3d.com/f/cases/1196137/.
        /// </remarks>
        public static GameObject CreateEmptyGameObject()
        {
            var result = GameObject.CreatePrimitive(PrimitiveType.Cube);
            // Strip all components but the transform to get an empty game object.
            var components = result.GetComponents<Component>();
            foreach (var component in components)
            {
                if (component is Transform)
                    continue;

                Object.DestroyImmediate(component);
            }

            return result;
        }

        /// <summary>
        /// Creates a Material with the provided shader path.
        /// This sets hideFlags to HideFlags.HideAndDontSave.
        /// </summary>
        /// <param name="shaderPath">Path of the shader used for the material.</param>
        /// <returns>A new Material instance using the shader found at the provided path.</returns>
        public static Material CreateEngineMaterial(string shaderPath)
        {
            var shader = Shader.Find(shaderPath);
            if (shader == null)
            {
                Debug.LogError("Cannot create required material because shader " + shaderPath + " could not be found");
                return null;
            }

            var mat = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return mat;
        }

        /// <summary>
        /// Destroys a UnityObject safely.
        /// </summary>
        /// <param name="obj">Object to destroy.</param>
        public static void Destroy(Object obj)
        {
            if (obj != null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    Object.Destroy(obj);
                else
                    Object.DestroyImmediate(obj);
#else
                Object.Destroy(obj);
#endif
            }
        }
    }
}
