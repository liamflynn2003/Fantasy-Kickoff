using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using Newtonsoft.Json;

public class MatchAnimator : MonoBehaviour
{
    [System.Serializable]
    public class PlayerPosition
    {
        public int iteration;
        public PositionData position;
    }

    [System.Serializable]
    public class PositionData
    {
        public float x;
        public float y;
    }

    [System.Serializable]
    public class PlayerData
    {
        public string name;
        public List<PlayerPosition> positions;
    }

    [System.Serializable]
    public class MatchSimulationResult
    {
        public List<PlayerData> kickOffTeam;
        public List<PlayerData> secondTeam;
    }

    public GameObject playerPrefab;
    public Transform playersParent;

    private MatchSimulationResult simulationResult;
    private List<GameObject> playerDots = new List<GameObject>();
    private int currentIteration = 0;
    private int maxIterations = 2000;

    public float timeBetweenIterations = 0.05f; // Speed of animation

    void Start()
{
    LoadSimulationData();

    if (simulationResult != null)
    {
        SpawnPlayers();
        StartCoroutine(PlayMatch());
    }
    else
    {
        Debug.LogError("Simulation data could not be loaded. Check if the JSON file exists and is valid.");
    }
}


    Vector2 ConvertToPitchPosition(Vector2 enginePos, bool isSecondTeam)
    {
        float engineMaxX = 500f;
        float engineMaxY = 620f;

        RectTransform pitchRect = playersParent.GetComponent<RectTransform>();
        float pitchWidth = pitchRect.rect.width;
        float pitchHeight = pitchRect.rect.height;

        // Normalize engine pos to 0–pitchWidth and 0–pitchHeight
        float normalizedX = (enginePos.x / engineMaxX) * pitchWidth;
        float normalizedY = (enginePos.y / engineMaxY) * pitchHeight;

        // Center
        normalizedX -= pitchWidth / 2f;
        normalizedY -= pitchHeight / 2f;

        // Flip second team
        if (isSecondTeam)
        {
            normalizedX = -normalizedX;
            normalizedY = -normalizedY;
        }

        return new Vector2(normalizedX, normalizedY);
    }


    void LoadSimulationData()
    {
        string path = Path.Combine(Application.persistentDataPath, "MatchSimulationResult.json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            simulationResult = JsonConvert.DeserializeObject<MatchSimulationResult>(json);
        }
        else
        {
            Debug.LogError("No simulation result found!");
        }
    }

    void SpawnPlayers()
    {
        foreach (var player in simulationResult.kickOffTeam)
        {
            GameObject dot = Instantiate(playerPrefab, playersParent);
            dot.GetComponent<SpriteRenderer>().color = Color.red; // KickOffTeam = Red
            playerDots.Add(dot);
        }

        foreach (var player in simulationResult.secondTeam)
        {
            GameObject dot = Instantiate(playerPrefab, playersParent);
            dot.GetComponent<SpriteRenderer>().color = Color.blue; // SecondTeam = Blue
            playerDots.Add(dot);
        }
    }

    IEnumerator PlayMatch()
    {
        while (currentIteration < maxIterations)
        {
            UpdatePlayerPositions(currentIteration);
            currentIteration++;
            yield return new WaitForSeconds(timeBetweenIterations);
        }
    }

    void UpdatePlayerPositions(int iteration)
{
    int index = 0;

    // Kickoff team
    foreach (var player in simulationResult.kickOffTeam)
    {
        if (iteration < player.positions.Count)
        {
            var pos = player.positions[iteration].position;
            Vector2 newPos = ConvertToPitchPosition(new Vector2(pos.x, pos.y), false); // false = not second team
            playerDots[index].transform.localPosition = new Vector3(newPos.x, newPos.y, 0);
        }
        index++;
    }

    // Second team
    foreach (var player in simulationResult.secondTeam)
    {
        if (iteration < player.positions.Count)
        {
            var pos = player.positions[iteration].position;
            Vector2 newPos = ConvertToPitchPosition(new Vector2(pos.x, pos.y), true); // true = second team
            playerDots[index].transform.localPosition = new Vector3(newPos.x, newPos.y, 0);
        }
        index++;
    }
}

}
