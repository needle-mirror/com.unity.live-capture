#if VP_CINEMACHINE_2_4_0
using System;
using Cinemachine;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    [Serializable]
    class CinemachineDriverComponent : ICameraDriverComponent
    {
        [SerializeField, Tooltip("The virtual camera to be driven.")]
        CinemachineVirtualCamera m_CinemachineVirtualCamera;
        CinemachineTransposer m_Transposer;
        CinemachineSameAsFollowTarget m_Aim;

        /// <summary>
        /// The Cinemachine virtual camera driven by this component.
        /// </summary>
        public CinemachineVirtualCamera cinemachineVirtualCamera
        {
            get { return m_CinemachineVirtualCamera;}
            set
            {
                m_CinemachineVirtualCamera = value;
                Validate();
            }
        }

        /// <summary>
        /// Tries and fetch required Cinemachine components from the virtual camera.
        /// </summary>
        public void Validate()
        {
            if (m_CinemachineVirtualCamera == null)
                return;

            var transposer = m_CinemachineVirtualCamera.GetCinemachineComponent<CinemachineTransposer>();

            if (transposer != null)
            {
                // Transposer needs reset only if it has changed.
                if (transposer != m_Transposer)
                {
                    transposer.m_BindingMode = CinemachineTransposer.BindingMode.LockToTarget;
                    transposer.m_FollowOffset = Vector3.zero;
                    transposer.m_XDamping = 0f;
                    transposer.m_YDamping = 0f;
                    transposer.m_ZDamping = 0f;
                }
                m_Transposer = transposer;
            }
            else
            {
                Debug.LogError($"{nameof(m_CinemachineVirtualCamera)} is expected to hold a {nameof(CinemachineTransposer)} component.");
            }

            m_Aim = m_CinemachineVirtualCamera.GetCinemachineComponent<CinemachineSameAsFollowTarget>();

            if (m_Aim == null)
            {
                Debug.LogError($"{nameof(m_CinemachineVirtualCamera)} is expected to hold a {nameof(CinemachineSameAsFollowTarget)} component.");
            }

            if (m_Transposer == null || m_Aim == null)
            {
                m_CinemachineVirtualCamera = null;
            }
        }

        /// <inheritdoc/>
        public bool SetDamping(Damping damping)
        {
            // In case the component was not assigned a virtual camera yet.
            if (m_Transposer == null || m_Aim == null)
                return false;

            var targetDamping = damping;

            if (!damping.enabled)
            {
                targetDamping.body = Vector3.zero;
                targetDamping.aim = 0;
            }

            m_Transposer.m_XDamping = targetDamping.body.x;
            m_Transposer.m_YDamping = targetDamping.body.y;
            m_Transposer.m_ZDamping = targetDamping.body.z;
            m_Aim.m_Damping = targetDamping.aim;

            return true;
        }

        /// <inheritdoc/>
        public bool SetFocusDistance(float value) { return false; }

        /// <inheritdoc/>
        public bool SetPhysicalCameraProperties(Lens lens, CameraBody cameraBody)
        {
            // In case the component was not assigned a virtual camera yet.
            if (m_CinemachineVirtualCamera == null)
                return false;

            // The Cinemachine brain's Camera must use physical properties
            // for sensor size to be pulled from the brain's output Camera's sensorSize property.
            // lens.SensorSize will be overwritten by Cinemachine so no point in assigning it here.
            // See `LensSettings.SnapshotCameraReadOnlyProperties`
            var brain = CinemachineCore.Instance.FindPotentialTargetBrain(m_CinemachineVirtualCamera);
            if (brain != null)
            {
                brain.OutputCamera.sensorSize = cameraBody.sensorSize;
                brain.OutputCamera.usePhysicalProperties = true;
            }

            m_CinemachineVirtualCamera.m_Lens.FieldOfView = Camera.FocalLengthToFieldOfView(lens.focalLength, cameraBody.sensorSize.y);

#if HDRP_10_2_OR_NEWER
            m_CinemachineVirtualCamera.m_Lens.Anamorphism = lens.anamorphism;
            m_CinemachineVirtualCamera.m_Lens.BarrelClipping = lens.barrelClipping;
            m_CinemachineVirtualCamera.m_Lens.Curvature = lens.curvature;
            m_CinemachineVirtualCamera.m_Lens.BladeCount = lens.bladeCount;
            m_CinemachineVirtualCamera.m_Lens.Iso = cameraBody.iso;
            m_CinemachineVirtualCamera.m_Lens.ShutterSpeed = cameraBody.shutterSpeed;
            m_CinemachineVirtualCamera.m_Lens.Aperture = lens.aperture;
#endif
            return true;
        }

        /// <inheritdoc/>
        public bool EnableDepthOfField(bool value) { return false; }
    }
}
#endif
