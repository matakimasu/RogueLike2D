using UnityEngine;

/// <summary>
/// �J�����̃r���[�|�[�g�]���ipillarbox / letterbox�j�ɍ��킹��
/// ���E�̃y�C���̕���������������B
/// Canvas �� Screen Space - Overlay �����B
/// </summary>
public class FitSidePanesToCamera : MonoBehaviour
{
    public Camera targetCamera;
    public RectTransform leftPane;
    public RectTransform rightPane;

    void LateUpdate()
    {
        if (targetCamera == null || leftPane == null || rightPane == null) return;

        // cam.rect �͐��K��(0..1)�B������s�N�Z�����ɕϊ�
        Rect r = targetCamera.rect;
        float screenW = Screen.width;
        float screenH = Screen.height;

        float viewWpx = r.width * screenW;
        float sidePx = (screenW - viewWpx) * 0.5f; // ���E�]��

        // ���y�C��
        Vector2 lmin = new Vector2(0, 0);
        Vector2 lmax = new Vector2(0, 1);
        leftPane.anchorMin = lmin;
        leftPane.anchorMax = lmax;
        leftPane.offsetMin = new Vector2(0, 0);               // ���[����
        leftPane.offsetMax = new Vector2(sidePx, 0);          // �E�[�� sidePx ��

        // �E�y�C��
        Vector2 rmin = new Vector2(1, 0);
        Vector2 rmax = new Vector2(1, 1);
        rightPane.anchorMin = rmin;
        rightPane.anchorMax = rmax;
        rightPane.offsetMin = new Vector2(-sidePx, 0);        // ���[�� -sidePx ��
        rightPane.offsetMax = new Vector2(0, 0);              // �E�[��
    }
}
