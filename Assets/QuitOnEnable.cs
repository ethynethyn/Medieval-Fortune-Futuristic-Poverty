using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class QuitOnEnable : MonoBehaviour
{
    public Image fadeImage;           // Full-screen black UI Image
    public float fadeDuration = 1f;   // Seconds to fade to black

    void OnEnable()
    {
        StartCoroutine(FadeAndQuit());
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

        // Quit the game
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;  // Stops play mode in Editor
#else
        Application.Quit();  // Quits build
#endif
    }
}
