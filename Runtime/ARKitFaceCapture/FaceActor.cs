using UnityEngine;

namespace Unity.LiveCapture.ARKitFaceCapture
{
    /// <summary>
    /// A component that applies a <see cref="FacePose"/> to a character in a scene.
    /// </summary>
    [DisallowMultipleComponent]
    [ExcludeFromPreset]
    [AddComponentMenu("Live Capture/ARKit Face Capture/ARKit Face Actor")]
    [ExecuteAlways]
    [RequireComponent(typeof(Animator))]
    public sealed class FaceActor : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The asset that configures how face pose data is mapped to this character's face rig.")]
        FaceMapper m_Mapper = null;

        [SerializeField]
        [Tooltip("The channels of face capture data to apply to this actor. This allows for recording all channels of a face capture, while later being able to use select parts of the capture.")]
        [EnumFlagButtonGroup(100f)]
        FaceChannelFlags m_EnabledChannels = FaceChannelFlags.All;

        [SerializeField]
        [Tooltip("The current face pose.")]
        FacePose m_Pose = FacePose.identity;

        [SerializeField]
        bool m_BlendShapesEnabled;
        [SerializeField]
        bool m_HeadOrientationEnabled;
        [SerializeField]
        bool m_EyeOrientationEnabled;

        FaceMapperCache m_Cache;
        FaceChannelFlags m_LastChannels;

        /// <summary>
        /// The Animator component used by the device for playing takes on this actor.
        /// </summary>
        public Animator animator { get; private set; }

        /// <summary>
        /// The asset that configures how face pose data is mapped to this character's face rig.
        /// </summary>
        public FaceMapper mapper => m_Mapper;

        /// <summary>
        /// The current face pose.
        /// </summary>
        public FacePose facePose
        {
            get => m_Pose;
            set => m_Pose = value;
        }

        void Awake()
        {
            animator = GetComponent<Animator>();
        }

        void OnDisable()
        {
            ClearCache();
        }

        void LateUpdate()
        {
            UpdateRig(true);
        }

        /// <summary>
        /// Sets the face mapper used by this face rig.
        /// </summary>
        /// <param name="mapper">The mapper to use, or null to clear the current mapper.</param>
        public void SetMapper(FaceMapper mapper)
        {
            m_Mapper = mapper;
            ClearCache();
        }

        /// <summary>
        /// Clears the mapper state cache for the actor, causing it to rebuild the next
        /// time the face rig updates.
        /// </summary>
        public void ClearCache()
        {
            m_Cache?.Dispose();
            m_Cache = null;
        }

        /// <summary>
        /// Updates the face rig from the current pose.
        /// </summary>
        /// <param name="continuous">When true, the new pose follows the current pose and they
        /// can be smoothed between, while false corresponds to a seek in the animation where the
        /// previous pose is invalidated and should not influence the new pose.</param>
        public void UpdateRig(bool continuous)
        {
            if (m_Mapper != null)
            {
                if (m_Cache == null)
                {
                    m_Cache = m_Mapper.CreateCache(this);

                    // we can't use any previous state since there is none yet
                    continuous = false;
                }

                // determine which face channels are enabled and have data to use
                var channels = m_EnabledChannels;

                if (!m_BlendShapesEnabled)
                {
                    channels &= ~FaceChannelFlags.BlendShapes;
                }
                if (!m_HeadOrientationEnabled)
                {
                    channels &= ~FaceChannelFlags.Head;
                }
                if (!m_EyeOrientationEnabled)
                {
                    channels &= ~FaceChannelFlags.Eyes;
                }

                // apply the active face channels
                if (channels.HasFlag(FaceChannelFlags.BlendShapes))
                {
                    m_Mapper.ApplyBlendShapesToRig(this, m_Cache, ref m_Pose, continuous && m_LastChannels.HasFlag(FaceChannelFlags.BlendShapes));
                }
                if (channels.HasFlag(FaceChannelFlags.Head))
                {
                    m_Mapper.ApplyHeadRotationToRig(this, m_Cache, ref m_Pose, continuous && m_LastChannels.HasFlag(FaceChannelFlags.Head));
                }
                if (channels.HasFlag(FaceChannelFlags.Eyes))
                {
                    m_Mapper.ApplyEyeRotationToRig(this, m_Cache, ref m_Pose, continuous && m_LastChannels.HasFlag(FaceChannelFlags.Eyes));
                }

                m_LastChannels = channels;
            }
        }
    }
}
