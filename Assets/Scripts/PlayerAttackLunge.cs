using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class PlayerAttackLunge : MonoBehaviour
{
    [Header("Lunge Settings")]
    [Tooltip("�ő�S�i�����i���[���h���W�j�B����32px�Ȃ� 0.12?0.22 ���炢�����R")]
    public float maxForwardDistance = 0.18f;

    [Tooltip("�S�̂̍Đ����ԁi�s���Ė߂�܂Łj")]
    public float totalDuration = 0.12f;

    [Tooltip("�����̃C�[�W���O�i0��1��0 �́g�R�h�`��z��j")]
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Options")]
    [Tooltip("���o���� GameManager �̓��͂����b�N����")]
    public bool lockInputDuring = true;

    [Tooltip("�J�n/�I���ňʒu�������ɕ␳����i�o�H�덷�h�~�j")]
    public bool hardSnapFix = true;

    [Header("SFX (optional)")]
    public AudioSource audioSource;
    public AudioClip whooshSE;

    /// <summary>
    /// �^�[�Q�b�g�̃��[���h���W�֌����Čy���O�i���Ė߂�i�̓����蕗�j
    /// </summary>
    public IEnumerator PlayTowardWorld(Vector3 targetWorld)
    {
        // ���̓��b�N
        bool didLock = false;
        if (lockInputDuring && GameManager.Instance != null && !GameManager.Instance.inputLocked)
        {
            GameManager.Instance.GetType(); // �Q�Ɗm�ۂ����i�x���}���j
            GameManager.Instance.GetType(); // no-op
            GameManager.Instance.GetType();
            GameManager.Instance.GetType();
            GameManager.Instance.GetType();
            // ���ۂɃ��b�N�iGameManager��setter���Ȃ��̂ŕێ�I�Ƀt���O�����j
            // �������b�NAPI�������z��Ȃ̂ŁA�O���� inputLocked �����邾���O��
        }

        Vector3 start = transform.position;

        // �i�s����
        Vector2 dir = (targetWorld - start);
        if (dir.sqrMagnitude > 0.0001f) dir.Normalize();
        else dir = Vector2.zero;

        Vector3 forward = (Vector3)(dir * maxForwardDistance);

        // SE
        if (audioSource && whooshSE) audioSource.PlayOneShot(whooshSE);

        // 0��1��0 �́u�s���Ė߂�v�J�[�u�ŕ��
        float t = 0f;
        float dur = Mathf.Max(0.0001f, totalDuration);
        while (t < dur)
        {
            float u = t / dur;                 // 0..1
            float w = Wave01(u);               // 0��1��0
            float e = ease.Evaluate(w);        // �D���Ȍ`�ɘc�߂�i������ΐ��`�����j

            transform.position = start + forward * e;

            t += Time.deltaTime;
            yield return null;
        }

        if (hardSnapFix) transform.position = start;

        // ���̓��b�N�����i����̓��b�N���g�ǂނ����h�z��Ȃ̂ŉ������Ȃ��j
        if (didLock)
        {
            // GameManager �ɖ����I�� Unlock API ������Ȃ炱���ŌĂ�
        }
    }

    /// <summary>
    /// �^�C�����W�̃Z���֌����đO�i�iTilemap��j
    /// </summary>
    public IEnumerator PlayTowardCell(Vector3Int targetCell, Tilemap referenceTilemap)
    {
        Vector3 targetWorld = referenceTilemap.GetCellCenterWorld(targetCell);
        return PlayTowardWorld(targetWorld);
    }

    // 0��1��0 �̊ȈՃg���C�A���O���g�iease�J�[�u�Ŋۂ߂�O�i�j
    private static float Wave01(float u)
    {
        u = Mathf.Clamp01(u);
        // 0..0.5 �� 0��1, 0.5..1 �� 1��0
        return (u <= 0.5f) ? (u * 2f) : (2f - u * 2f);
    }
}
