using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Một nút mua tháp trong shop. Gắn vào mỗi nút rồi kéo TowerStats tương ứng vào.
/// Nút tự mờ đi khi không đủ tiền và tự sáng lên khi đang được chọn.
/// </summary>
[RequireComponent(typeof(Button))]
public class TowerShopButton : MonoBehaviour
{
    [Header("Loại tháp")]
    [SerializeField] private TowerStats towerStats;

    [Header("Hiển thị")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Text costText;

    [Tooltip("Viền/nền sáng lên khi đang chọn loại tháp này.")]
    [SerializeField] private GameObject selectedIndicator;

    [Header("Màu")]
    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color tooExpensiveColor = new Color(0.55f, 0.55f, 0.55f);

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(HandleClick);
    }

    private void Start()
    {
        if (towerStats == null)
        {
            Debug.LogError($"[TowerShopButton] Nút '{name}' chưa gán TowerStats.", this);
            return;
        }

        if (iconImage != null && towerStats.shopIcon != null)
        {
            iconImage.sprite = towerStats.shopIcon;
        }

        if (nameText != null)
        {
            nameText.text = towerStats.displayName;
        }

        if (costText != null)
        {
            costText.text = towerStats.cost.ToString();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCoinsChanged += HandleCoinsChanged;
            HandleCoinsChanged(GameManager.Instance.Coins);
        }

        if (BuildManager.Instance != null)
        {
            BuildManager.Instance.OnSelectionChanged += HandleSelectionChanged;
            HandleSelectionChanged(BuildManager.Instance.SelectedTower);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCoinsChanged -= HandleCoinsChanged;
        }

        if (BuildManager.Instance != null)
        {
            BuildManager.Instance.OnSelectionChanged -= HandleSelectionChanged;
        }
    }

    private void HandleClick()
    {
        BuildManager.Instance?.SelectTower(towerStats);
    }

    private void HandleCoinsChanged(int coins)
    {
        if (towerStats == null)
            return;

        bool canAfford = coins >= towerStats.cost;

        if (iconImage != null)
        {
            iconImage.color = canAfford ? affordableColor : tooExpensiveColor;
        }

        if (costText != null)
        {
            costText.color = canAfford ? affordableColor : tooExpensiveColor;
        }

        // Vẫn cho bấm để xem thông tin, chỉ báo màu là không mua nổi.
        // Lúc thả tháp xuống đế mà thiếu tiền thì BuildManager sẽ chặn.
    }

    private void HandleSelectionChanged(TowerStats selected)
    {
        if (selectedIndicator != null)
        {
            selectedIndicator.SetActive(selected == towerStats);
        }
    }
}
