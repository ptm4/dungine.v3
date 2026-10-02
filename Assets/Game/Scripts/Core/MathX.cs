using UnityEngine;

namespace Dungine
{
    public static class MathX
    {
        /// <summary>
        /// GLSL-style smoothstep: 0 at e0, 1 at e1, clamped, smooth in between. Unity's MathX.Smoothstep(from, to, t)
        /// instead interpolates between from and to, which is a different function.
        /// </summary>
        public static float Smoothstep(float e0, float e1, float x)
        {
            float t = Mathf.Clamp01((x - e0) / (e1 - e0));
            return t * t * (3f - 2f * t);
        }
    }
}
