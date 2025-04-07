using UnityEngine;
using TMPro;

public class PlayerItemButton : MonoBehaviour
{
    public int positionIndex;
    public bool isTeamOne;
    private PlayerData playerData;
    private PlayerSelectionManager selectionManager;

    void Start()
    {
        selectionManager = FindObjectOfType<PlayerSelectionManager>();
    }

    public void SetPlayerData(PlayerData data)
    {
        playerData = data;
    }

    public void OnClick()
    {
        GameObject dbScreen = GameObject.Find("DatabaseScreen");
        if (dbScreen != null && dbScreen.activeSelf)
        {
            Debug.Log("DatabaseScreen is active, skipping selection.");
            return;
        }

        if (selectionManager != null && playerData != null)
        {
            selectionManager.AssignPlayer(positionIndex, playerData, isTeamOne);
            Debug.Log($"Assigned {playerData.name} to {(isTeamOne ? "Team 1" : "Team 2")} at position {positionIndex}");
        }
    }
}
