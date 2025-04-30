using TMPro;
using UnityEngine;
using Firebase.Database;
using Firebase;
using Firebase.Extensions;
public class MultiplayerUIStatusController : MonoBehaviour
{
    public TMP_Text lobbyCodeText;
    public TMP_Text statusText;

    void Start()
    {
        if (!string.IsNullOrEmpty(PlayerSessionInfo.lobbyId))
        {
            lobbyCodeText.text = $"Lobby Code: {PlayerSessionInfo.lobbyId}";
        }
        else
        {
            lobbyCodeText.text = "Lobby Code: (unknown)";
        }

        if (!PlayerSessionInfo.multiplayerIsTeamOne)
        {
            statusText.text = "Joined host's lobby.";
        } else {
            statusText.text = "Waiting for another player to join...";
        }
        if (PlayerSessionInfo.multiplayerIsTeamOne)
        {
            ListenForLobbyUpdates(PlayerSessionInfo.lobbyId);
        }
    }

    void ListenForLobbyUpdates(string lobbyId)
    {
        Debug.Log("[MultiplayerUIStatusController] Listening for lobby updates...");
        if (string.IsNullOrEmpty(lobbyId))
        {
            Debug.LogError("Lobby ID is null or empty. Cannot listen for updates.");
            return;
        }

        FirebaseApp app = FirebaseApp.DefaultInstance;

        var db = FirebaseDatabase.GetInstance(app, "https://fantasy-kickoff-default-rtdb.europe-west1.firebasedatabase.app/");
        var guestRef = db.GetReference("lobbies").Child(lobbyId).Child("guest");

        guestRef.ValueChanged += (sender, args) =>
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError("Firebase error: " + args.DatabaseError.Message);
                return;
            }

            if (args.Snapshot.Exists && !string.IsNullOrEmpty(args.Snapshot.Value?.ToString()))
            {
                Debug.Log("A guest has joined the lobby!");

                // Update the UI on the main thread
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    statusText.text = "A player has joined your lobby!";
                });
            }
        };
    }

    public void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        else
        {
            Debug.LogError("Status text is not assigned in the inspector.");
        }
    }
}