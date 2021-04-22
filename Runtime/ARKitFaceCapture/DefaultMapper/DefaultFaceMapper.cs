using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.LiveCapture.ARKitFaceCapture.DefaultMapper
{
    /// <summary>
    /// The default <see cref="FaceMapper"/> implementation.
    /// </summary>
    /// <remarks>
    /// The inspector is used to assign a prefab containing a face rig and configure the mapping for the rig.
    /// On assignment, all relevant skinned mesh renderers in the rig are detected and a best guess for the mapping
    /// is automatically generated. The mapping may be shared by different rigs, as long as the transform paths of
    /// all skinned meshes relative to the <see cref="FaceActor"/> components are consistent between the rigs.
    /// </remarks>
    [CreateAssetMenu(fileName = "NewFaceMapper", menuName = "Live Capture/ARKit Face Capture/Mapper")]
    public class DefaultFaceMapper : FaceMapper
    {
        internal enum EyeMovementDriverType
        {
            /// <summary>
            /// Use ARKit eye tracking to determine the eye orientation.
            /// </summary>
            EyeTracking,
            /// <summary>
            /// Calculate the eye orientation from ARKit blend shapes.
            /// </summary>
            BlendShapes,
        }

#if UNITY_EDITOR
#pragma warning disable CS0414
        [SerializeField]
        [Tooltip("Select the prefab containing the face rig to initialize the mapping with.")]
        GameObject m_RigPrefab = null;

        [SerializeField]
        [Tooltip("The threshold determining how close a mesh blend shape name must be to an ARKit blend shape name to automatically be bound.")]
        [Range(0f, 1f)]
        float m_ShapeMatchTolerance = 0.6f;

        [SerializeField]
        [Tooltip("The evaluator preset to assign when a new blend shape binding is created.")]
        EvaluatorPreset m_DefaultEvaluator = null;

        [SerializeField]
        [Tooltip("Change the blend shape editor to show the bindings from blend shapes on the meshes to the ARKit blend shape locations.")]
        bool m_InvertMappings = false;
#pragma warning restore CS0414
#endif

        [SerializeField]
        List<RendererMapping> m_Maps = new List<RendererMapping>();

        [SerializeField]
        [Tooltip("How to drive eye movement.")]
        EyeMovementDriverType m_EyeMovementDriver = EyeMovementDriverType.EyeTracking;

        [SerializeField]
        [Tooltip("The left eye bone transform.")]
        string m_LeftEye = string.Empty;

        [SerializeField]
        [Tooltip("The right eye bone transform.")]
        string m_RightEye = string.Empty;

        [SerializeField]
        [Tooltip("The horizontal and vertical arcs in degrees over which the eyes can rotate.")]
        Vector2 m_EyeAngleRange = new Vector2(45, 30);

        [SerializeField]
        [Tooltip("The horizontal and vertical angles in degrees by which to offset the resting position for the eyes.")]
        Vector2 m_EyeAngleOffset = new Vector2(0, 0);

        [SerializeField]
        [Tooltip("The amount of smoothing to apply to eye movement. " +
            "It can help reduce jitter in the face capture, but it will also smooth out fast motions.")]
        [Range(0f, 1f)]
        float m_EyeSmoothing = 0.1f;

        [SerializeField]
        [Tooltip("The head transform to drive.")]
        string m_Head = string.Empty;

        [SerializeField]
        [Tooltip("The amount of smoothing to apply to head movement. " +
            "It can help reduce jitter in the face capture, but it will also smooth out fast motions.")]
        [Range(0f, 1f)]
        float m_HeadSmoothing = 0.1f;

        struct BlendShapeData
        {
            public int useCount;
            public float weight;
        }

        struct BindingData
        {
            public readonly int shapeIndex;
            public readonly BindingConfig config;
            public float lastWeight;

            public BindingData(int shapeIndex, BindingConfig config)
            {
                this.shapeIndex = shapeIndex;
                this.config = config;
                lastWeight = 0f;
            }
        }

        readonly struct MappingData
        {
            public readonly FaceBlendShape location;
            public readonly BindingData[] bindings;

            public MappingData(FaceBlendShape location, BindingData[] bindings)
            {
                this.location = location;
                this.bindings = bindings;
            }
        }

        readonly struct RendererData
        {
            public readonly SkinnedMeshRenderer renderer;
            public readonly BlendShapeData[] blendShapeData;
            public readonly MappingData[] mappings;

            public RendererData(FaceActor actor, RendererMapping rendererMapping)
            {
                var target = actor == null ? null : actor.transform.Find(rendererMapping.path);
                renderer = target == null ? null : target.GetComponent<SkinnedMeshRenderer>();
                var mesh = renderer == null ? null : renderer.sharedMesh;

                if (mesh == null)
                {
                    blendShapeData = new BlendShapeData[0];
                    mappings = new MappingData[0];
                    return;
                }

                mappings = rendererMapping.bindings
                    .Where(b => b.shapeIndex >= 0 && b.shapeIndex < mesh.blendShapeCount)
                    .ToLookup(b => b.location, b => (b.shapeIndex, b.config))
                    .Select(m => new MappingData(m.Key, m.Select(b => new BindingData(b.shapeIndex, b.config)).ToArray()))
                    .ToArray();

                blendShapeData = new BlendShapeData[mesh.blendShapeCount];

                // count how many face shapes influence each blend shape on the mesh
                foreach (var mapping in mappings)
                {
                    foreach (var binding in mapping.bindings)
                    {
                        blendShapeData[binding.shapeIndex].useCount++;
                    }
                }
            }
        }

        class Cache : FaceMapperCache
        {
            readonly Dictionary<string, Transform> m_Bones = new Dictionary<string, Transform>();
            readonly List<RendererData> m_Renderers = new List<RendererData>();

            public IEnumerable<RendererData> renderers => m_Renderers;

            public Cache(FaceActor actor, IEnumerable<RendererMapping> maps)
            {
                foreach (var map in maps)
                {
                    m_Renderers.Add(new RendererData(actor, map));
                }
            }

            public bool TryGetBone(FaceActor actor, string path, out Transform bone)
            {
                if (!m_Bones.TryGetValue(path, out bone))
                {
                    bone = actor.transform.Find(path);

                    if (bone != null)
                        m_Bones.Add(path, bone);
                }

                return bone != null;
            }
        }

        void OnValidate()
        {
            var usedPaths = new HashSet<string>();

            for (var i = 0; i < m_Maps.Count;)
            {
                var rendererMapping = m_Maps[i];
                var path = rendererMapping.path;

                if (!string.IsNullOrEmpty(path) && usedPaths.Contains(path))
                {
                    m_Maps.RemoveAt(i);
                    Debug.LogError($"Face mapper \"{name}\" has multiple mappings for renderer \"{rendererMapping}\". The extra mapping has been removed.");
                    continue;
                }
                usedPaths.Add(path);

                rendererMapping.Validate(this);
                i++;
            }
        }

        /// <inheritdoc />
        public override FaceMapperCache CreateCache(FaceActor actor)
        {
            return new Cache(actor, m_Maps);
        }

        /// <inheritdoc />
        public override void ApplyBlendShapesToRig(FaceActor actor, FaceMapperCache cache, ref FacePose pose, bool continuous)
        {
            if (actor == null)
                return;

            var c = cache as Cache;

            foreach (var data in c.renderers)
            {
                // ensure the references are still valid
                if (data.renderer == null || data.renderer.sharedMesh == null)
                    continue;

                // clear the blend shape values
                for (var i = 0; i < data.blendShapeData.Length; i++)
                    data.blendShapeData[i].weight = 0f;

                // Compute the new weights. We sum the weights from all influences to get the final
                // result. It may be better to let the user select from multiple techniques instead.
                for (var i = 0; i < data.mappings.Length; i++)
                {
                    var mapping = data.mappings[i];
                    var bindings = mapping.bindings;
                    var value = pose.blendShapes.GetValue(mapping.location);

                    for (var j = 0; j < mapping.bindings.Length; j++)
                    {
                        var binding = bindings[j];
                        var config = binding.config;
                        var weight = config.GetEvaluator().Evaluate(value);

                        if (continuous)
                            weight = Mathf.Lerp(weight, binding.lastWeight, config.smoothing);

                        data.blendShapeData[binding.shapeIndex].weight += weight;
                        data.mappings[i].bindings[j].lastWeight = weight;
                    }
                }

                // apply the blend shape weights to the skinned mesh
                for (var i = 0; i < data.blendShapeData.Length; i++)
                {
                    if (data.blendShapeData[i].useCount > 0)
                        data.renderer.SetBlendShapeWeight(i, data.blendShapeData[i].weight);
                }
            }
        }

        /// <inheritdoc />
        public override void ApplyHeadRotationToRig(FaceActor actor, FaceMapperCache cache, ref FacePose pose, bool continuous)
        {
            if (actor == null)
                return;

            var c = cache as Cache;

            if (!string.IsNullOrEmpty(m_Head) && c.TryGetBone(actor, m_Head, out var head))
            {
                head.localRotation = continuous ? Quaternion.Slerp(pose.headOrientation, head.localRotation, m_HeadSmoothing) : pose.headOrientation;
            }
        }

        /// <inheritdoc />
        public override void ApplyEyeRotationToRig(FaceActor actor, FaceMapperCache cache, ref FacePose pose, bool continuous)
        {
            if (actor == null)
                return;

            var c = cache as Cache;

            if (!string.IsNullOrEmpty(m_LeftEye) && c.TryGetBone(actor, m_LeftEye, out var leftEye))
            {
                switch (m_EyeMovementDriver)
                {
                    case EyeMovementDriverType.BlendShapes:
                    {
                        var leftEyeH = pose.blendShapes.GetValue(FaceBlendShape.EyeLookInLeft) - pose.blendShapes.GetValue(FaceBlendShape.EyeLookOutLeft);
                        var leftEyeV = pose.blendShapes.GetValue(FaceBlendShape.EyeLookUpLeft) - pose.blendShapes.GetValue(FaceBlendShape.EyeLookDownLeft);

                        var leftEyeYaw = Quaternion.AngleAxis(leftEyeH * m_EyeAngleRange.x + m_EyeAngleOffset.x, Vector3.up);
                        var leftEyePitch = Quaternion.AngleAxis(leftEyeV * m_EyeAngleRange.y + m_EyeAngleOffset.y, Vector3.left);
                        var targetRotation = leftEyePitch * leftEyeYaw;

                        leftEye.localRotation = continuous ? Quaternion.Slerp(targetRotation, leftEye.localRotation, m_EyeSmoothing) : targetRotation;

                        break;
                    }
                    case EyeMovementDriverType.EyeTracking:
                    {
                        var targetRotation = continuous ? Quaternion.Slerp(pose.leftEyeOrientation, leftEye.localRotation, m_EyeSmoothing) : pose.leftEyeOrientation;

                        var offsetXRotation = Quaternion.AngleAxis(m_EyeAngleOffset.x, Vector3.up);
                        var offsetYRotation = Quaternion.AngleAxis(m_EyeAngleOffset.y, Vector3.left);
                        var offsetRotation = offsetXRotation * offsetYRotation;
                        targetRotation *= offsetRotation;

                        leftEye.localRotation = targetRotation;

                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(m_RightEye) && c.TryGetBone(actor, m_RightEye, out var rightEye))
            {
                switch (m_EyeMovementDriver)
                {
                    case EyeMovementDriverType.BlendShapes:
                    {
                        var rightEyeH = pose.blendShapes.GetValue(FaceBlendShape.EyeLookInRight) - pose.blendShapes.GetValue(FaceBlendShape.EyeLookOutRight);
                        var rightEyeV = pose.blendShapes.GetValue(FaceBlendShape.EyeLookUpRight) - pose.blendShapes.GetValue(FaceBlendShape.EyeLookDownRight);

                        var rightEyeYaw = Quaternion.AngleAxis(rightEyeH * m_EyeAngleRange.x + m_EyeAngleOffset.x, Vector3.down);
                        var rightEyePitch = Quaternion.AngleAxis(rightEyeV * m_EyeAngleRange.y + m_EyeAngleOffset.y, Vector3.left);
                        var targetRotation = rightEyePitch * rightEyeYaw;

                        rightEye.localRotation = continuous ? Quaternion.Slerp(targetRotation, rightEye.localRotation, m_EyeSmoothing) : targetRotation;

                        break;
                    }
                    case EyeMovementDriverType.EyeTracking:
                    {
                        var targetRotation = continuous ? Quaternion.Slerp(pose.rightEyeOrientation, rightEye.localRotation, m_EyeSmoothing) : pose.rightEyeOrientation;

                        var offsetXRotation = Quaternion.AngleAxis(m_EyeAngleOffset.x, Vector3.down);
                        var offsetYRotation = Quaternion.AngleAxis(m_EyeAngleOffset.y, Vector3.left);
                        var offsetRotation = offsetXRotation * offsetYRotation;
                        targetRotation *= offsetRotation;

                        rightEye.localRotation = targetRotation;
                        break;
                    }
                }
            }
        }
    }
}
