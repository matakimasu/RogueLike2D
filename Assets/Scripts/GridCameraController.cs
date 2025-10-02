using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class GridCameraController : MonoBehaviour
{
    [Header("�Ǐ]�ݒ�")]
    public Transform target;            // �v���C���[�������蓖��
    public bool followEnabled = true;   // �Ǐ]ON/OFF�ؑ�

    [Header("�Y�[���ݒ�")]
    public float zoomStep = 2f;         // �Y�[���̒P��
    public float minSize = 3f;          // �ŏ��T�C�Y
    public float maxSize = 15f;         // �ő�T�C�Y

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
            pos.z = transform.position.z; // �J����Z�͌Œ�
            transform.position = pos;
        }
    }

    private void Update()
    {
        // �}�E�X�z�C�[���ŃY�[��
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float newSize = Mathf.Clamp(cam.orthographicSize - scroll * zoomStep, minSize, maxSize);
            cam.orthographicSize = newSize;
        }
    }

    // ===== �V�F�C�N�@�\ =====
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
