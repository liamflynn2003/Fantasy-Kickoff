using TMPro;
using System.Collections;
using UnityEngine;
public class PlayerTooltipUI : MonoBehaviour
{
    public static PlayerTooltipUI Instance;
    public GameObject tooltipPanel;
    public TMP_Text tooltipText;

    private void Awake()
{
    Instance = this;
    tooltipPanel.SetActive(false);
}


public void ShowTooltip(PlayerListManager.PlayerData data, Vector3 _)
{
    if (data == null || data.skill == null)
    {
        Debug.LogWarning("Skipping tooltip display: PlayerData or skill is null.");
        return;
    }

    if (tooltipPanel == null || tooltipText == null)
    {
        Debug.LogWarning("Skipping tooltip: tooltipPanel or tooltipText is not assigned.");
        return;
    }

    tooltipPanel.SetActive(true);
    tooltipText.text = $"Passing: {data.skill.passing}, " +
                       $"Shooting: {data.skill.shooting}, " +
                       $"Tackling: {data.skill.tackling}, " +
                       $"Agility: {data.skill.agility}, " +
                       $"Strength: {data.skill.strength}, " +
                       $"Penalty: {data.skill.penaltyTaking}, " +
                       $"Jumping: {data.skill.jumping}, " +
                       $"Saving: {data.skill.saving}";
}



    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
    }
}
