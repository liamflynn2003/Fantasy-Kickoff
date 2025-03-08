const express = require('express');
const bodyParser = require('body-parser');
const engine = require('../football-simulation-engine/engine');

const app = express();
const port = 3000;

app.use(bodyParser.json());

// Simulate full match
app.post('/simulate', async (req, res) => {
  try {
    const { team1, team2, pitchDetails } = req.body;

    // Initialize the game (first half)
    let matchDetails = await engine.initiateGame(team1, team2, pitchDetails);
    
    let iterationCount = 0;
    let maxIterations = 50;

    // Initialize playerOverIterations
    let playerOverIterations = {
      kickOffTeam: team1.players.map(player => ({ id: player.id, name: player.name, positions: [] })),
      secondTeam: team2.players.map(player => ({ id: player.id, name: player.name, positions: [] }))
    };

    // Store all the player positions for each iteration
    let allIterations = [];

    // Simulate the match
    while (iterationCount < maxIterations && !matchDetails.endIteration) {
      // Play an iteration and get start and end positions
      let iterationResult = await engine.playIteration(matchDetails, playerOverIterations, iterationCount + 1);
      matchDetails = iterationResult.matchDetails;

      // Add the iteration data
      allIterations.push({
        iteration: iterationCount + 1,
        startPositions: iterationResult.startPositions,
        endPositions: iterationResult.endPositions,
        iterationLog: matchDetails.iterationLog,
      });

      iterationCount++;

      // Log every iteration
      console.log(`Iteration ${iterationCount}:`);
      console.log(matchDetails.iterationLog);

      // Check if halftime
      if (matchDetails.half === 1 && iterationCount < maxIterations) {
        matchDetails = await engine.startSecondHalf(matchDetails);
      }
    }

    // Get the current score
    const score = {
      team1Goals: matchDetails.kickOffTeamStatistics.goals,
      team2Goals: matchDetails.secondTeamStatistics.goals,
    };

    // Send back the final result with player positions for each iteration and the score
    res.json({
      matchDetails: matchDetails,
      totalIterations: iterationCount,
      playerOverIterations: playerOverIterations, // Include player positions over iterations
      score: score, // Include the final score
    });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Failed to simulate the game' });
  }
});

app.listen(port, '0.0.0.0', () => {
  console.log(`Server running on http://0.0.0.0:${port}`);
});