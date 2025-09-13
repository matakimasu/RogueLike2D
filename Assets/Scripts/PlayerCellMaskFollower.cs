using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// �v���C���[�̌��݃Z���́u�Z�����S�v�� SpriteMask ���X�i�b�v�Ǐ]������B
/// �����W�ȂǂŃv���C���[�����S����O��Ă���Ԃ͈ړ����Ȃ����߁A�׃}�X�������Ȃ��B
/// </summary>
[ExecuteAlways]
public class PlayerCellMaskFollower : MonoBehaviour
{
    [Header("�Q��")]
    public Transform player;        // �v���C���[��Transform�i�q�ɂ��Ȃ��j
    public Tilemap floorTilemap;    // ��Tilemap�iWorld<->Cell�ϊ��Ɏg�p�j

    [Header("����")]
    [Tooltip("�v���C���[���Z�����S�ɏ\���߂����̂݃X�i�b�v����臒l�i���[���h�����j")]
    public float snapEpsilon = 0.02f;

    [Tooltip("�G�f�B�^��~������Ɍ��݃Z�����S�ɍ��킹�����ꍇON�iScene��ňʒu�m�F�p�j")]
    public bool snapInEditMode = true;

    // ���߂ŃX�i�b�v�����ʒu�i�C�Ӂj
    private Vector3 _lastSnapped;

    private void Reset()
    {
        // ��������
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

        // �v���C���[�ʒu���猻�݃Z���ƁA���̒��S���[���h���W�����߂�
        Vector3Int cell = floorTilemap.WorldToCell(player.position);
        Vector3 cellCenter = floorTilemap.GetCellCenterWorld(cell);

        // �v���C���[���Z�����S�ɏ\���߂��������X�i�b�v
        float dist = Vector3.Distance(player.position, cellCenter);
        if (dist <= snapEpsilon)
        {
            if (_lastSnapped != cellCenter)
            {
                transform.position = cellCenter;
                _lastSnapped = cellCenter;
            }
        }
        // �߂��Ȃ��Ԃ́u�O��̃X�i�b�v�ʒu�v�ɗ��܂�i�������W���͌Œ�j
    }
}
