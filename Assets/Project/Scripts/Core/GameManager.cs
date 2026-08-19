using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Playing,
    Paused,
    Won,
    Lost
}

/// <summary>
/// Giữ trạng thái ván chơi: máu tổng của Base, số tiền, thắng/thua, tạm dừng.
/// Các màn UI nghe sự kiện ở đây chứ không tự đi hỏi từng nơi.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Thông số ban đầu")]
    [SerializeField] private int startingLives = 20;
    [SerializeField] private int startingCoins = 150;

    public event Action<int> OnLivesChanged;
    public event Action<int> OnCoinsChanged;
    public event Action<GameState> OnStateChanged;

    public int Lives { get; private set; }
    public int Coins { get; private set; }
    public GameState State { get; private set; } = GameState.Playing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Danh sách quái sống là static nên không tự sạch khi nạp lại scene.
        // Không dọn ở đây thì sau khi "Chơi lại" các tháp sẽ ngắm vào quái ma.
        Enemy.ClearRegistry();

        Lives = startingLives;
        Coins = startingCoins;
    }

    private void Start()
    {
        // Đề bài: bắt đầu là chơi luôn, không qua menu chính.
        Time.timeScale = 1f;
        SetState(GameState.Playing);

        OnLivesChanged?.Invoke(Lives);
        OnCoinsChanged?.Invoke(Coins);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ───────────────────────────── Tiền ─────────────────────────────

    public bool CanAfford(int amount) => Coins >= amount;

    public void AddCoins(int amount)
    {
        if (amount <= 0)
            return;

        Coins += amount;
        OnCoinsChanged?.Invoke(Coins);
    }

    /// <summary>Trừ tiền khi xây tháp. Trả về false nếu không đủ.</summary>
    public bool SpendCoins(int amount)
    {
        if (amount < 0 || Coins < amount)
            return false;

        Coins -= amount;
        OnCoinsChanged?.Invoke(Coins);
        return true;
    }

    // ───────────────────────────── Máu ─────────────────────────────

    /// <summary>Quái lọt về Base thì trừ máu tổng.</summary>
    public void TakeDamage(int amount)
    {
        if (State != GameState.Playing || amount <= 0)
            return;

        Lives = Mathf.Max(0, Lives - amount);
        OnLivesChanged?.Invoke(Lives);

        AudioManager.Instance?.PlayBaseHit();

        if (Lives == 0)
        {
            SetState(GameState.Lost);
        }
    }

    // ──────────────────────── Thắng / thua / dừng ────────────────────────

    /// <summary>EnemySpawner gọi khi đã hết đợt cuối và không còn con quái nào sống.</summary>
    public void ReportAllWavesCleared()
    {
        if (State == GameState.Playing)
        {
            SetState(GameState.Won);
        }
    }

    public void Pause()
    {
        if (State != GameState.Playing)
            return;

        Time.timeScale = 0f;
        SetState(GameState.Paused);
    }

    public void Resume()
    {
        if (State != GameState.Paused)
            return;

        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

    public void Restart()
    {
        // Phải trả timeScale về 1 TRƯỚC khi nạp lại, không thì scene mới đứng hình.
        Time.timeScale = 1f;
        Enemy.ClearRegistry();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetState(GameState newState)
    {
        if (State == newState)
            return;

        State = newState;

        if (newState == GameState.Won || newState == GameState.Lost)
        {
            Time.timeScale = 0f;
        }

        OnStateChanged?.Invoke(newState);
    }
}
