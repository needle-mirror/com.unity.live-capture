using UnityEngine;
using UnityEngine.Playables;

namespace Unity.LiveCapture
{
    /// <summary>
    /// Live link is used by a device to set the properties of an actor.
    /// </summary>
    /// <remarks>
    /// The playable API is used to create a playable that uses an animation job to sets an actor's properties,
    /// which it injects as an output on a playable graph. This allows using a timeline to drive some or all of the
    /// actor's properties without needing to modify the timeline asset.
    /// </remarks>
    public interface ILiveLink
    {
        /// <summary>
        /// Gets the animator the live link outputs to.
        /// </summary>
        /// <returns>
        /// The animator component.
        /// </returns>
        Animator GetAnimator();

        /// <summary>
        /// Sets the animator the live link outputs to.
        /// </summary>
        /// <param name="animator">The animator to use, or null to clear the assigned animator.</param>
        void SetAnimator(Animator animator);

        /// <summary>
        /// Checks if the playables driving the live link are valid.
        /// </summary>
        /// <returns>
        /// true if valid; otherwise, false.
        /// </returns>
        bool IsValid();

        /// <summary>
        /// Gets if the live link is configured to output to the actor's properties.
        /// </summary>
        /// <returns>True if output is enabled; false otherwise.</returns>
        bool IsActive();

        /// <summary>
        /// Sets if the live link outputs to the actor's properties.
        /// </summary>
        /// <param name="value">True to enable output; false to disable output.</param>
        void SetActive(bool value);

        /// <summary>
        /// Updates the live link output.
        /// </summary>
        void Update();

        /// <summary>
        /// Rebuilds the live link using the specified PlayableGraph.
        /// </summary>
        /// <param name="graph">The PlayableGraph to drive the live link from.</param>
        void Build(PlayableGraph graph);
    }
}
