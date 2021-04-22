using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityObject = UnityEngine.Object;

namespace Unity.LiveCapture
{
    [CustomEditor(typeof(SlatePlayableAsset))]
    class SlatePlayableAssetEditor : Editor
    {
        class ProxySlate : ISlate
        {
            SlatePlayableAsset m_SlatePlayableAsset;

            public UnityObject unityObject => m_SlatePlayableAsset;

            public string directory
            {
                get => m_SlatePlayableAsset.directory;
                set => m_SlatePlayableAsset.directory = value;
            }

            public int sceneNumber
            {
                get => m_SlatePlayableAsset.sceneNumber;
                set => m_SlatePlayableAsset.sceneNumber = value;
            }

            public string shotName
            {
                get => m_SlatePlayableAsset.clip.displayName;
                set => m_SlatePlayableAsset.clip.displayName = value;
            }

            public int takeNumber
            {
                get => m_SlatePlayableAsset.takeNumber;
                set => m_SlatePlayableAsset.takeNumber = value;
            }

            public string description
            {
                get => m_SlatePlayableAsset.description;
                set => m_SlatePlayableAsset.description = value;
            }

            public Take take
            {
                get => m_SlatePlayableAsset.take;
                set => m_SlatePlayableAsset.take = value;
            }

            public Take iterationBase
            {
                get => m_SlatePlayableAsset.iterationBase;
                set => m_SlatePlayableAsset.iterationBase = value;
            }

            public double start => throw new NotImplementedException();

            public double duration => throw new NotImplementedException();

            public double time { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public ProxySlate(SlatePlayableAsset slatePlayableAsset)
            {
                m_SlatePlayableAsset = slatePlayableAsset;
            }

            public bool IsPlaying()
            {
                throw new NotImplementedException();
            }

            public void Pause()
            {
                throw new NotImplementedException();
            }

            public void Play()
            {
                throw new NotImplementedException();
            }
        }

        SerializedProperty m_DirectoryProp;
        SerializedProperty m_NameProp;
        SlatePlayableAsset m_SlateAsset;
        SlateInspector m_SlateInspector;
        ProxySlate m_ProxySlate;

        void OnEnable()
        {
            m_SlateInspector = new SlateInspector();
            m_SlateAsset = target as SlatePlayableAsset;

            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        void UndoRedoPerformed()
        {
            m_SlateInspector.Refresh();
        }

        public override void OnInspectorGUI()
        {
            CreateProxyIfNeeded();

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                m_SlateInspector.OnGUI(m_ProxySlate);

                if (change.changed)
                {
                    TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
                }
            }
        }

        void CreateProxyIfNeeded()
        {
            if (m_ProxySlate == null && m_SlateAsset.clip != null)
            {
                m_ProxySlate = new ProxySlate(m_SlateAsset);
            }
        }
    }
}
