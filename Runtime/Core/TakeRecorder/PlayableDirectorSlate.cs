using System;
using UnityEngine;
using UnityEngine.Playables;

using UnityObject = UnityEngine.Object;

namespace Unity.LiveCapture
{
    [Serializable]
    class PlayableDirectorSlate : ISlatePlayer, ISlate
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

        public UnityObject UnityObject
        {
            get => m_UnityObject;
            set => m_UnityObject = value;
        }

        public PlayableDirector Director
        {
            get => m_Director;
            set => m_Director = value;
        }

        public string Directory
        {
            get => m_Directory;
            set => m_Directory = value;
        }

        public int SceneNumber
        {
            get => m_SceneNumber;
            set => m_SceneNumber = value;
        }

        public string ShotName
        {
            get => m_Name;
            set => m_Name = value;
        }

        public int TakeNumber
        {
            get => m_TakeNumber;
            set => m_TakeNumber = value;
        }

        public string Description
        {
            get => m_Description;
            set => m_Description = value;
        }

        public Take Take
        {
            get => m_Take;
            set => m_Take = value;
        }

        public Take IterationBase
        {
            get => m_IterationBase;
            set => m_IterationBase = value;
        }

        public double Duration
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

        public ISlate GetActiveSlate()
        {
            return this;
        }

        public ISlate GetSlate(int index)
        {
            return this;
        }

        public int GetSlateCount()
        {
            return 1;
        }

        public double GetTime()
        {
            if (m_Director != null)
            {
                return m_Director.time;
            }

            return 0d;
        }

        public void SetTime(double time)
        {
            SetTime(GetActiveSlate(), time);
        }

        public void SetTime(ISlate slate, double time)
        {
            if (m_Director != null)
            {
                m_Director.time = time;
                m_Director.Pause();
                m_Director.DeferredEvaluate();

                Callbacks.InvokeSeekOccurred(this, Director);
            }
        }
    }
}
