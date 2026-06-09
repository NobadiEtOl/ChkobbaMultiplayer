# Project Overview

- **Game Title:** Chkobba Multiplayer (Chkobba / Chkob/Çkobba card game)
- **High-Level Concept:** Online multiplayer Tunisian card game (Chkobba) with 2- and 4-player rooms, quick-play matchmaking, and super-power mechanics.
- **Players:** Networking multiplayer (2 or 4 players) via Unity Relay, with optional bot mode for quick play.
- **Inspiration / Reference Games:** Traditional Chkobba / Scopa card games.
- **Tone / Art Direction:** N/A for this task (networking refactor only).
- **Target Platform:** Mobile (Android + iOS) **primary**; WebGL functionality **preserved** as a secondary target.
- **Screen Orientation / Resolution:** N/A for this task.
- **Render Pipeline:** URP (Medium_PipelineAsset)

## Scope of THIS plan
This is a **networking cleanup/refactor**, not a gameplay change. The goal: make the game use the **Unity Multiplayer Services package (Sessions API, `com.unity.services.multiplayer` 2.2.3)** *as intended* so that:
1. A player can **create a room** (host) — 2P / 4P, private.
2. A player can **join a room** by code.
3. **Quick Play** finds an open session or hosts a new one (bot mode).
4. Players can **cleanly disconnect / return to the main menu** (host and client).
5. All leftover logic from the old **direct Relay + Lobby + ParrelSync** workflow that now breaks or interferes is **removed**.

**Explicitly deferred (per user decision):** Automatic reconnection and host migration (survivor re-host) will be **rebuilt later** on top of a working basic connection. This plan **removes** their currently-broken implementations and leaves the reusable server-side snapshot/sync infrastructure dormant for the future rebuild.

# Game Mechanics

## Core Gameplay Loop
Unchanged by this task. Players join a session, the host (Server) deals cards and runs authoritative game logic, clients send moves via `NetworkRelay` ServerRPCs and receive state via ClientRPCs.

## Controls and Input Methods
Unchanged. Connection is driven by UI buttons wired in `NetworkManagerUI.SetupButtonListeners()`:
- Create Room 2P / 4P, Join Room (by code), Quick Play 2P / 4P, Return-to-menu button.

# UI
No new UI. Existing `MainUIScript` screens are reused via its public callbacks:
- `OpenWaitingScreenUI(string color, string playerCount, string joinCode)`
- `OnReturnFromWaitingScreen()`
- `OnDisconnectDetected()`
- `ReturnToMainPage()`

# Root-Cause Analysis (why connections are broken)

The project migrated to Unity 6 + the Multiplayer Services package, which **bundles and internally manages Relay + Lobby**. The main connection flow in `NetworkManagerUI` was already ported to the Sessions API (`MultiplayerService.Instance.CreateSessionAsync / JoinSessionByCodeAsync / JoinSessionByIdAsync / QuerySessionsAsync` and `ISession.LeaveAsync`). However, large amounts of **pre-Sessions logic remain** and now actively interfere:

