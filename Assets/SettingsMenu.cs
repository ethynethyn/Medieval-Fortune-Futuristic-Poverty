using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using Cinemachine;
using StarterAssets;

public class SettingsMenu : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider fovSlider;
    public Slider sfxSlider;
    public Slider musicSlider;
    public Slider voiceSlider;         // Added Voice volume slider
    public Slider ambienceSlider;      // Added Ambience volume slider
    public Slider mouseSensitivitySlider;
    public TMP_Dropdown windowModeDropdown;
    public TMP_Dropdown resolutionDropdown;

    [Header("References")]
    public CinemachineVirtualCamera virtualCamera;
    public AudioMixer audioMixer;
    public FirstPersonController controller;

    private Resolution[] resolutions;

    void Start()
    {
        ApplySavedSettings();          // Apply stored or current mixer settings to gameplay and UI

        // Add listeners for sliders and dropdowns
        fovSlider.onValueChanged.AddListener(SetFOV);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        voiceSlider.onValueChanged.AddListener(SetVoiceVolume);
        ambienceSlider.onValueChanged.AddListener(SetAmbienceVolume);
        mouseSensitivitySlider.onValueChanged.AddListener(SetMouseSensitivity);
        windowModeDropdown.onValueChanged.AddListener(SetWindowMode);

        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    void OnEnable()
    {
        SyncSlidersToCurrentSettings(); // Refresh UI when menu re-opens
    }

    // Applies saved settings or current AudioMixer values to game and UI
    public void ApplySavedSettings()
    {
        // --- FOV ---
        float savedFOV = PlayerPrefs.GetFloat("CameraFOV", 80f);
        if (virtualCamera != null)
            virtualCamera.m_Lens.FieldOfView = savedFOV;

        // --- SFX Volume ---
        if (!audioMixer.GetFloat("SFX", out float sfxDb))
        {
            float savedSFX = Mathf.Clamp(PlayerPrefs.GetFloat("SFXVolume", 0.75f), 0.001f, 2f);
            sfxDb = Mathf.Log10(savedSFX) * 20;
            audioMixer.SetFloat("SFX", sfxDb);
        }

        // --- Music Volume ---
        if (!audioMixer.GetFloat("Music", out float musicDb))
        {
            float savedMusic = Mathf.Clamp(PlayerPrefs.GetFloat("MusicVolume", 0.75f), 0.001f, 2f);
            musicDb = Mathf.Log10(savedMusic) * 20;
            audioMixer.SetFloat("Music", musicDb);
        }

        // --- Voice Volume ---
        if (!audioMixer.GetFloat("Voice", out float voiceDb))
        {
            float savedVoice = Mathf.Clamp(PlayerPrefs.GetFloat("VoiceVolume", 0.75f), 0.001f, 2f);
            voiceDb = Mathf.Log10(savedVoice) * 20;
            audioMixer.SetFloat("Voice", voiceDb);
        }

        // --- Ambience Volume ---
        if (!audioMixer.GetFloat("Ambience", out float ambienceDb))
        {
            float savedAmbience = Mathf.Clamp(PlayerPrefs.GetFloat("AmbienceVolume", 0.75f), 0.001f, 2f);
            ambienceDb = Mathf.Log10(savedAmbience) * 20;
            audioMixer.SetFloat("Ambience", ambienceDb);
        }

        // --- Mouse Sensitivity ---
        float savedSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 1f);
        if (controller != null)
            controller.RotationSpeed = savedSensitivity;

        // --- Window Mode ---
        windowModeDropdown.ClearOptions();
        windowModeDropdown.AddOptions(new System.Collections.Generic.List<string> { "Windowed", "Fullscreen" });
        windowModeDropdown.value = Screen.fullScreenMode == FullScreenMode.Windowed ? 0 : 1;

        // --- Resolutions ---
        if (resolutionDropdown != null)
        {
            resolutions = Screen.resolutions;
            resolutionDropdown.ClearOptions();

            var options = new System.Collections.Generic.List<string>();
            int current = 0;

            for (int i = 0; i < resolutions.Length; i++)
            {
                string res = $"{resolutions[i].width} x {resolutions[i].height} @ {resolutions[i].refreshRate}Hz";
                options.Add(res);

                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height &&
                    resolutions[i].refreshRate == Screen.currentResolution.refreshRate)
                {
                    current = i;
                }
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = current;
            resolutionDropdown.RefreshShownValue();
        }

        SyncSlidersToCurrentSettings(); // Make sure sliders match values now
    }

    // Sync UI sliders to current game settings and mixer values
    public void SyncSlidersToCurrentSettings()
    {
        // --- FOV ---
        if (virtualCamera != null)
            fovSlider.value = virtualCamera.m_Lens.FieldOfView;

        // --- SFX Volume ---
        if (audioMixer.GetFloat("SFX", out float sfxDb))
            sfxSlider.value = Mathf.Pow(10f, sfxDb / 20f);

        // --- Music Volume ---
        if (audioMixer.GetFloat("Music", out float musicDb))
            musicSlider.value = Mathf.Pow(10f, musicDb / 20f);

        // --- Voice Volume ---
        if (audioMixer.GetFloat("Voice", out float voiceDb))
            voiceSlider.value = Mathf.Pow(10f, voiceDb / 20f);

        // --- Ambience Volume ---
        if (audioMixer.GetFloat("Ambience", out float ambienceDb))
            ambienceSlider.value = Mathf.Pow(10f, ambienceDb / 20f);

        // --- Mouse Sensitivity ---
        if (controller != null)
            mouseSensitivitySlider.value = controller.RotationSpeed;
    }

    // --- Setters for each setting ---
    public void SetFOV(float value)
    {
        if (virtualCamera != null)
            virtualCamera.m_Lens.FieldOfView = value;

        PlayerPrefs.SetFloat("CameraFOV", value);
    }

    public void SetSFXVolume(float value)
    {
        audioMixer.SetFloat("SFX", Mathf.Log10(Mathf.Clamp(value, 0.001f, 2f)) * 20);
        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    public void SetMusicVolume(float value)
    {
        audioMixer.SetFloat("Music", Mathf.Log10(Mathf.Clamp(value, 0.001f, 2f)) * 20);
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    public void SetVoiceVolume(float value)
    {
        audioMixer.SetFloat("Voice", Mathf.Log10(Mathf.Clamp(value, 0.001f, 2f)) * 20);
        PlayerPrefs.SetFloat("VoiceVolume", value);
    }

    public void SetAmbienceVolume(float value)
    {
        audioMixer.SetFloat("Ambience", Mathf.Log10(Mathf.Clamp(value, 0.001f, 2f)) * 20);
        PlayerPrefs.SetFloat("AmbienceVolume", value);
    }

    public void SetMouseSensitivity(float value)
    {
        if (controller != null)
            controller.RotationSpeed = value;

        PlayerPrefs.SetFloat("MouseSensitivity", value);
    }

    public void SetWindowMode(int index)
    {
        Screen.fullScreenMode = index == 0 ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;
    }

    public void SetResolution(int index)
    {
        if (resolutions == null || index < 0 || index >= resolutions.Length) return;

        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreenMode, res.refreshRate);
    }
}
