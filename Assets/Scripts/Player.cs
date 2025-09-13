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
    public LayerMask obstacleLayer;
    public LayerMask attackableLayer;

    [Header("攻撃設定")]
    public int attackPower = 3;

    [Header("サウンド設定")]
    public AudioSource audioSource;
    public AudioClip attackSE;
    public AudioClip footstepSE;

    private enum ActionMode { Search, Attack }
    private ActionMode currentActionMode = ActionMode.Search;

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

        if (GameManager.Instance.currentState != TurnState.Player_Move &&
            GameManager.Instance.currentState != TurnState.Player_Action)
            return;

        if (isMoving) return;

        // --- Player_Move ---
        if (GameManager.Instance.currentState == TurnState.Player_Move &&
            Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0;

            Vector3Int clickedCell = tilemap.WorldToCell(mouseWorld);
            Vector3Int currentCell = tilemap.WorldToCell(transform.position);
            Vector3Int diff = clickedCell - currentCell;

            if (Mathf.Abs(diff.x) <= 1 && Mathf.Abs(diff.y) <= 1)
            {
                if (!tilemap.HasTile(clickedCell)) return;

                Vector3 moveTarget = tilemap.GetCellCenterWorld(clickedCell);

                if (diff == Vector3Int.zero)
                {
                    GameManager.Instance.NextState();
                    return;
                }

                Collider2D hit = Physics2D.OverlapBox(moveTarget, new Vector2(0.9f, 0.9f), 0f, obstacleLayer);
                if (hit != null)
                {
                    Debug.Log("そのマスに障害物があるため移動不可");
                    return;
                }

                StartCoroutine(MoveToPosition(moveTarget));
                return;
            }
        }

        // --- Player_Action ---
        if (GameManager.Instance.currentState == TurnState.Player_Action)
        {
            if (actionLocked) return;

            // 右クリックでモード切替（Search ? Attack）
            if (Input.GetMouseButtonDown(1))
            {
                currentActionMode = (currentActionMode == ActionMode.Search) ? ActionMode.Attack : ActionMode.Search;
                ApplyCurrentActionAnimation();
            }

            // 左クリックで実行
            if (Input.GetMouseButtonDown(0))
            {
                // ★ 修正: mousePosition（Pは大文字）
                Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mouseWorld.z = 0;

                Vector3Int clickedCell = tilemap.WorldToCell(mouseWorld);
                Vector3Int currentCell = tilemap.WorldToCell(transform.position);
                Vector3Int diff = clickedCell - currentCell;

                if (currentActionMode == ActionMode.Search)
                {
                    var thrower = GetComponent<Thrower>();
                    if (thrower != null)
                    {
                        var bullet = thrower.FireAndReturnBullet(mouseWorld);
                        if (animator != null) animator.SetTrigger("ToIdle");

                        if (bullet != null)
                        {
                            actionLocked = true;
                            bullet.OnFinished += () =>
                            {
                                actionLocked = false;
                                GameManager.Instance.NextState();
                            };
                        }
                        else
                        {
                            StartCoroutine(DelayNextStateAndUnlock());
                        }
                    }
                    else
                    {
                        StartCoroutine(DelayNextStateAndUnlock());
                    }
                }
                else // Attack
                {
                    if (Mathf.Abs(diff.x) <= 1 && Mathf.Abs(diff.y) <= 1 && !(diff.x == 0 && diff.y == 0))
                    {
                        StartCoroutine(PerformAttack(clickedCell));
                    }
                    else
                    {
                        Debug.Log("攻撃範囲外です");
                    }
                }
            }
        }
    }

    public void ApplyCurrentActionAnimation()
    {
        if (animator != null)
        {
            switch (currentActionMode)
            {
                case ActionMode.Search:
                    animator.SetTrigger("ToSearch");
                    break;
                case ActionMode.Attack:
                    animator.SetTrigger("ToAttack");
                    break;
            }
        }
    }

    private IEnumerator MoveToPosition(Vector3 target)
    {
        isMoving = true;
        Vector3 start = transform.position;
        float elapsed = 0;

        while (elapsed < moveTime)
        {
            transform.position = Vector3.Lerp(start, target, elapsed / moveTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = target;
        isMoving = false;

        if (audioSource != null && footstepSE != null)
        {
            StartCoroutine(PlayFootstepSounds());
        }

        GameManager.Instance.NextState();
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

    private IEnumerator DelayNextStateAndUnlock()
    {
        actionLocked = true;
        yield return new WaitForSeconds(turnDelay);
        actionLocked = false;
        GameManager.Instance.NextState();
    }

    private void ResetToIdleWithDelay()
    {
        if (animator != null) animator.SetTrigger("ToIdle");
        StartCoroutine(DelayNextStateToEnemyTurn());
    }

    private IEnumerator DelayNextStateToEnemyTurn()
    {
        yield return new WaitForSeconds(turnDelay);
        GameManager.Instance.NextState();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(targetPos, new Vector2(0.9f, 0.9f));
    }

    private IEnumerator PerformAttack(Vector3Int clickedCell)
    {
        actionLocked = true;

        var cam = FindObjectOfType<CameraController>();
        bool camFound = cam != null;
        if (camFound) cam.followEnabled = false;

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
                if (hit.CompareTag("Bullet"))
                {
                    Destroy(hit.gameObject);
                }
                else if (hit.TryGetComponent<EnemyHealth>(out var enemy))
                {
                    enemy.TakeDamage(attackPower);
                }
            }
        }
        finally
        {
            if (camFound) cam.followEnabled = true;
            ResetToIdleWithDelay();
            actionLocked = false;
        }
    }

    // ★ 追加：TileHighlighter 用のアクセサ
    public string GetCurrentActionMode()
    {
        return currentActionMode.ToString(); // "Search" or "Attack"
    }
}
