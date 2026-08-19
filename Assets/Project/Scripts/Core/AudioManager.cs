using UnityEngine;

/// <summary>
/// Âm thanh: nhạc nền + hiệu ứng. Âm lượng lưu bằng PlayerPrefs nên mở lại game
/// vẫn giữ nguyên mức người chơi đã chỉnh.
///
/// Mọi nơi gọi đều dùng dạng AudioManager.Instance?.PlayX() — thiếu AudioManager
/// trong scene thì game vẫn chạy, chỉ là không có tiếng.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string MusicVolumeKey = "td_music_volume";
    private const string SfxVolumeKey = "td_sfx_volume";

    [Header("Nguồn phát")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Nhạc nền")]
    [SerializeField] private AudioClip backgroundMusic;

    [Header("Hiệu ứng")]
    [SerializeField] private AudioClip towerShootClip;
    [SerializeField] private AudioClip enemyDeathClip;
    [SerializeField] private AudioClip buildClip;
    [SerializeField] private AudioClip baseHitClip;
    [SerializeField] private AudioClip errorClip;
    [SerializeField] private AudioClip victoryClip;
    [SerializeField] private AudioClip defeatClip;

    [Header("Giới hạn")]
    [Tooltip("Nhiều tháp bắn cùng lúc thì tiếng chồng nhau chói tai. Đây là khoảng cách tối thiểu giữa 2 tiếng súng (giây).")]
    [SerializeField] private float shootSfxCooldown = 0.05f;

    private float lastShootSfxTime = -999f;

    public float MusicVolume { get; private set; } = 0.5f;
    public float SfxVolume { get; private set; } = 0.8f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 0.5f);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 0.8f);

        ApplyVolumes();
    }

    private void Start()
    {
        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ───────────────────────────── Âm lượng ─────────────────────────────

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        ApplyVolumes();
    }

    public void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        if (musicSource != null)
        {
            musicSource.volume = MusicVolume;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = SfxVolume;
        }
    }

    // ───────────────────────────── Hiệu ứng ─────────────────────────────

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
            return;

        // Dùng PlayOneShot để các tiếng chồng lên nhau được, không cắt ngang tiếng trước.
        sfxSource.PlayOneShot(clip, SfxVolume);
    }

    public void PlayTowerShoot()
    {
        if (Time.unscaledTime - lastShootSfxTime < shootSfxCooldown)
            return;

        lastShootSfxTime = Time.unscaledTime;
        PlaySfx(towerShootClip);
    }

    public void PlayEnemyDeath() => PlaySfx(enemyDeathClip);
    public void PlayBuild() => PlaySfx(buildClip);
    public void PlayBaseHit() => PlaySfx(baseHitClip);
    public void PlayError() => PlaySfx(errorClip);
    public void PlayVictory() => PlaySfx(victoryClip);
    public void PlayDefeat() => PlaySfx(defeatClip);
}
