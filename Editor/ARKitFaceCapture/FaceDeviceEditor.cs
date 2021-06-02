using Unity.LiveCapture.CompanionApp.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture.ARKitFaceCapture.Editor
{
    [CustomEditor(typeof(FaceDevice))]
    class FaceDeviceEditor : CompanionAppDeviceEditor<IFaceClient>
    {
        static readonly string[] s_ExcludeProperties = { "m_Script", "m_Actor", "m_LiveLink" };

        FaceDevice m_Device;
        SerializedProperty m_Actor;
        SerializedProperty m_LiveLinkChannels;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_Device = target as FaceDevice;

            m_Actor = serializedObject.FindProperty("m_Actor");
            m_LiveLinkChannels = serializedObject.FindProperty("m_LiveLink.Channels");
        }

        protected override void OnDeviceGUI()
        {
            DoClientGUI();

            serializedObject.Update();

            DoActorGUI(m_Actor);
            DoLiveLinkChannelsGUI(m_LiveLinkChannels);
            DrawPropertiesExcluding(serializedObject, s_ExcludeProperties);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
