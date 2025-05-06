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
    public bool viewOnlyMode = false;

    // UI Elements
    public TMP_Dropdown teamDropdown;
    public TMP_Dropdown leagueDropdown;
    public Transform contentParent;
    public GameObject playerListPrefab;
    public GameObject loadingPanel;
    public GameObject DatabaseScreen;
    public GameObject scrollView;

    private Transform teamOnePanel;
    private Transform teamTwoPanel;
    private PlayerSelectionManager selectionManager;

    // Player Card
    public GameObject playerDetailScreen;
    public TMP_Text playerCardNameText;
    public Image playerCardImage;
    public TMP_Text passingText;
    public TMP_Text shootingText;
    public TMP_Text tacklingText;
    public TMP_Text savingText;
    public TMP_Text agilityText;
    public TMP_Text strengthText;
    public TMP_Text penaltyTakingText;
    public TMP_Text goalsText;
    public TMP_Text shotsText;
    public TMP_Text tacklesText;
    public TMP_Text dribblesText;
    public TMP_Text keyPassesText;
    public TMP_Text totalPassesText;
    public TMP_Text heightText;


    // API Configuration
    [Header("API Settings")]
public string apiKey;

private void Awake()
{
    if (string.IsNullOrEmpty(apiKey))
    {
        apiKey = Environment.GetEnvironmentVariable("FOOTBALL_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("[PlayerListManager] API key not set via Inspector or environment variable!");
        }
    }
}

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
// ========== PASSING ==========
float passTotal = stats.passes?.total ?? 0;
float keyPasses = stats.passes?.key ?? 0;
float passAccuracy = stats.passes?.accuracy ?? 0;

float passScore = 50f; // Base score
passScore += keyPasses; // +1 per key pass
passScore += Mathf.Floor(passAccuracy / 5f) * 2f; // +2 per 5% accuracy

// ========== SHOOTING ==========
float shotsTotal = stats.shots?.total ?? 0;
float shotsOnTarget = stats.shots?.on ?? 0;
float goals = stats.goals?.total ?? 0;

float shotAccuracy = shotsTotal > 0 ? (shotsOnTarget / shotsTotal) * 100f : 0f;
float conversionRate = shotsTotal > 0 ? (goals / shotsTotal) * 100f : 0f;

float rawShotScore = (shotAccuracy * 0.3f + conversionRate * 0.7f) * 2f;
float shotSampleWeight = Mathf.Log10(shotsTotal + 1f);
float weightedShotScore = rawShotScore * Mathf.Clamp01(shotSampleWeight);
float goalBonus = goals >= 1 ? Mathf.Floor(goals / 5f) * 5f : 0f;

float shootingScore = Mathf.Clamp(weightedShotScore + goalBonus, 0f, 100f);

// ========== TACKLING ==========
// ========== TACKLING ==========
float tackles = stats.tackles?.total ?? 0f;
float interceptions = stats.tackles?.interceptions ?? 0f;
float duelWinRate = (stats.duels?.total ?? 0) > 0
    ? ((stats.duels?.won ?? 0) / (float)(stats.duels?.total ?? 1)) * 100f
    : 0f;

// Scaled-down weights
float rawTacklingScore = (tackles * 1f) + (interceptions * 1f) + (duelWinRate * 0.1f);
float tacklingScore = Mathf.Clamp(rawTacklingScore, 0f, 100f);

// ========== SAVING ==========
float saves = stats.goals?.saves ?? 0;
float conceded = stats.goals?.conceded ?? 0;
float saveRate = (saves + conceded) > 0 ? (saves / (saves + conceded)) * 100f : 0f;

float savingScore = saveRate;

// ========== AGILITY ==========
float dribbleAttempts = stats.dribbles?.attempts ?? 0;
float dribbleSuccess = stats.dribbles?.success ?? 0;
float dribbleRate = dribbleAttempts > 0 ? (dribbleSuccess / dribbleAttempts) * 100f : 0f;
float foulsDrawn = stats.fouls?.drawn ?? 0f;

float agilityScore = dribbleRate + foulsDrawn;

// ========== STRENGTH ==========
float foulsCommitted = stats.fouls?.committed ?? 0;
float weightKg = Convert.ToInt32(player.weight?.Replace(" kg", "") ?? "0");
float weightFactor = weightKg * 0.5f;

float strengthScore = (duelWinRate - foulsCommitted) + weightFactor;

// ========== PENALTIES ==========
float pensScored = stats.penalty?.scored ?? 0;
float pensMissed = stats.penalty?.missed ?? 0;
float totalPens = pensScored + pensMissed;

float penaltyRate = totalPens > 0 ? (pensScored / totalPens) * 100f : 0f;
float penaltyScore = penaltyRate;

