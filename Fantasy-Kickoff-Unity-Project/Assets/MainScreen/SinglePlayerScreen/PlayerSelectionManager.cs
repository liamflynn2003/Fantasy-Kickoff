using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerSelectionManager : MonoBehaviour
{
    public GameObject[] teamOnePlayers;
    public GameObject[] teamTwoPlayers;
    private Dictionary<int, PlayerData> selectedTeamOne = new Dictionary<int, PlayerData>();
    private Dictionary<int, PlayerData> selectedTeamTwo = new Dictionary<int, PlayerData>();

    public void AssignPlayer(int positionIndex, PlayerData player, bool isTeamOne)
    {
        if (isTeamOne)
        {
            selectedTeamOne[positionIndex] = player;
            UpdateUI(teamOnePlayers[positionIndex], player);
        }
        else
        {
            selectedTeamTwo[positionIndex] = player;
            UpdateUI(teamTwoPlayers[positionIndex], player);
        }
    }

    private void UpdateUI(GameObject playerUI, PlayerData player)
    {
        TMP_Text nameText = playerUI.transform.Find("PlayerName").GetComponent<TMP_Text>();
        Image jerseyImage = playerUI.transform.Find("JerseyImage").GetComponent<Image>();

        if (nameText) nameText.text = player.name;
        if (jerseyImage) jerseyImage.sprite = player.jerseySprite;
    }

    public Dictionary<int, PlayerData> GetSelectedTeam(bool isTeamOne)
    {
        return isTeamOne ? selectedTeamOne : selectedTeamTwo;
    }
}
