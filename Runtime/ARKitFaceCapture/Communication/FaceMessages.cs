namespace Unity.LiveCapture.ARKitFaceCapture
{
    /// <summary>
    /// A class that contains the message IDs which define the face capture protocol.
    /// </summary>
    static class FaceMessages
    {
        public static class ToServer
        {
            const string k_Base = "ARKitFaceCapture_ToServer_";

            public const string k_FacePoseSample = k_Base + "FacePoseSample";
        }
    }
}
