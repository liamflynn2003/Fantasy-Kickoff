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

    private string apiKey = Environment.GetEnvironmentVariable("FOOTBALL_API_KEY");
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
        string selectedLeague = leagueDropdown.options[leagueDropdown.value].text;
        Debug.Log("Selected League: " + selectedLeague);

        int leagueId = GetLeagueId(selectedLeague);
        int season = (leagueId == 357) ? 2025 : 2024; // Use 2025 for Lg. of Ireland, otherwise default to 2024

        if (leagueId > 0)
        {
            StartCoroutine(FetchTeams(leagueId, season));
        }
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
        TeamApiResponse response = JsonConvert.DeserializeObject<TeamApiResponse>(request.downloadHandler.text);
        PopulateTeamDropdown(response);
    }

    private void PopulateTeamDropdown(TeamApiResponse response)
    {
        teamDropdown.ClearOptions();
        teamIdMap.Clear(); // Clear previous team IDs

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

        foreach (var teamData in response.response)
        {
            TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData(teamData.team.name);
            options.Add(option);
            teamIdMap[teamData.team.name] = teamData.team.id; // Store team ID

            StartCoroutine(LoadLogo(teamData.team.logo, option));
        }

        teamDropdown.AddOptions(options);
        teamDropdown.value = 0;
        teamDropdown.captionText.text = "Select a Team";
    }

    private IEnumerator LoadLogo(string url, TMP_Dropdown.OptionData option)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();
        Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
        Sprite logoSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

        option.image = logoSprite;
        teamDropdown.RefreshShownValue();
    }

    public int GetSelectedTeamId()
    {
        string selectedTeam = teamDropdown.options[teamDropdown.value].text;
        return teamIdMap.ContainsKey(selectedTeam) ? teamIdMap[selectedTeam] : -1;
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
            default: return 39; // Default to Premier League
        }
    }
}

// JSON Classes
[System.Serializable]
public class TeamApiResponse
{
    public TeamData[] response;
}

[System.Serializable]
public class TeamData
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
