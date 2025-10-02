using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class GridCameraController : MonoBehaviour
{
    [Header("追従設定")]
    public Transform target;            // プレイヤー等を割り当て
    public bool followEnabled = true;   // 追従ON/OFF切替

    [Header("ズーム設定")]
    public float zoomStep = 2f;         // ズームの単位
    public float minSize = 3f;          // 最小サイズ
    public float maxSize = 15f;         // 最大サイズ

    private Camera cam;
    private Vector3 shakeOffset = Vector3.zero;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (target != null && followEnabled)
        {
            Vector3 pos = target.position + shakeOffset;
            pos.z = transform.position.z; // カメラZは固定
            transform.position = pos;
        }
    }

    private void Update()
    {
        // マウスホイールでズーム
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float newSize = Mathf.Clamp(cam.orthographicSize - scroll * zoomStep, minSize, maxSize);
            cam.orthographicSize = newSize;
        }
    }

    // ===== シェイク機能 =====
    public void Shake(float duration, float magnitude)
    {
        StartCoroutine(DoShake(duration, magnitude));
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            shakeOffset = new Vector3(
                Random.Range(-1f, 1f) * magnitude,
                Random.Range(-1f, 1f) * magnitude,
                0f
            );
            elapsed += Time.deltaTime;
            yield return null;
        }
        shakeOffset = Vector3.zero;
    }
}
