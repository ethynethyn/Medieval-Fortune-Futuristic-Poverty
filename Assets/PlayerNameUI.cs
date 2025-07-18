using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class PlayerNameUI : MonoBehaviour
{
    public TMP_InputField nameInputField;
    public TextMeshProUGUI displayText;

    [Header("Objects to Activate After Name Entry")]
    public List<GameObject> objectsToActivate;

    [Header("Character Creator Parent Object")]
    public GameObject characterCreatorObject;

    private string playerName;

    private void Start()
    {
        // Unlock the cursor at start
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Deactivate all gameplay-related objects at start
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        if (PlayerPrefs.HasKey("PlayerName"))
        {
            // Load player name
            playerName = PlayerPrefs.GetString("PlayerName");
            displayText.text = "Welcome back, " + playerName + "!";

            // Hide input and character creator UI
            if (nameInputField != null)
                nameInputField.gameObject.SetActive(false);

            if (characterCreatorObject != null)
                characterCreatorObject.SetActive(false);

            // Load saved facial feature states
            LoadFacialFeatures();

            // Continue with game
            ActivateObjects();
            LockCursor();
        }
        else
        {
            // Set up name input listener
            nameInputField.onSubmit.AddListener(SubmitName);
            nameInputField.ActivateInputField();

            // Show character creator at start
            if (characterCreatorObject != null)
                characterCreatorObject.SetActive(true);
        }
    }

    private void Update()
    {
        // Keep the input field focused while active
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

            // Save name
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.Save();

            displayText.text = "Welcome, " + playerName + "!";

            // Hide UI elements
            if (nameInputField != null)
                nameInputField.gameObject.SetActive(false);

            if (characterCreatorObject != null)
                characterCreatorObject.SetActive(false);

            // Save facial features
            SaveFacialFeatures();

            // Activate game objects
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

    private void SaveFacialFeatures()
    {
        if (characterCreatorObject == null) return;

        Transform[] allChildren = characterCreatorObject.GetComponentsInChildren<Transform>(true);

        foreach (Transform part in allChildren)
        {
            if (part == characterCreatorObject.transform) continue; // skip root object

            string key = "FacePart_" + part.name;
            PlayerPrefs.SetInt(key, part.gameObject.activeSelf ? 1 : 0);
        }

        PlayerPrefs.Save();
    }

    private void LoadFacialFeatures()
    {
        if (characterCreatorObject == null) return;

        Transform[] allChildren = characterCreatorObject.GetComponentsInChildren<Transform>(true);

        foreach (Transform part in allChildren)
        {
            if (part == characterCreatorObject.transform) continue; // skip root object

            string key = "FacePart_" + part.name;
            if (PlayerPrefs.HasKey(key))
            {
                bool isActive = PlayerPrefs.GetInt(key) == 1;
                part.gameObject.SetActive(isActive);
            }
        }
    }
}
