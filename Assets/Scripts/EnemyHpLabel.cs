using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class EnemyHpLabel : MonoBehaviour
{
    public enum Mode { Auto, World3D, UI }

    [Header("モード指定")]
    public Mode mode = Mode.World3D;   // ★ デフォルトでワールド扱いに固定

    [Header("ワールド(TMP 3D)用オフセット")]
    public Vector3 worldOffset = new Vector3(0f, -0.65f, 0f); // ★ 指定どおり

    [Header("UI(TextMeshProUGUI)用オフセット(px)")]
    public Vector2 uiAnchoredOffset = new Vector2(0f, -20f);

    [Header("制御フラグ")]
    public bool controlX = true;   // Xは上書きしない
    public bool controlY = true;    // Yのみ制御

    [Header("ズーム一定(任意)")]
    public bool keepConstantScreenSize = false;
    public float baseOrthoSize = 5f;
    public float baseScale = 1f;

    [Header("描画順(任意/3Dのみ)")]
    public int sortingOrderOffset = 10;

    private TMP_Text text;
    private EnemyHealth hp;
    private Transform parent;
    private Camera cam;
    private Renderer meshRenderer;

    private bool runtimeIsUI = false; // 実際に使う判定
    private int lastHp = int.MinValue, lastMax = int.MinValue;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
        parent = transform.parent;
        cam = Camera.main;

        if (parent != null) parent.TryGetComponent(out hp);

        // 実モード決定
        bool autoIsUI = (GetComponent<TextMeshProUGUI>() != null) ||
                        (GetComponentInParent<Canvas>() != null);
        runtimeIsUI = mode == Mode.UI ? true :
                      mode == Mode.World3D ? false : autoIsUI;

        // 3D のときだけソート順を親SRに追従
        meshRenderer = GetComponent<Renderer>();
        var parentSR = parent ? parent.GetComponent<SpriteRenderer>() : null;
        if (!runtimeIsUI && meshRenderer != null && parentSR != null)
        {
            meshRenderer.sortingLayerID = parentSR.sortingLayerID;
            meshRenderer.sortingOrder = parentSR.sortingOrder + sortingOrderOffset;
        }

        text.alignment = TextAlignmentOptions.MidlineGeoAligned;
        text.raycastTarget = false;
    }

    void OnEnable()
    {
        if (hp != null) hp.OnHPChanged += HandleHpChanged;
        ForceRefresh();
    }

    void OnDisable()
    {
        if (hp != null) hp.OnHPChanged -= HandleHpChanged;
    }

    void LateUpdate()
    {
        if (parent == null) return;

        // --- 位置制御 ---
        if (runtimeIsUI)
        {
            // UI (Canvas配下 / TextMeshProUGUI)
            var rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                var ap = rt.anchoredPosition;
                if (controlX) ap.x = uiAnchoredOffset.x;
                if (controlY) ap.y = uiAnchoredOffset.y;
                rt.anchoredPosition = ap;
            }
        }
        else
        {
            // ワールド (3D TMP)
            var lp = transform.localPosition;
            if (controlX) lp.x = worldOffset.x;
            if (controlY) lp.y = worldOffset.y;          // ★ -0.65 を適用
            transform.localPosition = lp;
        }

        // ズーム一定（必要時のみ）
        if (keepConstantScreenSize && cam != null)
        {
            if (cam.orthographic)
            {
                float k = baseOrthoSize / Mathf.Max(0.0001f, cam.orthographicSize);
                transform.localScale = Vector3.one * baseScale * k;
            }
            else
            {
                float dist = Vector3.Distance(cam.transform.position, transform.position);
                float k = Mathf.Clamp(dist, 0.1f, 100f) / 10f;
                transform.localScale = Vector3.one * baseScale * k;
            }
        }

        SoftRefresh();
    }

    private void HandleHpChanged(int cur, int max) => SetText(cur);

    private void ForceRefresh()
    {
        if (hp == null) return;
        lastHp = hp.CurrentHP; lastMax = hp.MaxHP;
        SetText(lastHp);
    }

    private void SoftRefresh()
    {
        if (hp == null) return;
        int cur = hp.CurrentHP, max = hp.MaxHP;
        if (cur != lastHp || max != lastMax)
        {
            lastHp = cur; lastMax = max;
            SetText(cur);
        }
    }

    private void SetText(int cur)
    {
        // フォントに全角コロンが無いことがあるため半角にしています
        if (text != null) text.text = $"HP: {cur}";
    }
}
