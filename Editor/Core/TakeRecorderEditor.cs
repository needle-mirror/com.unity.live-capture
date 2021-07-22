using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.Timeline;

namespace Unity.LiveCapture.Editor
{
    [CustomEditor(typeof(TakeRecorder))]
    class TakeRecorderEditor : UnityEditor.Editor
    {
        static class Contents
        {
            public static readonly GUIContent LiveToggleContent = EditorGUIUtility.TrTextContent("", "Toggle the live state of the device.");
            public static readonly GUIContent LiveButtonContent = EditorGUIUtility.TrTextContent("Live", "Set to live mode for previewing and recording takes.");
            public static readonly GUIContent PreviewButtonContent = EditorGUIUtility.TrTextContent("Playback", "Set to playback mode for reviewing takes.");
            public static readonly GUIContent StartRecordingLabel = EditorGUIUtility.TrTextContentWithIcon("Start Recording", "Start recording a new Take.", "Animation.Record");
            public static readonly GUIContent StopRecordingLabel = EditorGUIUtility.TrTextContentWithIcon("Stop Recording", "Stop the ongoing recording.", "Animation.Record");
            public static readonly GUIContent PlayPreviewLabel = EditorGUIUtility.TrTextContentWithIcon("Start Preview", "Start previewing the selected Take.", "PlayButton");
            public static readonly GUIContent StopPreviewLabel = EditorGUIUtility.TrTextContentWithIcon("Stop Preview", "Stop the ongoing playback.", "PauseButton");
            public static readonly GUIContent FrameRateLabel = EditorGUIUtility.TrTextContent("Frame Rate", "The frame rate to use for recording.");
            public static readonly GUIContent ProjectSettingsButton = EditorGUIUtility.TrTextContent("Open Project Settings", "Open the Take System project settings.");
            public static readonly string ExternalSlateMsg = EditorGUIUtility.TrTextContent("Slate data provided externally using a Timeline track.").text;
            public static readonly GUIStyle ButtonToggleStyle = "Button";
            public static readonly string UndoSetLive = "Toggle Live Mode";
            public static readonly string UndoCreateDevice = "Create Capture Device";
            public static readonly GUILayoutOption[] RecordButtonOptions =
            {
                GUILayout.Height(30f)
            };
        }

        static IEnumerable<(Type, CreateDeviceMenuItemAttribute[])> s_CreateDeviceMenuItems;

        SlateInspector m_SlateInspector;
        TakeRecorder m_TakeRecorder;
        SerializedProperty m_FrameRateProp;
        SerializedProperty m_Take;
        SerializedProperty m_DevicesProp;
        ReorderableList m_DeviceList;

        void OnEnable()
        {
            m_SlateInspector = new SlateInspector();
            m_FrameRateProp = serializedObject.FindProperty("m_FrameRate");
            m_Take = serializedObject.FindProperty("m_TakePlayer.m_Take");
            m_DevicesProp = serializedObject.FindProperty("m_Devices");

            m_TakeRecorder = target as TakeRecorder;

            CreateDeviceList();

            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;

            m_SlateInspector.Dispose();
        }

        void UndoRedoPerformed()
        {
            m_SlateInspector.Refresh();
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
                    isLive = GUI.Toggle(buttonRect, isLive, Contents.LiveToggleContent);

                    if (change.changed)
                    {
                        Undo.RegisterCompleteObjectUndo(device, Contents.UndoSetLive);

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

            var go = new GameObject($"New {type.Name}", type);

            go.transform.SetParent(m_TakeRecorder.transform, false);
            GameObjectUtility.EnsureUniqueNameForSibling(go);

            Undo.RegisterCreatedObjectUndo(go, Contents.UndoCreateDevice);

            m_TakeRecorder.Rebuild();

            EditorGUIUtility.PingObject(go);
        }

        public override void OnInspectorGUI()
        {
            DoRecordGUI();
            DoExternalPrevewMsg();
            DoSlateInspector();

            EditorGUILayout.Space();

            DoDevicesGUI();

            EditorGUILayout.Space();

            DoSettingsLinkGUI();
        }

        void DoExternalPrevewMsg()
        {
            if (m_TakeRecorder.IsExternalSlatePlayer())
            {
                EditorGUILayout.HelpBox(Contents.ExternalSlateMsg, MessageType.None, true);
            }
        }

        void DoRecordGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(m_TakeRecorder.IsRecording()))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (var change = new EditorGUI.ChangeCheckScope())
                    {
                        var value = GUILayout.Toggle(m_TakeRecorder.IsLive(),
                            Contents.LiveButtonContent, Contents.ButtonToggleStyle);

                        if (change.changed && value)
                        {
                            Undo.RegisterCompleteObjectUndo(m_TakeRecorder, Contents.UndoSetLive);

                            m_TakeRecorder.SetLive(value);

                            EditorUtility.SetDirty(m_TakeRecorder);
                        }
                    }

                    using (var change = new EditorGUI.ChangeCheckScope())
                    {
                        var value = GUILayout.Toggle(!m_TakeRecorder.IsLive(),
                            Contents.PreviewButtonContent, Contents.ButtonToggleStyle);

                        if (change.changed && value)
                        {
                            Undo.RegisterCompleteObjectUndo(m_TakeRecorder, Contents.UndoSetLive);

                            m_TakeRecorder.SetLive(!value);

                            EditorUtility.SetDirty(m_TakeRecorder);
                        }
                    }
                }
            }

            var slate = m_TakeRecorder.GetActiveSlate();

            using (new EditorGUI.DisabledScope(slate == null))
            {
                if (m_TakeRecorder.IsLive())
                {
                    DoRecordButton();
                }
                else
                {
                    DoPlayButton();
                }
            }

            using (new EditorGUI.DisabledScope(m_TakeRecorder.IsRecording()))
            {
                EditorGUILayout.PropertyField(m_FrameRateProp, Contents.FrameRateLabel);
            }

            serializedObject.ApplyModifiedProperties();
        }

        void DoSlateInspector()
        {
            var slate = m_TakeRecorder.GetActiveSlate();

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                m_SlateInspector.OnGUI(slate, m_TakeRecorder.GetPlayableDirector());

                if (change.changed)
                {
                    TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
                }
            }
        }

        void DoDevicesGUI()
        {
            serializedObject.Update();

            m_DeviceList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        void DoSettingsLinkGUI()
        {
            if (GUILayout.Button(Contents.ProjectSettingsButton))
            {
                LiveCaptureSettingsProvider.Open();
            }
        }

        void DoRecordButton()
        {
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                GUILayout.Toggle(m_TakeRecorder.IsRecording(),
                    GetRecordButtonContent(), Contents.ButtonToggleStyle, Contents.RecordButtonOptions);

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
                    GetPreviewButtonContent(), Contents.ButtonToggleStyle, Contents.RecordButtonOptions);

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
                return Contents.StopRecordingLabel;
            }
            else
            {
                return Contents.StartRecordingLabel;
            }
        }

        GUIContent GetPreviewButtonContent()
        {
            if (m_TakeRecorder.IsPreviewPlaying())
            {
                return Contents.StopPreviewLabel;
            }
            else
            {
                return Contents.PlayPreviewLabel;
            }
        }
    }
}
