using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField]private MainUIScript mainUIScript;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button hostTwoPlayerButton;
    [SerializeField] private Button hostFourPlayerButton;
    [SerializeField] private Button quickPlayTwoPlayerButton;
    [SerializeField] private Button quickPlayFourPlayerButton;
    [SerializeField] private InputField inputField;
    [SerializeField] private Text joinCodeText;
    [SerializeField] private Button returnToMainMenuButton; // Return button for waiting screen
    private string joinCodeVar;

    private bool isInGame = false; // Whether we're currently in an active game
    private bool isMigrating = false;

    // === SESSIONS API ===
    private ISession currentSession;
    public ISession CurrentSession => currentSession;

    // === UI REFERENCES ===
    private GameObject mainScreen;

    void Awake()
    {
        SetupButtonListeners();
    }

    private void InitializeMainScreenReference()
    {
        mainScreen = GameObject.Find("MainScreen");
        if (mainScreen != null) Debug.Log("[NetworkManagerUI] Main screen reference initialized successfully");
        else Debug.LogError("[NetworkManagerUI] Main screen GameObject not found!");
    }

    async void Start()
    {
        InitializeMainScreenReference();
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        await EnsureServicesReady();
        
        // Restore last used join code if available
        if (PlayerPrefs.HasKey("LastSessionCode") && inputField != null)
        {
            inputField.text = PlayerPrefs.GetString("LastSessionCode");
        }
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private async Task EnsureServicesReady()
    {
        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized) await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (Exception e) { Debug.LogError($"[NetworkManagerUI] EnsureServicesReady failed: {e.Message}"); }
    }

    public async Task<string> StartHostWithRelay(int playerCount, bool privateFlag, bool isQuickPlay = false)
    {
        if (Server.Singleton != null) 
        {
            Server.Singleton.ResetAllServerVariables();
            Server.Singleton.SetBotModeEnabled(isQuickPlay);
        }
        if (GameManager.LocalInstance != null) GameManager.LocalInstance.ResetForNewGame();
        await EnsureServicesReady();
        try
        {
            var options = new SessionOptions { MaxPlayers = playerCount, IsPrivate = privateFlag }.WithRelayNetwork();
            currentSession = await MultiplayerService.Instance.CreateSessionAsync(options);
            joinCodeText.text = currentSession.Code;
            if (inputField != null) inputField.text = currentSession.Code;
            PlayerPrefs.SetString("LastSessionCode", currentSession.Code);
            PlayerPrefs.Save();
            if (mainUIScript != null) mainUIScript.OpenWaitingScreenUI(privateFlag ? "blue" : "red", playerCount.ToString(), currentSession.Code);
            if (Server.Singleton != null) Server.Singleton.SetPlayerCount(playerCount);
            isInGame = true;
            return currentSession.Code;
        }
        catch (Exception e) { Debug.LogError($"[NetworkManagerUI] Failed to start host: {e.Message}"); return null; }
    }

    public async Task<bool> StartClientWithRelay()
    {
        string inputJoinCode = inputField.text;
        if (string.IsNullOrEmpty(inputJoinCode)) return false;
        if (mainUIScript != null) mainUIScript.OpenWaitingScreenUI("yellow", "2", inputJoinCode);
        await EnsureServicesReady();
        try
        {
            currentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(inputJoinCode);
            PlayerPrefs.SetString("LastSessionCode", inputJoinCode);
            PlayerPrefs.Save();
            
            // Check if there's a migrated relay code in the session properties
            if (currentSession.Properties.ContainsKey("ACTIVE_RELAY_CODE"))
            {
                string migratedCode = currentSession.Properties["ACTIVE_RELAY_CODE"].Value;
                Debug.Log("[NetworkManagerUI] Re-joining migrated session. Using active relay code: " + migratedCode);
                
                var joinAllocation = await RelayService.Instance.JoinAllocationAsync(migratedCode);
                
                var utp = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
                if (utp != null)
                {
                    ConfigureTransport(utp, joinAllocation);
                }
            }

            // CRITICAL: Explicitly start client if not automated
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.StartClient();
            }
            
            isInGame = true;
            return true;
        }
        catch (Exception e) { Debug.LogError($"[NetworkManagerUI] Failed to join session: {e.Message}"); if (mainUIScript != null) mainUIScript.OnDisconnectDetected(); return false; }
    }

    public async Task FindLobbiesAndStartHostIfNoneExist(int playerCount)
    {
        if (mainUIScript != null) mainUIScript.OpenWaitingScreenUI("red", playerCount.ToString(), "Quick Play...");
        await EnsureServicesReady();
        try
        {
            var queryOptions = new QuerySessionsOptions 
            {
                FilterOptions = new List<FilterOption> 
                {
                    new FilterOption(FilterField.MaxPlayers, playerCount.ToString(), FilterOperation.Equal),
                    new FilterOption(FilterField.AvailableSlots, "0", FilterOperation.Greater)
                }
            };
            var queryResponse = await MultiplayerService.Instance.QuerySessionsAsync(queryOptions);
            if (queryResponse.Sessions.Count > 0)
            {
                currentSession = await MultiplayerService.Instance.JoinSessionByIdAsync(queryResponse.Sessions[0].Id);
                isInGame = true;
            }
            else await StartHostWithRelay(playerCount, false, true);
        }
        catch (Exception e) { Debug.LogError($"[NetworkManagerUI] QuickPlay failed: {e.Message}"); if (mainUIScript != null) mainUIScript.OnDisconnectDetected(); }
    }

    private void OnClientConnected(ulong clientId) { isInGame = true; }
    private void OnClientDisconnected(ulong clientId)
    {
        // If we were in a game and not the host, this might be a host drop
        if (isInGame && !NetworkManager.Singleton.IsHost && !isMigrating)
        {
            Debug.Log($"[NetworkManagerUI] NGO Disconnected (ID: {clientId}). Checking session for migration...");
            if (currentSession != null)
            {
                isMigrating = true;
                StartCoroutine(HandleHostMigrationRoutine());
                return;
            }
        }

        // Standard disconnect handling (only if not migrating)
        if (!isMigrating && (clientId == NetworkManager.Singleton.LocalClientId || clientId == NetworkManager.ServerClientId)) 
        {
            PerformDisconnect();
        }
    }

    private IEnumerator HandleHostMigrationRoutine()
    {
        Debug.Log("[NetworkManagerUI] Host migration routine started. Waiting for session update...");
        
        // Ensure NGO is shut down before we try to restart it
        if (NetworkManager.Singleton.IsListening) NetworkManager.Singleton.Shutdown();
        
        // Wait a few seconds for the Session API to elect a new host
        float timeout = 10f;
        float elapsed = 0f;
        while (elapsed < timeout)
        {
            if (currentSession != null && currentSession.IsHost)
            {
                Debug.Log("[NetworkManagerUI] I am the new session host! Starting Relay re-allocation...");
                
                // 1. Create a NEW Relay allocation
                var allocationTask = RelayService.Instance.CreateAllocationAsync(currentSession.MaxPlayers - 1);
                yield return new WaitUntil(() => allocationTask.IsCompleted);
                if (allocationTask.IsFaulted)
                {
                    Debug.LogError("[NetworkManagerUI] Relay allocation failed: " + allocationTask.Exception.Message);
                    isMigrating = false;
                    PerformDisconnect();
                    yield break;
                }
                var allocation = allocationTask.Result;

                var joinCodeTask = RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                yield return new WaitUntil(() => joinCodeTask.IsCompleted);
                var relayJoinCode = joinCodeTask.Result;
                Debug.Log("[NetworkManagerUI] New Relay Join Code generated: " + relayJoinCode);

                // 2. Update the session with the new relay code so others can find us
                var hostSession = currentSession.AsHost();
                hostSession.SetProperty("ACTIVE_RELAY_CODE", new SessionProperty(relayJoinCode, VisibilityPropertyOptions.Public));
                var saveTask = hostSession.SavePropertiesAsync();
                yield return new WaitUntil(() => saveTask.IsCompleted);

                // 3. Configure NetworkManager with the new Relay data
                var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
                var utp = transport as UnityTransport;
                if (utp != null)
                {
                    ConfigureTransport(utp, allocation);
                }
                else
                {
                    Debug.LogWarning("[NetworkManagerUI] Could not cast transport to UnityTransport. Migration might fail.");
                }

                // 4. Start as NGO Host
                NetworkManager.Singleton.StartHost();
                
                // 5. Wait for Server singleton to be ready
                yield return new WaitUntil(() => Server.Singleton != null);
                
                // 6. Apply the last known state to the new authoritative server
                if (GameManager.LocalInstance != null && GameManager.LocalInstance.lastReceivedGameState.snapshotVersion > 0)
                {
                    Debug.Log("[NetworkManagerUI] Restoring game state from last received snapshot version " + GameManager.LocalInstance.lastReceivedGameState.snapshotVersion);
                    Server.Singleton.ApplyGameStateToServer(GameManager.LocalInstance.lastReceivedGameState);
                    
                    // SELF-BINDING: Map our new host clientId to our original seat
                    if (DeckController.LocalInstance != null)
                    {
                        int mySeat = DeckController.LocalInstance.thisPlayerNumber;
                        Debug.Log($"[NetworkManagerUI] Host Self-Binding: Claiming seat {mySeat} for Host ID {NetworkManager.Singleton.LocalClientId}");
                        Server.Singleton.RebindPlayerClientId(mySeat, NetworkManager.Singleton.LocalClientId);
                        Server.Singleton.AnotherPlayerConnected(NetworkManager.Singleton.LocalClientId);
                    }
                    
                    // CRITICAL: Broadcast the restored state to all clients (including ourselves)
                    var relay = UnityEngine.Object.FindAnyObjectByType<GameNetworkRelay>();
                    if (relay != null)
                    {
                        relay.ApplyGameStateClientRPC(GameManager.LocalInstance.lastReceivedGameState);
                    }
                    
                    // Mark NGO host as ready for clients to rejoin
                    UpdateSessionHostReadyProperty();
                }
                else
                {
                    Debug.LogError($"[NetworkManagerUI] Cannot restore state: GameManager.LocalInstance or valid snapshot is missing!");
                }
                
                isMigrating = false;
                break;
            }
            
            // Refresh session to get latest properties from backend
            if (currentSession != null)
            {
                var refreshTask = currentSession.RefreshAsync();
                yield return new WaitUntil(() => refreshTask.IsCompleted);
            }

            // If we are NOT the host, check if the session property indicates the new host is ready
            if (currentSession != null && !currentSession.IsHost && currentSession.Properties.ContainsKey("ACTIVE_RELAY_CODE"))
            {
                string newRelayCode = currentSession.Properties["ACTIVE_RELAY_CODE"].Value;
                Debug.Log("[NetworkManagerUI] New Relay Join Code detected: " + newRelayCode + ". Re-connecting...");
                
                // 1. Join the NEW Relay allocation
                var joinTask = RelayService.Instance.JoinAllocationAsync(newRelayCode);
                yield return new WaitUntil(() => joinTask.IsCompleted);
                if (joinTask.IsFaulted)
                {
                    Debug.LogError("[NetworkManagerUI] Failed to join new Relay allocation: " + joinTask.Exception.Message);
                    isMigrating = false;
                    PerformDisconnect();
                    yield break;
                }
                var joinAllocation = joinTask.Result;

                // 2. Configure Transport
                var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
                var utp = transport as UnityTransport;
                if (utp != null)
                {
                    ConfigureTransport(utp, joinAllocation);
                }

                // 3. Re-join NGO as client
                StartCoroutine(RejoinNGOHostRoutine());
                yield break;
            }

            yield return new WaitForSeconds(1f);
            elapsed += 1f;
        }

        if (currentSession == null || (elapsed >= timeout && !currentSession.IsHost))
        {
            Debug.LogWarning($"[NetworkManagerUI] Host migration failed or timed out. currentSession null: {currentSession == null}, timeout: {elapsed >= timeout}, !IsHost: {!currentSession?.IsHost}");
            isMigrating = false;
            PerformDisconnect();
        }
    }

    private async void UpdateSessionHostReadyProperty()
    {
        if (currentSession == null || !currentSession.IsHost) return;
        try
        {
            // Use the host-specific session interface to modify properties
            var hostSession = currentSession.AsHost();
            hostSession.SetProperty("NGO_HOST_READY", new SessionProperty("true", VisibilityPropertyOptions.Public));
            await hostSession.SavePropertiesAsync();
            Debug.Log("[NetworkManagerUI] Session property 'NGO_HOST_READY' set to true.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[NetworkManagerUI] Failed to update session property: {e.Message}");
        }
    }

    private IEnumerator RejoinNGOHostRoutine()
    {
        // 1. Shutdown current client (if any)
        if (NetworkManager.Singleton.IsListening) NetworkManager.Singleton.Shutdown();
        yield return new WaitUntil(() => !NetworkManager.Singleton.IsListening);

        // 2. Start as client (Relay data is already in currentSession)
        NetworkManager.Singleton.StartClient();
        
        // 3. Wait for connection
        float timeout = 10f;
        float elapsed = 0f;
        while (!NetworkManager.Singleton.IsConnectedClient && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }
        
        if (!NetworkManager.Singleton.IsConnectedClient)
        {
            Debug.LogError("[NetworkManagerUI] Failed to re-join NGO server (Timeout).");
            PerformDisconnect();
            yield break;
        }
        
        // 4. Perform the Reclaim Seat handshake
        if (DeckController.LocalInstance != null)
        {
            int myOriginalSeat = DeckController.LocalInstance.thisPlayerNumber;
            Debug.Log($"[NetworkManagerUI] Re-joined! Reclaiming seat {myOriginalSeat}...");
            
            // Find the relay object - check GameManager first as it usually has it
            var relay = UnityEngine.Object.FindAnyObjectByType<GameNetworkRelay>();
            if (relay != null)
            {
                relay.ReclaimSeatServerRPC(myOriginalSeat, NetworkManager.Singleton.LocalClientId);
                
                // Request full state sync to reconstruct scene
                Debug.Log("[NetworkManagerUI] Requesting full state sync to reconstruct scene...");
                relay.RequestFullStateSyncServerRPC();
            }
        }
        isMigrating = false;
    }

    public void OnReturnToMainMenuButtonClicked() { PerformDisconnect(); }

    private async void PerformDisconnect()
    {
        isInGame = false;
        isMigrating = false;
        ResetPlayerGameStateBeforeDisconnection();
        EnsureMainScreenIsActive();
        if (DeckController.LocalInstance != null) DeckController.LocalInstance.DestroyAllCards();
        try { if (currentSession != null) { await currentSession.LeaveAsync(); currentSession = null; } }
        catch (Exception e) { Debug.LogWarning($"[NetworkManagerUI] Error leaving session: {e.Message}"); }
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) NetworkManager.Singleton.Shutdown();
        if (mainUIScript != null) mainUIScript.OnReturnFromWaitingScreen();
    }

    private void ResetPlayerGameStateBeforeDisconnection()
    {
        SuperPowerSpawner spawner = FindObjectOfType<SuperPowerSpawner>();
        if (spawner != null) { spawner.ResetGoldToStarting(); spawner.ClearAllSpawnedPowers(); }
    }

    private void EnsureMainScreenIsActive()
    {
        if (mainScreen != null) mainScreen.SetActive(true);
        if (mainUIScript != null)
        {
            var field = mainUIScript.GetType().GetField("startingScreenUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                GameObject ui = field.GetValue(mainUIScript) as GameObject;
                if (ui != null) ui.SetActive(true);
            }
        }
    }

    void SetupButtonListeners()
    {
        clientButton.onClick.AddListener(async () => await StartClientWithRelay());
        hostTwoPlayerButton.onClick.AddListener(async () => await StartHostWithRelay(2, true, false));
        hostFourPlayerButton.onClick.AddListener(async () => await StartHostWithRelay(4, true, false));
        quickPlayTwoPlayerButton.onClick.AddListener(async () => await FindLobbiesAndStartHostIfNoneExist(2));
        quickPlayFourPlayerButton.onClick.AddListener(async () => await FindLobbiesAndStartHostIfNoneExist(4));
        if (returnToMainMenuButton != null) returnToMainMenuButton.onClick.AddListener(OnReturnToMainMenuButtonClicked);
    }

    private void ConfigureTransport(UnityTransport utp, Allocation allocation)
    {
        string preferredType = utp.UseWebSockets ? "wss" : "dtls";
        string fallbackType = utp.UseWebSockets ? "ws" : "udp";

        var endpoint = allocation.ServerEndpoints.FirstOrDefault(e => e.ConnectionType == preferredType) ??
                       allocation.ServerEndpoints.FirstOrDefault(e => e.ConnectionType == fallbackType);
        if (endpoint == null)
        {
            Debug.LogError("[NetworkManagerUI] No compatible relay endpoint found for host migration.");
            return;
        }

        bool isWebSocket = endpoint.ConnectionType == "ws" || endpoint.ConnectionType == "wss";
        bool isSecure = endpoint.Secure;

        // Keep transport interface aligned with selected relay endpoint.
        utp.UseWebSockets = isWebSocket;

        var relayServerData = new RelayServerData(
            endpoint.Host,
            (ushort)endpoint.Port,
            allocation.AllocationIdBytes,
            allocation.ConnectionData,
            allocation.ConnectionData,
            allocation.Key,
            isSecure,
            isWebSocket
        );
        utp.SetRelayServerData(relayServerData);
        Debug.Log($"[NetworkManagerUI] Host relay configured: type={endpoint.ConnectionType}, secure={isSecure}, webSocket={isWebSocket}");
    }

    private void ConfigureTransport(UnityTransport utp, JoinAllocation allocation)
    {
        string preferredType = utp.UseWebSockets ? "wss" : "dtls";
        string fallbackType = utp.UseWebSockets ? "ws" : "udp";

        var endpoint = allocation.ServerEndpoints.FirstOrDefault(e => e.ConnectionType == preferredType) ??
                       allocation.ServerEndpoints.FirstOrDefault(e => e.ConnectionType == fallbackType);
        if (endpoint == null)
        {
            Debug.LogError("[NetworkManagerUI] No compatible relay endpoint found for client migration.");
            return;
        }

        bool isWebSocket = endpoint.ConnectionType == "ws" || endpoint.ConnectionType == "wss";
        bool isSecure = endpoint.Secure;

        // Keep transport interface aligned with selected relay endpoint.
        utp.UseWebSockets = isWebSocket;

        var relayServerData = new RelayServerData(
            endpoint.Host,
            (ushort)endpoint.Port,
            allocation.AllocationIdBytes,
            allocation.ConnectionData,
            allocation.HostConnectionData,
            allocation.Key,
            isSecure,
            isWebSocket
        );
        utp.SetRelayServerData(relayServerData);
        Debug.Log($"[NetworkManagerUI] Client relay configured: type={endpoint.ConnectionType}, secure={isSecure}, webSocket={isWebSocket}");
    }
}
