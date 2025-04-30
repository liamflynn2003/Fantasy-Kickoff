using System.Linq;
using Firebase;
using Firebase.Database;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    public TMP_InputField lobbyCodeInput; // Input field for entering the lobby code
    public TMP_Text lobbyStatusText; // Text to display the status of lobby actions

    public UnityEngine.UI.Button createLobbyButton; // Button to create a new lobby
    public UnityEngine.UI.Button joinLobbyButton; // Button to join an existing lobby

    private DatabaseReference dbRef; // Reference to the Firebase Realtime Database
    private string userId; // The ID of the current user

    void Start()
    {
        // Initialize buttons as non-interactable until Firebase is ready
        createLobbyButton.interactable = false;
        joinLobbyButton.interactable = false;

        // Check and fix Firebase dependencies
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                // Handle dependency errors
                return;
            }

            if (task.Result == DependencyStatus.Available)
            {
                // Initialize Firebase app and database reference
                FirebaseApp app = FirebaseApp.DefaultInstance;
                dbRef = FirebaseDatabase.GetInstance(app, "https://fantasy-kickoff-default-rtdb.europe-west1.firebasedatabase.app/").RootReference;

                // Retrieve the user ID from FirebaseAuth or PlayerPrefs
                if (FirebaseAuth.DefaultInstance.CurrentUser != null)
                {
                    userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
                }
                else if (PlayerPrefs.HasKey("userId"))
                {
                    userId = PlayerPrefs.GetString("userId");
                }
                else
                {
                    return;
                }

                // Enable the buttons once Firebase is ready
                createLobbyButton.interactable = true;
                joinLobbyButton.interactable = true;

                Debug.Log("Firebase ready");
            }
            else
            {
                Debug.LogError($"Firebase dependencies not available: {task.Result}");
            }
        });
    }

    // Function to create a new lobby
    public void CreateLobby()
    {
        if (dbRef == null || string.IsNullOrEmpty(userId))
        {
            Debug.LogError("Database reference or userId is not ready.");
            return;
        }

        // Generate a unique lobby code
        string lobbyId = GenerateLobbyCode();
        dbRef.Child("lobbies").Child(lobbyId).Child("host").SetValueAsync(userId).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                Debug.Log("Lobby created with ID: " + lobbyId);
                lobbyStatusText.text = "Lobby created! ID: " + lobbyId;

                // Set session info and navigate to the multiplayer selection screen
                PlayerSessionInfo.multiplayerIsTeamOne = true;
                PlayerSessionInfo.lobbyId = lobbyId;

                PlayerPrefs.SetString("lobbyId", lobbyId);
                UnityEngine.SceneManagement.SceneManager.LoadScene("MultiplayerSelectionScreen");
            }
            else
            {
                Debug.LogError("Failed to create lobby: " + task.Exception);
            }
        });
    }

    // Function to join an existing lobby
    public void JoinLobby()
    {
        if (dbRef == null || string.IsNullOrEmpty(userId))
        {
            Debug.LogError("Database reference or userId is not ready.");
            return;
        }

        // Get the lobby ID from the input field
        string lobbyId = lobbyCodeInput.text;
        if (string.IsNullOrEmpty(lobbyId))
        {
            Debug.LogError("Lobby ID is empty.");
            lobbyStatusText.text = "Please enter a valid lobby ID.";
            return;
        }

        // Check if the lobby exists in the database
        dbRef.Child("lobbies").Child(lobbyId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                // Add the current user as a guest to the lobby
                dbRef.Child("lobbies").Child(lobbyId).Child("guest").SetValueAsync(userId).ContinueWithOnMainThread(joinTask =>
                {
                    if (joinTask.IsCompleted && !joinTask.IsFaulted)
                    {
                        Debug.Log("Joined lobby: " + lobbyId);
                        lobbyStatusText.text = "Joined lobby!";

                        // Set session info and navigate to the multiplayer selection screen
                        PlayerSessionInfo.multiplayerIsTeamOne = false;
                        PlayerSessionInfo.lobbyId = lobbyId;

                        PlayerPrefs.SetString("lobbyId", lobbyId);
                        UnityEngine.SceneManagement.SceneManager.LoadScene("MultiplayerSelectionScreen");
                    }
                    else
                    {
                        Debug.LogError("Failed to join lobby: " + joinTask.Exception);
                        lobbyStatusText.text = "Failed to join lobby. Please try again.";
                    }
                });
            }
            else
            {
                Debug.LogWarning("Lobby not found: " + lobbyId);
                lobbyStatusText.text = "Lobby does not exist.";
            }
        });
    }

    // Function to generate a random lobby code
    private string GenerateLobbyCode(int length = 6)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        System.Random random = new System.Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}