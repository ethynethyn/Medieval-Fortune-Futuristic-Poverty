using UnityEngine;

public class DisableWhileActive : MonoBehaviour
{
    [Header("Watched Object (e.g. UI Panel)")]
    public GameObject watchedObject;

    [Header("Object To Disable While Watched Object Is Active")]
    public GameObject targetToDisable;

    private bool wasActive;

    void Start()
    {
        if (watchedObject != null && targetToDisable != null)
        {
            wasActive = watchedObject.activeInHierarchy;
            UpdateTargetState(wasActive);
        }
    }

    void Update()
    {
        if (watchedObject == null || targetToDisable == null) return;

        bool isNowActive = watchedObject.activeInHierarchy;

        if (isNowActive != wasActive)
        {
            UpdateTargetState(isNowActive);
            wasActive = isNowActive;
        }
    }

    void UpdateTargetState(bool disableTarget)
    {
        targetToDisable.SetActive(!disableTarget);
    }
}
