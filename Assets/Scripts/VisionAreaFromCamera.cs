using UnityEngine;

/// <summary>
/// カメラの現在の可視正方形（cam.rect を考慮）に BoxCollider2D を同期する。
/// Layer は "Vision" に設定しておくこと。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class VisionAreaFromCamera : MonoBehaviour
{
    public Camera cam;             // 1:1 ビューポートを使っているカメラ
    public Transform follow;       // たとえば cam.transform（カメラの位置に置く）
    public float padding = 0f;     // 余白を足したい場合（ワールド単位）

    private BoxCollider2D box;

    void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        if (cam == null) cam = Camera.main;
        if (follow == null && cam != null) follow = cam.transform;
    }

    void LateUpdate()
    {
        if (cam == null || follow == null) return;

        // 1) カメラの実効アスペクト（cam.rect を反映、1:1なら常に 1）
        float aspect = cam.aspect;

        // 2) 現在の可視サイズ（オーソサイズは縦の半分）
        float worldHeight = cam.orthographicSize * 2f;
        float worldWidth = worldHeight * aspect;

        // 3) 正方形ビューポート（aspect=1）を想定しているなら、worldWidth は worldHeight と一致
        //    それ以外でも cam.rect ベースの aspect で正しいサイズになる

        // 4) コライダーへ反映
        box.size = new Vector2(worldWidth + padding * 2f, worldHeight + padding * 2f);
        box.offset = Vector2.zero;

        // 5) 位置をカメラに合わせる
        transform.position = new Vector3(follow.position.x, follow.position.y, 0f);
    }
}
