using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using System;

public class PlayerListManager : MonoBehaviour
{
    // ============================
    // Fields and Variables
    // ============================

    // UI Elements
    public TMP_Dropdown teamDropdown;
    public TMP_Dropdown leagueDropdown;
    public Transform contentParent;
    public GameObject playerListPrefab;
    public GameObject loadingPanel;

    // API Configuration
    private string apiKey = Environment.GetEnvironmentVariable("FOOTBALL_API_KEY");
    private string baseUrl = "https://v3.football.api-sports.io/players?team={0}&season=2024&page={1}";

    // Data Caching
    private Dictionary<int, List<PlayerData>> playerCache = new Dictionary<int, List<PlayerData>>();
    private LocalDataManager localDataManager;

    // ============================
    // Nested Classes
    // ============================

    public class PlayerApiResponse
    {
        public List<PlayerData> response;
        public PagingInfo paging;
    }

    public class PlayerData
    {
        public PlayerJsonObject player;
        public Skill skill;
        public List<PlayerStatistics> statistics;

        public string name;
        public string position;
        public int rating;

        [JsonIgnore]
        public Vector2 currentPos;
        public int fitness;
        public bool injured;

        public void CalculateSkillsFromJson(string json)
        {
            try
            {
                PlayerJsonObject playerData = JsonConvert.DeserializeObject<PlayerJsonObject>(json);
                player = playerData;

                if (statistics != null && statistics.Count > 0)
                {
                    var stats = statistics[0];
skill = new Skill
{
    passing = Mathf.Clamp((stats.passes?.total ?? 0) + (stats.passes?.key ?? 0) * 2, 0, 100),
    shooting = Mathf.Clamp((stats.goals?.total ?? 0) * 10 + (stats.shots?.on ?? 0) * 2, 0, 100),
    tackling = Mathf.Clamp((stats.tackles?.total ?? 0) * 2 + (stats.duels?.won ?? 0), 0, 100),
    saving = Mathf.Clamp((stats.goals?.saves ?? 0) * 10, 0, 100),
    agility = Mathf.Clamp((stats.dribbles?.success ?? 0) * 2 + (stats.fouls?.drawn ?? 0), 0, 100),
    strength = Mathf.Clamp((stats.duels?.total ?? 0) + (stats.fouls?.committed ?? 0), 0, 100),
    penaltyTaking = Mathf.Clamp((stats.penalty?.scored ?? 0) * 10 - (stats.penalty?.missed ?? 0) * 5, 0, 100),
    jumping = Mathf.Clamp(Convert.ToInt32((player.height?.Replace(" cm", "") ?? "0")) / 2, 0, 100)
};


                }
                else
                {
                    Debug.LogWarning("No statistics available for this player.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error calculating skills from JSON: {ex.Message}");
            }
        }
    }

    public class PlayerJsonObject
    {
        public int id;
        public string firstname;
        public string lastname;
        public string photo;
        public string height;
        public string weight;
    }

    public class Skill
    {
        public int passing;
        public int shooting;
        public int tackling;
        public int saving;
        public int agility;
        public int strength;
        public int penaltyTaking;
        public int jumping;
    }

    public class PlayerStatistics
    {
        public GameStatistics games;
        public PassStatistics passes;
        public ShotStatistics shots;
        public GoalStatistics goals;
        public TackleStatistics tackles;
        public DuelStatistics duels;
        public DribbleStatistics dribbles;
        public FoulStatistics fouls;
        public PenaltyStatistics penalty;
    }

    public class GameStatistics { public int? appearences; }
    public class PassStatistics { public int? total; public int? key; }
    public class ShotStatistics { public int? total; public int? on; }
    public class GoalStatistics { public int? total; public int? saves; }
    public class TackleStatistics { public int? total; }
    public class DuelStatistics { public int? total; public int? won; }
    public class DribbleStatistics { public int? success; }
    public class FoulStatistics { public int? drawn; public int? committed; }
    public class PenaltyStatistics { public int? scored; public int? missed; }

    public class PagingInfo
    {
        public int current;
        public int total;
    }

    // ============================
    // Unity Lifecycle Methods
    // ============================

    private void Start()
    {
        localDataManager = GetComponent<LocalDataManager>();
        playerCache = localDataManager.LoadCache();

        if (teamDropdown != null)
        {
            leagueDropdown.onValueChanged.AddListener(delegate { OnLeagueChanged(); });
            teamDropdown.onValueChanged.AddListener(delegate { OnTeamSelected(); });

            SetInitialTeamDropdownValue();
        }

        OnLeagueChanged(); // Trigger initial league change
    }

    private void OnApplicationQuit()
    {
        localDataManager.SaveCache(playerCache);
    }

    // ============================
    // Dropdown Event Handlers
    // ============================

    private void SetInitialTeamDropdownValue()
    {
        if (teamDropdown.options.Count > 0)
        {
            Debug.Log("Setting team dropdown to default index (0).");
            teamDropdown.value = 0; // Set to the first option
        }
        else
        {
            Debug.LogWarning("Dropdown does not have enough options.");
        }
    }

    private void OnLeagueChanged()
    {
        Debug.Log("League dropdown changed. Current league index: " + leagueDropdown.value);
    }

    public void OnTeamSelected()
    {
        int teamId = FindObjectOfType<TeamDropdown>().GetSelectedTeamId();
        if (teamId > 0)
        {
            if (playerCache.ContainsKey(teamId))
            {
                Debug.Log("Using cached data for team ID: " + teamId);
                PopulateScrollView(playerCache[teamId]); // Display cached data
            }
            else
            {
                Debug.Log("Fetching data from API for team ID: " + teamId);
                StartCoroutine(FetchAllPlayers(teamId)); // Fetch fresh data from the API
            }
        }
    }

