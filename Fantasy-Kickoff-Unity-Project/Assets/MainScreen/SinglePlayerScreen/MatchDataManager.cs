using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using Newtonsoft.Json;

public class MatchDataManager : MonoBehaviour
{
    public PlayerSelectionManager selectionManager;
    private string serverUrl = "http://13.219.10.67:3000/simulate";

    public void SimulateMatch()
    {
        Dictionary<int, PlayerListManager.PlayerData> teamOne = selectionManager.GetSelectedTeam(true);
        Dictionary<int, PlayerListManager.PlayerData> teamTwo = selectionManager.GetSelectedTeam(false);

        if (teamOne == null || teamTwo == null)
        {
            Debug.LogError("One or both teams are null. Cannot simulate match.");
            return;
        }

        MatchRequest matchRequest = new MatchRequest(teamOne, teamTwo);
        Debug.Log(matchRequest.ToString());
        string jsonData = JsonConvert.SerializeObject(matchRequest, Formatting.Indented);
        Debug.Log($"Serialized JSON: {jsonData}");
        Debug.Log($"Final JSON being sent: {jsonData}");

        WriteJsonToFile(jsonData);
        if (string.IsNullOrEmpty(jsonData))
        {
            Debug.LogError("Failed to serialize MatchRequest to JSON.");
            return;
        }

        StartCoroutine(PostMatchData(jsonData));

    }

private void WriteJsonToFile(string jsonData)
{
    string path = Application.persistentDataPath + "/matchRequest.json";
    System.IO.File.WriteAllText(path, jsonData);
    Debug.Log($"JSON written to file: {path}");
}
    private IEnumerator PostMatchData(string json)
    {
        Debug.Log("Sending JSON data: " + json);
        UnityWebRequest request = new UnityWebRequest(serverUrl, "POST");
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Match simulation started successfully: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Failed to start match simulation: " + request.error);
        }
    }
}

[System.Serializable]
public class MatchRequest
{
    public MatchDetails matchDetails;

    public MatchRequest(Dictionary<int, PlayerListManager.PlayerData> teamOneDict, Dictionary<int, PlayerListManager.PlayerData> teamTwoDict)
    {
        matchDetails = new MatchDetails
        {
            matchID = GenerateMatchID(),
            kickOffTeam = new TeamData
            {
                name = "TeamOne",
                rating = 88,
                players = ConvertToJsonData(new List<PlayerListManager.PlayerData>(teamOneDict.Values))
            },
            opponentTeam = new TeamData
            {
                name = "TeamTwo",
                rating = 90,
                players = ConvertToJsonData(new List<PlayerListManager.PlayerData>(teamTwoDict.Values))
            }
        };
    }

    private static long GenerateMatchID()
    {
        return DateTime.Now.Ticks; // Generate a unique match ID based on the current timestamp
    }

    
    private static List<PlayerSelectionManager.PlayerJsonData> ConvertToJsonData(List<PlayerListManager.PlayerData> playerDataList)
{
    List<PlayerSelectionManager.PlayerJsonData> jsonList = new List<PlayerSelectionManager.PlayerJsonData>();

    foreach (var player in playerDataList)
    {
        Debug.Log($"Converting player: {player.player.firstname} {player.player.lastname}, Position: {player.position}");
        jsonList.Add(new PlayerSelectionManager.PlayerJsonData
        {
            name = $"{player.player.firstname} {player.player.lastname}",
            position = player.position,
            rating = CalculateAverageSkill(player.skill).ToString(),
            skill = new Dictionary<string, string>
            {
                { "passing", player.skill.passing.ToString() },
                { "shooting", player.skill.shooting.ToString() },
                { "tackling", player.skill.tackling.ToString() },
                { "saving", player.skill.saving.ToString() },
                { "agility", player.skill.agility.ToString() },
                { "strength", player.skill.strength.ToString() },
                { "penalty_taking", player.skill.penaltyTaking.ToString() },
                { "jumping", player.skill.jumping.ToString() }
            },
            currentPOS = new int[] { Mathf.RoundToInt(player.currentPOS.x), Mathf.RoundToInt(player.currentPOS.y) },
            originPOS = new int[] { Mathf.RoundToInt(player.currentPOS.x), Mathf.RoundToInt(player.currentPOS.y) },
            intentPOS = new int[] { Mathf.RoundToInt(player.currentPOS.x), Mathf.RoundToInt(player.currentPOS.y) },
            fitness = 99,
            injured = false,
            playerID = player.id,
            action = "none",
            offside = false,
            hasBall = false,
            stats = new PlayerSelectionManager.PlayerStats()
        });
    }

    return jsonList;
}

    private static int CalculateAverageSkill(PlayerListManager.Skill skill)
    {
        int total = skill.passing +
                    skill.shooting +
                    skill.tackling +
                    skill.saving +
                    skill.agility +
                    skill.strength +
                    skill.penaltyTaking +
                    skill.jumping;

        return Mathf.RoundToInt((float)total / 8);
    }
}

[System.Serializable]
public class MatchDetails
{
    public long matchID;
    public TeamData kickOffTeam;
    public TeamData opponentTeam;
}

[System.Serializable]
public class PlayerJsonData
{
    public string name;
    public string position;
    public string rating;
    public Dictionary<string, string> skill;
    public int[] currentPOS;
    public float fitness;
    public bool injured;
    public long playerID;
    public int[] originPOS;
    public int[] intentPOS;
    public string action;
    public bool offside;
    public bool hasBall;
    public PlayerStats stats;
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

[System.Serializable]
public class PitchDetails
{
    public int pitchWidth;
    public int pitchHeight;
    public int goalWidth;
}
