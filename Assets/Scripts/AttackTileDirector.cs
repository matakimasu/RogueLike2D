using System.Collections.Generic;
using UnityEngine;

public class AttackTileDirector : MonoBehaviour
{
    public static AttackTileDirector Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        // ターン冒頭で毎回更新
        TurnSignal.OnPlayerTurnStart += RefreshAll;
        TurnSignal.OnEnemyTurnStart += RefreshAll;

        // シーン上の既存罠に可視化通知を購読（起動SE終了→表示OKのタイミング）
        foreach (var t in FindObjectsOfType<TrapBaseSequenced>())
            t.OnArmedVisual += HandleTrapArmedVisual;
    }

    private void OnDisable()
    {
        TurnSignal.OnPlayerTurnStart -= RefreshAll;
        TurnSignal.OnEnemyTurnStart -= RefreshAll;

        foreach (var t in FindObjectsOfType<TrapBaseSequenced>())
            t.OnArmedVisual -= HandleTrapArmedVisual;
    }

    // 罠が「表示OK」になった瞬間に即反映（古いのを消して最新だけ表示）
    private void HandleTrapArmedVisual(TrapBaseSequenced _)
    {
        RefreshTrapsOnly();
    }

    // ========= 公開API =========

    /// <summary>敵・罠をすべて最新状態で描き直す（古い表示は完全クリア）</summary>
    public void RefreshAll()
    {
        var svc = AttackTileService.Instance;
        if (svc == null) return;

        // 1) 収集
        var enemyCounts = BuildEnemyCounts();
        var trapDirs = BuildTrapDirs();

        // 2) 全消し
        svc.ClearAll();

        // 3) 再描画（波アニメは廃止）
        svc.PaintEnemyStaticLayered(enemyCounts);
        svc.PaintTrapArrowsLayered(trapDirs);
    }

    /// <summary>
    /// 敵の行動直後など、古い表示が残らないように
    /// 一度全消去してから「敵＋罠」をまとめて再描画（=最新化）
    /// </summary>
    public void RefreshEnemyOnly()
    {
        RefreshAll();
    }

    /// <summary>
    /// 罠の可視化イベント直後なども、全消去→最新で描画（=古い表示を残さない）
    /// </summary>
    public void RefreshTrapsOnly()
    {
        RefreshAll();
    }

    // ========= 内部：敵セルの重複カウント =========
    private Dictionary<Vector3Int, int> BuildEnemyCounts()
    {
        var enemyCounts = new Dictionary<Vector3Int, int>();

        foreach (var ai in FindObjectsOfType<EnemyAIBase>())
        {
            if (ai == null) continue;

            foreach (var c in ai.GetAttackCellsPreview())
            {
                if (enemyCounts.TryGetValue(c, out var n)) enemyCounts[c] = n + 1;
                else enemyCounts[c] = 1;
            }
        }

        return enemyCounts;
    }

    // ========= 内部：罠セル→方向リスト（armedのみ） =========
    private Dictionary<Vector3Int, List<Vector2Int>> BuildTrapDirs()
    {
        var trapDirs = new Dictionary<Vector3Int, List<Vector2Int>>();

        foreach (var t in FindObjectsOfType<TrapBaseSequenced>())
        {
            if (t == null || t.floorTilemap == null || !t.IsArmed) continue;

            var origin = t.floorTilemap.WorldToCell(t.transform.position);
            foreach (var cell in t.PreviewCells())
            {
                var dx = Mathf.Clamp(cell.x - origin.x, -1, 1);
                var dy = Mathf.Clamp(cell.y - origin.y, -1, 1);
                var dir = new Vector2Int(dx, dy);
                if (dir == Vector2Int.zero) continue;

                if (!trapDirs.TryGetValue(cell, out var list))
                {
                    list = new List<Vector2Int>();
                    trapDirs[cell] = list;
                }
                // 同方向の重複は1本に（複数本見せたい場合はContainsを外す）
                if (!list.Contains(dir)) list.Add(dir);
            }
        }

        // 視認性のため固定順でソート（任意）
        foreach (var kv in trapDirs)
        {
            kv.Value.Sort((a, b) =>
            {
                int Rank(Vector2Int d)
                {
                    if (d == new Vector2Int(0, 1)) return 0;  // ↑
                    if (d == new Vector2Int(1, 0)) return 1;  // →
                    if (d == new Vector2Int(0, -1)) return 2; // ↓
                    if (d == new Vector2Int(-1, 0)) return 3; // ←
                    if (d == new Vector2Int(1, 1)) return 4;  // 斜め
                    if (d == new Vector2Int(1, -1)) return 5;
                    if (d == new Vector2Int(-1, 1)) return 6;
                    return 7;
                }
                return Rank(a).CompareTo(Rank(b));
            });
        }

        return trapDirs;
    }
}
