using UnityObject = UnityEngine.Object;

namespace Unity.LiveCapture
{
    /// <summary>
    /// Represents a shot. A shot stores information about the take to play, the slate used, and the
    /// output directory. The <see cref="TakeRecorder"/> uses that information to name and store the
    /// recorded takes in the correct directory.
    /// </summary>
    public interface IShot
    {
        /// <summary>
        /// The UnityEngine.Object that stores the serialized data (if any).
        /// </summary>
        /// <remarks>
        /// Used internally to support undo operations.
        /// </remarks>
        UnityObject UnityObject { get; }

        /// <summary>
        /// The file path containing the recorded takes.
        /// </summary>
        string Directory { get; set; }

        /// <summary>
        /// The slate associated with the shot to record.
        /// </summary>
        Slate Slate { get; set; }

        /// <summary>
        /// The selected take of the slate.
        /// </summary>
        Take Take { get; set; }

        /// <summary>
        /// The take to iterate from in the next recording.
        /// </summary>
        Take IterationBase { get; set; }
    }
}