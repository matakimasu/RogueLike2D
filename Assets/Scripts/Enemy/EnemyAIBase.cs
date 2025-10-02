using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public abstract class EnemyAIBase : MonoBehaviour
{
    [Header("参照設定")]
    [SerializeField] protected Tilemap tilemap;       // ← Floor 用（既存フィールドを継続利用）
    [SerializeField] protected Tilemap wallTilemap;   // ← 追加：Wall 用
    [SerializeField] protected Transform playerTransform;

    [Header("Layer設定")]
    [SerializeField] protected LayerMask obstacleLayer; // 壁・進入禁止物など（コライダーベース）
    [SerializeField] protected LayerMask playerLayer;   // プレイヤーの占有セル判定用（踏み込み防止）

    [Header("移動設定")]
    public float moveSpeed = 3.0f;

    [Header("攻撃設定")]
    public int attackPower = 1;

    [Header("ダメージ演出設定")]
    public float flashDuration = 0.2f;
    public float flashInterval = 0.05f;

    [Header("効果音設定")]
    public AudioSource audioSource;
    public AudioClip moveSE;
    public AudioClip attackSE;

    [Header("演出（ルンジ）設定")]
    [Tooltip("プレイヤー攻撃時に前進→戻る演出を行う")]
    public bool useLungeOnAttack = true;

    protected SpriteRenderer spriteRenderer;

    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }
    }

    protected virtual void Start()
    {
        if (tilemap != null)
        {
            Vector3Int cell = tilemap.WorldToCell(transform.position);
            transform.position = tilemap.GetCellCenterWorld(cell);
        }

        GameManager.Instance?.RegisterEnemy(this);
    }

    /// <summary>
    /// 優先順位：
    /// 1) プレイヤーを攻撃できるなら攻撃
    /// 2) 攻撃できない → プレイヤーに近づく移動を試みる（Floor のみ、Wall 不可、障害物/プレイヤー占有も不可）
    /// </summary>
    public virtual IEnumerator TryMoveTowardPlayer()
    {
        if (playerTransform == null || tilemap == null) yield break;

        Vector3Int currentCell = tilemap.WorldToCell(transform.position);
        Vector3Int playerCell = tilemap.WorldToCell(playerTransform.position);

        // ===== 1) 近接攻撃可能？ =====
        if (CanAttackPlayer(currentCell, playerCell))
        {
            // ルンジ演出
            if (useLungeOnAttack)
            {
                var lunge = GetComponent<PlayerAttackLunge>();
                if (lunge != null)
                {
                    Vector3 playerCenter = tilemap.GetCellCenterWorld(playerCell);
                    yield return StartCoroutine(lunge.PlayTowardWorld(playerCenter));
                }
            }

            // 攻撃SE
            if (audioSource != null && attackSE != null)
                audioSource.PlayOneShot(attackSE);

            // ダメージ適用
            GameManager.Instance.TakeDamage(attackPower);

            yield return new WaitForSeconds(0.2f);
            yield break;
        }

        // ===== 2) 追従移動（距離が縮む方向を貪欲に選択） =====
        Vector3Int[] directions = GetMovementDirections();
        foreach (Vector3Int dir in directions)
        {
            Vector3Int nextCell = currentCell + dir;

            // --- ★ Floor / Wall 判定（最優先で弾く） ---
            bool hasFloor = tilemap.HasTile(nextCell);
            bool isWall = (wallTilemap != null) && wallTilemap.HasTile(nextCell);
            if (!hasFloor || isWall) continue;  // Floor じゃない or Wall にタイルがある → 進入不可

            Vector3 worldPos = tilemap.GetCellCenterWorld(nextCell);
            Vector2 checkPoint = new Vector2(worldPos.x, worldPos.y);

            // 進入不可チェック（物理 & プレイヤー占有）
            if (Physics2D.OverlapCircle(checkPoint, 0.1f, playerLayer)) continue;   // プレイヤー位置は不可
            if (Physics2D.OverlapPoint(checkPoint, obstacleLayer)) continue;        // コライダー障害物は不可

            float currentDist = Vector3.SqrMagnitude((Vector3)(currentCell - playerCell));
            float newDist = Vector3.SqrMagnitude((Vector3)(nextCell - playerCell));

            if (newDist < currentDist)
            {
                yield return StartCoroutine(MoveToPosition(worldPos));
                yield break;
            }
        }
    }

    // ===== 抽象メソッド =====
    protected abstract Vector3Int[] GetMovementDirections();
    protected abstract bool CanAttackPlayer(Vector3Int enemyCell, Vector3Int playerCell);

    // ===== 移動処理 =====
    protected virtual IEnumerator MoveToPosition(Vector3 target)
    {
        if (audioSource != null && moveSE != null)
            audioSource.PlayOneShot(moveSE);

        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
    }

    // ===== ダメージ演出 =====
    public void PlayDamageFlash()
    {
        StartCoroutine(DamageFlashRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        if (spriteRenderer == null) yield break;

        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        spriteRenderer.enabled = true;
    }

    // ===== 攻撃範囲プレビュー（AttackTileDirector用） =====
    public virtual IEnumerable<Vector3Int> GetAttackCellsPreview()
    {
        if (tilemap == null) yield break;

        Vector3Int currentCell = tilemap.WorldToCell(transform.position);
        foreach (var dir in GetMovementDirections())
        {
            Vector3Int checkCell = currentCell + dir;
            if (CanAttackPlayer(currentCell, checkCell))
                yield return checkCell;
        }
    }
}
