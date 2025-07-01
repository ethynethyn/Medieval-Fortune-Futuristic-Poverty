using UnityEngine;
using System.Collections.Generic;

public class Character : MonoBehaviour
{
    [Header("Character Identity")]
    public string characterName = "Unnamed";

    [Header("Character Stats")]
    public List<CharacterStat> stats = new List<CharacterStat>();

    public float GetStatValue(string statName)
    {
        foreach (var stat in stats)
        {
            if (stat.definition != null && stat.definition.statName == statName)
                return stat.currentValue;
        }

        Debug.LogWarning($"Stat '{statName}' not found on {characterName}.");
        return 0;
    }

    public void ModifyStat(string statName, float amount)
    {
        foreach (var stat in stats)
        {
            if (stat.definition != null && stat.definition.statName == statName)
            {
                stat.currentValue += amount;
                Debug.Log($"{characterName}'s {statName} changed by {amount}. New value: {stat.currentValue}");
                return;
            }
        }

        Debug.LogWarning($"Stat '{statName}' not found on {characterName}.");
    }

    public void SaveStats()
    {
        foreach (var stat in stats)
        {
            if (stat.definition != null)
            {
                string key = characterName + "_" + stat.definition.statName;
                PlayerPrefs.SetFloat(key, stat.currentValue);
            }
        }

        PlayerPrefs.Save();
        Debug.Log($"Saved stats for {characterName}");
    }

    public void LoadStats()
    {
        foreach (var stat in stats)
        {
            if (stat.definition != null)
            {
                string key = characterName + "_" + stat.definition.statName;
                if (PlayerPrefs.HasKey(key))
                {
                    stat.currentValue = PlayerPrefs.GetFloat(key);
                }
            }
        }

        Debug.Log($"Loaded stats for {characterName}");
    }
}
