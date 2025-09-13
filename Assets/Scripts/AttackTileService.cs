using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class AttackTileService : MonoBehaviour
{
    public static AttackTileService Instance { get; private set; }

    [Header("Enemy Overlays (Static symbol layers - back to front)")]
    [SerializeField] private Tilemap[] enemyOverlays;      // �������C���Ή�
    [SerializeField] private TileBase enemyStaticTile;     // ��F���A�C�R��

    [Header("Trap Overlays (Arrow-based 8-dir layers - back to front)")]
    [SerializeField] private Tilemap[] trapOverlays;       // �������C���Ή�
    [SerializeField] private TileBase trapArrow_U;
    [SerializeField] private TileBase trapArrow_D;
    [SerializeField] private TileBase trapArrow_L;
    [SerializeField] private TileBase trapArrow_R;
    [SerializeField] private TileBase trapArrow_UL;
    [SerializeField] private TileBase trapArrow_UR;
    [SerializeField] private TileBase trapArrow_DL;
    [SerializeField] private TileBase trapArrow_DR;

    private Dictionary<Vector2Int, TileBase> _trapDirMap;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _trapDirMap = new Dictionary<Vector2Int, TileBase>
        {
            [new Vector2Int(0, 1)] = trapArrow_U,
            [new Vector2Int(0, -1)] = trapArrow_D,
            [new Vector2Int(-1, 0)] = trapArrow_L,
            [new Vector2Int(1, 0)] = trapArrow_R,
            [new Vector2Int(-1, 1)] = trapArrow_UL,
            [new Vector2Int(1, 1)] = trapArrow_UR,
            [new Vector2Int(-1, -1)] = trapArrow_DL,
            [new Vector2Int(1, -1)] = trapArrow_DR,
        };
    }

    public void ClearAll()
    {
        if (enemyOverlays != null)
            foreach (var tm in enemyOverlays) if (tm) tm.ClearAllTiles();
        if (trapOverlays != null)
            foreach (var tm in trapOverlays) if (tm) tm.ClearAllTiles();
    }

    // �G�F�Z�����d���� �̕��z�`��i���C��0��1���ځA1��2���ځc�j
    public void PaintEnemyStaticLayered(Dictionary<Vector3Int, int> cellCount)
    {
        if (enemyOverlays == null || enemyOverlays.Length == 0 || enemyStaticTile == null) return;
        int layers = enemyOverlays.Length;
        if (layers == 0) return;

        // ���C�����Ƃɂ܂Ƃ߂�SetTiles
        for (int layer = 0; layer < layers; layer++)
        {
            var tm = enemyOverlays[layer];
            if (!tm) continue;

            var cells = new List<Vector3Int>();
            var tiles = new List<TileBase>();
            foreach (var kv in cellCount)
            {
                // ���̃Z���� layer+1 ���ڂ����݂���Ȃ�`��
                if (kv.Value > layer)
                {
                    cells.Add(kv.Key);
                    tiles.Add(enemyStaticTile);
                }
            }
            if (cells.Count > 0) tm.SetTiles(cells.ToArray(), tiles.ToArray());
        }
    }

    // 㩁F�Z�����������X�g �̕��z�`��i���C��0��1�{�ځA1��2�{�ځc�j
    public void PaintTrapArrowsLayered(Dictionary<Vector3Int, List<Vector2Int>> cellDirs)
    {
        if (trapOverlays == null || trapOverlays.Length == 0) return;
        int layers = trapOverlays.Length;
        if (layers == 0) return;

        for (int layer = 0; layer < layers; layer++)
        {
            var tm = trapOverlays[layer];
            if (!tm) continue;

            foreach (var kv in cellDirs)
            {
                var list = kv.Value;
                if (list == null || list.Count <= layer) continue; // ���̑w�ɕ`���ׂ���󂪂Ȃ�
                var dir = list[layer];
                if (_trapDirMap != null && _trapDirMap.TryGetValue(dir, out var tile) && tile != null)
                {
                    tm.SetTile(kv.Key, tile);
                }
            }
        }
    }
}
