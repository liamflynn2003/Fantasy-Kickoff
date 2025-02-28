# FANTASY KICKOFF - Football Player Management Unity Mobile Game
<p align="center">
  <img src="https://github.com/user-attachments/assets/e148240d-12b2-4cfa-8019-af1a37e9be86"/>
</p>
This project is a Unity-based mobile game that allows users to view football player data, retrieve player statistics from an API, and display player information using a scroll view. It caches data locally so that the app can persist player information after closing, improving performance and reducing the need for frequent API calls.

## Features

- Fetches player data from an external API.
- Caches player data locally for faster future access.
- Displays player data in a user-friendly interface with images and statistics.
- Handles API pagination for fetching players across multiple pages.
- Supports dropdown menus for selecting teams and leagues.

## Leagues and Teams
- Utilizes the current or most recent season from certain supported Leagues.
  
<p align="center">
  <img src="https://github.com/user-attachments/assets/6e3f27d3-326c-4288-89b8-07743e7ca926" width="300"/>
</p>
- Supported Leagues: Premier League, EFL Championship, La Liga, Serie A, Bundesliga, Ligue Une, Belgian League, League of Ireland

## API Integration

The game pulls player data from the API using the endpoint:
```
https://v3.football.api-sports.io/players?team={teamId}&season=2024&page={page}
```

You can find the full documentation on the [API Football Documentation](https://www.api-football.com/).

## Simulation Engine

The football match simulation engine is hosted on a Node.js server running on AWS. Unity sends the selected teams' information to the server, which processes the match and returns the simulation results. These results are then displayed to the player as a match, including statistics and player performances.

The engine can be found in the project repo here: https://github.com/liamflynn2003/Fantasy-Kickoff/tree/main/Simulation%20Engines/footballSimulationEngine-master

This engine is gratefully adapted from Aiden Gallagher's public and free-to-use work on the Football Simulation Engine, which can be found here: https://github.com/GallagherAiden/footballSimulationEngine
