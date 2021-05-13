using System;
using UnityEngine;

namespace Unity.LiveCapture
{
    static class AnimationCurveUtility
    {
        const float k_DefaultWeight = 1f / 3f;
        const float k_CurveTimeEpsilon = 0.00001f;
        const float k_Bias = 0.5f;

        public static void UpdateTangents(this AnimationCurve curve, int index)
        {
            if (index < 0)
                return;

            var keyframe = curve[index];

            if (index == 0 ||  index == curve.length - 1)
            {
                keyframe.inTangent = 0f;
                keyframe.outTangent = 0f;

                if (curve.length > 0)
                {
                    keyframe.inWeight = k_DefaultWeight;
                    keyframe.outWeight = k_DefaultWeight;
                }
            }
            else
            {
                var keyframePrev = curve[index - 1];
                var keyframeNext = curve[index + 1];
                var dx1 = keyframe.time - keyframePrev.time;
                var dy1 = keyframe.value - keyframePrev.value;
                var dx2 = keyframeNext.time - keyframe.time;
                var dy2 = keyframeNext.value - keyframe.value;
                var dx = dx1 + dx2;
                var dy = dy1 + dy2;
                var m1 = SafeDeltaDivide(dy1, dx1);
                var m2 = SafeDeltaDivide(dy2, dx2);
                var m = SafeDeltaDivide(dy, dx);

                if ((m1 > 0 && m2 > 0) || (m1 < 0 && m2 < 0))
                {
                    var lower_bias = (1f - k_Bias) * 0.5f;
                    var upper_bias = lower_bias + k_Bias;
                    var lower_dy = dy * lower_bias;
                    var upper_dy = dy * upper_bias;

                    if (Mathf.Abs(dy1) >= Mathf.Abs(upper_dy))
                    {
                        var b = SafeDeltaDivide(dy1 - upper_dy, lower_dy);
                        var mp = (1f - b) * m;

                        keyframe.inTangent = mp;
                        keyframe.outTangent = mp;
                    }
                    else if (Mathf.Abs(dy1) < Mathf.Abs(lower_dy))
                    {
                        var b = SafeDeltaDivide(dy1, lower_dy);
                        var mp = b * m;

                        keyframe.inTangent = mp;
                        keyframe.outTangent = mp;
                    }
                    else
                    {
                        keyframe.inTangent = m;
                        keyframe.outTangent = m;
                    }
                }
                else
                {
                    keyframe.inTangent = 0f;
                    keyframe.outTangent = 0f;
                }

                keyframe.inWeight = k_DefaultWeight;
                keyframe.outWeight = k_DefaultWeight;
            }

            curve.MoveKey(index, keyframe);
        }

        static float SafeDeltaDivide(float y, float x)
        {
            if (Mathf.Abs(x) > k_CurveTimeEpsilon)
                return y / x;
            else
                return 0f;
        }
    }
}
