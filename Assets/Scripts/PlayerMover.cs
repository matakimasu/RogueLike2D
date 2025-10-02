using UnityEngine;
using System.Collections;
using UnityEngine.Tilemaps;

public class PlayerMover : MonoBehaviour
{
    public float moveTime = 0.1f;
    public float turnDelay = 0.2f;

    private bool isMoving = false;
    private Vector3 targetPos;

    public Tilemap tilemap;
    public LayerMask obstacleLayer;    // 壁/進入禁止物
    public LayerMask attackableLayer;  // 攻撃対象

    [Header("攻撃設定")]
    public int attackPower = 3;

    [Header("サウンド設定")]
    public AudioSource audioSource;
    public AudioClip attackSE;
    public AudioClip footstepSE;

    [Header("Click FX（成立セルで1回だけ発火）")]
    [SerializeField] private ClickFlashFX clickFxPrefab;              // ★ ここに ClickFlashFX プレハブを割当
    public string fxSortingLayerName = "Overlays";
    public int fxSortingOrder = 600;
    public Color moveFlashColor = new Color(0.20f, 0.90f, 1f, 1f);
    public Color attackFlashColor = new Color(1f, 0.25f, 0.2f, 1f);
    public Color stayFlashColor = new Color(0.80f, 0.80f, 0.80f, 1f); // 自セルクリック時など

