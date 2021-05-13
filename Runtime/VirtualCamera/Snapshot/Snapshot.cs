using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityObject = UnityEngine.Object;

namespace Unity.LiveCapture.VirtualCamera
{
    [Serializable]
    class Snapshot
    {
        [SerializeField]
        Pose m_Pose;
        [SerializeField]
        LensAsset m_LensAsset;
        [SerializeField]
        Lens m_Lens;
        [SerializeField]
        CameraBody m_CameraBody;
        [SerializeField]
        Texture2D m_Screenshot;
        [SerializeField]
        PlayableAsset m_Slate;
        [SerializeField]
        FrameRate m_FrameRate;
        [SerializeField]
        double m_Time;

        public Pose Pose
        {
            get => m_Pose;
            set => m_Pose = value;
        }

        public LensAsset LensAsset
        {
            get => m_LensAsset;
            set => m_LensAsset = value;
        }

        public Lens Lens
        {
            get => m_Lens;
            set => m_Lens = value;
        }

        public CameraBody CameraBody
        {
            get => m_CameraBody;
            set => m_CameraBody = value;
        }

        public Texture2D Screenshot
        {
            get => m_Screenshot;
            set => m_Screenshot = value;
        }

        public ISlate Slate
        {
            get => m_Slate as ISlate;
            set => m_Slate = value as PlayableAsset;
        }

        public FrameRate FrameRate
        {
            get => m_FrameRate;
            set => m_FrameRate = value;
        }

        public double Time
        {
            get => m_Time;
            set => m_Time = value;
        }
    }
}
