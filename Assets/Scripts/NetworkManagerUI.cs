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
    private const string SeatMapPropertyKey = "SEAT_MAP";

    [Serializable]
    private class SeatMapPayload
    {
        public int version = 1;
        public List<SeatBinding> seats = new List<SeatBinding>();
    }

    [Serializable]
    private class SeatBinding
    {
        public int seat;
        public string ownerPlayerId;
        public string clientId;
    }

    private readonly struct SeatOwner
    {
        public string OwnerPlayerId { get; }
        public ulong ClientId { get; }

        public SeatOwner(string ownerPlayerId, ulong clientId)
        {
            OwnerPlayerId = ownerPlayerId;
            ClientId = clientId;
        }
    }

    private static class SeatMapSerializer
    {
        public static string Serialize(Dictionary<int, SeatOwner> seatMap)
        {
            var payload = new SeatMapPayload();
            if (seatMap != null)
            {
                payload.seats = seatMap
                    .OrderBy(entry => entry.Key)
                    .Select(entry => new SeatBinding
                    {
                        seat = entry.Key,
                        ownerPlayerId = entry.Value.OwnerPlayerId,
                        clientId = entry.Value.ClientId.ToString()
                    })
                    .ToList();
            }

            return JsonUtility.ToJson(payload);
        }
    }

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
    private bool isFlowBusy = false;
    private SerializableGameState localMigrationSnapshot;

    // === SESSIONS API ===
    private ISession currentSession;
    public ISession CurrentSession => currentSession;
    public bool IsMigrating => isMigrating;

    [Serializable]
    public struct ServerTruths
    {
        public int seed;
        public int[] points;
        public int[] pistiCounts;
        public int turnCounter;
        public int roundCount;
        public int lastPlayerToCapture;
        public int currentPlayer;
        public int startingPlayerNo;
        public List<Server.PlayerGoldEntry> playerGolds;
        public List<Server.PlayerPowersEntry> playerSuperPowers;
    }

    private SerializableGameState Merge(ServerTruths truths, SerializableGameState snapshot)
    {
        snapshot.seed = truths.seed;
        
        // Prioritize local real-time captured turn state over stale cloud truths
        if (snapshot.turnCounter == 0)
        {
            snapshot.turnCounter = truths.turnCounter;
            snapshot.currentPlayer = truths.currentPlayer;
        }
        else
        {
            
        }

        snapshot.roundCount = truths.roundCount;
        snapshot.lastPlayerToCapture = truths.lastPlayerToCapture;
        snapshot.points = new SerializableIntArray(truths.points);
        snapshot.pistiCounts = new SerializableIntArray(truths.pistiCounts);
        snapshot.startingPlayerNo = truths.startingPlayerNo;

        // Restore player gold dictionaries from ServerTruths during migration merge
        if (truths.playerGolds != null && truths.playerGolds.Count > 0)
        {
            var goldDict = new Dictionary<int, int>();
            foreach (var entry in truths.playerGolds)
            {
                goldDict[entry.playerNo] = entry.gold;
            }
            snapshot.playerGold = new SerializableIntDictionary(goldDict);
            
        }

        // Restore player superpowers from ServerTruths during migration merge
        if (truths.playerSuperPowers != null && truths.playerSuperPowers.Count > 0)
        {
            var powersDict = new Dictionary<int, List<string>>();
            foreach (var entry in truths.playerSuperPowers)
            {
                powersDict[entry.playerNo] = new List<string>(entry.powers);
            }
            snapshot.playerSuperPowers = new SerializableDictionary(powersDict);
            
        }

        return snapshot;
    }

    public ServerTruths ReadServerTruthsFromSession()
    {
        if (currentSession != null && currentSession.Properties.ContainsKey("GAME_META"))
        {
            string json = currentSession.Properties["GAME_META"].Value;
            
            return JsonUtility.FromJson<ServerTruths>(json);
        }
        
        return default;
    }

    // === UI REFERENCES ===
    private GameObject mainScreen;

    void Awake()
    {
        SetupButtonListeners();
    }

    private void InitializeMainScreenReference()
    {
        mainScreen = GameObject.Find("MainScreen");
    }

    async void Start()
    {
        InitializeMainScreenReference();
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            // Platform auto-configuration for WebSockets vs UDP/DTLS
            var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            var utp = transport as UnityTransport;
            if (utp != null)
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                utp.UseWebSockets = true;
                #else
                utp.UseWebSockets = false;
                #endif
                
            }
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
        catch (Exception e) {  }
    }

    private bool TryBeginFlow(string flowName)
    {
        if (isFlowBusy)
        {
            
            return false;
        }

        isFlowBusy = true;
        
        return true;
    }

    private void EndFlow(string flowName)
    {
        isFlowBusy = false;
        
    }

    public async Task<string> StartNewHost(int playerCount, bool isPrivate)
    {
        const string flowName = "StartNewHost";
        if (!TryBeginFlow(flowName)) return null;

        try
        {
            return await StartHostWithRelay(playerCount, isPrivate, false);
        }
        finally
        {
            EndFlow(flowName);
        }
    }

    public async Task<bool> StartNewClient(string joinCode)
    {
        const string flowName = "StartNewClient";
        if (!TryBeginFlow(flowName)) return false;

        try
        {
            return await StartClientWithRelay(joinCode);
        }
        finally
        {
            EndFlow(flowName);
        }
    }

    public void AttemptReconnect()
    {
        if (NetworkManager.Singleton == null)
        {
            
            return;
        }

        
        StartCoroutine(RejoinNGOHostRoutine());
    }

    private async Task PersistInitialSeatMapAsync()
    {
        if (currentSession == null || !currentSession.IsHost) return;

        string ownerPlayerId = AuthenticationService.Instance.PlayerId;
        if (string.IsNullOrEmpty(ownerPlayerId))
        {
            
            return;
        }

        int hostSeat = 0;
        if (DeckController.LocalInstance != null && DeckController.LocalInstance.thisPlayerNumber >= 0)
        {
            hostSeat = DeckController.LocalInstance.thisPlayerNumber;
        }

        ulong hostClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL;
        var seatMap = new Dictionary<int, SeatOwner>
        {
            [hostSeat] = new SeatOwner(ownerPlayerId, hostClientId)
        };

        string serializedSeatMap = SeatMapSerializer.Serialize(seatMap);

        try
        {
            var hostSession = currentSession.AsHost();
            hostSession.SetProperty(SeatMapPropertyKey, new SessionProperty(serializedSeatMap, VisibilityPropertyOptions.Public));
            await hostSession.SavePropertiesAsync();
            
        }
        catch (Exception e)
        {
            
        }
    }

    public async Task<string> StartHostWithRelay(int playerCount, bool privateFlag, bool isQuickPlay = false)
    {
        if (Server.Singleton != null) 
        {
            Server.Singleton.ResetAllServerVariables();
            Server.Singleton.SetBotModeEnabled(isQuickPlay);
        }
        if (GameManager.LocalInstance != null) GameManager.LocalInstance.ResetForNewGame();

        if (privateFlag && mainUIScript != null)
        {
            mainUIScript.OpenWaitingScreenUI("blue", playerCount.ToString(), "YÜKLENİYOR");
        }

        await EnsureServicesReady();
        try
        {
            var options = new SessionOptions { MaxPlayers = playerCount, IsPrivate = privateFlag }.WithRelayNetwork();
            currentSession = await MultiplayerService.Instance.CreateSessionAsync(options);
            await PersistInitialSeatMapAsync();
            joinCodeText.text = currentSession.Code;
            if (inputField != null) inputField.text = currentSession.Code;
            PlayerPrefs.SetString("LastSessionCode", currentSession.Code);
            PlayerPrefs.Save();
            if (mainUIScript != null) mainUIScript.OpenWaitingScreenUI(privateFlag ? "blue" : "red", playerCount.ToString(), currentSession.Code);
            if (Server.Singleton != null) Server.Singleton.SetPlayerCount(playerCount);
            isInGame = true;
            return currentSession.Code;
        }
        catch (Exception e) 
        { 
             
            if (privateFlag && mainUIScript != null)
            {
                mainUIScript.OnReturnFromWaitingScreen();
            }
            return null; 
        }
    }

    public async Task<bool> StartClientWithRelay(string joinCodeOverride = null)
    {
        string inputJoinCode = string.IsNullOrWhiteSpace(joinCodeOverride)
            ? (inputField != null ? inputField.text : string.Empty)
            : joinCodeOverride.Trim();

        if (inputField != null && !string.IsNullOrWhiteSpace(joinCodeOverride))
        {
            inputField.text = inputJoinCode;
        }

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
        catch (Exception e) {  return false; }
    }

    public async Task FindLobbiesAndStartHostIfNoneExist(int playerCount)
    {
        if (mainUIScript != null) mainUIScript.OpenWaitingScreenUI("red", playerCount.ToString(), "Quick Play...");
        await EnsureServicesReady();
        try
        {
            // For quickplay with bot mode enabled, directly start a bot game without querying other sessions
            // This ensures that when the player wants to debug with bots, they always get a bot opponent
            
            await StartHostWithRelay(playerCount, false, true);
        }
        catch (Exception e) {  }
    }

    private void OnClientConnected(ulong clientId) { isInGame = true; }
    private void OnClientDisconnected(ulong clientId)
    {
        // If we were in a game and not the host, this might be a host drop
        if (isInGame && !NetworkManager.Singleton.IsHost && !isMigrating)
        {
            
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
        

        // 1. Pull metadata from Session
        ServerTruths truths = ReadServerTruthsFromSession();

        // 2. Capture visual snapshot from scene
        if (GameManager.LocalInstance != null)
        {
            localMigrationSnapshot = GameManager.LocalInstance.CaptureVisualSnapshot();

            // 3. Merge
            localMigrationSnapshot = Merge(truths, localMigrationSnapshot);
            
        }
        else
        {
            
            localMigrationSnapshot = default;
        }

        // 4. Shutdown NGO
        if (NetworkManager.Singleton.IsListening) NetworkManager.Singleton.Shutdown();
        yield return new WaitUntil(() => !NetworkManager.Singleton.IsListening);

        // A7: Staggered start based on seat index to prevent simultaneous promotion attempts
        int mySeat = DeckController.LocalInstance != null ? DeckController.LocalInstance.thisPlayerNumber : 0;
        float staggerDelay = mySeat * 1.5f;
        
        yield return new WaitForSeconds(staggerDelay);

        // Wait a few seconds for the Session API to elect a new host
        float timeout = 15f; // Increased timeout for staggered starts
        float elapsed = 0f;
        while (elapsed < timeout)
        {
            if (currentSession != null && currentSession.IsHost)
            {
                yield return StartCoroutine(HandleHostMigrationAsNewHost());
                yield break;
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
                
                
                // 1. Join the NEW Relay allocation
                var joinTask = RelayService.Instance.JoinAllocationAsync(newRelayCode);
                yield return new WaitUntil(() => joinTask.IsCompleted);
                if (joinTask.IsFaulted)
                {
                    
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
                AttemptReconnect();
                yield break;
            }

            yield return new WaitForSeconds(1f);
            elapsed += 1f;
        }

        if (currentSession == null || (elapsed >= timeout && !currentSession.IsHost))
        {
            
            isMigrating = false;
            PerformDisconnect();
        }
    }

    private IEnumerator HandleHostMigrationAsNewHost()
    {
        

        var allocationTask = RelayService.Instance.CreateAllocationAsync(currentSession.MaxPlayers - 1);
        yield return new WaitUntil(() => allocationTask.IsCompleted);
        if (allocationTask.IsFaulted)
        {
            
            isMigrating = false;
            PerformDisconnect();
            yield break;
        }
        var allocation = allocationTask.Result;

        var joinCodeTask = RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        yield return new WaitUntil(() => joinCodeTask.IsCompleted);
        var relayJoinCode = joinCodeTask.Result;
        

        var hostSession = currentSession.AsHost();
        hostSession.SetProperty("ACTIVE_RELAY_CODE", new SessionProperty(relayJoinCode, VisibilityPropertyOptions.Public));
        var saveTask = hostSession.SavePropertiesAsync();
        yield return new WaitUntil(() => saveTask.IsCompleted);

        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        var utp = transport as UnityTransport;
        if (utp != null)
        {
            ConfigureTransport(utp, allocation);
        }
        else
        {
            
        }

        NetworkManager.Singleton.StartHost();
        yield return new WaitUntil(() => Server.Singleton != null);

        bool restoreSuccess = false;
        if (localMigrationSnapshot.snapshotVersion > 0)
        {
            
            restoreSuccess = Server.Singleton.RestoreFromSnapshot(localMigrationSnapshot);
        }
        else
        {
            
            var migrationSnapshot = Server.Singleton.CaptureMigrationSnapshot();
            restoreSuccess = Server.Singleton.RestoreFromSnapshot(migrationSnapshot);
        }

        if (!restoreSuccess)
        {
            
            isMigrating = false;
            PerformDisconnect();
            yield break;
        }

        

        int mySeat = -1;
        if (DeckController.LocalInstance != null)
        {
            mySeat = DeckController.LocalInstance.thisPlayerNumber;
        }

        
        Server.Singleton.HandleMigratedHostSelfBind(mySeat, NetworkManager.Singleton.LocalClientId);

        UpdateSessionHostReadyProperty();
        isMigrating = false;
        
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
            
        }
        catch (Exception e)
        {
            
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
            
            PerformDisconnect();
            yield break;
        }
        
        // 4. Perform the Reclaim Seat handshake.
        if (DeckController.LocalInstance != null)
        {
            // Prefer the live, safe in-memory RAM seat over the shared registry PlayerPrefs
            // to support seamless local multi-tab testing on a single computer.
            int myOriginalSeat = DeckController.LocalInstance.thisPlayerNumber;
            if (myOriginalSeat < 0) myOriginalSeat = PlayerPrefs.GetInt("SavedPlayerSeat", -1);
            
            
            // Find the relay object - check GameManager first as it usually has it
            var relay = UnityEngine.Object.FindAnyObjectByType<GameNetworkRelay>();
            if (relay != null)
            {
                // ReclaimSeatServerRPC starts the TARGETED reconnect handshake: the server
                // sends only this client its snapshot once it confirms its cards are ready.
                // Do NOT call RequestFullStateSyncServerRPC here, that is a global broadcast
                // which resets every other client's scene.
                relay.ReclaimSeatServerRPC(myOriginalSeat, NetworkManager.Singleton.LocalClientId, AuthenticationService.Instance.PlayerId);
            }
        }
        isMigrating = false;
    }

    public void OnReturnToMainMenuButtonClicked() { PerformDisconnect(); }

    private async void PerformDisconnect()
    {
        isInGame = false;
        isMigrating = false;

        // SESSION TEARDOWN: clear all session-scoped reconnect/seat state so the NEXT lobby
        // starts from the clean fresh-join path. Without this, a finished game can leave a
        // saved seat or reconnect tracking behind and the next game is wrongly treated as a
        // reconnection.
        if (Server.Singleton != null) Server.Singleton.ResetSessionStateForTeardown();
        PlayerPrefs.DeleteKey("SavedPlayerSeat");
        PlayerPrefs.Save();

        ResetPlayerGameStateBeforeDisconnection();
        EnsureMainScreenIsActive();
        if (DeckController.LocalInstance != null) DeckController.LocalInstance.DestroyAllCards();
        try { if (currentSession != null) { await currentSession.LeaveAsync(); currentSession = null; } }
        catch (Exception e) {  }
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) NetworkManager.Singleton.Shutdown();
        if (mainUIScript != null) mainUIScript.OnReturnFromWaitingScreen();
    }

    private void ResetPlayerGameStateBeforeDisconnection()
    {
        SuperPowerSpawner spawner = FindFirstObjectByType<SuperPowerSpawner>();
        if (spawner != null)
        {
            spawner.isDisconnectingCleanUp = true;
            spawner.ResetGoldToStarting();
            spawner.ClearAllSpawnedPowers();
        }
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
        clientButton.onClick.AddListener(async () => await StartNewClient(inputField != null ? inputField.text : string.Empty));
        hostTwoPlayerButton.onClick.AddListener(async () => await StartNewHost(2, true));
        hostFourPlayerButton.onClick.AddListener(async () => await StartNewHost(4, true));
        // 1v1 quickplay now opens single player mode instead of bot mode
        quickPlayTwoPlayerButton.onClick.AddListener(OnSinglePlayerQuickPlayButtonClicked);
        quickPlayFourPlayerButton.onClick.AddListener(async () => await FindLobbiesAndStartHostIfNoneExist(4));
        if (returnToMainMenuButton != null) returnToMainMenuButton.onClick.AddListener(OnReturnToMainMenuButtonClicked);
    }

    /// <summary>
    /// Called when the 1v1 Quickplay button is pressed.
    /// Opens the single player run settings panel instead of launching a bot game directly.
    /// </summary>
    private void OnSinglePlayerQuickPlayButtonClicked()
    {
        
        
        // Get reference to MainUIScript to open run settings panel
        MainUIScript mainUI = FindObjectOfType<MainUIScript>();
        if (mainUI != null)
        {
            // The run settings panel should already be part of quickPlayUI structure
            // MainUIScript will handle opening it
            mainUI.ShowRunSettingsPanel();
        }
        else
        {
            
        }
    }

    private void ConfigureTransport(UnityTransport utp, Allocation allocation)
    {
        string preferredType = utp.UseWebSockets ? "wss" : "dtls";
        string fallbackType = utp.UseWebSockets ? "ws" : "udp";

        var endpoint = allocation.ServerEndpoints.FirstOrDefault(e => e.ConnectionType == preferredType) ??
                       allocation.ServerEndpoints.FirstOrDefault(e => e.ConnectionType == fallbackType);
        if (endpoint == null)
        {
            
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
        
    }

    private void ConfigureTransport(UnityTransport utp, JoinAllocation allocation)
    {
        string preferredType = utp.UseWebSockets ? "wss" : "dtls";
        string fallbackType = utp.UseWebSockets ? "ws" : "udp";

        var endpoint = allocation.ServerEndpoints.FirstOrDefault(e => e.ConnectionType == preferredType) ??
                       allocation.ServerEndpoints.FirstOrDefault(e => e.ConnectionType == fallbackType);
        if (endpoint == null)
        {
            
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
        
    }
}
