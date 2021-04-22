using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    class VirtualCameraTrackMetadata : ITrackMetadata
    {
        [SerializeField, EnumFlagButtonGroup(100f)]
        VirtualCameraChannelFlags m_Channels;
        [SerializeField]
        Lens m_Lens = Lens.defaultParams;
        [SerializeField]
        CameraBody m_CameraBody = CameraBody.defaultParams;

        public VirtualCameraChannelFlags channels
        {
            get => m_Channels;
            set => m_Channels = value;
        }

        public Lens lens
        {
            get => m_Lens;
            set => m_Lens = value;
        }

        public CameraBody cameraBody
        {
            get => m_CameraBody;
            set => m_CameraBody = value;
        }
    }
}
