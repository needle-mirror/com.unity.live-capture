using System;
using UnityEngine;

namespace Unity.LiveCapture.CompanionApp
{
    /// <summary>
    /// Class that stores information of a take. The client uses this information to build its UI.
    /// </summary>
    [Serializable]
    class TakeDescriptor
    {
        [SerializeField]
        SerializableGuid m_Guid;
        [SerializeField]
        string m_Name;
        [SerializeField]
        int m_SceneNumber;
        [SerializeField]
        string m_ShotName;
        [SerializeField]
        int m_TakeNumber;
        [SerializeField]
        string m_Description;
        [SerializeField]
        int m_Rating;
        [SerializeField]
        FrameRate m_FrameRate;
        [SerializeField]
        SerializableGuid m_Screenshot;

        /// <summary>
        /// The globally unique identifier of the take asset.
        /// </summary>
        public Guid Guid
        {
            get => m_Guid;
            set => m_Guid = value;
        }

        /// <summary>
        /// The name of the take.
        /// </summary>
        public string Name
        {
            get => m_Name;
            set => m_Name = value;
        }

        /// <summary>
        /// The number associated with the scene where the take was captured.
        /// </summary>
        public int SceneNumber
        {
            get => m_SceneNumber;
            set => m_SceneNumber = value;
        }

        /// <summary>
        /// The name of the shot where the take was captured.
        /// </summary>
        public string ShotName
        {
            get => m_ShotName;
            set => m_ShotName = value;
        }

        /// <summary>
        /// The number associated with the take.
        /// </summary>
        public int TakeNumber
        {
            get => m_TakeNumber;
            set => m_TakeNumber = value;
        }

        /// <summary>
        /// The description of the shot where the take was captured.
        /// </summary>
        public string Description
        {
            get => m_Description;
            set => m_Description = value;
        }

        /// <summary>
        /// The rating of the take.
        /// </summary>
        public int Rating
        {
            get => m_Rating;
            set => m_Rating = value;
        }

        /// <summary>
        /// The frame rate used during the recording.
        /// </summary>
        public FrameRate FrameRate
        {
            get => m_FrameRate;
            set => m_FrameRate = value;
        }

        /// <summary>
        /// The Guid of the screenshot of the take.
        /// </summary>
        public Guid Screenshot
        {
            get => m_Screenshot;
            set => m_Screenshot = value;
        }

        internal static TakeDescriptor Create(Take take)
        {
            var descriptor = new TakeDescriptor();
#if UNITY_EDITOR
            descriptor.Guid = SerializableGuid.FromString(AssetDatabaseUtility.GetAssetGUID(take));
            descriptor.Name = take.name;
            descriptor.SceneNumber = take.SceneNumber;
            descriptor.ShotName = take.ShotName;
            descriptor.TakeNumber = take.TakeNumber;
            descriptor.Description = take.Description;
            descriptor.Rating = take.Rating;
            descriptor.FrameRate = take.FrameRate;

            if (take.Screenshot != null)
            {
                descriptor.Screenshot = SerializableGuid.FromString(AssetDatabaseUtility.GetAssetGUID(take.Screenshot));
            }
#endif
            return descriptor;
        }
    }
}
