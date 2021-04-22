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
        Transform m_Root;
        Camera m_Camera;
        HDAdditionalCameraData m_HDCameraData;

        /// <summary>
        /// Camera to be driven by this component.
        /// </summary>
        public Camera camera
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
        public bool SetPhysicalCameraProperties(Lens lens, CameraBody cameraBody)
        {
            CompositeCameraDriverImpl.UpdateCamera(m_Camera, lens, cameraBody);

            m_HDCameraData.physicalParameters.aperture = lens.aperture;
            m_HDCameraData.physicalParameters.iso = cameraBody.iso;
            m_HDCameraData.physicalParameters.shutterSpeed = cameraBody.shutterSpeed;
            m_HDCameraData.physicalParameters.anamorphism = lens.anamorphism;
            m_HDCameraData.physicalParameters.curvature = lens.curvature;
            m_HDCameraData.physicalParameters.barrelClipping = lens.barrelClipping;
            m_HDCameraData.physicalParameters.bladeCount = lens.bladeCount;

            return true;
        }
    }
}
#endif
