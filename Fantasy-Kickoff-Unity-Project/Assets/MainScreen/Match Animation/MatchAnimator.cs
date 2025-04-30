using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using TMPro;
using Newtonsoft.Json;

public class MatchAnimator : MonoBehaviour
{
    [System.Serializable]
    public class MatchSimulationResult
    {
        public MatchDetails matchDetails;
        public int totalIterations;
        public PlayersOverIterations playersOverIterations;
        public List<ScoreTimelineEntry> scoreTimeline;
    }

    [System.Serializable]
    public class PlayersOverIterations
    {
        public List<PlayerData> kickOffTeam;
        public List<PlayerData> secondTeam;
    }

    [System.Serializable]
    public class ScoreTimelineEntry
    {
        public int iteration;
        public int team1Goals;
        public int team2Goals;
    }

    [System.Serializable]
    public class MatchDetails
    {
        public BallDetails ball;
    }

    [System.Serializable]
    public class BallDetails
    {
        public List<BallPosition> ballOverIterationsHistory;
    }

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

    [System.Serializable]
    public class BallPosition
    {
        public int iteration;
        public List<float> position;
    }

    public GameObject playerPrefab;
    public GameObject ballPrefab;
    public Transform playersParent;

    public TMP_Text kickoffTeamScoreText;
    public TMP_Text secondTeamScoreText;

    private MatchSimulationResult simulationResult;
    private List<AnimatedPlayer> animatedPlayers = new List<AnimatedPlayer>();
    private AnimatedBall animatedBall;

    private float pitchWidth = 680f;
    private float pitchHeight = 1050f;

    private int currentIteration = 0;
    private int maxIterations = 10000;
    public float timeBetweenIterations = 0.00005f;

    private int currentKickoffTeamScore = 0;
    private int currentSecondTeamScore = 0;
    private int scoreHistoryIndex = 0;

