using UnityEngine;
using UnityEditor;
using Unity.LiveCapture.Editor;

namespace Unity.LiveCapture.Mocap.Editor
{
    /// <summary>
    /// The default Inspector for <see cref="Unity.LiveCapture.Mocap.MocapDevice{T}"/>.
    /// </summary>
    /// <remarks>
    /// Inherit from this class when implementing the editor for a custom mocap source device.
    /// </remarks>
    [CustomEditor(typeof(MocapDevice<>), true)]
    public class MocapDeviceEditor : LiveCaptureDeviceEditor
    {
        static class Contents
        {
            public static readonly GUIContent Animator = EditorGUIUtility.TrTextContent("Animator", "The Animator component to animate.");
        }

        IMocapDevice m_Device;
        SerializedProperty m_Animator;
        SerializedProperty m_Recorder;

        /// <inheritdoc/>
        protected override void OnEnable()
        {
            base.OnEnable();

            m_Device = target as IMocapDevice;
            m_Animator = serializedObject.FindProperty("m_Animator");
            m_Recorder = serializedObject.FindProperty("m_Recorder");
        }

        /// <inheritdoc/>
        protected override void OnDeviceGUI()
        {
            serializedObject.Update();

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                using (new EditorGUI.DisabledScope(m_Device.TryGetMocapGroup(out var _)))
                {
                    EditorGUILayout.PropertyField(m_Animator, Contents.Animator);
                }

                EditorGUILayout.PropertyField(m_Recorder);

                if (change.changed)
                {
                    m_Device.RegisterLiveProperties();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
