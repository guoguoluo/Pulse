using UnityEngine;

/// <summary>Shared timing for rating-triggered light: rise, hold, and fall.</summary>
public static class ConductorLightEnvelope
{
    public static float Evaluate(float elapsed, float riseSeconds, float holdSeconds,
        float fallSeconds, AnimationCurve riseCurve)
    {
        if (elapsed < 0f) return 0f;

        float rise = Mathf.Max(0.01f, riseSeconds);
        if (elapsed < rise)
        {
            float t = Mathf.Clamp01(elapsed / rise);
            return Mathf.Clamp01(riseCurve != null ? riseCurve.Evaluate(t) : t);
        }

        if (elapsed < rise + Mathf.Max(0f, holdSeconds)) return 1f;
        float fall = Mathf.Max(0.01f, fallSeconds);
        float fade = (elapsed - rise - Mathf.Max(0f, holdSeconds)) / fall;
        return 1f - Mathf.Clamp01(fade);
    }
}
