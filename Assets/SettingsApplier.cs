using UnityEngine;
using UnityEngine.Audio;
using Cinemachine;
using StarterAssets;

public class SettingsApplier : MonoBehaviour
{
    public CinemachineVirtualCamera virtualCamera;
    public AudioMixer audioMixer;
    public FirstPersonController controller;

    void Awake()
    {
        // Apply FOV
        float savedFOV = PlayerPrefs.GetFloat("CameraFOV", 80f);
        if (virtualCamera != null)
            virtualCamera.m_Lens.FieldOfView = savedFOV;

        // Apply SFX
        float savedSFX = Mathf.Clamp(PlayerPrefs.GetFloat("SFXVolume", 0.75f), 0.001f, 2f);
        audioMixer.SetFloat("SFX", Mathf.Log10(savedSFX) * 20);

        // Apply Music
        float savedMusic = Mathf.Clamp(PlayerPrefs.GetFloat("MusicVolume", 0.75f), 0.001f, 2f);
        audioMixer.SetFloat("Music", Mathf.Log10(savedMusic) * 20);

        // Apply Voice
        float savedVoice = Mathf.Clamp(PlayerPrefs.GetFloat("VoiceVolume", 0.75f), 0.001f, 2f);
        audioMixer.SetFloat("Voice", Mathf.Log10(savedVoice) * 20);

        // Apply Ambience
        float savedAmbience = Mathf.Clamp(PlayerPrefs.GetFloat("AmbienceVolume", 0.75f), 0.001f, 2f);
        audioMixer.SetFloat("Ambience", Mathf.Log10(savedAmbience) * 20);

        // Apply Sensitivity
        float savedSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 1f);
        if (controller != null)
            controller.RotationSpeed = savedSensitivity;
    }
}
