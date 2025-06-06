using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Newtonsoft.Json;
using System;

public class TeamDropdown : MonoBehaviour
{
    public TMP_Dropdown leagueDropdown;
    public TMP_Dropdown teamDropdown;
    private Dictionary<string, Sprite> logoCache = new Dictionary<string, Sprite>();
    private Coroutine leagueChangeCoroutine;

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
    private string baseUrl = "https://v3.football.api-sports.io/teams?league={0}&season={1}";

    private Dictionary<string, int> teamIdMap = new Dictionary<string, int>(); // Stores team names and IDs

    private void Start()
    {
        if (leagueDropdown != null)
        {
            leagueDropdown.onValueChanged.AddListener(delegate { OnLeagueChanged(); });
        }
        OnLeagueChanged();
    }

private void OnLeagueChanged()
{
    if (leagueChangeCoroutine != null)
    {
        StopCoroutine(leagueChangeCoroutine);
    }
    leagueChangeCoroutine = StartCoroutine(DebouncedLeagueChange());
}

private IEnumerator DebouncedLeagueChange()
{
    yield return new WaitForSeconds(0.5f); // Wait for 0.5 seconds to debounce
    string selectedLeague = leagueDropdown.options[leagueDropdown.value].text;
    Debug.Log("Selected League: " + selectedLeague);

    int leagueId = GetLeagueId(selectedLeague);
    int season = (leagueId == 357) ? 2025 : 2024; // Use 2025 for Lg. of Ireland, otherwise default to 2024

    if (leagueId > 0)
    {
        StartCoroutine(FetchTeamsAndNotify(leagueId, season));
    }
}

private IEnumerator FetchTeamsAndNotify(int leagueId, int season)
{
    yield return FetchTeams(leagueId, season); // Wait for teams to be fetched

    // Ensure the team dropdown is fully updated before notifying PlayerListManager
    if (teamDropdown.options.Count > 0)
    {
        teamDropdown.value = 0; // Set to the first option
        teamDropdown.captionText.text = teamDropdown.options[0].text; // Update the caption
    }

    // Notify PlayerListManager after the dropdown is fully updated
    FindObjectOfType<PlayerListManager>().OnTeamSelected();
}

    public string GetSelectedLeague()
    {
        return leagueDropdown.options[leagueDropdown.value].text;
    }

private IEnumerator FetchTeams(int leagueId, int season)
{
    string apiUrl = string.Format(baseUrl, leagueId, season);
    UnityWebRequest request = UnityWebRequest.Get(apiUrl);
    request.SetRequestHeader("x-apisports-key", apiKey);

    yield return request.SendWebRequest();

    if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
    {
        Debug.LogError("Error fetching teams: " + request.error);
        yield break;
    }

    TeamApiResponse response = JsonConvert.DeserializeObject<TeamApiResponse>(request.downloadHandler.text);

    if (response == null || response.response == null || response.response.Length == 0)
    {
        Debug.LogWarning("No teams found for league ID: " + leagueId);
        yield break;
    }

    PopulateTeamDropdown(response);
}

private void PopulateTeamDropdown(TeamApiResponse response)
{
    teamDropdown.onValueChanged.RemoveAllListeners(); // Temporarily disable listeners
    teamDropdown.ClearOptions();
    teamIdMap.Clear(); // Clear previous team IDs

    List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
    List<string> logoUrls = new List<string>();
    List<TMP_Dropdown.OptionData> logoOptions = new List<TMP_Dropdown.OptionData>();

    foreach (var teamData in response.response)
    {
        TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData(teamData.team.name);
        options.Add(option);
        teamIdMap[teamData.team.name] = teamData.team.id; // Store team ID

        // Collect logo URLs and options for batch processing
        if (!string.IsNullOrEmpty(teamData.team.logo))
        {
            logoUrls.Add(teamData.team.logo);
            logoOptions.Add(option);
        }
    }

    teamDropdown.AddOptions(options);

    if (options.Count > 0)
    {
        Debug.Log("Team dropdown populated with " + options.Count + " teams.");
    }
    else
    {
        Debug.LogWarning("No valid options to set in the team dropdown.");
    }

    // Start loading logos in batches
    StartCoroutine(LoadLogosInBatches(logoUrls, logoOptions));

    teamDropdown.onValueChanged.AddListener(delegate { FindObjectOfType<PlayerListManager>().OnTeamSelected(); });
}

private IEnumerator LoadLogosInBatches(List<string> urls, List<TMP_Dropdown.OptionData> options)
{
    for (int i = 0; i < urls.Count; i++)
    {
        yield return LoadLogo(urls[i], options[i]);
        yield return new WaitForSeconds(0.1f);
    }
}

private IEnumerator LoadLogo(string url, TMP_Dropdown.OptionData option)
{
    if (logoCache.ContainsKey(url))
    {
        option.image = logoCache[url]; // Use cached logo
        teamDropdown.RefreshShownValue();
        yield break;
    }

    UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
    int retryCount = 0;
    const int maxRetries = 5;

    while (retryCount < maxRetries)
    {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            if (request.responseCode == 429)
            {
                retryCount++;
                float waitTime = Mathf.Pow(2, retryCount);
                Debug.LogWarning($"Rate limit hit. Retrying in {waitTime} seconds...");
                yield return new WaitForSeconds(waitTime);
                continue; // Retry the request
            }

            Debug.LogError("Error loading logo from URL: " + url + " - " + request.error);
            yield break;
        }
        break;
    }

    if (retryCount == maxRetries)
    {
        Debug.LogError("Max retries reached. Failed to load logo from URL: " + url);
        yield break;
    }

    // Process the successful response
    Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
    Sprite logoSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    logoCache[url] = logoSprite;
    option.image = logoSprite; 
    teamDropdown.RefreshShownValue();
}

    public int GetSelectedTeamId()
{
    if (teamDropdown.options.Count == 0)
    {
        Debug.LogError("Team dropdown is empty. Cannot get selected team ID.");
        return -1;
    }

    string selectedTeam = teamDropdown.options[teamDropdown.value].text;
    return teamIdMap.ContainsKey(selectedTeam) ? teamIdMap[selectedTeam] : 0;
}

    private int GetLeagueId(string leagueName)
    {
        switch (leagueName)
        {
            case "Premier League": return 39;
            case "Championship": return 40;
            case "La Liga": return 140;
            case "Serie A": return 135;
            case "Bundesliga": return 78;
            case "Ligue Une": return 61;
            case "Liga Portugal": return 94;
            case "Belgian Pro Lg.": return 144;
            case "Lg. of Ireland": return 357;
            default: return 39;
        }
    }
}

// JSON Classes
[System.Serializable]
public class TeamApiResponse
{
    public DropdownTeamData[] response;
}

[System.Serializable]
public class DropdownTeamData
{
    public Team team;
}

[System.Serializable]
public class Team
{
    public int id;
    public string name;
    public string logo;
}
