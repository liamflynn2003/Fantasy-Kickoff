using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Newtonsoft.Json;

public class PlayerViewManager : MonoBehaviour
{
    public TMP_Dropdown leagueDropdown;
    public TMP_Dropdown teamDropdown;

    private string apiKey = "3496853baf021377f539dce314b05abe";
    private string baseUrl = "https://v3.football.api-sports.io/players?team={0}&season=2023";

    private bool isRequestActive = false; // Prevents multiple requests
    private bool leagueReady = false;
    private bool teamReady = false;

    public TeamDropdown teamDropdownScript;

    private void Start()
    {
        leagueDropdown.onValueChanged.AddListener(delegate { CheckDropdowns(); });
        teamDropdown.onValueChanged.AddListener(delegate { CheckDropdowns(); });

        StartCoroutine(InitializeDropdowns());
    }

    private IEnumerator InitializeDropdowns()
    {
        yield return new WaitForSeconds(1f);
        int selectedTeamId = teamDropdownScript.GetSelectedTeamId();
        Debug.Log("Selected Team ID: " + selectedTeamId);
    }

    private void CheckDropdowns()
    {
        leagueReady = leagueDropdown.options.Count > 1 && leagueDropdown.value >= 0;
        teamReady = teamDropdown.options.Count > 1 && teamDropdown.value >= 0;

        if (leagueReady && teamReady)
        {
            StartCoroutine(SendApiRequest());
        }
    }


    private IEnumerator SendApiRequest()
    {
        isRequestActive = true;

        int teamId = teamDropdownScript.GetSelectedTeamId();
        if (teamId > 0)
        {
            string apiUrl = string.Format(baseUrl, teamId);
            Debug.Log("Fetching Players from API: " + apiUrl); // Log the URL

            UnityWebRequest request = UnityWebRequest.Get(apiUrl);
            request.SetRequestHeader("x-apisports-key", apiKey);

            yield return request.SendWebRequest();

            Debug.Log("API Raw Response: " + request.downloadHandler.text); // Log full response

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("API Request Error: " + request.error);
            }
            else
            {
                Debug.Log("API Response: " + request.downloadHandler.text);
            }
        }

        yield return new WaitForSeconds(1f);
        isRequestActive = false;
    }

}
