using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages buffering of moves during client synchronization to prevent desync issues
/// </summary>
[Serializable]
public class MoveBuffer
{
    [SerializeField] private List<GameMove> bufferedMoves = new List<GameMove>();
    [SerializeField] private Dictionary<int, float> moveTimeouts = new Dictionary<int, float>();
    [SerializeField] private float moveTimeoutDuration = 30f; // 30 seconds timeout for buffered moves
    [SerializeField] private int maxBufferSize = 100; // Maximum number of buffered moves
    
    private bool isBuffering = false;
    private float bufferStartTime = 0f;
    
    /// <summary>
    /// Event fired when a move is buffered
    /// </summary>
    public event Action<GameMove> OnMoveBuffered;
    
    /// <summary>
    /// Event fired when buffered moves are applied
    /// </summary>
    public event Action<GameMove[]> OnBufferedMovesApplied;
    
    /// <summary>
    /// Event fired when a buffered move times out
    /// </summary>
    public event Action<GameMove> OnMoveTimedOut;
    
    public bool IsBuffering => isBuffering;
    public int BufferedMoveCount => bufferedMoves.Count;
    public float BufferDuration => isBuffering ? Time.time - bufferStartTime : 0f;
    
    /// <summary>
    /// Starts buffering moves
    /// </summary>
    public void StartBuffering()
    {
        if (isBuffering)
        {
            Debug.LogWarning("[MoveBuffer] Already buffering moves - ignoring start request");
            return;
        }
        
        Debug.Log("[MoveBuffer] Starting move buffering");
        isBuffering = true;
        bufferStartTime = Time.time;
        bufferedMoves.Clear();
        moveTimeouts.Clear();
    }
    
    /// <summary>
    /// Stops buffering and returns all buffered moves
    /// </summary>
    public GameMove[] StopBuffering()
    {
        if (!isBuffering)
        {
            Debug.LogWarning("[MoveBuffer] Not currently buffering - returning empty array");
            return new GameMove[0];
        }
        
        float bufferDuration = Time.time - bufferStartTime;
        Debug.Log($"[MoveBuffer] Stopping move buffering after {bufferDuration:F2} seconds - {bufferedMoves.Count} moves buffered");
        
        isBuffering = false;
        var moves = bufferedMoves.ToArray();
        
        // Fire event
        OnBufferedMovesApplied?.Invoke(moves);
        
        // Clear buffers
        bufferedMoves.Clear();
        moveTimeouts.Clear();
        
        return moves;
    }
    
    /// <summary>
    /// Buffers a move if buffering is active
    /// </summary>
    public bool BufferMove(GameMove move)
    {
        if (!isBuffering)
        {
            Debug.LogWarning($"[MoveBuffer] Not buffering - ignoring move {move.moveType} by P{move.playerNumber}");
            return false;
        }
        
        // Check buffer size limit
        if (bufferedMoves.Count >= maxBufferSize)
        {
            Debug.LogError($"[MoveBuffer] Buffer full ({maxBufferSize} moves) - dropping oldest move");
            RemoveOldestMove();
        }
        
        Debug.Log($"[MoveBuffer] Buffering move {move.moveId}: {move.moveType} by P{move.playerNumber}");
        
        bufferedMoves.Add(move);
        moveTimeouts[move.moveId] = Time.time + moveTimeoutDuration;
        
        // Fire event
        OnMoveBuffered?.Invoke(move);
        
        return true;
    }
    
    /// <summary>
    /// Forces buffering to stop and clears all buffered moves (emergency stop)
    /// </summary>
    public void ClearBuffer()
    {
        Debug.LogWarning($"[MoveBuffer] Clearing buffer - {bufferedMoves.Count} moves will be lost");
        
        isBuffering = false;
        bufferedMoves.Clear();
        moveTimeouts.Clear();
    }
    
    /// <summary>
    /// Checks for timed out moves and removes them
    /// </summary>
    public void CheckForTimeouts()
    {
        if (!isBuffering) return;
        
        var currentTime = Time.time;
        var timedOutMoves = new List<int>();
        
        foreach (var kvp in moveTimeouts)
        {
            if (currentTime > kvp.Value)
            {
                timedOutMoves.Add(kvp.Key);
            }
        }
        
        foreach (var moveId in timedOutMoves)
        {
            var timedOutMove = bufferedMoves.FirstOrDefault(m => m.moveId == moveId);
            if (timedOutMove.moveId != 0) // Default value check
            {
                Debug.LogWarning($"[MoveBuffer] Move {moveId} timed out - removing from buffer");
                bufferedMoves.Remove(timedOutMove);
                moveTimeouts.Remove(moveId);
                
                // Fire event
                OnMoveTimedOut?.Invoke(timedOutMove);
            }
        }
    }
    
    /// <summary>
    /// Gets all currently buffered moves (for debugging)
    /// </summary>
    public GameMove[] GetBufferedMoves()
    {
        return bufferedMoves.ToArray();
    }
    
    /// <summary>
    /// Gets buffer statistics for debugging
    /// </summary>
    public string GetBufferStats()
    {
        if (!isBuffering)
        {
            return "[MoveBuffer] Not currently buffering";
        }
        
        var duration = Time.time - bufferStartTime;
        var avgMovesPerSecond = bufferedMoves.Count / Math.Max(duration, 0.1f);
        
        return $"[MoveBuffer] Buffering for {duration:F2}s | {bufferedMoves.Count} moves | {avgMovesPerSecond:F1} moves/sec";
    }
    
    /// <summary>
    /// Removes the oldest move from the buffer
    /// </summary>
    private void RemoveOldestMove()
    {
        if (bufferedMoves.Count > 0)
        {
            var oldestMove = bufferedMoves[0];
            bufferedMoves.RemoveAt(0);
            moveTimeouts.Remove(oldestMove.moveId);
            Debug.LogWarning($"[MoveBuffer] Removed oldest move {oldestMove.moveId}: {oldestMove.moveType} by P{oldestMove.playerNumber}");
        }
    }
    
    /// <summary>
    /// Validates move order and integrity
    /// </summary>
    public bool ValidateBufferedMoves()
    {
        if (!isBuffering && bufferedMoves.Count == 0) return true;
        
        // Check for duplicate move IDs
        var moveIds = bufferedMoves.Select(m => m.moveId).ToList();
        var uniqueIds = moveIds.Distinct().ToList();
        
        if (moveIds.Count != uniqueIds.Count)
        {
            Debug.LogError("[MoveBuffer] Duplicate move IDs detected in buffer!");
            return false;
        }
        
        // Check for proper ordering (move IDs should be sequential)
        var sortedIds = uniqueIds.OrderBy(id => id).ToList();
        for (int i = 1; i < sortedIds.Count; i++)
        {
            if (sortedIds[i] != sortedIds[i-1] + 1)
            {
                Debug.LogWarning($"[MoveBuffer] Non-sequential move IDs detected: {sortedIds[i-1]} -> {sortedIds[i]}");
                // This is a warning, not an error - gaps in IDs can happen during normal gameplay
            }
        }
        
        Debug.Log($"[MoveBuffer] Buffer validation passed - {bufferedMoves.Count} moves are valid");
        return true;
    }
}
