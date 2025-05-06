using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ConfirmTeamButton : MonoBehaviour
{
    public PlayerSelectionManager playerSelectionManager;
    public Button confirmButton;
    public TextMeshProUGUI warningText;

    void Update()
    {
        if (playerSelectionManager == null || confirmButton == null || warningText == null)
            return;

        // Get the correct team side based on multiplayer flag
        bool isTeamOne = PlayerSessionInfo.multiplayerIsTeamOne;

        string warning = GetTeamValidationWarning(isTeamOne);

        if (string.IsNullOrEmpty(warning))
        {
            confirmButton.interactable = true;
            warningText.text = "";
            warningText.transform.parent.gameObject.SetActive(false);
        }
        else
        {
            confirmButton.interactable = false;
            warningText.text = warning;
            warningText.transform.parent.gameObject.SetActive(true);
        }
    }

    private string GetTeamValidationWarning(bool isTeamOne)
    {
        Dictionary<int, PlayerListManager.PlayerData> team = playerSelectionManager.GetSelectedTeam(isTeamOne);

        string warnings = "";

        if (team.Count != 11)
            warnings += "Your team must have exactly 11 players.\n";
        if (HasDuplicates(team))
            warnings += "Duplicate player detected in your team.\n";

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
