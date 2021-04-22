using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Unity.LiveCapture
{
    /// <summary>
    /// A component that plays a <see cref="Take"/>.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayableDirector))]
    [ExcludeFromPreset]
    [AddComponentMenu("Live Capture/Take Player")]
    public class TakePlayer : MonoBehaviour
    {
        [SerializeField, HideInInspector]
        PlayableAsset m_NullPlayableAsset = null;
        [SerializeField]
        Take m_Take;
        PlayableDirector m_Director;
        IEnumerable<TrackBindingEntry> m_Entries;

        /// <summary>
        /// The <see cref="Take"/> to play.
        /// </summary>
        public Take take
        {
            get => m_Take;
            set
            {
                if (m_Take != value)
                {
                    ClearSceneBindings();

                    m_Take = value;

                    HandlePlayableAssetChange();
                    SetSceneBindings();
                }
            }
        }

        void OnValidate()
        {
            Validate();
        }

        void Reset()
        {
            SetupComponentsIfNeeded();
        }

        void Awake()
        {
            SetupComponentsIfNeeded();
        }

        void SetupComponentsIfNeeded()
        {
            if (m_Director == null)
            {
                m_Director = GetComponent<PlayableDirector>();
            }
        }

        void Update()
        {
            HandlePlayableAssetChange();
        }

        void HandlePlayableAssetChange()
        {
            SetupComponentsIfNeeded();

            Debug.Assert(m_Director != null);
            Debug.Assert(m_NullPlayableAsset != null);

            var playableAsset = m_NullPlayableAsset;

            if (m_Take != null)
            {
                playableAsset = m_Take.timeline;
            }

            if (m_Director.playableAsset != playableAsset)
            {
                m_Director.playableAsset = playableAsset;
                m_Director.DeferredEvaluate();
            }
        }

        internal void Validate()
        {
            SetupComponentsIfNeeded();
            ClearSceneBindings();

            Debug.Assert(m_Director != null);

            if (m_Take != null)
            {
                var resolver = m_Director as IExposedPropertyTable;
                var entries = m_Take.bindingEntries;

                foreach (var entry in entries)
                {
                    var binding = entry.binding;

                    if (binding == null)
                    {
                        continue;
                    }

                    var value = binding.GetValue(resolver);

                    if (value == null)
                    {
                        continue;
                    }

                    if (typeof(Component).IsAssignableFrom(binding.type) &&
                        value is GameObject go)
                    {
                        value = go.GetComponent(binding.type);

                        if (value != null)
                        {
                            binding.SetValue(value, resolver);
                        }
                        else
                        {
                            binding.ClearValue(resolver);
                        }
                    }
                    else if (!binding.type.IsAssignableFrom(value.GetType()))
                    {
                        binding.ClearValue(resolver);
                    }
                }
            }

            SetSceneBindings();
        }

        void ClearSceneBindings()
        {
            if (m_Entries == null)
            {
                return;
            }

            Debug.Assert(m_Director != null);

            foreach (var entry in m_Entries)
            {
                m_Director.ClearGenericBinding(entry.track);
            }

            m_Entries = null;
        }

        void SetSceneBindings()
        {
            m_Entries = null;

            if (m_Take == null || m_Director == null)
            {
                return;
            }

            m_Entries = m_Take.bindingEntries;

            foreach (var entry in m_Entries)
            {
                var track = entry.track;
                var binding = entry.binding;

                if (track == null ||  binding == null)
                {
                    continue;
                }

                var value = binding.GetValue(m_Director);

                m_Director.SetGenericBinding(track, value);
            }
        }
    }
}
