# Host Migration & Reconnection Plan (Relay Re-allocation Update)

## Project Overview
- **Game Title**: Chkobba Multiplayer
- **Target Platform**: WebGL / PC
- **Transport**: Unity Relay via NGO
- **Session Management**: Unity Multiplayer Services (Sessions)

## Implementation Steps

### Step 1: Persistent Join Code Cache
**Description**: Store the current Session Join Code in PlayerPrefs so it survives UI resets and is visually present for the re-joining host.
- **File**: `Assets/Scripts/NetworkManagerUI.cs`
- **Assigned Role**: Developer

### Step 2: Promoted Host Relay Re-allocation
**Description**: When a survivor is promoted to Host, they must generate a NEW Relay allocation and publish it to the session via a property (e.g., "ACTIVE_RELAY_CODE").
- **File**: `Assets/Scripts/NetworkManagerUI.cs`
- **Assigned Role**: Developer

### Step 3: Survivor Transport Refresh
**Description**: Remaining survivors must detect the "ACTIVE_RELAY_CODE" property change and reconnect their NetworkManager to the new Relay pipe.
- **File**: `Assets/Scripts/NetworkManagerUI.cs`
- **Assigned Role**: Developer

### Step 4: Rejoining Handshake & Scene Reconstruction
**Description**: The previous host joins the session, reads the new Relay code, connects as a client, and calls `RequestFullStateSyncServerRPC` to reconstruct their scene.
- **File**: `Assets/Scripts/NetworkManagerUI.cs`, `Assets/Scripts/GameNetworkRelay.cs`
- **Assigned Role**: Developer

### Step 5: Identity Self-Binding
**Description**: The new host must map their new `clientId` (0) to their original player seat in the restored `Server.cs` state.
- **File**: `Assets/Scripts/Server.cs`
- **Assigned Role**: Developer

## Verification & Testing
- Use MPPM (2+ instances).
- Verify Host disconnects -> survivor promotes -> survivor allocates new relay.
- Verify Join code is waiting in previous host's input box.
- Verify previous host rejoins and visual cards/scores are restored.
