#if HDRP_10_2_OR_NEWER
using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.HighDefinition;

namespace Unity.LiveCapture.VirtualCamera
{
    [Serializable]
    class HdrpNoCinemachineCameraDriverComponent : ICameraDriverComponent
    {
        Camera m_Camera;
        HDAdditionalCameraData m_HDCameraData;

        /// <summary>
        /// Camera to be driven by this component.
        /// </summary>
        public Camera Camera
        {
            set
            {
                m_Camera = value;
                m_HDCameraData = m_Camera.GetComponent<HDAdditionalCameraData>();
                Assert.IsNotNull(m_HDCameraData);
            }
        }

        /// <inheritdoc/>
        public bool SetDamping(Damping dampingData) { return false; }

        /// <inheritdoc/>
        public bool EnableDepthOfField(bool value) { return false; }

        /// <inheritdoc/>
        public bool SetFocusDistance(float value) { return false; }

        /// <inheritdoc/>
        public bool SetPhysicalCameraProperties(Lens lens, LensIntrinsics intrinsics, CameraBody cameraBody)
        {
            CompositeCameraDriverImpl.UpdateCamera(m_Camera, lens, intrinsics, cameraBody);

            m_HDCameraData.physicalParameters.aperture = lens.Aperture;
            m_HDCameraData.physicalParameters.iso = cameraBody.Iso;
            m_HDCameraData.physicalParameters.shutterSpeed = cameraBody.ShutterSpeed;
            m_HDCameraData.physicalParameters.anamorphism = intrinsics.Anamorphism;
            m_HDCameraData.physicalParameters.curvature = intrinsics.Curvature;
            m_HDCameraData.physicalParameters.barrelClipping = intrinsics.BarrelClipping;
            m_HDCameraData.physicalParameters.bladeCount = intrinsics.BladeCount;

            return true;
        }
    }
}
#endif
