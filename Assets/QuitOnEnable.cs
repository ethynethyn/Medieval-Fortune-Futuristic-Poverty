using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class QuitOnEnable : MonoBehaviour
{
    public Image fadeImage;           // Full-screen black UI Image
    public float fadeDuration = 1f;   // Seconds to fade to black
    public LayerMask pickupLayer;     // Set to only include the "Pickup" layer

    void OnEnable()
    {
        SavePlayerPosition();
        SavePickupPositions();
        PlayerPrefs.Save();

        StartCoroutine(FadeAndQuit());
    }

    void SavePlayerPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            Vector3 pos = player.transform.position;
            PlayerPrefs.SetFloat("PlayerPosX", pos.x);
            PlayerPrefs.SetFloat("PlayerPosY", pos.y);
            PlayerPrefs.SetFloat("PlayerPosZ", pos.z);
        }
    }

    void SavePickupPositions()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (((1 << obj.layer) & pickupLayer) != 0)
            {
                string key = "Pickup_" + obj.name;
                Vector3 pos = obj.transform.position;
                PlayerPrefs.SetFloat(key + "_X", pos.x);
                PlayerPrefs.SetFloat(key + "_Y", pos.y);
                PlayerPrefs.SetFloat(key + "_Z", pos.z);
            }
        }
    }

    IEnumerator FadeAndQuit()
    {
        float elapsed = 0f;

        if (fadeImage != null)
        {
            // Ensure fade image starts transparent
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeDuration);

            if (fadeImage != null)
            {
                Color c = fadeImage.color;
                c.a = alpha;
                fadeImage.color = c;
            }

            yield return null;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
