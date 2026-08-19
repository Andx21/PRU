using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thanh thông tin trên đầu màn hình: máu tổng, tiền, đợt hiện tại.
/// Nghe sự kiện từ GameManager/EnemySpawner chứ không dò trong Update.
/// </summary>
public class HUD : MonoBehaviour
{
    [Header("Chữ")]
    [SerializeField] private Text livesText;
    [SerializeField] private Text coinsText;
    [SerializeField] private Text waveText;

    [Header("Thanh máu tổng (không bắt buộc)")]
    [SerializeField] private Image livesFillBar;

    [Header("Tham chiếu")]
    [SerializeField] private EnemySpawner spawner;

    private int maxLivesSeen;

    private void Start()
    {
        if (spawner == null)
        {
            spawner = FindFirstObjectByType<EnemySpawner>();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLivesChanged += HandleLivesChanged;
            GameManager.Instance.OnCoinsChanged += HandleCoinsChanged;

            HandleLivesChanged(GameManager.Instance.Lives);
            HandleCoinsChanged(GameManager.Instance.Coins);
        }

        if (spawner != null)
        {
            spawner.OnWaveChanged += HandleWaveChanged;
            HandleWaveChanged(spawner.CurrentWave, spawner.TotalWaves);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLivesChanged -= HandleLivesChanged;
            GameManager.Instance.OnCoinsChanged -= HandleCoinsChanged;
        }

        if (spawner != null)
        {
            spawner.OnWaveChanged -= HandleWaveChanged;
        }
    }

    private void HandleLivesChanged(int lives)
    {
        // Máu ban đầu chính là máu cao nhất, ghi lại để tính % cho thanh máu.
        if (lives > maxLivesSeen)
        {
            maxLivesSeen = lives;
        }

        if (livesText != null)
        {
            livesText.text = lives.ToString();
        }

        if (livesFillBar != null && maxLivesSeen > 0)
        {
            livesFillBar.fillAmount = (float)lives / maxLivesSeen;
        }
    }

    private void HandleCoinsChanged(int coins)
    {
        if (coinsText != null)
        {
            coinsText.text = coins.ToString();
        }
    }

    private void HandleWaveChanged(int current, int total)
    {
        if (waveText == null)
            return;

        // Trước đợt 1 thì hiện 0 sẽ khó hiểu — hiện "Chuẩn bị..." cho rõ.
        waveText.text = current <= 0
            ? "Chuẩn bị..."
            : $"Đợt {current}/{total}";
    }
}