1. **Relay transport connection type not set per-platform.** `StartHostWithRelay()` calls `.WithRelayNetwork()` with no connection type. WebGL can **only** connect through Relay using **secure WebSockets (`wss`)** — if the relay defaults to UDP/DTLS, WebGL clients fail to connect entirely. Mobile (Android/iOS) works best with **`dtls`** (lower latency) but also supports `wss`. Because the game ships **primarily to mobile while preserving WebGL**, the fix is a **platform-aware connection type** (WebGL → `wss`, everything else → `dtls`). This is a primary suspect for the broken connection. *(Confirmed by Unity guidance: WebGL requires `wss` for Relay via the Multiplayer Services package — https://discussions.unity.com/t/unity-relay-through-multiplayer-services-package-webgl-compatibility/1638058 )*

2. **Custom heartbeat system** (`NetworkManagerUI` heartbeat/disconnect-detection coroutines + `Server.cs` `clientHeartbeats` / `CheckHeartbeatTimeouts`). Netcode + Sessions already detect disconnects. Worse, `Server.CheckHeartbeatTimeouts()` runs every frame and **force-disconnects any client after 20s without a heartbeat RPC** — a false-positive disconnect generator. The client side is already disabled (commented out in `Start()`), so heartbeats are never sent, making the server-side timeout especially dangerous.

3. **Manual relay keep-alive** (`NetworkManagerUI` client keep-alive + `Server.cs` `RelayKeepAliveCoroutine`, `SendRelayHeartbeat`, `SendHostHeartbeatToClients`, `UpdateLobbyHeartbeat`). The Sessions/Relay network keeps allocations alive automatically. `UpdateLobbyHeartbeat()` is the only remaining **live `LobbyService` call** and only no-ops because `currentLobby` is always null now.

4. **`currentLobby` is dead.** Under Sessions, the lobby is internal; `NetworkManagerUI.currentLobby` / `CurrentLobby` are never assigned (always null). `Server.cs` still reads `networkManagerUI.currentLobby.Data[...]` in disconnect logging and keep-alive checks — null-deref risk / dead branches.

5. **Coordinated-disconnection handshake** (`Server.StartCoordinatedDisconnection` / `OnClientConfirmedDisconnection` / `OnAllClientsConfirmedDisconnection`, and `NetworkRelay` `NotifyClientsToDisconnect*` / `ConfirmClientDisconnection*`). This existed to let clients leave the manual Relay allocation before the host tore it down. With Sessions, `ISession.LeaveAsync()` + `NetworkManager.Shutdown()` handle this; clients receive `OnClientDisconnectCallback` automatically when the host leaves.

6. **Survivor election / host migration** (`NetworkManagerUI.OnClientDisconnected` failover branch, `ShouldAttemptRehost`, `SurvivorElectionAndRehost`, `SurvivorRehost`; `Server.ComputeAndSaveSurvivorChain`; `NetworkRelay.DistributeSurvivorChainClientRPC`). `SurvivorRehost()` calls the **old direct APIs** (`RelayService.Instance.CreateAllocationAsync`, `GetJoinCodeAsync`, `LobbyService.Instance.UpdateLobbyAsync`, `UnityTransport.SetRelayServerData`, `NetworkManager.StartHost`) mixed into a Sessions-managed connection, and depends on the now-null `currentLobby`. It cannot work. **User decision: remove for now, rebuild later.**

7. **Auto-reconnect on startup + ParrelSync auth churn.** `Start()` auto-initializes services and auto-rejoins a saved session; `EnsureFreshAnonymousSignIn()` signs **out then in** on every launch; `InitializeUnityServicesCoroutine` / `AutoInitializeCoroutine` set a **ParrelSync** auth profile via reflection. Multiplayer Play Mode (`com.unity.multiplayer.playmode` 1.6.3) isolates each virtual player automatically, so ParrelSync profile-switching is obsolete; the sign-out/sign-in churn can cause auth instability. **User decision: defer auto-reconnect; remove the broken reconnect + ParrelSync logic now.** *(Note: anonymous sign-in itself is still required — confirmed by the same Unity guidance above; only the churn/profile logic is removed.)*

8. **`Update()` null-deref.** `NetworkManagerUI.Update()` reads `NetworkManager.Singleton.IsListening` with no null check → `NullReferenceException` spam before any NetworkManager exists. Pure debug code; remove.

9. **Duplicate `using` blocks** at the top of `NetworkManagerUI.cs` (lines 1–16 and 20–31) and unused `Unity.Services.Relay` / `Unity.Services.Lobbies` imports.

10. **App-pause = disconnect** (`OnApplicationPause` → `OnDisconnectDetected` / coordinated host disconnect). **User decision: ignore pause.**

> Note: The two Console "errors" currently shown are not real errors — they are normal `Debug.LogError(...)` used as info logging in `Server.AnotherPlayerConnected` ("SERVER MESSAGE…"). They can stay; they are not part of this fix (optional downgrade to `Debug.Log` noted in Testing).

# Key Asset & Context

### Files to modify
- `Assets/Scripts/NetworkManagerUI.cs` — primary cleanup target.
- `Assets/Scripts/Server.cs` — remove keep-alive, heartbeat, coordinated-disconnect, survivor-chain subsystems; drop Lobby usings/calls.
- `Assets/Scripts/NetworkRelay.cs` — remove obsolete RPCs.

### Files NOT modified (left dormant for future reconnection rebuild)
- Server-side snapshot/sync infra is **kept**: `Server.BuildGameStateSnapshot()`, `ApplyGameStateToServer`, `RebindPlayerClientId`, `RemoveReconnectingClient`, `OnReconnectionReady`, `reconnectingClients`; and `NetworkRelay` reconnection-sync RPCs (`ApplyGameStateClientRPC`, `ApplyGameStateToReconnectedClientClientRPC`, `ReconnectingClientCardsReadyServerRPC`, `AnnounceReconnectedPlayerNumberServerRPC`, `GivePlayerCountForReconnectedClientClientRPC`, `InitializeCardPrefabsForReconnectedClientClientRPC`, `TriggerDesyncCheckForReconnectedClientClientRPC`). These are not wired into any active flow after cleanup but remain compilable for the later rebuild.

### Target API shapes (for reference during implementation)
```csharp
// Platform-aware relay connection type: WebGL needs "wss"; mobile/native prefer "dtls".
// Add a small static helper (e.g. in NetworkManagerUI):
private static string RelayConnectionType()
{
#if UNITY_WEBGL && !UNITY_EDITOR
    return "wss";   // WebGL build: secure WebSockets only
#else
    return "dtls";  // Android / iOS / Editor: low-latency UDP-based
#endif
}

// HOST (cross-platform): choose connection type at runtime
var options = new SessionOptions {
    MaxPlayers = playerCount,
    IsPrivate  = privateFlag,
    SessionProperties = sessionProps
}.WithRelayNetwork(RelayConnectionType());   // <-- mobile=dtls, WebGL=wss
currentSession = await MultiplayerService.Instance.CreateSessionAsync(options);

// CLIENT
currentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);

// QUICK PLAY
var results = await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions{ Count = 25 });
// join match by id or CreateSessionAsync(...) fallback

// LEAVE (host or client)
if (currentSession != null) { await currentSession.LeaveAsync(); currentSession = null; }
if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
    NetworkManager.Singleton.Shutdown();

// AUTH (once, no churn, no ParrelSync)
if (UnityServices.State != ServicesInitializationState.Initialized)
    await UnityServices.InitializeAsync();
if (!AuthenticationService.Instance.IsSignedIn)
    await AuthenticationService.Instance.SignInAnonymouslyAsync();
```

# Target Behaviour After Cleanup

- **Create Room (2P/4P):** `CreateSessionAsync(... .WithRelayNetwork("wss"))` → Sessions starts NetworkManager as Host → show waiting screen with `currentSession.Code` → `Server.SetPlayerCount(n)`.
- **Join Room:** `JoinSessionByCodeAsync(code)` → Sessions starts NetworkManager as Client.
- **Quick Play:** query sessions → join first match with free slot, else create new (bot mode on).
- **Return to menu (client):** leave session + shutdown + `MainUIScript.OnReturnFromWaitingScreen()`.
- **Return to menu (host):** same; clients detect host loss via `OnClientDisconnectCallback` and return to menu themselves (no handshake needed).
- **Host disconnect mid-game:** game ends for everyone; each client returns to menu (host-migration removed).
- **Pause/focus-loss:** ignored (no disconnect).
- **Reconnection:** none for now (returns to menu on drop); to be rebuilt in a later plan.

# Implementation Steps

### Step 1 — `NetworkManagerUI.cs`: header & lifecycle cleanup
- **Description:**
  - Collapse the duplicated `using` blocks into one; remove `using Unity.Services.Relay;`, `Unity.Services.Relay.Models;`, `Unity.Services.Lobbies;`, `Unity.Services.Lobbies.Models;`, `Unity.Networking.Transport.Relay;` and the `#if UNITY_EDITOR // ParrelSync` comment block. Keep `Unity.Services.Core`, `Unity.Services.Authentication`, `Unity.Services.Multiplayer`, `Unity.Netcode`, `Unity.Netcode.Transports.UTP` (UTP still referenced only if needed — remove if unused after Step 6), `System.Threading.Tasks`.
  - Delete the ParrelSync reflection helpers `IsParrelSyncClone()` and `GetParrelSyncArgument()`.
  - Remove `Update()` entirely (null-deref debug code) and the `isListeningFlag` field.
  - Remove `OnApplicationPause(...)` disconnect handling (user: ignore pause). Either delete the method or make it a no-op.
- **Assigned role:** developer
- **Dependencies:** None
- **Parallelizable:** No (other NetworkManagerUI steps touch the same file)

### Step 2 — `NetworkManagerUI.cs`: simplify auth & service init
- **Description:**
  - Replace `EnsureFreshAnonymousSignIn()` (sign-out-then-in) with an idempotent helper that only signs in if not already signed in.
  - Replace `InitializeUnityServicesCoroutine` / `AutoInitializeCoroutine` / `AutoInitializeUnityServicesOnStartup` with a single `EnsureServicesReady()` async/coroutine that: `UnityServices.InitializeAsync()` if not initialized, then anonymous sign-in if not signed in. **No ParrelSync `SetProfile`.**
  - `Start()`: subscribe to `OnClientConnectedCallback` / `OnClientDisconnectCallback` (drop `OnServerStarted` heartbeat hookups), and kick off `EnsureServicesReady()`. Remove the `CheckForSavedGameAndReconnect()` call.
  - `Awake()`: keep button listeners + main-screen ref; remove ParrelSync PlayerPrefs-key init and `LoadLastGameInfo()` (see Step 4).
- **Assigned role:** developer
- **Dependencies:** Step 1
- **Parallelizable:** No

### Step 3 — `NetworkManagerUI.cs`: cross-platform relay + connection methods
- **Description:**
  - Add a `RelayConnectionType()` helper (platform-aware: `wss` on WebGL builds, `dtls` otherwise — see "Target API shapes").
  - `StartHostWithRelay(int, bool)`: change `.WithRelayNetwork()` → `.WithRelayNetwork(RelayConnectionType())`. Replace the `IsUnityServicesInitialized()` early-return with `await EnsureServicesReady()`. Keep session creation, `joinCodeText`, `OpenWaitingScreenUI`, and `Server.Singleton.SetPlayerCount(playerCount)`. Remove the `SaveGameJoinCode(...)` call (reconnection deferred).
  - `StartClientWithRelay()`: `await EnsureServicesReady()`; keep `JoinSessionByCodeAsync`; remove `SaveGameJoinCode`.
  - `FindLobbiesAndStartHostIfNoneExist(int)`: `await EnsureServicesReady()`; keep query/join/create-fallback logic.
  - `JoinSessionById(string)`: keep; remove `SaveGameJoinCode`.
  - Keep `IsUnityServicesInitialized()` only if still used elsewhere; otherwise delete.
- **Assigned role:** developer
- **Dependencies:** Step 2
- **Parallelizable:** No

### Step 4 — `NetworkManagerUI.cs`: remove heartbeat, keep-alive, reconnection, survivor, coordinated-disconnect, debug-chain
- **Description:** Delete the following members and their fields/coroutines:
  - **Heartbeat/disconnect-detection:** `heartbeatInterval`, `disconnectTimeout`, `lastHeartbeatTime`, `isHeartbeatActive`, `heartbeatCoroutine`, `disconnectCheckCoroutine`, `lastHostHeartbeat`, `HOST_HEARTBEAT_TIMEOUT`, `StartDisconnectDetection()`, `StopDisconnectDetection()`, `UpdateHostHeartbeat()`, `HeartbeatCoroutine()`, `DisconnectCheckCoroutine()`, `CheckHostTimeout()`, `SendHeartbeat()`, `CheckHeartbeatTimeout()`.
  - **Client relay keep-alive:** `clientRelayKeepAliveCoroutine`, `isClientRelayKeepAliveActive`, `StartClientRelayKeepAlive()`, `StopClientRelayKeepAlive()`, `ClientRelayKeepAliveCoroutine()`, `StartRelayKeepAliveAfterHostStart(...)`.
  - **Survivor / host-migration:** `survivorElectionCoroutine`, `isRecoveringHostLoss`, `IsRecoveringHostLoss`, `ShouldAttemptRehost(...)`, `REHOST_CASCADE_TIMEOUT`, `SurvivorElectionAndRehost(...)`, `SurvivorRehost(...)`.
  - **Auto-reconnect (deferred):** `enableAutomaticReconnection`, `lastGameJoined`, `CheckForSavedGameAndReconnect()`, `AutoInitializeUnityServicesAndCheckForSavedGame()`, `AttemptReconnection()`, `AttemptReconnectionCoroutine(...)`, `AttemptReconnectionToGame(...)`, `HandleSuccessfulReconnection()`, `ManualReconnectionAttempt()`, `GetLastGameJoinCode()`, and PlayerPrefs join-code helpers `SaveGameJoinCode`, `LoadLastGameInfo`, `ClearSavedGameInfo`, `ClearSavedGameInfoContextMenu`, `InitializePlayerPrefsKeys`, key fields `LAST_JOIN_CODE_KEY/…`. Also remove `GetUniquePlayerId()` if unused after this.
  - **Coordinated disconnection:** `CoordinateHostDisconnection()`, `CoordinateHostDisconnectionOnPause()`, `CoordinateHostDisconnectionOnQuit()`, `OnClientRequestedToDisconnect()`, `ConfirmDisconnectionToHost()`, `OnAllClientsConfirmedDisconnection()`, `OnHostDisconnected()` (replaced by direct disconnect handling).
  - **Debug chain:** `reconnectionDebugChain`, `AddDebugStep`, `AddParrelSyncDebugInfo`, `PrintDebugChain`.
  - **Lobby:** `currentLobby`, `CurrentLobby`. Keep `currentSession` / `CurrentSession`.
  - `DisconnectFromLobbyAndNetwork()` (the "keep relay alive" variant) — delete; only the complete-disconnect path is kept.
- **Assigned role:** developer
- **Dependencies:** Step 1
- **Parallelizable:** No

### Step 5 — `NetworkManagerUI.cs`: simplify disconnect & network event handlers
- **Description:**
  - `OnReturnToMainMenuButtonClicked()`: keep the `IsGameReadyToStart()` guard and game-state reset; then for BOTH host and client just call `PerformDisconnect()` (merge `PerformClientDisconnection` / `PerformHostDisconnection` into one — they were identical). No coordinated handshake.
  - `PerformDisconnect()`: `DeckController.LocalInstance?.DestroyAllCards()` → `DisconnectCompletelyFromLobbyAndRelay()` → `MainUIScript.OnReturnFromWaitingScreen()`.
  - `DisconnectCompletelyFromLobbyAndRelay()` (rename optional → `DisconnectAndReturnToMenu`): leave session (`currentSession.LeaveAsync()`), `NetworkManager.Shutdown()` if listening, reset `isInGame`, `Server.ResetServerSingletonForMainMenu()`. Remove keep-alive stop calls, lobby code, and `lastGameJoined` references.
  - `OnClientConnected(ulong)`: set `isInGame = true` (no heartbeat start).
  - `OnClientDisconnected(ulong)`: remove survivor/failover branch. New behavior: if we are a **client** and the disconnected id is the server (`NetworkManager.ServerClientId`) **or** it is our own id → host/connection lost → `DisconnectAndReturnToMenu()` + `MainUIScript.OnDisconnectDetected()`. If we are the **host** and a client left **pre-game** → stay on waiting screen (do nothing). 
  - `OnServerStarted()`: keep `isInGame = true` only (or remove subscription in Step 2 and delete this).
  - `OnApplicationQuit()`: keep only `currentSession.LeaveAsync()` cleanup; remove coordinated host-quit coroutine.
  - Keep helper UI methods used by the flow: `EnsureMainScreenIsActive`, `ResetPlayerGameStateBeforeDisconnection`, `ResetPlayerGoldToStarting`, `RemoveAllSuperpowerTokens`, `IsPreGameDisconnection`, `UpdateReturnButtonState`, `IsCurrentlyInGame`, `SetServerBotMode`, `ClearGamePlayerPrefs` (still clears `PlayerNumber` etc. for fresh games — keep but it no longer needs the reconnection keys).
- **Assigned role:** developer
- **Dependencies:** Steps 2, 4
- **Parallelizable:** No

### Step 6 — `Server.cs`: remove keep-alive, heartbeat, coordinated-disconnect, survivor-chain, Lobby
- **Description:**
  - Remove `using Unity.Services.Lobbies;` and `using Unity.Services.Lobbies.Models;`.
  - **Keep-alive:** delete `relayKeepAliveCoroutine`, `isRelayKeepAliveActive`, `hostAllocationId`, `clientAllocationIds`, `StartRelayKeepAlive`, `StopRelayKeepAlive`, `AddClientAllocationForKeepAlive`, `RelayKeepAliveCoroutine`, `SendRelayHeartbeat`, `SendHostHeartbeatToClients`, `UpdateLobbyHeartbeat`, `OnClientConnectedForKeepAlive`, `OnClientDisconnectedKeepAlive`, `StartServerSideKeepAliveForDisconnectedClient`, `CheckRelayAllocationStatus`. Remove the keep-alive restart inside `AnotherPlayerConnected`. Remove `StopRelayKeepAlive()` call inside `ResetServerSingletonForMainMenu` and `HandleHostDisconnection`.
  - **Heartbeat:** delete `clientHeartbeats`, `OnClientHeartbeat`, `CheckHeartbeatTimeouts`, and the per-frame `Update()` call to it (and any `.Remove(...)`/`.Clear()` on `clientHeartbeats`).
  - **Coordinated disconnect:** delete `clientsConfirmedDisconnection`, `isCoordinatedDisconnectionInProgress`, `IsCoordinatedDisconnectionInProgress`, `StartCoordinatedDisconnection`, `OnClientConfirmedDisconnection`, `OnAllClientsConfirmedDisconnection`.
  - **Survivor chain:** delete `ComputeAndSaveSurvivorChain` and its call site at game start; remove the `DistributeSurvivorChainClientRPC` invocation. Remove the `"SurvivorChain"` PlayerPrefs write here (the read inside `BuildGameStateSnapshot` can stay harmlessly or be removed — prefer remove to avoid confusion).
  - **Lobby refs:** in `OnClientDisconnected` and elsewhere, delete the `networkManagerUI.currentLobby...` logging/branches (field no longer exists).
  - **Host-loss notify:** in `HandleHostDisconnection`, remove `networkRelay.NotifyHostDisconnectedClientRPC()` (clients now detect host loss via `OnClientDisconnectCallback`). Keep the rest of the disconnect bookkeeping.
  - **Keep** untouched: `AnotherPlayerConnected` (minus keep-alive restart), `OnClientDisconnected` (simplified), `SetPlayerCount`, `GetPlayerCount`, `IsGameReadyToStart`, `GetPlayerNoForClient`, `RebindPlayerClientId`, `ResetAllServerVariables`, `ResetServerSingletonForMainMenu`, `BuildGameStateSnapshot`, `ApplyRestoredSnapshotOnRehost`, `RemoveReconnectingClient`, `OnReconnectionReady` (dormant infra for future reconnection).
- **Assigned role:** developer
- **Dependencies:** Step 5 (so external callers in NetworkManagerUI are already removed)
- **Parallelizable:** No (same file ordering matters; but independent of NetworkRelay → can run parallel with Step 7)

### Step 7 — `NetworkRelay.cs`: remove obsolete RPCs
- **Description:** Delete these RPC methods (no longer called after Steps 4–6):
  - `SendHeartbeatServerRPC`, `SendHostHeartbeatClientRPC` (heartbeat).
  - `NotifyClientsToDisconnectServerRPC`, `NotifyClientsToDisconnectClientRPC`, `ConfirmClientDisconnectionServerRPC` (coordinated disconnect).
  - `NotifyHostDisconnectedClientRPC` (host-loss now via Netcode callback).
  - `DistributeSurvivorChainClientRPC` (survivor migration).
  - Keep all gameplay RPCs and the dormant reconnection-sync RPCs listed in "Files NOT modified".
- **Assigned role:** developer
- **Dependencies:** Steps 4–6 must remove the callers; this step removes the definitions. (Safe to do together with Step 6.)
- **Parallelizable:** Yes (with Step 6, different file) — but compile only after callers removed.

### Step 8 — Compile & resolve references
- **Description:** After Steps 1–7, do a full compile pass. Resolve any remaining references to deleted members (search the whole project for: `StartRelayKeepAlive`, `OnClientHeartbeat`, `StartCoordinatedDisconnection`, `OnAllClientsConfirmedDisconnection`, `currentLobby`, `CurrentLobby`, `SendHeartbeatServerRPC`, `SendHostHeartbeatClientRPC`, `NotifyClientsToDisconnect`, `ConfirmClientDisconnection`, `NotifyHostDisconnectedClientRPC`, `DistributeSurvivorChainClientRPC`, `AttemptReconnection`, `lastGameJoined`, ParrelSync). Fix or remove each.
- **Assigned role:** developer
- **Dependencies:** Steps 1–7
- **Parallelizable:** No

# Verification & Testing

### Build / compile
1. Project compiles with **zero errors** and no references to removed members (run the grep list in Step 8).
2. Confirm no remaining `using Unity.Services.Relay` / `Unity.Services.Lobbies` / `ParrelSync` references in `NetworkManagerUI.cs`, `Server.cs`, `NetworkRelay.cs`.

### Editor / Multiplayer Play Mode (2 virtual players)
3. **Create + Join (2P):** Player A "Create Room 2P" → waiting screen shows a session code. Player B "Join Room" with that code → both connect; `Server.AnotherPlayerConnected` fires for B; game starts. No false disconnects after 20–30s (confirms heartbeat-timeout removal).
4. **Quick Play (2P):** First instance Quick Play creates a session; second instance Quick Play joins it (or bot mode fills). Verify `QuerySessionsAsync` match path.
5. **Client return-to-menu:** Client presses return → leaves session, returns to main menu; host stays on waiting screen (pre-game) without errors.
6. **Host return-to-menu / host quit:** Host returns/quits → client receives `OnClientDisconnectCallback` and returns to main menu automatically (no handshake, no host-migration attempt). Verify no `SurvivorRehost`/`RelayService` calls in logs.
7. **Pause/alt-tab:** Alt-tab the client during a game → it stays connected (no disconnect on pause).

### Mobile build (primary target)
8. Make an **Android** (and/or iOS) development build. Host from a mobile build, join from the editor (or a second device) by code. Verify connection succeeds with the `dtls` path and a full short round plays. This is the main shipping path.

### WebGL build (preserved secondary target)
9. Make a **WebGL** build (and/or run one WebGL build + one editor instance). Host from one, join from the other by code. **This validates the `wss` path** — connection should succeed where it previously failed. Confirm the `RelayConnectionType()` `#if UNITY_WEBGL` branch compiles into the WebGL build. If it still fails, capture the transport/relay error logs.
10. **Cross-platform sanity:** confirm a mobile/native client and a WebGL client can coexist in the same session (Relay bridges `dtls` and `wss` peers). If mixed-transport proves problematic in practice, the fallback is to use `wss` on all platforms — note this as a known toggle.

### Regression
11. Play a full short round (deal, a few moves, super-power use) to confirm gameplay RPCs in `NetworkRelay` are intact after the RPC deletions.
12. (Optional polish) Downgrade the "SERVER MESSAGE…" `Debug.LogError` calls in `Server.AnotherPlayerConnected` to `Debug.Log` so the Console no longer shows them as errors.

### Out of scope (next plan)
- Rebuild **automatic reconnection** (rejoin saved session, re-bind player number via `RebindPlayerClientId`, replay `BuildGameStateSnapshot`).
- Optionally evaluate the Sessions package's built-in **host migration** instead of the old custom survivor re-host.
