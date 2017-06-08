using System.Collections;
using UnityEngine;

namespace HoloToolkit.Examples.HoloPair
{
    public static class Utils
    {
        public delegate void NoParamFunction();

        public static IEnumerator ExecuteAfterDelay(float totalTime, NoParamFunction ActionToCall)
        {
            float rate = 1.0f / totalTime;
            float t = 0.0f;
            while (t < 1.0)
            {
                t += Time.deltaTime * rate;
                yield return null;
            }
            ActionToCall();
        }

        public static IEnumerator TranslateTo(Transform thisTransform, Vector3 endPos, float value)
        {
            yield return TranslationFromTo(thisTransform, thisTransform.position, endPos, value);
        }

        public static IEnumerator TranslationRelativeBy(Transform thisTransform, Vector3 relPos, float value)
        {
            yield return TranslationFromTo(thisTransform, thisTransform.position, thisTransform.position + relPos, value);
        }

        public static IEnumerator TranslationFromTo(Transform thisTransform, Vector3 startPos, Vector3 endPos, float value)
        {
            float rate = 1.0f / value;
            float t = 0.0f;
            while (t < 1.0)
            {
                t += Time.deltaTime * rate;
                thisTransform.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0.0f, 1.0f, t));
                yield return null;
            }
        }
    }
}

