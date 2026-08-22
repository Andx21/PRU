using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Xử lý việc chọn loại tháp ở shop rồi bấm lên đế để xây.
///
/// Lưu ý: project bật Active Input Handling = "Input System Package (New)",
/// nên KHÔNG dùng được UnityEngine.Input kiểu cũ — phải đọc chuột qua
/// <see cref="Mouse.current"/>, nếu không sẽ ném lỗi ngay lúc chạy.
/// </summary>
public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance { get; private set; }

    [Header("Tham chiếu")]
    [Tooltip("Prefab tháp dùng chung cho cả 3 loại. Sprite và chỉ số lấy từ TowerStats.")]
    [SerializeField] private GameObject towerPrefab;

    [SerializeField] private Camera gameCamera;

    /// <summary>Loại tháp đang chọn mua. null = không đang mua gì.</summary>
    public TowerStats SelectedTower { get; private set; }

    public event Action<TowerStats> OnSelectionChanged;

    private TowerSlot hoveredSlot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (gameCamera == null)
        {
            gameCamera = Camera.main;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
        {
            ClearHover();
            return;
        }

        if (Mouse.current == null)
            return;

        // Bấm nút trong shop thì không được coi là bấm xuống map.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            ClearHover();
            return;
        }

        UpdateHover();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleLeftClick();
        }

        // Chuột phải = bỏ chọn, khỏi phải bấm lại đúng nút đang sáng.
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            SelectTower(null);
        }
    }

    // ───────────────────────────── Chọn tháp ─────────────────────────────

    /// <summary>Nút trong shop gọi hàm này. Bấm lại đúng nút đang chọn thì bỏ chọn.</summary>
    public void SelectTower(TowerStats stats)
    {
        SelectedTower = SelectedTower == stats ? null : stats;

        if (SelectedTower == null)
        {
            ClearHover();
        }

        OnSelectionChanged?.Invoke(SelectedTower);
    }

    // ───────────────────────────── Chuột ─────────────────────────────

    private Vector3 GetMouseWorldPosition()
    {
        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector3 world = gameCamera.ScreenToWorldPoint(screenPosition);
        world.z = 0f;
        return world;
    }

    private TowerSlot FindSlotUnderMouse()
    {
        Vector3 mouseWorld = GetMouseWorldPosition();

        TowerSlot nearest = null;
        float nearestDistance = float.MaxValue;

        var slots = TowerSlot.AllSlots;

        for (int i = 0; i < slots.Count; i++)
        {
            TowerSlot slot = slots[i];

            if (slot == null)
                continue;

            float distance = Vector2.Distance(mouseWorld, slot.transform.position);

            if (distance <= slot.ClickRadius && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = slot;
            }
        }

        return nearest;
    }

    private void UpdateHover()
    {
        TowerSlot slot = SelectedTower != null ? FindSlotUnderMouse() : null;

        if (slot == hoveredSlot)
            return;

        if (hoveredSlot != null)
        {
            hoveredSlot.SetHighlight(false, true);
        }

        hoveredSlot = slot;

        if (hoveredSlot != null)
        {
            bool canAfford = GameManager.Instance != null
                             && GameManager.Instance.CanAfford(SelectedTower.cost);

            hoveredSlot.SetHighlight(true, canAfford);
        }
    }

    private void ClearHover()
    {
        if (hoveredSlot != null)
        {
            hoveredSlot.SetHighlight(false, true);
            hoveredSlot = null;
        }
    }

    private void HandleLeftClick()
    {
        if (SelectedTower == null)
            return;

        TowerSlot slot = FindSlotUnderMouse();

        if (slot == null)
            return;

        TryBuild(slot);
    }

    // ───────────────────────────── Xây ─────────────────────────────

    public bool TryBuild(TowerSlot slot)
    {
        if (slot == null || SelectedTower == null)
            return false;

        if (!slot.IsEmpty)
        {
            AudioManager.Instance?.PlayError();
            return false;
        }

        if (towerPrefab == null)
        {
            Debug.LogError("[BuildManager] Chưa gán Tower Prefab.", this);
            return false;
        }

        if (GameManager.Instance == null || !GameManager.Instance.SpendCoins(SelectedTower.cost))
        {
            AudioManager.Instance?.PlayError();
            return false;
        }

        GameObject towerObject = Instantiate(towerPrefab, slot.transform.position, Quaternion.identity);
        towerObject.name = $"Tower_{SelectedTower.displayName}";

        Tower tower = towerObject.GetComponent<Tower>();

        if (tower == null)
        {
            Debug.LogError("[BuildManager] Tower Prefab thiếu component Tower.", towerPrefab);
            Destroy(towerObject);
            return false;
        }

        tower.Initialize(SelectedTower);
        slot.SetBuiltTower(tower);

        AudioManager.Instance?.PlayBuild();

        // Xây xong thì bỏ chọn, tránh bấm nhầm phát nữa mất tiền oan.
        SelectTower(null);

        return true;
    }
}
