﻿using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Mirror.Examples.AdditiveLevels
{
    public class FadeInOut : MonoBehaviour
    {
        // set these in the inspector
        [Tooltip("Reference to Image component on child panel")]
        public Image fadeImage;

        [Tooltip("Color to use during scene transition")]
        public Color fadeColor = Color.black;

        [Range(1, 100)] [Tooltip("Rate of fade in / out: higher is faster")]
        public byte stepRate = 2;

        private float step;

        private void Start()
        {
            // Convert user-friendly setting value to working value
            step = stepRate * 0.001f;
        }

        private void OnValidate()
        {
            if (fadeImage == null)
                fadeImage = GetComponentInChildren<Image>();
        }

        /// <summary>
        ///     Calculates FadeIn / FadeOut time.
        /// </summary>
        /// <returns>Duration in seconds</returns>
        public float GetDuration()
        {
            var frames = 1 / step;
            var frameRate = Time.deltaTime;
            var duration = frames * frameRate * 0.1f;
            return duration;
        }

        public IEnumerator FadeIn()
        {
            var alpha = fadeImage.color.a;

            while (alpha < 1)
            {
                yield return null;
                alpha += step;
                fadeColor.a = alpha;
                fadeImage.color = fadeColor;
            }
        }

        public IEnumerator FadeOut()
        {
            var alpha = fadeImage.color.a;

            while (alpha > 0)
            {
                yield return null;
                alpha -= step;
                fadeColor.a = alpha;
                fadeImage.color = fadeColor;
            }
        }
    }
}