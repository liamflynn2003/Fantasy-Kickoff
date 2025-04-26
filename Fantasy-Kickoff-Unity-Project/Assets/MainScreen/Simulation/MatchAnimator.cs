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
        SpawnPlayers();
        StartCoroutine(PlayMatch());
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
        foreach (var player in simulationResult.kickOffTeam)
        {
            if (iteration < player.positions.Count)
            {
                var pos = player.positions[iteration].position;
                playerDots[index].transform.localPosition = new Vector3(pos.x, pos.y, 0);
            }
            index++;
        }

        foreach (var player in simulationResult.secondTeam)
        {
            if (iteration < player.positions.Count)
            {
                var pos = player.positions[iteration].position;
                playerDots[index].transform.localPosition = new Vector3(pos.x, pos.y, 0);
            }
            index++;
        }
    }
}
