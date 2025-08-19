using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Integrates move chain tracking with existing game systems
/// Hooks into existing methods without modifying their logic
/// </summary>
public class MoveChainIntegrator : MonoBehaviour
{
    [Header("Integration Settings")]
    [SerializeField] private bool enableMoveTracking = true;
    [SerializeField] private bool enableDesyncDetection = true;
    [SerializeField] private float chainValidationInterval = 5f; // Validate chains every 5 seconds
    
    private float lastValidationTime = 0f;
    
    void Start()
    {
        if (!enableMoveTracking) return;
        
        // Hook into existing systems
        SetupServerHooks();
        SetupClientHooks();
        SetupSuperpowerHooks();
        
        Debug.Log("[MoveChainIntegrator] Move chain integration initialized");
    }
    
    void Update()
    {
        if (!enableDesyncDetection) return;
        
        // Periodic chain validation (optional)
        if (Time.time - lastValidationTime > chainValidationInterval)
        {
            lastValidationTime = Time.time;
            TriggerPeriodicValidation();
        }
    }
    
    /// <summary>
    /// Triggers periodic validation between client and server chains
    /// </summary>
    private void TriggerPeriodicValidation()
    {
        if (MoveChainTracker.ClientInstance == null || MoveChainTracker.ServerInstance == null)
            return;
            
        var clientChain = MoveChainTracker.ClientInstance.GetCurrentChain();
        var serverChain = MoveChainTracker.ServerInstance.GetCurrentChain();
        
        // Only validate if both chains have moves
        if (clientChain.chainVersion > 0 && serverChain.chainVersion > 0)
        {
            var validationResult = clientChain.ValidateAgainst(serverChain, out int mismatchIndex);
            bool isValid = validationResult == MoveChain.ValidationResult.Valid;
            if (!isValid)
            {
                Debug.LogError($"[MoveChainIntegrator] PERIODIC VALIDATION: Desync detected at move index {mismatchIndex}! Result: {validationResult}");
                OnClientDesyncDetected(mismatchIndex);
            }
            else
            {
                Debug.Log($"[MoveChainIntegrator] Periodic validation: Chains in sync (Client: {clientChain.chainVersion}, Server: {serverChain.chainVersion})");
            }
        }
    }
    
    /// <summary>
    /// Sets up hooks for server-side move tracking
    /// </summary>
    private void SetupServerHooks()
    {
        var server = FindObjectOfType<Server>();
        if (server == null) return;
        
        // Add MoveChainTracker to server if it doesn't exist
        var serverTracker = server.GetComponent<MoveChainTracker>();
        if (serverTracker == null)
        {
            serverTracker = server.gameObject.AddComponent<MoveChainTracker>();
        }
        
        // Hook into server move recording
        if (serverTracker != null)
        {
            serverTracker.OnMoveRecorded += OnServerMoveRecorded;
            serverTracker.OnDesyncDetected += OnServerDesyncDetected;
        }
        
        Debug.Log("[MoveChainIntegrator] Server hooks established");
    }
    
    /// <summary>
    /// Sets up hooks for client-side move tracking
    /// </summary>
    private void SetupClientHooks()
    {
        var gameManager = GameManager.LocalInstance;
        if (gameManager == null) return;
        
        // Add MoveChainTracker to GameManager if it doesn't exist
        var clientTracker = gameManager.GetComponent<MoveChainTracker>();
        if (clientTracker == null)
        {
            clientTracker = gameManager.gameObject.AddComponent<MoveChainTracker>();
        }
        
        // Hook into client move recording
        if (clientTracker != null)
        {
            clientTracker.OnMoveRecorded += OnClientMoveRecorded;
            clientTracker.OnDesyncDetected += OnClientDesyncDetected;
        }
        
        Debug.Log("[MoveChainIntegrator] Client hooks established");
    }
    
    /// <summary>
    /// Sets up hooks for superpower tracking
    /// </summary>
    private void SetupSuperpowerHooks()
    {
        // This will be called by the superpower tracking methods we'll add to existing scripts
        Debug.Log("[MoveChainIntegrator] Superpower hooks ready");
    }
    
    /// <summary>
    /// Called when server records a move
    /// </summary>
    private void OnServerMoveRecorded(GameMove move)
    {
        Debug.Log($"[MoveChainIntegrator] Server move recorded: {move.moveType} by P{move.playerNumber}");
        
        // Broadcast move to clients for validation
        var networkRelay = FindObjectOfType<NetworkRelay>();
        if (networkRelay != null)
        {
            networkRelay.BroadcastMoveForValidationClientRPC(move);
        }
    }
    
    /// <summary>
    /// Called when client records a move
    /// </summary>
    private void OnClientMoveRecorded(GameMove move)
    {
        Debug.Log($"[MoveChainIntegrator] Client move recorded: {move.moveType} by P{move.playerNumber}");
        // Could send to server for validation if needed
    }
    
    /// <summary>
    /// Called when server detects a desync
    /// </summary>
    private void OnServerDesyncDetected(int mismatchIndex)
    {
        Debug.LogError($"[MoveChainIntegrator] Server detected desync at move index {mismatchIndex}");
        
        // Trigger full state sync
        TriggerFullStateSync();
    }
    
