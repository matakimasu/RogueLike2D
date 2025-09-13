using UnityEngine;

public class TurnSignal : MonoBehaviour
{
    public static System.Action<TurnState> OnPhaseChanged;
    public static System.Action OnPlayerTurnStart;
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

        OnPhaseChanged?.Invoke(now);
        if (now == TurnState.Player_Move) OnPlayerTurnStart?.Invoke();
        if (now == TurnState.Enemy_Turn) OnEnemyTurnStart?.Invoke();

        _last = now;
    }
}
