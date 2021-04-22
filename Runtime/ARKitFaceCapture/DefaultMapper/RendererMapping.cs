using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Unity.LiveCapture.ARKitFaceCapture.DefaultMapper
{
    /// <summary>
    /// Defines mappings from <see cref="FaceBlendShape"/> values to blend shapes on a skinned mesh renderer.
    /// </summary>
    [Serializable]
    class RendererMapping
    {
        [SerializeField]
        [Tooltip("The renderer in the rig prefab these mappings apply to.")]
        string m_Path = string.Empty;

        [SerializeField]
        Binding[] m_Bindings = new Binding[0];

#pragma warning disable CS0414
        [SerializeField, HideInInspector]
        bool m_IsExpanded = true;
#pragma warning restore CS0414

        /// <summary>
        /// The transform path from the <see cref="FaceActor"/> component to the target SkinnedMeshRenderer component.
        /// </summary>
        public string path => m_Path;

        /// <summary>
        /// The blend shape mappings for this renderer.
        /// </summary>
        public Binding[] bindings => m_Bindings;

        /// <summary>
        /// Fixes or reports issues in the mapping to ensure it is correctly defined.
        /// </summary>
        /// <param name="mapper">The mapper that owns this mapping.</param>
        public void Validate(FaceMapper mapper)
        {
            var usedKeys = new HashSet<(FaceBlendShape, int)>();

            foreach (var binding in bindings)
            {
                var key = (binding.location, binding.shapeIndex);

                if (usedKeys.Contains(key))
                {
                    Debug.LogError($"Renderer \"{this}\" in face mapper \"{mapper.name}\" has multiple bindings for {binding}. " +
                        $"Try removing the duplicate, or re-initialize the mapping for the renderer to fix the asset.");
                    continue;
                }
                usedKeys.Add(key);
            }
        }

        /// <inheritdoc />
        public override string ToString() => Path.GetFileName(path);
    }
}
