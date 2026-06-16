# Plan: Robust Host Migration & Stateful Reconnection

## Project Overview
- **Game Title**: Chkobba Multiplayer
- **Goal**: Implement a robust, single-shot reconnection flow that separates Host Migration from Client Reconnection to prevent scene resets and race conditions.

## Problem Analysis
The current logic uses a single method (`AnotherPlayerConnected`) for all connection types. This causes the migrating host to trigger "reconnection" logic on themselves, which includes a command to reset their own scene (`InitializeCardPrefabs`). Additionally, multiple RPCs are sending the full game state at different times, leading to race conditions.

## Proposed Solution
1.  **Explicit Connection Intents**: Split `AnotherPlayerConnected` into three distinct logic paths: `HandleFreshJoin`, `HandleReconnectJoin`, and `HandleMigratedHostSelfBind`.
2.  **Host Migration Isolation**: The migrating host will use a dedicated path that restores the snapshot and binds their seat *without* triggering any client-side reset/reconnect RPCs.
3.  **Single-Shot Handshake**: `ReclaimSeatServerRPC` will no longer push the game state. It will only register the intent. The **single** game state snapshot will only be sent after the client confirms their local card prefabs are ready.
4.  **Reconnection State Tracking**: A state machine in `Server.cs` will track each client's progress (`None` -> `InitSent` -> `ReadyReceived` -> `SnapshotSent` -> `Completed`) to prevent duplicate initialization.
5.  **Safety Cleanup**: A cleanup routine will remove "stale" reconnecting clients who fail to finish the handshake.

---

## Key Assets & Context
- `Assets/Scripts/Server.cs`: Central authority for game state and connection handling.
- `Assets/Scripts/GameNetworkRelay.cs`: Networking bridge for RPC communication.
- `Assets/Scripts/NetworkManagerUI.cs`: Migration routine and session management.

---

## Implementation Steps

### 1. Define Reconnect State Tracking (Server.cs)
- **Description**: 
    - Add `public enum ReconnectPhase { None, InitSent, ReadyReceived, SnapshotSent, Completed }`.
    - Add `private Dictionary<ulong, ReconnectPhase> clientReconnectPhases`.
- **Assigned role**: developer
- **Dependencies**: None

### 2. Refactor Server Connection Logic (Server.cs)
- **Description**: 
    - Create `HandleFreshJoin(clientId)`: Handles initial lobby connections.
    - Create `HandleReconnectJoin(clientId)`: Handles mid-game joiners. Sends `InitializeCardPrefabs` RPC.
    - Create `HandleMigratedHostSelfBind(playerNo, clientId)`: Specifically for the new host. Rebinds the seat and increments counts but **skips** sending catch-up RPCs.
- **Assigned role**: developer
- **Dependencies**: Step 1

### 3. Update Migration Handshake (NetworkManagerUI.cs)
- **Description**: Modify `HandleHostMigrationRoutine` to call `Server.Singleton.HandleMigratedHostSelfBind` instead of `AnotherPlayerConnected`.
- **Assigned role**: developer
- **Dependencies**: Step 2

### 4. Refine GameNetworkRelay Handshake (GameNetworkRelay.cs)
- **Description**: 
    - **ReclaimSeatServerRPC**: Remove the immediate `ApplyGameStateClientRPC` call. It should now only trigger `server.HandleReconnectJoin`.
    - **ReconnectingClientCardsReadyServerRPC**: Ensure this is the **only** trigger for pushing the game state. Update the client's phase in the server's tracking dictionary.
- **Assigned role**: developer
- **Dependencies**: Step 3

### 5. Implement Safety Cleanup (Server.cs)
- **Description**: Add a timeout check (coroutine) that clears clients from `reconnectingClients` if they fail to progress from `InitSent` to `Completed` within a time limit (e.g., 15 seconds).
- **Assigned role**: developer
- **Dependencies**: Step 1

---

## Verification & Testing
- **Test Case 1: Host Migration (New Host View)**: 
    - Client becomes host. Confirm their table **does not** clear or reset during `HandleHostMigrationRoutine`.
- **Test Case 2: Handshake Sequence**: 
    - Original host reconnects. Server logs should show the transition through `ReconnectPhase` states.
    - Verify that `ApplyGameState` only arrives **after** the client has finished spawning their local card objects.
- **Test Case 3: Perspective Check**: 
    - Reconnecting player must see their own seat at the bottom.
- **Test Case 4: Timeout**: 
    - Artificially block a client from sending "Ready". Verify the server removes them after the timeout.
