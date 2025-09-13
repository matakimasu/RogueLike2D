using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class MaskAutoExpand : MonoBehaviour
{
    [Header("Expand")]
    public float duration = 0.3f;
    public float targetScale = 1f;
    public bool startFromZero = true;
    public bool useUnscaledTime = false;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Event")]
    public UnityEvent OnFinished;

    void OnEnable()
    {
        if (startFromZero) transform.localScale = Vector3.zero;
        StartCoroutine(CoExpand());
    }

    IEnumerator CoExpand()
    {
        Vector3 start = transform.localScale;
        Vector3 goal = Vector3.one * targetScale;
        float t = 0f;

        while (t < duration)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            float k = curve != null ? curve.Evaluate(u) : u;
            transform.localScale = Vector3.LerpUnclamped(start, goal, k);
            yield return null;
        }

        transform.localScale = goal;
        OnFinished?.Invoke();
    }
}