// ========== JUMPING ==========
float heightCm = Convert.ToInt32(player.height?.Replace(" cm", "") ?? "0");
float jumpingScore = Mathf.Clamp(heightCm, 0f, 300f);

                skill = new Skill
                {
                    passing = NormalizeToSkillRange(passScore),
                    shooting = NormalizeToSkillRange(shootingScore),
                    tackling = NormalizeToSkillRange(tacklingScore),
                    saving = NormalizeToSkillRange(savingScore),
                    agility = NormalizeToSkillRange(agilityScore),
                    strength = NormalizeToSkillRange(strengthScore),
                    penaltyTaking = NormalizeToSkillRange(penaltyScore),
                    jumping = Mathf.RoundToInt(jumpingScore)
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
    public class PassStatistics { public int? total; public int? key; public int? accuracy; }
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
    localDataManager = GetComponent<LocalDataManager>();
    playerCache = localDataManager.LoadCache();

    if (!viewOnlyMode)
    {
        selectionManager = FindObjectOfType<PlayerSelectionManager>();

        if (selectionManager == null)
        {
            Debug.LogError("PlayerSelectionManager not found in the scene.");
            return;
        }

        if (selectionManager.multiplayerMode)
        {
            teamTwoPanel = GameObject.Find("TeamPanel")?.transform;
        }
        else
        {
            teamOnePanel = GameObject.Find("TeamOnePanel")?.transform;
            teamTwoPanel = GameObject.Find("TeamTwoPanel")?.transform;
        }
    }

    if (teamDropdown != null)
    {
        leagueDropdown.onValueChanged.AddListener(delegate { OnLeagueChanged(); });
        teamDropdown.onValueChanged.AddListener(delegate { OnTeamSelected(); });

        SetInitialTeamDropdownValue();
    }

    OnLeagueChanged();
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
                    if (isOn && !DatabaseScreen.activeInHierarchy)
                    {
                    Debug.Log("Player Item clicked!");
                    OnPlayerItemClicked(playerData);
                    scrollView.SetActive(false);
                    }
                    if (isOn && DatabaseScreen.activeInHierarchy)
                    {
                        ShowPlayerDetailScreen(playerData);
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
        if (viewOnlyMode)
    {
        Debug.Log("Clicked player in view-only mode. No assignment performed.");
        return;
    }
    Debug.Log("Player Item clicked!"); // Debug message added

    // Get the PlayerSelectionContext component
    PlayerSelectionContext selectionContext = FindObjectOfType<PlayerSelectionContext>();
    if (selectionContext == null)
    {
        Debug.LogError("PlayerSelectionContext not found in the scene.");
        return;
    }

    // Get the PlayerSelectionManager component

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

if (selectionManager.multiplayerMode)
{
    selectionManager.MultiplayerAssignPlayer(positionIndex, playerData);
}
else
{
    selectionManager.AssignPlayer(positionIndex, playerData, selectionContext.isTeamOne);
}

// Update UI
UpdatePlayerButtonUI(selectionContext, playerData, positionIndex);

// Log player
string playerJson = JsonConvert.SerializeObject(playerData);
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
    // Player Detail Card SCreen
    // ============================
public void ShowPlayerDetailScreen(PlayerData playerData)
{
    playerCardNameText.text = $"{playerData.player.firstname} {playerData.player.lastname}";

    if (!string.IsNullOrEmpty(playerData.player.photo))
        StartCoroutine(LoadPlayerImage(playerData.player.photo, playerCardImage));

    passingText.text = $"Passing: {playerData.skill.passing}";
    shootingText.text = $"Shooting: {playerData.skill.shooting}";
    tacklingText.text = $"Tackling: {playerData.skill.tackling}";
    savingText.text = $"Saving: {playerData.skill.saving}";
    agilityText.text = $"Agility: {playerData.skill.agility}";
    strengthText.text = $"Strength: {playerData.skill.strength}";
    penaltyTakingText.text = $"Penalty Taking: {playerData.skill.penaltyTaking}";

    var stats = playerData.statistics[0];

    goalsText.text = $"Goals: {stats.goals?.total ?? 0}";
    shotsText.text = $"Shots: {stats.shots?.total ?? 0} (On Target: {stats.shots?.on ?? 0})";
    tacklesText.text = $"Tackles: {stats.tackles?.total ?? 0}, Intercept.: {stats.tackles?.interceptions ?? 0}";
    dribblesText.text = $"Dribbles: {stats.dribbles?.success ?? 0} / {stats.dribbles?.attempts ?? 0}";
    keyPassesText.text = $"Key Passes: {stats.passes?.key ?? 0}";
    totalPassesText.text = $"Total Passes: {stats.passes?.total ?? 0}";
    heightText.text = $"Height: {playerData.player.height ?? "N/A"}";

    playerDetailScreen.SetActive(true);
    DatabaseScreen.SetActive(false);
}

public void ClosePlayerDetailScreen()
{
    playerDetailScreen.SetActive(false);
    DatabaseScreen.SetActive(true);
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

    public void ClearPlayerCache()
    {
        playerCache.Clear();
        localDataManager.SaveCache(playerCache); // Save the cleared cache to persist the changes
        Debug.Log("Player cache cleared.");
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