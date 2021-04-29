using System;
using System.Linq;

namespace Unity.LiveCapture.CompanionApp
{
    /// <summary>
    /// Class that stores information of a slate. The client uses this information to build its UI.
    /// </summary>
    [Serializable]
    class SlateDescriptor
    {
        /// <summary>
        /// The number associated with the scene to record.
        /// </summary>
        public int SceneNumber;

        /// <summary>
        /// The name of the shot stored in the slate.
        /// </summary>
        public string ShotName = string.Empty;

        /// <summary>
        /// The number associated with the take to record.
        /// </summary>
        public int TakeNumber;

        /// <summary>
        /// The description of the shot stored in the slate.
        /// </summary>
        public string Description = string.Empty;

        /// <summary>
        /// The duration of the slate in seconds.
        /// </summary>
        public double Duration;

        /// <summary>
        /// The index of the selected take in the take list.
        /// </summary>
        public int SelectedTake = -1;

        /// <summary>
        /// The index of the iteration base in the take list.
        /// </summary>
        public int IterationBase = -1;

        /// <summary>
        /// The list of available takes in the slate.
        /// </summary>
        public TakeDescriptor[] Takes;

        internal static SlateDescriptor Create(ISlate slate)
        {
            var descriptor = new SlateDescriptor();
#if UNITY_EDITOR
            if (slate != null)
            {
                var takes = AssetDatabaseUtility.GetAssetsAtPath<Take>(slate.Directory);
                descriptor.SceneNumber = slate.SceneNumber;
                descriptor.ShotName = slate.ShotName;
                descriptor.TakeNumber = slate.TakeNumber;
                descriptor.Description = slate.Description;
                descriptor.SelectedTake = takes.IndexOf(slate.Take);
                descriptor.IterationBase = takes.IndexOf(slate.IterationBase);
                descriptor.Takes = takes.Select(take => TakeDescriptor.Create(take)).ToArray();
            }
#endif
            return descriptor;
        }
    }
}
