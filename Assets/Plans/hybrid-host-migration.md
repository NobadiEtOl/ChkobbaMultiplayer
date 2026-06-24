# Project Overview
- **Game Title**: Chkobba Multiplayer
- **High-Level Concept**: A multiplayer card game where strategic host migration ensures game continuity even if the host disconnects.
- **Goal**: Implement a "Hybrid Host Migration" strategy using Unity Session properties for metadata and local scene auditing for card data.
- **Core Requirement**: The new host (survivor) must be completely independent of the old host's state at the moment of failure.

# Game Mechanics
## Hybrid State Recovery
To avoid data limits in the Session API and avoid "Reboot Amnesia" on the survivor, the state is split:
1. **Global Metadata (Session API)**: Small variables (Seed, Scores, Turn Counter, Round Counter) are pushed to the Session properties by the host whenever they change.
2. **Local Card State (Visual Audit)**: Heavy card data (which ID is in which hand/center) is reconstructed by the survivor by "scraping" their own visual scene before they promote themselves to host.

## Perspective Translation
The survivor uses their absolute seat number (`thisPlayerNumber`) to map relative visual indices back to global indices:
- **Global Index** = `(MySeat + RelativeIndex) % PlayerCount`

# UI
- **Migration Status**: Visual feedback (e.g., "Host Migrating... Please Wait") to inform players that the session is being handed over.
- **Score Stability**: Scores should persist on the UI throughout the migration process.

# Key Asset & Context
- `SerializableGameState.cs`: The DTO that will be "born" from the merged audit and metadata.
- `NetworkManagerUI.cs`: Orchestrates the migration flow and local caching.
- `GameManager.cs`: Performs the visual "Scrape" of the card IDs.
- `Server.cs`: Receives the injected state and restores the game logic.

# Implementation Steps

## A1: Null-Safety & Data Robustness
- **Description**: Fix the `NullReferenceException` in `SerializableDictionary.cs` by ensuring internal lists are initialized and `ToDictionary()` handles nulls gracefully.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## A2: Global Metadata Sync (Session API)
- **Description**: Update `Server.cs` to push "Global Truths" to the Session properties at key intervals (End of turn, score changes).
- **Data to Sync**: `seed`, `points[]`, `turnCounter`, `roundCount`, `lastPlayerToCapture`.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## A3: Survivor Visual Auditor
- **Description**: Implement `CaptureVisualSnapshot()` in `GameManager.cs`.
- **Logic**: Iterates through `DeckController` card containers and maps cards to absolute seats based on the client's `thisPlayerNumber`.
- **Assigned role**: developer
- **Dependencies**: A1
- **Parallelizable**: No

## A4: Migration Flow Orchestration
- **Description**: Update `NetworkManagerUI.cs` `HandleHostMigrationRoutine` to:
    1. Detect host drop.
    2. **Pull Metadata**: Read `seed`, `scores`, etc., from the Session properties.
    3. **Perform Audit**: Call `GameManager.CaptureVisualSnapshot()`.
    4. **Merge & Cache**: Combine them into a local `SerializableGameState` variable.
    5. **Role Switch**: Shutdown Client -> Start Host.
- **Assigned role**: developer
- **Dependencies**: A2, A3
- **Parallelizable**: No

## A5: State Injection & Re-Initialization
- **Description**: Update `HandleHostMigrationAsNewHost` to inject the cached state into the new `Server` instance before accepting client reconnections.
- **Assigned role**: developer
- **Dependencies**: A4
- **Parallelizable**: No

# Verification & Testing
- **Session API Check**: Verify that `GAME_SEED` and `TEAM_SCORES` are updated in the Session metadata during active gameplay.
- **Audit Check**: Trigger a manual "Visual Scrape" and print the results to console; verify indices match the global server state.
- **Migration Test**: Kill the host process. Verify the survivor becomes the host, restores the correct score, and keeps the same cards in center/hands.
