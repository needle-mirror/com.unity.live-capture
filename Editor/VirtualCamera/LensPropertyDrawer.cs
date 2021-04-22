using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    [CustomPropertyDrawer(typeof(Lens))]
    class LensPropertyDrawer : PropertyDrawer
    {
        internal static class Contents
        {
            public static GUIContent apertureShapeLabel = EditorGUIUtility.TrTextContent("Aperture Shape");
            public static GUIContent focalLength = EditorGUIUtility.TrTextContent("Focal Length", "Focal distance in millimeters.");
            public static GUIContent focalLengthRange = EditorGUIUtility.TrTextContent("Range", "The minimum and maximum focal length.");
            public static GUIContent focusDistance = EditorGUIUtility.TrTextContent("Focus Distance", "Focus distance in world units.");
            public static GUIContent focusDistanceRange = EditorGUIUtility.TrTextContent("Range", "The minimum and maximum focus distance.");
            public static GUIContent aperture = EditorGUIUtility.TrTextContent("Aperture", "Aperture of the lens in f-number.");
            public static GUIContent apertureRange = EditorGUIUtility.TrTextContent("Range", "The minimum and maximum aperture.");
            public static GUIContent bladeCount = EditorGUIUtility.TrTextContent("Blade Count", "Number of diaphragm blades " +
                "the camera uses to form the aperture.");
            public static GUIContent curvature = EditorGUIUtility.TrTextContent("Curvature", "Maps an aperture range to blade " +
                "curvature. Aperture blades become more visible on bokeh at higher aperture values.");
            public static GUIContent anamorphism = EditorGUIUtility.TrTextContent("Anamorphism", "Stretch the sensor to " +
                "simulate an anamorphic look. Positive values distort the camera vertically, negative will distort the Camera horizontally.");
            public static GUIContent barrelClipping = EditorGUIUtility.TrTextContent("Barrel Clipping", "The strength of the " +
                "\"cat eye” effect. You can see this effect on bokeh as a result of lens shadowing (distortion along the edges of the frame).");
            public static GUIContent lensShift = EditorGUIUtility.TrTextContent("Shift", "The horizontal and vertical shift from the center.");
            public static readonly GUIContent rangeSelectLabel = EditorGUIUtility.TrTextContent("...", "Select a range");
        }

        static class Sections
        {
            public const string apertureShape = "apertureShape";
            public const string focalLengthRange = "focalLengthRange";
            public const string focusDistanceRange = "focusDistanceRange";
            public const string apertureRange = "apertureRange";
        }

        SerializedProperty m_FocalLengthProp;
        SerializedProperty m_FocalLengthRangeProp;
        SerializedProperty m_FocusDistanceProp;
        SerializedProperty m_FocusDistanceRangeProp;
        SerializedProperty m_ApertureProp;
        SerializedProperty m_ApertureRangeProp;
        SerializedProperty m_LensShiftProp;
        SerializedProperty m_BladeCountProp;
        SerializedProperty m_CurvatureProp;
        SerializedProperty m_BarrelClippingProp;
        SerializedProperty m_AnamorphismProp;
        Dictionary<string, string> m_Keys = new Dictionary<string, string>();

        string GetKey(SerializedProperty property, string section)
        {
            if (!m_Keys.TryGetValue(section, out var key))
            {
                key = $"{property.serializedObject.targetObject.GetInstanceID()}/{section}";
                m_Keys.Add(section, key);
            }

            return key;
        }

        bool IsExpanded(SerializedProperty property, string section)
        {
            return SessionState.GetBool(GetKey(property, section), false);
        }

        void SetExpanded(SerializedProperty property, string section, bool expanded)
        {
            SessionState.SetBool(GetKey(property, section), expanded);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var lineHeight = GetLineHeight() + 2f;
            var rowCount = 6;

            if (IsExpanded(property, Sections.focalLengthRange))
            {
                ++rowCount;
            }

            if (IsExpanded(property, Sections.focusDistanceRange))
            {
                ++rowCount;
            }

            if (IsExpanded(property, Sections.apertureRange))
            {
                ++rowCount;
            }

            if (IsExpanded(property, Sections.apertureShape))
            {
                rowCount += 4;
            }

            return lineHeight * rowCount - 2f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            m_FocalLengthProp = property.FindPropertyRelative("focalLength");
            m_FocalLengthRangeProp = property.FindPropertyRelative("focalLengthRange");
            m_FocusDistanceProp = property.FindPropertyRelative("focusDistance");
            m_FocusDistanceRangeProp = property.FindPropertyRelative("focusDistanceRange");
            m_ApertureProp = property.FindPropertyRelative("aperture");
            m_ApertureRangeProp = property.FindPropertyRelative("apertureRange");
            m_LensShiftProp = property.FindPropertyRelative("lensShift");
            m_BladeCountProp = property.FindPropertyRelative("bladeCount");
            m_CurvatureProp = property.FindPropertyRelative("curvature");
            m_BarrelClippingProp = property.FindPropertyRelative("barrelClipping");
            m_AnamorphismProp = property.FindPropertyRelative("anamorphism");

            EditorGUIUtility.wideMode = true;

            position = BeginLine(position);

            using (new EditorGUI.IndentLevelScope())
            {
                DoFocalLengthGUI(ref position, property);

                position = NextLine(position);

                DoFocusDistanceGUI(ref position, property);

                position = NextLine(position);

                DoApertureGUI(ref position, property);

                position = NextLine(position);

                DoLensShiftGUI(ref position, property);

                position = NextLine(position);

                DoApertureShapeGUI(ref position, property);
            }
        }

        void DoFocalLengthGUI(ref Rect position, SerializedProperty property)
        {
            var range = m_FocalLengthRangeProp.vector2Value;
            var bounds = LensParameterBounds.focalLength;
            var sliderRect = new Rect(position.x, position.y, position.width - 32f, position.height);
            var buttonRect = new Rect(position.x + position.width - 30f, position.y, 30f, position.height);

            using (new EditorGUI.DisabledScope(range.x == range.y))
            {
                EditorGUI.Slider(sliderRect, m_FocalLengthProp, range.x, range.y, Contents.focalLength);
            }

            var expanded = IsExpanded(property, Sections.focalLengthRange);

            if (GUI.Button(buttonRect, Contents.rangeSelectLabel, EditorStyles.miniButton))
            {
                SetExpanded(property, Sections.focalLengthRange, !expanded);
            }

            if (expanded)
            {
                position = NextLine(position);

                DoRangeGUI(position, Contents.focalLengthRange, m_FocalLengthRangeProp, bounds.x, bounds.y, true);
            }
        }

        void DoFocusDistanceGUI(ref Rect position, SerializedProperty property)
        {
            var range = m_FocusDistanceRangeProp.vector2Value;
            var bounds = LensParameterBounds.focusDistance;
            var sliderRect = new Rect(position.x, position.y, position.width - 32f, position.height);
            var buttonRect = new Rect(position.x + position.width - 30f, position.y, 30f, position.height);

            using (new EditorGUI.DisabledScope(range.x == range.y))
            {
                EditorGUI.Slider(sliderRect, m_FocusDistanceProp, range.x, range.y, Contents.focusDistance);
            }

            var expanded = IsExpanded(property, Sections.focusDistanceRange);

            if (GUI.Button(buttonRect, Contents.rangeSelectLabel, EditorStyles.miniButton))
            {
                SetExpanded(property, Sections.focusDistanceRange, !expanded);
            }

            if (expanded)
            {
                position = NextLine(position);

                DoRangeGUI(position, Contents.focusDistanceRange, m_FocusDistanceRangeProp, bounds.x, bounds.y, true);
            }
        }

        void DoApertureGUI(ref Rect position, SerializedProperty property)
        {
            var range = m_ApertureRangeProp.vector2Value;
            var bounds = LensParameterBounds.aperture;
            var sliderRect = new Rect(position.x, position.y, position.width - 32f, position.height);
            var buttonRect = new Rect(position.x + position.width - 30f, position.y, 30f, position.height);

            using (new EditorGUI.DisabledScope(range.x == range.y))
            {
                EditorGUI.Slider(sliderRect, m_ApertureProp, range.x, range.y, Contents.aperture);
            }

            var expanded = IsExpanded(property, Sections.apertureRange);

            if (GUI.Button(buttonRect, Contents.rangeSelectLabel, EditorStyles.miniButton))
            {
                SetExpanded(property, Sections.apertureRange, !expanded);
            }

            if (expanded)
            {
                position = NextLine(position);

                DoRangeGUI(position, Contents.apertureRange, m_ApertureRangeProp, bounds.x, bounds.y, true);
            }
        }

        void DoLensShiftGUI(ref Rect position, SerializedProperty property)
        {
            EditorGUI.PropertyField(position, m_LensShiftProp, Contents.lensShift);
        }

        void DoApertureShapeGUI(ref Rect position, SerializedProperty property)
        {
            var bladeCountBounds = LensParameterBounds.bladeCount;
            var curvatureBounds = LensParameterBounds.curvature;
            var barrelClippingBounds = LensParameterBounds.barrelClipping;
            var anamorphismBounds = LensParameterBounds.anamorphism;
            var expanded = EditorGUI.Foldout(
                position, IsExpanded(property, Sections.apertureShape), Contents.apertureShapeLabel, true);

            SetExpanded(property, Sections.apertureShape, expanded);

            if (expanded)
            {
                position = NextLine(position);

                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUI.IntSlider(
                        position,
                        m_BladeCountProp,
                        bladeCountBounds.x,
                        bladeCountBounds.y,
                        Contents.bladeCount);

                    position = NextLine(position);

                    DoRangeGUI(position, Contents.curvature, m_CurvatureProp, curvatureBounds.x, curvatureBounds.y, false);

                    position = NextLine(position);

                    EditorGUI.Slider(
                        position,
                        m_BarrelClippingProp,
                        barrelClippingBounds.x,
                        barrelClippingBounds.y,
                        Contents.barrelClipping);

                    position = NextLine(position);

                    EditorGUI.Slider(
                        position,
                        m_AnamorphismProp,
                        anamorphismBounds.x,
                        anamorphismBounds.y,
                        Contents.anamorphism);
                }
            }
        }

        void DoRangeGUI(Rect position, GUIContent label, SerializedProperty property, float min, float max, bool indent = true)
        {
            var showMixedValues = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            var value = DoRangeGUI(position, label, property.vector2Value, min, max, indent);
            property.vector2Value = value;
            EditorGUI.showMixedValue = showMixedValues;
        }

        Vector2 DoRangeGUI(Rect position, GUIContent label, Vector2 value, float min, float max, bool indent = true)
        {
            if (indent)
                EditorGUI.indentLevel++;

            // From HDCameraUI.Drawers.cs
            var v = value;
            // The layout system breaks alignment when mixing inspector fields with custom layout'd
            // fields as soon as a scrollbar is needed in the inspector, so we'll do the layout
            // manually instead
            const int kFloatFieldWidth = 50;
            const int kSeparatorWidth = 5;
            var indentOffset = EditorGUI.indentLevel * 15f;

            var labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth - indentOffset, position.height);
            var floatFieldLeft = new Rect(labelRect.xMax, position.y, kFloatFieldWidth + indentOffset, position.height);
            var sliderRect = new Rect(floatFieldLeft.xMax + kSeparatorWidth - indentOffset, position.y,
                position.width - labelRect.width - kFloatFieldWidth * 2 - kSeparatorWidth * 2, position.height);
            var floatFieldRight = new Rect(sliderRect.xMax + kSeparatorWidth - indentOffset, position.y,
                kFloatFieldWidth + indentOffset, position.height);

            EditorGUI.PrefixLabel(labelRect, label);
            v.x = EditorGUI.FloatField(floatFieldLeft, v.x);

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                EditorGUI.MinMaxSlider(sliderRect, ref v.x, ref v.y, min, max);

                if (change.changed)
                {
                    v.x = (float)Math.Round((float)v.x, 2);
                    v.y = (float)Math.Round((float)v.y, 2);
                }
            }

            v.y = EditorGUI.FloatField(floatFieldRight, v.y);

            if (indent)
                EditorGUI.indentLevel--;

            return v;
        }

        float GetLineHeight()
        {
            return EditorGUIUtility.singleLineHeight;
        }

        Rect BeginLine(Rect position)
        {
            return new Rect(
                position.x,
                position.y,
                position.width,
                GetLineHeight());
        }

        Rect NextLine(Rect position)
        {
            return new Rect(
                position.x,
                position.y + position.height + 2f,
                position.width,
                GetLineHeight());
        }
    }
}
