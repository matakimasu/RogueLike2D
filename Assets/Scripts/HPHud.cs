using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HPHud : MonoBehaviour
{
    [Header("���ׂ��")]
    public RectTransform container;      // �� HPBar ���g�����蓖��
    [Header("1���̃v���n�u")]
    public GameObject iconPrefab;        // �� HPIcon.prefab

    [Header("�\���I�v�V����")]
    public bool showEmptyToMax = false;  // true: �ő�l�܂ŋ�A�C�R�������ׂ�
    public Sprite emptyIconSprite;       // ��\���p�i�C�ӁF���ݒ�Ȃ��\���j

    private readonly List<Image> pool = new List<Image>();

    public void Refresh(int current, int max)
    {
        if (container == null || iconPrefab == null) return;
        if (current < 0) current = 0;
        if (max < 0) max = 0;
        int need = showEmptyToMax ? max : current;

        // �v�[����[
        while (pool.Count < need)
        {
            var go = Instantiate(iconPrefab, container);
            var img = go.GetComponent<Image>();
            pool.Add(img);
        }

        // �K�v�������L����
        for (int i = 0; i < pool.Count; i++)
        {
            bool active = i < need;
            if (pool[i].gameObject.activeSelf != active)
                pool[i].gameObject.SetActive(active);
        }

        // �����ڍX�V
        for (int i = 0; i < need; i++)
        {
            var img = pool[i];

            if (showEmptyToMax && i >= current)
            {
                // ��A�C�R���i�ő�l�܂ŕ��ׂ�ꍇ�j
                if (emptyIconSprite != null)
                    img.sprite = emptyIconSprite;
                img.color = Color.white; // ��A�C�R���͌��F�̂܂�
            }
            else
            {
                // ����HP���͖��^���A�C�R��
                // ���^���� prefab �̌����ڂ��̂܂܁i�F�ς��s�v�j
                // ��F�F�ŏo�������Ȃ� img.color = new Color(1f, 0f, 0f, 1f);
            }
        }
    }
}
