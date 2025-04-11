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
        Dictionary<int, PlayerListManager.PlayerData> teamOne = selectionManager.GetSelectedTeam(true);
        Dictionary<int, PlayerListManager.PlayerData> teamTwo = selectionManager.GetSelectedTeam(false);

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

    public MatchRequest(Dictionary<int, PlayerListManager.PlayerData> teamOneDict, Dictionary<int, PlayerListManager.PlayerData> teamTwoDict)
    {
        team1 = new TeamData
        {
            name = "TeamOne",
            rating = 90,
            players = ConvertToJsonData(new List<PlayerListManager.PlayerData>(teamOneDict.Values))
        };

        team2 = new TeamData
        {
            name = "TeamTwo",
            rating = 90,
            players = ConvertToJsonData(new List<PlayerListManager.PlayerData>(teamTwoDict.Values))
        };

        pitchDetails = new PitchDetails
        {
            pitchWidth = 680,
            pitchHeight = 1050,
            goalWidth = 90
        };
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
            currentPos = player.currentPos,
            fitness = 100,
            injured = false
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
