using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// プレイヤーの現在セルの「セル中心」に SpriteMask をスナップ追従させる。
/// ルンジなどでプレイヤーが中心から外れている間は移動しないため、隣マスが見えない。
/// </summary>
[ExecuteAlways]
public class PlayerCellMaskFollower : MonoBehaviour
{
    [Header("参照")]
    public Transform player;        // プレイヤーのTransform（子にしない）
    public Tilemap floorTilemap;    // 床Tilemap（World<->Cell変換に使用）

    [Header("挙動")]
    [Tooltip("プレイヤーがセル中心に十分近い時のみスナップする閾値（ワールド距離）")]
    public float snapEpsilon = 0.02f;

    [Tooltip("エディタ停止中も常に現在セル中心に合わせたい場合ON（Scene上で位置確認用）")]
    public bool snapInEditMode = true;

    // 直近でスナップした位置（任意）
    private Vector3 _lastSnapped;

    private void Reset()
    {
        // 自動推測
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        if (floorTilemap == null)
        {
            var tm = FindObjectOfType<Tilemap>();
            if (tm) floorTilemap = tm;
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && !snapInEditMode) return;
#endif
        if (player == null || floorTilemap == null) return;

        // プレイヤー位置から現在セルと、その中心ワールド座標を求める
        Vector3Int cell = floorTilemap.WorldToCell(player.position);
        Vector3 cellCenter = floorTilemap.GetCellCenterWorld(cell);

        // プレイヤーがセル中心に十分近い時だけスナップ
        float dist = Vector3.Distance(player.position, cellCenter);
        if (dist <= snapEpsilon)
        {
            if (_lastSnapped != cellCenter)
            {
                transform.position = cellCenter;
                _lastSnapped = cellCenter;
            }
        }
        // 近くない間は「前回のスナップ位置」に留まる（＝ルンジ中は固定）
    }
}
