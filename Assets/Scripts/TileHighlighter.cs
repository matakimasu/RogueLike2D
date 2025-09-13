using UnityEngine;
using UnityEngine.Tilemaps;

public class TileHighlighter : MonoBehaviour
{
    public Tilemap tilemap;
    public LayerMask obstacleLayer;
    public Transform cursorIndicator;
    public SpriteRenderer cursorRenderer;

    [Header("判定レイヤー")]
    [Tooltip("マーク対象（敵・罠など）が属するレイヤー")]
    public LayerMask markableLayer = 0;
    [Tooltip("視界領域（Vision）のレイヤー。ここに重なっているセルだけ Mark 可。")]
    public LayerMask visionLayer = 0;

    [Header("ハイライトカラー")]
    public Color canMoveColor = Color.cyan;
    public Color cannotColor = Color.red;
    public Color searchColor = Color.green;
    public Color attackColor = Color.red;
    public Color markColor = Color.blue;

    [Range(0f, 1f)] public float cursorAlpha = 1f;
    [Header("当たりサイズ（セル判定）")]
    public Vector2 cellBoxSize = new Vector2(0.9f, 0.9f);

    private Vector3Int lastCellPos;

    void Start()
    {
        if (tilemap == null || Camera.main == null) return;
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        lastCellPos = tilemap.WorldToCell(mouseWorld);
        if (cursorIndicator != null)
            cursorIndicator.position = tilemap.GetCellCenterWorld(lastCellPos);
    }

    void Update()
    {
        if (tilemap == null || Camera.main == null || cursorRenderer == null) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;
        Vector3Int mouseCell = tilemap.WorldToCell(mouseWorld);
        Vector3 cellCenter = tilemap.GetCellCenterWorld(mouseCell);

        if (mouseCell != lastCellPos)
        {
            if (cursorIndicator != null) cursorIndicator.position = cellCenter;
            lastCellPos = mouseCell;
        }

        if (GameManager.Instance == null) { SetCursorColor(cannotColor); return; }

        var state = GameManager.Instance.currentState;

        if (state == TurnState.Player_Action)
        {
            var player = GameObject.FindWithTag("Player");
            if (player == null) { SetCursorColor(cannotColor); return; }

            var mover = player.GetComponent<PlayerMover>();
            if (mover == null) { SetCursorColor(cannotColor); return; }

            string mode = mover.GetCurrentActionMode();

            if (mode == "Search")
            {
                SetCursorColor(searchColor); return;
            }
            else if (mode == "Attack")
            {
                Vector3Int pc = tilemap.WorldToCell(player.transform.position);
                int dx = Mathf.Abs(mouseCell.x - pc.x);
                int dy = Mathf.Abs(mouseCell.y - pc.y);
                bool isAround = (dx <= 1 && dy <= 1 && (dx != 0 || dy != 0));
                SetCursorColor(isAround ? attackColor : cannotColor);
                return;
            }
            else if (mode == "Mark")
            {
                bool hasMarkable = Physics2D.OverlapBox(cellCenter, cellBoxSize, 0f, markableLayer) != null;
                bool insideVision = Physics2D.OverlapPoint(cellCenter, visionLayer) != null ||
                                    Physics2D.OverlapBox(cellCenter, cellBoxSize, 0f, visionLayer) != null;

                SetCursorColor((hasMarkable && insideVision) ? markColor : cannotColor);
                return;
            }

            SetCursorColor(cannotColor); return;
        }

        if (state == TurnState.Player_Move)
        {
            var player = GameObject.FindWithTag("Player");
            if (player == null) { SetCursorColor(cannotColor); return; }

            Vector3Int pc = tilemap.WorldToCell(player.transform.position);
            int dx = Mathf.Abs(mouseCell.x - pc.x);
            int dy = Mathf.Abs(mouseCell.y - pc.y);
            bool isReachable = (dx <= 1 && dy <= 1);

            if (isReachable && tilemap.HasTile(mouseCell))
            {
                var hit = Physics2D.OverlapBox(cellCenter, cellBoxSize, 0f, obstacleLayer);
                SetCursorColor((hit == null) ? canMoveColor : cannotColor);
            }
            else SetCursorColor(cannotColor);

            return;
        }

        SetCursorColor(cannotColor);
    }

    private void SetCursorColor(Color baseColor)
    {
        Color c = baseColor; c.a = cursorAlpha;
        cursorRenderer.color = c;
    }
}
