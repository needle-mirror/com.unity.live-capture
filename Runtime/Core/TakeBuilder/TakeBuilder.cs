using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.Timeline;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityObject = UnityEngine.Object;

namespace Unity.LiveCapture
{
    class TakeBuilder : ITakeBuilder, IDisposable
    {
        internal class Wildcards
        {
            public const string kScene = "<Scene>";
            public const string kName = "<Name>";
            public const string kShot = "<Shot>";
            public const string kTake = "<Take>";
            public const string kTimecode = "<Timecode>";
        }

        WildcardFormatter m_WildcardFormatter;
        IExposedPropertyTable m_Resolver;
        string m_ContentsDirectory;
        string m_AssetNameFormat;
        Take m_Take;

        public Take take => m_Take;

        /// <summary>
        /// The directory that stores the take content data, like the recorded clips.
        /// </summary>
        public string contentsDirectory => m_ContentsDirectory;

        public TakeBuilder(
            string takeNameFormat,
            string assetNameFormat,
            int sceneNumber,
            string shotName,
            int takeNumber,
            string description,
            string directory,
            Take iterationBase,
            FrameRate frameRate,
            IExposedPropertyTable resolver)
        {
#if UNITY_EDITOR
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            shotName = FileNameFormatter.instance.Format(shotName);

            if (string.IsNullOrEmpty(shotName))
            {
                throw new InvalidDataException("Shot name is invalid or empty");
            }

            m_AssetNameFormat = assetNameFormat;
            m_Resolver = resolver;

            var sceneNumberStr = sceneNumber.ToString("D3");
            var takeNumberStr = takeNumber.ToString("D3");

            m_WildcardFormatter = new WildcardFormatter();
            m_WildcardFormatter.AddReplacement(Wildcards.kScene, sceneNumberStr);
            m_WildcardFormatter.AddReplacement(Wildcards.kShot, shotName);
            m_WildcardFormatter.AddReplacement(Wildcards.kTake, takeNumberStr);

            var assetName = m_WildcardFormatter.Format(takeNameFormat);
            assetName = FileNameFormatter.instance.Format(assetName);

            if (string.IsNullOrEmpty(assetName))
            {
                assetName = $"[{sceneNumberStr}] {shotName} [{takeNumberStr}]";
            }

            var assetPath = $"{directory}/{assetName}.asset";
            var exists = AssetDatabase.LoadMainAssetAtPath(assetPath) != null;

            if (exists)
            {
                assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
                shotName = Path.GetFileNameWithoutExtension(assetPath);
            }

            m_ContentsDirectory = Path.GetDirectoryName(assetPath) + "/" + shotName;

            Directory.CreateDirectory(m_ContentsDirectory);
            AssetDatabase.Refresh();

            if (iterationBase == null)
            {
                var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                m_Take = ScriptableObject.CreateInstance<Take>();

                AssetDatabase.CreateAsset(m_Take, assetPath);
                AssetDatabase.AddObjectToAsset(timeline, m_Take);
                AssetDatabase.SetMainObject(m_Take, assetPath);

                m_Take.timeline = timeline;
            }
            else
            {
                var sourceAssetPath = AssetDatabase.GetAssetPath(iterationBase);
                AssetDatabase.CopyAsset(sourceAssetPath, assetPath);
                m_Take = AssetDatabase.LoadAssetAtPath<Take>(assetPath);
            }

            m_Take.sceneNumber = sceneNumber;
            m_Take.shotName = shotName;
            m_Take.takeNumber = takeNumber;
            m_Take.description = description;
            m_Take.frameRate = frameRate;
            m_Take.timeline.name = m_Take.name;
#endif
        }

        public void Dispose()
        {
#if UNITY_EDITOR
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            m_Take = null;
            m_Resolver = null;
#endif
        }

        /// <inheritdoc/>
        public void CreateAnimationTrack(string name, Animator animator, AnimationClip animationClip)
        {
            CreateAnimationTrackInternal(name, animator, animationClip);
        }

        /// <inheritdoc/>
        public void CreateAnimationTrack(string name, Animator animator, AnimationClip animationClip, ITrackMetadata metadata)
        {
            var track = CreateAnimationTrackInternal(name, animator, animationClip);

            AddMetadata(track, metadata);
        }

        AnimationTrack CreateAnimationTrackInternal(string name, Animator animator, AnimationClip animationClip)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Track name is null or empty.", nameof(name));
            }

            if (animator == null)
            {
                throw new ArgumentNullException(nameof(animator));
            }

            if (animationClip == null)
            {
                throw new ArgumentNullException(nameof(animationClip));
            }

            var assetName = animator.gameObject.name;

            SaveAsAsset(animationClip, assetName);

            var binding = CreateBinding<AnimatorTakeBinding>(assetName, animator);
            var parent = GetTracks<AnimationTrack>(binding).FirstOrDefault();
            var track = CreateTrackWithParent<AnimationTrack>(name, parent);

            // Track overrides don't need redundant bindings.
            if (parent == null)
            {
                m_Take.AddTrackBinding(track, binding);
            }

