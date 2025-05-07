using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using Newtonsoft.Json;
using System.IO;
using Firebase;
using Firebase.Database;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Storage;

public class MatchDataManager : MonoBehaviour
{
    public PlayerSelectionManager selectionManager;
    private string serverUrl;
    private string firebaseStorageUrl;
    private string firebaseDatabaseUrl;

private void Start()
{
    serverUrl = Environment.GetEnvironmentVariable("SERVER_POST_URL");
    firebaseStorageUrl = Environment.GetEnvironmentVariable("FIREBASE_STORAGE_URL");
    firebaseDatabaseUrl = Environment.GetEnvironmentVariable("FIREBASE_DATABASE_URL");

    Debug.Log("Loaded serverUrl: " + serverUrl);
    Debug.Log("Firebase DB URL: " + firebaseDatabaseUrl);
    Debug.Log("Firebase Storage URL: " + firebaseStorageUrl);
}

    public GameObject loadingScreen; 

public void SimulateMatch()
{
    Dictionary<int, PlayerListManager.PlayerData> teamOne = selectionManager.GetSelectedTeam(true);
    Dictionary<int, PlayerListManager.PlayerData> teamTwo = selectionManager.GetSelectedTeam(false);

    if (teamOne == null || teamTwo == null)
    {
        Debug.LogError("One or both teams are null. Cannot simulate match.");
        return;
    }

    MatchRequest matchRequest = new MatchRequest(teamOne, teamTwo);
    Debug.Log(matchRequest.ToString());

    // Serialize MatchRequest
    string jsonData = JsonConvert.SerializeObject(matchRequest, Formatting.Indented);

    if (string.IsNullOrEmpty(jsonData))
    {
        Debug.LogError("Failed to serialize MatchRequest to JSON.");
        return;
    }

    SaveJsonRequest(jsonData);
    StartCoroutine(PostMatchData(jsonData));
}

public void SimulateMultiplayerMatchFromFirebase()
{
    Debug.Log("Beginning multiplayer match simulation from Firebase.");
    string lobbyId = PlayerSessionInfo.lobbyId;
    if (string.IsNullOrEmpty(lobbyId))
    {
        Debug.LogError("Lobby ID is missing from PlayerSessionInfo.");
        return;
    }
        FirebaseApp app = FirebaseApp.DefaultInstance;

        var db = FirebaseDatabase.GetInstance(app, firebaseDatabaseUrl);
        DatabaseReference lobbyRef = db.RootReference.Child("lobbies").Child(lobbyId);

        Debug.Log("Fetching lobby data from Firebase...");
        lobbyRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
        if (task.IsFaulted)
        {
            Debug.LogError("Failed to fetch lobby data from Firebase: " + task.Exception);
            return;
        }

        DataSnapshot snapshot = task.Result;

        if (!snapshot.HasChild("teamOne") || !snapshot.HasChild("teamTwo"))
        {
            Debug.LogWarning("Both teams are not yet available in Firebase.");
            return;
        }

        string teamOneJson = snapshot.Child("teamOne").GetRawJsonValue();
        string teamTwoJson = snapshot.Child("teamTwo").GetRawJsonValue();

        Debug.Log($"[SimulateMultiplayerMatchFromFirebase] Raw teamOne JSON:\n{teamOneJson}");
        Debug.Log($"[SimulateMultiplayerMatchFromFirebase] Raw teamTwo JSON:\n{teamTwoJson}");

        if (string.IsNullOrEmpty(teamOneJson) || string.IsNullOrEmpty(teamTwoJson))
        {
            Debug.LogError("One or both team JSON strings are empty.");
            return;
        }

List<PlayerSelectionManager.PlayerJsonData> teamOneJsonList =
    JsonConvert.DeserializeObject<List<PlayerSelectionManager.PlayerJsonData>>(teamOneJson);

List<PlayerSelectionManager.PlayerJsonData> teamTwoJsonList =
    JsonConvert.DeserializeObject<List<PlayerSelectionManager.PlayerJsonData>>(teamTwoJson);

Dictionary<int, PlayerListManager.PlayerData> teamOne = new();
Dictionary<int, PlayerListManager.PlayerData> teamTwo = new();

for (int i = 0; i < teamOneJsonList.Count; i++)
{
    var playerJson = teamOneJsonList[i];
    teamOne[i] = ConvertFromJson(playerJson);
}

for (int i = 0; i < teamTwoJsonList.Count; i++)
{
    var playerJson = teamTwoJsonList[i];
    teamTwo[i] = ConvertFromJson(playerJson);
}

        if (teamOne == null || teamTwo == null)
        {
            Debug.LogError("Failed to deserialize one or both teams.");
            return;
        }

        MatchRequest matchRequest = new MatchRequest(teamOne, teamTwo);
        string jsonData = JsonConvert.SerializeObject(matchRequest, Formatting.Indented);

        if (string.IsNullOrEmpty(jsonData))
        {
            Debug.LogError("Failed to serialize MatchRequest to JSON.");
            return;
        }
        
        Debug.Log("[Multiplayer] Final JSON to send:\n" + jsonData);

        SaveJsonRequest(jsonData);
        StartCoroutine(PostMatchData(jsonData));
        Debug.Log("[Multiplayer] PostMatchData coroutine has started.");

    });
}

