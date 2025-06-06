//------------------------
//    NPM Modules
//------------------------
const common = require('./lib/common')
const setPositions = require('./lib/setPositions')
const setVariables = require('./lib/setVariables')
const playerMovement = require('./lib/playerMovement')
const ballMovement = require('./lib/ballMovement')
const validate = require('./lib/validate')

//------------------------
//    Functions
//------------------------
async function initiateGame(team1, team2, pitchDetails) {
  validate.validateArguments(team1, team2, pitchDetails)
  validate.validateTeam(team1)
  validate.validateTeam(team2)
  validate.validatePitch(pitchDetails)
  let matchDetails = setVariables.populateMatchDetails(team1, team2, pitchDetails)
  let kickOffTeam = setVariables.setGameVariables(matchDetails.kickOffTeam)
  let secondTeam = setVariables.setGameVariables(matchDetails.secondTeam)
  kickOffTeam = setVariables.koDecider(kickOffTeam, matchDetails)
  matchDetails.iterationLog.push(`Team to kick off - ${kickOffTeam.name}`)
  matchDetails.iterationLog.push(`Second team - ${secondTeam.name}`)
  setPositions.switchSide(matchDetails, secondTeam)
  matchDetails.kickOffTeam = kickOffTeam
  matchDetails.secondTeam = secondTeam
  return matchDetails
}

async function playIteration(matchDetails, playersOverIterations, iterationCount) {
  let closestPlayerA = { 'name': '', 'position': 100000 }
  let closestPlayerB = { 'name': '', 'position': 100000 }

  validate.validateMatchDetails(matchDetails)
  validate.validateTeamSecondHalf(matchDetails.kickOffTeam)
  validate.validateTeamSecondHalf(matchDetails.secondTeam)
  validate.validatePlayerPositions(matchDetails)

  matchDetails.iterationLog = []

  let { kickOffTeam, secondTeam } = matchDetails
  common.matchInjury(matchDetails, kickOffTeam)
  common.matchInjury(matchDetails, secondTeam)

  matchDetails = ballMovement.moveBall(matchDetails)

  // Capture start positions
  let startPositions = {
    ball: Object.assign({}, matchDetails.ball.position),
    players: {
      kickOffTeam: kickOffTeam.players.map(player => ({
        id: player.id,
        name: player.name,
        iteration: iterationCount
      })),
      secondTeam: secondTeam.players.map(player => ({
        id: player.id,
        name: player.name,
        iteration: iterationCount
      }))
    }
  }

  // Add player positions to playerOverIterations
  playersOverIterations.kickOffTeam.forEach((player, index) => {
    player.positions.push({
      name: kickOffTeam.players[index].name,
      iteration: iterationCount,
      position: {
        x: kickOffTeam.players[index].currentPOS[0],
        y: kickOffTeam.players[index].currentPOS[1]
      }
    })
  })
  playersOverIterations.secondTeam.forEach((player, index) => {
    player.positions.push({
      name: secondTeam.players[index].name,
      iteration: iterationCount,
      position: {
        x: secondTeam.players[index].currentPOS[0],
        y: secondTeam.players[index].currentPOS[1]
      }
    })
  })

  if (matchDetails.endIteration === true) {
    delete matchDetails.endIteration
    return { matchDetails, startPositions, endPositions: startPositions }
  }

  playerMovement.closestPlayerToBall(closestPlayerA, kickOffTeam, matchDetails)
  playerMovement.closestPlayerToBall(closestPlayerB, secondTeam, matchDetails)

  kickOffTeam = playerMovement.decideMovement(closestPlayerA, kickOffTeam, secondTeam, matchDetails)
  secondTeam = playerMovement.decideMovement(closestPlayerB, secondTeam, kickOffTeam, matchDetails)

  matchDetails.kickOffTeam = kickOffTeam
  matchDetails.secondTeam = secondTeam

  if (matchDetails.ball.ballOverIterations.length === 0 || matchDetails.ball.withTeam !== '') {
    playerMovement.checkOffside(kickOffTeam, secondTeam, matchDetails)
  }

  if (matchDetails.specialState === 'freeKick' || matchDetails.specialState === 'goalKick') {
    ballMovement.handleSpecialState(matchDetails)
    playerMovement.handleSpecialState(matchDetails)

    matchDetails.specialState = null
  }

  let endPositions = {
    ball: Object.assign({}, matchDetails.ball.position),
    players: {
      kickOffTeam: kickOffTeam.players.map(player => ({
        id: player.id,
        name: player.name,
        iteration: iterationCount,
        position: { x: player.currentPOS[0], y: player.currentPOS[1] }
      })),
      secondTeam: secondTeam.players.map(player => ({
        id: player.id,
        name: player.name,
        iteration: iterationCount,
        position: { x: player.currentPOS[0], y: player.currentPOS[1] }
      }))
    }
  }
  return { matchDetails, startPositions, endPositions }
}


async function startSecondHalf(matchDetails) {
  validate.validateMatchDetails(matchDetails)
  validate.validateTeamSecondHalf(matchDetails.kickOffTeam)
  validate.validateTeamSecondHalf(matchDetails.secondTeam)
  validate.validatePlayerPositions(matchDetails)
  let { kickOffTeam, secondTeam } = matchDetails
  setPositions.switchSide(matchDetails, kickOffTeam)
  setPositions.switchSide(matchDetails, secondTeam)
  common.removeBallFromAllPlayers(matchDetails)
  setVariables.resetPlayerPositions(matchDetails)
  setPositions.setBallSpecificGoalScoreValue(matchDetails, matchDetails.secondTeam)
  matchDetails.iterationLog = [`Second Half Started: ${matchDetails.secondTeam.name} to kick offs`]
  matchDetails.kickOffTeam.intent = `defend`
  matchDetails.secondTeam.intent = `attack`
  matchDetails.half++
  return matchDetails
}

module.exports = {
  initiateGame,
  playIteration,
  startSecondHalf
}
