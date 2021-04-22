using System;
using System.Linq;

namespace Unity.LiveCapture.CompanionApp
{
    /// <summary>
    /// Class that stores information of a slate. The client uses this information to build its UI.
    /// </summary>
    [Serializable]
    public class SlateDescriptor
    {
        /// <summary>
        /// The index of the selected take in the take list.
        /// </summary>
        public int selectedTake = -1;

        /// <summary>
        /// The list of available takes in the slate.
        /// </summary>
        public TakeDescriptor[] takes;

        internal static SlateDescriptor Create(ISlate slate)
        {
            var descriptor = new SlateDescriptor();
#if UNITY_EDITOR
            var takes = AssetDatabaseUtility.GetAssetsAtPath<Take>(slate.directory);
            descriptor.selectedTake = takes.IndexOf(slate.take);
            descriptor.takes = takes.Select(take => new TakeDescriptor()
            {
                guid = SerializableGuid.FromString(AssetDatabaseUtility.GetAssetGUID(take)),
                name = take.name
            }).ToArray();
#endif
            return descriptor;
        }
    }
}
