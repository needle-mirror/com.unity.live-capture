using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.LiveCapture.ARKitFaceCapture.DefaultMapper
{
    [CustomEditor(typeof(DefaultFaceMapper))]
    class DefaultFaceMapperEditor : Editor
    {
        static class Contents
        {
            public static readonly GUILayoutOption[] buttonOptions =
            {
                GUILayout.Width(230f),
                GUILayout.Height(24f),
            };

            public static readonly GUIContent eyesHeader = new GUIContent("Eyes");
            public static readonly GUIContent headHeader = new GUIContent("Head");
            public static readonly GUIContent blendShapesHeader = new GUIContent("Blend Shapes");
            public static readonly GUIContent addRenderer = new GUIContent("Add Renderer", "Add a renderer to control its blend shapes using face capture.");
            public static readonly GUIContent addEmpty = new GUIContent("Empty");
            public static readonly GUIStyle rendererPathHeaderStyle = "RL FooterButton";
            public static readonly GUIContent menuIcon = EditorGUIUtility.TrIconContent("_Menu");
            public static readonly GUIContent reinitialize = new GUIContent("Initialize", "Clear the mappings and initialize from the currently assigned rig prefab.");
            public static readonly GUIContent deleteMappings = new GUIContent("Delete", "Delete the mappings for this renderer.");
            public static readonly float menuButtonWidth = 16f;
            public static readonly GUIContent editPathButtonIcon = EditorGUIUtility.TrIconContent("d_UnityEditor.SceneHierarchyWindow", "Enables changing the renderer for these mappings.");
            public static readonly float editPathButtonWidth = 20f;
            public static readonly GUIContent mappingDirection = new GUIContent("Mapping Direction", "Whether mappings should be driver-to-blend shape or blend shape-to-driver.");
        }

        enum MappingDirection
        {
            DriverToBlendShape = 0,
            BlendShapeToDriver = 1
        }

        static ReorderableList.Defaults s_ListDefaults;

        readonly Dictionary<string, MappingList> m_MappingLists = new Dictionary<string, MappingList>();

        SerializedProperty m_RigPrefab;
        SerializedProperty m_ShapeMatchTolerance;
        SerializedProperty m_DefaultEvaluator;
        SerializedProperty m_InvertMappings;

        SerializedProperty m_Maps;
        SerializedProperty m_LeftEye;
        SerializedProperty m_RightEye;
        SerializedProperty m_EyeMovementDriver;
        SerializedProperty m_EyeAngleRange;
        SerializedProperty m_EyeAngleOffset;
        SerializedProperty m_EyeSmoothing;
        SerializedProperty m_Head;
        SerializedProperty m_HeadSmoothing;

        MappingDirection m_MappingDirection;

        void OnEnable()
        {
            m_RigPrefab = serializedObject.FindProperty("m_RigPrefab");
            m_ShapeMatchTolerance = serializedObject.FindProperty("m_ShapeMatchTolerance");
            m_DefaultEvaluator = serializedObject.FindProperty("m_DefaultEvaluator");
            m_InvertMappings = serializedObject.FindProperty("m_InvertMappings");

            m_MappingDirection = m_InvertMappings.boolValue ? MappingDirection.BlendShapeToDriver : MappingDirection.DriverToBlendShape;

            m_Maps = serializedObject.FindProperty("m_Maps");
            m_LeftEye = serializedObject.FindProperty("m_LeftEye");
            m_RightEye = serializedObject.FindProperty("m_RightEye");
            m_EyeMovementDriver = serializedObject.FindProperty("m_EyeMovementDriver");
            m_EyeAngleRange = serializedObject.FindProperty("m_EyeAngleRange");
            m_EyeAngleOffset = serializedObject.FindProperty("m_EyeAngleOffset");
            m_EyeSmoothing = serializedObject.FindProperty("m_EyeSmoothing");

            m_Head = serializedObject.FindProperty("m_Head");
            m_HeadSmoothing = serializedObject.FindProperty("m_HeadSmoothing");

            m_MappingLists.Clear();

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        void OnUndoRedoPerformed()
        {
            m_MappingLists.Clear();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_RigPrefab);

            var prefab = m_RigPrefab.objectReferenceValue as GameObject;
            var mappingChanged = false;

            // only allow configuring the mapping once a valid rig prefab is assigned
            if (CheckPrefabValid(prefab, true))
            {
                var actor = prefab.GetComponentInChildren<FaceActor>(true);

                using (var changeCheck = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField(Contents.eyesHeader, EditorStyles.boldLabel);

                    DoEyeGUI(actor);

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField(Contents.headHeader, EditorStyles.boldLabel);

                    DoHeadGUI(actor);

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField(Contents.blendShapesHeader, EditorStyles.boldLabel);

                    DoRenderersGUI(actor);

                    mappingChanged |= changeCheck.changed;
                }
            }

            serializedObject.ApplyModifiedProperties();

            // when the configuration has changed, any cached data for this mapping must be invalidated
            if (mappingChanged)
            {
                foreach (var actor in FindObjectsOfType<FaceActor>())
                {
                    if (actor.mapper == target)
                        actor.ClearCache();
                }
            }
        }

        void DoRenderersGUI(FaceActor actor)
        {
            Profiler.BeginSample($"{nameof(DefaultFaceMapperEditor)}.{nameof(DoRenderersGUI)}()");

            using (new EditorGUI.IndentLevelScope())
            {
                // when changing the mapping options we need to invalidate the renderer display states
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUILayout.PropertyField(m_ShapeMatchTolerance);
                    EditorGUILayout.PropertyField(m_DefaultEvaluator);

                    m_MappingDirection = (MappingDirection)EditorGUILayout.EnumPopup(Contents.mappingDirection, m_MappingDirection);
                    m_InvertMappings.boolValue = (int)m_MappingDirection == 1;

                    if (change.changed)
                        m_MappingLists.Clear();
                }

                for (var i = 0; i < m_Maps.arraySize; ++i)
                {
                    var rendererMapping = m_Maps.GetArrayElementAtIndex(i);
                    DrawRendererMapping(actor, rendererMapping, i);
                }
            }

            DrawAddRendererButton(actor);

            Profiler.EndSample();
        }

        void DrawAddRendererButton(FaceActor actor)
        {
            EditorGUILayout.Space();
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(Contents.addRenderer, Contents.buttonOptions))
                {
                    var menu = new GenericMenu();

                    var skinnedMeshRenderers = actor.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                        .Where(r => r.sharedMesh != null && r.sharedMesh.blendShapeCount > 0 && !IsRendererUsed(actor, r))
                        .ToArray();

                    foreach (var renderer in skinnedMeshRenderers)
                    {
                        menu.AddItem(new GUIContent(renderer.name), false, () =>
                        {
                            AddRendererMapping(actor, renderer);
                        });
                    }

                    if (skinnedMeshRenderers.Length > 0)
                        menu.AddSeparator(string.Empty);

                    menu.AddItem(Contents.addEmpty, false, () =>
                    {
                        AddRendererMapping(actor, null);
                    });

                    menu.ShowAsContext();
                }
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.Space();
        }

        void AddRendererMapping(FaceActor actor, SkinnedMeshRenderer renderer)
        {
            var rendererMapping = m_Maps.GetArrayElementAtIndex(m_Maps.arraySize++);

            if (renderer != null)
            {
                var path = AnimationUtility.CalculateTransformPath(renderer.transform, actor.transform);
                Initialize(rendererMapping, path, renderer.sharedMesh);
            }
            else
            {
                Initialize(rendererMapping, string.Empty, null);
            }

            SetIsEditingPath(rendererMapping.FindPropertyRelative("m_Path"), renderer == null);

            serializedObject.ApplyModifiedProperties();
        }

        void DrawRendererMapping(FaceActor actor, SerializedProperty rendererMapping, int index)
        {
            EditorGUILayout.Space();

            var path = rendererMapping.FindPropertyRelative("m_Path");

            // draw the list header
            var headerRect = GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true));

            if (s_ListDefaults == null)
                s_ListDefaults = new ReorderableList.Defaults();

            s_ListDefaults.DrawHeaderBackground(headerRect);

            // draw the dropdown menu
            var menuButtonRect = new Rect(headerRect)
            {
                xMin = headerRect.xMax - 2f - Contents.menuButtonWidth,
                xMax = headerRect.xMax - 2f,
                y = headerRect.y + 2f,
            };
            if (GUI.Button(menuButtonRect, Contents.menuIcon, Contents.rendererPathHeaderStyle))
            {
                var menu = new GenericMenu();

                if (TryGetMesh(actor, rendererMapping, out var m, false))
                {
                    menu.AddItem(Contents.reinitialize, false, () =>
                    {
                        Initialize(rendererMapping, path.stringValue, m);
                        serializedObject.ApplyModifiedProperties();
                    });
                }
                else
                {
                    menu.AddDisabledItem(Contents.reinitialize, false);
                }

                menu.AddItem(Contents.deleteMappings, false, () =>
                {
                    m_MappingLists.Clear();
                    SetIsEditingPath(path, false);

                    m_Maps.DeleteArrayElementAtIndex(index);
                    serializedObject.ApplyModifiedProperties();
                });

                menu.ShowAsContext();
            }

            // let the user toggle if the assigned renderer is editable
            var isEditingPath = GetIsEditingPath(path);

            var editPathButtonRect = new Rect(headerRect)
            {
                xMin = menuButtonRect.xMin - 2f - Contents.editPathButtonWidth,
                xMax = menuButtonRect.xMin - 2f,
                y = headerRect.y + 2f,
            };
            using (new EditorGUI.DisabledScope(!TryGetMesh(actor, rendererMapping, out _, false)))
            {
                if (GUI.Button(editPathButtonRect, Contents.editPathButtonIcon, Contents.rendererPathHeaderStyle))
                {
                    isEditingPath = !isEditingPath;
                    SetIsEditingPath(path, isEditingPath);
                }
            }

            // draw the foldout that shows the mappings list
            var pathRect = new Rect(headerRect)
            {
                xMax = editPathButtonRect.xMin
            };

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                // when the path input field is shown we need to prevent the foldout from using the GUI events
                var foldoutRect = pathRect;
                if (isEditingPath)
                {
                    foldoutRect.width = 20f;
                }

                if (TryGetMappingList(actor, rendererMapping, out var list))
                {
                    var rendererName = Path.GetFileName(path.stringValue);
                    var expanded = EditorGUI.Foldout(foldoutRect, list.isExpanded, rendererName, true);

                    if (change.changed)
                        list.isExpanded = expanded;
                }
            }

            if (isEditingPath)
            {
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    PickComponent<SkinnedMeshRenderer>(actor, pathRect, path, GUIContent.none, (p, r) =>
                    {
                        // don't allow assigning the same renderer to different mappings
                        var isDuplicate = false;
                        for (var i = 0; i < m_Maps.arraySize; i++)
                        {
                            var mapping = m_Maps.GetArrayElementAtIndex(i);

                            if (mapping.propertyPath != rendererMapping.propertyPath && p == mapping.FindPropertyRelative("m_Path").stringValue)
                            {
                                isDuplicate = true;
                                break;
                            }
                        }
                        return !isDuplicate;
                    });

                    // we need to recreate the lists since an assigned mesh may have changed
                    if (change.changed)
                        m_MappingLists.Clear();
                }
            }

            // check if the renderer and mesh is valid
            if (!TryGetMesh(actor, rendererMapping, out _, true))
            {
                SetIsEditingPath(path, true);
                return;
            }

            if (TryGetMappingList(actor, rendererMapping, out var mappingList))
            {
                if (!mappingList.isExpanded)
                    return;

                mappingList.OnGUI();
            }
        }

        bool IsRendererUsed(FaceActor actor, SkinnedMeshRenderer renderer)
        {
            for (var i = 0; i < m_Maps.arraySize; i++)
            {
                var mapping = m_Maps.GetArrayElementAtIndex(i);
                var path = mapping.FindPropertyRelative("m_Path");
                var transform = actor.transform.Find(path.stringValue);

                if (transform != null && transform.TryGetComponent<SkinnedMeshRenderer>(out var r) && r == renderer)
                    return true;
            }
            return false;
        }

        bool TryGetMappingList(FaceActor actor, SerializedProperty rendererMapping, out MappingList list)
        {
            if (!TryGetMesh(actor, rendererMapping, out var mesh, false))
            {
                list = null;
                return false;
            }

            var listID = $"{target.GetInstanceID()}/{rendererMapping.propertyPath}/{mesh.GetInstanceID()}";

            if (!m_MappingLists.TryGetValue(listID, out list))
            {
                var blendShapes = BlendShapeUtility.GetBlendShapeNames(mesh)
                    .Select(s => new GUIContent(s))
                    .ToArray();

                list = new MappingList(rendererMapping, m_DefaultEvaluator, m_InvertMappings.boolValue, blendShapes);
                m_MappingLists.Add(listID, list);
            }

            return true;
        }

        bool TryGetMesh(FaceActor actor, SerializedProperty rendererMapping, out Mesh mesh, bool showMessages)
        {
            var path = rendererMapping.FindPropertyRelative("m_Path");
            var transform = actor.transform.Find(path.stringValue);

            if (transform == null || !transform.TryGetComponent<SkinnedMeshRenderer>(out var renderer))
            {
                if (showMessages)
                    EditorGUILayout.HelpBox($"Assign a unique renderer from the rig prefab.", MessageType.Info);
                mesh = default;
                return false;
            }

            mesh = renderer.sharedMesh;

            if (mesh == null)
            {
                if (showMessages)
                    EditorGUILayout.HelpBox($"This renderer does not have a mesh assigned.", MessageType.Warning);
                return false;
            }
            if (mesh.blendShapeCount <= 0)
            {
                if (showMessages)
                    EditorGUILayout.HelpBox($"The mesh assigned to this renderer does not have any blend shapes.", MessageType.Warning);
                return false;
            }

            return true;
        }

        bool GetIsEditingPath(SerializedProperty path)
        {
            return SessionState.GetBool($"{target.GetInstanceID()}/{path.propertyPath}", false);
        }

        void SetIsEditingPath(SerializedProperty path, bool isEditingPath)
        {
            SessionState.SetBool($"{target.GetInstanceID()}/{path.propertyPath}", isEditingPath);
        }

        void DoEyeGUI(FaceActor actor)
        {
            PickComponent<Transform>(actor, m_LeftEye);
            PickComponent<Transform>(actor, m_RightEye);

            using (new EditorGUI.IndentLevelScope())
            {
                if (!string.IsNullOrEmpty(m_LeftEye.stringValue) || !string.IsNullOrEmpty(m_RightEye.stringValue))
                {
                    EditorGUILayout.PropertyField(m_EyeMovementDriver);

                    if (m_EyeMovementDriver.intValue == (int)DefaultFaceMapper.EyeMovementDriverType.BlendShapes)
                        EditorGUILayout.PropertyField(m_EyeAngleRange);

                    EditorGUILayout.PropertyField(m_EyeAngleOffset);
                    EditorGUILayout.PropertyField(m_EyeSmoothing);
                }
            }
        }

        void DoHeadGUI(FaceActor actor)
        {
            PickComponent<Transform>(actor, m_Head);

            using (new EditorGUI.IndentLevelScope())
            {
                if (!string.IsNullOrEmpty(m_Head.stringValue))
                {
                    EditorGUILayout.PropertyField(m_HeadSmoothing);
                }
            }
        }

        static bool CheckPrefabValid(GameObject prefab, bool showMessages)
        {
            if (prefab == null)
                return false;

            var isValid = true;
            var actors = prefab.GetComponentsInChildren<FaceActor>(true);

            if (actors.Length == 0)
            {
                isValid = false;

                if (showMessages)
                {
                    EditorGUILayout.HelpBox($"The rig does not contain a {nameof(FaceActor)} component! Add one to the rig to enable mapping.", MessageType.Error);
                }
            }
            else if (actors.Length > 1)
            {
                isValid = false;

                if (showMessages)
                {
                    var actorList = string.Join(", ", actors.Select(a => a.name));
                    EditorGUILayout.HelpBox($"Multiple {nameof(FaceActor)} components were found on the rig ({actorList})! The rig must contain only one.", MessageType.Error);
                }
            }

            if (!isValid)
                return false;

            var renderers = actors.First().GetComponentsInChildren<SkinnedMeshRenderer>(true);

            if (renderers.Length == 0 && showMessages)
            {
                EditorGUILayout.HelpBox($"The rig does not contain any {nameof(SkinnedMeshRenderer)} components under the {nameof(FaceActor)}, so blend shape mapping cannot be used.", MessageType.Warning);
            }
            else if (!renderers.Any(r => r.sharedMesh != null && r.sharedMesh.blendShapeCount > 0) && showMessages)
            {
                EditorGUILayout.HelpBox($"The rig's {nameof(SkinnedMeshRenderer)} components have not been assigned meshes with blend shapes, so blend shape mapping cannot be used.", MessageType.Warning);
            }

            return isValid;
        }

        void Initialize(SerializedProperty rendererMapping, string path, Mesh mesh)
        {
            m_MappingLists.Clear();

            rendererMapping.FindPropertyRelative("m_Path").stringValue = path;

            var bindings = rendererMapping.FindPropertyRelative("m_Bindings");
            bindings.arraySize = 0;

            if (mesh == null)
                return;

            var locationNames = FaceBlendShapePose.shapes.Select(s => s.ToString()).ToArray();
            var meshBlendShapes = BlendShapeUtility.GetBlendShapeNames(mesh);

            foreach (var(indexA, indexB) in BlendShapeUtility.FindMatches(locationNames, meshBlendShapes, m_ShapeMatchTolerance.floatValue))
            {
                var binding = bindings.GetArrayElementAtIndex(bindings.arraySize++);

                var location = BlendShapeUtility.GetLocation(locationNames[indexA]);
                var config = new BindingConfig(m_DefaultEvaluator.objectReferenceValue as EvaluatorPreset);

                binding.SetValue(new Binding(location, indexB, config, true));
            }
        }

        static void PickComponent<T>(FaceActor actor, SerializedProperty path) where T : Component
        {
            var rect = EditorGUILayout.GetControlRect();
            var content = new GUIContent(path.displayName, path.tooltip);
            PickComponent<T>(actor, rect, path, content);
        }

        static void PickComponent<T>(FaceActor actor, Rect rect, SerializedProperty path, GUIContent label, Func<string, T, bool> filter = null) where T : Component
        {
            var oldComponent = string.IsNullOrEmpty(path.stringValue) ? null : actor.transform.Find(path.stringValue);

            using (var prop = new EditorGUI.PropertyScope(rect, label, path))
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                var showLabel = label != null && label != GUIContent.none;

                using (new EditorGUI.IndentLevelScope())
                {
                    if (showLabel)
                    {
                        rect = EditorGUI.PrefixLabel(rect, prop.content);
                    }
                    else
                    {
                        // draw an invisible label so we can show the tooltip
                        EditorGUI.LabelField(rect, new GUIContent(string.Empty, prop.content.tooltip));
                    }
                }

                var component = EditorGUI.ObjectField(rect, oldComponent, typeof(T), true) as T;

                if (change.changed)
                {
                    // We want to allow picking from components in the scene so that the user doesn't need to specifically pick
                    // from components on the rig prefab instance (tis is generally not possible). To do this correctly, we
                    // must validate the path from the object picked in the scene matches a valid path of the prefab instance.
                    if (component != null)
                    {
                        var sceneActor = component.GetComponentInParent<FaceActor>();

                        if (sceneActor != null)
                        {
                            var transformPath = AnimationUtility.CalculateTransformPath(component.transform, sceneActor.transform);
                            var prefabTransform = actor.transform.Find(transformPath);

                            if (prefabTransform != null && prefabTransform.TryGetComponent<T>(out var prefabComponent) &&
                                (filter == null || filter(transformPath, prefabComponent)))
                            {
                                path.stringValue = transformPath;
                                return;
                            }
                        }
                    }

                    path.stringValue = string.Empty;
                }
            }
        }
    }
}
