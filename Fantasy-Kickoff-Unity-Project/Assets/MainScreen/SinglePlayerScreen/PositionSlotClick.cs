using UnityEngine;

public class PositionSlotClick : MonoBehaviour
{
    public int positionIndex { get; private set; }
    public bool isTeamOne { get; private set; }

    // This function can be called from any other script to set the context for the position
    public void SetPositionSlotContext()
    {
        // Determine if this slot belongs to Team 1 or Team 2 based on its parent
        Transform parent = transform.parent.parent;
        if (parent != null)
        {
            Debug.Log($"Parent name: {parent.name}");
        }
        else
        {
            Debug.LogWarning("No parent found for this object.");
        }
        if (parent != null && parent.name == "TeamOnePanel")
        {
            isTeamOne = true;
        }
        else
        {
            isTeamOne = false;
        }

        // Extract position index from the object's name
        string[] parts = transform.parent.gameObject.name.Split('_');
        if (parts.Length > 1 && int.TryParse(parts[parts.Length - 1], out int index))
        {
            positionIndex = index;
        }
        else
        {
            Debug.LogWarning($"Could not parse position index from object name: {gameObject.name}");
        }

        // Find the PlayerSelectionContext and set the context for this position
        PlayerSelectionContext context = PlayerSelectionContext.Instance;
if (context != null)
{
    context.SetContext(positionIndex, isTeamOne);
    Debug.Log($"Context set to index {positionIndex}, team: {(isTeamOne ? "Team 1" : "Team 2")}");
}
else
{
    Debug.LogWarning("PlayerSelectionContext instance not found.");
}
    }
}
