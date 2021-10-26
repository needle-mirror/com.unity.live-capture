using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture.Editor
{
    /// <summary>
    /// The default Inspector for <see cref="LiveCaptureDevice"/>.
    /// </summary>
    /// <remarks>
    /// Inherit from this class when implementing the editor for a custom device.
    /// </remarks>
    [CustomEditor(typeof(LiveCaptureDevice), true)]
    public class LiveCaptureDeviceEditor : UnityEditor.Editor
    {
        static class Contents
        {
            public static readonly GUIContent TakeRecorderNotFound = EditorGUIUtility.TrTextContent($"{nameof(TakeRecorder)} not found. " +
                $"Place the device as a child of a {nameof(TakeRecorder)} component in the hierarchy.");
            public static readonly GUIContent ConfigurationIssues = EditorGUIUtility.TrTextContent("Help",
                "Find out why the device is not ready to operate.");
        }

        static readonly string[] s_ExcludeProperties = { "m_Script" };

        /// <summary>
        /// Called when the editor is being initialized.
        /// </summary>
        protected virtual void OnEnable()
        {
        }

        /// <summary>
        /// Makes a custom inspector GUI for <see cref="LiveCaptureDevice"/>.
        /// </summary>
        public override void OnInspectorGUI()
        {
            DoTakeRecorderHelpBox();
            OnDeviceGUI();
        }

        /// <summary>
        /// Draws the Inspector for the inspected device.
        /// </summary>
        protected virtual void OnDeviceGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, s_ExcludeProperties);

            serializedObject.ApplyModifiedProperties();
        }

        void DoTakeRecorderHelpBox()
        {
            var device = target as LiveCaptureDevice;

            if (device.GetTakeRecorder() == null)
            {
                EditorGUILayout.HelpBox(Contents.TakeRecorderNotFound.text, MessageType.Warning);
            }
        }
    }
}
