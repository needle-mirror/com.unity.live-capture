using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    [Serializable]
    class VcamTrackMetadataListDescriptor
    {
        static readonly List<VcamTrackMetadataDescriptor> m_TmpDescriptors = new List<VcamTrackMetadataDescriptor>();

        [SerializeField]
        List<VcamTrackMetadataDescriptor> m_Descriptors = new List<VcamTrackMetadataDescriptor>();

        public VcamTrackMetadataDescriptor[] Descriptors
        {
            get => m_Descriptors.ToArray();
        }

        public VcamTrackMetadataListDescriptor(IEnumerable<VcamTrackMetadataDescriptor> descriptors)
        {
            m_Descriptors.AddRange(descriptors);
        }

        public static VcamTrackMetadataListDescriptor Create(ISlate slate)
        {
            m_TmpDescriptors.Clear();

#if UNITY_EDITOR
            if (slate != null)
            {
                var takes = AssetDatabaseUtility.GetAssetsAtPath<Take>(slate.Directory);

                foreach (var take in takes)
                {
                    foreach (var entry in take.MetadataEntries)
                    {
                        if (entry.Metadata is VirtualCameraTrackMetadata data)
                        {
                            m_TmpDescriptors.Add(VcamTrackMetadataDescriptor.Create(take, data));
                        }
                    }
                }
            }
#endif
            return new VcamTrackMetadataListDescriptor(m_TmpDescriptors);
        }
    }
}
