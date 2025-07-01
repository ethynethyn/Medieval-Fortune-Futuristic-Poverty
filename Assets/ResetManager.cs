using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class ResetWarningManager : MonoBehaviour
{
    public GameObject resetWarningUI;     // Warning message UI
    public Image fadeImage;               // Full-screen black UI Image
    public float warningTimeout = 5f;     // Seconds before warning disappears
    public float fadeDuration = 1f;       // Seconds to fade to black

    private bool isWaitingForConfirm = false;
    private float timer = 0f;
    private bool isFading = false;

    void Start()
    {
        if (resetWarningUI != null)
            resetWarningUI.SetActive(false);

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }

    void Update()
    {
        if (isFading) return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (!isWaitingForConfirm)
            {
                ShowResetWarning();
            }
            else
            {
                StartCoroutine(FadeAndReset());
            }
        }

        if (isWaitingForConfirm)
        {
            timer += Time.deltaTime;
            if (timer > warningTimeout)
            {
                CancelReset();
            }
        }
    }

    void ShowResetWarning()
    {
        isWaitingForConfirm = true;
        timer = 0f;

        if (resetWarningUI != null)
            resetWarningUI.SetActive(true);
    }

    void CancelReset()
    {
        isWaitingForConfirm = false;
        timer = 0f;

        if (resetWarningUI != null)
            resetWarningUI.SetActive(false);
    }

    IEnumerator FadeAndReset()
    {
        isFading = true;

        // Optional: Disable player controls here

        float elapsed = 0f;
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

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
