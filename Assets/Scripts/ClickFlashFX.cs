using UnityEngine;

[DisallowMultipleComponent]
public class ClickFlashFX : MonoBehaviour
{
    public SpriteRenderer sr;

    [Header("Timing")]
    public float duration = 0.22f;

    [Header("Animation")]
    public float startScale = 0.7f;
    public float endScale = 1.6f;
    [Range(0f, 1f)] public float startAlpha = 0.9f;
    [Range(0f, 1f)] public float endAlpha = 0.0f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Sorting")]
    public string sortingLayerName = "Overlays";
    public int sortingOrder = 600;

    // === 追加: 重複発火ガード（グローバル&インスタンス） ===
    [Header("Duplicate Guard")]
    [Tooltip("この秒数以内に近い座標でPlayが呼ばれたら無視する")]
    public float globalCooldown = 0.05f;
    [Tooltip("前回位置からこの距離未満なら同一クリックとしてマージ（0で無効）")]
    public float mergeDistance = 0.1f;

    private static int s_lastSpawnFrame = -9999;
    private static float s_lastSpawnTime = -999f;
    private static Vector3 s_lastPos;

    private bool _playing = false; // このインスタンスの多重再生ガード

    void Reset()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
    }

    /// <summary>
    /// クリックFXを再生。重複ガード付き。
    /// </summary>
    public void Play(Vector3 worldPos, Color tint, string layerName, int order)
    {
        // ---- グローバル重複ガード（同フレーム/短時間/近接位置）----
        // 同フレームにもう出してたら抑止
        if (Time.frameCount == s_lastSpawnFrame)
        {
            // 位置がほぼ同じならマージ
            if (mergeDistance > 0f && (worldPos - s_lastPos).sqrMagnitude <= mergeDistance * mergeDistance)
                return;
        }
        // 短時間内の連続生成を抑止
        if (Time.unscaledTime - s_lastSpawnTime < globalCooldown)
        {
            if (mergeDistance <= 0f || (worldPos - s_lastPos).sqrMagnitude <= mergeDistance * mergeDistance)
                return;
        }

        // ---- インスタンス側の重複ガード ----
        if (_playing) return;

        if (sr == null) sr = GetComponent<SpriteRenderer>();
        transform.position = worldPos;
        if (!string.IsNullOrEmpty(layerName)) sortingLayerName = layerName;
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = order;

        // グローバル記録更新
        s_lastSpawnFrame = Time.frameCount;
        s_lastSpawnTime = Time.unscaledTime;
        s_lastPos = worldPos;

        StartCoroutine(PlayRoutine(tint));
    }

    System.Collections.IEnumerator PlayRoutine(Color tint)
    {
        _playing = true;

        float t = 0f;
        while (t < duration)
        {
            float k = ease.Evaluate(t / duration);
            float s = Mathf.Lerp(startScale, endScale, k);
            float a = Mathf.Lerp(startAlpha, endAlpha, k);

            transform.localScale = new Vector3(s, s, 1f);
            var c = tint; c.a = a;
            if (sr != null) sr.color = c;

            t += Time.deltaTime;
            yield return null;
        }

        _playing = false;
        Destroy(gameObject);
    }
}
