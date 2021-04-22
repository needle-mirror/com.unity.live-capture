using Unity.LiveCapture.CompanionApp;
using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture.ARKitFaceCapture
{
    [CustomEditor(typeof(FaceDevice))]
    class FaceDeviceEditor : CompanionAppDeviceEditor<IFaceClient>
    {
        static readonly string[] s_ExcludeProperties = { "m_Script", "m_Actor", "m_LiveLink" };

        FaceDevice m_Device;
        SerializedProperty m_LiveLinkChannels;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_Device = target as FaceDevice;

            m_LiveLinkChannels = serializedObject.FindProperty("m_LiveLink.channels");
        }

        protected override void OnDeviceGUI()
        {
            DoClientGUI();
            DoActorGUI(m_Device.actor, (actor) => m_Device.actor = actor);

            serializedObject.Update();

            DoLiveLinkChannelsGUI(m_LiveLinkChannels);
            DrawPropertiesExcluding(serializedObject, s_ExcludeProperties);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
