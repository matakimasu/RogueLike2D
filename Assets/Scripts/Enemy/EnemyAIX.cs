using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemyAIX : EnemyAIBase
{
    protected override void Awake()
    {
        base.Awake();

        // �v���C���[Transform�������擾�i���S�̂��ߏd���`�F�b�N�j
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                playerTransform = playerObj.transform;
        }
    }

    // �ړ��������΂�4�����݂̂Ɍ���
    protected override Vector3Int[] GetMovementDirections()
    {
        return new Vector3Int[]
        {
            new Vector3Int( 1,  1, 0),   // �E��
            new Vector3Int( 1, -1, 0),   // �E��
            new Vector3Int(-1,  1, 0),   // ����
            new Vector3Int(-1, -1, 0)    // ����
        };
    }

    // �U��������΂߂̂݁i�΂�1�}�X���U���Ώہj
    protected override bool CanAttackPlayer(Vector3Int enemyCell, Vector3Int playerCell)
    {
        int dx = Mathf.Abs(enemyCell.x - playerCell.x);
        int dy = Mathf.Abs(enemyCell.y - playerCell.y);
        return (dx == 1 && dy == 1);
    }

    // �� GetAttackCellsPreview() �� EnemyAIBase ���̊��������OK
    //    �i��� GetMovementDirections / CanAttackPlayer ���g���Ď΂�4�������v���r���[���܂��j
}
