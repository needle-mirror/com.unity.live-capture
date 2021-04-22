using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// The actor used to hold the parameters of a camera.
    /// </summary>
    [DisallowMultipleComponent]
    [ExcludeFromPreset]
    [AddComponentMenu("Live Capture/Virtual Camera/Virtual Camera Actor")]
    [ExecuteAlways]
    [RequireComponent(typeof(Animator))]
    public class VirtualCameraActor : MonoBehaviour
    {
        [SerializeField]
        Lens m_Lens = Lens.defaultParams;

        [SerializeField]
        CameraBody m_CameraBody = CameraBody.defaultParams;

        [SerializeField, Tooltip("Depth of field enabled state")]
        bool m_DepthOfField;

        Animator m_Animator;

        /// <summary>
        /// The Animator component used by the device for playing takes on this actor.
        /// </summary>
        public Animator animator { get => m_Animator; }

        /// <summary>
        /// The <see cref="Lens"/> parameters of the actor. The parameters will be used by a camera driver
        /// to configure the final camera component.
        /// </summary>
        public Lens lens => m_Lens;

        /// <summary>
        /// The <see cref="CameraBody"/> parameters of the actor. The parameters will be used by a camera driver
        /// to configure the final camera component.
        /// </summary>
        public CameraBody cameraBody => m_CameraBody;

        /// <summary>
        /// Depth of field enabled state. The camera driver will use this property to enable or disable the
        /// depth of field backend.
        /// </summary>
        public bool depthOfFieldEnabled => m_DepthOfField;

        void Awake()
        {
            m_Animator = GetComponent<Animator>();
        }

        void Reset()
        {
            m_Lens = Lens.defaultParams;
            m_CameraBody = CameraBody.defaultParams;
            m_DepthOfField = false;
        }

        void OnValidate()
        {
            m_Lens.Validate();
            m_CameraBody.Validate();
        }
    }
}
