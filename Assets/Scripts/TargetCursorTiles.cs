using UnityEngine;
using UnityEngine.Tilemaps;

public class TargetCursorTiles : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap floorTilemap;
    public Tilemap wallTilemap;        // 任意
    public Tilemap overlayTilemap;     // 表示用オーバーレイ

    [Header("Layers")]
    public LayerMask obstacleLayer;
    public LayerMask attackableLayer;

    [Header("Cursor Tiles")]
    public TileBase moveTile;          // 足跡枠（待機もこれ）
    public TileBase attackTile;        // 剣枠
    public TileBase blockedTile;       // 不可用（未指定なら非表示に）

    [Header("表示ポリシー（カーソルの可視/不可）")]
    public bool hideBlocked = true;
    public bool showDuringPlayerTurn = true;
    public bool showDuringEnemyTurn = true;
    public bool showDuringOtherStates = false;
    public bool hideWhenInputLocked = false;

    [Header("判定")]
    public bool onlyAdjacent = true;
    public Vector2 cellBoxSize = new Vector2(0.9f, 0.9f);

    [Header("描画順（敵/罠より前に）")]
    public string sortingLayerName = "Overlays";
    public int sortingOrder = 500;

    // ▼ 参照は残すが、このコンポーネントからは発火しない（PlayerMover側に集約）
    [Header("Click Flash (※カーソル側では発火しません)")]
    public ClickFlashFX clickFXPrefab;
    public bool spawnFlashOnBlocked = false;
    public Color moveFlashColor = new Color(0.2f, 0.9f, 1f, 1f);
    public Color attackFlashColor = new Color(1f, 0.25f, 0.2f, 1f);
    public Color blockedFlashColor = new Color(0.8f, 0.2f, 0.2f, 1f);

    [Header("Player Animator Hook")]
    public Animator playerAnimator;                 // 未設定なら Awake で自動取得
    public string attackTriggerName = "ToAttack";   // Animator の Trigger 名
    public string idleTriggerName = "ToIdle";

    [Header("アニメ再生を許可するターン")]
    public bool animateDuringPlayerTurn = true;
    public bool animateDuringEnemyTurn = true;
    public bool animateDuringOtherStates = false;

    private Vector3Int _lastCell;
    private TileBase _lastTile;
    private Camera _cam;

    private bool _hoverIsAttack = false;
    private int _toAttackID, _toIdleID;

    void Awake()
    {
        _cam = Camera.main;

        if (overlayTilemap != null)
        {
            var r = overlayTilemap.GetComponent<TilemapRenderer>();
            if (r != null)
            {
                r.sortingLayerName = sortingLayerName;
                r.sortingOrder = sortingOrder;
            }
        }

        _toAttackID = Animator.StringToHash(attackTriggerName);
        _toIdleID = Animator.StringToHash(idleTriggerName);

        // 未設定ならプレイヤーから拾う
        if (playerAnimator == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player) playerAnimator = player.GetComponent<Animator>();
        }
    }

    void OnDisable() => ClearLast();

    void Update()
    {
        if (floorTilemap == null || overlayTilemap == null)
        {
            ClearLast();
            return;
        }
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) { ClearLast(); return; }

        // カーソル可視の判定
        if (!ShouldShowCursor())
        {
            ClearLast();
            // カーソル非表示中はアタック構えも解除
            HandleHoverAnimation(isAttack: false, forceIdle: true);
            return;
        }

        // マウス → ワールド → セル
        Vector3 mp = Input.mousePosition;
        mp.z = Mathf.Abs(_cam.transform.position.z);
        Vector3 world = _cam.ScreenToWorldPoint(mp); world.z = 0f;
        Vector3Int cell = floorTilemap.WorldToCell(world);

        var tile = DecideTile(cell);

        if (cell != _lastCell || tile != _lastTile)
            Apply(cell, tile);

        // Attackタイルをホバー中にアニメ切替（許可されたターンのみ）
        bool isAttackTile = (tile == attackTile);
        HandleHoverAnimation(isAttackTile);

        // ★ ここでの ClickFX 発火は廃止（重複防止）
        //    実際の行動が“成立”したタイミングで PlayerMover 側から発火する。
    }

    // ===== カーソル表示の可否 =====
    bool ShouldShowCursor()
    {
        var gm = GameManager.Instance;
        if (gm == null) return true;
        if (hideWhenInputLocked && gm.inputLocked) return false;

        switch (gm.currentState)
        {
            case TurnState.Player_Turn: return showDuringPlayerTurn;
            case TurnState.Enemy_Turn: return showDuringEnemyTurn;
            default: return showDuringOtherStates;
        }
    }

    // ===== アニメ制御（ターン別許可） =====
    void HandleHoverAnimation(bool isAttack, bool forceIdle = false)
    {
        bool allow = true;
        var gm = GameManager.Instance;

        if (gm != null)
        {
            if (hideWhenInputLocked && gm.inputLocked) allow = false;
            if (allow)
            {
                switch (gm.currentState)
                {
                    case TurnState.Player_Turn: allow = animateDuringPlayerTurn; break;
                    case TurnState.Enemy_Turn: allow = animateDuringEnemyTurn; break;
                    default: allow = animateDuringOtherStates; break;
                }
            }
        }

        if (!allow) isAttack = false;

        if (playerAnimator == null) { _hoverIsAttack = isAttack; return; }

        if (forceIdle)
        {
            if (_hoverIsAttack)
            {
                playerAnimator.ResetTrigger(_toAttackID);
                if (!string.IsNullOrEmpty(idleTriggerName)) playerAnimator.SetTrigger(_toIdleID);
            }
            _hoverIsAttack = false;
            return;
        }

        if (isAttack == _hoverIsAttack) return;

        if (isAttack)
        {
            playerAnimator.ResetTrigger(_toIdleID);
            if (!string.IsNullOrEmpty(attackTriggerName)) playerAnimator.SetTrigger(_toAttackID);
        }
        else
        {
            playerAnimator.ResetTrigger(_toAttackID);
            if (!string.IsNullOrEmpty(idleTriggerName)) playerAnimator.SetTrigger(_toIdleID);
        }

        _hoverIsAttack = isAttack;
    }

    // ===== タイル適用/解除 =====
    void Apply(Vector3Int cell, TileBase tile)
    {
        if (_lastTile != null) overlayTilemap.SetTile(_lastCell, null);
        if (tile != null || !hideBlocked)
            overlayTilemap.SetTile(cell, tile ?? blockedTile);
        _lastCell = cell;
        _lastTile = tile;
    }

    void ClearLast()
    {
        if (_lastTile != null && overlayTilemap != null)
            overlayTilemap.SetTile(_lastCell, null);
        _lastTile = null;
    }

    // ===== 判定 =====
    TileBase DecideTile(Vector3Int cell)
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null) return hideBlocked ? null : blockedTile;

        Vector3Int pc = floorTilemap.WorldToCell(player.transform.position);
        int dx = Mathf.Abs(cell.x - pc.x);
        int dy = Mathf.Abs(cell.y - pc.y);

        // 自セル＝待機も moveTile
        if (dx == 0 && dy == 0) return moveTile;

        if (onlyAdjacent && (dx > 1 || dy > 1))
            return hideBlocked ? null : blockedTile;

        Vector3 center = floorTilemap.GetCellCenterWorld(cell);

        // 攻撃優先
        bool attackable = Physics2D.OverlapBox(center, cellBoxSize, 0f, attackableLayer);
        if (attackable) return attackTile;

        // 移動：床あり && 壁なし && 障害物なし
        bool hasFloor = floorTilemap.HasTile(cell);
        bool isWall = (wallTilemap != null) && wallTilemap.HasTile(cell);
        bool blocked = Physics2D.OverlapBox(center, cellBoxSize, 0f, obstacleLayer);

        if (hasFloor && !isWall && !blocked) return moveTile;

        return hideBlocked ? null : blockedTile;
    }
}
