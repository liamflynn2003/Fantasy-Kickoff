using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;

public class PlayerListManager : MonoBehaviour
{
    public TMP_Dropdown teamDropdown;
    public Transform contentPanel;
    public Transform contentParent;
    public GameObject playerListPrefab;
    public GameObject loadingPanel;

    private string apiKey = "3496853baf021377f539dce314b05abe";
    private string baseUrl = "https://v3.football.api-sports.io/players?team={0}&season=2024&page={1}";
    private Dictionary<int, List<PlayerData>> playerCache = new Dictionary<int, List<PlayerData>>();

   private void Start()
{
    if (teamDropdown != null)
    {
        teamDropdown.onValueChanged.AddListener(delegate { OnTeamSelected(); });

        teamDropdown.onValueChanged.AddListener(delegate { SetInitialTeamDropdownValue(); });
    }
}

private void SetInitialTeamDropdownValue()
{
    if (teamDropdown.options.Count > 1)
    {
        teamDropdown.value = 1;
        OnTeamSelected();
    }
}


    private void OnTeamSelected()
    {
        int teamId = FindObjectOfType<TeamDropdown>().GetSelectedTeamId();
        if (teamId > 0)
        {
            if (playerCache.ContainsKey(teamId))
            {
                // Use cached data if available
                PopulateScrollView(playerCache[teamId]);
            }
            else
            {
                StartCoroutine(FetchAllPlayers(teamId));
            }
        }
    }

    private IEnumerator FetchAllPlayers(int teamId)
{
    // Show the loading UI
    loadingPanel.SetActive(true);

    int currentPage = 1;
    int totalPages = 1;
    List<PlayerData> allPlayers = new List<PlayerData>();

    while (currentPage <= totalPages)
    {
        string apiUrl = string.Format(baseUrl, teamId, currentPage);

        UnityWebRequest request = UnityWebRequest.Get(apiUrl);
        request.SetRequestHeader("x-apisports-key", apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || 
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            yield break; // No debug logs for errors
        }
        else
        {
            string jsonResponse = request.downloadHandler.text;
            PlayerApiResponse response = JsonConvert.DeserializeObject<PlayerApiResponse>(jsonResponse);

            if (response != null && response.response != null)
            {
                foreach (var playerData in response.response)
                {
                    if (playerData.statistics != null && playerData.statistics.Count > 0)
                    {
                        var gameStats = playerData.statistics[0].games;
                        int playerAppearances = gameStats.appearences ?? 0;

                        if (playerAppearances > 0)
                        {
                            allPlayers.Add(playerData);
                        }
                    }
                }

                totalPages = response.paging.total;
                currentPage++;
            }
            else
            {
                break; // No need for debug logs here
            }
        }
    }

    if (allPlayers.Count > 0)
    {
        // Cache the result
        playerCache[teamId] = allPlayers;
    }

    PopulateScrollView(allPlayers);

    // Hide the loading UI once the data is loaded
    loadingPanel.SetActive(false);
}


    public void PopulateScrollView(List<PlayerData> players)
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        float yOffset = -10f;
        float itemHeight = 20f;
        float spacing = 0f;

        foreach (var playerData in players)
        {
            string firstName = playerData.player.firstname.Split(' ')[0];
            string lastName = playerData.player.lastname.Split(' ')[0];
            string fullPlayerName = $"{firstName} {lastName}";

            GameObject playerListObject = Instantiate(playerListPrefab, contentParent);
            TMP_Text nameText = playerListObject.transform.Find("PlayerName").GetComponent<TMP_Text>();
            nameText.text = fullPlayerName;

            Image playerImage = playerListObject.transform.Find("PlayerImage").GetComponent<Image>();
            if (playerImage != null && !string.IsNullOrEmpty(playerData.player.photo))
            {
                StartCoroutine(LoadPlayerImage(playerData.player.photo, playerImage));
            }

            RectTransform rectTransform = playerListObject.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(93, yOffset);

            yOffset -= (itemHeight + spacing);
        }

        RectTransform contentRect = contentParent.GetComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, Mathf.Abs(yOffset) + 20);
        contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, 0);
    }

    private IEnumerator LoadPlayerImage(string url, Image playerImage)
    {
        // Check if the image is already cached
        if (PlayerImageCache.Instance.HasImage(url))
        {
            playerImage.sprite = PlayerImageCache.Instance.GetImage(url);
            yield break;
        }

        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            playerImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            PlayerImageCache.Instance.CacheImage(url, playerImage.sprite); // Cache the image for future use
        }
        else
        {
            // Fallback image logic
            string placeholderUrl = "https://media.api-sports.io/football/players/328089.png";
            UnityWebRequest placeholderRequest = UnityWebRequestTexture.GetTexture(placeholderUrl);
            yield return placeholderRequest.SendWebRequest();

            if (placeholderRequest.result == UnityWebRequest.Result.Success)
            {
                Texture2D placeholderTexture = ((DownloadHandlerTexture)placeholderRequest.downloadHandler).texture;
                playerImage.sprite = Sprite.Create(placeholderTexture, new Rect(0, 0, placeholderTexture.width, placeholderTexture.height), new Vector2(0.5f, 0.5f));
            }
        }
    }
}

public class PlayerImageCache
{
    private static PlayerImageCache _instance;
    private Dictionary<string, Sprite> _imageCache = new Dictionary<string, Sprite>();

    public static PlayerImageCache Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new PlayerImageCache();
            }
            return _instance;
        }
    }

    public void CacheImage(string url, Sprite sprite)
    {
        if (!_imageCache.ContainsKey(url))
        {
            _imageCache[url] = sprite;
        }
    }

    public bool HasImage(string url)
    {
        return _imageCache.ContainsKey(url);
    }

    public Sprite GetImage(string url)
    {
        return _imageCache.ContainsKey(url) ? _imageCache[url] : null;
    }
}


[System.Serializable]
public class PlayerApiResponse
{
    public List<PlayerData> response;
    public PagingInfo paging;
}

[System.Serializable]
public class PlayerData
{
    public PlayerInfo player;
    public List<PlayerStatistics> statistics;
}

[System.Serializable]
public class PlayerInfo
{
    public int id;
    public string name;
    public string firstname;
    public string lastname;
    public int age;
    public BirthInfo birth;
    public string height;
    public bool injured;
    public string photo;
}

[System.Serializable]
public class BirthInfo
{
    public string date;
    public string place;
    public string country;
}

[System.Serializable]
public class PagingInfo
{
    public int current;
    public int total;
}

[System.Serializable]
public class PlayerStatistics
{
    public GameStatistics games;
}

[System.Serializable]
public class GameStatistics
{
    public int? appearences;
}

