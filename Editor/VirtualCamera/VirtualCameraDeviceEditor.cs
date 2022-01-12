using System;
using Unity.LiveCapture.Editor;
using Unity.LiveCapture.CompanionApp.Editor;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.ObjectModel;

namespace Unity.LiveCapture.VirtualCamera.Editor
{
    [CustomEditor(typeof(VirtualCameraDevice))]
    class VirtualCameraDeviceEditor : CompanionAppDeviceEditor<IVirtualCameraClient>
    {
        static readonly float k_SnapshotElementHeight = 4f * (EditorGUIUtility.singleLineHeight + 2f) + 2f;
        static readonly float k_PreviewWidth = k_SnapshotElementHeight * 16f / 9f;

        static readonly ReadOnlyCollection<(GUIContent label, Func<UnityEngine.Object> createActor)> k_ActorCreateMenuItems =
            new ReadOnlyCollection<(GUIContent, Func<UnityEngine.Object>)>
            (
                new(GUIContent, Func<UnityEngine.Object>)[]
                {
                    (Contents.CreateVirtualCameraActor, CreateVirtualCameraActor),
#if VP_CINEMACHINE_2_4_0
                    (Contents.CreateCinemachineCameraActor, CreateCinemachineCameraActor)
#endif
                }
            );

        public static class Contents
        {
            public static GUIContent None = EditorGUIUtility.TrTextContent("None");
            public static string ActorWarningMessage = L10n.Tr("Device requires a Virtual Camera Actor target.");
            public static GUIContent CreateVirtualCameraActor = EditorGUIUtility.TrTextContent("Virtual Camera Actor");
            public static GUIContent CreateCinemachineCameraActor = EditorGUIUtility.TrTextContent("Cinemachine Camera Actor");
            public static GUIContent LensAssetLabel = EditorGUIUtility.TrTextContent("Lens Asset", "The asset that provides the lens intrinsics.");
            public static GUIContent CameraBody = EditorGUIUtility.TrTextContent("Camera Body", "The parameters of the camera's body.");
            public static GUIContent Settings = EditorGUIUtility.TrTextContent("Settings", "The settings of the device.");
            public static GUIContent VideoSettingsButton = EditorGUIUtility.TrTextContent("Open Video Settings", "Open the settings of the video server.");
            public static string Deleted = L10n.Tr("(deleted)");
            public static GUIContent Snapshots = EditorGUIUtility.TrTextContent("Snapshots", "The snapshots taken using this device.");
            public static GUIContent TakeSnapshot = EditorGUIUtility.TrTextContent("Take Snapshot", "Save the current position, lens and camera body while generating a screenshot.");
            public static GUIContent GotoLabel = EditorGUIUtility.TrTextContent("Go To", "Move the camera to the saved position.");
            public static GUIContent Load = EditorGUIUtility.TrTextContent("Load", "Move the camera to the saved position and restore the saved lens and the camera body.");
            public static string PreviewNotAvailable = L10n.Tr("Preview not available.");

            static GUIStyle s_CenteredLabel;
            public static GUIStyle CenteredLabel
            {
                get
                {
                    if (s_CenteredLabel == null)
                    {
                        s_CenteredLabel = new GUIStyle(GUI.skin.label)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            wordWrap = true
                        };
                    }
                    return s_CenteredLabel;
                }
            }
        }

        VirtualCameraDevice m_Device;
        SerializedProperty m_Actor;
        SerializedProperty m_LiveLinkChannels;
        SerializedProperty m_Lens;
        SerializedProperty m_LensAsset;
        SerializedProperty m_LensIntrinsics;
        SerializedProperty m_CameraBody;
        SerializedProperty m_Settings;
        SerializedProperty m_Snapshots;
        CompactList m_List;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_Device = target as VirtualCameraDevice;

            m_Actor = serializedObject.FindProperty("m_Actor");
            m_LiveLinkChannels = serializedObject.FindProperty("m_Channels");
            m_LensAsset = serializedObject.FindProperty("m_LensAsset");
            m_Lens = serializedObject.FindProperty("m_Lens");
            m_LensIntrinsics = serializedObject.FindProperty("m_LensIntrinsics");
            m_CameraBody = serializedObject.FindProperty("m_CameraBody");
            m_Settings = serializedObject.FindProperty("m_Settings");
            m_Snapshots = serializedObject.FindProperty("m_Snapshots");

