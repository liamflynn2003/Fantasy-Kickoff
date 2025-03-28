using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class MatchDataManager : MonoBehaviour
{
    public PlayerSelectionManager selectionManager;
    private string apiUrl = "https://your-api-endpoint.com/simulate";

    public void SimulateMatch()
    {
        Dictionary<int, PlayerData> teamOne = selectionManager.GetSelectedTeam(true);
        Dictionary<int, PlayerData> teamTwo = selectionManager.GetSelectedTeam(false);

        MatchRequest matchRequest = new MatchRequest(teamOne, teamTwo);
        string jsonData = JsonUtility.ToJson(matchRequest);

        StartCoroutine(PostMatchData(jsonData));
    }

    private IEnumerator PostMatchData(string json)
    {
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
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
