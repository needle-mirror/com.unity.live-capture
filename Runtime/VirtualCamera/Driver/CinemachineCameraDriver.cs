using UnityEngine;
using System;
#if VP_CINEMACHINE_2_4_0
using Cinemachine;
#endif

namespace Unity.LiveCapture.VirtualCamera
{
    [AddComponentMenu("Live Capture/Virtual Camera/Cinemachine Camera Driver")]
    class CinemachineCameraDriver : BaseCameraDriver, ICustomDamping
    {
#if VP_CINEMACHINE_2_4_0
        [SerializeField, Tooltip("Cinemachine camera driver component.")]
        CinemachineDriverComponent m_CinemachineComponent = new CinemachineDriverComponent();
#if HDRP_10_2_OR_NEWER
        [SerializeField, Tooltip("High Definition Render Pipeline camera driver component.")]
        HdrpCoreCameraDriverComponent m_HdrpCoreComponent = new HdrpCoreCameraDriverComponent();
#endif
        ICameraDriverImpl m_Impl;

        public CinemachineVirtualCamera cinemachineVirtualCamera
        {
            get => m_CinemachineComponent.cinemachineVirtualCamera;
            set => m_CinemachineComponent.cinemachineVirtualCamera = value;
        }

        protected override void OnAwake()
        {
#if HDRP_10_2_OR_NEWER
            m_HdrpCoreComponent.SetRoot(gameObject);
#endif
        }

        void OnValidate()
        {
            m_CinemachineComponent.Validate();
        }

        protected override ICameraDriverImpl GetImplementation()
        {
            if (m_Impl == null)
            {
                m_Impl = new CompositeCameraDriverImpl(new ICameraDriverComponent[]
                {
                    m_CinemachineComponent,
#if HDRP_10_2_OR_NEWER
                    m_HdrpCoreComponent,
#endif
                });
            }

            return m_Impl;
        }

        /// <inheritdoc/>
        public override Camera GetCamera()
        {
            var brain = CinemachineCore.Instance.FindPotentialTargetBrain(cinemachineVirtualCamera);
            if (brain != null)
                return brain.OutputCamera;

            return null;
        }

#else
        protected override void OnAwake()
        {
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
