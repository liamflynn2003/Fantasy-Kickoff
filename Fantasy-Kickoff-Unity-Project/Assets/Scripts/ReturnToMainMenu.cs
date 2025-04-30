using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToMainMenu : MonoBehaviour
{
    public void OnReturnButtonClicked()
    {
        SceneManager.LoadScene("MainScreen");
    }
}