            CreateList();
        }

        protected override void OnDeviceGUI()
        {
            DoClientGUI();

            serializedObject.Update();

            DoActorGUI(m_Actor);
            if (m_Actor.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(Contents.ActorWarningMessage, MessageType.Warning);
                DoActorCreateGUI(m_Actor, k_ActorCreateMenuItems);
            }

            DoLiveLinkChannelsGUI(m_LiveLinkChannels);
            DoLensAssetField();

            if (GUILayout.Button(Contents.VideoSettingsButton))
            {
                VideoServerSettingsProvider.Open();
            }

            LensDrawerUtility.DoLensGUI(m_Lens, m_LensIntrinsics);

            EditorGUILayout.PropertyField(m_CameraBody, Contents.CameraBody);
            EditorGUILayout.PropertyField(m_Settings, Contents.Settings);

            DoSnapshotsGUI();

            serializedObject.ApplyModifiedProperties();

#if URP_10_2_OR_NEWER
            if (m_Device.Settings.FocusPlane)
            {
                RenderFeatureEditor<FocusPlaneRenderer, VirtualCameraScriptableRenderFeature>.OnInspectorGUI();
            }

            if (m_Device.Settings.GateMask ||
                m_Device.Settings.AspectRatioLines ||
                m_Device.Settings.CenterMarker)
            {
                RenderFeatureEditor<FrameLines, VirtualCameraScriptableRenderFeature>.OnInspectorGUI();
            }
#endif
        }

        void CreateList()
        {
            m_List = new CompactList(m_Snapshots);
            m_List.OnCanAddCallback = () => false;
            m_List.OnCanRemoveCallback = () => true;
            m_List.DrawElementCallback = null;
            m_List.Reorderable = true;
            m_List.ItemHeight = k_SnapshotElementHeight;
            m_List.ShowSearchBar = false;
            m_List.DrawElementCallback = (r, i) => {};
            m_List.ElementHeightCallback = (i) => 0f;
            m_List.DrawListItemCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = m_Snapshots.GetArrayElementAtIndex(index);
                var screenshot = element.FindPropertyRelative("m_Screenshot").objectReferenceValue as Texture2D;
                var slate = element.FindPropertyRelative("m_Slate").objectReferenceValue as ISlate;
                var frameRate = new FrameRate(
                    element.FindPropertyRelative("m_FrameRate.m_Numerator").intValue,
                    element.FindPropertyRelative("m_FrameRate.m_Denominator").intValue,
                    element.FindPropertyRelative("m_FrameRate.m_IsDropFrame").boolValue);
                var time = element.FindPropertyRelative("m_Time").doubleValue;
                var focalLength = element.FindPropertyRelative("m_Lens.m_FocalLength").floatValue;
                var aperture = element.FindPropertyRelative("m_Lens.m_Aperture").floatValue;
                var lensAsset = element.FindPropertyRelative("m_LensAsset").objectReferenceValue;
                var sensorSize = element.FindPropertyRelative("m_CameraBody.m_SensorSize").vector2Value;
                var previewRect = new Rect(rect.x, rect.y + 2.5f, k_PreviewWidth, rect.height - 5f);
                var propertiesRect = new Rect(previewRect.xMax + 5f, rect.y + 2f, rect.width - previewRect.width, rect.height);

                if (screenshot != null)
                {
                    var preview = AssetPreview.GetAssetPreview(screenshot);

                    if (preview != null)
                    {
                        var textureRect = BestFit(previewRect, preview);

                        EditorGUI.DrawPreviewTexture(textureRect, preview);
                    }
                }
                else
                {
                    EditorGUI.LabelField(previewRect, Contents.PreviewNotAvailable, Contents.CenteredLabel);
                }

                var rowHeight = EditorGUIUtility.singleLineHeight;
                var yOffset = rowHeight + 2f;
                var rowRect1 = new Rect(propertiesRect.x, propertiesRect.y, propertiesRect.width, rowHeight);
                var rowRect2 = new Rect(propertiesRect.x, propertiesRect.y + yOffset, propertiesRect.width, rowHeight);
                var rowRect3 = new Rect(propertiesRect.x, propertiesRect.y + yOffset * 2f, propertiesRect.width, rowHeight);
                var rowRect4 = new Rect(propertiesRect.x, propertiesRect.y + yOffset * 3f, propertiesRect.width, rowHeight);
                var slateField = string.Empty;
                var timecode = Timecode.FromSeconds(frameRate, time);

                if (slate != null)
                {
                    slateField = $"[{slate.SceneNumber.ToString("D3")}] {slate.ShotName} TC [{timecode}]";
                }
                else
                {
                    slateField = $"TC [{timecode}]";
                }

                var lensAssetField = string.Empty;

                if (lensAsset != null)
                {
                    lensAssetField = $"Lens: {lensAsset.name}";
                }
                else
                {
                    lensAssetField = $"Lens: {Contents.Deleted}";
                }

                var lensField = $"{focalLength.ToString("F1")}mm f/{aperture}";
                var sensorField = GetSensorName(sensorSize);

                EditorGUI.LabelField(rowRect1, slateField);
                EditorGUI.LabelField(rowRect2, lensAssetField);
                EditorGUI.LabelField(rowRect3, lensField);
                EditorGUI.LabelField(rowRect4, sensorField);
            };
            m_List.OnRemoveCallback = () =>
            {
                m_Device.DeleteSnapshot(m_List.Index);
            };
        }

        static VirtualCameraActor CreateVirtualCameraActor()
        {
            return GetNewActor(VirtualCameraCreatorUtilities.CreateVirtualCameraActor());
        }

