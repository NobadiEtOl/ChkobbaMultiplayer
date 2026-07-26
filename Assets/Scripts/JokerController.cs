using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages joker definitions, selection UI, passive effects, and damage modifiers.
/// Jokers are passive modifiers that affect damage calculations.
/// Similar to Balatro's joker system.
/// </summary>
public static class JokerController
{
    [System.Serializable]
    public class JokerModifiers
    {
        public float damageMultiplier = 1.0f;      // 1.0 = no change, 1.5 = +50%
        public int extraDamagePerCapture = 0;      // +X to all captures
        public int jackCaptureBonusDamage = 0;     // +X to Jack captures only
        public int pistiCaptureBonusDamage = 0;    // +X to pişti captures

        public override string ToString()
        {
            return $"[Mult:{damageMultiplier}x | Extra:{extraDamagePerCapture} | Jack+:{jackCaptureBonusDamage} | Pişti+:{pistiCaptureBonusDamage}]";
        }
    }

    [System.Serializable]
    public class JokerDefinition
    {
        public int jokerID;
        public string jokerName;
        public string description;
        public Sprite jokerImage;
        public JokerModifiers modifiers;
        public JokerRarity rarity;

        public override string ToString()
        {
            return $"[{jokerID}] {jokerName}: {modifiers}";
        }
    }

    public enum JokerRarity
    {
        Common,
        Uncommon,
        Rare
    }

    // Static pool of jokers (loaded from ScriptableObject in production)
    private static List<JokerDefinition> jokerPool = new List<JokerDefinition>();
    private static bool jokerPoolInitialized = false;

    // Currently active joker for the run
    private static JokerDefinition activeJoker = null;

    /// <summary>
    /// Display joker selection panel with 3 random options.
    /// Player selects one, then callback is invoked with joker ID.
    /// </summary>
    public static void DisplayJokerSelectionPanel(
        List<JokerDefinition> options,
        System.Action<int> onSelectCallback)
    {
        if (options == null || options.Count == 0)
        {
            
            return;
        }

        

        // Find the joker selection panel UI
        // TODO: Implement UI display logic
        // For now, just log the options and simulate a selection after delay
        
        foreach (var joker in options)
        {
            
        }

        // TODO: Show UI panel, wait for player click
        // For testing, auto-select first joker after delay
        // UnityEngine.MonoBehaviour.StartCoroutine(SimulateJokerSelection(options[0].jokerID, onSelectCallback));
    }

    /// <summary>
    /// Generate random joker options from the pool.
    /// </summary>
    public static List<JokerDefinition> GenerateRandomJokerOptions(int count, UnityEngine.Random.State randomState)
    {
        EnsureJokerPoolInitialized();

        if (jokerPool.Count == 0)
        {
            
            return new List<JokerDefinition>();
        }

        var options = new List<JokerDefinition>();
        var usedIndices = new HashSet<int>();

        for (int i = 0; i < count && i < jokerPool.Count; i++)
        {
            int randomIndex;
            do
            {
                randomIndex = UnityEngine.Random.Range(0, jokerPool.Count);
            } while (usedIndices.Contains(randomIndex));

            usedIndices.Add(randomIndex);
            options.Add(jokerPool[randomIndex]);
        }

        
        return options;
    }

    /// <summary>
    /// Get joker definition by ID.
    /// </summary>
    public static JokerDefinition GetJokerById(int jokerID)
    {
        EnsureJokerPoolInitialized();

        foreach (var joker in jokerPool)
        {
            if (joker.jokerID == jokerID)
            {
                return joker;
            }
        }

        
        return null;
    }

    /// <summary>
    /// Set the active joker for the current run.
    /// </summary>
    public static void SetActiveJoker(int jokerID)
    {
        activeJoker = GetJokerById(jokerID);
        if (activeJoker != null)
        {
            
        }
    }

    /// <summary>
    /// Get the modifiers of the currently active joker.
    /// </summary>
    public static JokerModifiers GetActiveJokerModifiers(int jokerID)
    {
        if (jokerID < 0)
        {
            // No joker selected, return neutral modifiers
            return new JokerModifiers();
        }

        var joker = GetJokerById(jokerID);
        if (joker != null)
        {
            return joker.modifiers;
        }

        
        return new JokerModifiers();
    }

    /// <summary>
    /// Initialize joker pool with placeholder jokers.
    /// In production, this would load from a ScriptableObject.
    /// </summary>
    private static void EnsureJokerPoolInitialized()
    {
        if (jokerPoolInitialized && jokerPool.Count > 0)
        {
            return;
        }

        

        // Clear and rebuild pool
        jokerPool.Clear();

        // Create 10 placeholder jokers with random modifiers
        System.Random rng = new System.Random();

        for (int i = 0; i < 10; i++)
        {
            var joker = new JokerDefinition
            {
                jokerID = i,
                jokerName = $"Joker {i}",
                description = $"Placeholder joker {i}",
                jokerImage = null, // Placeholder white image would go here
                rarity = (JokerRarity)(i % 3),
                modifiers = new JokerModifiers
                {
                    damageMultiplier = 0.8f + (float)rng.NextDouble() * 0.7f, // 0.8 - 1.5
                    extraDamagePerCapture = rng.Next(0, 3),                    // 0-2
                    jackCaptureBonusDamage = rng.Next(0, 2),                   // 0-1
                    pistiCaptureBonusDamage = rng.Next(0, 3)                   // 0-2
                }
            };

            jokerPool.Add(joker);
            
        }

        jokerPoolInitialized = true;
        
    }

    /// <summary>
    /// Debug method to print all jokers in the pool.
    /// </summary>
    [ContextMenu("Print Joker Pool")]
    public static void PrintJokerPool()
    {
        EnsureJokerPoolInitialized();

        
        foreach (var joker in jokerPool)
        {
            
        }
    }
}
