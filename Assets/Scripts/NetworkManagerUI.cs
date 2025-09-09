using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;
#if UNITY_EDITOR
using ParrelSync;
#endif

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
    private string joinCodeVar;

    // === DISCONNECT DETECTION & RECONNECTION ===
    [Header("Disconnect Detection")]
    [SerializeField] private float heartbeatInterval = 10f; // Send heartbeat every 10 seconds (less aggressive)
    [SerializeField] private float disconnectTimeout = 30f; // Consider disconnected after 30 seconds without heartbeat (more lenient)
    [SerializeField] private bool enableAutomaticReconnection = true;
    
    private string lastGameJoined = ""; // Current/last game join code
    private bool isInGame = false; // Whether we're currently in an active game
    private float lastHeartbeatTime = 0f;
    private bool isHeartbeatActive = false;
    private Coroutine heartbeatCoroutine;
    private Coroutine disconnectCheckCoroutine;
    
    // === CLIENT RELAY KEEP-ALIVE ===
    private Coroutine clientRelayKeepAliveCoroutine;
    private bool isClientRelayKeepAliveActive = false;
    
    // === DEBUG CHAIN ===
    private List<string> reconnectionDebugChain = new List<string>();
    
    public Lobby currentLobby; // Store the current lobby when you join/create it
    public Lobby CurrentLobby { get { return currentLobby; } } // Public access for Server

    // === PLAYER PREFERENCES KEYS ===
    private string LAST_JOIN_CODE_KEY;
    private string LAST_PLAYER_COUNT_KEY;
    private string LAST_GAME_TIMESTAMP_KEY;

    void Awake()
    {
        // Initialize ParrelSync-aware PlayerPrefs keys
        InitializePlayerPrefsKeys();
        
        // Load last game info from PlayerPrefs
        LoadLastGameInfo();
        
        // Setup button listeners
        SetupButtonListeners();
    }

    /// <summary>
    /// Initializes PlayerPrefs keys with ParrelSync-aware unique identifiers
    /// </summary>
    private void InitializePlayerPrefsKeys()
    {
        string uniqueId = GetUniquePlayerId();
        LAST_JOIN_CODE_KEY = $"LastGameJoinCode_{uniqueId}";
        LAST_PLAYER_COUNT_KEY = $"LastPlayerCount_{uniqueId}";
        LAST_GAME_TIMESTAMP_KEY = $"LastGameTimestamp_{uniqueId}";
        
        // Debug.Log($"[NetworkManagerUI] Initialized PlayerPrefs keys with unique ID: {uniqueId}");
    }

    void Start()
    {
        // Subscribe to network events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        }
        
        // TEMPORARILY DISABLED: Heartbeat system might be causing disconnections
        // TODO: Re-enable this once normal game flow is working properly
        // if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsHost)
        // {
        //     StartDisconnectDetection();
        // }
        
        // RE-ENABLED: Auto-initialize Unity Services on startup (as originally requested)
        // This ensures Unity Services is ready immediately when game opens
        StartCoroutine(AutoInitializeUnityServicesOnStartup());
        
        // RE-ENABLED: Check for saved games and attempt reconnection (after Unity Services is ready)
        StartCoroutine(CheckForSavedGameAndReconnect());
    }

    void OnDestroy()
    {
        // Unsubscribe from network events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
        
        // Stop coroutines
        StopDisconnectDetection();
        StopClientRelayKeepAlive();
    }

    // === DISCONNECT DETECTION & HEARTBEAT ===
    
    /// <summary>
    /// Starts the disconnect detection system
    /// </summary>
    private void StartDisconnectDetection()
    {
        if (isHeartbeatActive) return;
        
        isHeartbeatActive = true;
        heartbeatCoroutine = StartCoroutine(HeartbeatCoroutine());
        disconnectCheckCoroutine = StartCoroutine(DisconnectCheckCoroutine());
        
        // Debug.Log("[NetworkManagerUI] Disconnect detection started");
    }

    /// <summary>
    /// Stops the disconnect detection system
    /// </summary>
    private void StopDisconnectDetection()
    {
        if (!isHeartbeatActive) return;
        
        isHeartbeatActive = false;
        
        if (heartbeatCoroutine != null)
        {
            StopCoroutine(heartbeatCoroutine);
            heartbeatCoroutine = null;
        }
        
        if (disconnectCheckCoroutine != null)
        {
            StopCoroutine(disconnectCheckCoroutine);
            disconnectCheckCoroutine = null;
        }
        
        // Debug.Log("[NetworkManagerUI] Disconnect detection stopped");
    }

    /// <summary>
    /// Coroutine that sends periodic heartbeats to the server
    /// </summary>
    private IEnumerator HeartbeatCoroutine()
    {
        while (isHeartbeatActive)
        {
            // CRITICAL FIX: Only clients should send heartbeats, not the host
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient && !NetworkManager.Singleton.IsHost)
            {
                SendHeartbeat();
            }
            yield return new WaitForSeconds(heartbeatInterval);
        }
    }

    /// <summary>
    /// Coroutine that checks for disconnections based on heartbeat timeouts
    /// </summary>
    private IEnumerator DisconnectCheckCoroutine()
    {
        while (isHeartbeatActive)
        {
            // CRITICAL FIX: Only clients should check heartbeat timeouts, not the host
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient && !NetworkManager.Singleton.IsHost)
            {
                CheckHeartbeatTimeout();
            }
            yield return new WaitForSeconds(1f); // Check every second
        }
    }

    /// <summary>
    /// Sends a heartbeat to the server to indicate we're still connected
    /// </summary>
    private void SendHeartbeat()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            // Send heartbeat RPC to server
            var networkRelay = FindObjectOfType<NetworkRelay>();
            if (networkRelay != null)
            {
                networkRelay.SendHeartbeatServerRPC();
                lastHeartbeatTime = Time.time;
                // Debug.Log($"[NetworkManagerUI] Heartbeat sent at {Time.time:F2}");
            }
        }
    }

    /// <summary>
    /// Checks if we've missed heartbeats and should consider ourselves disconnected
    /// </summary>
    private void CheckHeartbeatTimeout()
    {
        if (Time.time - lastHeartbeatTime > disconnectTimeout)
        {
            // Debug.LogWarning($"[NetworkManagerUI] Heartbeat timeout detected! Last heartbeat: {lastHeartbeatTime:F2}, Current time: {Time.time:F2}");
            OnDisconnectDetected("Heartbeat timeout");
        }
    }

    /// <summary>
    /// Called when a disconnect is detected (either through events or heartbeat timeout)
    /// </summary>
    private void OnDisconnectDetected(string reason)
    {
        // Debug.LogWarning($"[NetworkManagerUI] Disconnect detected: {reason}");
        
        // Stop disconnect detection
        StopDisconnectDetection();
        
        // Mark as disconnected
        isInGame = false;
        
        // CRITICAL FIX: Only clients should attempt automatic reconnection
        // Hosts should NOT try to reconnect to their own lobby
        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        
        if (!isHost && enableAutomaticReconnection && !string.IsNullOrEmpty(lastGameJoined))
        {
            // Debug.Log($"[NetworkManagerUI] Client attempting automatic reconnection to: {lastGameJoined}");
            StartCoroutine(AttemptReconnection());
        }
        else if (isHost)
        {
            // Debug.Log("[NetworkManagerUI] Host detected disconnect - waiting for client to reconnect");
            // Host should just return to main page, not try to reconnect
            if (mainUIScript != null)
            {
                mainUIScript.OnDisconnectDetected();
            }
        }
        else
        {
            // Debug.Log("[NetworkManagerUI] No automatic reconnection - returning to main page");
            // Notify MainUIScript to return to main page
            if (mainUIScript != null)
            {
                mainUIScript.OnDisconnectDetected();
            }
        }
    }

    // === NETWORK EVENT HANDLERS ===
    
    /// <summary>
    /// Called when a client connects to the network
    /// </summary>
    private void OnClientConnected(ulong clientId)
    {
        // Debug.Log($"[NetworkManagerUI] Client connected: {clientId}");
        
        // Mark as in game
        isInGame = true;
        
        // Note: Disconnect detection is already started in Start() for clients
        // No need to start it again here
    }

    /// <summary>
    /// Called when a client disconnects from the network
    /// </summary>
    private void OnClientDisconnected(ulong clientId)
    {
        // Debug.LogWarning($"[NetworkManagerUI] Client disconnected: {clientId}");
        OnDisconnectDetected("Network disconnect event");
    }

    /// <summary>
    /// Called when the server starts
    /// </summary>
    private void OnServerStarted()
    {
        // Debug.Log("[NetworkManagerUI] Server started");
        
        // CRITICAL FIX: Host should NOT start disconnect detection
        // Host doesn't need to send heartbeats or detect disconnections
        // Only clients need this functionality
        
        isInGame = true;
    }

    // === AUTO-INITIALIZATION & SAVED GAME CHECK ===
    
    /// <summary>
    /// Checks if Unity Services is already initialized
    /// </summary>
    private bool IsUnityServicesInitialized()
    {
        try
        {
            // Try to access AuthenticationService - if it throws an exception, services aren't initialized
            var playerId = AuthenticationService.Instance.PlayerId;
            return !string.IsNullOrEmpty(playerId);
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Auto-initializes Unity Services on startup (without trying to reconnect to saved games)
    /// </summary>
    private IEnumerator AutoInitializeUnityServicesOnStartup()
    {
        // Debug.Log("[NetworkManagerUI] Auto-initializing Unity Services on startup...");
        
        // Check if Unity Services is already initialized
        if (IsUnityServicesInitialized())
        {
            // Debug.Log("[NetworkManagerUI] Unity Services already initialized - skipping");
            yield break;
        }
        
        // Initialize Unity Services in the background
        bool initializationComplete = false;
        bool initializationSuccess = false;
        
        StartCoroutine(InitializeUnityServicesCoroutine((success) => {
            initializationSuccess = success;
            initializationComplete = true;
        }));
        
        // Wait for initialization to complete
        while (!initializationComplete)
        {
            yield return null;
        }
        
        if (initializationSuccess)
        {
            // Debug.Log("[NetworkManagerUI] Unity Services auto-initialized successfully");
        }
        else
        {
            // Debug.LogWarning("[NetworkManagerUI] Unity Services auto-initialization failed");
        }
    }
    
    /// <summary>
    /// Coroutine wrapper for Unity Services initialization
    /// </summary>
    private IEnumerator InitializeUnityServicesCoroutine(System.Action<bool> callback)
    {
        // Initialize Unity Services
        // Debug.Log("[NetworkManagerUI] Initializing Unity Services...");
        var initialOptions = new InitializationOptions();
        
        #if UNITY_EDITOR
        if (ClonesManager.IsClone())
        {
            string parrelArgument = ClonesManager.GetArgument();
            initialOptions.SetProfile(parrelArgument);
            // Debug.Log("ParrelSync argument: " + parrelArgument);
        }
        #endif
        
        var initTask = UnityServices.InitializeAsync(initialOptions);
        while (!initTask.IsCompleted)
        {
            yield return null;
        }
        
        if (initTask.IsFaulted)
        {
            Debug.LogError($"[NetworkManagerUI] Unity Services initialization failed: {initTask.Exception}");
            callback?.Invoke(false);
            yield break;
        }
        
        // Debug.Log("[NetworkManagerUI] Unity Services initialized successfully");
        
        // Sign in anonymously
        Debug.Log("[NetworkManagerUI] Signing in anonymously...");
        var signInTask = EnsureFreshAnonymousSignIn();
        while (!signInTask.IsCompleted)
        {
            yield return null;
        }
        
        if (signInTask.IsFaulted)
        {
            Debug.LogError($"[NetworkManagerUI] Anonymous sign-in failed: {signInTask.Exception}");
            callback?.Invoke(false);
            yield break;
        }
        
        // Debug.Log("[NetworkManagerUI] Anonymous sign-in completed");
        callback?.Invoke(true);
    }
    
    /// <summary>
    /// Checks for saved games and attempts reconnection (runs after Unity Services is initialized)
    /// </summary>
    private IEnumerator CheckForSavedGameAndReconnect()
    {
        // Wait for Unity Services to be initialized
        while (!IsUnityServicesInitialized())
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        // Add a small delay to ensure everything is properly initialized
        yield return new WaitForSeconds(1f);
        
        // Debug.Log("[NetworkManagerUI] Unity Services ready, checking for saved games...");
        
        // Check if we have a saved join code
        if (string.IsNullOrEmpty(lastGameJoined))
        {
            // Debug.Log("[NetworkManagerUI] No saved game found - ready for manual connection");
            yield break;
        }
        
        // CRITICAL: Don't auto-reconnect if we're already connected to a game
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            // Debug.Log("[NetworkManagerUI] Already connected to a game - skipping auto-reconnection");
            yield break;
        }
        
        // Debug.Log($"[NetworkManagerUI] Found saved game: {lastGameJoined} - attempting reconnection...");
        
        // Show connecting UI to user - use the saved player count
        int lastPlayerCount = PlayerPrefs.GetInt(LAST_PLAYER_COUNT_KEY, 2);
        if (mainUIScript != null)
        {
            mainUIScript.OpenWaitingScreenUI("yellow", lastPlayerCount.ToString(), "Reconnecting...");
        }
        
        // Attempt reconnection
        bool reconnectionComplete = false;
        bool reconnectionSuccess = false;
        
        StartCoroutine(AttemptReconnectionCoroutine(lastGameJoined, (success) => {
            reconnectionSuccess = success;
            reconnectionComplete = true;
        }));
        
        // Wait for reconnection attempt to complete
        while (!reconnectionComplete)
        {
            yield return null;
        }
        
        if (reconnectionSuccess)
        {
            // Debug.Log("[NetworkManagerUI] Auto-reconnection successful! Connected to lobby only (step-by-step testing)");
            
            // STOP HERE - Don't close waiting screen or request game state sync
            // The UI will show the lobby code instead of "Reconnecting..."
        }
        else
        {
            // Debug.LogWarning("[NetworkManagerUI] Auto-reconnection failed - returning to main menu");
            ClearSavedGameInfo();
            
            // Return to main page
            if (mainUIScript != null)
            {
                mainUIScript.OnDisconnectDetected();
            }
        }
    }
    
    /// <summary>
    /// Automatically initializes Unity Services and checks for saved game on startup
    /// </summary>
    private IEnumerator AutoInitializeUnityServicesAndCheckForSavedGame()
    {
        Debug.Log("[NetworkManagerUI] Starting auto-initialization of Unity Services...");
        
        // Check if we already have a saved join code
        if (string.IsNullOrEmpty(lastGameJoined))
        {
            Debug.Log("[NetworkManagerUI] No saved game found - proceeding to main menu");
            yield break; // No saved game, just show main menu
        }
        
        // CRITICAL: Don't auto-reconnect if we're already connected to a game
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            // Debug.Log("[NetworkManagerUI] Already connected to a game - skipping auto-reconnection");
            yield break;
        }
        
        // Show connecting UI to user
        if (mainUIScript != null)
        {
            mainUIScript.OpenWaitingScreenUI("yellow", "2", "Reconnecting...");
        }
        
        // Use a flag to track completion since we can't use await in coroutine
        bool initializationComplete = false;
        bool initializationSuccess = false;
        
        // Start the async initialization
        StartCoroutine(AutoInitializeCoroutine((success) => {
            initializationSuccess = success;
            initializationComplete = true;
        }));
        
        // Wait for initialization to complete
        while (!initializationComplete)
        {
            yield return null;
        }
        
        if (initializationSuccess)
        {
            Debug.Log("[NetworkManagerUI] Auto-reconnection successful! Going to game...");
            // The game will continue from where it left off
        }
        else
        {
            // Debug.LogWarning("[NetworkManagerUI] Auto-reconnection failed - returning to main menu");
            ClearSavedGameInfo();
            
            // Return to main page
            if (mainUIScript != null)
            {
                mainUIScript.OnDisconnectDetected();
            }
        }
    }
    
    /// <summary>
    /// Coroutine wrapper for the async auto-initialization
    /// </summary>
    private IEnumerator AutoInitializeCoroutine(System.Action<bool> callback)
    {
        bool initializationSuccess = false;
        Exception caughtException = null;
        
        // Step 1: Initialize Unity Services
        Debug.Log("[NetworkManagerUI] Initializing Unity Services...");
        var initialOptions = new InitializationOptions();
        
        #if UNITY_EDITOR
        if (ClonesManager.IsClone())
        {
            string parrelArgument = ClonesManager.GetArgument();
            initialOptions.SetProfile(parrelArgument);
            // Debug.Log("ParrelSync argument: " + parrelArgument);
        }
        #endif
        
        var initTask = UnityServices.InitializeAsync(initialOptions);
        while (!initTask.IsCompleted)
        {
            yield return null;
        }
        
        if (initTask.IsFaulted)
        {
            Debug.LogError($"[NetworkManagerUI] Unity Services initialization failed: {initTask.Exception}");
            callback?.Invoke(false);
            yield break;
        }
        
        // Debug.Log("[NetworkManagerUI] Unity Services initialized successfully");
        
        // Step 2: Sign in anonymously
        Debug.Log("[NetworkManagerUI] Signing in anonymously...");
        var signInTask = EnsureFreshAnonymousSignIn();
        while (!signInTask.IsCompleted)
        {
            yield return null;
        }
        
        if (signInTask.IsFaulted)
        {
            Debug.LogError($"[NetworkManagerUI] Anonymous sign-in failed: {signInTask.Exception}");
            callback?.Invoke(false);
            yield break;
        }
        
        // Debug.Log("[NetworkManagerUI] Anonymous sign-in completed");
        
        // Step 3: Check if the saved game is still active and attempt reconnection
        Debug.Log($"[NetworkManagerUI] Checking if saved game {lastGameJoined} is still active...");
        var reconnectionTask = AttemptReconnectionToGame(lastGameJoined);
        while (!reconnectionTask.IsCompleted)
        {
            yield return null;
        }
        
        if (reconnectionTask.IsFaulted)
        {
            Debug.LogError($"[NetworkManagerUI] Reconnection failed: {reconnectionTask.Exception}");
            callback?.Invoke(false);
            yield break;
        }
        
        bool reconnectionSuccess = reconnectionTask.Result;
        callback?.Invoke(reconnectionSuccess);
    }

    // === JOIN CODE STORAGE & MANAGEMENT ===
    
    /// <summary>
    /// Saves the current game join code to PlayerPrefs
    /// </summary>
    private void SaveGameJoinCode(string joinCode, int playerCount)
    {
        lastGameJoined = joinCode;
        PlayerPrefs.SetString(LAST_JOIN_CODE_KEY, joinCode);
        PlayerPrefs.SetInt(LAST_PLAYER_COUNT_KEY, playerCount);
        PlayerPrefs.SetString(LAST_GAME_TIMESTAMP_KEY, DateTime.UtcNow.ToString("O"));
        PlayerPrefs.Save();
        
        // Debug.Log($"[NetworkManagerUI] Saved game join code: {joinCode} for {playerCount} players");
    }

    /// <summary>
    /// Loads the last game join code from PlayerPrefs
    /// </summary>
    private void LoadLastGameInfo()
    {
        lastGameJoined = PlayerPrefs.GetString(LAST_JOIN_CODE_KEY, "");
        int lastPlayerCount = PlayerPrefs.GetInt(LAST_PLAYER_COUNT_KEY, 0);
        string lastTimestamp = PlayerPrefs.GetString(LAST_GAME_TIMESTAMP_KEY, "");
        
        if (!string.IsNullOrEmpty(lastGameJoined))
        {
            // Debug.Log($"[NetworkManagerUI] Loaded last game: {lastGameJoined} ({lastPlayerCount} players) from {lastTimestamp}");
        }
    }

    /// <summary>
    /// Clears the saved game join code
    /// </summary>
    public void ClearSavedGameInfo()
    {
        lastGameJoined = "";
        PlayerPrefs.DeleteKey(LAST_JOIN_CODE_KEY);
        PlayerPrefs.DeleteKey(LAST_PLAYER_COUNT_KEY);
        PlayerPrefs.DeleteKey(LAST_GAME_TIMESTAMP_KEY);
        PlayerPrefs.Save();
        
        // Debug.Log("[NetworkManagerUI] Cleared saved game info");
    }

    // === AUTOMATIC RECONNECTION ===
    
    /// <summary>
    /// Attempts to reconnect to the last game
    /// </summary>
    private IEnumerator AttemptReconnection()
    {
        // Debug.Log($"[NetworkManagerUI] Starting reconnection attempt to: {lastGameJoined}");
        
        // Wait a moment before attempting reconnection
        yield return new WaitForSeconds(2f);
        
        // Try to reconnect using the saved join code
        // Use a flag to track the result since we can't use await in coroutine
        bool reconnectionAttempted = false;
        bool reconnectionResult = false;
        
        // Start the async reconnection
        StartCoroutine(AttemptReconnectionCoroutine(lastGameJoined, (result) => {
            reconnectionResult = result;
            reconnectionAttempted = true;
        }));
        
        // Wait for the reconnection attempt to complete
        while (!reconnectionAttempted)
        {
            yield return null;
        }
        
        if (reconnectionResult)
        {
            // Debug.Log("[NetworkManagerUI] Reconnection successful!");
            // The game will continue from where it left off
        }
        else
        {
            // Debug.LogWarning("[NetworkManagerUI] Reconnection failed - returning to main page");
            ClearSavedGameInfo();
            
            // Notify MainUIScript to return to main page
            if (mainUIScript != null)
            {
                mainUIScript.OnDisconnectDetected();
            }
        }
    }

    /// <summary>
    /// Coroutine wrapper for the async reconnection attempt
    /// </summary>
    private IEnumerator AttemptReconnectionCoroutine(string joinCode, System.Action<bool> callback)
    {
        bool result = false;
        
        // Start the async operation
        var task = AttemptReconnectionToGame(joinCode);
        
        // Wait for the task to complete
        while (!task.IsCompleted)
        {
            yield return null;
        }
        
        // Get the result and call the callback
        result = task.Result;
        callback?.Invoke(result);
    }

    /// <summary>
    /// Attempts to reconnect to lobby and relay using stored relay join code
    /// </summary>
    private async Task<bool> AttemptReconnectionToGame(string lobbyJoinCode)
    {
        // Initialize debug chain
        reconnectionDebugChain.Clear();
        AddDebugStep($"Starting reconnection attempt for lobby code: {lobbyJoinCode}");
        
        // Add ParrelSync debugging information
        AddParrelSyncDebugInfo();
        
        try
        {
            AddDebugStep("FULL RECONNECTION: Attempting to reconnect to lobby and relay");
            
            // Step 1: Join lobby using saved lobby join code
            AddDebugStep($"Step 1: Joining lobby with code: {lobbyJoinCode}");
            currentLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyJoinCode);
            AddDebugStep($"SUCCESS: Successfully rejoined lobby: {currentLobby.Name}");
            AddDebugStep($"Lobby ID: {currentLobby.Id}");
            AddDebugStep($"Lobby Code: {currentLobby.LobbyCode}");
            AddDebugStep($"Players in lobby: {currentLobby.Players.Count} (currently occupied slots)");
            AddDebugStep($"Max players in lobby: {currentLobby.MaxPlayers} (total available slots)");
            AddDebugStep($"Available slots: {currentLobby.AvailableSlots} (empty slots)");
            
            // Update UI to show lobby code
            if (mainUIScript != null)
            {
                mainUIScript.OpenWaitingScreenUI("yellow", "2", currentLobby.LobbyCode);
            }
            
            // Step 2: Wait for lobby connection to stabilize
            AddDebugStep("Step 2: Waiting for lobby connection to stabilize...");
            await Task.Delay(2000); // 2 second delay
            AddDebugStep("Lobby connection stabilized");
            
            // Step 3: Get relay join code from lobby metadata
            if (currentLobby.Data != null && currentLobby.Data.ContainsKey("RelayJoinCode"))
            {
                string relayJoinCode = currentLobby.Data["RelayJoinCode"].Value;
                AddDebugStep($"Step 3: Retrieved relay join code from lobby: {relayJoinCode}");
                AddDebugStep($"Lobby metadata keys: {string.Join(", ", currentLobby.Data.Keys)}");
                string hostAllocationId = currentLobby.Data.ContainsKey("HostAllocationId") ? currentLobby.Data["HostAllocationId"].Value : "NOT FOUND";
                AddDebugStep($"Host allocation ID: {hostAllocationId}");
                
                // Wait a bit more before relay connection
                AddDebugStep("Preparing for relay connection...");
                await Task.Delay(1000); // 1 second delay
                
                // Step 4: Connect to relay using stored relay join code (get fresh connection token)
                AddDebugStep($"Step 4: Getting fresh connection token for relay with code: {relayJoinCode}");
                AddDebugStep($"Relay join code length: {relayJoinCode?.Length ?? 0}");
                AddDebugStep($"Relay join code characters: {relayJoinCode?.ToCharArray().Select(c => (int)c).ToArray() ?? new int[0]}");
                
                // CRITICAL: Attempt to join relay allocation (this will fail if no slots available)
                AddDebugStep("Step 4.1: Attempting to join relay allocation...");
                var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode: relayJoinCode);
                AddDebugStep("Fresh connection token obtained successfully");
                
                // Step 4.5: Set the fresh relay server data with new connection token
                AddDebugStep("Step 4.5: Setting fresh relay server data");
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "wss"));
                AddDebugStep("Fresh relay server data set successfully");
                
                // Wait for transport to be ready
                AddDebugStep("Waiting for transport to be ready...");
                await Task.Delay(1000); // 1 second delay
                
                // Step 5: Start client connection
                AddDebugStep("Step 5: Starting NetworkManager client...");
                bool success = NetworkManager.Singleton.StartClient();
                
                if (success)
                {
                    AddDebugStep("RELAY RECONNECTION SUCCESS: Connected to both lobby and relay");
                    PrintDebugChain("SUCCESS");
                    // Stop client relay keep-alive since we're now properly connected
                    StopClientRelayKeepAlive();
                    return true;
                }
                else
                {
                    AddDebugStep("Failed to start NetworkManager client");
                    PrintDebugChain("FAILED - NetworkManager.StartClient() returned false");
                    // DON'T disconnect - keep in lobby for debugging
                    return false;
                }
            }
            else
            {
                AddDebugStep("ERROR: Lobby doesn't contain RelayJoinCode in metadata!");
                PrintDebugChain("FAILED - No RelayJoinCode in lobby metadata");
                // DON'T disconnect - keep in lobby for debugging
                return false;
            }
        }
        catch (Exception e)
        {
            AddDebugStep($"EXCEPTION: RELAY RECONNECTION FAILED: {e.Message}");
            AddDebugStep($"Exception Type: {e.GetType().Name}");
            AddDebugStep($"Stack Trace: {e.StackTrace}");
            PrintDebugChain($"FAILED - Exception: {e.Message}");
            // DON'T disconnect - keep in lobby for debugging
            return false;
        }
    }


    /// <summary>
    /// Handles successful reconnection by closing waiting screen and requesting game state sync
    /// </summary>
    private IEnumerator HandleSuccessfulReconnection()
    {
        Debug.Log("[NetworkManagerUI] Handling successful reconnection...");
        
        // Wait a moment for the network connection to stabilize
        yield return new WaitForSeconds(1f);
        
        // ENHANCED RECONNECTION: Don't close the main UI - just close waiting screens
        if (mainUIScript != null)
        {
            Debug.Log("[NetworkManagerUI] Closing reconnection waiting screen (keeping game UI open)");
            // Don't call ReturnToMainPage() as it would close the game screen
            // The waiting screen should already be closed by the reconnection process
        }
        
        // Wait a bit more for UI to close
        yield return new WaitForSeconds(0.5f);
        
        // SIMPLE RECONNECTION: Let server handle scene initialization via GivePlayerCount()
        // No need to force desync detection here - DeckController.GetPlayerCount() will handle it
        Debug.Log("[NetworkManagerUI] Reconnection handling complete - server will initialize scene and sync");
    }

    // === PUBLIC METHODS FOR EXTERNAL USE ===
    
    /// <summary>
    /// Public method to manually attempt reconnection
    /// </summary>
    [ContextMenu("Attempt Manual Reconnection")]
    public void ManualReconnectionAttempt()
    {
        if (!string.IsNullOrEmpty(lastGameJoined))
        {
            Debug.Log("[NetworkManagerUI] Manual reconnection attempt triggered");
            StartCoroutine(AttemptReconnection());
        }
        else
        {
            Debug.LogWarning("[NetworkManagerUI] No saved game to reconnect to");
        }
    }

    /// <summary>
    /// Public method to check if we're currently in a game
    /// </summary>
    public bool IsCurrentlyInGame()
    {
        return isInGame && NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient;
    }

    /// <summary>
    /// Public method to get the last game join code
    /// </summary>
    public string GetLastGameJoinCode()
    {
        return lastGameJoined;
    }

    /// <summary>
    /// Context menu method to manually clear saved game info (for testing/debugging)
    /// </summary>
    [ContextMenu("Clear Saved Game Info")]
    public void ClearSavedGameInfoContextMenu()
    {
        Debug.Log("[NetworkManagerUI] Context Menu: Clearing saved game info...");
        ClearSavedGameInfo();
        Debug.Log("[NetworkManagerUI] Context Menu: Saved game info cleared successfully");
    }

    // === ORIGINAL METHODS (UPDATED) ===

    void SetupButtonListeners()
    {
        clientButton.onClick.AddListener(async () => { await StartClientWithRelay(); });
        hostTwoPlayerButton.onClick.AddListener(async () => {await StartHostWithRelay(2,true); });
        hostFourPlayerButton.onClick.AddListener(async () => {await StartHostWithRelay(4,true); });
        quickPlayTwoPlayerButton.onClick.AddListener(async () => { await FindLobbiesAndStartHostIfNoneExist(2); });
        quickPlayFourPlayerButton.onClick.AddListener(async () => { await FindLobbiesAndStartHostIfNoneExist(4); });

        //FindLobbiesAndStartHostIfNoneExist(4);
    }

    bool isListeningFlag = false;
    void Update()
    {
        if (NetworkManager.Singleton.IsListening && isListeningFlag == false)
        {
            Debug.LogWarning("IsListening");
            isListeningFlag = true;
        }
    }

    public async Task EnsureFreshAnonymousSignIn()
    {
        if (AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut(); // Use synchronous version
        }
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private string GetUniquePlayerId()
    {
        // Generate a unique ID per tab and store it in PlayerPrefs
        string uniquePlayerId = PlayerPrefs.GetString("UniquePlayerId", null);

        if (string.IsNullOrEmpty(uniquePlayerId))
        {
            uniquePlayerId = Guid.NewGuid().ToString();
            PlayerPrefs.SetString("UniquePlayerId", uniquePlayerId);
        }

        Debug.Log($"Unique Player ID for this tab: {uniquePlayerId}");
        return uniquePlayerId;
    }

    public async Task<string> StartHostWithRelay(int playerCount,bool privateFlag)
    {
        // Use existing Unity Services connection (initialized at startup)
        if (!IsUnityServicesInitialized())
        {
            Debug.LogError("[NetworkManagerUI] Unity Services not initialized! This should have been done at startup.");
            return null;
        }
        
        Debug.Log("[NetworkManagerUI] Using existing Unity Services connection for hosting");

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(playerCount);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "wss"));
        joinCodeVar = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        joinCodeText.text = joinCodeVar;

        if(privateFlag)mainUIScript.OpenWaitingScreenUI("blue", playerCount.ToString(), joinCodeVar);

        CreateLobbyOptions options = new CreateLobbyOptions
        {
            IsPrivate = privateFlag, // Keep private lobbies for friend groups
            Data = new Dictionary<string, DataObject>
            {
                { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCodeVar) }, // CRITICAL: Store relay join code
                { "HostAllocationId", new DataObject(DataObject.VisibilityOptions.Public, allocation.AllocationId.ToString()) }, // For keep-alive
                { "GameStatus", new DataObject(DataObject.VisibilityOptions.Public, "Active") }, // Track game status
                { "PlayerCount", new DataObject(DataObject.VisibilityOptions.Public, playerCount.ToString()) }, // Track player count
                { "LastHeartbeat", new DataObject(DataObject.VisibilityOptions.Public, DateTime.UtcNow.ToString("O")) }, // Heartbeat timestamp
                { "HostPlayerId", new DataObject(DataObject.VisibilityOptions.Public, AuthenticationService.Instance.PlayerId) }
            }
        };

        currentLobby = await Lobbies.Instance.CreateLobbyAsync("MyLobby", playerCount, options);
        
        // CRITICAL: Display lobby codes for client connection, save lobby codes for reconnection
        if (privateFlag)
        {
            Debug.Log($"Host created private lobby with Relay join code: {joinCodeVar}");
            Debug.Log($"Private lobby code for clients: {currentLobby.LobbyCode}");
            joinCodeText.text = currentLobby.LobbyCode; // Clients use this to join lobby first
            
            // CRITICAL: Save LOBBY code for reconnection (lobby-first approach)
            SaveGameJoinCode(currentLobby.LobbyCode, playerCount);
        }
        else
        {
            Debug.Log($"Host created public lobby with Relay join code: {joinCodeVar}");
            Debug.Log($"Public lobby code for clients: {currentLobby.LobbyCode}");
            joinCodeText.text = currentLobby.LobbyCode; // Clients use this to join lobby first
            
            // CRITICAL: Save LOBBY code for reconnection (lobby-first approach)
            SaveGameJoinCode(currentLobby.LobbyCode, playerCount);
        }
        
        Server.Singleton.SetPlayerCount(playerCount);
        
        bool hostStarted = NetworkManager.Singleton.StartHost();
        if (hostStarted)
        {
            Debug.Log($"[NetworkManagerUI] Host started successfully with relay join code: {joinCodeVar}");
            
            // CRITICAL: Start relay keep-alive system AFTER host is started
            // Use a coroutine to ensure Server singleton is fully initialized
            StartCoroutine(StartRelayKeepAliveAfterHostStart(allocation.AllocationId.ToString()));
            
            return joinCodeVar;
        }
        else
        {
            Debug.LogError($"[NetworkManagerUI] CRITICAL ERROR: Failed to start host! Relay keep-alive may not work properly.");
            return null;
        }
    }

    public async Task<bool> StartClientWithRelay()
    {
        string inputJoinCode = inputField.text;
        mainUIScript.OpenWaitingScreenUI("yellow", "2", inputJoinCode);

        // Use existing Unity Services connection (initialized at startup)
        if (!IsUnityServicesInitialized())
        {
            Debug.LogError("[NetworkManagerUI] Unity Services not initialized! This should have been done at startup.");
            return false;
        }
        
        Debug.Log("[NetworkManagerUI] Using existing Unity Services connection for client");
        Debug.Log($"[NetworkManagerUI] LOBBY-FIRST CONNECTION: Trying to join lobby with code: {inputJoinCode}");
        joinCodeText.text = inputJoinCode;

        // CRITICAL: LOBBY-FIRST CONNECTION FLOW
        // Step 1: Join the lobby using the provided lobby join code
        try
        {
            Debug.Log($"[NetworkManagerUI] Step 1: Joining lobby with code: {inputJoinCode}");
            currentLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(inputJoinCode);
            Debug.Log($"[NetworkManagerUI] Successfully joined lobby: {currentLobby.Name}");
            
            // Step 2: Get relay join code from lobby metadata
            if (currentLobby.Data != null && currentLobby.Data.ContainsKey("RelayJoinCode"))
            {
                string relayJoinCode = currentLobby.Data["RelayJoinCode"].Value;
                Debug.Log($"[NetworkManagerUI] Step 2: Retrieved relay join code from lobby: {relayJoinCode}");
                
                // Step 3: Connect to relay using the stored relay join code (get fresh connection token)
                Debug.Log($"[NetworkManagerUI] Step 3: Getting fresh connection token for relay with code: {relayJoinCode}");
                var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode: relayJoinCode);
                Debug.Log($"[NetworkManagerUI] Fresh connection token obtained successfully");
                
                // Set the fresh relay server data with new connection token
                Debug.Log($"[NetworkManagerUI] Setting fresh relay server data");
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "wss"));
                
                // Step 4: Save LOBBY join code for reconnection (not relay code)
                SaveGameJoinCode(inputJoinCode, currentLobby.MaxPlayers);
                Debug.Log($"[NetworkManagerUI] Step 4: Saved lobby code for reconnection: {inputJoinCode}");
                
                // Step 5: Start client connection
                bool success = NetworkManager.Singleton.StartClient();
                if (success)
                {
                    Debug.Log($"[NetworkManagerUI] LOBBY-FIRST SUCCESS: Connected to lobby and relay successfully");
                    return true;
                }
                else
                {
                    Debug.LogError($"[NetworkManagerUI] Failed to start NetworkManager client");
                    return false;
                }
            }
            else
            {
                Debug.LogError($"[NetworkManagerUI] Lobby doesn't contain RelayJoinCode in metadata!");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[NetworkManagerUI] LOBBY-FIRST CONNECTION FAILED: {e.Message}");
            
            // FALLBACK: Try to find lobby by searching (for backward compatibility)
            Debug.Log("[NetworkManagerUI] FALLBACK: Searching for lobby with matching join code...");
            try
            {
                var queryOptions = new QueryLobbiesOptions();
                var lobbies = await Lobbies.Instance.QueryLobbiesAsync(queryOptions);
                
                foreach (var lobby in lobbies.Results)
                {
                    if (lobby.Data != null && lobby.Data.ContainsKey("RelayJoinCode"))
                    {
                        string lobbyRelayCode = lobby.Data["RelayJoinCode"].Value;
                        if (lobbyRelayCode == inputJoinCode)
                        {
                            Debug.Log($"[NetworkManagerUI] FALLBACK: Found lobby with matching relay code: {lobby.Name}");
                            currentLobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobby.Id);
                            
                            Debug.Log($"[NetworkManagerUI] FALLBACK: Getting fresh connection token for relay with code: {inputJoinCode}");
                            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode: inputJoinCode);
                            Debug.Log($"[NetworkManagerUI] FALLBACK: Fresh connection token obtained successfully");
                            
                            Debug.Log($"[NetworkManagerUI] FALLBACK: Setting fresh relay server data");
                            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "wss"));
                            
                            SaveGameJoinCode(lobby.LobbyCode, lobby.MaxPlayers);
                            return NetworkManager.Singleton.StartClient();
                        }
                    }
                }
                
                Debug.LogError($"[NetworkManagerUI] FALLBACK FAILED: No lobby found with join code: {inputJoinCode}");
                return false;
            }
            catch (Exception fallbackException)
            {
                Debug.LogError($"[NetworkManagerUI] FALLBACK EXCEPTION: {fallbackException.Message}");
                return false;
            }
        }
    }

    public async Task FindLobbiesAndStartHostIfNoneExist(int playerCount)
    {
        mainUIScript.OpenWaitingScreenUI("red", playerCount.ToString(),"abc");

        // Use existing Unity Services connection (initialized at startup)
        if (!IsUnityServicesInitialized())
        {
            Debug.LogError("[NetworkManagerUI] Unity Services not initialized! This should have been done at startup.");
            return;
        }
        
        Debug.Log("[NetworkManagerUI] Using existing Unity Services connection for quick play");

        QueryLobbiesOptions queryOptions = new QueryLobbiesOptions
        {
            Filters = new List<QueryFilter>
            {
                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT), // Lobbies with available slots
                new QueryFilter(QueryFilter.FieldOptions.MaxPlayers, playerCount.ToString(), QueryFilter.OpOptions.EQ)
            }
        };

        var lobbies = await Lobbies.Instance.QueryLobbiesAsync(queryOptions);

        if (lobbies.Results.Count > 0)
        {
            Debug.Log($"Found {lobbies.Results.Count} open lobbies for {playerCount} players.");
            foreach (var lobby in lobbies.Results)
            {
                Debug.Log($"Lobby Name: {lobby.Name}, Player Count: {playerCount}, Available Slots: {lobby.AvailableSlots}");
            }

            // Join the first lobby found using lobby-first approach
            if (lobbies.Results.Count > 0)
            {
                await JoinLobbyById(lobbies.Results[0].Id);
            }
        }
        else
        {
            Debug.Log($"No open lobbies for {playerCount} players found. Creating a new one...");
            await StartHostWithRelay(playerCount,false);
        }

        //if(playerCount==2)Server.Singleton.StartGameAfterDelayTwoPlayer();
        //if(playerCount==4)Server.Singleton.StartGameAfterDelayFourPlayer();
    }


    public async Task JoinLobbyById(string lobbyId)
    {
        try
        {
            Debug.Log($"[NetworkManagerUI] QUICKPLAY: Joining lobby by ID: {lobbyId}");
            var lobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobbyId);
            currentLobby = lobby;
            
            if (lobby == null)
            {
                Debug.LogError("[NetworkManagerUI] Lobby is null!");
                return;
            }
            
            if (lobby.Data == null || !lobby.Data.ContainsKey("RelayJoinCode"))
            {
                Debug.LogError("[NetworkManagerUI] Lobby data or RelayJoinCode is missing!");
                return;
            }

            Debug.Log($"[NetworkManagerUI] QUICKPLAY: Successfully joined lobby: {lobby.Name}");
            
            // LOBBY-FIRST APPROACH: Get relay join code from lobby metadata
            string relayJoinCode = lobby.Data["RelayJoinCode"].Value;
            Debug.Log($"[NetworkManagerUI] QUICKPLAY: Retrieved relay join code: {relayJoinCode}");
            
            // Connect to relay using stored relay join code (get fresh connection token)
            Debug.Log($"[NetworkManagerUI] QUICKPLAY: Getting fresh connection token for relay with code: {relayJoinCode}");
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode: relayJoinCode);
            Debug.Log($"[NetworkManagerUI] QUICKPLAY: Fresh connection token obtained successfully");
            
            // Set the fresh relay server data with new connection token
            Debug.Log($"[NetworkManagerUI] QUICKPLAY: Setting fresh relay server data");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "wss"));

            // CRITICAL: Save LOBBY code for reconnection (not relay code)
            SaveGameJoinCode(lobby.LobbyCode, lobby.MaxPlayers);
            Debug.Log($"[NetworkManagerUI] QUICKPLAY: Saved lobby code for reconnection: {lobby.LobbyCode}");

            bool success = NetworkManager.Singleton.StartClient();
            if (success)
            {
                Debug.Log("[NetworkManagerUI] QUICKPLAY SUCCESS: Connected to lobby and relay successfully");
            }
            else
            {
                Debug.LogError("[NetworkManagerUI] QUICKPLAY FAILED: Could not start NetworkManager client");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[NetworkManagerUI] Error joining lobby by ID: {e}");
        }
    }

    // Legacy method for backward compatibility
    public async void JoinLobby(string lobbyId)
    {
        await JoinLobbyById(lobbyId);
    }

    /// <summary>
    /// Properly disconnects from lobby services and cleans up network state
    /// KEEPS Unity Services connected for potential reconnection
    /// </summary>
    public async void DisconnectFromLobbyAndNetwork()
    {
        Debug.Log("[NetworkManagerUI] Disconnecting from lobby and cleaning up network state...");
        
        try
        {
            // STEP 1: Keep player in lobby for reconnection (don't remove them)
            if (currentLobby != null && AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log($"[NetworkManagerUI] Keeping player in lobby for reconnection: {currentLobby.Name}");
                // CRITICAL: Do NOT remove player from lobby - keep them for reconnection
                // await Lobbies.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
                // currentLobby = null; // Keep reference for potential reconnection
                Debug.Log("[NetworkManagerUI] Player kept in lobby for reconnection");
            }
            
            // STEP 2: CRITICAL FIX - Keep relay allocation alive by NOT shutting down NetworkManager
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                Debug.Log("[NetworkManagerUI] Keeping NetworkManager alive to maintain relay allocation for reconnection");
                
                if (NetworkManager.Singleton.IsHost)
                {
                    Debug.Log("[NetworkManagerUI] Host keeping relay allocation alive");
                }
                else if (NetworkManager.Singleton.IsClient)
                {
                    Debug.Log("[NetworkManagerUI] Client keeping relay allocation alive - NOT shutting down NetworkManager");
                    // CRITICAL: Do NOT shutdown NetworkManager for clients - this deallocates their relay slot
                    // The client will stay connected to the relay but disconnected from the game
                    
                    // NOTE: Client-side keep-alive removed - server handles keep-alive for disconnected clients
                    Debug.Log("[NetworkManagerUI] Client disconnected - server will maintain relay allocation");
                }
            }
            
            // STEP 3: KEEP Unity Services connected (don't sign out)
            // This allows for automatic reconnection to saved games
            Debug.Log("[NetworkManagerUI] Keeping Unity Services connected for potential reconnection");
            
            Debug.Log("[NetworkManagerUI] Successfully disconnected from lobby and network (Unity Services still connected)");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[NetworkManagerUI] Error during disconnection: {e.Message}");
            // Continue with cleanup even if there's an error
        }
    }

    private async void OnApplicationQuit()
    {
        // Only remove player from lobby when application is actually quitting
        if (currentLobby != null && AuthenticationService.Instance.IsSignedIn)
        {
            try
            {
                Debug.Log($"[NetworkManagerUI] Application quitting - removing player from lobby: {currentLobby.Name}");
                await Lobbies.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
                Debug.Log("[NetworkManagerUI] Successfully left lobby on application quit");
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to leave lobby on quit: " + e);
            }
        }
    }

    // === CLIENT RELAY KEEP-ALIVE SYSTEM ===
    
    /// <summary>
    /// Starts client-side relay keep-alive to prevent allocation deallocation
    /// </summary>
    private void StartClientRelayKeepAlive()
    {
        if (isClientRelayKeepAliveActive) return;
        
        isClientRelayKeepAliveActive = true;
        clientRelayKeepAliveCoroutine = StartCoroutine(ClientRelayKeepAliveCoroutine());
        Debug.Log("[NetworkManagerUI] Started client relay keep-alive to prevent allocation deallocation");
    }
    
    /// <summary>
    /// Stops client-side relay keep-alive
    /// </summary>
    private void StopClientRelayKeepAlive()
    {
        if (!isClientRelayKeepAliveActive) return;
        
        isClientRelayKeepAliveActive = false;
        
        if (clientRelayKeepAliveCoroutine != null)
        {
            StopCoroutine(clientRelayKeepAliveCoroutine);
            clientRelayKeepAliveCoroutine = null;
        }
        
        Debug.Log("[NetworkManagerUI] Stopped client relay keep-alive");
    }
    
    /// <summary>
    /// Coroutine that sends PING messages every 5 seconds to keep relay allocation alive
    /// </summary>
    private IEnumerator ClientRelayKeepAliveCoroutine()
    {
        Debug.Log("[NetworkManagerUI] Client relay keep-alive coroutine started");
        
        while (isClientRelayKeepAliveActive)
        {
            yield return new WaitForSeconds(5f); // Send PING every 5 seconds
            
            if (isClientRelayKeepAliveActive && NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
            {
                // Send a minimal RPC to keep the relay connection alive
                var networkRelay = FindObjectOfType<NetworkRelay>();
                if (networkRelay != null)
                {
                    networkRelay.SendHeartbeatServerRPC();
                    Debug.Log($"[NetworkManagerUI] Sent client relay keep-alive PING at {DateTime.UtcNow:HH:mm:ss}");
                }
            }
        }
        
        Debug.Log("[NetworkManagerUI] Client relay keep-alive coroutine stopped");
    }

    // === RELAY KEEP-ALIVE DELAYED START ===
    
    /// <summary>
    /// Starts relay keep-alive after host is fully initialized
    /// </summary>
    private IEnumerator StartRelayKeepAliveAfterHostStart(string hostAllocationId)
    {
        // Wait for Server singleton to be fully initialized
        yield return new WaitForSeconds(1f);
        
        // Try multiple times to ensure Server singleton is ready
        int attempts = 0;
        while (Server.Singleton == null && attempts < 10)
        {
            Debug.LogWarning($"[NetworkManagerUI] Waiting for Server.Singleton to initialize... attempt {attempts + 1}");
            yield return new WaitForSeconds(0.5f);
            attempts++;
        }
        
        if (Server.Singleton != null)
        {
            Server.Singleton.StartRelayKeepAlive(hostAllocationId);
            Debug.Log($"[NetworkManagerUI] Successfully started relay keep-alive system for allocation: {hostAllocationId}");
        }
        else
        {
            Debug.LogError($"[NetworkManagerUI] CRITICAL ERROR: Server.Singleton is still NULL after 10 attempts! Cannot start relay keep-alive for allocation: {hostAllocationId}");
        }
    }

    // === DEBUG CHAIN SYSTEM ===
    
    /// <summary>
    /// Adds a step to the reconnection debug chain
    /// </summary>
    private void AddDebugStep(string step)
    {
        string timestamp = DateTime.UtcNow.ToString("HH:mm:ss.fff");
        string debugStep = $"[{timestamp}] {step}";
        reconnectionDebugChain.Add(debugStep);
        Debug.LogWarning($"[RECONNECTION DEBUG] {debugStep}");
    }
    
    /// <summary>
    /// Adds ParrelSync-specific debugging information
    /// </summary>
    private void AddParrelSyncDebugInfo()
    {
        #if UNITY_EDITOR
        bool isClone = ClonesManager.IsClone();
        AddDebugStep($"ParrelSync - Is Clone: {isClone}");
        
        if (isClone)
        {
            string argument = ClonesManager.GetArgument();
            AddDebugStep($"ParrelSync - Clone Argument: {argument}");
        }
        
        AddDebugStep($"ParrelSync - Project Path: {Application.dataPath}");
        AddDebugStep($"ParrelSync - Persistent Data Path: {Application.persistentDataPath}");
        #else
        AddDebugStep("ParrelSync - Not in Editor (Build)");
        #endif
        
        // PlayerPrefs debugging
        string uniqueId = GetUniquePlayerId();
        AddDebugStep($"PlayerPrefs - Unique ID: {uniqueId}");
        AddDebugStep($"PlayerPrefs - Last Join Code Key: {LAST_JOIN_CODE_KEY}");
        AddDebugStep($"PlayerPrefs - Last Player Count Key: {LAST_PLAYER_COUNT_KEY}");
        AddDebugStep($"PlayerPrefs - Last Game Timestamp Key: {LAST_GAME_TIMESTAMP_KEY}");
        
        // Authentication debugging
        try
        {
            string playerId = AuthenticationService.Instance.PlayerId;
            AddDebugStep($"Authentication - Player ID: {playerId}");
            AddDebugStep($"Authentication - Is Signed In: {AuthenticationService.Instance.IsSignedIn}");
        }
        catch (Exception e)
        {
            AddDebugStep($"Authentication - Error: {e.Message}");
        }
        
        // Unity Services debugging
        try
        {
            AddDebugStep($"Unity Services - Is Initialized: {IsUnityServicesInitialized()}");
        }
        catch (Exception e)
        {
            AddDebugStep($"Unity Services - Error: {e.Message}");
        }
        
        // NetworkManager debugging
        try
        {
            if (NetworkManager.Singleton != null)
            {
                AddDebugStep($"NetworkManager - Is Connected Client: {NetworkManager.Singleton.IsConnectedClient}");
                AddDebugStep($"NetworkManager - Is Host: {NetworkManager.Singleton.IsHost}");
                AddDebugStep($"NetworkManager - Is Server: {NetworkManager.Singleton.IsServer}");
                AddDebugStep($"NetworkManager - Is Listening: {NetworkManager.Singleton.IsListening}");
            }
            else
            {
                AddDebugStep("NetworkManager - Singleton is NULL");
            }
        }
        catch (Exception e)
        {
            AddDebugStep($"NetworkManager - Error: {e.Message}");
        }
    }
    
    /// <summary>
    /// Prints the complete debug chain in one log for easy copy-pasting
    /// </summary>
    private void PrintDebugChain(string result)
    {
        // Build the complete debug chain as one string
        var debugChainText = new System.Text.StringBuilder();
        debugChainText.AppendLine("Önemli");
        debugChainText.AppendLine("=== RECONNECTION DEBUG CHAIN ===");
        debugChainText.AppendLine($"RESULT: {result}");
        debugChainText.AppendLine($"TOTAL STEPS: {reconnectionDebugChain.Count}");
        debugChainText.AppendLine("STEPS:");
        
        for (int i = 0; i < reconnectionDebugChain.Count; i++)
        {
            debugChainText.AppendLine($"  {i + 1:D2}. {reconnectionDebugChain[i]}");
        }
        
        debugChainText.AppendLine("=== END RECONNECTION DEBUG CHAIN ===");
        
        // Print as one log entry
        Debug.LogError(debugChainText.ToString());
        
        // Clear the chain for next attempt
        reconnectionDebugChain.Clear();
    }

}
