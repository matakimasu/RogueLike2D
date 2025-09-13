using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HPHud : MonoBehaviour
{
    [Header("並べる先")]
    public RectTransform container;      // ← HPBar 自身を割り当て
    [Header("1個分のプレハブ")]
    public GameObject iconPrefab;        // ← HPIcon.prefab

    [Header("表示オプション")]
    public bool showEmptyToMax = false;  // true: 最大値まで空アイコンも並べる
    public Sprite emptyIconSprite;       // 空表示用（任意：未設定なら非表示）

    private readonly List<Image> pool = new List<Image>();

    public void Refresh(int current, int max)
    {
        if (container == null || iconPrefab == null) return;
        if (current < 0) current = 0;
        if (max < 0) max = 0;
        int need = showEmptyToMax ? max : current;

        // プール補充
        while (pool.Count < need)
        {
            var go = Instantiate(iconPrefab, container);
            var img = go.GetComponent<Image>();
            pool.Add(img);
        }

        // 必要数だけ有効化
        for (int i = 0; i < pool.Count; i++)
        {
            bool active = i < need;
            if (pool[i].gameObject.activeSelf != active)
                pool[i].gameObject.SetActive(active);
        }

        // 見た目更新
        for (int i = 0; i < need; i++)
        {
            var img = pool[i];

            if (showEmptyToMax && i >= current)
            {
                // 空アイコン（最大値まで並べる場合）
                if (emptyIconSprite != null)
                    img.sprite = emptyIconSprite;
                img.color = Color.white; // 空アイコンは元色のまま
            }
            else
            {
                // 現在HP分は満タンアイコン
                // 満タンは prefab の見た目そのまま（色変え不要）
                // 例：色で出したいなら img.color = new Color(1f, 0f, 0f, 1f);
            }
        }
    }
}
