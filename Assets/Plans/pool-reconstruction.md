# Project Overview 
- Game Title: Chkobba Multiplayer
- High-Level Concept: Multiplayer card game based on Chkobba / Pişti with power-up mechanics.
- Players: Single player vs AI, or multiplayer (2 players 1v1, or 4 players in 2v2 teams)
- Inspiration / Reference Games: Chkobba / Pişti / Traditional Turkish card games with unique visual powers.
- Tone / Art Direction: 3D stylized cards and board
- Target Platform: WebGL / PC
- Screen Orientation / Resolution: Landscape (1920x1080)
- Render Pipeline: URP (Medium_PipelineAsset)

# Game Mechanics 
## Core Gameplay Loop
Players play cards from their hands to capture cards in the center by matching values or summing values. Captured cards go to player pools, and piştis occur when matching a single center card. First team to reach a target score wins.
## Controls and Input Methods
Mouse click/touch selection of hand and center cards.

# UI
Lobby screens, game table HUD, turn timer, and scoreboard.

# Key Asset & Context
- **Server.cs**: Serves as the authoritative match arbiter. Tracks game states, scores, turn-processing, and maintains card pools.
- **DeckController.cs**: Manages 3D card entities, animations, and coordinates local player hands, pools, and pişti visual layouts.
- **GameManager.cs**: Manages visual synchronization, handles client reconnection/migration game state applications, and triggers desync checks.
- **SerializableGameState.cs**: State serialization DTO used to synchronize clients and save snapshot states.

# Implementation Steps

### Step 1: Implement Server-Side Pişti Tracking
- **File**: `Assets/Scripts/Server.cs`
- **Assigned Role**: Developer
- **Dependencies**: None
- **Parallelizable**: Yes
- **Description**: Add a parallel dictionary `playersPiştiPoolCardsIDs` to track which unique card IDs belong to each player's pişti pool.
- **Details**:
  - Declare `private Dictionary<int, List<string>> playersPiştiPoolCardsIDs;`
  - Initialize the lists inside game startup/initialization methods (`InitializeGame` / `Start`).
  - Inside `AddDiscardedCardsToPlayerPool`, if a Pişti is validated, add the two discarded card IDs to `playersPiştiPoolCardsIDs[playerNumber]`.

### Step 2: Export Pişti State to Snapshot
- **File**: `Assets/Scripts/Server.cs`
- **Assigned Role**: Developer
- **Dependencies**: Step 1
- **Parallelizable**: No
- **Description**: Populate `snapshot.pistiPools` when generating snapshots.
- **Details**:
  - In `BuildGameStateSnapshot()`, replace the stub initialization of `snapshot.pistiPools` with actual data from `playersPiştiPoolCardsIDs`.

### Step 3: Re-Import Pişti State on Re-host / Migration
- **File**: `Assets/Scripts/Server.cs`
- **Assigned Role**: Developer
- **Dependencies**: Step 2
- **Parallelizable**: No
- **Description**: Restore `playersPiştiPoolCardsIDs` when applying or loading a snapshot.
- **Details**:
  - In `ApplyGameStateToServer(SerializableGameState snapshot)`, read the `pistiPools` dictionary from the snapshot and populate `playersPiştiPoolCardsIDs`.

### Step 4: Implement Client-Side Pool Assignment & Layout Updates
- **File**: `Assets/Scripts/DeckController.cs`
- **Assigned Role**: Developer
- **Dependencies**: None
- **Parallelizable**: Yes
- **Description**: Re-implement `AssignCardsToPlayerPools` to position/scale restored cards correctly, and implement `AssignCardsToPlayerPistiPools` to handle face-up pişti visual stacks.
- **Details**:
  - In `AssignCardsToPlayerPools`, loop through cards and position them at `localPosition = Vector3.zero` under the corresponding pool transform with correct scale and face-down rotation.
  - Implement `AssignCardsToPlayerPistiPools` which loops through pişti cards, parenting them to `playerPiştiPoolTransforms`. Set the capturing card (last card) as face-up and offset slightly, and keep the others face-down at zero.

### Step 5: Remap and Reconstruct Pools in GameManager
- **File**: `Assets/Scripts/GameManager.cs`
- **Assigned Role**: Developer
- **Dependencies**: Step 4
- **Parallelizable**: No
- **Description**: Add the visual reconstruction step for pişti pools inside client-side game state application.
- **Details**:
  - In `RebuildCardContainers(SerializableGameState snapshot)` coroutine, read `snapshot.pistiPools`, remap absolute seat indices to relative client indices, and invoke `deckCtrl.AssignCardsToPlayerPistiPools(new SerializableDictionary(relPistiPools))`.

# Verification & Testing
## Automated Checks
- Compile without warnings or errors in the Unity Editor.
- Verify through unit or integration logs that `CaptureVisualSnapshot` captures non-empty arrays for pools and pişti pools when cards are present in them.

## Manual Edge Cases & Scenarios
1. **Reconnection Visual Restructuring**:
   - Start a 2-player match.
   - Capture a few cards normally, and perform at least one Pişti capture.
   - Force-disconnect a client, then reconnect them.
   - Verify that the reconnected client sees the exact number of cards in their normal pool (stacked face-down) and the correct face-up pişti cards stacked at their relative seat.
2. **Host Migration Recovery**:
   - Start a match with 3 players (1 host, 1 client, 1 bot).
   - Perform several captures.
   - Disconnect the host to trigger a host migration.
   - Verify that the surviving client migrates to host, and all player pools and piştis are perfectly restored on the board without visual pile-ups or double-rendering.
