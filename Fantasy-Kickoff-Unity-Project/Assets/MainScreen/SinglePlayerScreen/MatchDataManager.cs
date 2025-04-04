using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class MatchDataManager : MonoBehaviour
{
    public PlayerSelectionManager selectionManager;
    private string serverUrl = "http://13.219.10.67:3000/simulate";

    public void SimulateMatch()
    {
        Dictionary<int, PlayerData> teamOne = selectionManager.GetSelectedTeam(true);
        Dictionary<int, PlayerData> teamTwo = selectionManager.GetSelectedTeam(false);

        if (teamOne == null || teamTwo == null)
        {
            Debug.LogError("One or both teams are null. Cannot simulate match.");
            return;
        }

        MatchRequest matchRequest = new MatchRequest(teamOne, teamTwo);

        string jsonData = JsonUtility.ToJson(matchRequest);

        if (string.IsNullOrEmpty(jsonData))
        {
            Debug.LogError("Failed to serialize MatchRequest to JSON.");
            return;
        }

        StartCoroutine(PostMatchData(jsonData));
    }

    private IEnumerator PostMatchData(string json)
    {
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
    public TeamData team1;
    public TeamData team2;
    public PitchDetails pitchDetails;

    public MatchRequest(Dictionary<int, PlayerData> teamOneDict, Dictionary<int, PlayerData> teamTwoDict)
    {
        team1 = new TeamData
        {
            name = "TeamOne",
            rating = 90,
            players = ConvertToJsonData(new List<PlayerData>(teamOneDict.Values))
        };

        team2 = new TeamData
        {
            name = "TeamTwo",
            rating = 90,
            players = ConvertToJsonData(new List<PlayerData>(teamTwoDict.Values))
        };

        pitchDetails = new PitchDetails
        {
            pitchWidth = 680,
            pitchHeight = 1050,
            goalWidth = 90
        };
    }

    private static List<PlayerJsonData> ConvertToJsonData(List<PlayerData> playerDataList)
    {
        List<PlayerJsonData> jsonList = new List<PlayerJsonData>();

        foreach (var player in playerDataList)
        {
            jsonList.Add(new PlayerJsonData
            {
                name = player.name,
                position = player.position,
                rating = player.rating.ToString(),
                skill = player.skill,
                currentPOS = new List<float> { player.currentPOS.x, player.currentPOS.y },
                fitness = player.fitness,
                injured = player.injured
            });
        }

        return jsonList;
    }
}

[System.Serializable]
public class PitchDetails
{
    public int pitchWidth;
    public int pitchHeight;
    public int goalWidth;
}
