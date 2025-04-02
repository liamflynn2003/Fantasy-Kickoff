using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Newtonsoft.Json;

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
        if (jerseyImage) jerseyImage.sprite = player.jerseySprite; // Ensure jerseySprite is set correctly
    }

    public Dictionary<int, PlayerData> GetSelectedTeam(bool isTeamOne)
    {
        return isTeamOne ? selectedTeamOne : selectedTeamTwo;
    }

    // New Method: Load teams from JSON
    public void LoadTeamsFromJson(string json)
    {
        try
        {
            var parsedData = JsonConvert.DeserializeObject<TeamJsonData>(json);

            // Populate Team 1
            PopulateTeam(parsedData.team1.players, true);

            // Populate Team 2
            PopulateTeam(parsedData.team2.players, false);
        }
        catch (JsonException ex)
        {
            Debug.LogError("Failed to parse JSON: " + ex.Message);
        }
    }

    private void PopulateTeam(List<PlayerJsonData> players, bool isTeamOne)
    {
        GameObject[] teamPlayers = isTeamOne ? teamOnePlayers : teamTwoPlayers;

        for (int i = 0; i < players.Count && i < teamPlayers.Length; i++)
        {
            PlayerJsonData playerJson = players[i];
            PlayerData playerData = new PlayerData
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
}

// Supporting Classes for JSON Parsing
[System.Serializable]
public class TeamJsonData
{
    public TeamData team1;
    public TeamData team2;
}

[System.Serializable]
public class TeamData
{
    public string name;
    public int rating;
    public List<PlayerJsonData> players;
}

[System.Serializable]
public class PlayerJsonData
{
    public string name;
    public string position;
    public string rating;
    public Skill skill;
    public List<float> currentPOS;
    public int fitness;
    public bool injured;
}

[System.Serializable]
public class PlayerData
{
    public string name;
    public string position;
    public int rating;
    public Skill skill;
    public Vector2 currentPOS;
    public int fitness;
    public bool injured;
    public Sprite jerseySprite;
}

[System.Serializable]
public class Skill
{
    public int passing;
    public int shooting;
    public int tackling;
    public int saving;
    public int agility;
    public int strength;
    public int penalty_taking;
    public int jumping;
}