    void Start()
    {
        RectTransform pitchRect = playersParent.GetComponent<RectTransform>();
        pitchWidth = pitchRect.rect.width;
        pitchHeight = pitchRect.rect.height;

        LoadSimulationData();

        if (simulationResult != null)
        {
            kickoffTeamScoreText.text = "0";
            secondTeamScoreText.text = "0";
            SpawnPlayersAndBall();
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

    void SpawnPlayersAndBall()
    {
        if (simulationResult == null || simulationResult.playersOverIterations == null)
        {
            Debug.LogError("Simulation result or players missing!");
            return;
        }

// Kickoff team (red)
foreach (var player in simulationResult.playersOverIterations.kickOffTeam)
{
    GameObject dot = Instantiate(playerPrefab, playersParent);
    var firstPos = player.positions[0].position;
    dot.GetComponent<RectTransform>().anchoredPosition = CenteredPosition(firstPos.x, firstPos.y);
    
    Image circleImage = dot.GetComponentInChildren<Image>();
    if (circleImage != null)
    {
        circleImage.color = Color.red;
    }

    animatedPlayers.Add(new AnimatedPlayer(dot, player.positions));
}

// Second team (blue)
foreach (var player in simulationResult.playersOverIterations.secondTeam)
{
    GameObject dot = Instantiate(playerPrefab, playersParent);
    var firstPos = player.positions[0].position;
    dot.GetComponent<RectTransform>().anchoredPosition = CenteredPosition(firstPos.x, firstPos.y);
    
    Image circleImage = dot.GetComponentInChildren<Image>();
    if (circleImage != null)
    {
        circleImage.color = Color.blue;
    }

    animatedPlayers.Add(new AnimatedPlayer(dot, player.positions));
}


        if (simulationResult.matchDetails.ball.ballOverIterationsHistory != null)
        {
            GameObject ballDot = Instantiate(ballPrefab, playersParent);
            var firstBallPos = simulationResult.matchDetails.ball.ballOverIterationsHistory[0].position;
            ballDot.GetComponent<RectTransform>().anchoredPosition = CenteredPosition(firstBallPos[0], firstBallPos[1]);
            animatedBall = new AnimatedBall(ballDot, simulationResult.matchDetails.ball.ballOverIterationsHistory);
        }

        StartCoroutine(PlayMatch());
    }

    IEnumerator PlayMatch()
    {
        while (currentIteration < maxIterations)
        {
            if (currentIteration % 100 == 0)
            {
                Debug.Log($"Animating iteration {currentIteration}");
            }
            UpdatePlayerPositions(currentIteration);
            UpdateBallPosition(currentIteration);
            UpdateScore(currentIteration);

            currentIteration++;
            yield return new WaitForSeconds(timeBetweenIterations);
        }
    }

    void UpdatePlayerPositions(int iteration)
    {
        foreach (var player in animatedPlayers)
        {
            if (player.currentIndex < player.positions.Count)
            {
                var playerPos = player.positions[player.currentIndex];
                if (playerPos.iteration == iteration)
                {
                    player.lastPosition = player.targetPosition;
                    player.targetPosition = new Vector2(playerPos.position.x, playerPos.position.y);
                    player.lerpProgress = 0f;
                    player.currentIndex++;
                }

                player.lerpProgress += Time.deltaTime / timeBetweenIterations;
                player.lerpProgress = Mathf.Clamp01(player.lerpProgress);

                Vector2 interpolatedPos = Vector2.Lerp(player.lastPosition, player.targetPosition, player.lerpProgress);
                player.dot.GetComponent<RectTransform>().anchoredPosition = CenteredPosition(interpolatedPos.x, interpolatedPos.y);
            }
        }
    }

    void UpdateBallPosition(int iteration)
    {
        if (animatedBall != null && animatedBall.currentIndex < animatedBall.positions.Count)
        {
            var ballPos = animatedBall.positions[animatedBall.currentIndex];
            if (ballPos.iteration == iteration)
            {
                animatedBall.lastPosition = animatedBall.targetPosition;
                animatedBall.targetPosition = new Vector2(ballPos.position[0], ballPos.position[1]);
                animatedBall.lerpProgress = 0f;
                animatedBall.currentIndex++;
            }

            animatedBall.lerpProgress += Time.deltaTime / timeBetweenIterations;
            animatedBall.lerpProgress = Mathf.Clamp01(animatedBall.lerpProgress);

            Vector2 interpolatedBallPos = Vector2.Lerp(animatedBall.lastPosition, animatedBall.targetPosition, animatedBall.lerpProgress);
            animatedBall.ballObject.GetComponent<RectTransform>().anchoredPosition = CenteredPosition(interpolatedBallPos.x, interpolatedBallPos.y);
        }
    }

    void UpdateScore(int iteration)
{
    if (simulationResult.scoreTimeline == null || scoreHistoryIndex >= simulationResult.scoreTimeline.Count)
        return;

    var nextScore = simulationResult.scoreTimeline[scoreHistoryIndex];

    if (nextScore.iteration == iteration)
    {
        kickoffTeamScoreText.text = nextScore.team1Goals.ToString();
        secondTeamScoreText.text = nextScore.team2Goals.ToString();

        currentKickoffTeamScore = nextScore.team1Goals;
        currentSecondTeamScore = nextScore.team2Goals;

        Debug.Log($"[Score Update] Iteration {iteration}: Team1 {currentKickoffTeamScore} - Team2 {currentSecondTeamScore}");

        scoreHistoryIndex++;
    }
}

    private Vector2 CenteredPosition(float engineX, float engineY)
    {
        float centeredX = engineX - (pitchWidth / 2f);
        float centeredY = engineY - (pitchHeight / 2f);
        return new Vector2(centeredX, centeredY);
    }

    private class AnimatedPlayer
    {
        public GameObject dot;
        public List<PlayerPosition> positions;
        public int currentIndex = 0;
        public Vector2 lastPosition;
        public Vector2 targetPosition;
        public float lerpProgress = 0f;

        public AnimatedPlayer(GameObject dotObject, List<PlayerPosition> posList)
        {
            dot = dotObject;
            positions = posList;
            if (positions.Count > 0)
            {
                lastPosition = new Vector2(positions[0].position.x, positions[0].position.y);
                targetPosition = lastPosition;
            }
        }
    }

    private class AnimatedBall
    {
        public GameObject ballObject;
        public List<BallPosition> positions;
        public int currentIndex = 0;
        public Vector2 lastPosition;
        public Vector2 targetPosition;
        public float lerpProgress = 0f;

        public AnimatedBall(GameObject obj, List<BallPosition> posList)
        {
            ballObject = obj;
            positions = posList;
            if (positions.Count > 0)
            {
                lastPosition = new Vector2(posList[0].position[0], posList[0].position[1]);
                targetPosition = lastPosition;
            }
        }
    }
}
