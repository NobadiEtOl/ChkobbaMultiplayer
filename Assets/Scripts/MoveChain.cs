using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Maintains a chain of game moves for sync validation
/// Completely decoupled from existing game logic
/// </summary>
[Serializable]
public struct MoveChain : INetworkSerializable
{
    public int chainVersion;
    public GameMove[] moves;
    public string chainHash; // Hash of entire chain

    public MoveChain(List<GameMove> moveList)
    {
        moves = moveList?.ToArray() ?? new GameMove[0];
        chainVersion = moves.Length;
        chainHash = CalculateChainHash(moves);
    }

    public List<GameMove> ToList()
    {
        return moves != null ? new List<GameMove>(moves) : new List<GameMove>();
    }

    /// <summary>
    /// Calculates hash for the entire chain
    /// </summary>
    private static string CalculateChainHash(GameMove[] moves)
    {
        if (moves == null || moves.Length == 0) return "";
        
        // Simple hash: concatenate all move hashes
        var concatenated = string.Join("|", moves.Select(m => m.moveHash));
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(concatenated));
    }

    /// <summary>
    /// Validates this chain against another chain
    /// </summary>
    public ValidationResult ValidateAgainst(MoveChain otherChain, out int firstMismatchIndex)
    {
        firstMismatchIndex = -1;
        
        // Check version mismatch
        if (chainVersion != otherChain.chainVersion)
        {
            
            return ValidationResult.VersionMismatch;
        }
        
        // Check hash mismatch (quick check)
        if (chainHash != otherChain.chainHash)
        {
            
            
            // Find the specific move that mismatches
            var thisMoves = ToList();
            var otherMoves = otherChain.ToList();
            
            int maxCount = Math.Min(thisMoves.Count, otherMoves.Count);
            for (int i = 0; i < maxCount; i++)
            {
                if (!thisMoves[i].MatchesMove(otherMoves[i]))
                {
                    firstMismatchIndex = i;
                    
                    return ValidationResult.MoveMismatch;
                }
            }
            
            // If all moves match but counts differ
            if (thisMoves.Count != otherMoves.Count)
            {
                firstMismatchIndex = maxCount;
                return ValidationResult.CountMismatch;
            }
            
            return ValidationResult.HashMismatch;
        }
        
        return ValidationResult.Valid;
    }

    public enum ValidationResult
    {
        Valid,
        VersionMismatch,
        MoveMismatch,
        CountMismatch,
        HashMismatch
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref chainVersion);
        serializer.SerializeValue(ref moves);
        serializer.SerializeValue(ref chainHash);
    }
}
