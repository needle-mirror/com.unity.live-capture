using System;

namespace Unity.LiveCapture.CompanionApp
{
    /// <summary>
    /// Class that stores information of a take. The client uses this information to build its UI.
    /// </summary>
    [Serializable]
    class TakeDescriptor
    {
        /// <summary>
        /// The globally unique identifier of the take asset.
        /// </summary>
        public SerializableGuid Guid;

        /// <summary>
        /// The name of the take.
        /// </summary>
        public string Name;

        /// <summary>
        /// The number associated with the scene where the take was captured.
        /// </summary>
        public int SceneNumber;

        /// <summary>
        /// The name of the shot where the take was captured.
        /// </summary>
        public string ShotName;

        /// <summary>
        /// The number associated with the take.
        /// </summary>
        public int TakeNumber;

        /// <summary>
        /// The description of the shot where the take was captured.
        /// </summary>
        public string Description;

        /// <summary>
        /// The rating of the take.
        /// </summary>
        public int Rating;

        /// <summary>
        /// The frame rate used during the recording.
        /// </summary>
        public FrameRate FrameRate;

        /// <summary>
        /// The Guid of the screenshot of the take.
        /// </summary>
        public SerializableGuid Screenshot;

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
