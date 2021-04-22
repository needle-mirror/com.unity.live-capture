using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Unity.LiveCapture
{
    /// <summary>
    /// A component that manages a collection of slates.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [ExcludeFromPreset]
    [RequireComponent(typeof(PlayableDirector))]
    [RequireComponent(typeof(TakePlayer))]
    [AddComponentMenu("Live Capture/Slate Database")]
    public class SlateDatabase : MonoBehaviour
    {
        [SerializeField]
        PlayableDirectorSlate m_DefaultSlate = new PlayableDirectorSlate();
        TakePlayer m_TakePlayer;
        List<ISlate> m_Slates = new List<ISlate>();
        int m_SelectedSlate;

        /// <summary>
        /// The selected take of the collection. If the collection is empty, the method returns a
        /// default slate instead.
        /// </summary>
        /// <remarks>
        /// The <see cref="TakeRecorder"/> uses the selected slate for the recordings.
        /// The <see cref="SlateTrack"/> uses this to set the active slate to play when needed.
        /// </remarks>
        public ISlate slate
        {
            get => GetSlate();
            set
            {
                m_SelectedSlate = m_Slates.IndexOf(value);

                PrepareTakePlayer();
            }
        }

        internal TakePlayer takePlayer => m_TakePlayer;

        internal int slateCount => m_Slates.Count;

        /// <summary>
        /// Adds a <see cref="ISlate"/> into the collection.
        /// </summary>
        /// <param name="slate">The slate to add.</param>
        public void AddSlate(ISlate slate)
        {
            if (slate == m_DefaultSlate)
            {
                return;
            }

            m_Slates.AddUnique(slate);
        }

        /// <summary>
        /// Removes a <see cref="ISlate"/> from the collection.
        /// </summary>
        /// <param name="slate">The slate to remove.</param>
        public void RemoveSlate(ISlate slate)
        {
            m_Slates.Remove(slate);
        }

        /// <summary>
        /// Gets the slates from the collection.
        /// </summary>
        /// <returns>
        /// An enumerable of slates.
        /// </returns>
        public IEnumerable<ISlate> GetSlates()
        {
            return m_Slates;
        }

        void Awake()
        {
            SetupReferences();
        }

        void Reset()
        {
            SetupReferences();
        }

        void SetupReferences()
        {
            m_DefaultSlate.unityObject = this;
            m_DefaultSlate.director = GetComponent<PlayableDirector>();
            m_TakePlayer = GetComponent<TakePlayer>();
        }

        void Update()
        {
            PrepareTakePlayer();
        }

        void PrepareTakePlayer()
        {
            if (m_TakePlayer != null)
            {
                Debug.Assert(slate != null);

                m_TakePlayer.take = slate.take;
            }
        }

        ISlate GetSlate()
        {
            ISlate slate = m_DefaultSlate;

            if (m_SelectedSlate >= 0 && m_SelectedSlate < m_Slates.Count)
            {
                slate = m_Slates[m_SelectedSlate];
            }
            else if (m_Slates.Count > 0)
            {
                slate = m_Slates[0];
            }

            return slate;
        }
    }
}
