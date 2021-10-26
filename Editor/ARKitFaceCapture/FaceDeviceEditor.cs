using UnityEditor;
using Unity.LiveCapture.CompanionApp.Editor;
using Unity.LiveCapture.Editor;

namespace Unity.LiveCapture.ARKitFaceCapture.Editor
{
    [CustomEditor(typeof(FaceDevice))]
    class FaceDeviceEditor : CompanionAppDeviceEditor<IFaceClient>
    {
        static class Contents
        {
            public static string MissingActorText = L10n.Tr("The device requires a Face Actor target.");
            public static string MissingClientText = L10n.Tr("The device requires a connected Client.");
            public static string ReadMoreText = L10n.Tr("read more");
            public static string ConnectClientURL = "https://docs.unity3d.com/Packages/com.unity.live-capture@1.0/manual/setup-connecting.html";
            public static string SetupActorURL = "https://docs.unity3d.com/Packages/com.unity.live-capture@1.0/manual/face-capture-getting-started.html";
        }

        static readonly string[] s_ExcludeProperties = { "m_Script", "m_Actor", "m_Channels" };

        FaceDevice m_Device;
        SerializedProperty m_Actor;
        SerializedProperty m_Channels;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_Device = target as FaceDevice;

            m_Actor = serializedObject.FindProperty("m_Actor");
            m_Channels = serializedObject.FindProperty("m_Channels");
        }

        protected override void OnDeviceGUI()
        {
            DoClientGUI();

            if (m_Device.GetClient() == null)
            {
                LiveCaptureGUI.HelpBoxWithURL(Contents.MissingClientText, Contents.ReadMoreText,
                    Contents.ConnectClientURL, MessageType.Warning);
            }

            serializedObject.Update();

            DoActorGUI(m_Actor);

            if (m_Actor.objectReferenceValue == null)
            {
                LiveCaptureGUI.HelpBoxWithURL(Contents.MissingActorText, Contents.ReadMoreText,
                    Contents.SetupActorURL, MessageType.Warning);
            }

            DoChannelsGUI(m_Channels);
            DrawPropertiesExcluding(serializedObject, s_ExcludeProperties);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
