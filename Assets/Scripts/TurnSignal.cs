using UnityEngine;

public class TurnSignal : MonoBehaviour
{
    // ���݂̃t�F�[�Y���ω������Ƃ��ɒʒm�i���� = �V������ԁj
    public static System.Action<TurnState> OnPhaseChanged;

    // �v���C���[�^�[���J�n�ʒm
    public static System.Action OnPlayerTurnStart;

    // �G�^�[���J�n�ʒm
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

        // �t�F�[�Y���ς������C�x���g�𔭉�
        OnPhaseChanged?.Invoke(now);

        // ��Ԃɉ����Đ�p�C�x���g�𔭉�
        if (now == TurnState.Player_Turn)
            OnPlayerTurnStart?.Invoke();
        else if (now == TurnState.Enemy_Turn)
            OnEnemyTurnStart?.Invoke();

        _last = now;
    }
}
