using UnityEngine;

namespace Unity.LiveCapture.ARKitFaceCapture
{
    /// <summary>
    /// A class that customizes and manages the application of face pose data to a character rig.
    /// </summary>
    /// <remarks>
    /// To use a mapper, it must be assigned to a <see cref="FaceActor"/> component.
    /// <see cref="Unity.LiveCapture.ARKitFaceCapture.DefaultMapper.DefaultFaceMapper"/> is the default mapper implementation, designed to work for rigs
    /// that can have their bone transforms and renderer blend shapes modified directly. For complex
    /// rigs that need more advanced re-targeting of the captured face animation, inherit from this class
    /// to implement custom mapper.
    /// </remarks>
    public abstract class FaceMapper : ScriptableObject
    {
        /// <summary>
        /// Creates a mapper state cache for the given actor.
        /// </summary>
        /// <param name="actor">The face rig to create the cache for.</param>
        /// <returns>The new cache instance, or null if no cache is needed by the mapper.</returns>
        public virtual FaceMapperCache CreateCache(FaceActor actor)
        {
            return null;
        }

        /// <summary>
        /// Updates a face rig to show a face pose.
        /// </summary>
        /// <param name="actor">The face rig the pose is applied to.</param>
        /// <param name="cache">The mapper state cache for the given actor.</param>
        /// <param name="pose">The face pose to apply.</param>
        /// <param name="continuous">When true, the new pose follows the current pose and they
        /// can be smoothed between, while false corresponds to a seek in the animation where the
        /// previous pose is invalidated and should not influence the new pose.</param>
        public abstract void ApplyBlendShapesToRig(FaceActor actor, FaceMapperCache cache, ref FacePose pose, bool continuous);

        /// <summary>
        /// Updates a face rig to show a head pose.
        /// </summary>
        /// <param name="actor">The face rig the pose is applied to.</param>
        /// <param name="cache">The mapper state cache for the given actor.</param>
        /// <param name="pose">The face pose to apply.</param>
        /// <param name="continuous">When true, the new pose follows the current pose and they
        /// can be smoothed between, while false corresponds to a seek in the animation where the
        /// previous pose is invalidated and should not influence the new pose.</param>
        public abstract void ApplyHeadRotationToRig(FaceActor actor, FaceMapperCache cache, ref FacePose pose, bool continuous);

        /// <summary>
        /// Updates a face rig to show a eye pose.
        /// </summary>
        /// <param name="actor">The face rig the pose is applied to.</param>
        /// <param name="cache">The mapper state cache for the given actor.</param>
        /// <param name="pose">The face pose to apply.</param>
        /// <param name="continuous">When true, the new pose follows the current pose and they
        /// can be smoothed between, while false corresponds to a seek in the animation where the
        /// previous pose is invalidated and should not influence the new pose.</param>
        public abstract void ApplyEyeRotationToRig(FaceActor actor, FaceMapperCache cache, ref FacePose pose, bool continuous);
    }
}
