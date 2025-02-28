# FANTASY KICKOFF - Football Player Management Unity Mobile Game

This project is a Unity-based mobile game that allows users to view football player data, retrieve player statistics from an API, and display player information using a scroll view. It caches data locally so that the app can persist player information after closing, improving performance and reducing the need for frequent API calls.

## Features

- Fetches player data from an external API.
- Caches player data locally for faster future access.
- Displays player data in a user-friendly interface with images and statistics.
- Handles API pagination for fetching players across multiple pages.
- Supports dropdown menus for selecting teams and leagues.

## API Integration

The game pulls player data from the API using the endpoint:
```
https://v3.football.api-sports.io/players?team={teamId}&season=2024&page={page}
```

You can find the full documentation on the [API Football Documentation](https://www.api-football.com/).
