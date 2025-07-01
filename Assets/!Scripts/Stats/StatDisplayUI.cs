using UnityEngine;
using TMPro;

public class StatDisplayUI : MonoBehaviour
{
    [Header("Stat Source")]
    public Character character;
    public string statName = "Health";
    public TextMeshProUGUI statText;

    [Header("Formatting Options")]
    public bool showAsComplete = false;     // Complete / Incomplete
    public bool showAsBooleanText = false;  // YES / NO
    public bool showAsFraction = false;     // value / max
    public bool showOnlyNumber = false;     // just the number
    public bool showAsCurrency = false;     // use a prefix
    public bool showAsPercent = false;      // add % after number

    [Tooltip("Max value for fraction or percent")]
    public float maxValue = 100f;

    [Header("Advanced Formatting")]
    public string customCurrencySymbol = "$"; // 🆕 Add your own prefix!

    void Update()
    {
        if (character == null || statText == null) return;

        float value = character.GetStatValue(statName);
        string output = "";

        // Priority 1: Complete / Incomplete
        if (showAsComplete)
        {
            output = value >= 1 ? "Complete" : "Incomplete";
        }
        // Priority 2: YES / NO
        else if (showAsBooleanText)
        {
            output = value >= 1 ? "YES" : "NO";
        }
        // Priority 3: Fraction
        else if (showAsFraction)
        {
            output = $"{Mathf.FloorToInt(value)}/{Mathf.FloorToInt(maxValue)}";
        }
        // Priority 4: Only number
        else if (showOnlyNumber)
        {
            output = Mathf.FloorToInt(value).ToString();
        }
        // Priority 5: Default format with stat name
        else
        {
            output = $"{statName}: {Mathf.FloorToInt(value)}";
        }

        // Decide if we should strip the label
        bool stripStatName = (showAsCurrency || showAsPercent)
                             && !showAsFraction && !showAsComplete && !showAsBooleanText && !showOnlyNumber;

        // Add currency symbol if applicable
        if (showAsCurrency)
        {
            string prefix = string.IsNullOrEmpty(customCurrencySymbol) ? "$" : customCurrencySymbol;
            output = prefix + output;
        }

        // Add percent if applicable
        if (showAsPercent)
        {
            output += "%";
        }

        // Remove label if currency/percent is meant to be standalone
        if (stripStatName)
        {
            output = output.Replace(statName + ": ", "");
        }

        statText.text = output;
    }
}
