using UnityEngine;
using System.Collections;

public class PlayerDamageEffect : MonoBehaviour
{
    [Header("無敵時間設定")]
    public float invincibleTime = 1.0f; // ダメージ後の無敵時間
    public float flashInterval = 0.1f;  // 点滅間隔

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

            // カメラシェイク（CameraControllerを使用）
            CameraController cam = FindObjectOfType<CameraController>();
            if (cam != null)
            {
                cam.Shake(0.2f, 0.15f); // 0.2秒間、0.15の強さで揺らす
            }
        }
    }

    private IEnumerator DamageFlash()
    {
        isInvincible = true;

        float elapsed = 0f;
        while (elapsed < invincibleTime)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled; // ON/OFF切り替え
            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        spriteRenderer.enabled = true; // 最後は表示状態に戻す
        isInvincible = false;
    }
}