            var clip = track.CreateClip(animationClip);
            var playableAsset = clip.asset as AnimationPlayableAsset;

            playableAsset.removeStartOffset = false;

            if (parent != null)
            {
                var partentClip = parent.GetClips().FirstOrDefault();

                if (partentClip != null)
                {
                    var duration = partentClip.duration;

                    clip.duration = duration;
                }
            }

            return track;
        }

        void AddMetadata(TrackAsset track, ITrackMetadata metadata)
        {
            m_Take.AddTrackMetadata(track, metadata);
        }

        /// <summary>
        /// Creates a new <see cref="ITakeBinding"/>.
        /// </summary>
        /// <param name="name">The binding name.</param>
        /// <param name="value">The binding object.</param>
        public T CreateBinding<T>(string name, UnityObject value) where T : ITakeBinding, new()
        {
            CheckDisposed();

            var binding = new T();

            binding.SetName(name);
            binding.SetValue(value, m_Resolver);

            return binding;
        }

        /// <summary>
        /// Creates a new track without binding.
        /// </summary>
        /// <param name="name">The name of the track.</param>
        /// <param name="parent">The track to set as parent.</param>
        public T CreateTrack<T>(string name) where T : TrackAsset, new()
        {
            return CreateTrackWithParent<T>(name, null);
        }

        /// <summary>
        /// Creates a new track with binding.
        /// </summary>
        /// <param name="name">The name of the track.</param>
        /// <param name="binding">The binding to use for the new track.</param>
        /// <param name="parent">The track to set as parent.</param>
        public T CreateTrack<T>(string name, ITakeBinding binding) where T : TrackAsset, new()
        {
            var track = CreateTrack<T>(name);

            m_Take.AddTrackBinding(track, binding);

            return track;
        }

        /// <summary>
        /// Gets all the available tracks.
        /// </summary>
        /// <remarks>
        /// When recording take iterations, the capture device might overwrite existing tracks
        /// instead of creating new ones.
        /// </remarks>
        /// <returns>
        /// An enumerable of the tracks.
        /// </returns>
        public IEnumerable<T> GetTracks<T>() where T : TrackAsset, new()
        {
            return m_Take.timeline.GetRootTracks()
                .Where(t => t is T)
                .Select(t => t as T);
        }

        /// <summary>
        /// Gets all the available tracks that use the provided <see cref="ITakeBinding"/>.
        /// </summary>
        /// <remarks>
        /// When recording take iterations, the capture device might overwrite existing tracks
        /// instead of creating new ones.
        /// </remarks>
        /// <param name="binding">The binding to query from.</param>
        /// <returns>
        /// An enumerable of the tracks.
        /// </returns>
        public IEnumerable<T> GetTracks<T>(ITakeBinding binding) where T : TrackAsset, new()
        {
            return m_Take.bindingEntries
                .Where(e => e.track is T && e.binding.Equals(binding))
                .Select(e => e.track as T);
        }

        /// <summary>
        /// Stores an object into an asset inside the contents directory.
        /// </summary>
        /// <remarks>
        /// Devices might use this method to store the artifacts of the recording, like a clip.
        /// </remarks>
        /// <param name="obj">The object to store.</param>
        /// <param name="name">The name to use in the asset.</param>
        public void SaveAsAsset<T>(T obj, string name) where T : UnityObject
        {
#if UNITY_EDITOR
            var extension = string.Empty;

            if (string.IsNullOrEmpty(name))
            {
                throw new Exception("Can't save as asset. Provided object has no name.");
            }

            if (typeof(T) == typeof(AnimationClip))
            {
                extension = "anim";
            }
            else if (typeof(ScriptableObject).IsAssignableFrom(typeof(T)))
            {
                extension = "asset";
            }
            else
            {
                throw new Exception($"Can't save assets of type {typeof(T)}");
            }

            m_WildcardFormatter.AddReplacement(Wildcards.kName, name);

            var assetName = m_WildcardFormatter.Format(m_AssetNameFormat);
            assetName = FileNameFormatter.instance.Format(assetName);

            if (string.IsNullOrEmpty(assetName))
            {
                assetName = name;
            }

            var path = $"{m_ContentsDirectory}/{assetName}.{extension}";

            path = AssetDatabase.GenerateUniqueAssetPath(path);

            AssetDatabase.CreateAsset(obj, path);
#endif
        }

        void CheckDisposed()
        {
            if (m_Take == null)
            {
                throw new ObjectDisposedException($"{nameof(Take)} is invalid or has been disposed.");
            }

            if (m_Resolver == null)
            {
                throw new ObjectDisposedException($"{nameof(IExposedPropertyTable)} is invalid or has been disposed.");
            }
        }

        T CreateTrackWithParent<T>(string name, TrackAsset parent) where T : TrackAsset, new()
        {
            CheckDisposed();

            var timeline = m_Take.timeline;
            var track = timeline.CreateTrack<T>(parent, name);

            track.locked = true;

            return track;
        }
    }
}
