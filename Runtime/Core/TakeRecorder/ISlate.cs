using UnityObject = UnityEngine.Object;

namespace Unity.LiveCapture
{
    /// <summary>
    /// Represents a slate. A slate stores information about the shot and the take to play. The
    /// <see cref="TakeRecorder"/> uses that information to name and store the recorded take in the
    /// right directory.
    /// </summary>
    public interface ISlate
    {
        /// <summary>
        /// The UnityEngine.Object that stores the serialized data (if any).
        /// </summary>
        /// <remarks>
        /// Used internally to support undo operations.
        /// </remarks>
        UnityObject unityObject { get; }

        /// <summary>
        /// The file path containing the recorded takes.
        /// </summary>
        string directory { get; set; }

        /// <summary>
        /// The number associated with the scene to record.
        /// </summary>
        int sceneNumber { get; set; }

        /// <summary>
        /// The name of the shot stored in the slate.
        /// </summary>
        /// <remarks>
        /// The recorded takes automatically inherit from this name.
        /// </remarks>
        string shotName { get; set; }

        /// <summary>
        /// The number associated with the take to record.
        /// </summary>
        /// <remarks>
        /// The number increments after recording a take.
        /// </remarks>
        int takeNumber { get; set; }

        /// <summary>
        /// The description of the shot stored in the slate.
        /// </summary>
        string description { get; set; }

        /// <summary>
        /// The selected take of the slate.
        /// </summary>
        Take take { get; set; }

        /// <summary>
        /// The take to iterate from in the next recording.
        /// </summary>
        Take iterationBase { get; set; }

        /// <summary>
        /// The duration of the slate in seconds.
        /// </summary>
        double duration { get; }

        /// <summary>
        /// The current evaluation time of the slate in seconds.
        /// </summary>
        double time { get; set; }
    }
}
