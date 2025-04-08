using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Newtonsoft.Json;

public class PlayerSelectionManager : MonoBehaviour
{
    public GameObject[] teamOnePlayers;
    public GameObject[] teamTwoPlayers;
    private Dictionary<int, PlayerListManager.PlayerData> selectedTeamOne = new Dictionary<int, PlayerListManager.PlayerData>();
    private Dictionary<int, PlayerListManager.PlayerData> selectedTeamTwo = new Dictionary<int, PlayerListManager.PlayerData>();

    public void AssignPlayer(int positionIndex, PlayerListManager.PlayerData player, bool isTeamOne)
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

    private void UpdateUI(GameObject playerUI, PlayerListManager.PlayerData player)
    {
        TMP_Text nameText = playerUI.transform.Find("PlayerName").GetComponent<TMP_Text>();

        if (nameText) nameText.text = player.name;
    }

    public Dictionary<int, PlayerListManager.PlayerData> GetSelectedTeam(bool isTeamOne)
    {
        return isTeamOne ? selectedTeamOne : selectedTeamTwo;
    }

    public void LoadTeamsFromJson(string json)
    {
    try
    {
        var parsedData = JsonConvert.DeserializeObject<TeamJsonData>(json);

        if (parsedData?.team1?.players == null || parsedData?.team2?.players == null)
        {
            Debug.LogError("Invalid JSON structure: Missing team or player data.");
            return;
        }

        // Populate Team 1
        PopulateTeam(parsedData.team1.players, true);

        // Populate Team 2
        PopulateTeam(parsedData.team2.players, false);
    }
    catch (JsonException ex)
    {
        Debug.LogError("Failed to parse JSON: " + ex.Message);
    }
    catch (Exception ex)
    {
        Debug.LogError("Unexpected error while loading teams: " + ex.Message);
    }
    }

    private void PopulateTeam(List<PlayerJsonData> players, bool isTeamOne)
    {
        GameObject[] teamPlayers = isTeamOne ? teamOnePlayers : teamTwoPlayers;

        for (int i = 0; i < players.Count && i < teamPlayers.Length; i++)
        {
            PlayerJsonData playerJson = players[i];
            PlayerListManager.PlayerData playerData = new PlayerListManager.PlayerData
            {
                name = playerJson.name,
                position = playerJson.position,
                rating = int.Parse(playerJson.rating),
                skill = playerJson.skill,
                currentPOS = new Vector2(playerJson.currentPOS[0], playerJson.currentPOS[1]),
                fitness = playerJson.fitness,
                injured = playerJson.injured
            };

            AssignPlayer(i, playerData, isTeamOne);
        }
    }
    public class PlayerJsonData
{
    public string name;
    public string position;
    public string rating;
    public PlayerListManager.Skill skill;
    public List<float> currentPOS;
    public int fitness;
    public bool injured;
}
}

public class TeamJsonData
{
    public TeamData team1;
    public TeamData team2;
}

public class TeamData
{
    public string name;
    public int rating;
    public List<PlayerSelectionManager.PlayerJsonData> players;
}


