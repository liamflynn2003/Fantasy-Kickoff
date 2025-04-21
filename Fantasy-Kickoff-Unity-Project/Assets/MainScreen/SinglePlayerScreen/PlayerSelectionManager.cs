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
            player.position = CalculatePosition(positionIndex);
            player.startPOS= CalculateStartPos(positionIndex, isTeamOne);
            selectedTeamOne[positionIndex] = player;
            UpdateUI(teamOnePlayers[positionIndex], player);
        }
        else
        {
            player.position = CalculatePosition(positionIndex);
            player.startPOS=CalculateStartPos(positionIndex, isTeamOne);
            selectedTeamTwo[positionIndex] = player;
            UpdateUI(teamTwoPlayers[positionIndex], player);
        }
    }

    public string CalculatePosition(int positionIndex)
    {
        switch (positionIndex)
        {
            case 0: return "GK";
            case 1: return "RB";
            case 2: return "CB";
            case 3: return "CB";
            case 4: return "LB";
            case 5: return "RM";
            case 6: return "CM";
            case 7: return "LM";
            case 8: return "RW";
            case 9: return "ST";
            case 10: return "LW";
            default: return "Unknown";
        }
    }

    public Vector2 CalculateStartPos(int positionIndex, bool isTeamOne)
{
        switch (positionIndex)
        {
            case 0: return new Vector2(340, 0);   // GK
            case 1: return new Vector2(600, 80);  // RB
            case 2: return new Vector2(420, 80); // CB
            case 3: return new Vector2(230, 80); // CB
            case 4: return new Vector2(80, 80); // LB
            case 5: return new Vector2(510, 270); // RM
            case 6: return new Vector2(340, 270); // CM
            case 7: return new Vector2(170, 270); // LM
            case 8: return new Vector2(510, 500); // RW
            case 9: return new Vector2(340, 500); // ST
            case 10: return new Vector2(170, 500); // LW
            default: return new Vector2(340, 500);   // Default position
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
                rating = 50,
                skill = ConvertToSkill(playerJson.skill),
                startPOS = new Vector2(playerJson.startPOS[0], playerJson.startPOS[1]),
                fitness = playerJson.fitness,
                injured = playerJson.injured
            };

            AssignPlayer(i, playerData, isTeamOne);
        }
    }

    private PlayerListManager.Skill ConvertToSkill(Dictionary<string, string> skillDict)
{
    PlayerListManager.Skill skill = new PlayerListManager.Skill
    {
        passing = int.Parse(skillDict["passing"]),
        shooting = int.Parse(skillDict["shooting"]),
        tackling = int.Parse(skillDict["tackling"]),
        saving = int.Parse(skillDict["saving"]),
        agility = int.Parse(skillDict["agility"]),
        strength = int.Parse(skillDict["strength"]),
        penaltyTaking = int.Parse(skillDict["penalty_taking"]),
        jumping = int.Parse(skillDict["jumping"])
    };

    return skill;
}

    public class PlayerJsonData
{
    public string name;
    public string position;
    public string rating;
    public Dictionary<string, string> skill;
    
    public int[] startPOS;

    public int fitness;
    public bool injured;
    public int playerID;
    public string action;
    public bool offside;
    public bool hasBall;
}

[System.Serializable]
public class PlayerStats
{
    public int goals = 0;
    public ShotStats shots = new ShotStats();
    public CardStats cards = new CardStats();
    public PassStats passes = new PassStats();
    public TackleStats tackles = new TackleStats();
    public int saves = 0;
}

[System.Serializable]
public class ShotStats
{
    public int total = 0;
    public int on = 0;
    public int off = 0;
}

[System.Serializable]
public class CardStats
{
    public int yellow = 0;
    public int red = 0;
}

[System.Serializable]
public class PassStats
{
    public int total = 0;
    public int on = 0;
    public int off = 0;
}

[System.Serializable]
public class TackleStats
{
    public int total = 0;
    public int on = 0;
    public int off = 0;
    public int fouls = 0;
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


