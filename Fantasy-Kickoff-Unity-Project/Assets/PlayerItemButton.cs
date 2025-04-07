using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerItemButton : MonoBehaviour
{
    private PlayerData playerData;
    private int positionIndex;
    private bool isTeamOne;

    // Reference to the PlayerSelectionManager
    private PlayerSelectionManager selectionManager;

    private void Start()
    {
        // Get the PlayerSelectionManager instance
        selectionManager = FindObjectOfType<PlayerSelectionManager>();
        
        // Get PlayerData from the PlayerItem
        playerData = GetComponent<PlayerDataComponent>().playerData;

        // Add onClick listener to button
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }
        else
        {
            Debug.LogWarning("Button component not found on player item!");
        }
    }

    // This will be called when a player item is clicked
    public void OnClick()
    {
        if (selectionManager != null)
        {
            // Assign the player to the correct team and position
            selectionManager.AssignPlayer(positionIndex, playerData, isTeamOne);

            Debug.Log($"Assigned player {playerData.name} to position {positionIndex} on {(isTeamOne ? "Team 1" : "Team 2")}");
        }
        else
        {
            Debug.LogWarning("PlayerSelectionManager not found.");
        }
    }

    // Call this function when the position context is set
    public void SetContext(int position, bool teamOne)
    {
        positionIndex = position;
        isTeamOne = teamOne;
    }
}