    private Animator animator;
    private bool actionLocked = false;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.inputLocked) return;
        if (GameManager.Instance.currentState != TurnState.Player_Turn) return;
        if (tilemap == null) return;
        if (isMoving || actionLocked) return;

        if (Input.GetMouseButtonDown(0))
        {
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 mp = Input.mousePosition;
            mp.z = Mathf.Abs(cam.transform.position.z);
            Vector3 mouseWorld = cam.ScreenToWorldPoint(mp);
            mouseWorld.z = 0f;

            Vector3Int clickedCell = tilemap.WorldToCell(mouseWorld);
            Vector3Int currentCell = tilemap.WorldToCell(transform.position);
            Vector3Int diff = clickedCell - currentCell;

            // ★ 先取りゲート：ここで同フレーム連打をブロック
            if (!GameManager.Instance.TryBeginAction()) return;

            // 自セルクリック → その場でターン消費（=行動成立とみなす）
            if (diff == Vector3Int.zero)
            {
                Vector3 center = tilemap.GetCellCenterWorld(clickedCell);
                SpawnClickFX(center, stayFlashColor);
                GameManager.Instance.ConsumeAction();
                return;
            }

            // 1マス以内のみ
            bool isNeighbor = (Mathf.Abs(diff.x) <= 1 && Mathf.Abs(diff.y) <= 1);
            if (!isNeighbor)
            {
                CancelBegunAction();
                Debug.Log("遠すぎて何もできない");
                return;
            }

            Vector3 cellCenter = tilemap.GetCellCenterWorld(clickedCell);

            // ===== 攻撃優先 =====
            Collider2D hitAttackable = Physics2D.OverlapBox(cellCenter, new Vector2(0.9f, 0.9f), 0f, attackableLayer);
            if (hitAttackable != null)
            {
                // 行動成立：このセルで1回だけFX
                SpawnClickFX(cellCenter, attackFlashColor);
                StartCoroutine(PerformAttack(clickedCell));
                return;
            }

            // ===== 移動（床あり & 障害物なし） =====
            if (tilemap.HasTile(clickedCell))
            {
                bool blocked = Physics2D.OverlapBox(cellCenter, new Vector2(0.9f, 0.9f), 0f, obstacleLayer);
                if (!blocked)
                {
                    // 行動成立：このセルで1回だけFX
                    SpawnClickFX(cellCenter, moveFlashColor);
                    StartCoroutine(MoveToPosition(cellCenter));
                    return;
                }

                CancelBegunAction();
                Debug.Log("そのマスに障害物があるため移動不可");
                return;
            }

            CancelBegunAction();
            Debug.Log("そのマスには移動も攻撃もできない");
        }
    }

    // ★ 成立セルでのみ呼ぶ：ClickFlashFX を1回だけ再生
    private void SpawnClickFX(Vector3 pos, Color tint)
    {
        if (clickFxPrefab == null) return; // 未割当なら何もしない
        var fx = Instantiate(clickFxPrefab, pos, Quaternion.identity);
        fx.Play(pos, tint, fxSortingLayerName, fxSortingOrder);
    }

    // 行動開始を取り消す（TryBeginAction成功後の不発ケース）
    private void CancelBegunAction()
    {
        StartCoroutine(_CancelGateNextFrame());
    }
    private IEnumerator _CancelGateNextFrame()
    {
        actionLocked = true;    // そのフレームは入力を食わない
        yield return null;      // 次フレーム
        actionLocked = false;
    }

    private IEnumerator MoveToPosition(Vector3 target)
    {
        isMoving = true;
        actionLocked = true;

        Vector3 start = transform.position;
        targetPos = target;
        float elapsed = 0;

        while (elapsed < moveTime)
        {
            transform.position = Vector3.Lerp(start, target, elapsed / moveTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = target;
        isMoving = false;
        actionLocked = false;

        if (audioSource != null && footstepSE != null)
        {
            StartCoroutine(PlayFootstepSounds());
        }

        // 移動成功 → 行動消費してターン終了
        GameManager.Instance.ConsumeAction();
    }

    private IEnumerator PlayFootstepSounds()
    {
        int stepCount = 2;
        float interval = 0.3f;

        for (int i = 0; i < stepCount; i++)
        {
            audioSource.PlayOneShot(footstepSE);
            yield return new WaitForSeconds(interval);
        }
    }

    private void ResetToIdleWithDelay()
    {
        if (animator != null) animator.SetTrigger("ToIdle");
        StartCoroutine(DelayEndTurn());
    }

    private IEnumerator DelayEndTurn()
    {
        yield return new WaitForSeconds(turnDelay);
        GameManager.Instance.ConsumeAction();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(targetPos, new Vector2(0.9f, 0.9f));
    }

    private IEnumerator PerformAttack(Vector3Int clickedCell)
    {
        actionLocked = true;

        var camCtrl = FindObjectOfType<GridCameraController>();
        bool camFound = camCtrl != null;
        if (camFound) camCtrl.followEnabled = false;

        try
        {
            Vector3 attackTarget = tilemap.GetCellCenterWorld(clickedCell);

            Collider2D hit = Physics2D.OverlapBox(
                attackTarget,
                new Vector2(0.9f, 0.9f),
                0f,
                attackableLayer
            );

            var lunge = GetComponent<PlayerAttackLunge>();
            if (hit != null && lunge != null)
            {
                yield return StartCoroutine(lunge.PlayTowardCell(clickedCell, tilemap));
            }

            if (hit != null && audioSource != null && attackSE != null)
            {
                audioSource.PlayOneShot(attackSE);
            }

            if (hit != null)
            {
                if (hit.TryGetComponent<EnemyHealth>(out var enemy))
                {
                    enemy.TakeDamage(attackPower);
                    Debug.Log($"敵に{attackPower}ダメージを与えた");
                }
                else if (hit.TryGetComponent<TrapBaseSequenced>(out var trap))
                {
                    trap.ForceArm();
                    Debug.Log("罠を攻撃して起動させた！");
                }
            }
            else
            {
                Debug.Log("攻撃範囲に何もなし");
            }
        }
        finally
        {
            if (camFound) camCtrl.followEnabled = true;
            ResetToIdleWithDelay(); // ConsumeAction は DelayEndTurn() 内で呼ぶ
            actionLocked = false;
        }
    }

    public string GetCurrentActionMode()
    {
        return "Attack";
    }
}
