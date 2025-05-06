using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PlayMatchButtonController : MonoBehaviour
{
    public PlayerSelectionManager playerSelectionManager;
    public Button playMatchButton;
    public TextMeshProUGUI warningText;

void Update()
{
    if (playerSelectionManager == null || playMatchButton == null || warningText == null)
        return;

    string warnings = GetValidationWarnings();

    if (string.IsNullOrEmpty(warnings))
    {
        playMatchButton.interactable = true;
        warningText.text = "";
        warningText.transform.parent.gameObject.SetActive(false);
    }
    else
    {
        playMatchButton.interactable = false;
        warningText.text = warnings;
        warningText.transform.parent.gameObject.SetActive(true);
    }
}


private string GetValidationWarnings()
{
    string warnings = "";

    Dictionary<int, PlayerListManager.PlayerData> teamOne = playerSelectionManager.GetSelectedTeam(true);
    Dictionary<int, PlayerListManager.PlayerData> teamTwo = playerSelectionManager.GetSelectedTeam(false);

    if (teamOne.Count != 11)
        warnings += "Team One is missing players.\n";
    if (HasDuplicates(teamOne))
        warnings += "Duplicate player detected in Team One.\n";

    if (teamTwo.Count != 11)
        warnings += "Team Two is missing players.\n";
    if (HasDuplicates(teamTwo))
        warnings += "Duplicate player detected in Team Two.\n";

    return warnings.Trim();
}

    private bool HasDuplicates(Dictionary<int, PlayerListManager.PlayerData> team)
{
    HashSet<string> names = new HashSet<string>();

    foreach (var player in team.Values)
    {
        if (string.IsNullOrEmpty(player.name))
        {
            if (player.player != null)
            {
                player.name = player.player.firstname + " " + player.player.lastname;
            }
            else
            {
                Debug.LogWarning("Player missing player data, cannot build name!");
                continue;
            }
        }

        if (!names.Add(player.name))
            return true;
    }
    return false;
}

}
