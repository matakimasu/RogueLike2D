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
    public LayerMask obstacleLayer;    // ��/�i���֎~��
    public LayerMask attackableLayer;  // �U���Ώ�

    [Header("�U���ݒ�")]
    public int attackPower = 3;

    [Header("�T�E���h�ݒ�")]
    public AudioSource audioSource;
    public AudioClip attackSE;
    public AudioClip footstepSE;

    [Header("Click FX�i�����Z����1�񂾂����΁j")]
    [SerializeField] private ClickFlashFX clickFxPrefab;              // �� ������ ClickFlashFX �v���n�u������
    public string fxSortingLayerName = "Overlays";
    public int fxSortingOrder = 600;
    public Color moveFlashColor = new Color(0.20f, 0.90f, 1f, 1f);
    public Color attackFlashColor = new Color(1f, 0.25f, 0.2f, 1f);
    public Color stayFlashColor = new Color(0.80f, 0.80f, 0.80f, 1f); // ���Z���N���b�N���Ȃ�

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

            // �� ����Q�[�g�F�����œ��t���[���A�ł��u���b�N
            if (!GameManager.Instance.TryBeginAction()) return;

            // ���Z���N���b�N �� ���̏�Ń^�[������i=�s�������Ƃ݂Ȃ��j
            if (diff == Vector3Int.zero)
            {
                Vector3 center = tilemap.GetCellCenterWorld(clickedCell);
                SpawnClickFX(center, stayFlashColor);
                GameManager.Instance.ConsumeAction();
                return;
            }

            // 1�}�X�ȓ��̂�
            bool isNeighbor = (Mathf.Abs(diff.x) <= 1 && Mathf.Abs(diff.y) <= 1);
            if (!isNeighbor)
            {
                CancelBegunAction();
                Debug.Log("�������ĉ����ł��Ȃ�");
                return;
            }

            Vector3 cellCenter = tilemap.GetCellCenterWorld(clickedCell);

            // ===== �U���D�� =====
            Collider2D hitAttackable = Physics2D.OverlapBox(cellCenter, new Vector2(0.9f, 0.9f), 0f, attackableLayer);
            if (hitAttackable != null)
            {
                // �s�������F���̃Z����1�񂾂�FX
                SpawnClickFX(cellCenter, attackFlashColor);
                StartCoroutine(PerformAttack(clickedCell));
                return;
            }

            // ===== �ړ��i������ & ��Q���Ȃ��j =====
            if (tilemap.HasTile(clickedCell))
            {
                bool blocked = Physics2D.OverlapBox(cellCenter, new Vector2(0.9f, 0.9f), 0f, obstacleLayer);
                if (!blocked)
                {
                    // �s�������F���̃Z����1�񂾂�FX
                    SpawnClickFX(cellCenter, moveFlashColor);
                    StartCoroutine(MoveToPosition(cellCenter));
                    return;
                }

                CancelBegunAction();
                Debug.Log("���̃}�X�ɏ�Q�������邽�߈ړ��s��");
                return;
            }

            CancelBegunAction();
            Debug.Log("���̃}�X�ɂ͈ړ����U�����ł��Ȃ�");
        }
    }

    // �� �����Z���ł̂݌ĂԁFClickFlashFX ��1�񂾂��Đ�
    private void SpawnClickFX(Vector3 pos, Color tint)
    {
        if (clickFxPrefab == null) return; // �������Ȃ牽�����Ȃ�
        var fx = Instantiate(clickFxPrefab, pos, Quaternion.identity);
        fx.Play(pos, tint, fxSortingLayerName, fxSortingOrder);
    }

    // �s���J�n���������iTryBeginAction������̕s���P�[�X�j
    private void CancelBegunAction()
    {
        StartCoroutine(_CancelGateNextFrame());
    }
    private IEnumerator _CancelGateNextFrame()
    {
        actionLocked = true;    // ���̃t���[���͓��͂�H��Ȃ�
        yield return null;      // ���t���[��
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

        // �ړ����� �� �s������ă^�[���I��
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
                    Debug.Log($"�G��{attackPower}�_���[�W��^����");
                }
                else if (hit.TryGetComponent<TrapBaseSequenced>(out var trap))
                {
                    trap.ForceArm();
                    Debug.Log("㩂��U�����ċN���������I");
                }
            }
            else
            {
                Debug.Log("�U���͈͂ɉ����Ȃ�");
            }
        }
        finally
        {
            if (camFound) camCtrl.followEnabled = true;
            ResetToIdleWithDelay(); // ConsumeAction �� DelayEndTurn() ���ŌĂ�
            actionLocked = false;
        }
    }

    public string GetCurrentActionMode()
    {
        return "Attack";
    }
}
