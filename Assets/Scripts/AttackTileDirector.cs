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
        // �^�[���`���Ŗ���X�V
        TurnSignal.OnPlayerTurnStart += RefreshAll;
        TurnSignal.OnEnemyTurnStart += RefreshAll;

        // �V�[����̊���㩂ɉ����ʒm���w�ǁi�N��SE�I�����\��OK�̃^�C�~���O�j
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

    // 㩂��u�\��OK�v�ɂȂ����u�Ԃɑ����f�i�Â��̂������čŐV�����\���j
    private void HandleTrapArmedVisual(TrapBaseSequenced _)
    {
        RefreshTrapsOnly();
    }

    // ========= ���JAPI =========

    /// <summary>�G�E㩂����ׂčŐV��Ԃŕ`�������i�Â��\���͊��S�N���A�j</summary>
    public void RefreshAll()
    {
        var svc = AttackTileService.Instance;
        if (svc == null) return;

        // 1) ���W
        var enemyCounts = BuildEnemyCounts();
        var trapDirs = BuildTrapDirs();

        // 2) �S����
        svc.ClearAll();

        // 3) �ĕ`��i�g�A�j���͔p�~�j
        svc.PaintEnemyStaticLayered(enemyCounts);
        svc.PaintTrapArrowsLayered(trapDirs);
    }

    /// <summary>
    /// �G�̍s������ȂǁA�Â��\�����c��Ȃ��悤��
    /// ��x�S�������Ă���u�G�{㩁v���܂Ƃ߂čĕ`��i=�ŐV���j
    /// </summary>
    public void RefreshEnemyOnly()
    {
        RefreshAll();
    }

    /// <summary>
    /// 㩂̉����C�x���g����Ȃǂ��A�S�������ŐV�ŕ`��i=�Â��\�����c���Ȃ��j
    /// </summary>
    public void RefreshTrapsOnly()
    {
        RefreshAll();
    }

    // ========= �����F�G�Z���̏d���J�E���g =========
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

    // ========= �����F㩃Z�����������X�g�iarmed�̂݁j =========
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
                // �������̏d����1�{�Ɂi�����{���������ꍇ��Contains���O���j
                if (!list.Contains(dir)) list.Add(dir);
            }
        }

        // ���F���̂��ߌŒ菇�Ń\�[�g�i�C�Ӂj
        foreach (var kv in trapDirs)
        {
            kv.Value.Sort((a, b) =>
            {
                int Rank(Vector2Int d)
                {
                    if (d == new Vector2Int(0, 1)) return 0;  // ��
                    if (d == new Vector2Int(1, 0)) return 1;  // ��
                    if (d == new Vector2Int(0, -1)) return 2; // ��
                    if (d == new Vector2Int(-1, 0)) return 3; // ��
                    if (d == new Vector2Int(1, 1)) return 4;  // �΂�
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
