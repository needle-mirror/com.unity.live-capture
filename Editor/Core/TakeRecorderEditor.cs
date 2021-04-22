using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Unity.LiveCapture
{
    [CustomEditor(typeof(TakeRecorder))]
    class TakeRecorderEditor : Editor
    {
        static class Contents
        {
            public static readonly GUIContent liveToggleContent = EditorGUIUtility.TrTextContent("", "Toggle the live state of the device.");
            public static readonly GUIContent liveButtonContent = EditorGUIUtility.TrTextContent("Live", "Set live mode.");
            public static readonly GUIContent previewButtonContent = EditorGUIUtility.TrTextContent("Preview", "Set preview mode.");
            public static readonly GUIContent takeNameFormatLabel = EditorGUIUtility.TrTextContent("Take Name Format", "The format of the file name of the output take.");
            public static readonly GUIContent assetNameFormatLabel = EditorGUIUtility.TrTextContent("Asset Name Format", "The format of the file name of the generated assets.");
            public static readonly GUIContent startRecordingLabel = EditorGUIUtility.TrTextContentWithIcon("Start Recording", "Start recording a new Take.", "Animation.Record");
            public static readonly GUIContent stopRecordingLabel = EditorGUIUtility.TrTextContentWithIcon("Stop Recording", "Stop the ongoing recording.", "Animation.Record");
            public static readonly GUIContent playPreviewLabel = EditorGUIUtility.TrTextContentWithIcon("Start Preview", "Start previewing the slected Take.", "PlayButton");
            public static readonly GUIContent stopPreviewLabel = EditorGUIUtility.TrTextContentWithIcon("Stop Preview", "Stop the ongoing playback.", "PauseButton");
            public static GUIContent frameRateLabel = EditorGUIUtility.TrTextContent("Frame Rate", "The frame rate to use for recording.");
            public static readonly GUIStyle buttonToggleStyle = "Button";
            public static readonly string undoSetLive = "Toggle Live Mode";
            public static readonly string undoCreateDevice = "Create Capture Device";
            public static readonly GUILayoutOption[] recordButtonOptions = new[]
            {
                GUILayout.Height(30f)
            };
        }

        static IEnumerable<(Type, CreateDeviceMenuItemAttribute[])> s_CreateDeviceMenuItems;

        TakeRecorder m_TakeRecorder;
        SerializedProperty m_FrameRateProp;
        SerializedProperty m_DevicesProp;
        ReorderableList m_DeviceList;

        void OnEnable()
        {
            m_DevicesProp = serializedObject.FindProperty("m_Devices");
            m_FrameRateProp = serializedObject.FindProperty("m_FrameRate");

            m_TakeRecorder = target as TakeRecorder;

            CreateDeviceList();
        }

        void CreateDeviceList()
        {
            m_DeviceList = new ReorderableList(serializedObject, m_DevicesProp, true, true, true, false);

            m_DeviceList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, new GUIContent("Capture Devices"));
            };
            m_DeviceList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = m_DevicesProp.GetArrayElementAtIndex(index);
                var device = element.objectReferenceValue as LiveCaptureDevice;
                var isLive = device != null ? device.IsLive() : false;

                rect.y += 1.5f;
                rect.height = EditorGUIUtility.singleLineHeight;

                var buttonRect = rect;
                var fieldRect = rect;

                buttonRect.width = 15f;
                fieldRect.x = buttonRect.xMax + 5f;
                fieldRect.width -= 20f;

                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    isLive = GUI.Toggle(buttonRect, isLive, Contents.liveToggleContent);

                    if (change.changed)
                    {
                        Undo.RegisterCompleteObjectUndo(device, Contents.undoSetLive);

                        device.SetLive(isLive);

                        EditorUtility.SetDirty(device);
                    }
                }

                using (new EditorGUI.DisabledGroupScope(true))
                {
                    EditorGUI.PropertyField(fieldRect, element, GUIContent.none);
                }
            };
            m_DeviceList.onAddDropdownCallback = (Rect buttonRect, ReorderableList l) =>
            {
                if (s_CreateDeviceMenuItems == null)
                {
                    s_CreateDeviceMenuItems = AttributeUtility.GetAllTypes<CreateDeviceMenuItemAttribute>();
                }

                var menu = MenuUtility.CreateMenu(s_CreateDeviceMenuItems, (t) => true, (type, attribute) =>
                {
                    CreateDevice(type);
                });

                menu.ShowAsContext();
            };
        }

        void CreateDevice(Type type)
        {
            Debug.Assert(type != null);

            var go = new GameObject($"New {type.Name}", new Type[] { type });
            var device = go.GetComponent<LiveCaptureDevice>();

            go.transform.SetParent(m_TakeRecorder.transform, false);

            Undo.RegisterCreatedObjectUndo(go, Contents.undoCreateDevice);

            m_TakeRecorder.Rebuild();

            EditorGUIUtility.PingObject(go);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(m_TakeRecorder.IsRecording()))
            {
                EditorGUILayout.PropertyField(m_FrameRateProp, Contents.frameRateLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (var change = new EditorGUI.ChangeCheckScope())
                    {
                        var value = GUILayout.Toggle(m_TakeRecorder.IsLive(),
                            Contents.liveButtonContent, Contents.buttonToggleStyle);

                        if (change.changed && value)
                        {
                            Undo.RegisterCompleteObjectUndo(m_TakeRecorder, Contents.undoSetLive);

                            m_TakeRecorder.SetLive(value);

                            EditorUtility.SetDirty(m_TakeRecorder);
                        }
                    }

                    using (var change = new EditorGUI.ChangeCheckScope())
                    {
                        var value = GUILayout.Toggle(!m_TakeRecorder.IsLive(),
                            Contents.previewButtonContent, Contents.buttonToggleStyle);

                        if (change.changed && value)
                        {
                            Undo.RegisterCompleteObjectUndo(m_TakeRecorder, Contents.undoSetLive);

                            m_TakeRecorder.SetLive(!value);

                            EditorUtility.SetDirty(m_TakeRecorder);
                        }
                    }
                }
            }

            if (m_TakeRecorder.IsLive())
            {
                DoRecordButton();
            }
            else
            {
                DoPlayButton();
            }

            EditorGUILayout.Space();

            m_DeviceList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        void DoRecordButton()
        {
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                GUILayout.Toggle(m_TakeRecorder.IsRecording(),
                    GetRecordButtonContent(), Contents.buttonToggleStyle, Contents.recordButtonOptions);

                if (change.changed)
                {
                    if (m_TakeRecorder.IsRecording())
                    {
                        m_TakeRecorder.StopRecording();
                    }
                    else
                    {
                        m_TakeRecorder.StartRecording();
                    }
                }
            }
        }

        void DoPlayButton()
        {
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                GUILayout.Toggle(m_TakeRecorder.IsPreviewPlaying(),
                    GetPreviewButtonContent(), Contents.buttonToggleStyle, Contents.recordButtonOptions);

                if (change.changed)
                {
                    if (m_TakeRecorder.IsPreviewPlaying())
                    {
                        m_TakeRecorder.SetPreviewTime(0d);
                    }
                    else
                    {
                        m_TakeRecorder.PlayPreview();
                    }
                }
            }
        }

        GUIContent GetRecordButtonContent()
        {
            if (m_TakeRecorder.IsRecording())
            {
                return Contents.stopRecordingLabel;
            }
            else
            {
                return Contents.startRecordingLabel;
            }
        }

        GUIContent GetPreviewButtonContent()
        {
            if (m_TakeRecorder.IsPreviewPlaying())
            {
                return Contents.stopPreviewLabel;
            }
            else
            {
                return Contents.playPreviewLabel;
            }
        }
    }
}
