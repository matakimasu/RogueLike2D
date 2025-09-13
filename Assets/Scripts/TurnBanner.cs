using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class TurnBanner : MonoBehaviour
{
    [Header("Refs")]
    public TextMeshProUGUI label;   // 中央テキスト（子から自動取得OK）
    public CanvasGroup canvasGroup; // フェード制御（親に付与）
    public Image backdrop;          // すりガラス背景（親 or 子、どちらでもOK）

    [Header("Text")]
    public string playerTitle = "PLAYER TURN";
    public string enemyTitle = "ENEMY TURN";
    public string turnFormat = "TURN {0}";

    [Header("Timings")]
    public float fadeIn = 0.15f;
    public float hold = 0.80f;
    public float fadeOut = 0.20f;

    [Header("Look & Feel")]
    [Tooltip("サブ行（ターン数）の相対サイズ")]
    public float sublineScale = 0.7f;
    [Tooltip("背景のパディング（px）")]
    public Vector4 backdropPadding = new Vector4(24, 24, 24, 24); // L,T,R,B

    [Header("Auto wire")]
    public bool autoWire = true;

    void Awake()
    {
        if (autoWire)
        {
            if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
            if (!label) label = GetComponentInChildren<TextMeshProUGUI>(true);

            // 親に Image が無ければ子から拾う（今回の階層に合う）
            if (!backdrop)
            {
                backdrop = GetComponent<Image>();
                if (!backdrop) backdrop = GetComponentInChildren<Image>(true);
            }
        }

        // 初期は非表示
        if (canvasGroup) canvasGroup.alpha = 0f;

        // 背景のレイアウト（子ImageでもOK）
        if (backdrop)
        {
            var brt = backdrop.rectTransform;
            // 親いっぱいにストレッチしてパディングだけ与える
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.offsetMin = new Vector2(backdropPadding.x, backdropPadding.y); // left, bottom
            brt.offsetMax = new Vector2(-backdropPadding.z, -backdropPadding.w); // right, top
            // クリック邪魔しない
            backdrop.raycastTarget = false;
            // すりガラス風の色（適宜変更）
            if (backdrop.color.a <= 0.01f)
                backdrop.color = new Color(1f, 1f, 1f, 0.35f);
        }

        // テキストもクリック邪魔しない
        if (label) label.raycastTarget = false;
    }

    public IEnumerator ShowPlayer(int cycleIndex) => ShowInternal(
        $"{playerTitle}\n<size={Mathf.RoundToInt(sublineScale * 100)}%>{string.Format(turnFormat, cycleIndex)}</size>"
    );

    public IEnumerator ShowEnemy(int cycleIndex) => ShowInternal(
        $"{enemyTitle}\n<size={Mathf.RoundToInt(sublineScale * 100)}%>{string.Format(turnFormat, cycleIndex)}</size>"
    );

    IEnumerator ShowInternal(string text)
    {
        if (!canvasGroup || !label) yield break;

        label.text = text;

        // 自身は常に画面中央
        var rt = (RectTransform)transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        // IN
        yield return FadeTo(1f, fadeIn);
        // HOLD
        yield return new WaitForSecondsRealtime(hold);
        // OUT
        yield return FadeTo(0f, fadeOut);
    }

    IEnumerator FadeTo(float target, float dur)
    {
        float start = canvasGroup.alpha;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / dur));
            yield return null;
        }
        canvasGroup.alpha = target;
    }
}
