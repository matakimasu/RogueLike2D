using UnityEngine;
using System.Collections;

/// <summary>
/// 追従 + シェイク + グリッド整列 + 中心基準ズーム + 1:1正方形ビューポート
/// - プレイヤー中心に“滑らかに”追従（目標は格子にスナップ、カメラはスムーズに寄る）
/// - 終盤だけソフト吸着して端の境界をピタッと合わせる
/// - 1:1モードでは画面中央に正方形ビューポート（高さ＝画面の縦幅）
/// - 横幅マス数の奇数固定オプションあり
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Follow")]
    public Transform target;
    [Tooltip("追従の滑らかさ（大きいほど素早く追従）")]
    public float smoothSpeed = 10f;
    public bool followEnabled = true;

    // Shake
    private Vector3 shakeOffset;
    private Coroutine shakeRoutine;

    [Header("Grid / Tile")]
    [Tooltip("1マスのワールド単位（Tilemapのセルが1なら1）")]
    public float tileSize = 1f;

    [Header("Initial Align")]
    public int initialTilesY = 15;
    public bool alignOnStart = true;

    [Header("Zoom (center-based)")]
    public int minTilesY = 8;
    public int maxTilesY = 24;
    public int zoomStep = 2; // 2マス単位ズーム
    public bool respectInputLock = false;

    [Header("Snapping")]
    [Tooltip("追従時：目標中心に充分近づいたらだけ吸着する割合（タイルサイズに対する比率）")]
    [Range(0.05f, 1f)] public float softSnapThresholdTiles = 0.25f; // 0.25タイル以内で吸着
    public bool snapContinuously = true; // 非追従時やズーム時は従来通りスナップ

    [Header("Width Parity")]
    [Tooltip("横幅のマス数を必ず奇数に固定する（1:1モード時も有効）")]
    public bool forceOddTilesX = true;

    [Header("Square View (1:1)")]
    [Tooltip("trueで画面中央に正方形ビューポート（高さ=画面の縦幅）を適用")]
    public bool useSquareViewport = true;

    private Camera cam;
    private int tilesY;
    private int tilesX;
    private int lastScreenW, lastScreenH;

    // ★ スナップ用中心オフセット（横・縦）
    private float centerOffsetX, centerOffsetY;

    void Awake()
    {
        cam = GetComponent<Camera>();
        // 先に1:1ビューポートを適用して、以降のアスペクト計算を正しくする
        ApplyViewportRect();

        tilesY = Mathf.Clamp(initialTilesY, Mathf.Max(1, minTilesY), Mathf.Max(minTilesY, maxTilesY));
    }

    void Start()
    {
        if (alignOnStart)
        {
            ApplySizeAndSnap(); // サイズ計算 → オフセット再計算 → スナップ
        }
        CacheScreenSize();
    }

    void Update()
    {
        // ズーム（中心基準）
        if (!(respectInputLock && GameManagerLocked()))
        {
            float scroll = Input.mouseScrollDelta.y; // 上で正、下で負
            if (Mathf.Abs(scroll) > 0.01f)
            {
                int delta = (scroll > 0f) ? -zoomStep : +zoomStep;
                ZoomTilesY(delta);
            }
        }

        // 画面サイズ変化：ビューポート→サイズ→スナップ
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
            // 位置固定 + シェイクのみ
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

        // 1) プレイヤー位置から「端が境界になる中心座標」（格子スナップ済みの目標）を算出
        Vector3 desiredCenter = GetSnappedCenterFromTarget(target.position);

        // 2) カメラはその中心へ“滑らかに”寄せる
        Vector3 targetPos = desiredCenter + shakeOffset;
        Vector3 blended = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
        transform.position = WithCurrentZ(blended);

        // 3) 充分近づいたらだけ“パチッ”と吸着して端境界を完璧に
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

    // ====== 目標中心（端が境界になる格子中心） ======
    private Vector3 GetSnappedCenterFromTarget(Vector3 targetPos)
    {
        return SnappedPoint(targetPos); // Zはカメラの現在値を保持
    }

    // ====== 公開API：シェイク ======
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

    // ====== 公開API：縦マス数を直接指定 ======
    public void SetTilesY(int newTilesY)
    {
        tilesY = Mathf.Clamp(newTilesY, Mathf.Max(1, minTilesY), Mathf.Max(minTilesY, maxTilesY));
        ApplySizeAndSnap();
    }

    public void ForceSnap() => SnapCameraToGrid();

    // ====== 内部：ズーム（中心基準） ======
    private void ZoomTilesY(int deltaTilesY)
    {
        int newTilesY = Mathf.Clamp(tilesY + deltaTilesY, Mathf.Max(1, minTilesY), Mathf.Max(minTilesY, maxTilesY));
        if (newTilesY == tilesY) return;

        tilesY = newTilesY;

        // ズーム直後：サイズを再計算し、プレイヤー中心に合わせてからスナップ
        ApplySizeFromTilesY();           // tilesX/Y と orthographicSize を更新（＆オフセット再計算）
        CenterOnTargetAndSnap();         // いったん中心へ → 最後にスナップ
    }

    // ====== 内部：tilesY → orthographicSize / tilesX ======
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

        // ★ tilesX/Y が決まったので、スナップ中心のオフセットを再計算
        RecalcSnapOffsets();
    }

    // ====== 内部：カメラ位置を格子へ（初期/ズーム/固定時用） ======
    private void SnapCameraToGrid()
    {
        transform.position = SnappedPoint(transform.position);
    }

    // ====== 1:1 ビューポート適用 ======
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

    private float GetEffectiveAspect() => cam.aspect; // cam.rect を反映した実効アスペクト

    // ====== ★ ここから共通化ヘルパ ======

    /// <summary>現在の tilesX/Y に基づくスナップ中心オフセットを計算・キャッシュ</summary>
    private void RecalcSnapOffsets()
    {
        float halfTilesX = tilesX * 0.5f;
        float halfTilesY = tilesY * 0.5f;
        centerOffsetX = (IsInteger(halfTilesX) ? 0f : 0.5f) * tileSize;
        centerOffsetY = (IsInteger(halfTilesY) ? 0f : 0.5f) * tileSize;
    }

    /// <summary>任意の点を「端が境界になる中心格子」へスナップして返す（Zは現在のカメラを維持）</summary>
    private Vector3 SnappedPoint(Vector3 p)
    {
        p = WithCurrentZ(p);
        p.x = SnapWithOffset(p.x, tileSize, centerOffsetX);
        p.y = SnapWithOffset(p.y, tileSize, centerOffsetY);
        return p;
    }

    /// <summary>Z を現在のカメラの Z に合わせる</summary>
    private Vector3 WithCurrentZ(Vector3 v)
    {
        v.z = transform.position.z;
        return v;
    }

    /// <summary>サイズを反映してからスナップ（起動時/解像度変更時などの定型）</summary>
    private void ApplySizeAndSnap()
    {
        ApplySizeFromTilesY();
        SnapCameraToGrid();
    }

    /// <summary>プレイヤー中心へ一旦合わせ、その後スナップ（ズーム直後用）</summary>
    private void CenterOnTargetAndSnap()
    {
        if (followEnabled && target != null)
        {
            transform.position = WithCurrentZ(GetSnappedCenterFromTarget(target.position) + shakeOffset);
        }
        SnapCameraToGrid();
    }

    // ====== ユーティリティ ======
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
