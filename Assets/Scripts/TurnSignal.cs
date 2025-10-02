using UnityEngine;

public class TurnSignal : MonoBehaviour
{
    // 現在のフェーズが変化したときに通知（引数 = 新しい状態）
    public static System.Action<TurnState> OnPhaseChanged;

    // プレイヤーターン開始通知
    public static System.Action OnPlayerTurnStart;

    // 敵ターン開始通知
    public static System.Action OnEnemyTurnStart;

    private TurnState _last;

    void Start()
    {
        if (GameManager.Instance != null)
            _last = GameManager.Instance.currentState;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        var now = gm.currentState;
        if (now == _last) return;

        // フェーズが変わったらイベントを発火
        OnPhaseChanged?.Invoke(now);

        // 状態に応じて専用イベントを発火
        if (now == TurnState.Player_Turn)
            OnPlayerTurnStart?.Invoke();
        else if (now == TurnState.Enemy_Turn)
            OnEnemyTurnStart?.Invoke();

        _last = now;
    }
}
