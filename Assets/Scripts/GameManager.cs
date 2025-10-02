using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public TurnState currentState { get; private set; }

    [Header("Player HP 管理")]
    public int maxHP = 20;
    private int currentHP;

    [Header("UI 参照")]
    public HPHud hpHud;
    public TurnBanner turnBanner;
    [SerializeField] private GameObject gameOverPanel;

    [Header("サウンド設定")]
    public AudioSource audioSource;
    public AudioClip damageSE;

    public bool inputLocked { get; private set; } = false;

    private readonly List<EnemyAIBase> enemyList = new List<EnemyAIBase>();
    private readonly List<TrapBaseSequenced> trapList = new List<TrapBaseSequenced>();

    [Header("罠の実行テンポ")]
    [SerializeField] private float trapStepDelay = 0.05f;
    [SerializeField] private float enemyToTrapDelay = 0.15f;
    [SerializeField] private float afterTrapsDelay = 0.20f;

    private bool _advancing = false;
    private int _cycleIndex = 1;
    private bool _firstPlayerBannerShown = false;

    public event System.Action OnRoundAdvanced;

    private bool isGameOver = false;
    public bool IsGameOver() => isGameOver;

    // === 追加: 1ターン1アクション制御 ===
    private bool actionConsumed = false; // 行動を消費済みか
    private bool actionBegun = false; // 行動“開始中”（先取りゲート）

    /// <summary>今ターン、行動を開始してよいか（未開始・未消費・プレイヤーターン・非ロック）</summary>
    public bool TryBeginAction()
    {
        if (currentState != TurnState.Player_Turn) return false;
        if (inputLocked) return false;
        if (actionConsumed) return false;
        if (actionBegun) return false; // ★ ここで多重発火をブロック

        actionBegun = true;
        return true;
    }

    /// <summary>行動を消費（ターン終了まで）（行動完了時に呼ぶ）</summary>
    public void ConsumeAction()
    {
        if (currentState != TurnState.Player_Turn) return;
        if (actionConsumed) return;

        actionConsumed = true;
        actionBegun = false;
        EndPlayerTurn();
    }

    private void ResetActionFlags()
    {
        actionConsumed = false;
        actionBegun = false;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        currentHP = maxHP;
        UpdateHPUI();

        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // 最初は Player ターンから
        StartCoroutine(SwitchPhase(TurnState.Player_Turn));
    }

    private void Update()
    {
        if (isGameOver && Input.GetKeyDown(KeyCode.R))
        {
            Retry();
        }
    }

    /// <summary>プレイヤーが行動（Move/Action）を消費したら呼ばれる</summary>
    public void EndPlayerTurn()
    {
        if (_advancing || isGameOver) return;
        StartCoroutine(SwitchPhase(TurnState.Enemy_Turn));
    }

    private IEnumerator SwitchPhase(TurnState next)
    {
        if (_advancing || isGameOver) yield break;
        _advancing = true;
        inputLocked = true;

        // バナー表示
        if (turnBanner != null)
        {
            if (next == TurnState.Player_Turn)
            {
                if (_firstPlayerBannerShown) _cycleIndex++;
                else _firstPlayerBannerShown = true;

                yield return turnBanner.ShowPlayer(_cycleIndex);
            }
            else if (next == TurnState.Enemy_Turn)
            {
                yield return turnBanner.ShowEnemy(_cycleIndex);
            }
        }
        else
        {
            yield return new WaitForSeconds(0.05f);
        }

        if (isGameOver) { _advancing = false; yield break; }

        currentState = next;

        // フェーズ開始時に罠の残ターン進行
        AdvanceTrapsForPhase(next);

        if (next == TurnState.Player_Turn)
        {
            ResetActionFlags(); // ★ プレイヤーターン開始でリセット
            OnRoundAdvanced?.Invoke();
            AttackTileDirector.Instance?.RefreshAll();
            inputLocked = false;
        }
        else if (next == TurnState.Enemy_Turn)
        {
            StartCoroutine(HandleEnemyTurn());
        }

        _advancing = false;
    }

    private void AdvanceTrapsForPhase(TurnState phase)
    {
        if (trapList.Count == 0) return;

        var snapshot = new List<TrapBaseSequenced>(trapList);
        foreach (var trap in snapshot)
        {
            if (trap == null) continue;
            trap.OnTurnAdvanced(phase);
        }

        AttackTileDirector.Instance?.RefreshTrapsOnly();
    }

    private IEnumerator HandleEnemyTurn()
    {
        inputLocked = true;

        if (isGameOver) yield break;

        if (enemyList.Count == 0)
        {
            if (enemyToTrapDelay > 0f) yield return new WaitForSeconds(enemyToTrapDelay);

            AttackTileDirector.Instance?.RefreshTrapsOnly();

            yield return HandleTrapsAfterEnemies();

            if (afterTrapsDelay > 0f) yield return new WaitForSeconds(afterTrapsDelay);

            if (!isGameOver) yield return SwitchPhase(TurnState.Player_Turn);
            yield break;
        }

        // 敵AI順次実行
        foreach (EnemyAIBase ai in new List<EnemyAIBase>(enemyList))
        {
            if (isGameOver) break;
            if (ai != null)
            {
                yield return StartCoroutine(ai.TryMoveTowardPlayer());
                AttackTileDirector.Instance?.RefreshEnemyOnly();
            }
        }

        if (isGameOver) yield break;

        if (enemyToTrapDelay > 0f) yield return new WaitForSeconds(enemyToTrapDelay);

        AttackTileDirector.Instance?.RefreshTrapsOnly();

        yield return HandleTrapsAfterEnemies();

        if (afterTrapsDelay > 0f) yield return new WaitForSeconds(afterTrapsDelay);

        if (!isGameOver) yield return SwitchPhase(TurnState.Player_Turn);
    }

    private IEnumerator HandleTrapsAfterEnemies()
    {
        if (trapList.Count == 0) yield break;

        var snapshot = new List<TrapBaseSequenced>(trapList);
        foreach (var trap in snapshot)
        {
            if (isGameOver) yield break;
            if (trap == null) continue;

            if (trap.ShouldActOnPhase(TurnState.Enemy_Turn) && trap.IsReadyToAct())
            {
                yield return trap.Act(TurnState.Enemy_Turn);

                AttackTileDirector.Instance?.RefreshTrapsOnly();

                if (trapStepDelay > 0f)
                    yield return new WaitForSeconds(trapStepDelay);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isGameOver) return;

        currentHP = Mathf.Max(currentHP - damage, 0);
        UpdateHPUI();

        var effect = FindObjectOfType<PlayerDamageEffect>();
        if (effect != null) effect.TakeDamageEffect();

        if (audioSource != null && damageSE != null)
            audioSource.PlayOneShot(damageSE);

        if (currentHP <= 0)
        {
            StartCoroutine(GameOverRoutine());
        }
    }

    public void Heal(int amount)
    {
        if (isGameOver) return;
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        UpdateHPUI();
    }

    private void UpdateHPUI()
    {
        if (hpHud != null) hpHud.Refresh(currentHP, maxHP);
    }

    public void RegisterEnemy(EnemyAIBase enemy)
    {
        if (!enemyList.Contains(enemy)) enemyList.Add(enemy);
    }

    public void UnregisterEnemy(EnemyAIBase enemy)
    {
        if (enemyList.Contains(enemy)) enemyList.Remove(enemy);
    }

    public List<EnemyAIBase> GetEnemyList() => enemyList;

    public void RegisterTrap(TrapBaseSequenced t)
    {
        if (t != null && !trapList.Contains(t)) trapList.Add(t);
    }

    public void UnregisterTrap(TrapBaseSequenced t)
    {
        if (t != null) trapList.Remove(t);
    }

    private IEnumerator GameOverRoutine()
    {
        if (isGameOver) yield break;
        isGameOver = true;
        inputLocked = true;

        var player = GameObject.FindWithTag("Player");
        if (player)
        {
            var anim = player.GetComponent<Animator>();
            if (anim && HasAnimatorParam(anim, "Die"))
                anim.SetTrigger("Die");
        }

        yield return new WaitForSecondsRealtime(0.8f);

        Time.timeScale = 0f;

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    private bool HasAnimatorParam(Animator animator, string name)
    {
        foreach (var p in animator.parameters)
            if (p.name == name) return true;
        return false;
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        var idx = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(idx);
    }
}
