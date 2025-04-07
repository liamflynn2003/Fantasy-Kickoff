using UnityEngine;

public class PlayerSelectionContext : MonoBehaviour
{
    public int currentPositionIndex = -1;
    public bool isTeamOne = true;

    public void SetContext(int index, bool teamOne)
    {
        currentPositionIndex = index;
        isTeamOne = teamOne;
    }
}
