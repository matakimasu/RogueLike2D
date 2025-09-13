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
    public TextMeshProUGUI label;   // �����e�L�X�g�i�q���玩���擾OK�j
    public CanvasGroup canvasGroup; // �t�F�[�h����i�e�ɕt�^�j
    public Image backdrop;          // ����K���X�w�i�i�e or �q�A�ǂ���ł�OK�j

    [Header("Text")]
    public string playerTitle = "PLAYER TURN";
    public string enemyTitle = "ENEMY TURN";
    public string turnFormat = "TURN {0}";

    [Header("Timings")]
    public float fadeIn = 0.15f;
    public float hold = 0.80f;
    public float fadeOut = 0.20f;

    [Header("Look & Feel")]
    [Tooltip("�T�u�s�i�^�[�����j�̑��΃T�C�Y")]
    public float sublineScale = 0.7f;
    [Tooltip("�w�i�̃p�f�B���O�ipx�j")]
    public Vector4 backdropPadding = new Vector4(24, 24, 24, 24); // L,T,R,B

    [Header("Auto wire")]
    public bool autoWire = true;

    void Awake()
    {
        if (autoWire)
        {
            if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
            if (!label) label = GetComponentInChildren<TextMeshProUGUI>(true);

            // �e�� Image ��������Ύq����E���i����̊K�w�ɍ����j
            if (!backdrop)
            {
                backdrop = GetComponent<Image>();
                if (!backdrop) backdrop = GetComponentInChildren<Image>(true);
            }
        }

        // �����͔�\��
        if (canvasGroup) canvasGroup.alpha = 0f;

        // �w�i�̃��C�A�E�g�i�qImage�ł�OK�j
        if (backdrop)
        {
            var brt = backdrop.rectTransform;
            // �e�����ς��ɃX�g���b�`���ăp�f�B���O�����^����
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.offsetMin = new Vector2(backdropPadding.x, backdropPadding.y); // left, bottom
            brt.offsetMax = new Vector2(-backdropPadding.z, -backdropPadding.w); // right, top
            // �N���b�N�ז����Ȃ�
            backdrop.raycastTarget = false;
            // ����K���X���̐F�i�K�X�ύX�j
            if (backdrop.color.a <= 0.01f)
                backdrop.color = new Color(1f, 1f, 1f, 0.35f);
        }

        // �e�L�X�g���N���b�N�ז����Ȃ�
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

        // ���g�͏�ɉ�ʒ���
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
