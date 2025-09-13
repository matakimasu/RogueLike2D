using UnityEngine;

/// <summary>
/// カメラのビューポート余白（pillarbox / letterbox）に合わせて
/// 左右のペインの幅を自動調整する。
/// Canvas は Screen Space - Overlay 推奨。
/// </summary>
public class FitSidePanesToCamera : MonoBehaviour
{
    public Camera targetCamera;
    public RectTransform leftPane;
    public RectTransform rightPane;

    void LateUpdate()
    {
        if (targetCamera == null || leftPane == null || rightPane == null) return;

        // cam.rect は正規化(0..1)。これをピクセル幅に変換
        Rect r = targetCamera.rect;
        float screenW = Screen.width;
        float screenH = Screen.height;

        float viewWpx = r.width * screenW;
        float sidePx = (screenW - viewWpx) * 0.5f; // 左右余白

        // 左ペイン
        Vector2 lmin = new Vector2(0, 0);
        Vector2 lmax = new Vector2(0, 1);
        leftPane.anchorMin = lmin;
        leftPane.anchorMax = lmax;
        leftPane.offsetMin = new Vector2(0, 0);               // 左端から
        leftPane.offsetMax = new Vector2(sidePx, 0);          // 右端を sidePx に

        // 右ペイン
        Vector2 rmin = new Vector2(1, 0);
        Vector2 rmax = new Vector2(1, 1);
        rightPane.anchorMin = rmin;
        rightPane.anchorMax = rmax;
        rightPane.offsetMin = new Vector2(-sidePx, 0);        // 左端を -sidePx に
        rightPane.offsetMax = new Vector2(0, 0);              // 右端へ
    }
}
