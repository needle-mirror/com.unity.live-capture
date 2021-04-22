namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// A class that contains the message IDs which define the virtual camera protocol.
    /// </summary>
    static class VirtualCameraMessages
    {
        public static class ToClient
        {
            const string k_Base = "VirtualCamera_ToClient_";

            public const string k_CameraLens = k_Base + "CameraLens";
            public const string k_CameraBody = k_Base + "CameraBody";
            public const string k_CameraState = k_Base + "CameraState";
            public const string k_VideoStreamState = k_Base + "VideoStreamState";
        }

        public static class ToServer
        {
            const string k_Base = "VirtualCamera_ToServer_";

            public const string k_PoseSample = k_Base + "PoseSample";
            public const string k_FocalLengthSample = k_Base + "FocalLengthSample";
            public const string k_FocusDistanceSample = k_Base + "FocusDistanceSample";
            public const string k_ApertureSample = k_Base + "ApertureSample";
            public const string k_SetCameraState = k_Base + "SetCameraState";
            public const string k_SetReticlePosition = k_Base + "SetReticlePosition";
            public const string k_SetPoseToOrigin = k_Base + "SetPoseToOrigin";
        }
    }
}
