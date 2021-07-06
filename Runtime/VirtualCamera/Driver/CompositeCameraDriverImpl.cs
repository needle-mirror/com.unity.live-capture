using System.Collections.Generic;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// The driver must support multiple render pipelines, and optionally, Cinemachine.
    /// We keep the induced complexity in check by having each pipeline/package's features
    /// living in independent components, who are then added/used depending on the project's configuration.
    /// </summary>
    class CompositeCameraDriverImpl : ICameraDriverImpl
    {
        IEnumerable<ICameraDriverComponent> m_Components;

        public CompositeCameraDriverImpl(IEnumerable<ICameraDriverComponent> components)
        {
            m_Components = components;
        }

        public void EnableDepthOfField(bool value)
        {
            foreach (var component in m_Components)
                if (component.EnableDepthOfField(value))
                    return;
        }

        public void SetFocusDistance(float focusDistance)
        {
            foreach (var component in m_Components)
                if (component.SetFocusDistance(focusDistance))
                    break;
        }

        public void SetPhysicalCameraProperties(Lens lens, LensIntrinsics intrinsics, CameraBody cameraBody)
        {
            foreach (var component in m_Components)
                if (component.SetPhysicalCameraProperties(lens, intrinsics, cameraBody))
                    break;
        }

        /// <summary>
        /// A utility to update Camera properties based on lens and camera body data.
        /// </summary>
        internal static void UpdateCamera(Camera camera, Lens lens, LensIntrinsics intrinsics, CameraBody cameraBody)
        {
            camera.sensorSize = cameraBody.SensorSize;
            camera.lensShift = intrinsics.LensShift;
            camera.focalLength = lens.FocalLength;
        }
    }
}
