using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Newtonsoft.Json;
using System.Collections.Generic;

public class TeamDropdown : MonoBehaviour
{
    public TMP_Dropdown leagueDropdown;
    public TMP_Dropdown teamDropdown;

    private string apiKey = "3496853baf021377f539dce314b05abe";
    private string baseUrl = "https://v3.football.api-sports.io/teams?league={0}&season=2023";

    private void Start()
    {
        if (leagueDropdown != null)
        {
            leagueDropdown.onValueChanged.AddListener(delegate { OnLeagueChanged(); });
        }
    }

    private void OnLeagueChanged()
    {
        string selectedLeague = leagueDropdown.options[leagueDropdown.value].text;
        Debug.Log("Selected League: " + selectedLeague);

        int leagueId = GetLeagueId(selectedLeague);
        if (leagueId > 0)
        {
            StartCoroutine(FetchTeams(leagueId));
        }
    }

    public string GetSelectedLeague()
    {
        return leagueDropdown.options[leagueDropdown.value].text;
    }

    private IEnumerator FetchTeams(int leagueId)
    {
        string apiUrl = string.Format(baseUrl, leagueId);
        UnityWebRequest request = UnityWebRequest.Get(apiUrl);
        request.SetRequestHeader("x-apisports-key", apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("API Request Error: " + request.error);
        }
        else
        {
            // Log the raw JSON response to the console
            Debug.Log("Raw JSON Response: " + request.downloadHandler.text);

            // Deserialize JSON
            TeamApiResponse response = JsonConvert.DeserializeObject<TeamApiResponse>(request.downloadHandler.text);

            // Debugging: Log the response object to check its structure
            Debug.Log("Converted Response: " + JsonConvert.SerializeObject(response));

            // Check if we have any teams and print their names
            if (response != null && response.response != null)
            {
                foreach (var teamData in response.response)
                {
                    if (teamData != null && teamData.team != null)
                    {
                        Debug.Log("Team Name: " + teamData.team.name); // Log the team name
                    }
                    else
                    {
                        Debug.LogWarning("TeamData or Team is null.");
                    }
                }
            }
            else
            {
                Debug.LogWarning("No teams found in the response.");
            }

            PopulateTeamDropdown(response);
        }
    }


    private void PopulateTeamDropdown(TeamApiResponse response)
    {
        teamDropdown.ClearOptions();

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        Dictionary<string, TMP_Dropdown.OptionData> optionMap = new Dictionary<string, TMP_Dropdown.OptionData>();

        foreach (var teamData in response.response)
        {
            TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData(teamData.team.name);
            options.Add(option);
            optionMap[teamData.team.name] = option;

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

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error loading image: " + request.error);
        }
        else
        {
            Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            Sprite logoSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

            option.image = logoSprite;

            teamDropdown.RefreshShownValue();
            ForceDropdownRefresh();
        }
    }

    private void ForceDropdownRefresh()
    {
        int tempValue = teamDropdown.value;
        teamDropdown.value = (tempValue == 0) ? 1 : 0; // Toggle value to trigger refresh
        teamDropdown.value = tempValue;
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
            default: return -1;
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
