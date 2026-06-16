# Plan: Host Migration & Reconnection State Sync

## Project Overview
- **Game Title**: Chkobba Multiplayer
- **High-Level Concept**: A card game with multiplayer support using Unity Netcode for GameObjects (NGO) and Session/Relay services.
- **Players**: 1v1 (2 players) or 4 players.
- **Target Platform**: WebGL (PC/Mobile)
- **Render Pipeline**: URP (Medium_PipelineAsset)

## Problem Analysis
Currently, when a host migrates (host leaves, client becomes new host), and the original host reconnects:
1.  **Identity Loss**: The reconnecting player is assigned a new player number by the server based on connection order, breaking perspective.
2.  **State Desync**: The reconnecting player doesn't receive the current hand/table state, leading to a "different scene" or a potential "redeal" trigger.
3.  **Perspective Conflict**: Miscalculated `thisPlayerNumber` causes the same player to appear at the bottom for both clients.

## Proposed Solution: Client-Led Identity & State Push
We will shift from "Server-Assigned" to "Client-Reclaimed" identity. The client remembers their seat and tells the server who they are. The server then pushes the current snapshot to that specific client.

---

## Key Assets & Context
- `Assets/Scripts/Server.cs`: Authoritative game state manager.
- `Assets/Scripts/DeckController.cs`: Handles local card display and perspective.
- `Assets/Scripts/GameNetworkRelay.cs`: Communication bridge between clients and server.
- `Assets/Scripts/NetworkManagerUI.cs`: Handles host migration routine.

---

## Implementation Steps

### 1. Persistent Identity (Client Side)
- **Description**: Modify `DeckController` to save the assigned player number to `PlayerPrefs` and provide a way to retrieve it during reconnection.
- **Assigned Role**: Developer
- **Dependencies**: None
- **Files**: `Assets/Scripts/DeckController.cs`

### 2. Guard Automatic Assignment (Server Side)
- **Description**: Update `Server.AnotherPlayerConnected` to skip automatic `GetPlayerNumberClientRPC` if the game is already in progress (`isReconnection`). This prevents the server from overriding a player's seat during migration.
- **Assigned Role**: Developer
- **Dependencies**: None
- **Files**: `Assets/Scripts/Server.cs`

### 3. Seat Reclamation & Targeted State Push
- **Description**: 
    - Update `GameNetworkRelay.ReclaimSeatServerRPC` to not only rebind the ID but also immediately call `ApplyGameStateClientRPC` targeting the specific `clientId` that reconnected.
    - This ensures the reconnecting player gets the "center" and "hand" data without a redeal.
- **Assigned Role**: Developer
- **Dependencies**: Step 2
- **Files**: `Assets/Scripts/GameNetworkRelay.cs`, `Assets/Scripts/Server.cs`

### 4. Migration State Restoration (Host Side)
- **Description**: 
    - Ensure `HandleHostMigrationRoutine` in `NetworkManagerUI.cs` correctly populates the `Server`'s dictionaries from the last known `GameManager` snapshot.
    - Add a flag to `Server` (e.g., `isRestoredFromMigration`) to prevent `StartGame` or initial dealing logic from firing if we already have hand data.
- **Assigned Role**: Developer
- **Dependencies**: Step 3
- **Files**: `Assets/Scripts/NetworkManagerUI.cs`, `Assets/Scripts/Server.cs`

### 5. Seed Synchronization
- **Description**: Verify `SerializableGameState` includes the `seed`. Ensure `ApplyGameStateToServer` applies this seed to the server's randomizer so the deck order remains consistent after migration.
- **Assigned Role**: Developer
- **Dependencies**: None
- **Files**: `Assets/Scripts/Server.cs`

---

## Verification & Testing
- **Test Case 1: Perspective Check**:
    - Connect P0 and P1.
    - Confirm P0 sees P0 at bottom, P1 sees P1 at bottom.
- **Test Case 2: Host Migration**:
    - Disconnect P0 (Host).
    - P1 becomes New Host. P1 must remain in their original seat at the bottom.
- **Test Case 3: Reconnection**:
    - P0 reconnects to P1.
    - P0 must reclaim Seat 0.
    - P0 must receive the hands/center cards immediately without any cards moving or redealing.
- **Test Case 4: Card Logic**:
    - Play a card as P0 after reconnecting. P1 must see the correct card played.
