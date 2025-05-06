using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using Firebase;
using Firebase.Database;
using Firebase.Auth;
using System.IO;
using Firebase.Storage;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class MultiplayerTeamUploader : MonoBehaviour
{
    public PlayerSelectionManager selectionManager;
    private DatabaseReference dbRef;
    public MultiplayerUIStatusController uiStatus;
    public MatchDataManager matchDataManager;
    public GameObject loadingScreen;

void Start()
{
    FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
    {
        if (task.Result == DependencyStatus.Available)
        {
            FirebaseApp app = FirebaseApp.DefaultInstance;
            dbRef = FirebaseDatabase.GetInstance(app, "https://fantasy-kickoff-default-rtdb.europe-west1.firebasedatabase.app/").RootReference;

            Debug.Log("[MultiplayerTeamUploader] Firebase initialized and DatabaseRef set.");
        }
        else
        {
            Debug.LogError("Firebase dependencies not available: " + task.Result);
        }
    });
}


    public void OnConfirmTeamButtonClicked()
    {
        if (!selectionManager.multiplayerMode)
        {
            Debug.LogError("Tried to confirm team in non-multiplayer mode.");
            return;
        }

        bool isTeamOne = PlayerSessionInfo.multiplayerIsTeamOne;
        if (!selectionManager.IsTeamValid(isTeamOne))
        {
            Debug.LogWarning("Team is not valid.");
            return;
        }

        var selectedTeam = selectionManager.GetSelectedTeam(isTeamOne);

// Ensure currentPOS is valid
foreach (var kvp in selectedTeam)
{
    int index = kvp.Key;
    if (selectedTeam[index] != null)
    {
        selectedTeam[index].currentPOS = selectionManager.CalculateStartPos(index, isTeamOne);
    }
}

var cleanedPlayers = MatchRequest.ConvertToJsonData(
    MatchRequest.BuildOrderedPlayerList(selectedTeam)
);

string teamJson = JsonConvert.SerializeObject(cleanedPlayers, Formatting.Indented);

        string lobbyId = PlayerSessionInfo.lobbyId;
        string teamKey = isTeamOne ? "teamOne" : "teamTwo";

        dbRef.Child("lobbies").Child(lobbyId).Child(teamKey)
            .SetRawJsonValueAsync(teamJson).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log("Team uploaded to Firebase.");
                uiStatus.SetStatus("Waiting for opponent's team...");
                WaitForOpponent(lobbyId);
            }
            else
            {
                Debug.LogError("Failed to upload team: " + task.Exception);
            }
        });
    }

private void WaitForOpponent(string lobbyId)
{
    Debug.Log("[MultiplayerTeamUploader] Waiting for both teams to be uploaded...");

    var lobbyRef = dbRef.Child("lobbies").Child(lobbyId);

    lobbyRef.ValueChanged += (sender, args) =>
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError("Error while listening for team data: " + args.DatabaseError.Message);
            return;
        }

        var snapshot = args.Snapshot;

        if (snapshot.HasChild("teamOne") && snapshot.HasChild("teamTwo"))
        {
            Debug.Log("[MultiplayerTeamUploader] Both teams detected!");

            if (PlayerSessionInfo.multiplayerIsTeamOne)
{
    Debug.Log("[MultiplayerTeamUploader] I am host. Triggering match simulation...");
    matchDataManager.SimulateMultiplayerMatchFromFirebase();
}
else
{
    Debug.Log("[MultiplayerTeamUploader] I am opponent. Waiting for matchResult in Firebase Storage...");
    if (loadingScreen != null) loadingScreen.SetActive(true);
    StartCoroutine(WaitForMatchResultFromFirebaseStorage());

}
        }
    };
}
private IEnumerator WaitForMatchResultFromFirebaseStorage()
{
    Firebase.Storage.FirebaseStorage storage = Firebase.Storage.FirebaseStorage.DefaultInstance;
    Firebase.Storage.StorageReference storageRef = storage.GetReferenceFromUrl("gs://fantasy-kickoff.firebasestorage.app");
    Firebase.Storage.StorageReference matchResultRef = storageRef.Child($"matchResults/{PlayerSessionInfo.lobbyId}_matchResult.json");

    int attempts = 0;

    while (true)
    {
        var urlTask = matchResultRef.GetDownloadUrlAsync();
        yield return new WaitUntil(() => urlTask.IsCompleted);

        if (urlTask.IsFaulted || urlTask.IsCanceled)
        {
            Debug.LogWarning($"[WaitForMatchResult] Attempt {attempts + 1}: Could not get download URL. Retrying...");
            attempts++;
            yield return new WaitForSeconds(1f);
            continue;
        }

        string url = urlTask.Result.ToString();
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string matchJson = request.downloadHandler.text;
            string filePath = Path.Combine(Application.persistentDataPath, "MatchSimulationResult.json");
            File.WriteAllText(filePath, matchJson);

            Debug.Log("[WaitForMatchResult] Match result downloaded and saved locally.");

            if (loadingScreen != null) loadingScreen.SetActive(false);
            UnityEngine.SceneManagement.SceneManager.LoadScene("SimScreen");
            yield break;
        }
        else
        {
            Debug.LogWarning($"[WaitForMatchResult] Attempt {attempts + 1}: Download failed: {request.error}. Retrying...");
            attempts++;
            yield return new WaitForSeconds(1f);
        }
    }
}

}
