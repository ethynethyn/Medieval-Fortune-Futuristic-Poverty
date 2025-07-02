using UnityEngine;

public class MouseModeWhenActive : MonoBehaviour
{
    [Header("Target UI Object (e.g. Pause Menu)")]
    public GameObject uiObject;

    [Header("Lock Cursor When Inactive")]
    public bool lockCursorWhenUIHidden = true;

    private bool cursorIsUnlocked = false;

    void Update()
    {
        if (uiObject == null) return;

        if (uiObject.activeInHierarchy && !cursorIsUnlocked)
        {
            UnlockCursor();
        }
        else if (!uiObject.activeInHierarchy && cursorIsUnlocked && lockCursorWhenUIHidden)
        {
            LockCursor();
        }
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cursorIsUnlocked = true;
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cursorIsUnlocked = false;
    }
}