private PlayerListManager.PlayerData ConvertFromJson(PlayerSelectionManager.PlayerJsonData json)
{
    return new PlayerListManager.PlayerData
    {
        name = json.name,
        position = json.position,
        rating = int.Parse(json.rating),
        skill = new PlayerListManager.Skill
        {
            passing = int.Parse(json.skill["passing"]),
            shooting = int.Parse(json.skill["shooting"]),
            tackling = int.Parse(json.skill["tackling"]),
            saving = int.Parse(json.skill["saving"]),
            agility = int.Parse(json.skill["agility"]),
            strength = int.Parse(json.skill["strength"]),
            penaltyTaking = int.Parse(json.skill["penalty_taking"]),
            jumping = int.Parse(json.skill["jumping"])
        },
        currentPOS = new Vector2(json.currentPOS[0], json.currentPOS[1]),
        fitness = json.fitness,
        injured = json.injured
    };
}


private void SaveJsonRequest(string json)
{
    try
    {
        string path = Path.Combine(Application.persistentDataPath, "LastMatchRequest.json");
        File.WriteAllText(path, json);
        Debug.Log($"Saved match request JSON at: {path}");
    }
    catch (Exception ex)
    {
        Debug.LogError("Failed to save match request JSON: " + ex.Message);
    }
}

private IEnumerator PostMatchData(string json)
{
    int maxRetries = 10;
    int retryCount = 0;
    bool success = false;

    if (loadingScreen != null)
        loadingScreen.SetActive(true);

    while (retryCount < maxRetries && !success)
    {
        Debug.Log($"Sending attempt {retryCount + 1}");

        UnityWebRequest request = new UnityWebRequest(serverUrl, "POST");
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = -1;

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            success = true;
            Debug.Log("Match simulation started successfully.");
            string responseText = request.downloadHandler.text;

            string filePath = Path.Combine(Application.persistentDataPath, "MatchSimulationResult.json");
            File.WriteAllText(filePath, responseText);
            Debug.Log($"Match simulation result saved to: {filePath}");

            if (PlayerSessionInfo.multiplayerIsTeamOne)
            {
                Debug.Log("[Host] Uploading match result JSON to Firebase Storage...");

                byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(responseText);
                Firebase.Storage.FirebaseStorage storage = Firebase.Storage.FirebaseStorage.DefaultInstance;
                Firebase.Storage.StorageReference storageRef = storage.GetReferenceFromUrl(firebaseStorageUrl);
                Firebase.Storage.StorageReference matchResultRef = storageRef.Child($"matchResults/{PlayerSessionInfo.lobbyId}_matchResult.json");

                var uploadTask = matchResultRef.PutBytesAsync(jsonBytes);
                uploadTask.ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompletedSuccessfully)
                    {
                        Debug.Log("Match result JSON successfully uploaded to Firebase Storage.");
                        UnityEngine.SceneManagement.SceneManager.LoadScene("SimScreen");
                    }
                    else
                    {
                        Debug.LogError("Failed to upload match result to Firebase Storage: " + task.Exception);
                    }
                });
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("SimScreen");
            }
        }
        else
        {
            Debug.LogError($"Failed to start match simulation: {request.error}. Retrying...");
            retryCount++;
            yield return new WaitForSeconds(1f);
        }

        request.Dispose();
    }

    if (!success)
    {
        Debug.LogError("All retry attempts failed. Could not start match simulation.");
        if (loadingScreen != null)
            loadingScreen.SetActive(false);
    }
}


private void UploadMatchResultToFirebase(string matchJson)
{
    string lobbyId = PlayerSessionInfo.lobbyId;
    if (string.IsNullOrEmpty(lobbyId))
    {
        Debug.LogError("Missing lobbyId when uploading match result.");
        return;
    }

    FirebaseApp app = FirebaseApp.DefaultInstance;
    var db = FirebaseDatabase.GetInstance(app, firebaseDatabaseUrl);

    db.RootReference
        .Child("lobbies").Child(lobbyId).Child("matchResult")
        .SetRawJsonValueAsync(matchJson)
        .ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log("Match result uploaded to Firebase.");
            }
            else
            {
                Debug.LogError("Failed to upload match result: " + task.Exception);
            }
        });
}



}

