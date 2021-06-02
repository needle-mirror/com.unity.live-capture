using System;
using UnityEngine;

namespace Unity.LiveCapture.CompanionApp.Networking
{
    [Serializable]
    class TakeDescriptorV0
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

        public static explicit operator TakeDescriptorV0(TakeDescriptor take)
        {
            return new TakeDescriptorV0
            {
                m_Guid = take.Guid,
                m_Name = take.Name,
                m_SceneNumber = take.SceneNumber,
                m_ShotName = take.ShotName,
                m_TakeNumber = take.TakeNumber,
                m_Description = take.Description,
                m_Rating = take.Rating,
                m_FrameRate = take.FrameRate,
            };
        }

        public static explicit operator TakeDescriptor(TakeDescriptorV0 take)
        {
            return new TakeDescriptor
            {
                Guid = take.m_Guid,
                Name = take.m_Name,
                SceneNumber = take.m_SceneNumber,
                ShotName = take.m_ShotName,
                TakeNumber = take.m_TakeNumber,
                Description = take.m_Description,
                Rating = take.m_Rating,
                FrameRate = take.m_FrameRate,
            };
        }
    }
}
