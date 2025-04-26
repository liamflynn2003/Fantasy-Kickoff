using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using Newtonsoft.Json;
using System.IO;

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

    // Use Newtonsoft.Json for serialization
    string jsonData = JsonConvert.SerializeObject(matchRequest, Formatting.Indented);

    if (string.IsNullOrEmpty(jsonData))
    {
        Debug.LogError("Failed to serialize MatchRequest to JSON.");
        return;
    }

    StartCoroutine(PostMatchData(jsonData));
}
private IEnumerator PostMatchData(string json)
{
    int maxRetries = 5;
    int retryCount = 0;
    bool success = false;

    while (retryCount < maxRetries && !success)
    {
        Debug.Log($"Sending attempt {retryCount + 1}");

        UnityWebRequest request = new UnityWebRequest(serverUrl, "POST");
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        request.timeout = 30;

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            success = true;
            Debug.Log("Match simulation started successfully.");
            string responseText = request.downloadHandler.text;

            string filePath = Path.Combine(Application.persistentDataPath, "MatchSimulationResult.json");

            // Delete old match json file if it exists
            if (File.Exists(filePath))
            {
                Debug.Log("Old match simulation result found. Deleting...");
                File.Delete(filePath);
            }

            // Save new simulation result
            File.WriteAllText(filePath, responseText);
            Debug.Log($"Match simulation result saved to: {filePath}");

            // Load SimScreen scene now
            UnityEngine.SceneManagement.SceneManager.LoadScene("SimScreen");
        }
        else
        {
            Debug.LogError($"Failed to start match simulation: {request.error}. Retrying...");
            retryCount++;

            yield return new WaitForSeconds(1f);
        }
    }

    if (!success)
    {
        Debug.LogError("All retry attempts failed. Could not start match simulation.");
    }
}



}

[System.Serializable]
public class MatchRequest
{
    public TeamData team1;
    public TeamData team2;
    public PitchDetails pitchDetails;

    public MatchRequest(Dictionary<int, PlayerListManager.PlayerData> teamOneDict, Dictionary<int, PlayerListManager.PlayerData> teamTwoDict)
    {
        team1 = new TeamData
        {
            name = "TeamOne",
            rating = CalculateTeamRating(teamOneDict.Values),
            players = ConvertToJsonData(new List<PlayerListManager.PlayerData>(teamOneDict.Values))
        };
        team2 = new TeamData
        {
            name = "TeamTwo",
            rating = CalculateTeamRating(teamTwoDict.Values),
            players = ConvertToJsonData(new List<PlayerListManager.PlayerData>(teamTwoDict.Values))
        };
        pitchDetails = new PitchDetails
        {
            pitchWidth = 500,
            pitchHeight = 620,
            goalWidth = 100
        };
    }

    private static int CalculateTeamRating(IEnumerable<PlayerListManager.PlayerData> players)
    {
        int totalRating = 0;
        int playerCount = 0;

        foreach (var player in players)
        {
            totalRating += CalculateAverageSkill(player.skill);
            playerCount++;
        }

        return playerCount > 0 ? Mathf.RoundToInt((float)totalRating / playerCount) : 0;
    }

    private static List<PlayerSelectionManager.PlayerJsonData> ConvertToJsonData(List<PlayerListManager.PlayerData> playerDataList)
    {
        List<PlayerSelectionManager.PlayerJsonData> jsonList = new List<PlayerSelectionManager.PlayerJsonData>();

        foreach (var player in playerDataList)
        {
            var skillDict = new Dictionary<string, string>
            {
                { "passing", player.skill.passing.ToString() },
                { "shooting", player.skill.shooting.ToString() },
                { "tackling", player.skill.tackling.ToString() },
                { "saving", player.skill.saving.ToString() },
                { "agility", player.skill.agility.ToString() },
                { "strength", player.skill.strength.ToString() },
                { "penalty_taking", player.skill.penaltyTaking.ToString() },
                { "jumping", player.skill.jumping.ToString() }
            };

            jsonList.Add(new PlayerSelectionManager.PlayerJsonData
            {
                name = $"{player.player.firstname} {player.player.lastname}",
                position = player.position,
                rating = CalculateAverageSkill(player.skill).ToString(),
                skill = skillDict,
                currentPOS = new int[] { Mathf.RoundToInt(player.currentPOS.x), Mathf.RoundToInt(player.currentPOS.y) },
                fitness = 99,
                injured = false
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