    // ============================
    // API Handling
    // ============================

    private IEnumerator FetchAllPlayers(int teamId)
    {
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
                Debug.LogError($"Error fetching player data: {request.error}. Retrying...");
                yield return new WaitForSeconds(2f); // Retry after a delay
                continue;
            }

            string jsonResponse = request.downloadHandler.text;
            PlayerApiResponse response = JsonConvert.DeserializeObject<PlayerApiResponse>(jsonResponse);

            if (response != null && response.response != null)
            {
                foreach (var playerData in response.response)
                {
                    if (playerData.statistics != null && playerData.statistics.Count > 0)
                    {
                        int appearances = playerData.statistics[0].games?.appearences ?? 0;

                            if (appearances > 0)
                            {
                                string json = JsonConvert.SerializeObject(playerData.player);
                                playerData.CalculateSkillsFromJson(json);

                                allPlayers.Add(playerData);
                            }
                    }
                }


                totalPages = response.paging.total;
                currentPage++;
            }
            else
            {
                Debug.LogError("Invalid response received from API.");
                break;
            }
        }

        if (allPlayers.Count > 0)
        {
            playerCache[teamId] = allPlayers;
            localDataManager.SavePlayerData(allPlayers);
        }

        PopulateScrollView(allPlayers);
        loadingPanel.SetActive(false);
    }

    // ============================
    // Scroll View Population
    // ============================

    public void PopulateScrollView(List<PlayerData> players)
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        float yOffset = -10f;
        float itemHeight = 20f;
        float spacing = 0f;

        for (int i = 0; i < players.Count; i++)
        {
            var playerData = players[i];
            if (playerData?.player == null) continue;

            // Extract first name and last name
            string[] firstNameParts = playerData.player.firstname.Split(' ');
            string firstName = firstNameParts.Length > 0 ? firstNameParts[0] : string.Empty;

            string[] lastNameParts = playerData.player.lastname.Split(' ');
            string lastName = lastNameParts.Length > 0 ? lastNameParts[lastNameParts.Length - 1] : string.Empty;

            string fullPlayerName = $"{firstName} {lastName}";

            // Create a new list item with the player's name and image
            GameObject playerListObject = Instantiate(playerListPrefab, contentParent);
            TMP_Text nameText = playerListObject.transform.Find("PlayerName").GetComponent<TMP_Text>();
            nameText.text = fullPlayerName;
            Image playerImage = playerListObject.transform.Find("PlayerImage").GetComponent<Image>();

            if (playerImage != null && !string.IsNullOrEmpty(playerData.player.photo))
            {
                StartCoroutine(LoadPlayerImage(playerData.player.photo, playerImage));
            }

            Toggle toggle = playerListObject.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn)
                {
                Debug.Log("Player Item clicked!");
                OnPlayerItemClicked(playerData);
            }
        });
    }   

            RectTransform rectTransform = playerListObject.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(93, yOffset);

            yOffset -= (itemHeight + spacing);
        }

        RectTransform contentRect = contentParent.GetComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, Mathf.Abs(yOffset) + 20);
        contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, 0);
    }

    private void OnPlayerItemClicked(PlayerData playerData)
    {
    Debug.Log("Player Item clicked!"); // Debug message added

    // Get the PlayerSelectionContext component
    PlayerSelectionContext selectionContext = FindObjectOfType<PlayerSelectionContext>();
    if (selectionContext == null)
    {
        Debug.LogError("PlayerSelectionContext not found in the scene.");
        return;
    }

    // Get the PlayerSelectionManager component
    PlayerSelectionManager selectionManager = FindObjectOfType<PlayerSelectionManager>();
    if (selectionManager == null)
    {
        Debug.LogError("PlayerSelectionManager not found in the scene.");
        return;
    }

    // Use the currentPositionIndex from the selection context
    int positionIndex = selectionContext.currentPositionIndex;
    if (positionIndex == -1)
    {
        Debug.LogError("Invalid position index in PlayerSelectionContext.");
        return;
    }
    // Assign the player data to the correct team and position
    selectionManager.AssignPlayer(positionIndex, playerData, selectionContext.isTeamOne);

    // Serialize the entire player data to JSON string for logging
    string playerJson = JsonConvert.SerializeObject(playerData);

    // Log the player's first and last name along with the serialized player data
    Debug.Log($"Assigned player {playerData.player.firstname} {playerData.player.lastname} to team {(selectionContext.isTeamOne ? "One" : "Two")} at position {positionIndex}. Player Data: {playerJson}");

    }

    // ============================
    // Image Loading
    // ============================

    private IEnumerator LoadPlayerImage(string url, Image playerImage)
    {
        Sprite cachedSprite = PlayerImageCache.Instance.GetImage(url);
        if (cachedSprite != null)
        {
            playerImage.sprite = cachedSprite;
            yield break;
        }

        yield return LoadImageFromUrl(url, playerImage);

        if (playerImage.sprite == null)
        {
            string placeholderUrl = "https://media.api-sports.io/football/players/328089.png";
            yield return LoadImageFromUrl(placeholderUrl, playerImage);
        }
    }

    private IEnumerator LoadImageFromUrl(string url, Image playerImage)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            playerImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            PlayerImageCache.Instance.CacheImage(url, playerImage.sprite);
        }
    }
}

// ============================
// Image Cache Singleton
// ============================

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