using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityObject = UnityEngine.Object;

namespace Unity.LiveCapture
{
    [Serializable]
    class PlayableDirectorSlate : ISlate
    {
        const string k_DefaultDirectory = "Assets/Takes";
        const string k_DefaultName = "New Shot";

        [SerializeField]
        UnityObject m_UnityObject;
        [SerializeField]
        string m_Directory = k_DefaultDirectory;
        [SerializeField]
        int m_SceneNumber = 1;
        [SerializeField]
        string m_Name = k_DefaultName;
        [SerializeField]
        int m_TakeNumber = 1;
        [SerializeField]
        string m_Description;
        [SerializeField]
        Take m_Take;
        [SerializeField]
        Take m_IterationBase;
        [SerializeField]
        PlayableDirector m_Director;

        public UnityObject unityObject
        {
            get => m_UnityObject;
            set => m_UnityObject = value;
        }

        public PlayableDirector director
        {
            get => m_Director;
            set => m_Director = value;
        }

        public string directory
        {
            get => m_Directory;
            set => m_Directory = value;
        }

        public int sceneNumber
        {
            get => m_SceneNumber;
            set => m_SceneNumber = value;
        }

        public string shotName
        {
            get => m_Name;
            set => m_Name = value;
        }

        public int takeNumber
        {
            get => m_TakeNumber;
            set => m_TakeNumber = value;
        }

        public string description
        {
            get => m_Description;
            set => m_Description = value;
        }

        public Take take
        {
            get => m_Take;
            set => m_Take = value;
        }

        public Take iterationBase
        {
            get => m_IterationBase;
            set => m_IterationBase = value;
        }

        public double duration
        {
            get
            {
                if (m_Take != null && m_Director != null)
                {
                    return m_Director.duration;
                }

                return 0d;
            }
        }

        public double time
        {
            get
            {
                if (m_Director != null)
                {
                    return m_Director.time;
                }

                return 0d;
            }
            set
            {
                if (m_Director != null)
                {
                    m_Director.time = value;
                    m_Director.Pause();
                    m_Director.DeferredEvaluate();

                    Callbacks.InvokeSeekOccurred(this, director);
                }
            }
        }
    }
}
