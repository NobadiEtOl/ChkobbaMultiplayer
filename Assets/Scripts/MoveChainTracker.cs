using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Tracks and validates move chains for desync detection
/// Completely decoupled - can be added to existing systems without modification
/// </summary>
public class MoveChainTracker : MonoBehaviour
{
    private List<GameMove> moveChain = new List<GameMove>();
    private int nextMoveId = 0;
    private bool isServer = false;
    
    // Events for integration with existing systems
    public System.Action<GameMove> OnMoveRecorded;
    public System.Action<int> OnDesyncDetected; // Parameter is the mismatch index
    
    public static MoveChainTracker ServerInstance { get; private set; }
    public static MoveChainTracker ClientInstance { get; private set; }
    
    void Awake()
    {
        // Determine if this is server or client instance
        var server = GetComponent<Server>();
        var gameManager = GetComponent<GameManager>();
        
        if (server != null)
        {
            isServer = true;
            ServerInstance = this;
            Debug.Log("[MoveChainTracker] Server instance initialized");
        }
        else if (gameManager != null)
        {
            isServer = false;
            ClientInstance = this;
            Debug.Log("[MoveChainTracker] Client instance initialized");
        }
        else
        {
            Debug.LogError("[MoveChainTracker] Could not determine if this is server or client instance!");
        }
    }
    
    void OnDestroy()
    {
        if (isServer && ServerInstance == this)
            ServerInstance = null;
        else if (!isServer && ClientInstance == this)
            ClientInstance = null;
    }
    
    /// <summary>
    /// Records a card play move
    /// </summary>
    public void RecordCardPlay(int playerNumber, string cardId, int[] cardData, string[] capturedCardIds, int sumValue)
    {
        var move = GameMove.CreateCardPlayMove(nextMoveId++, playerNumber, cardId, cardData, capturedCardIds, sumValue);
        RecordMove(move);
        
        Debug.Log($"[MoveChainTracker] {(isServer ? "Server" : "Client")} recorded card play: {move.moveType} by P{playerNumber} with {cardId}");
    }
    
    /// <summary>
    /// Records a superpower activation
    /// </summary>
    public void RecordSuperpowerActivation(int playerNumber, string superPowerName)
    {
        var move = GameMove.CreateSuperpowerActivationMove(nextMoveId++, playerNumber, superPowerName);
        RecordMove(move);
        
        Debug.Log($"[MoveChainTracker] {(isServer ? "Server" : "Client")} recorded superpower activation: {superPowerName} by P{playerNumber}");
    }
    
    /// <summary>
    /// Records a superpower effect on cards
    /// </summary>
    public void RecordSuperpowerEffect(int playerNumber, string superPowerName, string[] affectedCardIds, 
        Dictionary<string, string> effectData = null)
    {
        var move = GameMove.CreateSuperpowerEffectMove(nextMoveId++, playerNumber, superPowerName, affectedCardIds, effectData);
        RecordMove(move);
        
        Debug.Log($"[MoveChainTracker] {(isServer ? "Server" : "Client")} recorded superpower effect: {superPowerName} affecting {affectedCardIds?.Length ?? 0} cards");
    }
    
    /// <summary>
    /// Records card movement between locations
    /// </summary>
    public void RecordCardMovement(int playerNumber, string cardId, string fromLocation, string toLocation, string reason)
    {
        var move = GameMove.CreateCardMovementMove(nextMoveId++, playerNumber, cardId, fromLocation, toLocation, reason);
        RecordMove(move);
        
        Debug.Log($"[MoveChainTracker] {(isServer ? "Server" : "Client")} recorded card movement: {cardId} from {fromLocation} to {toLocation} by P{playerNumber} - {reason}");
    }
    
    /// <summary>
    /// Gets the next available move ID for this tracker
    /// </summary>
    public int GetNextMoveId()
    {
        return nextMoveId;
    }
    
    /// <summary>
    /// Records card swap between players
    /// </summary>
    public void RecordCardSwap(int playerANumber, int playerBNumber, string cardAId, string cardBId, string reason)
    {
        var move = GameMove.CreateCardSwapMove(nextMoveId++, playerANumber, playerBNumber, cardAId, cardBId, reason);
        RecordMove(move);
        
        Debug.Log($"[MoveChainTracker] {(isServer ? "Server" : "Client")} recorded card swap: P{playerANumber} {cardAId} ↔ P{playerBNumber} {cardBId} - {reason}");
    }
    
    /// <summary>
    /// Records card parenting change (Unity hierarchy change)
    /// </summary>
    public void RecordCardParentingChange(int playerNumber, string cardId, string oldParent, string newParent, string reason)
    {
        var move = GameMove.CreateCardParentingChangeMove(nextMoveId++, playerNumber, cardId, oldParent, newParent, reason);
        RecordMove(move);
        
        Debug.Log($"[MoveChainTracker] {(isServer ? "Server" : "Client")} recorded card parenting change: {cardId} from {oldParent} to {newParent} by P{playerNumber} - {reason}");
    }
    
    /// <summary>
    /// Records a move and triggers events
    /// </summary>
    public void RecordMove(GameMove move)
    {
        moveChain.Add(move);
        OnMoveRecorded?.Invoke(move);
    }
    
    /// <summary>
    /// Validates the current chain against another chain
    /// </summary>
    public bool ValidateChain(MoveChain otherChain, out int mismatchIndex)
    {
        var currentChain = new MoveChain(moveChain);
        var result = currentChain.ValidateAgainst(otherChain, out mismatchIndex);
        
        if (result != MoveChain.ValidationResult.Valid)
        {
            Debug.LogError($"[MoveChainTracker] {(isServer ? "Server" : "Client")} chain validation failed: {result} at index {mismatchIndex}");
            OnDesyncDetected?.Invoke(mismatchIndex);
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Gets the current move chain
    /// </summary>
    public MoveChain GetCurrentChain()
    {
        return new MoveChain(moveChain);
    }
    
    /// <summary>
    /// Resets the chain (for new rounds)
    /// </summary>
    public void ResetChain()
    {
        moveChain.Clear();
        nextMoveId = 0;
        Debug.Log($"[MoveChainTracker] {(isServer ? "Server" : "Client")} chain reset");
    }
    
    /// <summary>
    /// Gets chain statistics for debugging
    /// </summary>
    public void LogChainStats()
    {
        var cardPlays = moveChain.Count(m => m.moveType == GameMove.MoveType.PlayToCenter || m.moveType == GameMove.MoveType.Capture);
        var superpowers = moveChain.Count(m => m.moveType == GameMove.MoveType.SuperPower_Activation);
        var effects = moveChain.Count(m => m.moveType == GameMove.MoveType.SuperPower_Effect);
        
        Debug.Log($"[MoveChainTracker] {(isServer ? "Server" : "Client")} chain stats: " +
                  $"Total moves: {moveChain.Count}, Card plays: {cardPlays}, Superpowers: {superpowers}, Effects: {effects}");
    }
    
    /// <summary>
    /// Context menu for debugging
    /// </summary>
    [ContextMenu("Log Chain Stats")]
    public void DebugLogChainStats()
    {
        LogChainStats();
    }
    
    [ContextMenu("Reset Chain")]
    public void DebugResetChain()
    {
        ResetChain();
    }
}
