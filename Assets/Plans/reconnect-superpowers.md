# Project Overview
- Game Title: ChkobbaMultiplayer
- High-Level Concept: An addictive multiplayer adaptation of the traditional Tunisian card game Chkobba with interactive superpowers and dynamic reconnection/host migration capabilities.
- Players: Multiplayer (2-4 players with dynamic bot filler takeover and player seat reclaim on reconnect).
- Inspiration / Reference Games: Traditional Chkobba, modern card-battler elements.
- Tone / Art Direction: Polished card game with smooth animations, satisfying sound effects, and clean UI/UX.
- Target Platform: WebGL (optimized for browser play).
- Render Pipeline: URP (Universal Render Pipeline).

---

# Game Mechanics
## Core Gameplay Loop
Players take turns playing cards from their hands to capture cards in the center of the table matching in value or sum. Special superpowers can be bought from the shop pouch (Kese) using gold accumulated via captures. These powers can then be used dynamically during the player's turn to shift the game state in their favor.

## Controls and Input Methods
Standard mouse / touch inputs to select, drag, and drop cards and superpower tokens.

---

# UI
Each player has an on-screen superpower token tray and a gold display showing their active gold balance. The gold and superpower displays are updated in real-time when gold is gained, tokens are drawn, or tokens are consumed.

---

# Key Asset & Context
### 1. `SerializableGameState.cs`
- Existing structure representing the network-serializable game state snapshot.
- To be extended with `playerGold` and `playerSuperPowers` fields to carry gold and power lists across reconnects and host migrations.

### 2. `Server.cs`
- The server-side source of truth.
- To be extended with authoritative dictionaries tracking gold and superpower token lists per player seat (`playerNo`).
- Snapshot builder (`BuildGameStateSnapshot()`) and state restorer (`ApplyGameStateToServer()`) to be updated to handle gold and powers.

### 3. `GameNetworkRelay.cs`
- The network broker.
- To be extended with `SyncGoldAndPowersServerRPC` to allow clients to update the server authoritative state upon any local inventory changes.

### 4. `SuperPowerSpawner.cs`
- The client-side manager of gold and superpower tokens.
- To be updated with server synchronization logic (`NotifyServerOfGoldAndPowers()`), direct local gold setter (`SetGold()`), and immediate visual token restoration (`RestorePowers()`).

### 5. `MainUIScript.cs`
- Manages client game states during disconnection cleanups.
- To be updated to prevent wiping out server-authoritative state when clearing local elements during cleanup.

---

# Implementation Steps

### Step 1: Extend network serialization
- **Description**: Add `playerSuperPowers` (`SerializableDictionary`) and verify `playerGold` (`SerializableIntDictionary`) inside `SerializableGameState.cs` to enable automatic network serialization.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2: Implement server-authoritative storage
- **Description**: Update `Server.cs` with dictionary storage for gold/powers per seat, populate them in `BuildGameStateSnapshot()`, and restore them in `ApplyGameStateToServer()`.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 3: Implement network sync broker
- **Description**: Add `SyncGoldAndPowersServerRPC(int playerNo, int gold, string[] powers)` in `GameNetworkRelay.cs` to route client inventory updates to the server.
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

### Step 4: Update client-side spawner and UI interactions
- **Description**:
  1. Add `NotifyServerOfGoldAndPowers()` inside `SuperPowerSpawner.cs` and trigger it in `AddGold()`, `SpendGold()`, `SpawnSuperPower()`, and `RemoveSpawnedSuperPower()`.
  2. Implement `SetGold(int amount)` and `RestorePowers(List<string> powerNames)` in `SuperPowerSpawner.cs` to allow direct instant setup on reconnect/migration.
- **Assigned role**: developer
- **Dependencies**: Step 3
- **Parallelizable**: No

### Step 5: Safeguard disconnection cleanup
- **Description**: Add an `isDisconnectingCleanUp` guard flag in `SuperPowerSpawner.cs` and set it inside `MainUIScript.cs` during disconnection reset to prevent the local wipe from deleting server state.
- **Assigned role**: developer
- **Dependencies**: Step 4
- **Parallelizable**: No

### Step 6: Visual state restoration on client
- **Description**: Integrate the gold and token restoration logic inside `ApplyGameStateCoroutine()` in `GameManager.cs` (around Step 5.5, before Update UI) to reconstruct local inventory for the reconnected/migrated client.
- **Assigned role**: developer
- **Dependencies**: Step 4, Step 5
- **Parallelizable**: No

---

# Verification & Testing
1. **Purchase Verification**: Purchase a superpower token from the pouch (Kese), verify that the local gold decreases, and check server logs to confirm that the server receives the updated gold and power list via the ServerRPC.
2. **Reconnection Verification**: Force disconnect a client who possesses gold and 2 superpower tokens, wait for bot takeover, reconnect the player, and verify that the reconnected player is assigned back to their seat and instantly receives their exact gold and superpower tokens.
3. **Host Migration Verification**: In a 3-player match, grant the non-host players gold and superpower tokens. Force disconnect the host to trigger migration, and verify that all surviving players preserve their exact seat mappings, gold, and superpower tokens.
