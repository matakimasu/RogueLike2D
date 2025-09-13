using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemyAIX : EnemyAIBase
{
    protected override void Awake()
    {
        base.Awake();

        // プレイヤーTransformを自動取得（安全のため重複チェック）
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                playerTransform = playerObj.transform;
        }
    }

    // 移動方向を斜め4方向のみに限定
    protected override Vector3Int[] GetMovementDirections()
    {
        return new Vector3Int[]
        {
            new Vector3Int( 1,  1, 0),   // 右上
            new Vector3Int( 1, -1, 0),   // 右下
            new Vector3Int(-1,  1, 0),   // 左上
            new Vector3Int(-1, -1, 0)    // 左下
        };
    }

    // 攻撃判定も斜めのみ（斜め1マスが攻撃対象）
    protected override bool CanAttackPlayer(Vector3Int enemyCell, Vector3Int playerCell)
    {
        int dx = Mathf.Abs(enemyCell.x - playerCell.x);
        int dy = Mathf.Abs(enemyCell.y - playerCell.y);
        return (dx == 1 && dy == 1);
    }

    // ※ GetAttackCellsPreview() は EnemyAIBase 側の既定実装でOK
    //    （上の GetMovementDirections / CanAttackPlayer を使って斜め4方向をプレビューします）
}
