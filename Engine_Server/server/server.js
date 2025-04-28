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
    matchDetails.ball.ballOverIterationsHistory = [];
    let iterationCount = 0;
    let maxIterations = 5000;

    // Initialize playerOverIterations
    let playersOverIterations = {
      kickOffTeam: team1.players.map(player => ({ id: player.id, name: player.name, positions: [] })),
      secondTeam: team2.players.map(player => ({ id: player.id, name: player.name, positions: [] }))
    };

    // Simulate the match
    while (iterationCount < maxIterations) {
      if (!matchDetails || !matchDetails.pitchSize) {
        console.error('Invalid matchDetails detected, stopping simulation');
        break;
      }

      // Play an iteration and get start and end positions
      matchDetails.currentIteration = iterationCount + 1;
      let iterationResult = await engine.playIteration(matchDetails, playersOverIterations, iterationCount + 1);
      if (!iterationResult || !iterationResult.matchDetails) {
        console.warn(`Skipping iteration ${iterationCount + 1} due to invalid result.`);
        break;
      }
      matchDetails = iterationResult.matchDetails;

      // Add the iteration data
      if (!iterationResult?.startPositions?.players) {
        console.warn(`Iteration ${iterationCount + 1} missing player positions — likely due to a special state.`);
      }
  
      iterationCount++;

      // Switch to the second half
      if (iterationCount === 2500 && matchDetails.half === 1) {
        matchDetails = await engine.startSecondHalf(matchDetails);
      }
    }

    // Get the current score
    const score = {
      team1Goals: matchDetails.kickOffTeamStatistics.goals,
      team2Goals: matchDetails.secondTeamStatistics.goals,
    };

    // Send back the final result with player positions for each iteration, the score, and iteration logs
    res.json({
      matchDetails: matchDetails,
      playersOverIterations: playersOverIterations, // Include player positions over iterations
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