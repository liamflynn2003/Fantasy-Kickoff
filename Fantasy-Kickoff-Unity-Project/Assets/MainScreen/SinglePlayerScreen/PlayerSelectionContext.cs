using UnityEngine;
public class PlayerSelectionContext : MonoBehaviour
{
    private static PlayerSelectionContext _instance;

    public static PlayerSelectionContext Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PlayerSelectionContext>();
            }
            return _instance;
        }
    }

    public int currentPositionIndex = -1;
    public bool isTeamOne = true;

    public void SetContext(int index, bool teamOne)
    {
        currentPositionIndex = index;
        isTeamOne = teamOne;
        Debug.Log($"Context set: PositionIndex = {currentPositionIndex}, IsTeamOne = {isTeamOne}");
    }
}
