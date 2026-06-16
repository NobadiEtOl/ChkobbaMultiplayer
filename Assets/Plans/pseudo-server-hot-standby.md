# Project Overview
- **Game Title:** Chkobba Multiplayer
- **High-Level Concept:** Card game with host-authoritative logic and client-side visuals.
- **Players:** Multiplayer (Netcode for GameObjects).
- **Current Challenge:** Host failover needs to be more robust. The visual state and server state need to stay in sync during host migration.
- **Solution:** "Hot Standby" Pseudo-Server architecture. Every client maintains a local `Server` instance that mirrors the host's state and becomes the authority upon promotion.

# Game Mechanics
## Core Gameplay Loop
- Players perform moves (play card, capture, use powers).
- Host validates moves, updates state, and broadcasts snapshots.
- **Hot Standby**: Non-host clients update their local `Server` singleton with every snapshot received.

## Controls and Input Methods
- No changes to user input. The Pseudo-Server operates in the background.

# UI
- No new UI. Debug logs will be added to monitor Pseudo-Server state and sync verification results.

# Key Asset & Context
- `Server.cs`: Will be modified to support a "Passive" mode where it updates its internal dictionaries but doesn't run timers or process logic.
- `GameNetworkRelay.cs`: Will trigger updates to the local Pseudo-Server upon receiving RPCs.
- `NetworkManagerUI.cs`: Host migration routine will be updated to "activate" the Pseudo-Server instead of just performing a fresh reset.

# Implementation Steps

## 1. Passive State Synchronization
- **Description**: Ensure the `Server` instance on clients is updated whenever a game state snapshot is received.
- **Details**: 
    - Modify `GameNetworkRelay.ApplyGameStateClientRPC` to call `Server.Singleton.ApplyGameStateToServer(snapshot)`.
    - Currently, `Server.ApplyGameStateToServer` is likely only called during migration. By calling it on every snapshot, the Pseudo-Server stays "Hot".
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## 2. Pseudo-Server Mode in Server.cs
- **Description**: Add logic to prevent the local `Server` from acting like a host while in "Pseudo" mode.
- **Details**:
    - Add a `bool isActiveHost` flag to `Server.cs` (default false).
    - In `Update()`, `StartGame()`, and timer coroutines, check `isActiveHost` and `IsServer`.
    - Ensure logic like `activeTurnTimerCoroutine` or `botPlayer` only runs when `isActiveHost` is true.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## 3. Migration Activation Logic
- **Description**: Update the migration routine to transition the Pseudo-Server to an Active Server.
- **Details**:
    - In `NetworkManagerUI.HandleHostMigrationRoutine`, after `NetworkManager.Singleton.StartHost()` and waiting for `Server.Singleton` to be ready:
        - Call a new method `Server.Singleton.ActivateFromPseudoServer()`.
        - This method should set `isActiveHost = true`, start necessary timers (if it's the current player's turn), and re-bind the local player's seat.
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

## 4. Preservation of State during Reset
- **Description**: Ensure `ResetAllServerVariables` doesn't wipe the "Hot" state during migration.
- **Details**:
    - Modify `Server.ResetAllServerVariables()` to accept a parameter `bool isMigration`.
    - If `isMigration` is true, skip clearing critical game data dictionaries (`centerCardsDict`, `allCardLookup`, etc.) but reset network-specific tracking like `connectedPlayerCount` or `pendingTurnConfirmations`.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

## 5. Post-Activation Sync Verification
- **Description**: Implement a "Sanity Check" to compare the promoted Server state with the existing Visual state.
- **Details**:
    - Create `Server.VerifySyncWithVisuals()`.
    - Compare `Server` center cards count vs `BoardController.centerCards`.
    - Compare `Server` player hand counts vs `DeckController`.
    - If desync found, log an error and immediately broadcast a new snapshot to "force" clients (and self) into the server's state.
- **Assigned role**: developer
- **Dependencies**: Step 3
- **Parallelizable**: No

# Verification & Testing
- **Failover Simulation**: 
    - Run Host + 1 Client. 
    - Perform 3 turns. 
    - Kill Host. 
    - Check if Client becomes Host and `Server.Singleton` has the correct `turnCounter` and `currentPlayer` without re-applying the snapshot from scratch.
- **Logs**:
    - "Pseudo-Server updated to version X" on clients.
    - "Pseudo-Server Activated - Sync Verification: PASS/FAIL" on new host.
- **Desync Test**:
    - Manually mess with a client card visually before killing the host. Verify the new host detects the desync and corrects it.
