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

    // Store all the player positions for each iteration
    let allIterations = [];

    // Simulate the match
    while (iterationCount < maxIterations && !matchDetails.endIteration) {
      // Log player positions for this iteration
      let playerPositions = matchDetails.kickOffTeam.players.map(player => ({
        name: player.name,
        position: player.currentPOS,
        team: matchDetails.kickOffTeam.name,
      }));

      playerPositions = playerPositions.concat(
        matchDetails.secondTeam.players.map(player => ({
          name: player.name,
          position: player.currentPOS,
          team: matchDetails.secondTeam.name,
        }))
      );

      // Add the iteration data
      allIterations.push({
        iteration: iterationCount + 1,
        playerPositions,
        iterationLog: matchDetails.iterationLog,
      });

      matchDetails = await engine.playIteration(matchDetails);
      iterationCount++;

      // Log every iteration
      console.log(`Iteration ${iterationCount}:`);
      console.log(matchDetails.iterationLog);

      // Check if halftime
      if (matchDetails.half === 1) {
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
      allIterations: allIterations, // Return all the iteration logs and player positions
      score: score, // Include the final score
    });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Failed to simulate the game' });
  }
});

app.listen(port, () => {
  console.log(`Server running on http://localhost:${port}`);
});
