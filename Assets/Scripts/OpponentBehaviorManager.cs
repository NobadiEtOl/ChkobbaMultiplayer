using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stateless rule-based system for opponent card selection.
/// Given opponent hand and behavior mode, returns card index to play.
/// Supports different difficulty levels with different play patterns.
/// </summary>
public static class OpponentBehaviorManager
{
    public enum OpponentBehaviorMode
    {
        PlayInOrder,      // Cycles through cards 0→1→2→3 (easy)
        Random,           // Pick random valid card (normal/hard)
    }

    [System.Serializable]
    public class OpponentBehaviorConfig
    {
        public OpponentBehaviorMode mode;
        public float randomness; // 0.0-1.0, unused for PlayInOrder
        public System.Random behaviorRng; // Seeded for determinism

        public OpponentBehaviorConfig(OpponentBehaviorMode mode, System.Random rng, float randomness = 0.5f)
        {
            this.mode = mode;
            this.behaviorRng = rng;
            this.randomness = randomness;
        }
    }

    /// <summary>
    /// Select which card the opponent should play given their hand and behavior config.
    /// Returns a valid card index (0-3), or -1 if no cards available.
    /// </summary>
    public static int SelectCardToPlay(
        List<string> opponentHand,
        OpponentBehaviorConfig config,
        int currentTurnIndex)
    {
        if (config == null)
        {
            Debug.LogError("[OpponentBehaviorManager] Config is null!");
            return -1;
        }

        if (opponentHand == null || opponentHand.Count == 0)
        {
            Debug.LogWarning("[OpponentBehaviorManager] Opponent hand is empty");
            return -1;
        }

        int selectedIndex = -1;

        switch (config.mode)
        {
            case OpponentBehaviorMode.PlayInOrder:
                selectedIndex = SelectPlayInOrder(opponentHand, currentTurnIndex);
                break;

            case OpponentBehaviorMode.Random:
                selectedIndex = SelectRandom(opponentHand, config.behaviorRng);
                break;

            default:
                Debug.LogError($"[OpponentBehaviorManager] Unknown behavior mode: {config.mode}");
                selectedIndex = 0;
                break;
        }

        return Mathf.Clamp(selectedIndex, 0, opponentHand.Count - 1);
    }

    /// <summary>
    /// PlayInOrder behavior: Always pick the first card in the hand.
    /// As cards are removed after playing, this results in playing cards in the order they were dealt (0, 1, 2, 3).
    /// Easy difficulty - predictable for player to plan around.
    /// </summary>
    private static int SelectPlayInOrder(List<string> opponentHand, int currentTurnIndex)
    {
        // Always pick the first card. The shrinking hand size ensures we play them in sequence.
        int selectedIndex = 0;
        Debug.Log($"[OpponentBehaviorManager] PlayInOrder: turn {currentTurnIndex}, selecting card index {selectedIndex} (first available)");
        return selectedIndex;
    }

    /// <summary>
    /// Random behavior: Pick random card from hand.
    /// Normal/hard difficulty - less predictable.
    /// </summary>
    private static int SelectRandom(List<string> opponentHand, System.Random rng)
    {
        if (rng == null)
        {
            rng = new System.Random();
        }

        int selectedIndex = rng.Next(0, opponentHand.Count);
        Debug.Log($"[OpponentBehaviorManager] Random: selecting card index {selectedIndex}");
        return selectedIndex;
    }

    /// <summary>
    /// Get behavior config for given difficulty level.
    /// Difficulty 0 (Easy): PlayInOrder
    /// Difficulty 1 (Normal): Random with seed
    /// Difficulty 2 (Hard): Random with different seed
    /// </summary>
    public static OpponentBehaviorConfig GetConfigForDifficulty(int difficulty, int seed)
    {
        OpponentBehaviorMode mode;
        System.Random rng = new System.Random(seed + difficulty); // Different seed per difficulty
        float randomness = 0.5f;

        switch (difficulty)
        {
            case 0: // Easy
                mode = OpponentBehaviorMode.PlayInOrder;
                break;

            case 1: // Normal
                mode = OpponentBehaviorMode.Random;
                randomness = 0.5f;
                break;

            case 2: // Hard
                mode = OpponentBehaviorMode.Random;
                randomness = 0.8f;
                break;

            default:
                Debug.LogWarning($"[OpponentBehaviorManager] Unknown difficulty: {difficulty}, using Easy");
                mode = OpponentBehaviorMode.PlayInOrder;
                break;
        }

        Debug.Log($"[OpponentBehaviorManager] Created config for difficulty {difficulty}: mode={mode}");
        return new OpponentBehaviorConfig(mode, rng, randomness);
    }
}
