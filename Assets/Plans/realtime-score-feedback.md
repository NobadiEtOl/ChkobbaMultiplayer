# Project Overview
- Game Title: Chkobba Multiplayer
- High-Level Concept: A multiplayer card game based on Chkobba, featuring interactive superpowers, realtime matchmaking, reconnection support, and host migration.
- Players: Multiplayer (1v1, 2v2 modes, with bot fallbacks)
- Inspiration / Reference Games: Traditional Chkobba (Scopa) with superpower card battle mechanics.
- Tone / Art Direction: Casual, polished 2D card game with visual effects for superpower activations.
- Target Platform: WebGL
- Screen Orientation / Resolution: Landscape (Responsive)
- Render Pipeline: URP (Medium_PipelineAsset)

# Game Mechanics
## Core Gameplay Loop
Players play cards from their hand to the center of the board to capture cards matching sum/value rules. Captured cards are placed in players' card pools. Points are officially tallied at the end of each round (Aces, Jacks, special cards, most cards bonus). Superpowers can modify card values in real-time or penalize/boost scores (Ver Zehri, Kutsal Deste, Kapkaç, Yandım Anam, Kopyala Yapıştır).
## Controls and Input Methods
Mouse/touch interactions for playing and selecting cards, choosing superpowers from HUD, and targeting cards in play.

# UI
The game has an always-visible points HUD displaying scores for both players/teams via:
* `PlayerPointText1` and `PlayerPointText2` Text components (UI elements).
* `TallyContainer` and `TallyContainer1` components representing point tallies.
This UI is authoritatively updated in real-time via `UpdateScoreDisplayClientRPC` calls from the server.
To prevent visual information leakage of the first 3 dealt center cards (which are face-down or "closed" in the pool), we use a state-driven **Perspective Scoreboard Render Pipeline** on the client. Closed point cards are visually subtracted from the opponent's score, maintaining the illusion until the round officially ends.

# Key Asset & Context
The following files are affected:
1. **`Assets/Scripts/SerializableGameState.cs`**: Holds network serialized state. Add `p1side_selfFakePointReduction`, `p2side_selfFakePointReduction`, `p1side_oppFakePointReduction`, and `p2side_oppFakePointReduction` to serialize and persist perspective scores.
2. **`Assets/Scripts/Server.cs`**: Contains server-side game state, card pools, permanent points, round termination (`DecideWinner`), and reconnection snapshot generation (`BuildGameStateSnapshot`). Includes live recalculation and closed-card calculations.
3. **`Assets/Scripts/GameNetworkRelay.cs`**: Handles RPC synchronization between client and server, including perspective variables passed to client score updates.
4. **`Assets/Scripts/GameManager.cs`**: Caches latest score/reductions state to prevent visual bypasses and executes pure perspective rendering in `RenderScoreBoardVisuals`.

# Implementation Steps

### Step 1: Add Perspective Reduction Variables to `SerializableGameState.cs`
- **File**: `Assets/Scripts/SerializableGameState.cs`
- **Assigned Role**: Developer
- **Dependencies**: None
- **Parallelizable**: No
- **Description**: Add the following 4 integer fields to `SerializableGameState` to hold side-specific visual fake point reductions:
  * `p1side_selfFakePointReduction` (Reduction applied to Team 0 score when viewed by Team 0)
  * `p2side_selfFakePointReduction` (Reduction applied to Team 1 score when viewed by Team 1)
  * `p1side_oppFakePointReduction` (Reduction applied to Team 0 score when viewed by opponent Team 1)
  * `p2side_oppFakePointReduction` (Reduction applied to Team 1 score when viewed by opponent Team 0)
- **Serialization**: Register these in the `NetworkSerialize` method of `SerializableGameState` so they are synchronized during reconnection and host migration.

### Step 2: Implement Closed-Card Point Calculations in `Server.cs`
- **File**: `Assets/Scripts/Server.cs`
- **Assigned Role**: Developer
- **Dependencies**: Step 1
- **Parallelizable**: No
- **Description**:
  1. Add fields for the 4 perspective reduction variables in `Server.cs`.
  2. Implement a side-effect-free helper method `CalculatePoolClosedCardPoints(int teamNo)` that tallies standard point values (Aces, Jacks, special cards) of any cards currently in `playersPooledCardsIDs` that are part of the `firstThreeDealtCardIds` list.
  3. Ensure that when a round officially ends and pools are cleared, `firstThreeDealtCardIds` is cleared on the server. This resets the reduction values back to `0`, naturally breaking the illusion.

