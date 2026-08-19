using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Đế tháp — chỗ được phép đặt tháp. Đặt sẵn trong scene dọc theo đường đi.
/// Mỗi đế chỉ chứa được một tháp.
/// </summary>
public class TowerSlot : MonoBehaviour
{
    [Header("Hiển thị")]
    [SerializeField] private SpriteRenderer slotRenderer;

    [SerializeField] private Color normalColor = Color.white;

    [Tooltip("Màu khi rê chuột vào lúc đang chọn mua tháp.")]
    [SerializeField] private Color highlightColor = new Color(0.6f, 1f, 0.6f);

    [Tooltip("Màu khi rê chuột vào mà không đủ tiền.")]
    [SerializeField] private Color cannotAffordColor = new Color(1f, 0.5f, 0.5f);

    [Header("Vùng bấm")]
    [Tooltip("Bán kính tính là bấm trúng đế này (unit).")]
    [SerializeField] private float clickRadius = 0.5f;

    private static readonly List<TowerSlot> allSlots = new List<TowerSlot>();

    public static IReadOnlyList<TowerSlot> AllSlots => allSlots;

    public Tower BuiltTower { get; private set; }
    public bool IsEmpty => BuiltTower == null;
    public float ClickRadius => clickRadius;

    private void Awake()
    {
        if (slotRenderer == null)
        {
            slotRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void OnEnable()
    {
        if (!allSlots.Contains(this))
        {
            allSlots.Add(this);
        }

        SetHighlight(false, true);
    }

    private void OnDisable()
    {
        allSlots.Remove(this);
    }

    public void SetBuiltTower(Tower tower)
    {
        BuiltTower = tower;
        SetHighlight(false, true);
    }

    /// <summary>Tô màu đế khi người chơi đang chọn mua và rê chuột lên.</summary>
    public void SetHighlight(bool active, bool canAfford)
    {
        if (slotRenderer == null)
            return;

        if (!active || !IsEmpty)
        {
            slotRenderer.color = normalColor;
            return;
        }

        slotRenderer.color = canAfford ? highlightColor : cannotAffordColor;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsEmpty ? Color.green : Color.grey;
        Gizmos.DrawWireSphere(transform.position, clickRadius);
    }
}
