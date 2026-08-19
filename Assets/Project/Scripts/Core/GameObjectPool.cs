using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pool đối tượng dùng List&lt;GameObject&gt;.
///
/// Nguyên tắc: KHÔNG bao giờ Destroy đối tượng trong lúc chơi. Quái chết hay đạn
/// trúng đích thì chỉ SetActive(false) rồi nằm chờ trong list, lần sau cần thì
/// lấy lại. Instantiate/Destroy liên tục sinh rác cho GC, gây giật khi có nhiều
/// quái cùng lúc — đó là lý do dùng pool.
/// </summary>
public class GameObjectPool
{
    private readonly GameObject prefab;
    private readonly Transform parent;
    private readonly List<GameObject> items = new List<GameObject>();

    /// <summary>Hết đối tượng rảnh thì có được tạo thêm không.</summary>
    private readonly bool canGrow;

    public GameObjectPool(GameObject prefab, int initialSize, Transform parent, bool canGrow = true)
    {
        this.prefab = prefab;
        this.parent = parent;
        this.canGrow = canGrow;

        for (int i = 0; i < initialSize; i++)
        {
            CreateNew();
        }
    }

    public GameObject Prefab => prefab;

    /// <summary>Tổng số đối tượng pool đang giữ (cả đang dùng lẫn đang rảnh).</summary>
    public int Count => items.Count;

    private GameObject CreateNew()
    {
        GameObject item = Object.Instantiate(prefab, parent);
        item.SetActive(false);
        items.Add(item);
        return item;
    }

    /// <summary>
    /// Lấy một đối tượng rảnh ra dùng. Trả về null nếu hết và pool không được phép nở thêm.
    /// Đối tượng trả về vẫn đang tắt — bên gọi tự đặt vị trí rồi SetActive(true).
    /// </summary>
    public GameObject Get()
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && !items[i].activeInHierarchy)
            {
                return items[i];
            }
        }

        if (!canGrow)
        {
            return null;
        }

        return CreateNew();
    }

    /// <summary>Trả đối tượng về pool. Chỉ tắt đi, không Destroy.</summary>
    public void Return(GameObject item)
    {
        if (item == null)
            return;

        item.SetActive(false);
    }

    /// <summary>Tắt sạch mọi đối tượng đang hoạt động — dùng khi chơi lại hoặc hết đợt.</summary>
    public void ReturnAll()
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null)
            {
                items[i].SetActive(false);
            }
        }
    }
}
