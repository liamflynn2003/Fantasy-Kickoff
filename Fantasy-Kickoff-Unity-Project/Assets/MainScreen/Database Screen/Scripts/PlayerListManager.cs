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
    public GameObject DatabaseScreen;
    public GameObject scrollView;
    private Sprite placeholderSprite;
    private bool placeholderLoaded = false;


    private Transform teamOnePanel;
    private Transform teamTwoPanel;

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
        public Vector2 currentPOS;
        public int fitness;
        public int id;
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
float minutes = stats.games?.appearences > 0 ? (stats.games.appearences ?? 0) * 90f : 1f; // fallback to 1 to avoid div by 0

float passTotal = stats.passes?.total ?? 0;
float keyPasses = stats.passes?.key ?? 0;
float passScore = passTotal > 0 ? (keyPasses / passTotal) * 100f : 0;

float shotsTotal = stats.shots?.total ?? 0;
float shotsOnTarget = stats.shots?.on ?? 0;
float goals = stats.goals?.total ?? 0;
float shotAccuracy = shotsTotal > 0 ? (shotsOnTarget / shotsTotal) * 100f : 0;
float conversion = shotsTotal > 0 ? (goals / shotsTotal) * 100f : 0;
float shootingScore = (shotAccuracy * 0.6f + conversion * 0.4f);

float tackles = stats.tackles?.total ?? 0;
float interceptions = stats.tackles?.interceptions ?? 0;
float duelsWon = stats.duels?.won ?? 0;
float duelsTotal = stats.duels?.total ?? 0;
float duelWinRate = duelsTotal > 0 ? (duelsWon / duelsTotal) * 100f : 0;
float tacklingScore = (tackles + interceptions) * 2 + duelWinRate * 0.5f;

float saves = stats.goals?.saves ?? 0;
float conceded = stats.goals?.conceded ?? 0;
float saveRate = (saves + conceded) > 0 ? (saves / (saves + conceded)) * 100f : 0;
float savingScore = saveRate;

