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

            public const string ChannelFlags = k_Base + "ChannelFlags";
            public const string CameraLens = k_Base + "CameraLens";
            public const string CameraBody = k_Base + "CameraBody";
            public const string Settings = k_Base + "Settings";
            public const string VideoStreamState = k_Base + "VideoStreamState";
            public const string LensKitDescriptor = k_Base + "LensKitDescriptor";
            public const string SnapshotListDescriptor = k_Base + "SnapshotListDescriptor";
        }

        public static class ToServer
        {
            const string k_Base = "VirtualCamera_ToServer_";

            public const string ChannelFlags = k_Base + "ChannelFlags";
            public const string PoseSample = k_Base + "PoseSample";
            public const string JoysticksSample = k_Base + "JoysticksSample";
            public const string FocalLengthSample = k_Base + "FocalLengthSample";
            public const string FocusDistanceSample = k_Base + "FocusDistanceSample";
            public const string ApertureSample = k_Base + "ApertureSample";
            public const string SetSettings = k_Base + "SetSettings";
            public const string SetReticlePosition = k_Base + "SetReticlePosition";
            public const string SetPoseToOrigin = k_Base + "SetPoseToOrigin";
            public const string SetLensAsset = k_Base + "SetLensAsset";
            public const string TakeSnapshot = k_Base + "TakeSnapshot";
            public const string GoToSnapshot = k_Base + "GoToSnapshot";
            public const string LoadSnapshot = k_Base + "LoadSnapshot";
            public const string DeleteSnapshot = k_Base + "DeleteSnapshot";
        }
    }
}
