using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Integrates move chain tracking with existing game systems
/// Hooks into existing methods without modifying their logic
/// </summary>
public class MoveChainIntegrator : MonoBehaviour
{
    public static MoveChainIntegrator LocalInstance { get; private set; }
    
    [Header("Integration Settings")]
    [SerializeField] private bool enableMoveTracking = true;
    [SerializeField] private bool enableDesyncDetection = true;
    [SerializeField] private float chainValidationInterval = 5f; // Validate chains every 5 seconds
    
    [Header("Power Completion Tracking")]
    [SerializeField] private bool enablePowerCompletionTracking = true;
    [SerializeField] private float powerCompletionTimeout = 30f; // Timeout for power completion
    
    private float lastValidationTime = 0f;
    
    // Power completion tracking
    private Dictionary<string, PowerCompletionState> activePowers = new Dictionary<string, PowerCompletionState>();
    private bool isPowerOperationInProgress = false;
    private float powerOperationStartTime = 0f;
    

    
    void Start()
    {
        if (!enableMoveTracking) return;
        
        // Set the local instance
        LocalInstance = this;
        
        // Hook into existing systems
        SetupServerHooks();
        SetupClientHooks();
        SetupSuperpowerHooks();
        
        Debug.Log("[MoveChainIntegrator] Move chain integration initialized");
    }
    
    void OnDestroy()
    {
        if (LocalInstance == this)
        {
            LocalInstance = null;
        }
    }
    
