using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Flexible post-capture damage evaluation framework.
/// Evaluates ALL registered damage conditions after each capture.
/// Returns total damage dealt, with joker modifier support.
/// Extensible for future damage conditions.
/// </summary>
public static class DamageSystem
{
    [System.Serializable]
    public class CaptureTelemetry
    {
        public List<int> capturedCardValues; // Values of captured cards (e.g., [1, 11, 2])
        public int capturedCardCount;        // Total count of cards captured
        public bool isPişti;                 // Was this a pişti (2 cards, same value)?
        public bool isJackPişti;             // Was this a jack pişti (2 jacks)?
        public int playerNumber;             // Who captured (player 0 or 1)

        public CaptureTelemetry()
        {
            capturedCardValues = new List<int>();
            capturedCardCount = 0;
            isPişti = false;
            isJackPişti = false;
            playerNumber = 0;
        }
    }

    /// <summary>
    /// Abstract base class for damage conditions.
    /// Subclasses implement specific damage evaluation logic.
    /// </summary>
    [System.Serializable]
    public abstract class DamageCondition
    {
        public abstract string ConditionName { get; }
        public abstract int EvaluateDamage(CaptureTelemetry telemetry);

        public virtual string GetDebugInfo()
        {
            return $"[{ConditionName}]";
        }
    }

    /// <summary>
    /// Damage Condition 1: Jack Capture
    /// Triggers when a Jack (value 11) is captured.
    /// Damage: 1
    /// </summary>
    [System.Serializable]
    public class JackCaptureDamage : DamageCondition
    {
        public override string ConditionName => "Jack Capture";
        public int damageAmount = 1;

        public override int EvaluateDamage(CaptureTelemetry telemetry)
        {
            // Check if any captured card is a Jack (value 11)
            bool hasJack = telemetry.capturedCardValues.Contains(11);
            int damage = hasJack ? damageAmount : 0;
                            
            return damage;
        }
    }

    /// <summary>
    /// Damage Condition 2: Normal Capture
    /// Triggers when one or more non-Jack cards are captured.
    /// Damage: 2
    /// </summary>
    [System.Serializable]
    public class NormalCaptureDamage : DamageCondition
    {
        public override string ConditionName => "Normal Capture";
        public int damageAmount = 2;

        public override int EvaluateDamage(CaptureTelemetry telemetry)
        {
            // Check if there's at least one non-Jack card captured
            bool hasNormalCard = telemetry.capturedCardValues.Any(v => v != 11);
            int damage = hasNormalCard ? damageAmount : 0;

            return damage;
        }
    }

    /// <summary>
    /// Damage Condition 3: Regular Pişti
    /// Triggers when 2 cards with the same value are captured (but not both Jacks).
    /// Damage: 3
    /// </summary>
    [System.Serializable]
    public class RegularPistiDamage : DamageCondition
    {
        public override string ConditionName => "Regular Pişti";
        public int damageAmount = 3;

        public override int EvaluateDamage(CaptureTelemetry telemetry)
        {
            // Pişti without Jack
            bool isRegularPisti = telemetry.isPişti && !telemetry.isJackPişti;
            int damage = isRegularPisti ? damageAmount : 0;
    
            return damage;
        }
    }

    /// <summary>
    /// Damage Condition 4: Jack Pişti
    /// Triggers when 2 Jacks are captured together.
    /// Damage: 4
    /// </summary>
    [System.Serializable]
    public class JackPistiDamage : DamageCondition
    {
        public override string ConditionName => "Jack Pişti";
        public int damageAmount = 4;

        public override int EvaluateDamage(CaptureTelemetry telemetry)
        {
            // Both pişti AND jack pişti
            bool isJackPisti = telemetry.isPişti && telemetry.isJackPişti;
            int damage = isJackPisti ? damageAmount : 0;
                
            
            return damage;
        }
    }

    /// <summary>
    /// Evaluate total damage from a capture, considering all registered conditions
    /// and applying joker modifiers.
    /// </summary>
    public static int EvaluateTotalDamage(
        CaptureTelemetry telemetry,
        JokerController.JokerModifiers jokerModifiers)
    {
        // Get default conditions (Jack, Normal, Pişti, JackPişti)
        var conditions = GetDefaultConditions();

        int baseDamage = 0;
        foreach (var condition in conditions)
        {
            int conditionDamage = condition.EvaluateDamage(telemetry);
            baseDamage += conditionDamage;
        }

        

        // Apply joker modifiers
        int modifiedDamage = ApplyJokerModifiers(baseDamage, telemetry, jokerModifiers);

        

        return modifiedDamage;
    }

    /// <summary>
    /// Apply joker modifiers to damage.
    /// Multiplies damage based on joker effects.
    /// </summary>
    private static int ApplyJokerModifiers(
        int baseDamage,
        CaptureTelemetry telemetry,
        JokerController.JokerModifiers jokerModifiers)
    {
        if (jokerModifiers == null)
        {
            return baseDamage;
        }

        float modifiedDamage = baseDamage;

        // Apply damage multiplier
        modifiedDamage *= jokerModifiers.damageMultiplier;

        // Apply extra damage per capture
        modifiedDamage += jokerModifiers.extraDamagePerCapture;

        // Apply specific card type bonuses
        if (telemetry.capturedCardValues.Contains(11))
        {
            modifiedDamage += jokerModifiers.jackCaptureBonusDamage;
        }

        if (telemetry.isPişti)
        {
            modifiedDamage += jokerModifiers.pistiCaptureBonusDamage;
        }
        

        // Round to nearest integer
        return Mathf.RoundToInt(modifiedDamage);
    }

    /// <summary>
    /// Get the default set of 4 damage conditions.
    /// This list defines the base damage evaluation rules.
    /// Can be extended with new conditions in the future.
    /// </summary>
    public static List<DamageCondition> GetDefaultConditions()
    {
        return new List<DamageCondition>
        {
            new JackCaptureDamage(),
            new NormalCaptureDamage(),
            new RegularPistiDamage(),
            new JackPistiDamage()
        };
    }
}
