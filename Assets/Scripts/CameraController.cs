using UnityEngine;
using System.Collections;

/// <summary>
/// �Ǐ] + �V�F�C�N + �O���b�h���� + ���S��Y�[�� + 1:1�����`�r���[�|�[�g
/// - �v���C���[���S�Ɂg���炩�Ɂh�Ǐ]�i�ڕW�͊i�q�ɃX�i�b�v�A�J�����̓X���[�Y�Ɋ��j
/// - �I�Ղ����\�t�g�z�����Ē[�̋��E���s�^�b�ƍ��킹��
/// - 1:1���[�h�ł͉�ʒ����ɐ����`�r���[�|�[�g�i��������ʂ̏c���j
/// - �����}�X���̊�Œ�I�v�V��������
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Follow")]
    public Transform target;
    [Tooltip("�Ǐ]�̊��炩���i�傫���قǑf�����Ǐ]�j")]
    public float smoothSpeed = 10f;
    public bool followEnabled = true;

    // Shake
    private Vector3 shakeOffset;
    private Coroutine shakeRoutine;

    [Header("Grid / Tile")]
    [Tooltip("1�}�X�̃��[���h�P�ʁiTilemap�̃Z����1�Ȃ�1�j")]
    public float tileSize = 1f;

    [Header("Initial Align")]
    public int initialTilesY = 15;
    public bool alignOnStart = true;

    [Header("Zoom (center-based)")]
    public int minTilesY = 8;
    public int maxTilesY = 24;
    public int zoomStep = 2; // 2�}�X�P�ʃY�[��
    public bool respectInputLock = false;

    [Header("Snapping")]
    [Tooltip("�Ǐ]���F�ڕW���S�ɏ[���߂Â����炾���z�����銄���i�^�C���T�C�Y�ɑ΂���䗦�j")]
    [Range(0.05f, 1f)] public float softSnapThresholdTiles = 0.25f; // 0.25�^�C���ȓ��ŋz��
    public bool snapContinuously = true; // ��Ǐ]����Y�[�����͏]���ʂ�X�i�b�v

    [Header("Width Parity")]
    [Tooltip("�����̃}�X����K����ɌŒ肷��i1:1���[�h�����L���j")]
    public bool forceOddTilesX = true;

    [Header("Square View (1:1)")]
    [Tooltip("true�ŉ�ʒ����ɐ����`�r���[�|�[�g�i����=��ʂ̏c���j��K�p")]
    public bool useSquareViewport = true;

    private Camera cam;
    private int tilesY;
    private int tilesX;
    private int lastScreenW, lastScreenH;

    // �� �X�i�b�v�p���S�I�t�Z�b�g�i���E�c�j
    private float centerOffsetX, centerOffsetY;

    void Awake()
    {
        cam = GetComponent<Camera>();
        // ���1:1�r���[�|�[�g��K�p���āA�ȍ~�̃A�X�y�N�g�v�Z�𐳂�������
        ApplyViewportRect();

        tilesY = Mathf.Clamp(initialTilesY, Mathf.Max(1, minTilesY), Mathf.Max(minTilesY, maxTilesY));
    }

    void Start()
    {
        if (alignOnStart)
        {
            ApplySizeAndSnap(); // �T�C�Y�v�Z �� �I�t�Z�b�g�Čv�Z �� �X�i�b�v
        }
        CacheScreenSize();
    }

    void Update()
    {
        // �Y�[���i���S��j
        if (!(respectInputLock && GameManagerLocked()))
        {
            float scroll = Input.mouseScrollDelta.y; // ��Ő��A���ŕ�
            if (Mathf.Abs(scroll) > 0.01f)
            {
                int delta = (scroll > 0f) ? -zoomStep : +zoomStep;
                ZoomTilesY(delta);
            }
        }

        // ��ʃT�C�Y�ω��F�r���[�|�[�g���T�C�Y���X�i�b�v
        if (ScreenSizeChanged())
        {
            ApplyViewportRect();
            ApplySizeAndSnap();
            CacheScreenSize();
        }
    }

    void LateUpdate()
    {
        if (!followEnabled)
        {
            // �ʒu�Œ� + �V�F�C�N�̂�
            Vector3 basePos = transform.position;
            transform.position = WithCurrentZ(basePos + shakeOffset);

            if (snapContinuously) SnapCameraToGrid();
            return;
        }

        if (target == null)
        {
            if (snapContinuously) SnapCameraToGrid();
            return;
        }

        // 1) �v���C���[�ʒu����u�[�����E�ɂȂ钆�S���W�v�i�i�q�X�i�b�v�ς݂̖ڕW�j���Z�o
        Vector3 desiredCenter = GetSnappedCenterFromTarget(target.position);

        // 2) �J�����͂��̒��S�ցg���炩�Ɂh�񂹂�
        Vector3 targetPos = desiredCenter + shakeOffset;
        Vector3 blended = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
        transform.position = WithCurrentZ(blended);

        // 3) �[���߂Â����炾���g�p�`�b�h�Ƌz�����Ē[���E��������
        if (snapContinuously)
        {
            float threshold = tileSize * softSnapThresholdTiles;
            if (Mathf.Abs(transform.position.x - desiredCenter.x) <= threshold &&
                Mathf.Abs(transform.position.y - desiredCenter.y) <= threshold)
            {
                transform.position = WithCurrentZ(desiredCenter);
            }
        }
    }

    // ====== �ڕW���S�i�[�����E�ɂȂ�i�q���S�j ======
    private Vector3 GetSnappedCenterFromTarget(Vector3 targetPos)
    {
        return SnappedPoint(targetPos); // Z�̓J�����̌��ݒl��ێ�
    }

    // ====== ���JAPI�F�V�F�C�N ======
    public void Shake(float duration, float magnitude)
    {
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;
            shakeOffset = new Vector3(offsetX, offsetY, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }
        shakeOffset = Vector3.zero;
        shakeRoutine = null;
    }

    // ====== ���JAPI�F�c�}�X���𒼐ڎw�� ======
    public void SetTilesY(int newTilesY)
    {
        tilesY = Mathf.Clamp(newTilesY, Mathf.Max(1, minTilesY), Mathf.Max(minTilesY, maxTilesY));
        ApplySizeAndSnap();
    }

    public void ForceSnap() => SnapCameraToGrid();

    // ====== �����F�Y�[���i���S��j ======
    private void ZoomTilesY(int deltaTilesY)
    {
        int newTilesY = Mathf.Clamp(tilesY + deltaTilesY, Mathf.Max(1, minTilesY), Mathf.Max(minTilesY, maxTilesY));
        if (newTilesY == tilesY) return;

        tilesY = newTilesY;

        // �Y�[������F�T�C�Y���Čv�Z���A�v���C���[���S�ɍ��킹�Ă���X�i�b�v
        ApplySizeFromTilesY();           // tilesX/Y �� orthographicSize ���X�V�i���I�t�Z�b�g�Čv�Z�j
        CenterOnTargetAndSnap();         // �������񒆐S�� �� �Ō�ɃX�i�b�v
    }

    // ====== �����FtilesY �� orthographicSize / tilesX ======
    private void ApplySizeFromTilesY()
    {
        tilesY = Mathf.Max(1, tilesY);

        float worldHeight = tilesY * tileSize;
        cam.orthographicSize = worldHeight * 0.5f;

        float effectiveAspect = GetEffectiveAspect();
        float worldWidth = worldHeight * effectiveAspect;

        tilesX = Mathf.Max(1, Mathf.RoundToInt(worldWidth / tileSize));
        if (forceOddTilesX && (tilesX % 2 == 0))
            tilesX += 1;

        float snappedWidth = tilesX * tileSize;
        cam.orthographicSize = (snappedWidth / effectiveAspect) * 0.5f;

        // �� tilesX/Y �����܂����̂ŁA�X�i�b�v���S�̃I�t�Z�b�g���Čv�Z
        RecalcSnapOffsets();
    }

    // ====== �����F�J�����ʒu���i�q�ցi����/�Y�[��/�Œ莞�p�j ======
    private void SnapCameraToGrid()
    {
        transform.position = SnappedPoint(transform.position);
    }

    // ====== 1:1 �r���[�|�[�g�K�p ======
    private void ApplyViewportRect()
    {
        if (!useSquareViewport)
        {
            cam.rect = new Rect(0, 0, 1, 1);
            return;
        }

        float screenW = Screen.width;
        float screenH = Screen.height;

        if (screenW >= screenH)
        {
            float rectW = screenH / screenW;
            cam.rect = new Rect((1f - rectW) * 0.5f, 0f, rectW, 1f);
        }
        else
        {
            float rectH = screenW / screenH;
            cam.rect = new Rect(0f, (1f - rectH) * 0.5f, 1f, rectH);
        }
    }

    private float GetEffectiveAspect() => cam.aspect; // cam.rect �𔽉f���������A�X�y�N�g

    // ====== �� �������狤�ʉ��w���p ======

    /// <summary>���݂� tilesX/Y �Ɋ�Â��X�i�b�v���S�I�t�Z�b�g���v�Z�E�L���b�V��</summary>
    private void RecalcSnapOffsets()
    {
        float halfTilesX = tilesX * 0.5f;
        float halfTilesY = tilesY * 0.5f;
        centerOffsetX = (IsInteger(halfTilesX) ? 0f : 0.5f) * tileSize;
        centerOffsetY = (IsInteger(halfTilesY) ? 0f : 0.5f) * tileSize;
    }

    /// <summary>�C�ӂ̓_���u�[�����E�ɂȂ钆�S�i�q�v�փX�i�b�v���ĕԂ��iZ�͌��݂̃J�������ێ��j</summary>
    private Vector3 SnappedPoint(Vector3 p)
    {
        p = WithCurrentZ(p);
        p.x = SnapWithOffset(p.x, tileSize, centerOffsetX);
        p.y = SnapWithOffset(p.y, tileSize, centerOffsetY);
        return p;
    }

    /// <summary>Z �����݂̃J������ Z �ɍ��킹��</summary>
    private Vector3 WithCurrentZ(Vector3 v)
    {
        v.z = transform.position.z;
        return v;
    }

    /// <summary>�T�C�Y�𔽉f���Ă���X�i�b�v�i�N����/�𑜓x�ύX���Ȃǂ̒�^�j</summary>
    private void ApplySizeAndSnap()
    {
        ApplySizeFromTilesY();
        SnapCameraToGrid();
    }

    /// <summary>�v���C���[���S�ֈ�U���킹�A���̌�X�i�b�v�i�Y�[������p�j</summary>
    private void CenterOnTargetAndSnap()
    {
        if (followEnabled && target != null)
        {
            transform.position = WithCurrentZ(GetSnappedCenterFromTarget(target.position) + shakeOffset);
        }
        SnapCameraToGrid();
    }

    // ====== ���[�e�B���e�B ======
    private static bool IsInteger(float v) => Mathf.Abs(v - Mathf.Round(v)) < 0.0001f;

    private static float SnapWithOffset(float value, float step, float offset)
    {
        float k = Mathf.Round((value - offset) / step);
        return k * step + offset;
    }

    private bool GameManagerLocked()
    {
        return (GameManager.Instance != null && GameManager.Instance.inputLocked);
    }

    private bool ScreenSizeChanged()
    {
        return (Screen.width != lastScreenW) || (Screen.height != lastScreenH);
    }

    private void CacheScreenSize()
    {
        lastScreenW = Screen.width;
        lastScreenH = Screen.height;
    }
}
