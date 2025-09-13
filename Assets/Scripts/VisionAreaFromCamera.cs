using UnityEngine;

/// <summary>
/// �J�����̌��݂̉������`�icam.rect ���l���j�� BoxCollider2D �𓯊�����B
/// Layer �� "Vision" �ɐݒ肵�Ă������ƁB
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class VisionAreaFromCamera : MonoBehaviour
{
    public Camera cam;             // 1:1 �r���[�|�[�g���g���Ă���J����
    public Transform follow;       // ���Ƃ��� cam.transform�i�J�����̈ʒu�ɒu���j
    public float padding = 0f;     // �]���𑫂������ꍇ�i���[���h�P�ʁj

    private BoxCollider2D box;

    void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        if (cam == null) cam = Camera.main;
        if (follow == null && cam != null) follow = cam.transform;
    }

    void LateUpdate()
    {
        if (cam == null || follow == null) return;

        // 1) �J�����̎����A�X�y�N�g�icam.rect �𔽉f�A1:1�Ȃ��� 1�j
        float aspect = cam.aspect;

        // 2) ���݂̉��T�C�Y�i�I�[�\�T�C�Y�͏c�̔����j
        float worldHeight = cam.orthographicSize * 2f;
        float worldWidth = worldHeight * aspect;

        // 3) �����`�r���[�|�[�g�iaspect=1�j��z�肵�Ă���Ȃ�AworldWidth �� worldHeight �ƈ�v
        //    ����ȊO�ł� cam.rect �x�[�X�� aspect �Ő������T�C�Y�ɂȂ�

        // 4) �R���C�_�[�֔��f
        box.size = new Vector2(worldWidth + padding * 2f, worldHeight + padding * 2f);
        box.offset = Vector2.zero;

        // 5) �ʒu���J�����ɍ��킹��
        transform.position = new Vector3(follow.position.x, follow.position.y, 0f);
    }
}
