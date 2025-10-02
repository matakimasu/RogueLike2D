using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public abstract class EnemyAIBase : MonoBehaviour
{
    [Header("�Q�Ɛݒ�")]
    [SerializeField] protected Tilemap tilemap;       // �� Floor �p�i�����t�B�[���h���p�����p�j
    [SerializeField] protected Tilemap wallTilemap;   // �� �ǉ��FWall �p
    [SerializeField] protected Transform playerTransform;

    [Header("Layer�ݒ�")]
    [SerializeField] protected LayerMask obstacleLayer; // �ǁE�i���֎~���Ȃǁi�R���C�_�[�x�[�X�j
    [SerializeField] protected LayerMask playerLayer;   // �v���C���[�̐�L�Z������p�i���ݍ��ݖh�~�j

    [Header("�ړ��ݒ�")]
    public float moveSpeed = 3.0f;

    [Header("�U���ݒ�")]
    public int attackPower = 1;

    [Header("�_���[�W���o�ݒ�")]
    public float flashDuration = 0.2f;
    public float flashInterval = 0.05f;

    [Header("���ʉ��ݒ�")]
    public AudioSource audioSource;
    public AudioClip moveSE;
    public AudioClip attackSE;

    [Header("���o�i�����W�j�ݒ�")]
    [Tooltip("�v���C���[�U�����ɑO�i���߂鉉�o���s��")]
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
    /// �D�揇�ʁF
    /// 1) �v���C���[���U���ł���Ȃ�U��
    /// 2) �U���ł��Ȃ� �� �v���C���[�ɋ߂Â��ړ������݂�iFloor �̂݁AWall �s�A��Q��/�v���C���[��L���s�j
    /// </summary>
    public virtual IEnumerator TryMoveTowardPlayer()
    {
        if (playerTransform == null || tilemap == null) yield break;

        Vector3Int currentCell = tilemap.WorldToCell(transform.position);
        Vector3Int playerCell = tilemap.WorldToCell(playerTransform.position);

        // ===== 1) �ߐڍU���\�H =====
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

        // ===== 2) �Ǐ]�ړ��i�������k�ޕ������×~�ɑI���j =====
        Vector3Int[] directions = GetMovementDirections();
        foreach (Vector3Int dir in directions)
        {
            Vector3Int nextCell = currentCell + dir;

            // --- �� Floor / Wall ����i�ŗD��Œe���j ---
            bool hasFloor = tilemap.HasTile(nextCell);
            bool isWall = (wallTilemap != null) && wallTilemap.HasTile(nextCell);
            if (!hasFloor || isWall) continue;  // Floor ����Ȃ� or Wall �Ƀ^�C�������� �� �i���s��

            Vector3 worldPos = tilemap.GetCellCenterWorld(nextCell);
            Vector2 checkPoint = new Vector2(worldPos.x, worldPos.y);

            // �i���s�`�F�b�N�i���� & �v���C���[��L�j
            if (Physics2D.OverlapCircle(checkPoint, 0.1f, playerLayer)) continue;   // �v���C���[�ʒu�͕s��
            if (Physics2D.OverlapPoint(checkPoint, obstacleLayer)) continue;        // �R���C�_�[��Q���͕s��

            float currentDist = Vector3.SqrMagnitude((Vector3)(currentCell - playerCell));
            float newDist = Vector3.SqrMagnitude((Vector3)(nextCell - playerCell));

            if (newDist < currentDist)
            {
                yield return StartCoroutine(MoveToPosition(worldPos));
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
