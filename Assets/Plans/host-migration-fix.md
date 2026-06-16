# Project Overview
- Game Title: Chkobba Multiplayer
- High-Level Concept: A multiplayer card game with host migration support.
- Players: Single player vs AI, Local/Online Multiplayer.
- Inspiration / Reference Games: Chkobba (Traditional card game).
- Target Platform: WebGL, PC.
- Render Pipeline: URP (Medium_PipelineAsset).

# Game Mechanics
## Core Gameplay Loop
Players take turns playing cards to the center or capturing cards. If the host leaves, one of the clients takes over the session (host migration), preserving the game state (deck, hands, scores).

## Host Migration Workflow
1. Clients keep a copy of the last valid game state.
2. If the host disconnects, a client starts a new host instance.
3. The new host restores the game state and waits for other clients to reconnect.
4. Returning clients reclaim their seats using their saved player index.

# Key Asset & Context
- **`Assets/Scripts/Server.cs`**: Authoritative game logic and seat management.
- **`Assets/Scripts/NetworkManagerUI.cs`**: Orchestrates the host migration process.
- **`Assets/Scripts/DeckController.cs`**: Saves player seat to `PlayerPrefs`.
- **`Assets/Scripts/SerializableGameState.cs`**: DTO for game state sync.

# Implementation Steps

## Step 1: Fix Seed Restoration in Server.cs
**Description**: Ensure the game seed is preserved across migration so the deck remains consistent.
- **Assigned role**: developer
- **Files**: `Assets/Scripts/Server.cs`
- **Details**: In `ApplyGameStateToServer(SerializableGameState snapshot)`, add `this.seed = snapshot.seed;`.
- **Dependencies**: None
- **Parallelizable**: Yes

## Step 2: Refine Connection Logic for Migration in Server.cs
**Description**: Fix `AnotherPlayerConnected` to correctly identify the host as a re-connected player and accurately track active client counts.
- **Assigned role**: developer
- **Files**: `Assets/Scripts/Server.cs`
- **Details**:
    - Update `isReconnection` logic to treat the migration host as a returning player.
    - Implement a `countedClients` HashSet to prevent double-counting connections while ensuring `connectedPlayerCount` reaches the required threshold.
    - Ensure `connectedPlayerCount` increments even if a client was pre-bound via `RebindPlayerClientId`.
- **Dependencies**: Step 1
- **Parallelizable**: No

## Step 3: Validate Migration Sequence in NetworkManagerUI.cs
**Description**: Ensure the migration routine calls server methods in the correct order to prevent state race conditions.
- **Assigned role**: developer
- **Files**: `Assets/Scripts/NetworkManagerUI.cs`
- **Details**: Verify `HandleHostMigrationRoutine` sequence:
    1. `NetworkManager.StartHost()`
    2. `Server.ResetAllServerVariables(true)`
    3. `Server.ApplyGameStateToServer(snapshot)`
    4. `Server.RebindPlayerClientId(mySeat, localId)`
    5. `Server.AnotherPlayerConnected(localId)`
- **Dependencies**: Step 2
- **Parallelizable**: No

## Step 4: Verification & Testing
**Description**: Run automated and manual tests to confirm session stability.
- **Assigned role**: developer
- **Details**:
    1. Start 2-player game.
    2. Disconnect host.
    3. Verify client becomes host and retains cards/perspective.
    4. Reconnect original host.
    5. Verify original host reclaims Player 1 seat and sees correct cards.
- **Dependencies**: Step 3
- **Parallelizable**: No

# Verification & Testing
- **Manual Check**: Perform host migration in 1v1 and 2v2 scenarios. Observe `DeckController.thisPlayerNumber` on all clients.
- **Console Logs**: Verify logs show `[Server] Client X re-bound to Player Index Y` and `[Server] Session restored with Seed Z`.
- **Card Match**: Confirm the cards in the center and in hands match the pre-migration state exactly.
