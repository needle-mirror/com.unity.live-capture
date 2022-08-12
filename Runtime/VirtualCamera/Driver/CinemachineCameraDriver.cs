using UnityEngine;
#if VP_CINEMACHINE_2_4_0
using Cinemachine;
#endif

namespace Unity.LiveCapture.VirtualCamera
{
    [AddComponentMenu("")]
    [HelpURL(Documentation.baseURL + "ref-component-cinemachine-camera-driver" + Documentation.endURL)]
    class CinemachineCameraDriver : BaseCameraDriver, ICustomDamping
    {
#if VP_CINEMACHINE_2_4_0
        [SerializeField, Tooltip("Cinemachine camera driver component.")]
        CinemachineDriverComponent m_CinemachineComponent = new CinemachineDriverComponent();
#if HDRP_10_2_OR_NEWER
        [SerializeField, Tooltip("High Definition Render Pipeline camera driver component.")]
        HdrpCinemachineCameraDriverComponent m_HdrpCinemachineComponent = new HdrpCinemachineCameraDriverComponent();
#endif
#if URP_10_2_OR_NEWER
        [SerializeField, Tooltip("Universal Render Pipeline camera driver component.")]
        UrpCinemachineCameraDriverComponent m_UrpComponent = new UrpCinemachineCameraDriverComponent();
#endif
        ICameraDriverImpl m_Impl;

        public CinemachineVirtualCamera CinemachineVirtualCamera
        {
            get => m_CinemachineComponent.CinemachineVirtualCamera;
            set
            {
                m_CinemachineComponent.CinemachineVirtualCamera = value;
                Validate();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (m_Impl == null)
            {
                m_Impl = new CompositeCameraDriverImpl(new ICameraDriverComponent[]
                {
                    m_CinemachineComponent,
#if HDRP_10_2_OR_NEWER
                    m_HdrpCinemachineComponent,
#endif
#if URP_10_2_OR_NEWER
                    m_UrpComponent,
#endif
                });
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            m_Impl.Dispose();
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            m_CinemachineComponent.Validate();

            Validate();
        }

        void Validate()
        {
#if HDRP_10_2_OR_NEWER
            m_HdrpCinemachineComponent.CinemachineVirtualCamera = CinemachineVirtualCamera;
#endif
#if URP_10_2_OR_NEWER
            m_UrpComponent.CinemachineVirtualCamera = CinemachineVirtualCamera;
#endif
        }

        protected override ICameraDriverImpl GetImplementation() => m_Impl;

        /// <inheritdoc/>
        public override Camera GetCamera()
        {
            var brain = CinemachineCore.Instance.FindPotentialTargetBrain(CinemachineVirtualCamera);
            if (brain != null)
                return brain.OutputCamera;

            return null;
        }

#else
        protected override void OnEnable()
        {
            base.OnEnable();

            Debug.LogError(
                $"A {nameof(CinemachineCameraDriver)} is used yet Cinemachine is not installed." +
                $"a {nameof(PhysicalCameraDriver)} should be used instead.");
        }

        protected override ICameraDriverImpl GetImplementation()
        {
            return null;
        }

        public override Camera GetCamera()
        {
            return null;
        }

#endif
        /// <inheritdoc/>
        public void SetDamping(Damping damping)
        {
#if VP_CINEMACHINE_2_4_0
            m_CinemachineComponent.SetDamping(damping);
#endif
        }
    }
}
