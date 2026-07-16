using System.Collections.Generic;

/// <summary>
/// Abstracts move processing for both singleplayer and multiplayer modes.
/// GameManager routes every card play through this interface so that
/// multiplayer RPC calls never leak into singleplayer code paths.
/// </summary>
public interface IMoveProcessor
{
    /// <summary>True when this processor should handle card plays.</summary>
    bool IsActive { get; }

    /// <summary>Display name for logging ("Singleplayer" or "Multiplayer").</summary>
    string ModeName { get; }

    /// <summary>
    /// Process a card play. Implementation decides whether to call RPCs
    /// or route directly to singleplayer validation.
    /// </summary>
    void ProcessCardPlay(
        string cardId,
        Dictionary<string, int[]> centerCards,
        int sumValue,
        int cardValue,
        int playerNumber
    );
}