[System.Serializable]
public class MatchRequest
{
    public TeamData team1;
    public TeamData team2;
    public PitchDetails pitchDetails;

    public MatchRequest(Dictionary<int, PlayerListManager.PlayerData> teamOneDict, Dictionary<int, PlayerListManager.PlayerData> teamTwoDict)
    {
        team1 = new TeamData
        {
            name = "TeamOne",
            rating = CalculateTeamRating(teamOneDict.Values),
            players = ConvertToJsonData(BuildOrderedPlayerList(teamOneDict))
        };

        team2 = new TeamData
        {
            name = "TeamTwo",
            rating = CalculateTeamRating(teamTwoDict.Values),
            players = ConvertToJsonData(BuildOrderedPlayerList(teamTwoDict))
        };
        pitchDetails = new PitchDetails
        {
            pitchWidth = 680,
            pitchHeight = 1050,
            goalWidth = 90
        };
    }

    public static List<PlayerListManager.PlayerData> BuildOrderedPlayerList(Dictionary<int, PlayerListManager.PlayerData> teamDict)
{
    List<PlayerListManager.PlayerData> orderedList = new List<PlayerListManager.PlayerData>();

    for (int i = 0; i < 11; i++)
    {
        if (teamDict.ContainsKey(i))
        {
            orderedList.Add(teamDict[i]);
        }
        else
        {
            Debug.LogError($"Missing player at slot {i}!");
        }
    }

    return orderedList;
}


    private static int CalculateTeamRating(IEnumerable<PlayerListManager.PlayerData> players)
    {
        int totalRating = 0;
        int playerCount = 0;

        foreach (var player in players)
        {
            totalRating += CalculateAverageSkill(player.skill);
            playerCount++;
        }

        return playerCount > 0 ? Mathf.RoundToInt((float)totalRating / playerCount) : 0;
    }

    public static List<PlayerSelectionManager.PlayerJsonData> ConvertToJsonData(List<PlayerListManager.PlayerData> playerDataList)
    {
        List<PlayerSelectionManager.PlayerJsonData> jsonList = new List<PlayerSelectionManager.PlayerJsonData>();

        foreach (var player in playerDataList)
        {
            var skillDict = new Dictionary<string, string>
            {
                { "passing", player.skill.passing.ToString() },
                { "shooting", player.skill.shooting.ToString() },
                { "tackling", player.skill.tackling.ToString() },
                { "saving", player.skill.saving.ToString() },
                { "agility", player.skill.agility.ToString() },
                { "strength", player.skill.strength.ToString() },
                { "penalty_taking", player.skill.penaltyTaking.ToString() },
                { "jumping", player.skill.jumping.ToString() }
            };

            jsonList.Add(new PlayerSelectionManager.PlayerJsonData
            {
                name = player.name ?? (player.player?.firstname + " " + player.player?.lastname) ?? "Unknown",
                position = player.position,
                rating = CalculateAverageSkill(player.skill).ToString(),
                skill = skillDict,
                currentPOS = new int[] { Mathf.RoundToInt(player.currentPOS.x), Mathf.RoundToInt(player.currentPOS.y) },
                fitness = 99,
                injured = false
            });
        }

        return jsonList;
    }

    private static int CalculateAverageSkill(PlayerListManager.Skill skill)
    {
        int total = skill.passing +
                    skill.shooting +
                    skill.tackling +
                    skill.saving +
                    skill.agility +
                    skill.strength +
                    skill.penaltyTaking +
                    skill.jumping;

        return Mathf.RoundToInt((float)total / 8);
    }
}

[System.Serializable]
public class MatchDetails
{
    public long matchID;
    public TeamData kickOffTeam;
    public TeamData opponentTeam;
}

[System.Serializable]
public class PlayerJsonData
{
    public string name;
    public string position;
    public string rating;
    public Dictionary<string, string> skill;
    public int[] currentPOS;
    public float fitness;
    public bool injured;
    public PlayerStats stats;
}

[System.Serializable]
public class PlayerStats
{
    public int goals = 0;
    public ShotStats shots = new ShotStats();
    public CardStats cards = new CardStats();
    public PassStats passes = new PassStats();
    public TackleStats tackles = new TackleStats();
    public int saves = 0;
}

[System.Serializable]
public class ShotStats
{
    public int total = 0;
    public int on = 0;
    public int off = 0;
}

[System.Serializable]
public class CardStats
{
    public int yellow = 0;
    public int red = 0;
}

[System.Serializable]
public class PassStats
{
    public int total = 0;
    public int on = 0;
    public int off = 0;
}

[System.Serializable]
public class TackleStats
{
    public int total = 0;
    public int on = 0;
    public int off = 0;
    public int fouls = 0;
}

[System.Serializable]
public class PitchDetails
{
    public int pitchWidth;
    public int pitchHeight;
    public int goalWidth;
}