#if VP_CINEMACHINE_2_4_0
        static VirtualCameraActor CreateCinemachineCameraActor()
        {
            return GetNewActor(VirtualCameraCreatorUtilities.CreateCinemachineCameraActor());
        }

#endif

        static VirtualCameraActor GetNewActor(GameObject newGameObject)
        {
            EditorGUIUtility.PingObject(newGameObject);

            var newActor = newGameObject.GetComponent<VirtualCameraActor>();
            Debug.Assert(newActor != null);
            return newActor;
        }

        string GetSensorName(Vector2 sensorSize)
        {
            var sensorSizes = SensorPresetsCache.GetSensorSizes();
            var index = Array.FindIndex(sensorSizes, (s) => s == sensorSize);

            if (index == -1)
            {
                return $"[{sensorSize.x} x {sensorSize.y}]";
            }

            var options = SensorPresetsCache.GetSensorNames();

            return options[index];
        }

        Rect BestFit(Rect rect, Texture2D texture)
        {
            var rectAspect = rect.width / rect.height;
            var textureAspect = texture.width / texture.height;

            if (textureAspect > rectAspect)
            {
                var height = rect.width / textureAspect;

                rect.y += (rect.height - height) * 0.5f;
                rect.height = height;
            }
            else if (textureAspect < rectAspect)
            {
                var width = rect.height * textureAspect;

                rect.x += (rect.width - width) * 0.5f;
                rect.width = width;
            }

            return rect;
        }

        void DoSnapshotsGUI()
        {
            var rect = EditorGUILayout.GetControlRect();
            var label = EditorGUI.BeginProperty(rect, Contents.Snapshots, m_Snapshots);
            m_Snapshots.isExpanded = EditorGUI.Foldout(rect, m_Snapshots.isExpanded, label, true);

            if (!m_Snapshots.isExpanded)
            {
                return;
            }

            m_List.DoGUILayout();

            using (new EditorGUILayout.HorizontalScope())
            using (new EditorGUI.DisabledScope(m_Device.IsRecording()))
            {
                using (new EditorGUI.DisabledScope(!m_Device.IsLiveActive()))
                {
                    if (GUILayout.Button(Contents.TakeSnapshot))
                    {
                        m_Device.TakeSnapshot();

                        EditorUtility.SetDirty(m_Device);
                    }
                }

                if (GUILayout.Button(Contents.GotoLabel))
                {
                    m_Device.GoToSnapshot(m_List.Index);

                    EditorUtility.SetDirty(m_Device);
                }

                if (GUILayout.Button(Contents.Load))
                {
                    m_Device.LoadSnapshot(m_List.Index);

                    EditorUtility.SetDirty(m_Device);
                }
            }

            EditorGUI.EndProperty();
        }

        void DoLensAssetField()
        {
            LensKitCache.Get(out var presets, out var contents);

            var asset = m_LensAsset.objectReferenceValue;
            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            var label = EditorGUI.BeginProperty(rect, Contents.LensAssetLabel, m_LensAsset);
            EditorGUI.LabelField(rect, label);

            rect.x += EditorGUIUtility.labelWidth + 5;
            rect.width -= EditorGUIUtility.labelWidth + 5;

            var name = Contents.None;

            if (asset != null)
            {
                name = new GUIContent(asset.name);
            }

            if (GUI.Button(rect, name, EditorStyles.popup))
            {
                var menu = new GenericMenu();

                for (var i = 0; i < contents.Length; ++i)
                {
                    menu.AddItem(contents[i], presets[i] == asset, (p) => SetLensPreset(p as LensAsset), presets[i]);
                }

                menu.ShowAsContext();
            }

            EditorGUI.EndProperty();
        }

        void SetLensPreset(LensAsset preset)
        {
            serializedObject.Update();

            m_LensAsset.objectReferenceValue = preset;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
