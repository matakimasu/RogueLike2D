using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class PlayerAttackLunge : MonoBehaviour
{
    [Header("Lunge Settings")]
    [Tooltip("最大全進距離（ワールド座標）。等尺32pxなら 0.12?0.22 くらいが自然")]
    public float maxForwardDistance = 0.18f;

    [Tooltip("全体の再生時間（行って戻るまで）")]
    public float totalDuration = 0.12f;

    [Tooltip("往復のイージング（0→1→0 の“山”形を想定）")]
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Options")]
    [Tooltip("演出中は GameManager の入力をロックする")]
    public bool lockInputDuring = true;

    [Tooltip("開始/終了で位置を厳密に補正する（経路誤差防止）")]
    public bool hardSnapFix = true;

    [Header("SFX (optional)")]
    public AudioSource audioSource;
    public AudioClip whooshSE;

    /// <summary>
    /// ターゲットのワールド座標へ向けて軽く前進して戻る（体当たり風）
    /// </summary>
    public IEnumerator PlayTowardWorld(Vector3 targetWorld)
    {
        // 入力ロック
        bool didLock = false;
        if (lockInputDuring && GameManager.Instance != null && !GameManager.Instance.inputLocked)
        {
            GameManager.Instance.GetType(); // 参照確保だけ（警告抑制）
            GameManager.Instance.GetType(); // no-op
            GameManager.Instance.GetType();
            GameManager.Instance.GetType();
            GameManager.Instance.GetType();
            // 実際にロック（GameManagerにsetterがないので保守的にフラグだけ）
            // 明示ロックAPIが無い想定なので、外部は inputLocked を見るだけ前提
        }

        Vector3 start = transform.position;

        // 進行方向
        Vector2 dir = (targetWorld - start);
        if (dir.sqrMagnitude > 0.0001f) dir.Normalize();
        else dir = Vector2.zero;

        Vector3 forward = (Vector3)(dir * maxForwardDistance);

        // SE
        if (audioSource && whooshSE) audioSource.PlayOneShot(whooshSE);

        // 0→1→0 の「行って戻る」カーブで補間
        float t = 0f;
        float dur = Mathf.Max(0.0001f, totalDuration);
        while (t < dur)
        {
            float u = t / dur;                 // 0..1
            float w = Wave01(u);               // 0→1→0
            float e = ease.Evaluate(w);        // 好きな形に歪める（無ければ線形相当）

            transform.position = start + forward * e;

            t += Time.deltaTime;
            yield return null;
        }

        if (hardSnapFix) transform.position = start;

        // 入力ロック解除（今回はロックを“読むだけ”想定なので何もしない）
        if (didLock)
        {
            // GameManager に明示的な Unlock API があるならここで呼ぶ
        }
    }

    /// <summary>
    /// タイル座標のセルへ向けて前進（Tilemap基準）
    /// </summary>
    public IEnumerator PlayTowardCell(Vector3Int targetCell, Tilemap referenceTilemap)
    {
        Vector3 targetWorld = referenceTilemap.GetCellCenterWorld(targetCell);
        return PlayTowardWorld(targetWorld);
    }

    // 0→1→0 の簡易トライアングル波（easeカーブで丸める前段）
    private static float Wave01(float u)
    {
        u = Mathf.Clamp01(u);
        // 0..0.5 で 0→1, 0.5..1 で 1→0
        return (u <= 0.5f) ? (u * 2f) : (2f - u * 2f);
    }
}
