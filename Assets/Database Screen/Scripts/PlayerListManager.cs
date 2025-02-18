using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Newtonsoft.Json;

public class PlayerListManager : MonoBehaviour
{
    public TMP_Dropdown teamDropdown;
    public Transform contentPanel;
    public Transform contentParent;
    public GameObject playerNamePrefab;

    private string apiKey = "3496853baf021377f539dce314b05abe";
    private string baseUrl = "https://v3.football.api-sports.io/players?team={0}&season=2023";

    private void Start()
    {
        if (teamDropdown != null)
        {
            teamDropdown.onValueChanged.AddListener(delegate { OnTeamSelected(); });
        }
    }

    private void OnTeamSelected()
    {
        int teamId = FindObjectOfType<TeamDropdown>().GetSelectedTeamId();
        if (teamId > 0)
        {
            StartCoroutine(FetchPlayers(teamId));
        }
    }

    private IEnumerator FetchPlayers(int teamId)
    {
        string apiUrl = string.Format(baseUrl, teamId);
        UnityWebRequest request = UnityWebRequest.Get(apiUrl);
        request.SetRequestHeader("x-apisports-key", apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("API Request Error: " + request.error);
        }
        else
        {
            string jsonResponse = request.downloadHandler.text;
            PlayerApiResponse response = JsonConvert.DeserializeObject<PlayerApiResponse>(jsonResponse);

            PopulateScrollView(response);
        }
    }
public void PopulateScrollView(PlayerApiResponse response)
{
    // Clear previous content
    foreach (Transform child in contentParent)
    {
        Destroy(child.gameObject);  // Remove any previous player entries
    }

    float yOffset = -10f;  // Start the first item at y = -10

    foreach (var playerData in response.response)
    {
        // Instantiate a new player name prefab
        GameObject playerNameObject = Instantiate(playerNamePrefab, contentParent);

        // Set the player's name (or any other data)
        playerNameObject.GetComponentInChildren<TMP_Text>().text = playerData.player.name;

        // Adjust the position to be below the last one
        RectTransform rectTransform = playerNameObject.GetComponent<RectTransform>();

        // Set the X position to 93 and Y position according to yOffset
        rectTransform.anchoredPosition = new Vector2(93, yOffset);

        // Update yOffset for the next player
        yOffset -= rectTransform.rect.height;  // Move downward for the next item
    }
}



}

// JSON Classes
[System.Serializable]
public class PlayerApiResponse
{
    public List<PlayerData> response;
}

[System.Serializable]
public class PlayerData
{
    public PlayerInfo player;
}

[System.Serializable]
public class PlayerInfo
{
    public string name;
}