### Step 3: Implement Pure-View State Memory and Perspective Rendering in `GameManager.cs`
- **File**: `Assets/Scripts/GameManager.cs`
- **Assigned Role**: Developer
- **Dependencies**: Step 1
- **Parallelizable**: No
- **Description**:
  1. Add persistent fields in `GameManager.cs` to act as Client State Memory:
     * `lastAuthoritativeScore0`, `lastAuthoritativeScore1`
     * `lastP1SelfPointReduction`, `lastP2SelfPointReduction`
     * `lastP1OppPointReduction`, `lastP2OppPointReduction`
  2. Create a centralized scoreboard update entry point `SyncVisualScores(int score0, int score1, int p1Self, int p2Self, int p1Opp, int p2Opp)` that caches these values and calls `RenderScoreBoardVisuals()`.
  3. Implement `RenderScoreBoardVisuals()` as the **only** UI update method. It reads `deckController.thisPlayerNumber`, resolves whether the client is Team 0 (Player 0/2) or Team 1 (Player 1/3), applies perspective reduction mathematics, and updates text and tally elements.
  4. Update `ApplyCoreGameState()` to extract these variables from the snapshot, call `SyncVisualScores()`, and apply correct perspective values instantly upon reconnection.

### Step 4: Update RPC and Snapshot Building to Sync Reduction Variables
- **Files**: `Assets/Scripts/Server.cs`, `Assets/Scripts/GameNetworkRelay.cs`
- **Assigned Role**: Developer
- **Dependencies**: Step 2, Step 3
- **Parallelizable**: No
- **Description**:
  1. Update `BuildGameStateSnapshot()` in `Server.cs` to copy the server's current reduction variables into the snapshot DTO.
  2. Update `RestoreFromSnapshot()` in `Server.cs` to restore the server's reduction variables from the snapshot.
  3. Update `UpdateScoreDisplayClientRPC` in `GameNetworkRelay.cs` to accept the 4 reduction variables as optional parameters (defaulting to 0) and pass them to the client's `SyncVisualScores()`.
  4. Modify `BroadcastLiveScoreUpdate()` in `Server.cs` to calculate these reduction values and broadcast them through the updated RPC.

### Step 5: Trigger Updates on Capture and Power Events
- **Files**: `Assets/Scripts/Server.cs`, `Assets/Scripts/GameNetworkRelay.cs`
- **Assigned Role**: Developer
- **Dependencies**: Step 4
- **Parallelizable**: No
- **Description**: Ensure all paths (regular captures, superpower activations, reconnection completion) continue to call `BroadcastLiveScoreUpdate()`, which automatically updates the client state memory and maintains perfect visual consistency.

# Verification & Testing

### Test Case 1: Standard Card Capture
1. Play a card to capture an Ace or Jack.
2. Verify that the scoreboard instantly increments by `+1` point for the capturing player/team.
3. Verify that tally marks update accordingly.

### Test Case 2: Special Point Cards
1. Play a card to capture the **2 of Clubs** or the **10 of Diamonds**.
2. Verify that the scoreboard instantly increments by `+2` or `+3` points respectively.

### Test Case 3: Closed-Card Capture Perspective Visual Check (Team 0 captures)
1. Team 0 captures a closed card worth points (e.g. Ace or Jack in `firstThreeDealtCardIds`).
2. Verify that **Team 0's scoreboard instantly updates** with the points (+1).
3. Verify that **Team 1's scoreboard shows NO change** for Team 0 (the points are hidden).
4. Verify that tally displays remain unchanged for Team 1.

### Test Case 4: Closed-Card Capture Perspective Visual Check (Team 1 captures)
1. Team 1 captures a closed card worth points.
2. Verify that **Team 1's scoreboard instantly updates**.
3. Verify that **Team 0's scoreboard shows NO change** for Team 1.

### Test Case 5: Reconnection & Migration Reduction Sync
1. Team 0 captures a closed Ace (Team 0 score = 1, Team 1 score = 0, Team 1 sees Team 0 score as 0).
2. Disconnect a client, wait 5 seconds, and reconnect.
3. Verify that the reconnected client displays their exact perspective-specific scores instantly.
4. Trigger host migration.
5. Verify that when the new host takes over, everyone's scoreboard and tallies display the correct, perspective-specific scores instantly.

### Test Case 6: End of Round Tally Alignment
1. End the round by playing all cards.
2. Verify that `DecideWinner()` is called, pools are evaluated, and the final round-over screen displays the true scores for both sides, successfully breaking the illusion.
