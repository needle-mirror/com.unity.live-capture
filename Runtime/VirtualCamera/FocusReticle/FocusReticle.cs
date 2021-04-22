using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// A widget representing the focus reticle, featuring animation.
    /// It is both used on device and in the server's game view.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("")]
    public class FocusReticle : MonoBehaviour
    {
        const float k_ScaleAnimationDuration = 0.3f;

        /// <summary>
        /// Invoked on completion of the animation. Can be used to deactivate the reticle gameObject for example.
        /// </summary>
        public event Action animationComplete = delegate {};

        Image m_ReticleImage;

        void Awake()
        {
            m_ReticleImage = transform.Find("Square").GetComponent<Image>();
        }

        /// <summary>
        /// Puts the focus reticle in its default visual state.
        /// Meant to revert from a state induced by animation.
        /// </summary>
        public void ResetAnimation()
        {
            m_ReticleImage.color = m_ReticleImage.color + Color.black;
            m_ReticleImage.transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Animates the focus reticle following a fade-in and optionaly fade-out pattern.
        /// </summary>
        /// <param name="hideOnComplete">whether or not the animation should fade out automatically after having faded in.</param>
        /// <returns></returns>
        public IEnumerator Animate(bool hideOnComplete)
        {
            m_ReticleImage.transform.localScale = Vector3.one * 2f;
            var startingScale = m_ReticleImage.transform.localScale;
            var t = 0f;
            var color = m_ReticleImage.color;
            while (t <= k_ScaleAnimationDuration)
            {
                color.a = t / k_ScaleAnimationDuration;
                m_ReticleImage.color = color;

                m_ReticleImage.transform.localScale = startingScale - Vector3.one * t / k_ScaleAnimationDuration;

                t += Time.deltaTime;
                yield return null;
            }

            color.a = 1f;
            m_ReticleImage.color = color;
            m_ReticleImage.transform.localScale = Vector3.one;

            if (hideOnComplete)
            {
                yield return new WaitForSeconds(0.2f);

                t = 0f;
                while (t <= k_ScaleAnimationDuration)
                {
                    color = m_ReticleImage.color;
                    color.a = 1 - t / k_ScaleAnimationDuration;
                    m_ReticleImage.color = color;

                    t += Time.deltaTime;
                    yield return null;
                }
            }

            animationComplete.Invoke();
        }
    }
}
