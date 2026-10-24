
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle muteToggle;

    private const string VolumeKey = "MasterVolume";
    private const string MuteKey = "IsMuted";

    private void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        bool savedMute = PlayerPrefs.GetInt(MuteKey, 0) == 1;

        volumeSlider.SetValueWithoutNotify(savedVolume);
        muteToggle.SetIsOnWithoutNotify(savedMute);

        ApplyVolume();

        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        muteToggle.onValueChanged.AddListener(OnMuteChanged);
    }

    private void OnDestroy()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);

        if (muteToggle != null)
            muteToggle.onValueChanged.RemoveListener(OnMuteChanged);
    }

    private void OnVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(VolumeKey, value);
        ApplyVolume();
        PlayerPrefs.Save();
    }

    private void OnMuteChanged(bool isMuted)
    {
        PlayerPrefs.SetInt(MuteKey, isMuted ? 1 : 0);
        ApplyVolume();
        PlayerPrefs.Save();
    }

    private void ApplyVolume()
    {
        AudioListener.volume = muteToggle.isOn
            ? 0f
            : volumeSlider.value;
    }
}
