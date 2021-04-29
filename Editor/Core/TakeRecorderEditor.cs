using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.Timeline;

namespace Unity.LiveCapture.Editor
{
    using Editor = UnityEditor.Editor;

    [CustomEditor(typeof(TakeRecorder))]
    class TakeRecorderEditor : Editor
    {
        static class Contents
        {
            public static readonly GUIContent EnableToggleContent = EditorGUIUtility.TrTextContent("", "Toggle the enabled state of the device.");
            public static readonly GUIContent LiveButtonContent = EditorGUIUtility.TrTextContent("Live", "Set to live mode for previewing and recording takes.");
            public static readonly GUIContent PreviewButtonContent = EditorGUIUtility.TrTextContent("Playback", "Set to playback mode for reviewing takes.");
            public static readonly GUIContent StartRecordingLabel = EditorGUIUtility.TrTextContentWithIcon("Start Recording", "Start recording a new Take.", "Animation.Record");
            public static readonly GUIContent CreateTrackLabel = EditorGUIUtility.TrTextContent("Create Track", "Create a Take Recorder Track in the current inspected Timeline.");
            public static readonly string LockButtonTooltip = L10n.Tr("Lock the Clip currently in context to keep it active in the Take Recorder regardless of the Timeline playhead position.");
            public static readonly string NoDevicesMessage = L10n.Tr("Recording requires at least a capture device.");
            public static readonly string NoDeviceReadyMessage = L10n.Tr("Recording requires an enabled and configured capture device.");
            public static readonly string CreateTrackMessage = L10n.Tr("Create a TakeRecorderTrack to start recording in Timeline");
            public static readonly string ReadMoreText = L10n.Tr("read more");
            public static readonly string CreateDeviceURL = Documentation.baseURL + "ref-component-take-recorder" + Documentation.endURL;
            public static readonly GUIContent StopRecordingLabel = EditorGUIUtility.TrTextContentWithIcon("Stop Recording", "Stop the ongoing recording.", "Animation.Record");
            public static readonly GUIContent PlayPreviewLabel = EditorGUIUtility.TrTextContentWithIcon("Start Preview", "Start previewing the selected Take.", "PlayButton");
            public static readonly GUIContent StopPreviewLabel = EditorGUIUtility.TrTextContentWithIcon("Stop Preview", "Stop the ongoing playback.", "PauseButton");
            public static readonly GUIContent FrameRateLabel = EditorGUIUtility.TrTextContent("Frame Rate", "The frame rate to use for recording.");
            public static readonly GUIContent ProjectSettingsButton = EditorGUIUtility.TrTextContent("Open Project Settings", "Open the Take System project settings.");
            public static readonly string ExternalSlateMsg = L10n.Tr("Slate data provided externally using a Timeline track.");
            public static readonly GUIContent DeviceReadyIcon = EditorGUIUtility.TrIconContent("winbtn_mac_max");
            public static readonly GUIContent DeviceNotReadyIcon = EditorGUIUtility.TrIconContent("winbtn_mac_close");
            public static readonly GUIStyle ButtonToggleStyle = "Button";
            public static readonly GUIStyle LockStyle = "IN LockButton";
            public static readonly string UndoSetLive = L10n.Tr("Toggle Live Mode");
            public static readonly string UndoEnableDevice = L10n.Tr("Set Enabled");
            public static readonly string UndoCreateDevice = L10n.Tr("Create Capture Device");
            public static readonly GUILayoutOption[] LargeButtonOptions =
            {
                GUILayout.Height(30f)
            };

            public static string GetRecordButtonMessage(int deviceCount)
            {
                if (deviceCount == 0)
                {
                    return NoDevicesMessage;
                }
                else
                {
                    return NoDeviceReadyMessage;
                }
            }
        }

        static List<TakeRecorderEditor> s_Editors = new List<TakeRecorderEditor>();

        internal static void RepaintEditors()
        {
            foreach (var editor in s_Editors)
            {
                editor.Repaint();
            }
        }

        static IEnumerable<(Type, CreateDeviceMenuItemAttribute[])> s_CreateDeviceMenuItems;

        SlateInspector m_SlateInspector;
        TakeRecorder m_TakeRecorder;
        PlayableDirector m_Director;
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
            m_Director = m_TakeRecorder.GetComponent<PlayableDirector>();

