using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionSlotClick : MonoBehaviour
{
    private void Start()
    {
        Transform parent = transform.parent;
        if (parent != null && parent.name == "TeamOnePanel")
        {
            isTeamOne = true;
        }
        else
        {
            isTeamOne = false;
        }
        string[] parts = gameObject.name.Split('_');
        if (parts.Length > 1 && int.TryParse(parts[parts.Length - 1], out int index))
        {
            positionIndex = index;
        }
        else
        {
            Debug.LogWarning($"Could not parse position index from object name: {gameObject.name}");
        }
    }

    public int positionIndex { get; private set; }
    public bool isTeamOne { get; private set; }

    public void OnClick()
    {
        PlayerSelectionContext context = FindObjectOfType<PlayerSelectionContext>();
        if (context != null)
        {
            context.SetContext(positionIndex, isTeamOne);
            Debug.Log($"Context set to index {positionIndex}, team: {(isTeamOne ? "Team 1" : "Team 2")}");
        }
    }
}

