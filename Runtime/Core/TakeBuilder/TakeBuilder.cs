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
        internal static class Wildcards
        {
            public const string Scene = "<Scene>";
            public const string Name = "<Name>";
            public const string Shot = "<Shot>";
            public const string Take = "<Take>";
            public const string Timecode = "<Timecode>";
        }

        static string TakeNameFormat => LiveCaptureSettings.Instance.TakeNameFormat;
        static string AssetNameFormat => LiveCaptureSettings.Instance.AssetNameFormat;

        WildcardFormatter m_WildcardFormatter;
        IExposedPropertyTable m_Resolver;
        string m_ContentsDirectory;
        Take m_Take;

        public Take Take => m_Take;

        /// <summary>
        /// The directory that stores the take content data, like the recorded clips.
        /// </summary>
        public string ContentsDirectory => m_ContentsDirectory;

        internal static string GetAssetName(
            int sceneNumber,
            string shotName,
            int takeNumber)
        {
            return GetAssetName(
                new WildcardFormatter(),
                sceneNumber,
                shotName,
                takeNumber);
        }

        static string GetAssetName(
            WildcardFormatter formatter,
            int sceneNumber,
            string shotName,
            int takeNumber)
        {
            var sceneNumberStr = sceneNumber.ToString("D3");
            var takeNumberStr = takeNumber.ToString("D3");

            formatter.AddReplacement(Wildcards.Scene, sceneNumberStr);
            formatter.AddReplacement(Wildcards.Shot, shotName);
            formatter.AddReplacement(Wildcards.Take, takeNumberStr);

            var assetName = formatter.Format(TakeNameFormat);
            assetName = FileNameFormatter.Instance.Format(assetName);

            if (string.IsNullOrEmpty(assetName))
            {
                assetName = $"[{sceneNumberStr}] {shotName} [{takeNumberStr}]";
            }

            return assetName;
        }

        public TakeBuilder(
            double duration,
            int sceneNumber,
            string shotName,
            int takeNumber,
            string description,
            string directory,
            Take iterationBase,
            FrameRate frameRate,
            Texture2D screenshot,
            IExposedPropertyTable resolver)
        {
#if UNITY_EDITOR
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            shotName = FileNameFormatter.Instance.Format(shotName);

            if (string.IsNullOrEmpty(shotName))
            {
                throw new InvalidDataException("Shot name is invalid or empty");
            }

            m_Resolver = resolver;
            m_WildcardFormatter = new WildcardFormatter();

            var assetName = GetAssetName(m_WildcardFormatter, sceneNumber, shotName, takeNumber);
            var assetPath = $"{directory}/{assetName}.asset";
            var exists = AssetDatabase.LoadMainAssetAtPath(assetPath) != null;

            if (exists)
            {
                assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
                shotName = Path.GetFileNameWithoutExtension(assetPath);
            }

            m_ContentsDirectory = Path.GetDirectoryName(assetPath) + "/" + shotName;

            Directory.CreateDirectory(m_ContentsDirectory);

            if (iterationBase == null)
            {
                var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                m_Take = ScriptableObject.CreateInstance<Take>();

                AssetDatabase.CreateAsset(m_Take, assetPath);
                AssetDatabase.AddObjectToAsset(timeline, m_Take);
                AssetDatabase.SetMainObject(m_Take, assetPath);

                m_Take.Timeline = timeline;
            }
            else
            {
                var sourceAssetPath = AssetDatabase.GetAssetPath(iterationBase);
                AssetDatabase.CopyAsset(sourceAssetPath, assetPath);
                m_Take = AssetDatabase.LoadAssetAtPath<Take>(assetPath);
            }

            m_Take.SceneNumber = sceneNumber;
            m_Take.ShotName = shotName;
            m_Take.TakeNumber = takeNumber;
            m_Take.Description = description;
            m_Take.FrameRate = frameRate;
            m_Take.Screenshot = SaveAsPNG(screenshot, assetName, m_ContentsDirectory);

            {
                var timeline = m_Take.Timeline;

                timeline.name = m_Take.name;
                timeline.editorSettings.fps = frameRate.AsFloat();

                if (duration > 0d)
                {
                    timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
                    timeline.fixedDuration = duration;
                }
                else
                {
                    timeline.durationMode = TimelineAsset.DurationMode.BasedOnClips;
                }
            }
#endif
        }

        public void Dispose()
        {
#if UNITY_EDITOR
            AssetDatabase.SaveAssets();

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
                    var duration = Math.Max(partentClip.duration, clip.duration);

                    partentClip.duration = duration;
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
        public T CreateTrack<T>(string name) where T : TrackAsset, new()
        {
            return CreateTrackWithParent<T>(name, null);
        }

        /// <summary>
        /// Creates a new track with binding.
        /// </summary>
        /// <param name="name">The name of the track.</param>
        /// <param name="binding">The binding to use for the new track.</param>
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
            return m_Take.Timeline
                .GetRootTracks()
                .OfType<T>();
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
            return m_Take.BindingEntries
                .Where(e => e.Track is T && e.Binding.Equals(binding))
                .Select(e => e.Track as T);
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

            m_WildcardFormatter.AddReplacement(Wildcards.Name, name);

            var assetName = m_WildcardFormatter.Format(AssetNameFormat);
            assetName = FileNameFormatter.Instance.Format(assetName);

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
                throw new ObjectDisposedException($"{nameof(LiveCapture.Take)} is invalid or has been disposed.");
            }

            if (m_Resolver == null)
            {
                throw new ObjectDisposedException($"{nameof(IExposedPropertyTable)} is invalid or has been disposed.");
            }
        }

        T CreateTrackWithParent<T>(string name, TrackAsset parent) where T : TrackAsset, new()
        {
            CheckDisposed();

            var timeline = m_Take.Timeline;
            var track = timeline.CreateTrack<T>(parent, name);

            track.locked = true;

            return track;
        }

        Texture2D SaveAsPNG(Texture2D texture, string filename, string directory)
        {
#if UNITY_EDITOR
            if (texture != null)
            {
                var assetPath = Screenshot.SaveAsPNG(texture, filename, directory);

                AssetDatabase.ImportAsset(assetPath);

                return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            }
#endif
            return null;
        }
    }
}
