using UnityEngine;
using System.Collections;

public class PlayerDamageEffect : MonoBehaviour
{
    [Header("���G���Ԑݒ�")]
    public float invincibleTime = 1.0f; // �_���[�W��̖��G����
    public float flashInterval = 0.1f;  // �_�ŊԊu

    private SpriteRenderer spriteRenderer;
    private bool isInvincible = false;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void TakeDamageEffect()
    {
        if (!isInvincible)
        {
            StartCoroutine(DamageFlash());

            // �J�����V�F�C�N�iCameraController���g�p�j
            CameraController cam = FindObjectOfType<CameraController>();
            if (cam != null)
            {
                cam.Shake(0.2f, 0.15f); // 0.2�b�ԁA0.15�̋����ŗh�炷
            }
        }
    }

    private IEnumerator DamageFlash()
    {
        isInvincible = true;

        float elapsed = 0f;
        while (elapsed < invincibleTime)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled; // ON/OFF�؂�ւ�
            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        spriteRenderer.enabled = true; // �Ō�͕\����Ԃɖ߂�
        isInvincible = false;
    }
}
