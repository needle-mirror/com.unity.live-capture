using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.LiveCapture.ARKitFaceCapture.DefaultMapper
{
    /// <summary>
    /// Defines how a <see cref="FaceBlendShape"/> affects a blend shape on a skinned mesh.
    /// </summary>
    [Serializable]
    class BindingConfig : IDrawable
    {
        enum Type
        {
            Simple,
            Curve,
        }

        [SerializeField]
        float m_Smoothing = 0.1f;
        [SerializeField]
        EvaluatorPreset m_EvaluatorPreset = null;
        [SerializeField]
        Type m_Type = Type.Simple;
        [SerializeField]
        SimpleEvaluator.Impl m_SimpleEvaluator = new SimpleEvaluator.Impl();
        [SerializeField]
        CurveEvaluator.Impl m_CurveEvaluator = new CurveEvaluator.Impl();

        /// <summary>
        /// The amount of smoothing to apply to the blend shape value, with a value in the range [0, 1].
        /// </summary>
        public float smoothing => m_Smoothing;

        /// <summary>
        /// Creates a new <see cref="BindingConfig"/> instance.
        /// </summary>
        /// <param name="preset">The preset evaluation function to use, or null to use a custom function.</param>
        public BindingConfig(EvaluatorPreset preset)
        {
            m_EvaluatorPreset = preset;
        }

        /// <summary>
        /// Gets the evaluation function defined by this configuration.
        /// </summary>
        public IEvaluator GetEvaluator()
        {
            if (m_EvaluatorPreset != null)
                return m_EvaluatorPreset.evaluator;

            switch (m_Type)
            {
                case Type.Simple:
                    return m_SimpleEvaluator;
                case Type.Curve:
                    return m_CurveEvaluator;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

#if UNITY_EDITOR
        static class Contents
        {
            public static readonly GUIContent smoothing = new GUIContent("Smoothing", "The amount of smoothing to apply to the blend shape value. " +
                "It can help reduce jitter in the face capture, but it will also smooth out fast motions.");
            public static readonly GUIContent evaluatorPreset = new GUIContent("Evaluator Preset", "A preset evaluation function to use. " +
                "If none is assigned, a new function must be configured for this blend shape.");
            public static readonly GUIContent type = new GUIContent("Type", "The type of evaluation function to use when a preset is not assigned.");
        }

        /// <inheritdoc/>
        public float GetHeight()
        {
            var height = (2 * EditorGUIUtility.singleLineHeight) + (1 * EditorGUIUtility.standardVerticalSpacing);

            if (m_EvaluatorPreset == null)
            {
                height += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
                height += EditorGUIUtility.standardVerticalSpacing + GetEvaluator().GetHeight();
            }

            return height;
        }

        /// <inheritdoc/>
        public void OnGUI(Rect rect)
        {
            var line = rect;
            line.height = EditorGUIUtility.singleLineHeight;

            m_Smoothing = EditorGUI.Slider(line, Contents.smoothing, m_Smoothing, 0f, 1f);

            GUIUtils.NextLine(ref line);
            m_EvaluatorPreset = EditorGUI.ObjectField(line, Contents.evaluatorPreset, m_EvaluatorPreset, typeof(EvaluatorPreset), false) as EvaluatorPreset;

            if (m_EvaluatorPreset == null)
            {
                GUIUtils.NextLine(ref line);
                m_Type = (Type)EditorGUI.EnumPopup(line, Contents.type, m_Type);

                var evaluatorRect = rect;
                evaluatorRect.yMin = line.yMax;
                GetEvaluator().OnGUI(evaluatorRect);
            }
        }

#endif
    }
}
