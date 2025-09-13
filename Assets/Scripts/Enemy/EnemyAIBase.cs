using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public abstract class EnemyAIBase : MonoBehaviour
{
    [Header("�Q�Ɛݒ�")]
    [SerializeField] protected Tilemap tilemap;
    [SerializeField] protected Transform playerTransform;

    [Header("Layer�ݒ�")]
    [SerializeField] protected LayerMask obstacleLayer;
    [SerializeField] protected LayerMask bulletLayer;
    [SerializeField] protected LayerMask playerLayer;

    [Header("�ړ��ݒ�")]
    public float moveSpeed = 3.0f;

    [Header("�U���ݒ�")]
    public int attackPower = 1;

    [Header("�_���[�W���o�ݒ�")]
    public float flashDuration = 0.2f;
    public float flashInterval = 0.05f;

    [Header("���ʉ��ݒ�")]
    public AudioSource audioSource;
    public AudioClip bulletBreakSE;
    public AudioClip moveSE;
    public AudioClip attackSE;   // �� �ǉ��F�G�̍U��SE

    [Header("���o�i�����W�j�ݒ�")]
    [Tooltip("�v���C���[�U�����ɑO�i���߂鉉�o���s��")]
    public bool useLungeOnAttack = true;
    [Tooltip("�e��j�󂷂鎞�ɑO�i���߂鉉�o���s��")]
    public bool useLungeOnBulletBreak = true;

    protected SpriteRenderer spriteRenderer;

    private bool _didDestroyBulletThisTurn = false;

    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                playerTransform = playerObj.transform;
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

    public virtual IEnumerator TryMoveTowardPlayer()
    {
        if (playerTransform == null || tilemap == null) yield break;

        Vector3Int currentCell = tilemap.WorldToCell(transform.position);
        Vector3Int playerCell = tilemap.WorldToCell(playerTransform.position);

        // ===== �ߐڍU���\�H =====
        if (CanAttackPlayer(currentCell, playerCell))
        {
            // �����W���o
            if (useLungeOnAttack)
            {
                var lunge = GetComponent<PlayerAttackLunge>();
                if (lunge != null)
                {
                    Vector3 playerCenter = tilemap.GetCellCenterWorld(playerCell);
                    yield return StartCoroutine(lunge.PlayTowardWorld(playerCenter));
                }
            }

            // �U��SE
            if (audioSource != null && attackSE != null)
                audioSource.PlayOneShot(attackSE);

            // �_���[�W�K�p
            GameManager.Instance.TakeDamage(attackPower);

            yield return new WaitForSeconds(0.2f);
            yield break;
        }

        // ===== �e�j��`�F�b�N =====
        yield return StartCoroutine(TryDestroyBulletRoutine(currentCell));
        if (_didDestroyBulletThisTurn) yield break;

        // ===== �ړ����� =====
        Vector3Int[] directions = GetMovementDirections();
        foreach (Vector3Int dir in directions)
        {
            Vector3Int nextCell = currentCell + dir;
            Vector3 worldPos = tilemap.GetCellCenterWorld(nextCell);
            Vector2 checkPoint = new Vector2(worldPos.x, worldPos.y);

            if (Physics2D.OverlapCircle(checkPoint, 0.1f, playerLayer)) continue;
            if (Physics2D.OverlapPoint(checkPoint, obstacleLayer)) continue;
            if (Physics2D.OverlapPoint(checkPoint, bulletLayer)) continue;

            float currentDist = Vector3.Distance(currentCell, playerCell);
            float newDist = Vector3.Distance(nextCell, playerCell);
            if (newDist < currentDist)
            {
                yield return StartCoroutine(MoveToPosition(worldPos));
                yield break;
            }
        }
    }

    // ===== �e�j�󏈗� =====
    protected virtual IEnumerator TryDestroyBulletRoutine(Vector3Int currentCell)
    {
        _didDestroyBulletThisTurn = false;

        Vector3Int[] directions = GetMovementDirections();

        foreach (Vector3Int dir in directions)
        {
            Vector3Int checkCell = currentCell + dir;
            Vector3 worldPos = tilemap.GetCellCenterWorld(checkCell);
            Vector2 checkPoint = new Vector2(worldPos.x, worldPos.y);

            Collider2D bulletHit = Physics2D.OverlapPoint(checkPoint, bulletLayer);
            if (bulletHit != null)
            {
                // �����W���o
                if (useLungeOnBulletBreak)
                {
                    var lunge = GetComponent<PlayerAttackLunge>();
                    if (lunge != null)
                    {
                        yield return StartCoroutine(lunge.PlayTowardWorld(worldPos));
                    }
                }

                // �j��SE
                if (audioSource != null && bulletBreakSE != null)
                    audioSource.PlayOneShot(bulletBreakSE);

                Destroy(bulletHit.gameObject);

                _didDestroyBulletThisTurn = true;
                yield return new WaitForSeconds(0.2f);
                yield break;
            }
        }
    }

    // ===== ���ۃ��\�b�h =====
    protected abstract Vector3Int[] GetMovementDirections();
    protected abstract bool CanAttackPlayer(Vector3Int enemyCell, Vector3Int playerCell);

    // ===== �ړ����� =====
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

    // ===== �_���[�W���o =====
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

    // ===== �U���͈̓v���r���[�iAttackTileDirector�p�j =====
    public virtual IEnumerable<Vector3Int> GetAttackCellsPreview()
    {
        if (tilemap == null)
            yield break;

        Vector3Int currentCell = tilemap.WorldToCell(transform.position);

        foreach (var dir in GetMovementDirections())
        {
            Vector3Int checkCell = currentCell + dir;

            if (CanAttackPlayer(currentCell, checkCell))
                yield return checkCell;
        }
    }
}
