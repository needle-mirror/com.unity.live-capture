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
        Lens m_Lens = Lens.DefaultParams;
        [SerializeField]
        LensIntrinsics m_LensIntrinsics = LensIntrinsics.DefaultParams;
        [SerializeField]
        CameraBody m_CameraBody = CameraBody.DefaultParams;
        [SerializeField]
        bool m_DepthOfField;
        [SerializeField, AspectRatio]
        float m_CropAspect = Settings.k_DefaultAspectRatio;

        /// <summary>
        /// The Animator component used by the device for playing takes on this actor.
        /// </summary>
        public Animator Animator { get; private set; }

        /// <summary>
        /// The <see cref="VirtualCamera.Lens"/> parameters of the actor.
        /// </summary>
        /// <remarks>
        /// The parameters will be used by a camera driver to configure the final camera component.
        /// </remarks>
        public Lens Lens => m_Lens;

        /// <summary>
        /// The <see cref="VirtualCamera.LensIntrinsics"/> parameters of the actor.
        /// </summary>
        /// <remarks>
        /// The parameters will be used by a camera driver to configure the final camera component.
        /// </remarks>
        public LensIntrinsics LensIntrinsics => m_LensIntrinsics;

        /// <summary>
        /// The <see cref="VirtualCamera.CameraBody"/> parameters of the actor.
        /// </summary>
        /// <remarks>
        /// The parameters will be used by a camera driver to configure the final camera component.
        /// </remarks>
        public CameraBody CameraBody => m_CameraBody;

        /// <summary>
        /// Is depth of field enabled.
        /// </summary>
        /// <remarks>
        /// The camera driver will use this property to enable or disable depth of field for the camera.
        /// </remarks>
        public bool DepthOfFieldEnabled => m_DepthOfField;

        /// <summary>
        /// The aspect ratio of the crop mask.
        /// </summary>
        public float CropAspect => m_CropAspect;

        void Awake()
        {
            Animator = GetComponent<Animator>();
        }

        void OnValidate()
        {
            m_LensIntrinsics.Validate();
            m_Lens.Validate(m_LensIntrinsics);
            m_CameraBody.Validate();
        }
    }
}
