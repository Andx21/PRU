using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Menu tạm dừng: tiếp tục, chơi lại, cài đặt, thoát.
/// Bấm nút Pause trên HUD hoặc phím Esc đều mở được.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Nút")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        if (pauseButton != null) pauseButton.onClick.AddListener(Pause);
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (closeSettingsButton != null) closeSettingsButton.onClick.AddListener(CloseSettings);
        if (quitButton != null) quitButton.onClick.AddListener(Quit);
    }

    private void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (pauseButton != null) pauseButton.onClick.RemoveListener(Pause);
        if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
        if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
        if (settingsButton != null) settingsButton.onClick.RemoveListener(OpenSettings);
        if (closeSettingsButton != null) closeSettingsButton.onClick.RemoveListener(CloseSettings);
        if (quitButton != null) quitButton.onClick.RemoveListener(Quit);
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (!Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        if (GameManager.Instance == null)
            return;

        // Đang mở cài đặt thì Esc chỉ đóng cài đặt, chưa thoát khỏi menu dừng.
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            CloseSettings();
            return;
        }

        if (GameManager.Instance.State == GameState.Playing)
        {
            Pause();
        }
        else if (GameManager.Instance.State == GameState.Paused)
        {
            Resume();
        }
    }

    public void Pause()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
            return;

        GameManager.Instance.Pause();

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
    }

    public void Resume()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        GameManager.Instance?.Resume();
    }

    public void Restart()
    {
        GameManager.Instance?.Restart();
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void Quit()
    {
        GameManager.Instance?.QuitGame();
    }
}
