using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Màn hình kết thúc — dùng chung cho cả thắng và thua, chỉ đổi tiêu đề và màu.
/// </summary>
public class GameEndPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [Header("Nội dung")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text messageText;

    [Header("Nút")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    [Header("Chữ hiển thị")]
    [SerializeField] private string wonTitle = "CHIẾN THẮNG";
    [SerializeField] private string lostTitle = "THẤT BẠI";
    [SerializeField] private Color wonColor = new Color(0.3f, 0.9f, 0.4f);
    [SerializeField] private Color lostColor = new Color(0.95f, 0.35f, 0.35f);

    private void Awake()
    {
        if (restartButton != null) restartButton.onClick.AddListener(HandleRestart);
        if (quitButton != null) quitButton.onClick.AddListener(HandleQuit);
    }

    private void Start()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += HandleStateChanged;
        }
    }

    private void OnDestroy()
    {
        if (restartButton != null) restartButton.onClick.RemoveListener(HandleRestart);
        if (quitButton != null) quitButton.onClick.RemoveListener(HandleQuit);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(GameState state)
    {
        if (state != GameState.Won && state != GameState.Lost)
            return;

        bool won = state == GameState.Won;

        if (titleText != null)
        {
            titleText.text = won ? wonTitle : lostTitle;
            titleText.color = won ? wonColor : lostColor;
        }

        if (messageText != null)
        {
            messageText.text = won
                ? "Bạn đã chặn được toàn bộ các đợt tấn công."
                : $"Base đã thất thủ ở đợt {GetCurrentWaveLabel()}.";
        }

        if (won)
        {
            AudioManager.Instance?.PlayVictory();
        }
        else
        {
            AudioManager.Instance?.PlayDefeat();
        }

        if (panel != null)
        {
            panel.SetActive(true);
        }
    }

    private string GetCurrentWaveLabel()
    {
        EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();

        return spawner != null
            ? $"{spawner.CurrentWave}/{spawner.TotalWaves}"
            : "?";
    }

    private void HandleRestart()
    {
        GameManager.Instance?.Restart();
    }

    private void HandleQuit()
    {
        GameManager.Instance?.QuitGame();
    }
}
