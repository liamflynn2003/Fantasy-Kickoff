using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using System.Threading.Tasks;

public class AuthManager : MonoBehaviour
{
    public GameObject loginSignupPanel;
    public GameObject loginForm;
    public GameObject signupForm;

    public TMP_InputField loginEmailInput;
    public TMP_InputField loginPasswordInput;
    public TMP_InputField signupEmailInput;
    public TMP_InputField signupPasswordInput;
    public GameObject loginButton;
    public GameObject signupButton;
    public GameObject backButton;
    public TextMeshProUGUI feedbackText;

    private FirebaseAuth auth;

    void Start()
    {
    FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
    {
        if (task.Result == DependencyStatus.Available)
        {
            auth = FirebaseAuth.DefaultInstance;
            Debug.Log("Firebase Auth initialized");

            auth.SignOut();
            PlayerPrefs.DeleteKey("userId");
        }
        else
        {
            Debug.LogError("Could not resolve Firebase dependencies: " + task.Result);
        }
    });
    }

    void OnApplicationQuit()
    {
        PlayerPrefs.DeleteKey("userId");
    }

    public void ShowLogin()
    {
        loginForm.SetActive(true);
        signupForm.SetActive(false);
        loginButton.SetActive(false);
        signupButton.SetActive(false);
        backButton.SetActive(true);
    }

    public void ShowSignup()
    {
        loginForm.SetActive(false);
        signupForm.SetActive(true);
        loginButton.SetActive(false);
        signupButton.SetActive(false);
        backButton.SetActive(true);
    }

    public void BackToMainMenu()
    {
        loginForm.SetActive(false);
        signupForm.SetActive(false);
        loginButton.SetActive(true);
        signupButton.SetActive(true);
        backButton.SetActive(false);
        feedbackText.text = " ";
        loginEmailInput.text = "";
        loginPasswordInput.text = "";
        signupEmailInput.text = "";
        signupPasswordInput.text = "";
    }
public async void Signup()
{
    string email = signupEmailInput.text;
    string password = signupPasswordInput.text;

    if (!IsValidEmail(email))
    {
        SetFeedbackText("Please enter a valid email address.");
        return;
    }

    if (!IsValidPassword(password))
    {
        SetFeedbackText("Password must be at least 8 characters and include at least one number.");
        return;
    }

    try
    {
        var userCredential = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
        Debug.Log("User created: " + userCredential.User.UserId);
        SetFeedbackText("Signup successful! Please log in.");
    }
    catch (FirebaseException ex)
    {
        var errorCode = (AuthError)ex.ErrorCode;
        switch (errorCode)
        {
            case AuthError.EmailAlreadyInUse:
                SetFeedbackText("That email is already in use.");
                break;
            case AuthError.WeakPassword:
                SetFeedbackText("Password is too weak.");
                break;
            default:
                SetFeedbackText("Signup failed: " + ex.Message);
                break;
        }

        Debug.LogWarning("Signup error: " + ex.Message);
    }
}


    public async void Login()
{
    string loginEmail = loginEmailInput.text;
    string loginPassword = loginPasswordInput.text;

    if (!IsValidEmail(loginEmail))
    {
        SetFeedbackText("Please enter a valid email address.");
        return;
    }

    if (string.IsNullOrEmpty(loginPassword))
    {
        SetFeedbackText("Please enter your password.");
        return;
    }

    try
    {
        var userCredential = await auth.SignInWithEmailAndPasswordAsync(loginEmail, loginPassword);
        
        SetFeedbackText("Login successful!");
        PlayerPrefs.SetString("userId", userCredential.User.UserId);
        Debug.Log("User logged in: " + userCredential.User.UserId);

        await AwaitSceneLoad(1);
    }
    catch (FirebaseException ex)
    {
        var errorCode = (AuthError)ex.ErrorCode;
        switch (errorCode)
        {
            case AuthError.InvalidEmail:
                SetFeedbackText("Invalid email address.");
                break;
            case AuthError.WrongPassword:
                SetFeedbackText("Incorrect password.");
                break;
            case AuthError.UserNotFound:
                SetFeedbackText("User not found.");
                break;
            default:
                SetFeedbackText("Login failed: " + ex.Message);
                break;
        }

        Debug.LogWarning("Login error: " + ex.Message);
    }
}

private async Task AwaitSceneLoad(int buildIndex)
{
    await Task.Yield();
    string scenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(buildIndex);
    string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
    Debug.Log("Switching to: " + sceneName);
    UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
}



    private void SetFeedbackText(string message)
    {
    if (UnityMainThreadDispatcher.Instance() != null)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            feedbackText.text = message;
        });
    }
    else
    {
        Debug.LogWarning("UnityMainThreadDispatcher is not initialized.");
    }
    }


    private bool IsValidPassword(string password)
    {
        if (password.Length < 8) return false;
        foreach (char c in password)
        {
            if (char.IsDigit(c)) return true;
        }
        return false;
    }

    private bool IsValidEmail(string email)
    {
        return email.Contains("@") && email.Contains(".");
    }
}
