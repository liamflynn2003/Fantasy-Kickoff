using UnityEngine;
using Firebase.Database;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Extensions;

public class MultiplayerLobbyExitHandler : MonoBehaviour
{
    // Name of the scene to return to when exiting the lobby
    public string returnSceneName = "MainScreen";

    // Called when the back button is pressed
    public void OnBackButtonPressed()
    {
        // Retrieve the current lobby ID and whether the user is the host
        string lobbyId = PlayerSessionInfo.lobbyId;
        bool isHost = PlayerSessionInfo.multiplayerIsTeamOne;

        // If no lobby ID is found, return to the main menu
        if (string.IsNullOrEmpty(lobbyId))
        {
            Debug.LogWarning("No lobby ID found. Returning to menu.");
            SceneManager.LoadScene(returnSceneName);
            return;
        }

        // Get the Firebase app instance
        FirebaseApp app = FirebaseApp.DefaultInstance;

        // Reference the lobby in the Firebase Realtime Database
        var lobbyRef = FirebaseDatabase.GetInstance(app, "https://fantasy-kickoff-default-rtdb.europe-west1.firebasedatabase.app/")
            .RootReference.Child("lobbies").Child(lobbyId);

        // If the user is the host, delete the entire lobby
        if (isHost)
        {
            lobbyRef.RemoveValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    Debug.Log("Lobby deleted by host.");
                }
                else
                {
                    Debug.LogError("Failed to delete lobby: " + task.Exception);
                }

                // Return to the main menu
                SceneManager.LoadScene(returnSceneName);
            });
        }
        // If the user is a guest, remove only the guest entry from the lobby
        else
        {
            lobbyRef.Child("guest").RemoveValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    Debug.Log("Guest left the lobby.");
                }
                else
                {
                    Debug.LogError("Failed to leave lobby: " + task.Exception);
                }

                // Return to the main menu
                SceneManager.LoadScene(returnSceneName);
            });
        }
    }
}