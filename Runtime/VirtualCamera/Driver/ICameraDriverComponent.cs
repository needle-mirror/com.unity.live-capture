namespace Unity.LiveCapture.VirtualCamera
{
    interface ICameraDriverComponent
    {
        /// <summary>
        /// Control Depth Of Field activation.
        /// </summary>
        /// <returns>
        /// True if the component actually supports the operation, false otherwise.
        /// </returns>
        bool EnableDepthOfField(bool value);

        /// <summary>
        /// Set damping data.
        /// </summary>
        /// <returns>
        /// True if the component actually supports the operation, false otherwise.
        /// </returns>
        bool SetDamping(Damping dampingData);

        /// <summary>
        /// Set Focus distance.
        /// </summary>
        /// <returns>
        /// True if the component actually supports the operation, false otherwise.
        /// </returns>
        bool SetFocusDistance(float focusDistance);

        /// <summary>
        /// Set Camera physical properties.
        /// </summary>
        /// <returns>
        /// True if the component actually supports the operation, false otherwise.
        /// </returns>
        bool SetPhysicalCameraProperties(Lens lens, LensIntrinsics intrinsics, CameraBody cameraBody);
    }
}
