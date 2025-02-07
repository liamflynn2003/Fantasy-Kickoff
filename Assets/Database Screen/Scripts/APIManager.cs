using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro; // For TextMeshPro
using UnityEngine.UI;

[System.Serializable]
public class PlayerData
{
    public string name;
    public string position;
    public string team;
}

[System.Serializable]
public class PlayerList
{
    public List<PlayerData> players;
}

public class APIManager : MonoBehaviour
{
    public string apiUrl = "https://your-api-url.com/players"; // Replace with your API URL
    public GameObject playerPrefab; // Prefab for each list item
    public Transform contentParent; // Scroll View Content (where items will be added)

    void Start()
    {
        StartCoroutine(FetchPlayers());
    }

    IEnumerator FetchPlayers()
    {
        UnityWebRequest request = UnityWebRequest.Get(apiUrl);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            PlayerList playerList = JsonUtility.FromJson<PlayerList>("{\"players\":" + json + "}"); // Wrap JSON into array format if needed
            PopulateList(playerList.players);
        }
        else
        {
            Debug.LogError("API Error: " + request.error);
        }
    }

    void PopulateList(List<PlayerData> players)
    {
        foreach (PlayerData player in players)
        {
            GameObject newItem = Instantiate(playerPrefab, contentParent);
            newItem.transform.Find("PlayerName").GetComponent<TextMeshProUGUI>().text = player.name;
            newItem.transform.Find("PlayerPosition").GetComponent<TextMeshProUGUI>().text = player.position;
            newItem.transform.Find("PlayerTeam").GetComponent<TextMeshProUGUI>().text = player.team;
        }
    }
}
