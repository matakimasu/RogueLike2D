using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemyAI : EnemyAIBase
{
    protected override void Awake()
    {
        base.Awake();

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                playerTransform = playerObj.transform;
        }
    }

    // ✅ abstract メソッドの正しいオーバーライド
    protected override Vector3Int[] GetMovementDirections()
    {
        return new Vector3Int[]
        {
            Vector3Int.up,
            Vector3Int.down,
            Vector3Int.left,
            Vector3Int.right
        };
    }

    protected override bool CanAttackPlayer(Vector3Int enemyCell, Vector3Int playerCell)
    {
        int dx = Mathf.Abs(enemyCell.x - playerCell.x);
        int dy = Mathf.Abs(enemyCell.y - playerCell.y);
        return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
    }
}
