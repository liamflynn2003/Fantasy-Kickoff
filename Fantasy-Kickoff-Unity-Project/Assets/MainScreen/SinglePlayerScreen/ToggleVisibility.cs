using UnityEngine;

public class ToggleVisibility : MonoBehaviour
{
    public GameObject playerScrollView;

    public void TogglePlayerScrollViewVisibility()
    {
        Debug.Log("Button clicked!");
        if (playerScrollView != null)
        {
            playerScrollView.SetActive(!playerScrollView.activeSelf);
            Debug.Log("PlayerScrollView visibility toggled. Current state: " + playerScrollView.activeSelf);
        }
        else
        {
            Debug.LogWarning("PlayerScrollView is not assigned.");
        }
    }
}