    void Update()
    {
        if (!enableDesyncDetection) return;
        
        // Check for power operation timeout
        if (isPowerOperationInProgress && Time.time - powerOperationStartTime > powerCompletionTimeout)
        {
            Debug.LogWarning("[MoveChainIntegrator] Power operation timeout reached, resuming sync checks");
            ResumeSyncChecks();
        }
        
        // Simple periodic chain validation (only when no power operations are in progress)
        if (!isPowerOperationInProgress && Time.time - lastValidationTime > chainValidationInterval)
        {
            lastValidationTime = Time.time;
            Debug.Log($"[MoveChainIntegrator] No power operations in progress, triggering validation");
            TriggerPeriodicValidation();
        }
        else if (isPowerOperationInProgress)
        {
            Debug.Log($"[MoveChainIntegrator] Power operations in progress, skipping validation. Active powers: {activePowers.Count}");
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
            Debug.Log($"[MoveChainIntegrator] Validating chains - Client v{clientChain.chainVersion} vs Server v{serverChain.chainVersion}");
            
            var validationResult = clientChain.ValidateAgainst(serverChain, out int mismatchIndex);
            bool isValid = validationResult == MoveChain.ValidationResult.Valid;
            if (!isValid)
            {
                Debug.LogError($"[MoveChainIntegrator] PERIODIC VALIDATION: Desync detected at move index {mismatchIndex}! Result: {validationResult}");
                Debug.LogError($"[MoveChainIntegrator] Client chain moves: {clientChain.moves?.Length ?? 0}");
                Debug.LogError($"[MoveChainIntegrator] Server chain moves: {serverChain.moves?.Length ?? 0}");
                
                // Track the desync in debug chain
                DebugChainPrinter.LocalInstance?.TrackLocalAction($"DESYNC DETECTED at move index {mismatchIndex}! Result: {validationResult}");
                DebugChainPrinter.LocalInstance?.TrackMoveChain($"DESYNC: Client v{clientChain.chainVersion} vs Server v{serverChain.chainVersion} at index {mismatchIndex}");
                
                OnClientDesyncDetected(mismatchIndex);
            }
            else
            {
                Debug.Log($"[MoveChainIntegrator] Periodic validation: Chains in sync (Client: {clientChain.chainVersion}, Server: {serverChain.chainVersion})");
                
                // Track successful validation
                DebugChainPrinter.LocalInstance?.TrackMoveChain($"Chains in sync: Client v{clientChain.chainVersion} = Server v{serverChain.chainVersion}");
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
        
        // STEP 2: Update client chains after server has recorded the move
        if (MoveChainTracker.ClientInstance != null)
        {
            MoveChainTracker.ClientInstance.RecordMove(move);
            Debug.Log($"[MoveChainIntegrator] Client chain synchronized with server move: {move.moveType} by P{move.playerNumber}");
        }
        
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
        
        // Track the desync in debug chain
        DebugChainPrinter.LocalInstance?.TrackLocalAction($"OnClientDesyncDetected called for move index {mismatchIndex}");
        DebugChainPrinter.LocalInstance?.TrackMoveChain($"Client desync handler triggered for index {mismatchIndex}");
        
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
        Debug.Log($"[MoveChainIntegrator] TrackServerCardPlay called: P{playerNumber} played {cardId}");
        
        // Track in debug chain
        DebugChainPrinter.LocalInstance?.TrackLocalAction($"Server card play: {cardId} by P{playerNumber}");
        DebugChainPrinter.LocalInstance?.TrackMoveChain($"Server card play: {cardId} by P{playerNumber}");
        
        // STEP 1: Record the move on the server chain
        if (MoveChainTracker.ServerInstance != null)
        {
            MoveChainTracker.ServerInstance.RecordCardPlay(playerNumber, cardId, cardData, capturedCardIds, sumValue);
            Debug.Log($"[MoveChainIntegrator] Server chain updated: Card play {cardId} by P{playerNumber}");
        }
        
        // STEP 2: Ensure client chains are synchronized before proceeding
        ConfirmChainUpdatesComplete(() => {
            Debug.Log($"[MoveChainIntegrator] Chain updates confirmed complete for card play {cardId} by P{playerNumber}");
        });
    }
    

    
    /// <summary>
    /// Call this from superpower activation methods to track superpower usage
    /// For hybrid powers (Kutsal Deste, Ver Zehri, Oynayamazsın), this tracks the INITIAL activation
    /// </summary>
    public static void TrackSuperpowerActivation(int playerNumber, string superPowerName)
    {
        Debug.Log($"[MoveChainIntegrator] TrackSuperpowerActivation called: {superPowerName} by P{playerNumber}");
        
        // Track the activation in debug chain
        DebugChainPrinter.LocalInstance?.TrackLocalAction($"TrackSuperpowerActivation called: {superPowerName} by P{playerNumber}");
        DebugChainPrinter.LocalInstance?.TrackMoveChain($"Superpower activation recorded: {superPowerName} by P{playerNumber}");
        
        // HALT SYNC CHECKS when power operation starts
        if (LocalInstance != null)
        {
            LocalInstance.HaltSyncChecksForPower(superPowerName, playerNumber);
        }
        else
        {
            Debug.LogWarning($"[MoveChainIntegrator] LocalInstance is null! Cannot halt sync checks for {superPowerName}");
        }
        
        // For hybrid powers, DON'T record on server chain yet - wait for true activation (handled by Server.cs)
        if (IsHybridPower(superPowerName))
        {
            Debug.Log($"[MoveChainIntegrator] Hybrid power {superPowerName} activated - waiting for card play to record on chain");
        }
        else
        {
            // For immediate powers, record on server chain immediately
            if (MoveChainTracker.ServerInstance != null)
            {
                MoveChainTracker.ServerInstance.RecordSuperpowerActivation(playerNumber, superPowerName);
                Debug.Log($"[MoveChainIntegrator] Server chain updated: {superPowerName} by P{playerNumber}");
            }
        }
        
        Debug.Log($"[MoveChainIntegrator] Power activation recorded: {superPowerName} by P{playerNumber}");
    }
    
    /// <summary>
    /// Call this when a hybrid power's effect actually takes place (after card play)
    /// This records BOTH the power activation AND the card play together
    /// </summary>
    public static void TrackHybridPowerTrueActivation(int playerNumber, string superPowerName, string cardId, int[] cardData, string[] capturedCardIds, int sumValue)
    {
        Debug.Log($"[MoveChainIntegrator] TrackHybridPowerTrueActivation called: {superPowerName} by P{playerNumber} with card {cardId}");
        
        // Track in debug chain
        DebugChainPrinter.LocalInstance?.TrackLocalAction($"Hybrid power true activation: {superPowerName} by P{playerNumber} with card {cardId}");
        DebugChainPrinter.LocalInstance?.TrackMoveChain($"Hybrid power effect: {superPowerName} by P{playerNumber} with card {cardId}");
        
        // STEP 1: Record BOTH power activation AND card play together on server chain
        if (MoveChainTracker.ServerInstance != null)
        {
            // Record the power activation first
            MoveChainTracker.ServerInstance.RecordSuperpowerActivation(playerNumber, superPowerName);
            Debug.Log($"[MoveChainIntegrator] Server chain updated: Power activation {superPowerName} by P{playerNumber}");
            
            // Then record the card play
            MoveChainTracker.ServerInstance.RecordCardPlay(playerNumber, cardId, cardData, capturedCardIds, sumValue);
            Debug.Log($"[MoveChainIntegrator] Server chain updated: Card play {cardId} by P{playerNumber}");
        }
        
        Debug.Log($"[MoveChainIntegrator] Hybrid power true activation recorded: {superPowerName} + {cardId} by P{playerNumber}");
        
        // Report power completion immediately since Server.cs handles the synchronization
        ReportPowerCompletion(superPowerName, playerNumber, $"True activation with card {cardId}");
    }
    

    
    /// <summary>
    /// Determines if a power is a hybrid power (activates immediately but takes effect after card play)
    /// </summary>
    private static bool IsHybridPower(string superPowerName)
    {
        return superPowerName == "Kutsal Deste" || 
               superPowerName == "Ver Zehri" || 
               superPowerName == "Oynayamazsın";
    }
    
    /// <summary>
    /// Call this when superpower effects are applied to cards
    /// </summary>
    public static void TrackSuperpowerEffect(int playerNumber, string superPowerName, string[] affectedCardIds, 
        Dictionary<string, string> effectData = null)
    {
        // STEP 1: Update server chain ONLY (server authoritative)
        if (MoveChainTracker.ServerInstance != null)
        {
            MoveChainTracker.ServerInstance.RecordSuperpowerEffect(playerNumber, superPowerName, affectedCardIds, effectData);
            Debug.Log($"[MoveChainIntegrator] Server chain updated: Superpower effect {superPowerName} by P{playerNumber}");
        }
        
        Debug.Log($"[MoveChainIntegrator] Server superpower effect recorded: {superPowerName} by P{playerNumber}");
    }
    
    /// <summary>
    /// Call this when pending powers actually take effect (at end of turn)
    /// </summary>
    public static void TrackPendingPowerActivation(int playerNumber, string superPowerName, string details)
    {
        // Track the actual activation in debug chain
        DebugChainPrinter.LocalInstance?.TrackLocalAction($"Pending power now active: {superPowerName} by P{playerNumber} - {details}");
        DebugChainPrinter.LocalInstance?.TrackMoveChain($"Pending power activated at end of turn: {superPowerName} by P{playerNumber}");
        
        // Now track on both server and client since the power is actually taking effect
        if (MoveChainTracker.ServerInstance != null)
        {
            MoveChainTracker.ServerInstance.RecordSuperpowerActivation(playerNumber, $"{superPowerName}_Activated");
        }
        
        if (MoveChainTracker.ClientInstance != null)
        {
            MoveChainTracker.ClientInstance.RecordSuperpowerActivation(playerNumber, $"{superPowerName}_Activated");
        }
        
        // Report power completion for pending powers
        ReportPowerCompletion(superPowerName, playerNumber, $"Activated at end of turn: {details}");
    }
    
    /// <summary>
    /// Call this when cards are moved between players or locations
    /// </summary>
    public static void TrackCardMovement(int playerNumber, string cardId, string fromLocation, string toLocation, string reason)
    {
        // Track in debug chain
        DebugChainPrinter.LocalInstance?.TrackCardMovement(cardId, fromLocation, toLocation, reason);
        DebugChainPrinter.LocalInstance?.TrackMoveChain($"Card movement: {cardId} from {fromLocation} to {toLocation} by P{playerNumber} - {reason}");
        
        // STEP 1: Update server chain ONLY (server authoritative)
        if (MoveChainTracker.ServerInstance != null)
        {
            MoveChainTracker.ServerInstance.RecordCardMovement(playerNumber, cardId, fromLocation, toLocation, reason);
            Debug.Log($"[MoveChainIntegrator] Server chain updated: Card movement {cardId} by P{playerNumber}");
        }
        
        Debug.Log($"[MoveChainIntegrator] Server card movement recorded: {cardId} by P{playerNumber}");
    }
    
    /// <summary>
    /// Call this when cards are swapped between players
    /// </summary>
    public static void TrackCardSwap(int playerANumber, int playerBNumber, string cardAId, string cardBId, string reason)
    {
        // Track in debug chain
        DebugChainPrinter.LocalInstance?.TrackLocalAction($"Card swap: P{playerANumber} {cardAId} ↔ P{playerBNumber} {cardBId} - {reason}");
        DebugChainPrinter.LocalInstance?.TrackMoveChain($"Card swap: P{playerANumber} {cardAId} ↔ P{playerBNumber} {cardBId} - {reason}");
        
        // STEP 1: Update server chain ONLY (server authoritative)
        if (MoveChainTracker.ServerInstance != null)
        {
            MoveChainTracker.ServerInstance.RecordCardSwap(playerANumber, playerBNumber, cardAId, cardBId, reason);
            Debug.Log($"[MoveChainIntegrator] Server chain updated: Card swap {cardAId} ↔ {cardBId} by P{playerANumber}");
        }
        
        Debug.Log($"[MoveChainIntegrator] Server card swap recorded: {cardAId} ↔ {cardBId} by P{playerANumber}");
        
        // Report power completion for swap-based powers
        if (reason.Contains("Şunu Değiş") || reason.Contains("swap"))
        {
            ReportPowerCompletion("CardSwap", playerANumber, $"Swapped {cardAId} with {cardBId} from P{playerBNumber}");
        }
    }
    
    /// <summary>
    /// Call this when card parenting changes (important for Unity hierarchy)
    /// </summary>
    public static void TrackCardParentingChange(int playerNumber, string cardId, string oldParent, string newParent, string reason)
    {
        // Track in debug chain
        DebugChainPrinter.LocalInstance?.TrackLocalAction($"Card parenting change: {cardId} from {oldParent} to {newParent} by P{playerNumber} - {reason}");
        DebugChainPrinter.LocalInstance?.TrackMoveChain($"Card parenting: {cardId} from {oldParent} to {newParent} by P{playerNumber} - {reason}");
        
        // STEP 1: Update server chain ONLY (server authoritative)
        if (MoveChainTracker.ServerInstance != null)
        {
            MoveChainTracker.ServerInstance.RecordCardParentingChange(playerNumber, cardId, oldParent, newParent, reason);
            Debug.Log($"[MoveChainIntegrator] Server chain updated: Card parenting {cardId} by P{playerNumber}");
        }
        
        Debug.Log($"[MoveChainIntegrator] Server card parenting recorded: {cardId} by P{playerNumber}");
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
    
    // === POWER COMPLETION TRACKING METHODS ===
    
    /// <summary>
    /// Halts sync checks when a power operation starts
    /// </summary>
    private void HaltSyncChecksForPower(string superPowerName, int playerNumber)
    {
        if (!enablePowerCompletionTracking) return;
        
        string powerKey = $"{superPowerName}_{playerNumber}_{Time.time}";
        
        if (!isPowerOperationInProgress)
        {
            isPowerOperationInProgress = true;
            powerOperationStartTime = Time.time;
            Debug.Log($"[MoveChainIntegrator] Sync checks HALTED for power: {superPowerName} by P{playerNumber}");
        }
        
        // Track this power as active
        activePowers[powerKey] = new PowerCompletionState
        {
            powerName = superPowerName,
            playerNumber = playerNumber,
            startTime = Time.time,
            isCompleted = false
        };
        
        Debug.Log($"[MoveChainIntegrator] Power operation started: {superPowerName} by P{playerNumber} (Key: {powerKey})");
        Debug.Log($"[MoveChainIntegrator] Active powers count: {activePowers.Count}");
    }
    
    /// <summary>
    /// Reports that a power operation has completed on a specific client
    /// </summary>
    public static void ReportPowerCompletion(string superPowerName, int playerNumber, string completionDetails = "")
    {
        Debug.Log($"[MoveChainIntegrator] ReportPowerCompletion called: {superPowerName} by P{playerNumber} - {completionDetails}");
        
        if (LocalInstance != null)
        {
            LocalInstance.CompletePowerOperation(superPowerName, playerNumber, completionDetails);
        }
        else
        {
            Debug.LogWarning($"[MoveChainIntegrator] LocalInstance is null! Cannot complete power {superPowerName}");
        }
    }
    
    /// <summary>
    /// Marks a power operation as completed
    /// </summary>
    private void CompletePowerOperation(string superPowerName, int playerNumber, string completionDetails)
    {
        if (!enablePowerCompletionTracking) return;
        
        Debug.Log($"[MoveChainIntegrator] Attempting to complete power: {superPowerName} by P{playerNumber}");
        
        // Find and mark the power as completed
        string powerKey = FindPowerKey(superPowerName, playerNumber);
        if (powerKey != null && activePowers.ContainsKey(powerKey))
        {
            activePowers[powerKey].isCompleted = true;
            activePowers[powerKey].completionDetails = completionDetails;
            activePowers[powerKey].completionTime = Time.time;
            
            Debug.Log($"[MoveChainIntegrator] Power completed: {superPowerName} by P{playerNumber} - {completionDetails}");
        }
        else
        {
            Debug.LogWarning($"[MoveChainIntegrator] Could not find power key for: {superPowerName} by P{playerNumber}");
            Debug.LogWarning($"[MoveChainIntegrator] Available power keys: {string.Join(", ", activePowers.Keys)}");
        }
        
        // Check if all active powers are completed
        if (AreAllPowersCompleted())
        {
            ResumeSyncChecks();
        }
    }
    
    /// <summary>
    /// Finds the key for a specific power operation
    /// </summary>
    private string FindPowerKey(string superPowerName, int playerNumber)
    {
        foreach (var kvp in activePowers)
        {
            if (kvp.Value.powerName == superPowerName && kvp.Value.playerNumber == playerNumber && !kvp.Value.isCompleted)
            {
                return kvp.Key;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Checks if all active power operations are completed
    /// </summary>
    private bool AreAllPowersCompleted()
    {
        if (activePowers.Count == 0) return true;
        
        foreach (var power in activePowers.Values)
        {
            if (!power.isCompleted)
            {
                return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// Resumes sync checks after all power operations are completed
    /// </summary>
    private void ResumeSyncChecks()
    {
        if (!isPowerOperationInProgress) return;
        
        isPowerOperationInProgress = false;
        lastValidationTime = Time.time; // Reset validation timer
        
        // Log completion summary
        Debug.Log($"[MoveChainIntegrator] All power operations completed. Resuming sync checks.");
        foreach (var power in activePowers.Values)
        {
            if (power.isCompleted)
            {
                Debug.Log($"[MoveChainIntegrator] ✓ {power.powerName} by P{power.playerNumber} completed in {power.completionTime - power.startTime:F2}s");
            }
        }
        
        // Clear completed powers
        activePowers.Clear();
        
        // Trigger immediate validation to catch up
        TriggerPeriodicValidation();
    }
    
    /// <summary>
    /// Forces resumption of sync checks (for emergency use)
    /// </summary>
    [ContextMenu("Force Resume Sync Checks")]
    public void ForceResumeSyncChecks()
    {
        Debug.LogWarning("[MoveChainIntegrator] Force resuming sync checks!");
        ResumeSyncChecks();
    }
    
    [ContextMenu("Debug Power Completion State")]
    public void DebugPowerCompletionState()
    {
        Debug.Log($"[MoveChainIntegrator] Power completion state:");
        Debug.Log($"[MoveChainIntegrator] isPowerOperationInProgress: {isPowerOperationInProgress}");
        Debug.Log($"[MoveChainIntegrator] Active powers count: {activePowers.Count}");
        Debug.Log($"[MoveChainIntegrator] powerOperationStartTime: {powerOperationStartTime}");
        Debug.Log($"[MoveChainIntegrator] Time since start: {Time.time - powerOperationStartTime:F2}s");
        
        foreach (var kvp in activePowers)
        {
            var power = kvp.Value;
            Debug.Log($"[MoveChainIntegrator] Power: {power.powerName} by P{power.playerNumber} - Completed: {power.isCompleted} - Details: {power.completionDetails}");
        }
    }
    
    /// <summary>
    /// Shows current power completion status
    /// </summary>
    [ContextMenu("Show Power Completion Status")]
    public void ShowPowerCompletionStatus()
    {
        Debug.Log($"[MoveChainIntegrator] Power Completion Status:");
        Debug.Log($"  Sync checks halted: {isPowerOperationInProgress}");
        Debug.Log($"  Active powers: {activePowers.Count}");
        
        foreach (var kvp in activePowers)
        {
            var power = kvp.Value;
            string status = power.isCompleted ? "✓ COMPLETED" : "⏳ IN PROGRESS";
            Debug.Log($"  {power.powerName} by P{power.playerNumber}: {status}");
            if (power.isCompleted)
            {
                Debug.Log($"    Completed in {power.completionTime - power.startTime:F2}s - {power.completionDetails}");
            }
        }
    }
    
    [ContextMenu("Test Power Completion")]
    public void TestPowerCompletion()
    {
        Debug.Log("[MoveChainIntegrator] Testing power completion...");
        ReportPowerCompletion("Bu Daha İyi", 0, "Manual test completion");
    }
    
    [ContextMenu("Force Chain Synchronization")]
    public void ForceChainSynchronization()
    {
        Debug.LogWarning("[MoveChainIntegrator] Force synchronizing chains...");
        
        if (MoveChainTracker.ClientInstance != null && MoveChainTracker.ServerInstance != null)
        {
            var clientChain = MoveChainTracker.ClientInstance.GetCurrentChain();
            var serverChain = MoveChainTracker.ServerInstance.GetCurrentChain();
            
            Debug.Log($"[MoveChainIntegrator] Before sync - Client v{clientChain.chainVersion} ({clientChain.moves?.Length ?? 0} moves), Server v{serverChain.chainVersion} ({serverChain.moves?.Length ?? 0} moves)");
            
            // Force client chain to match server chain
            if (serverChain.moves != null && serverChain.moves.Length > 0)
            {
                MoveChainTracker.ClientInstance.ResetChain();
                foreach (var move in serverChain.moves)
                {
                    MoveChainTracker.ClientInstance.RecordMove(move);
                }
                Debug.Log($"[MoveChainIntegrator] Client chain synchronized with server chain");
            }
            
            var newClientChain = MoveChainTracker.ClientInstance.GetCurrentChain();
            Debug.Log($"[MoveChainIntegrator] After sync - Client v{newClientChain.chainVersion} ({newClientChain.moves?.Length ?? 0} moves), Server v{serverChain.chainVersion} ({serverChain.moves?.Length ?? 0} moves)");
        }
    }
    
    /// <summary>
    /// Forces immediate validation to catch desyncs early
    /// </summary>
    private void ForceImmediateValidation()
    {
        Debug.Log("[MoveChainIntegrator] Force immediate validation triggered");
        
        // Reset validation timer to trigger validation immediately
        lastValidationTime = 0f;
        
        // Trigger validation on next Update cycle
        StartCoroutine(TriggerValidationNextFrame());
    }
    

    
    private System.Collections.IEnumerator TriggerValidationNextFrame()
    {
        yield return null; // Wait one frame
        TriggerPeriodicValidation();
    }
    
    [ContextMenu("Force Immediate Validation")]
    public void ForceImmediateValidationPublic()
    {
        ForceImmediateValidation();
    }
    
    /// <summary>
    /// Ensures both server and client chains are updated before proceeding with visual changes
    /// This implements the proper sequence: Chain updates FIRST, then visuals, then desync checks
    /// </summary>
    public static void ConfirmChainUpdatesComplete(System.Action onComplete = null)
    {
        Debug.Log("[MoveChainIntegrator] Confirming chain updates are complete before proceeding...");
        
        // Wait for all chain updates to propagate and ensure synchronization
        if (LocalInstance != null)
        {
            LocalInstance.StartCoroutine(LocalInstance.WaitForChainUpdatesCoroutine(onComplete));
        }
        else
        {
            // Fallback: execute immediately if no LocalInstance
            onComplete?.Invoke();
        }
    }
    
    private System.Collections.IEnumerator WaitForChainUpdatesCoroutine(System.Action onComplete)
    {
        // Wait one frame to ensure all chain updates have been processed
        yield return null;
        
        // Verify that both server and client chains are synchronized
        if (MoveChainTracker.ClientInstance != null && MoveChainTracker.ServerInstance != null)
        {
            var clientChain = MoveChainTracker.ClientInstance.GetCurrentChain();
            var serverChain = MoveChainTracker.ServerInstance.GetCurrentChain();
            
            Debug.Log($"[MoveChainIntegrator] Chain sync check - Client v{clientChain.chainVersion} ({clientChain.moves?.Length ?? 0} moves), Server v{serverChain.chainVersion} ({serverChain.moves?.Length ?? 0} moves)");
            
            // If chains are not synchronized, wait another frame
            if (clientChain.chainVersion != serverChain.chainVersion || 
                (clientChain.moves?.Length ?? 0) != (serverChain.moves?.Length ?? 0))
            {
                Debug.LogWarning("[MoveChainIntegrator] Chains not synchronized, waiting another frame...");
                yield return null;
            }
        }
        
        Debug.Log("[MoveChainIntegrator] Chain updates confirmed complete, proceeding with visual changes");
        onComplete?.Invoke();
    }
    

}

/// <summary>
/// Tracks the completion state of a power operation
/// </summary>
[System.Serializable]
public class PowerCompletionState
{
    public string powerName;
    public int playerNumber;
    public float startTime;
    public bool isCompleted;
    public float completionTime;
    public string completionDetails;
}
