using UnityEngine;

public class PauseDisabler : MonoBehaviour
{
    public GameObject pauseMenu;
    public MonoBehaviour componentToDisable; // drag your FirstPersonController here

    void Update()
    {
        if (pauseMenu != null && componentToDisable != null)
            componentToDisable.enabled = !pauseMenu.activeInHierarchy;
    }
}
