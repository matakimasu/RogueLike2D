using UnityEngine;
using System;

public class EnemyHealth : MonoBehaviour
{
    [Header("HP設定")]
    [Min(1)] public int maxHP = 5;

    [SerializeField] private int currentHP;

    // ★ HP変更を通知（cur, max）
    public event Action<int, int> OnHPChanged;

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;

    private void Awake()
    {
        if (currentHP <= 0) currentHP = maxHP;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        currentHP = Mathf.Max(currentHP - amount, 0);
        OnHPChanged?.Invoke(currentHP, maxHP);

        GetComponent<EnemyAIBase>()?.PlayDamageFlash();

        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    private void Die()
    {
        var ai = GetComponent<EnemyAIBase>();
        if (GameManager.Instance != null && ai != null)
        {
            GameManager.Instance.UnregisterEnemy(ai);
        }
        Destroy(gameObject);
    }
}