            CreateDeviceList();

            TakeRecorder.PlaybackStateChanged += OnPlaybackStateChanged;
            Undo.undoRedoPerformed += UndoRedoPerformed;

            s_Editors.AddUnique(this);
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            TakeRecorder.PlaybackStateChanged -= OnPlaybackStateChanged;

            m_SlateInspector.Dispose();

            s_Editors.Remove(this);
        }

        void OnPlaybackStateChanged(TakeRecorder takeRecorder)
        {
            if (m_TakeRecorder == takeRecorder)
            {
                Repaint();
            }
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
                var isDeviceEnabled = device != null ? device.enabled : false;

                rect.y += 1.5f;
                rect.height = EditorGUIUtility.singleLineHeight;

                var buttonRect = rect;
                var statusRect = rect;
                var fieldRect = rect;

                buttonRect.width = 15f;
                statusRect.width = 15f;
                statusRect.x = buttonRect.xMax + 5f;
                fieldRect.x = statusRect.xMax + 5f;
                fieldRect.width -= 40f;

                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    isDeviceEnabled = GUI.Toggle(buttonRect, isDeviceEnabled, Contents.EnableToggleContent);

                    if (change.changed)
                    {
                        Undo.RegisterCompleteObjectUndo(device, Contents.UndoEnableDevice);

                        device.enabled = isDeviceEnabled;

                        EditorUtility.SetDirty(device);
                        EditorApplication.QueuePlayerLoopUpdate();
                    }
                }

                EditorGUI.LabelField(statusRect, GetStatusIcon(device));

