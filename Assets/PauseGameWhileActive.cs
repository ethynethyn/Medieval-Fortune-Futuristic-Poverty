using UnityEngine;

public class PauseGameWhileActive : MonoBehaviour
{
    [Header("UI Object That Triggers Pause (e.g. Pause Menu)")]
    public GameObject pauseTriggerObject;

    [Header("Optional")]
    public bool resetTimeScaleOnDisable = true;

    private bool gameIsPaused = false;

    void Update()
    {
        if (pauseTriggerObject == null) return;

        if (pauseTriggerObject.activeInHierarchy && !gameIsPaused)
        {
            Time.timeScale = 0f;
            gameIsPaused = true;
        }
        else if (!pauseTriggerObject.activeInHierarchy && gameIsPaused)
        {
            if (resetTimeScaleOnDisable)
                Time.timeScale = 1f;

            gameIsPaused = false;
        }
    }

    void OnDisable()
    {
        if (resetTimeScaleOnDisable)
            Time.timeScale = 1f;
    }
}
