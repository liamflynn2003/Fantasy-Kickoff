using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using Newtonsoft.Json;

public class MatchAnimator : MonoBehaviour
{
    [System.Serializable]
    public class MatchSimulationResult
    {
        public MatchDetails matchDetails;
        public int totalIterations;
        public PlayersOverIterations playersOverIterations;
    }

    [System.Serializable]
    public class PlayersOverIterations
    {
        public List<PlayerData> kickOffTeam;
        public List<PlayerData> secondTeam;
    }

    [System.Serializable]
    public class MatchDetails { }

    [System.Serializable]
    public class PlayerData
    {
        public string name;
        public string position;
        public List<PlayerPosition> positions;
    }

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

    public GameObject playerPrefab;
    public Transform playersParent;

    private MatchSimulationResult simulationResult;
    private List<GameObject> playerDots = new List<GameObject>();

    private float pitchWidth = 680f;
    private float pitchHeight = 1050f;

    private int currentIteration = 0;
    private int maxIterations = 2000;
    public float timeBetweenIterations = 0.05f; // Speed of animation

    void Start()
    {
        RectTransform pitchRect = playersParent.GetComponent<RectTransform>();
        pitchWidth = pitchRect.rect.width;
        pitchHeight = pitchRect.rect.height;

        LoadSimulationData();

        if (simulationResult != null)
        {
            SpawnPlayers();
            // StartCoroutine(PlayMatch());
        }
        else
        {
            Debug.LogError("Simulation data could not be loaded. Check if the JSON file exists and is valid.");
        }
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
        if (simulationResult == null || simulationResult.playersOverIterations == null || playerPrefab == null || playersParent == null)
        {
            Debug.LogError("Missing important references in MatchAnimator!");
            return;
        }

        // KickOffTeam
        foreach (var player in simulationResult.playersOverIterations.kickOffTeam)
        {
            GameObject dot = Instantiate(playerPrefab, playersParent);

            if (player.positions != null && player.positions.Count > 0)
            {
                var firstPos = player.positions[0].position;
                dot.GetComponent<RectTransform>().anchoredPosition = CenteredPosition(firstPos.x, firstPos.y);
            }
            else
            {
                dot.name = $"TeamOne_{player.name}_NO_POS";
            }

            Image circleImage = dot.GetComponentInChildren<Image>();
            if (circleImage != null)
            {
                circleImage.color = Color.red;
            }

            playerDots.Add(dot);
        }

        // SecondTeam
        foreach (var player in simulationResult.playersOverIterations.secondTeam)
        {
            GameObject dot = Instantiate(playerPrefab, playersParent);

            if (player.positions != null && player.positions.Count > 0)
            {
                var firstPos = player.positions[0].position;
                dot.GetComponent<RectTransform>().anchoredPosition = CenteredPosition(firstPos.x, firstPos.y);
            }
            else
            {
                dot.name = $"TeamTwo_{player.name}_NO_POS";
            }

            Image circleImage = dot.GetComponentInChildren<Image>();
            if (circleImage != null)
            {
                circleImage.color = Color.blue;
            }

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
        int kickoffIndex = 0;
        int secondTeamIndex = simulationResult.playersOverIterations.kickOffTeam.Count;

        // Kickoff team
        foreach (var player in simulationResult.playersOverIterations.kickOffTeam)
        {
            if (iteration < player.positions.Count)
            {
                var pos = player.positions[iteration].position;
                playerDots[kickoffIndex].GetComponent<RectTransform>().anchoredPosition = new Vector2(pos.x, pos.y);
            }
            kickoffIndex++;
        }

        // Second team
        foreach (var player in simulationResult.playersOverIterations.secondTeam)
        {
            if (iteration < player.positions.Count)
            {
                var pos = player.positions[iteration].position;
                playerDots[secondTeamIndex].GetComponent<RectTransform>().anchoredPosition = new Vector2(pos.x, pos.y);
            }
            secondTeamIndex++;
        }
    }

    private Vector2 CenteredPosition(float engineX, float engineY)
    {
        float centeredX = engineX - (pitchWidth / 2f);
        float centeredY = engineY - (pitchHeight / 2f);
        return new Vector2(centeredX, centeredY);
    }

}
