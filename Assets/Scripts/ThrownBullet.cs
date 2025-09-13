using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

public class ThrownBullet : MonoBehaviour
{
    private Vector3 target;
    public float speed = 5f;

    [Header("Mask")]
    public GameObject maskPrefab; // 3×3サイズのSpriteMaskプレハブ
    public Tilemap wallTilemap;

    public LayerMask checkLayer;
    private bool hasArrived = false;

    public System.Action OnFinished;

    [Header("Mask Expansion Settings")]
    [Tooltip("マスク拡大に要する秒数")]
    public float expandDuration = 0.3f;
    [Tooltip("最終スケール（3×3Prefabなら1でOK）")]
    public float targetScale = 1.0f;

    [Header("Lifetime Link Options")]
    [Tooltip("trueにすると、弾がDestroyされた瞬間にマスクも同時に消える")]
    public bool destroyMaskWithBullet = false;
    [Tooltip("弾が拡大完了前に破壊されたときでもOnFinishedを発火して詰まりを防ぐ")]
    public bool notifyFinishedWhenBulletDestroyed = true;

    [Header("SFX (on bullet destroy)")]
    public AudioClip destroySE;
    [Range(0f, 1f)] public float destroySEVolume = 1f;
    public bool playSEOnDestroy = true;

    private bool _finishedNotified = false;
    private GameObject _maskInstance;   // 生成したマスク（弾とは非親子）
    private AudioSource _oneShotSrc;    // 事前に用意する一時用オーディオソース（ルートに置く）
    private GameObject _oneShotGO;
    private bool _sePlayed = false;

    public void SetTarget(Vector3 targetPos) { target = targetPos; }

    void Awake()
    {
        // ★ OnDestroyで新規生成しないため、最初にルートにAudioSourceを作って保持しておく（親子にしない）
        if (playSEOnDestroy)
        {
            _oneShotGO = new GameObject("_BulletOneShotSrc");
            _oneShotGO.transform.position = transform.position;
            _oneShotSrc = _oneShotGO.AddComponent<AudioSource>();
            _oneShotSrc.playOnAwake = false;
            _oneShotSrc.enabled = true;
            _oneShotSrc.spatialBlend = 1f;                 // 3D。2Dで良ければ0f
            _oneShotSrc.rolloffMode = AudioRolloffMode.Logarithmic;
            _oneShotSrc.dopplerLevel = 0f;
            _oneShotSrc.priority = 128;
            // 必要ならここでAudioMixerGroup等を設定
        }
    }

    void Update()
    {
        if (hasArrived) return;

        Vector3 direction = (target - transform.position).normalized;
        Vector3 nextPos = transform.position + direction * speed * Time.deltaTime;
        Vector3Int nextCell = wallTilemap != null ? wallTilemap.WorldToCell(nextPos) : Vector3Int.zero;

        // 壁でヒット扱い
        if (wallTilemap != null && wallTilemap.HasTile(nextCell))
        {
            Vector3Int currentCell = wallTilemap.WorldToCell(transform.position);
            Vector3 hitCenter = wallTilemap.GetCellCenterWorld(currentCell);
            BeginMaskAndFinish(hitCenter);
            hasArrived = true;
            return;
        }

        transform.position = nextPos;

        // 目標到達
        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            BeginMaskAndFinish(target);
            hasArrived = true;
        }
    }

    private void BeginMaskAndFinish(Vector3 pos)
    {
        // セル中心にスナップ
        Vector3 cellCenter = pos;
        if (wallTilemap != null)
        {
            Vector3Int cellPos = wallTilemap.WorldToCell(pos);
            cellCenter = wallTilemap.GetCellCenterWorld(cellPos);
        }
        transform.position = cellCenter;

        if (maskPrefab != null)
        {
            // 親子付けしない：弾と独立
            _maskInstance = Instantiate(maskPrefab, cellCenter, Quaternion.identity);

            // マスク側スクリプトがあればそれで拡大（完走）
            var auto = _maskInstance.GetComponent<MaskAutoExpand>();
            if (auto != null)
            {
                auto.duration = expandDuration;
                auto.targetScale = targetScale;
                auto.OnFinished.AddListener(() =>
                {
                    // 弾がまだ生きていても死んでいても、通知は一度だけ
                    NotifyFinishedOnce();
                });
            }
            else
            {
                // 保険：付いていない場合は弾側で拡大（親子付けしてないので弾が死んでも進む）
                _maskInstance.transform.localScale = Vector3.zero;
                StartCoroutine(ExpandMaskThenNotify(_maskInstance.transform));
            }
        }
        else
        {
            // マスクが無くても完了通知は行う
            NotifyFinishedOnce();
        }
    }

    private IEnumerator ExpandMaskThenNotify(Transform mask)
    {
        float t = 0f;
        Vector3 start = Vector3.zero;
        Vector3 goal = Vector3.one * targetScale; // 3×3Prefabなら1でOK

        while (t < expandDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / expandDuration);
            mask.localScale = Vector3.Lerp(start, goal, u);
            yield return null;
        }
        mask.localScale = goal;

        NotifyFinishedOnce();
    }

    private void NotifyFinishedOnce()
    {
        if (_finishedNotified) return;
        _finishedNotified = true;
        OnFinished?.Invoke();
    }

    // 弾がDestroyされたら（どの理由でも）ここに来る
    private void OnDestroy()
    {
        if (!Application.isPlaying) return;

        // マスク同時消滅オプション
        if (destroyMaskWithBullet && _maskInstance != null)
        {
            Destroy(_maskInstance);
            _maskInstance = null;
        }

        // 先に破壊された場合でも待ち受け側を進めたいなら通知
        if (notifyFinishedWhenBulletDestroyed)
        {
            NotifyFinishedOnce();
        }

        // ★ 破壊SE再生：OnDestroyで新規生成しない。事前生成したルートのAudioSourceを使う
        if (!_sePlayed && playSEOnDestroy && destroySE != null && _oneShotSrc != null && _oneShotGO != null)
        {
            _sePlayed = true;
            // 念のため有効化＆位置更新
            if (!_oneShotGO.activeSelf) _oneShotGO.SetActive(true);
            _oneShotSrc.enabled = true;

            _oneShotGO.transform.position = transform.position;
            _oneShotSrc.PlayOneShot(destroySE, destroySEVolume);

            // 再生後に後始末
            Destroy(_oneShotGO, destroySE.length + 0.05f);
        }
    }
}
