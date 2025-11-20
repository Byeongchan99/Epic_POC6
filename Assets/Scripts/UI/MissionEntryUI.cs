using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach this to Panel Mission Entry prefab
/// Allows direct assignment of checkbox images in Inspector
/// </summary>
public class MissionEntryUI : MonoBehaviour
{
    [Header("Mission UI Elements")]
    [SerializeField] private GameObject checkboxUnchecked;
    [SerializeField] private GameObject checkboxChecked;
    [SerializeField] private TextMeshProUGUI missionNameText;

    public void SetMissionName(string name)
    {
        if (missionNameText != null)
        {
            missionNameText.text = name;
        }
    }

    public void SetCheckboxState(bool isCompleted)
    {
        if (checkboxUnchecked != null)
        {
            checkboxUnchecked.SetActive(!isCompleted);
        }

        if (checkboxChecked != null)
        {
            checkboxChecked.SetActive(isCompleted);
        }
    }

    public bool HasValidCheckboxes()
    {
        return checkboxUnchecked != null && checkboxChecked != null;
    }
}
