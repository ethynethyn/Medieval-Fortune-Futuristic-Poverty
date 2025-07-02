using UnityEngine;
using UnityEngine.Audio;
using Cinemachine;
using StarterAssets;

public class SettingsApplier : MonoBehaviour
{
    public CinemachineVirtualCamera virtualCamera;
    public AudioMixer audioMixer;
    public FirstPersonController controller;
    public LayerMask pickupLayer; // Set to only include the "Pickup" layer

    void Awake()
    {
        LoadPlayerPosition();
        LoadPickupPositions();

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

    void LoadPlayerPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player && PlayerPrefs.HasKey("PlayerPosX"))
        {
            float x = PlayerPrefs.GetFloat("PlayerPosX");
            float y = PlayerPrefs.GetFloat("PlayerPosY");
            float z = PlayerPrefs.GetFloat("PlayerPosZ");
            player.transform.position = new Vector3(x, y, z);
        }
    }

    void LoadPickupPositions()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (((1 << obj.layer) & pickupLayer) != 0)
            {
                string key = "Pickup_" + obj.name;
                if (PlayerPrefs.HasKey(key + "_X"))
                {
                    float x = PlayerPrefs.GetFloat(key + "_X");
                    float y = PlayerPrefs.GetFloat(key + "_Y");
                    float z = PlayerPrefs.GetFloat(key + "_Z");
                    obj.transform.position = new Vector3(x, y, z);
                }
            }
        }
    }
}
