using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bảng cài đặt âm thanh: hai thanh trượt nhạc nền và hiệu ứng.
/// Mức âm lượng do AudioManager lưu bằng PlayerPrefs.
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [Header("Thanh trượt")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Chữ hiển thị % (không bắt buộc)")]
    [SerializeField] private Text musicValueText;
    [SerializeField] private Text sfxValueText;

    private void OnEnable()
    {
        // Gán giá trị TRƯỚC khi nối sự kiện, không thì lúc mở bảng slider sẽ
        // tự bắn onValueChanged và ghi đè âm lượng bằng giá trị mặc định.
        if (AudioManager.Instance != null)
        {
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
        }

        if (musicSlider != null) musicSlider.onValueChanged.AddListener(HandleMusicChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(HandleSfxChanged);

        RefreshLabels();
    }

    private void OnDisable()
    {
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(HandleMusicChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(HandleSfxChanged);
    }

    private void HandleMusicChanged(float value)
    {
        AudioManager.Instance?.SetMusicVolume(value);
        RefreshLabels();
    }

    private void HandleSfxChanged(float value)
    {
        AudioManager.Instance?.SetSfxVolume(value);
        RefreshLabels();
    }

    private void RefreshLabels()
    {
        if (musicValueText != null && musicSlider != null)
        {
            musicValueText.text = Mathf.RoundToInt(musicSlider.value * 100f) + "%";
        }

        if (sfxValueText != null && sfxSlider != null)
        {
            sfxValueText.text = Mathf.RoundToInt(sfxSlider.value * 100f) + "%";
        }
    }
}
