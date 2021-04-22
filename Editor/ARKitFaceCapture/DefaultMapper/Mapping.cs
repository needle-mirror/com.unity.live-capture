using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture.ARKitFaceCapture.DefaultMapper
{
    abstract class Mapping : IDrawable
    {
        protected static class Contents
        {
            public static readonly GUIContent faceShape = new GUIContent("Blend Shape", "The ARKit blend shape to influence this mesh blend shape with.");
            public static readonly GUIContent meshShape = new GUIContent("Blend Shape", "The blend shape in the target mesh to influence.");

            public static readonly float locationWidth = 160f;
            public static readonly float buttonWidth = 18f;
            public static readonly float bindingSpacing = 6f * EditorGUIUtility.standardVerticalSpacing;
            public static readonly float bindingSeparatorHeight = 2f;
            public static readonly Vector2 optionDropdownSize = new Vector2(300f, 250f);

            public static readonly Color configBackground1 = new Color(0f, 0f, 0f, 0.04f);
            public static readonly Color configBackground2 = new Color(1f, 1f, 1f, 0.04f);
            public static readonly Color separatorColor = new Color(0f, 0f, 0f, 0.1f);
            public static readonly GUIContent iconToolbarPlus = EditorGUIUtility.TrIconContent("Toolbar Plus", "Add mapping.");
            public static readonly GUIContent iconToolbarMinus = EditorGUIUtility.TrIconContent("Toolbar Minus", "Remove mapping.");
        }

        protected bool m_IsExpanded;
        protected readonly MappingList m_MappingList;

        /// <summary>
        /// The number of bindings under this mapping.
        /// </summary>
        protected abstract int bindingCount { get; }

        protected Mapping(MappingList mappingList)
        {
            m_MappingList = mappingList;
        }

        /// <summary>
        /// Hide the bindings for this mapping in the inspector.
        /// </summary>
        public void Collapse()
        {
            if (m_IsExpanded)
            {
                m_IsExpanded = false;
                GUI.changed = true;
            }
        }

        /// <inheritdoc />
        public float GetHeight()
        {
            var height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (m_IsExpanded)
            {
                for (var i = 0; i < bindingCount; i++)
                {
                    height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    height += GetConfig(i).GetHeight() + Contents.bindingSpacing;
                }
            }

            return height;
        }

        /// <inheritdoc />
        public void OnGUI(Rect rect)
        {
            var pos = new Rect(rect)
            {
                height = EditorGUIUtility.singleLineHeight,
            };

            var locationRect = new Rect(pos)
            {
                width = Contents.locationWidth,
            };
            var foldoutRect = new Rect(pos)
            {
                xMin = locationRect.xMax,
                xMax = pos.xMax - Contents.buttonWidth,
            };
            var buttonRect = new Rect(pos)
            {
                xMin = foldoutRect.xMax,
            };

            OnMappingValueGUI(locationRect);
            OnFoldoutGUI(foldoutRect);
            OnAddBindingGUI(buttonRect);

            if (m_IsExpanded)
            {
                for (var i = 0; i < bindingCount; i++)
                {
                    var config = GetConfig(i);

                    pos.y = pos.yMax;

                    var separatorRect = new Rect
                    {
                        xMin = pos.xMin,
                        xMax = pos.xMax,
                        y = pos.y - Contents.bindingSeparatorHeight / 2f,
                        height = Contents.bindingSeparatorHeight,
                    };
                    var bindingRect = new Rect
                    {
                        xMin = pos.xMin,
                        xMax = pos.xMax - 20f,
                        y = pos.y + (Contents.bindingSpacing / 2f),
                        height = EditorGUIUtility.singleLineHeight,
                    };
                    var configRect = new Rect
                    {
                        xMin = pos.xMin,
                        xMax = pos.xMax - 20f,
                        y = bindingRect.yMax + EditorGUIUtility.standardVerticalSpacing,
                        height = config.GetHeight(),
                    };
                    buttonRect.y = bindingRect.y;

                    pos.yMax = configRect.yMax + (Contents.bindingSpacing / 2f);

                    // draw the shape config background with alternating colors
                    if (Event.current.type == EventType.Repaint)
                    {
                        EditorGUI.DrawRect(pos, i % 2 == 0 ? Contents.configBackground1 : Contents.configBackground2);
                        EditorGUI.DrawRect(separatorRect, Contents.separatorColor);
                    }

                    OnBindingGUI(bindingRect, i);
                    config.OnGUI(configRect);

                    if (GUI.Button(buttonRect, Contents.iconToolbarMinus, GUIStyle.none))
                    {
                        RemoveBinding(i);
                    }
                }
            }
        }

        void OnFoldoutGUI(Rect rect)
        {
            string text;
            switch (bindingCount)
            {
                case 0:
                    text = string.Empty;
                    break;
                case 1:
                    text = $"{bindingCount} binding";
                    break;
                default:
                    text = $"{bindingCount} bindings";
                    break;
            }

            var foldoutLabel = new GUIContent(text);

            if (bindingCount == 0)
            {
                EditorGUI.LabelField(rect, foldoutLabel);
            }
            else
            {
                // foldouts don't clip the label, so we do it manually
                using (new GUI.GroupScope(rect))
                {
                    m_IsExpanded = EditorGUI.Foldout(new Rect(0, 0, rect.width, rect.height), m_IsExpanded, foldoutLabel, true);
                }
            }
        }

        protected void DrawLocation(Rect rect, bool drawLabel, FaceBlendShape location, List<FaceBlendShape> unusedLocations, Action<FaceBlendShape> onSelect)
        {
            if (drawLabel)
            {
                rect = EditorGUI.PrefixLabel(rect, Contents.faceShape);
            }

            var current = location.ToString();
            var content = new GUIContent(current, Contents.faceShape.tooltip);

            if (GUI.Button(rect, content, EditorStyles.popup))
            {
                var options = unusedLocations
                    .Select(l => new GUIContent(l.ToString()))
                    .ToArray();

                OptionSelectWindow.SelectOption(rect, Contents.optionDropdownSize, options, (index, value) =>
                {
                    onSelect.Invoke(BlendShapeUtility.GetLocation(value));
                    m_MappingList.ApplyToProperties();
                });
            }
        }

        protected void DrawShapeIndex(Rect rect, bool drawLabel, int shapeIndex, List<int> unusedShapeIndices, Action<int> onSelect)
        {
            var content = m_MappingList.GetShapeName(shapeIndex);

            if (content == null)
            {
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    shapeIndex = EditorGUI.IntField(rect, drawLabel ? Contents.meshShape : GUIContent.none, shapeIndex);

                    if (change.changed)
                        onSelect?.Invoke(shapeIndex);
                }
            }
            else
            {
                if (drawLabel)
                {
                    rect = EditorGUI.PrefixLabel(rect, Contents.meshShape);
                }

                if (GUI.Button(rect, new GUIContent(content.text, Contents.meshShape.tooltip), EditorStyles.popup))
                {
                    var options = unusedShapeIndices
                        .Select(i => m_MappingList.GetShapeName(i))
                        .ToArray();

                    OptionSelectWindow.SelectOption(rect, Contents.optionDropdownSize, options, (index, value) =>
                    {
                        onSelect.Invoke(m_MappingList.GetShapeIndex(value));
                        m_MappingList.ApplyToProperties();
                    });
                }
            }
        }

        /// <summary>
        /// Gets the bindings defined under this mapping.
        /// </summary>
        public abstract Binding[] GetBindings();

        protected abstract BindingConfig GetConfig(int index);
        protected abstract void RemoveBinding(int index);
        protected abstract void OnMappingValueGUI(Rect rect);
        protected abstract void OnAddBindingGUI(Rect rect);
        protected abstract void OnBindingGUI(Rect rect, int index);
    }

    class LocationMapping : Mapping
    {
        class ShapeIndexBinding
        {
            public int shapeIndex;
            public BindingConfig config;
        }

        FaceBlendShape m_Location;
        readonly List<ShapeIndexBinding> m_Bindings;
        readonly List<int> m_UnusedShapeIndices = new List<int>();

        public FaceBlendShape location => m_Location;

        /// <inheritdoc />
        protected override int bindingCount => m_Bindings.Count;

        public LocationMapping(MappingList mappingList, FaceBlendShape location) : base(mappingList)
        {
            m_Location = location;
            m_Bindings = new List<ShapeIndexBinding>();
            m_IsExpanded = true;

            RefreshUnusedShapeIndices();
        }

        public LocationMapping(MappingList mappingList, FaceBlendShape location, IEnumerable<(int shapeIndex, BindingConfig config, bool isExpanded)> bindings) : base(mappingList)
        {
            m_Location = location;
            m_Bindings = bindings.Select(binding => new ShapeIndexBinding
            {
                shapeIndex = binding.shapeIndex,
                config = binding.config,
            })
                .ToList();
            m_IsExpanded = bindings.Any(binding => binding.isExpanded);

            RefreshUnusedShapeIndices();
        }

        /// <inheritdoc />
        public override Binding[] GetBindings()
        {
            return m_Bindings
                .Select(b => new Binding(m_Location, b.shapeIndex, b.config, m_IsExpanded))
                .ToArray();
        }

        /// <inheritdoc />
        protected override BindingConfig GetConfig(int index)
        {
            return m_Bindings[index].config;
        }

        /// <inheritdoc />
        protected override void RemoveBinding(int index)
        {
            m_Bindings.RemoveAt(index);
            RefreshUnusedShapeIndices();
        }

        /// <inheritdoc />
        protected override void OnMappingValueGUI(Rect rect)
        {
            DrawLocation(rect, false, m_Location, m_MappingList.unusedLocations, (value) =>
            {
                m_Location = value;
                m_MappingList.RefreshUnusedMappings();
            });
        }

        /// <inheritdoc />
        protected override void OnAddBindingGUI(Rect rect)
        {
            using (new EditorGUI.DisabledScope(m_UnusedShapeIndices.Count == 0))
            {
                if (GUI.Button(rect, Contents.iconToolbarPlus, GUIStyle.none))
                {
                    var unusedShape = m_UnusedShapeIndices[0];

                    m_Bindings.Add(new ShapeIndexBinding
                    {
                        shapeIndex = unusedShape,
                        config = new BindingConfig(m_MappingList.defaultPreset),
                    });

                    RefreshUnusedShapeIndices();
                }
            }
        }

        /// <inheritdoc />
        protected override void OnBindingGUI(Rect rect, int index)
        {
            var binding = m_Bindings[index];

            DrawShapeIndex(rect, true, binding.shapeIndex, m_UnusedShapeIndices, (value) =>
            {
                binding.shapeIndex = value;
                RefreshUnusedShapeIndices();
            });
        }

        void RefreshUnusedShapeIndices()
        {
            m_UnusedShapeIndices.Clear();

            for (var i = 0; i < m_MappingList.meshBlendShapeCount; i++)
            {
                if (!m_Bindings.Any(b => b.shapeIndex == i))
                {
                    m_UnusedShapeIndices.Add(i);
                }
            }
        }
    }

    class ShapeIndexMapping : Mapping
    {
        class LocationBinding
        {
            public FaceBlendShape location;
            public BindingConfig config;
        }

        int m_ShapeIndex;
        readonly List<LocationBinding> m_Bindings;
        readonly List<FaceBlendShape> m_UnusedLocations = new List<FaceBlendShape>();

        public int shapeIndex => m_ShapeIndex;

        /// <inheritdoc />
        protected override int bindingCount => m_Bindings.Count;

        public ShapeIndexMapping(MappingList mappingList, int shapeIndex) : base(mappingList)
        {
            m_ShapeIndex = shapeIndex;
            m_Bindings = new List<LocationBinding>();
            m_IsExpanded = true;

            RefreshUnusedLocations();
        }

        public ShapeIndexMapping(MappingList mappingList, int shapeIndex, IEnumerable<(FaceBlendShape location, BindingConfig config, bool isExpanded)> bindings) : base(mappingList)
        {
            m_ShapeIndex = shapeIndex;
            m_Bindings = bindings.Select(binding => new LocationBinding
            {
                location = binding.location,
                config = binding.config,
            })
                .ToList();
            m_IsExpanded = bindings.Any(binding => binding.isExpanded);

            RefreshUnusedLocations();
        }

        /// <inheritdoc />
        public override Binding[] GetBindings()
        {
            return m_Bindings
                .Select(b => new Binding(b.location, m_ShapeIndex, b.config, m_IsExpanded))
                .ToArray();
        }

        /// <inheritdoc />
        protected override BindingConfig GetConfig(int index)
        {
            return m_Bindings[index].config;
        }

        /// <inheritdoc />
        protected override void RemoveBinding(int index)
        {
            m_Bindings.RemoveAt(index);
            RefreshUnusedLocations();
        }

        /// <inheritdoc />
        protected override void OnMappingValueGUI(Rect rect)
        {
            DrawShapeIndex(rect, false, m_ShapeIndex, m_MappingList.unusedShapeIndices, (value) =>
            {
                m_ShapeIndex = value;
                m_MappingList.RefreshUnusedMappings();
            });
        }

        /// <inheritdoc />
        protected override void OnAddBindingGUI(Rect rect)
        {
            using (new EditorGUI.DisabledScope(m_UnusedLocations.Count == 0))
            {
                if (GUI.Button(rect, Contents.iconToolbarPlus, GUIStyle.none))
                {
                    var unusedLocation = m_UnusedLocations[0];

                    m_Bindings.Add(new LocationBinding
                    {
                        location = unusedLocation,
                        config = new BindingConfig(m_MappingList.defaultPreset),
                    });

                    RefreshUnusedLocations();
                }
            }
        }

        /// <inheritdoc />
        protected override void OnBindingGUI(Rect rect, int index)
        {
            var binding = m_Bindings[index];

            DrawLocation(rect, true, binding.location, m_UnusedLocations, (value) =>
            {
                binding.location = value;
                RefreshUnusedLocations();
            });
        }

        void RefreshUnusedLocations()
        {
            m_UnusedLocations.Clear();

            foreach (var location in FaceBlendShapePose.shapes)
            {
                if (!m_Bindings.Any(b => b.location == location))
                {
                    m_UnusedLocations.Add(location);
                }
            }
        }
    }
}
