using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class PlayerNameUI : MonoBehaviour
{
    public TMP_InputField nameInputField;
    public TextMeshProUGUI displayText;

    [Header("Objects to Activate After Name Entry")]
    public List<GameObject> objectsToActivate;

    private string playerName;

    private void Start()
    {
        // Deactivate all listed objects at start
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        if (PlayerPrefs.HasKey("PlayerName"))
        {
            playerName = PlayerPrefs.GetString("PlayerName");
            displayText.text = "Welcome back, " + playerName + "!";
            nameInputField.gameObject.SetActive(false);

            ActivateObjects();
            LockCursor();
        }
        else
        {
            nameInputField.onSubmit.AddListener(SubmitName);
            nameInputField.ActivateInputField(); // Initial focus
        }
    }

    private void Update()
    {
        // Keep the input field selected while it's active
        if (nameInputField != null && nameInputField.gameObject.activeInHierarchy && !nameInputField.isFocused)
        {
            nameInputField.ActivateInputField();
        }
    }

    private void SubmitName(string submittedText)
    {
        if (!string.IsNullOrWhiteSpace(submittedText))
        {
            playerName = submittedText.Trim();
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.Save();

            displayText.text = "Welcome, " + playerName + "!";
            nameInputField.gameObject.SetActive(false);

            ActivateObjects();
            LockCursor();
        }
    }

    private void ActivateObjects()
    {
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null)
                obj.SetActive(true);
        }
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