    /// <summary>
    /// Called when client detects a desync
    /// </summary>
    private void OnClientDesyncDetected(int mismatchIndex)
    {
        Debug.LogError($"[MoveChainIntegrator] Client detected desync at move index {mismatchIndex}");
        
        // Request full state sync from server
        RequestFullStateSync();
    }
    
    /// <summary>
    /// Triggers a full state sync (server-side)
    /// </summary>
    private void TriggerFullStateSync()
    {
        var server = FindObjectOfType<Server>();
        if (server != null)
        {
            // Use existing save/load system
            var gameState = server.BuildGameStateSnapshot();
            var networkRelay = FindObjectOfType<NetworkRelay>();
            if (networkRelay != null)
            {
                networkRelay.ApplyGameStateClientRPC(gameState);
                Debug.Log("[MoveChainIntegrator] Full state sync triggered by server");
            }
        }
    }
    
    /// <summary>
    /// Requests a full state sync (client-side)
    /// </summary>
    private void RequestFullStateSync()
    {
        var networkRelay = FindObjectOfType<NetworkRelay>();
        if (networkRelay != null)
        {
            networkRelay.RequestFullStateSyncServerRPC();
            Debug.Log("[MoveChainIntegrator] Full state sync requested by client");
        }
    }
    
    // === INTEGRATION METHODS FOR EXISTING SYSTEMS ===
    
    /// <summary>
    /// Call this from Server.GetMove() to track card plays
    /// </summary>
    public static void TrackServerCardPlay(int playerNumber, string cardId, int[] cardData, string[] capturedCardIds, int sumValue)
    {
        if (MoveChainTracker.ServerInstance != null)
        {
            MoveChainTracker.ServerInstance.RecordCardPlay(playerNumber, cardId, cardData, capturedCardIds, sumValue);
        }
    }
    
    /// <summary>
    /// Call this from superpower activation methods to track superpower usage
    /// </summary>
    public static void TrackSuperpowerActivation(int playerNumber, string superPowerName)
    {
        // Track on both server and client
        if (MoveChainTracker.ServerInstance != null)
        {
            MoveChainTracker.ServerInstance.RecordSuperpowerActivation(playerNumber, superPowerName);
        }
        
        if (MoveChainTracker.ClientInstance != null)
        {
            MoveChainTracker.ClientInstance.RecordSuperpowerActivation(playerNumber, superPowerName);
        }
    }
    
    /// <summary>
    /// Call this when superpower effects are applied to cards
    /// </summary>
    public static void TrackSuperpowerEffect(int playerNumber, string superPowerName, string[] affectedCardIds, 
        Dictionary<string, string> effectData = null)
    {
        // Track on both server and client
        if (MoveChainTracker.ServerInstance != null)
        {
            MoveChainTracker.ServerInstance.RecordSuperpowerEffect(playerNumber, superPowerName, affectedCardIds, effectData);
        }
        
        if (MoveChainTracker.ClientInstance != null)
        {
            MoveChainTracker.ClientInstance.RecordSuperpowerEffect(playerNumber, superPowerName, affectedCardIds, effectData);
        }
    }
    
    /// <summary>
    /// Call this to reset chains when a new round starts
    /// </summary>
    public static void ResetChains()
    {
        if (MoveChainTracker.ServerInstance != null)
        {
            MoveChainTracker.ServerInstance.ResetChain();
        }
        
        if (MoveChainTracker.ClientInstance != null)
        {
            MoveChainTracker.ClientInstance.ResetChain();
        }
    }
    
    [ContextMenu("Trigger Test Desync")]
    public void TriggerTestDesync()
    {
        Debug.Log("[MoveChainIntegrator] Triggering test desync detection");
        OnClientDesyncDetected(99); // Test desync at move 99
    }
    
    [ContextMenu("Log All Chain Stats")]
    public void LogAllChainStats()
    {
        if (MoveChainTracker.ServerInstance != null)
        {
            MoveChainTracker.ServerInstance.LogChainStats();
        }
        
        if (MoveChainTracker.ClientInstance != null)
        {
            MoveChainTracker.ClientInstance.LogChainStats();
        }
    }
    
    [ContextMenu("Trigger Periodic Validation Now")]
    public void TriggerValidationNow()
    {
        TriggerPeriodicValidation();
    }
    
    [ContextMenu("Force Desync Test")]
    public void ForceDesyncTest()
    {
        Debug.Log("[MoveChainIntegrator] Forcing desync test...");
        
        // Create a fake move on client that doesn't exist on server
        if (MoveChainTracker.ClientInstance != null)
        {
            var fakeMove = GameMove.CreateCardPlayMove(999, 0, "fake_card_999", new int[] { 1, 5 }, new string[0], 0);
            MoveChainTracker.ClientInstance.RecordMove(fakeMove);
            
            Debug.LogWarning("[MoveChainIntegrator] Added fake move to client chain. This will cause a desync!");
            
            // Trigger validation immediately
            TriggerPeriodicValidation();
        }
    }
    
    public void RecordMove(GameMove move)
    {
        // This is a helper method for testing - adds move directly to client tracker
        if (MoveChainTracker.ClientInstance != null)
        {
            MoveChainTracker.ClientInstance.RecordMove(move);
        }
    }
}
