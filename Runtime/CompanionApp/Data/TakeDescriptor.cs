using System;

namespace Unity.LiveCapture.CompanionApp
{
    /// <summary>
    /// Class that stores information of a take. The client uses this information to build its UI.
    /// </summary>
    [Serializable]
    public class TakeDescriptor
    {
        /// <summary>
        /// The globally unique identifier of the take asset.
        /// </summary>
        public SerializableGuid guid;

        /// <summary>
        /// The name of the take.
        /// </summary>
        public string name;
    }
}