float dribbleAttempts = stats.dribbles?.attempts ?? 0;
float dribbleSuccess = stats.dribbles?.success ?? 0;
float dribbleRate = dribbleAttempts > 0 ? (dribbleSuccess / dribbleAttempts) * 100f : 0;
float agilityScore = dribbleRate + (stats.fouls?.drawn ?? 0);

                float foulsCommitted = stats.fouls?.committed ?? 0;
                float strengthScore = duelWinRate - foulsCommitted;

                float pensScored = stats.penalty?.scored ?? 0;
                float pensMissed = stats.penalty?.missed ?? 0;
                float totalPens = pensScored + pensMissed;
                float penaltyRate = totalPens > 0 ? (pensScored / totalPens) * 100f : 0;
                float penaltyScore = penaltyRate;

                float height = Convert.ToInt32(player.height?.Replace(" cm", "") ?? "0");
                float jumpingScore = (height - 150f) * 0.75f;

                skill = new Skill
                {
                    passing = NormalizeToSkillRange(passScore),
                    shooting = NormalizeToSkillRange(shootingScore),
                    tackling = NormalizeToSkillRange(tacklingScore),
                    saving = NormalizeToSkillRange(savingScore),
                    agility = NormalizeToSkillRange(agilityScore),
                    strength = NormalizeToSkillRange(strengthScore),
                    penaltyTaking = NormalizeToSkillRange(penaltyScore),
                    jumping = NormalizeToSkillRange(jumpingScore)
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

        private int NormalizeToSkillRange(float value)
        {
            value = Mathf.Clamp(value, 0f, 100f);
            return Mathf.Clamp(Mathf.RoundToInt((value / 100f) * 98f + 1f), 1, 99);
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
    public class GoalStatistics { public int? total; public int? saves; public int? conceded; } 
    public class TackleStatistics { public int? total; public int? interceptions; }
    public class DuelStatistics { public int? total; public int? won; }
    public class DribbleStatistics { public int? success; public int? attempts; }
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
        StartCoroutine(LoadPlaceholderImage());
        localDataManager = GetComponent<LocalDataManager>();
        playerCache = localDataManager.LoadCache();

        if(!DatabaseScreen.activeInHierarchy){
            teamOnePanel = GameObject.Find("TeamOnePanel").transform;
            teamTwoPanel = GameObject.Find("TeamTwoPanel").transform;
        }

        if (teamDropdown != null)
        {
        leagueDropdown.onValueChanged.AddListener(delegate { OnLeagueChanged(); });
        teamDropdown.onValueChanged.AddListener(delegate { OnTeamSelected(); });

        SetInitialTeamDropdownValue();
        StartCoroutine(WaitForDropdownToPopulateAndSelectTeam());
        }

        StartImageQueue();
        OnLeagueChanged();
        
    }
    private IEnumerator LoadPlaceholderImage()
{
    if (placeholderLoaded) yield break;

    UnityWebRequest request = UnityWebRequestTexture.GetTexture("https://media.api-sports.io/football/players/328089.png");
    yield return request.SendWebRequest();

    if (request.result == UnityWebRequest.Result.Success)
    {
        Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
        placeholderSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        placeholderLoaded = true;
    }
}


    private IEnumerator WaitForDropdownToPopulateAndSelectTeam()
{
    if (!this.isActiveAndEnabled) yield break;
    // Wait until the dropdown has options
    while (teamDropdown.options.Count == 0)
    {
        Debug.Log("Waiting for team dropdown to populate...");
        yield return null; // Wait for the next frame
    }

    Debug.Log("Team dropdown populated. Calling OnTeamSelected.");
    OnTeamSelected(); // Call OnTeamSelected after the dropdown is populated
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
    // Check if the team dropdown has any options
    if (teamDropdown.options.Count == 0)
    {
        Debug.LogError("Team dropdown is empty. Cannot select a team.");
        return;
    }

    // Get the selected team ID
    TeamDropdown teamDropdownComponent = FindObjectOfType<TeamDropdown>();
    if (teamDropdownComponent == null)
    {
        Debug.LogError("TeamDropdown component not found.");
        return;
    }

    int? selectedTeamId = teamDropdownComponent.GetSelectedTeamId();
    if (selectedTeamId == null || selectedTeamId <= 0)
    {
        Debug.Log("No valid team selected yet. Skipping team load.");
        return;
    }

    int teamId = selectedTeamId.Value;

    // Check if the team ID is already cached or needs to fetch data
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
private Queue<(string url, Image image)> imageQueue = new Queue<(string url, Image image)>();


private void StartImageQueue()
{
    StartCoroutine(ImageQueueWorker());
}

private IEnumerator ImageQueueWorker()
{
    while (true)
    {
        if (imageQueue.Count > 0)
        {
            var (url, image) = imageQueue.Dequeue();
            if (image != null && image.gameObject.activeInHierarchy)
                yield return LoadImageFromUrl(url, image);
        }
        yield return new WaitForSeconds(0.0001f); // 10fps fetch max
    }
}

private List<Coroutine> activeCoroutines = new List<Coroutine>();

public void PopulateScrollView(List<PlayerData> players)
{
    // Stop all active coroutines before clearing the scroll view
    foreach (var coroutine in activeCoroutines)
    {
        StopCoroutine(coroutine);
    }
    activeCoroutines.Clear();

    // Clear existing UI elements
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

        string fullPlayerName = $"{playerData.player.firstname} {playerData.player.lastname}";

        // Create a new list item with the player's name and image
        GameObject playerListObject = Instantiate(playerListPrefab, contentParent);
        TMP_Text nameText = playerListObject.transform.Find("PlayerName").GetComponent<TMP_Text>();
        nameText.text = fullPlayerName;

        Toggle toggle = playerListObject.GetComponent<Toggle>();
        if (toggle != null)
{
    var dataCopy = playerData; // capture properly in closure
    toggle.onValueChanged.AddListener((isOn) =>
    {
        if (isOn && !DatabaseScreen.activeInHierarchy)
        {
            Debug.Log("Player Item clicked!");
            OnPlayerItemClicked(dataCopy);
            scrollView.SetActive(false);
        }
    });
}

        Image playerImage = playerListObject.transform.Find("PlayerImage").GetComponent<Image>();
        if (playerImage != null && !string.IsNullOrEmpty(playerData.player.photo))
        {
            imageQueue.Enqueue((playerData.player.photo, playerImage));
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

    // Update UI
    UpdatePlayerButtonUI(selectionContext, playerData, positionIndex);
    // Serialize the entire player data to JSON string for logging
    string playerJson = JsonConvert.SerializeObject(playerData);

    // Log the player's first and last name along with the serialized player data
    Debug.Log($"Assigned player {playerData.player.firstname} {playerData.player.lastname} to team {(selectionContext.isTeamOne ? "One" : "Two")} at position {positionIndex}. Player Data: {playerJson}");

    }

    // Update UI
private void UpdatePlayerButtonUI(PlayerSelectionContext selectionContext, PlayerData playerData, int positionIndex)
{
    Transform teamPanel = selectionContext.isTeamOne ? teamOnePanel : teamTwoPanel;

    // Find the specific player button by name
    Transform playerButtonTransform = teamPanel.Find($"Player_{positionIndex}");
    if (playerButtonTransform == null)
    {
        Debug.LogError($"Player button for position {positionIndex} not found in {(selectionContext.isTeamOne ? "TeamOnePanel" : "TeamTwoPanel")}.");
        return;
    }

    // Update the player's name in the button
    TMP_Text nameText = playerButtonTransform.Find("PlayerName").GetComponent<TMP_Text>();
    if (nameText != null)
    {
        nameText.text = playerData.player.lastname;
    }
    else
    {
        Debug.LogError($"PlayerName TMP_Text component not found in Player_{positionIndex}.");
    }
}

    // ============================
    // Image Loading
    // ============================

private IEnumerator LoadPlayerImage(string url, Image playerImage)
{
    if (playerImage == null || playerImage.gameObject == null)
    {
        Debug.LogWarning("PlayerImage is null or destroyed. Skipping image loading.");
        yield break;
    }

    Sprite cachedSprite = PlayerImageCache.Instance.GetImage(url);
    if (cachedSprite != null)
    {
        if (playerImage != null)
        {
            playerImage.sprite = cachedSprite;
        }
        yield break;
    }

    // Retry logic with a maximum of 5 retries
    int retryCount = 0;
    bool imageLoaded = false;
    while (retryCount < 5 && !imageLoaded)
    {
        yield return LoadImageFromUrl(url, playerImage);
        
        if (playerImage.sprite != null)
        {
            imageLoaded = true;
        }
        else
        {
            // Log the error and retry after a delay (0.5f seconds)
            Debug.LogError($"Failed to load image from URL: {url}. Retrying... Attempt {retryCount + 1}/5.");
            retryCount++;
            yield return new WaitForSeconds(0.5f);  // Adding delay before retry
        }
    }

    if (!imageLoaded && placeholderSprite != null && playerImage != null)
{
    playerImage.sprite = placeholderSprite;
}

}

private IEnumerator LoadImageFromUrl(string url, Image playerImage)
{
    UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
    yield return request.SendWebRequest();

    if (request.result == UnityWebRequest.Result.Success)
    {
        if (playerImage != null && playerImage.gameObject != null && playerImage.gameObject.activeInHierarchy)
        {
            Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            playerImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            PlayerImageCache.Instance.CacheImage(url, playerImage.sprite);
        }
        else
        {
            Debug.LogWarning("PlayerImage is null or destroyed. Skipping sprite assignment.");
        }
    }
    else
    {
        Debug.LogError($"Failed to load image from URL: {url}. Error: {request.error}");
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