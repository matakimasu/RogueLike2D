using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(BoxCollider2D))]
public abstract class TrapBaseSequenced : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap floorTilemap;
    public Tilemap wallTilemap;

    [Header("When to act")]
    public bool actOnPlayerTurn = false;
    public bool actOnEnemyTurn = true;

    [Header("Trigger / Arming")]
    public bool requireArmed = true;
    public bool disarmAfterAct = true;

    public string visionLayerName = "Vision";
    public string playerVisionTag = "PlayerVision";
    public string bulletVisionTag = "BulletVision";
    public bool useLayerCheck = true;
    public bool useTagCheck = true;

    [Header("Attack")]
    public int attackPower = 2;
    public LayerMask hitLayers;                  // Enemy / Player / Bullet ���܂߂�
    public Vector2 cellHitBox = new Vector2(0.9f, 0.9f);

    [Header("Blocking")]
    public bool stopRayOnTrap = true;
    public LayerMask trapBlockLayer;

    [Header("SFX (optional)")]
    public AudioSource audioSource;
    public AudioClip armSE;     // �N����
    public AudioClip attackSE;  // ������

    [Header("Pacing")]
    public float actDuration = 0.20f;
    public bool waitForSELength = false;

    [Header("Visual Timing")]
    [Tooltip("�N����(SE)����I����Ă���AttackTile���o��")]
    public bool delayVisualUntilArmSE = true;
    [Tooltip("SE�����ɉ��Z����ǂ��f�B���C(�b)")]
    public float visualExtraDelay = 0f;

    // ===== Interval�i�Z�^�[����1��j =====
    [Header("Cycle / Interval")]
    [Min(1), Tooltip("���^�[�����Ƃɔ��΂��邩�i1=���^�[���j")]
    public int intervalTurns = 1;
    [Min(0), Tooltip("���񔭉΂܂ł̑҂��^�[���i0=�����j")]
    public int initialOffset = 0;

    // �������
    protected bool armed = false;
    public bool IsArmed => armed;

    [SerializeField] private int turnsUntilFire;   // �c�^�[���i�����p�j
    public int TurnsRemaining => turnsUntilFire;

    // �N���C�x���g�i�r�W���A���p�j
    public event System.Action<TrapBaseSequenced> OnArmed;        // Arm����
    public event System.Action<TrapBaseSequenced> OnArmedVisual;  // SE��̉���

    public virtual bool ShouldActOnPhase(TurnState phase)
    {
        return (phase == TurnState.Player_Move && actOnPlayerTurn)
            || (phase == TurnState.Enemy_Turn && actOnEnemyTurn);
    }
    public virtual bool IsReadyToAct() => true;
    protected virtual void AfterAct() { }

    protected virtual void Awake()
    {
        var box = GetComponent<BoxCollider2D>();
        if (box)
        {
            box.isTrigger = true;
            box.offset = Vector2.zero;
            if (box.size != cellHitBox) box.size = cellHitBox;
        }
    }

    protected virtual void Start()
    {
        // �Z�����S�փX�i�b�v
        if (floorTilemap != null)
        {
            var c = floorTilemap.WorldToCell(transform.position);
            transform.position = floorTilemap.GetCellCenterWorld(c);
        }

        // ����������
        ResetCycle();

        GameManager.Instance?.RegisterTrap(this);
    }

    protected virtual void OnDestroy()
    {
        GameManager.Instance?.UnregisterTrap(this);
    }

    // ===== Interval �������� =====
    public void ResetCycle()
    {
        SetTurnsRemaining(Mathf.Max(0, initialOffset));
    }

    /// <summary>�t�F�[�Y�J�n���� GameManager ����Ă΂��B�Y���t�F�[�Y�̂ݎc�^�[��-1�B</summary>
    public void OnTurnAdvanced(TurnState phase)
    {
        if (!ShouldActOnPhase(phase)) return;
        if (turnsUntilFire > 0) SetTurnsRemaining(turnsUntilFire - 1);
    }

    private void SetTurnsRemaining(int value)
    {
        turnsUntilFire = Mathf.Max(0, value);
    }

    private void ReloadCycleAfterAct()
    {
        // interval=1 -> �҂�0�Ainterval=3 -> �҂�2
        int wait = Mathf.Max(0, intervalTurns - 1);
        SetTurnsRemaining(wait);
    }

    // ===== �N������iVision Trigger������������ Arm�j =====
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (armed) return;

        bool matched = false;

        if (useLayerCheck)
        {
            int visionLayer = LayerMask.NameToLayer(visionLayerName);
            if (visionLayer >= 0 && other.gameObject.layer == visionLayer)
                matched = true;
        }

        if (!matched && useTagCheck)
        {
            if (!string.IsNullOrEmpty(playerVisionTag) && other.CompareTag(playerVisionTag)) matched = true;
            if (!string.IsNullOrEmpty(bulletVisionTag) && other.CompareTag(bulletVisionTag)) matched = true;
        }

        if (matched) Arm();
    }

    protected virtual void Arm()
    {
        armed = true;

        if (audioSource && armSE)
            audioSource.PlayOneShot(armSE);

        OnArmed?.Invoke(this);

        if (delayVisualUntilArmSE && audioSource && armSE)
        {
            float delay = Mathf.Max(0f, armSE.length + visualExtraDelay);
            StartCoroutine(NotifyVisualAfter(delay));
        }
        else
        {
            OnArmedVisual?.Invoke(this);
        }
    }

    private IEnumerator NotifyVisualAfter(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        OnArmedVisual?.Invoke(this);
    }

    // ===== ���s���� =====
    public virtual IEnumerator Act(TurnState phase)
    {
        if (!ShouldActOnPhase(phase)) yield break;
        if (turnsUntilFire > 0) yield break;
        if (!IsReadyToAct()) yield break;
        if (requireArmed && !armed) yield break;

        if (audioSource && attackSE) audioSource.PlayOneShot(attackSE);

        DoAttack();       // �d���q�b�g�}�~��
        AfterAct();

        if (disarmAfterAct) armed = false;

        // ����܂ł̑҂��^�[�����U
        ReloadCycleAfterAct();

        float wait = Mathf.Max(0f, actDuration);
        if (waitForSELength && attackSE != null)
            wait = Mathf.Max(wait, attackSE.length);

        if (wait > 0f) yield return new WaitForSeconds(wait);
        else yield return null;
    }

    // ===== �U���i����Act���̏d���q�b�g�}�~�j =====
    protected void DoAttack()
    {
        if (floorTilemap == null || wallTilemap == null) return;

        var origin = floorTilemap.WorldToCell(transform.position);

        var processedBullets = new HashSet<GameObject>();
        var processedEnemies = new HashSet<EnemyHealth>();
        bool playerProcessed = false;

        foreach (var cell in EnumerateAttackCells(origin))
        {
            var world = floorTilemap.GetCellCenterWorld(cell);
            var hits = Physics2D.OverlapBoxAll(world, cellHitBox, 0f, hitLayers);
            if (hits == null || hits.Length == 0) continue;

            foreach (var h in hits)
            {
                if (h == null) continue;

                if (h.CompareTag("Bullet"))
                {
                    if (processedBullets.Add(h.gameObject))
                    {
                        Destroy(h.gameObject);
                    }
                    continue;
                }

                if (h.TryGetComponent<EnemyHealth>(out var enemy))
                {
                    if (processedEnemies.Add(enemy))
                    {
                        enemy.TakeDamage(attackPower);
                        var ai = enemy.GetComponent<EnemyAIBase>();
                        ai?.PlayDamageFlash();
                    }
                    continue;
                }

                if (!playerProcessed && h.CompareTag("Player"))
                {
                    GameManager.Instance?.TakeDamage(attackPower);
                    playerProcessed = true;
                    continue;
                }
            }
        }
    }

    // ===== ���i�񋓁F��/㩂Œ�~�i���������Z���͊܂߂Ȃ��j =====
    protected IEnumerable<Vector3Int> RayCells(Vector3Int origin, Vector3Int dir)
    {
        var c = origin + dir;
        while (true)
        {
            if (wallTilemap.HasTile(c)) yield break;
            if (!floorTilemap.HasTile(c)) yield break;
            if (stopRayOnTrap && IsTrapBlockingCell(c)) yield break;

            yield return c;
            c += dir;
        }
    }

    private bool IsTrapBlockingCell(Vector3Int cell)
    {
        if (trapBlockLayer.value != 0)
        {
            Vector3 w = floorTilemap.GetCellCenterWorld(cell);
            var hit = Physics2D.OverlapBox(w, cellHitBox, 0f, trapBlockLayer);
            if (hit != null)
            {
                var trap = hit.GetComponentInParent<TrapBaseSequenced>();
                if (trap != null && trap != this) return true;
            }
            return false;
        }

        // �t�H�[���o�b�N�F�S㩑���
        var traps = FindObjectsOfType<TrapBaseSequenced>();
        foreach (var t in traps)
        {
            if (t == null || t == this) continue;
            if (t.floorTilemap == null) continue;

            var tc = t.floorTilemap.WorldToCell(t.transform.position);
            if (tc == cell) return true;
        }
        return false;
    }

    // ====== �\���p�v���r���[ ======
    public IEnumerable<Vector3Int> PreviewCells()
    {
        if (floorTilemap == null) yield break;
        var origin = floorTilemap.WorldToCell(transform.position);
        foreach (var c in EnumerateAttackCells(origin))
            yield return c;
    }

    public IEnumerable<Vector3Int> GetAttackCells(Vector3Int origin)
    {
        foreach (var c in EnumerateAttackCells(origin))
            yield return c;
    }

    // ===== �e㩂���������U���p�^�[�� =====
    protected abstract IEnumerable<Vector3Int> EnumerateAttackCells(Vector3Int origin);

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (floorTilemap == null || wallTilemap == null) return;
        var origin = floorTilemap.WorldToCell(transform.position);
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.9f);
        foreach (var c in EnumerateAttackCells(origin))
        {
            var w = floorTilemap.GetCellCenterWorld(c);
            Gizmos.DrawWireCube(w, new Vector3(cellHitBox.x, cellHitBox.y, 0f));
        }
    }
#endif
}