                using (new EditorGUI.DisabledGroupScope(true))
                {
                    EditorGUI.PropertyField(fieldRect, element, GUIContent.none);
                }
            };
            m_DeviceList.onAddDropdownCallback = (Rect buttonRect, ReorderableList l) =>
            {
                if (s_CreateDeviceMenuItems == null)
                {
                    var allTypes = AttributeUtility.GetAllTypes<CreateDeviceMenuItemAttribute>();
                    var assemblyName = "Unity.LiveCapture.Mocap";
                    var mocapGroupTypeName = "Unity.LiveCapture.Mocap.MocapGroup";
                    var mocapDeviceTypeName = "Unity.LiveCapture.Mocap.IMocapDevice";
                    var mocapGroupType = Type.GetType($"{mocapGroupTypeName}, {assemblyName}");
                    var mocapDeviceType = Type.GetType($"{mocapDeviceTypeName}, {assemblyName}");

                    Debug.Assert(mocapGroupType != null);
                    Debug.Assert(mocapDeviceType != null);

                    var hasMocapDevices = allTypes
                        .Where(t => mocapDeviceType.IsAssignableFrom(t.type))
                        .Any();

                    if (hasMocapDevices)
                    {
                        s_CreateDeviceMenuItems = allTypes;
                    }
                    else
                    {
                        s_CreateDeviceMenuItems = allTypes.Where(t => t.type != mocapGroupType);
                    }
                }

                var menu = MenuUtility.CreateMenu(s_CreateDeviceMenuItems, (t) => true, (type, attribute) =>
                {
                    CreateDevice(type);
                });

                menu.ShowAsContext();
            };
        }

        GUIContent GetStatusIcon(LiveCaptureDevice device)
        {
            if (device.IsReady())
            {
                return Contents.DeviceReadyIcon;
            }
            else
            {
                return Contents.DeviceNotReadyIcon;
            }
        }

        void CreateDevice(Type type)
        {
            Debug.Assert(type != null);

            var go = new GameObject($"New {type.Name}", type);

            go.transform.SetParent(m_TakeRecorder.transform, false);
            GameObjectUtility.EnsureUniqueNameForSibling(go);

            Undo.RegisterCreatedObjectUndo(go, Contents.UndoCreateDevice);

            EditorGUIUtility.PingObject(go);
        }

        public override void OnInspectorGUI()
        {
            DoRecordGUI();

            var disable = m_TakeRecorder.IsPreviewPlaying();

            using (new EditorGUI.DisabledScope(disable))
            {
                DoCreateTrackButton();
                DoTimelineInspector();
                DoSlateInspector();

                EditorGUILayout.Space();

                DoDevicesGUI();

                EditorGUILayout.Space();

                DoSettingsLinkGUI();
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
            var context = m_TakeRecorder.GetContext();

            if (context == null)
            {
                return;
            }

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                var slate = context.GetSlate();
                var director = context.GetResolver() as PlayableDirector;

                Debug.Assert(director != null);

                m_SlateInspector.OnGUI(slate, director);

                if (change.changed)
                {
                    context.Prepare(false);
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
            var canStartRecording = m_TakeRecorder.CanStartRecording();

            using (new EditorGUI.DisabledScope(!canStartRecording))
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                GUILayout.Toggle(m_TakeRecorder.IsRecording(),
                    GetRecordButtonContent(), Contents.ButtonToggleStyle, Contents.LargeButtonOptions);

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

            if (!canStartRecording)
            {
                var deviceCount = m_DevicesProp.arraySize;
                var message = Contents.GetRecordButtonMessage(deviceCount);

                LiveCaptureGUI.HelpBoxWithURL(message, Contents.ReadMoreText, Contents.CreateDeviceURL, MessageType.Warning);
            }
        }

        void DoPlayButton()
        {
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                GUILayout.Toggle(m_TakeRecorder.IsPreviewPlaying(),
                    GetPreviewButtonContent(), Contents.ButtonToggleStyle, Contents.LargeButtonOptions);

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

        void DoCreateTrackButton()
        {
            if (!Timeline.IsActive()
                || m_TakeRecorder.IsLocked()
                || m_TakeRecorder.HasExternalContextProvider()
                || m_Director == Timeline.InspectedDirector)
            {
                return;
            }

            EditorGUILayout.HelpBox(Contents.CreateTrackMessage, MessageType.None, true);

            if (GUILayout.Button(Contents.CreateTrackLabel, Contents.LargeButtonOptions))
            {
                var director = Timeline.InspectedDirector;
                var timeline = Timeline.InspectedAsset;
                var track = timeline.CreateTrack<TakeRecorderTrack>();
                var clip = track.CreateClip<SlatePlayableAsset>();

                clip.displayName = "New Shot";

                if (TimelineHierarchy.TryGetParentContext(director, out var parentDirector, out var parentClip))
                {
                    clip.duration = parentClip.duration;
                }
                else if (timeline.duration > 0d)
                {
                    clip.duration = timeline.duration;
                }

                TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
            }
        }

        void DoTimelineInspector()
        {
            var context = m_TakeRecorder.GetContext();
            var isLocked = m_TakeRecorder.IsLocked();
            var isPlaying = m_TakeRecorder.IsPreviewPlaying();

            if (context == null || !isLocked && !m_TakeRecorder.HasExternalContextProvider())
            {
                return;
            }

            EditorGUILayout.HelpBox(context.ToString(), MessageType.None, true);

            var slate = context.GetSlate();
            var content = new GUIContent(slate.ShotName, Contents.LockButtonTooltip);

            using (new EditorGUI.DisabledScope(m_TakeRecorder.IsPreviewPlaying()))
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                var value = DoLockToggle(content, isLocked || isPlaying,
                    Contents.ButtonToggleStyle, Contents.LargeButtonOptions);

                if (change.changed)
                {
                    if (value)
                    {
                        m_TakeRecorder.LockContext();
                    }
                    else
                    {
                        m_TakeRecorder.UnlockContext();
                    }

                    TimelineEditor.Refresh(RefreshReason.WindowNeedsRedraw);
                }
            }
        }

        static bool DoLockToggle(GUIContent content, bool isLocked, GUIStyle style, params GUILayoutOption[] options)
        {
            var rect = EditorGUILayout.GetControlRect(options);
            var value = GUI.Toggle(rect, isLocked, content, style);

            if (Event.current.type == EventType.Repaint)
            {
                var lockStyle = Contents.LockStyle;
                var textSize = Contents.ButtonToggleStyle.CalcSize(content);
                var position = new Rect(
                    rect.x + (rect.width - textSize.x) * 0.5f - lockStyle.fixedWidth,
                    rect.y + (rect.height - lockStyle.fixedHeight) * 0.5f,
                    lockStyle.fixedWidth, lockStyle.fixedHeight);

                if (position.x > rect.x)
                {
                    lockStyle.Draw(position, false, GUI.enabled, isLocked, false);
                }
            }

            return value;
        }
    }
}
