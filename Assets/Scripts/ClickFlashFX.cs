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

    // === �ǉ�: �d�����΃K�[�h�i�O���[�o��&�C���X�^���X�j ===
    [Header("Duplicate Guard")]
    [Tooltip("���̕b���ȓ��ɋ߂����W��Play���Ă΂ꂽ�疳������")]
    public float globalCooldown = 0.05f;
    [Tooltip("�O��ʒu���炱�̋��������Ȃ瓯��N���b�N�Ƃ��ă}�[�W�i0�Ŗ����j")]
    public float mergeDistance = 0.1f;

    private static int s_lastSpawnFrame = -9999;
    private static float s_lastSpawnTime = -999f;
    private static Vector3 s_lastPos;

    private bool _playing = false; // ���̃C���X�^���X�̑��d�Đ��K�[�h

    void Reset()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
    }

    /// <summary>
    /// �N���b�NFX���Đ��B�d���K�[�h�t���B
    /// </summary>
    public void Play(Vector3 worldPos, Color tint, string layerName, int order)
    {
        // ---- �O���[�o���d���K�[�h�i���t���[��/�Z����/�ߐڈʒu�j----
        // ���t���[���ɂ����o���Ă���}�~
        if (Time.frameCount == s_lastSpawnFrame)
        {
            // �ʒu���قړ����Ȃ�}�[�W
            if (mergeDistance > 0f && (worldPos - s_lastPos).sqrMagnitude <= mergeDistance * mergeDistance)
                return;
        }
        // �Z���ԓ��̘A��������}�~
        if (Time.unscaledTime - s_lastSpawnTime < globalCooldown)
        {
            if (mergeDistance <= 0f || (worldPos - s_lastPos).sqrMagnitude <= mergeDistance * mergeDistance)
                return;
        }

        // ---- �C���X�^���X���̏d���K�[�h ----
        if (_playing) return;

        if (sr == null) sr = GetComponent<SpriteRenderer>();
        transform.position = worldPos;
        if (!string.IsNullOrEmpty(layerName)) sortingLayerName = layerName;
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = order;

        // �O���[�o���L�^�X�V
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
