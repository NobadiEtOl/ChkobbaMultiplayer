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
    /// <summary>
    /// Defines the mutually exclusive capture types.
    /// A single capture is only one type; layering does not occur.
    /// </summary>
    public enum CaptureType
    {
        JackPisti,      // 2 Jacks (highest priority)
        RegularPisti,   // 2 same non-Jack cards
        Jack,           // Single Jack or multiple Jacks (but not both captured as pişti)
        Normal,         // At least one non-Jack card
        None         // No capture
    }

    [System.Serializable]
    public class DamageEvaluationBreakdown
    {
        public CaptureType captureType;      // The exclusive capture type that occurred
        public int baseDamage;               // Base damage for the capture type
        public float damageAfterMultiplier;  // After multiplier applied
        public int extraDamageBonusApplied;  // Bonus applied
        public float finalDamageBeforeRounding;
        public int finalDamage;
        public bool hasAnyPassiveModifier;
    }

    [System.Serializable]
    public class CaptureTelemetry
    {
        public List<int> capturedCardValues; // Values of captured cards (e.g., [1, 11, 2])
        public int capturedCardCount;        // Total count of cards captured
        public bool isPişti;                 // Was this a pişti (2 cards, same value)?
        public bool isJackPişti;             // Was this a jack pişti (2 jacks)?
        public int playerNumber;             // Who captured (player 0 or 1)
        public int playedCardValue;          // Value of the card played to capture
        
        // Passive effect modifiers (applied by PassiveManager before damage calculation)
        public float damageMultiplier = 1.0f;      // 1.0 = no change, 1.5 = +50%, 2.0 = x2, etc.
        public int extraDamageBonus = 0;           // +X to all captures

        public CaptureTelemetry()
        {
            capturedCardValues = new List<int>();
            capturedCardCount = 0;
            isPişti = false;
            isJackPişti = false;
            playerNumber = 0;
            playedCardValue = 0;
            damageMultiplier = 1.0f;
            extraDamageBonus = 0;
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
    /// Determine the exclusive capture type from telemetry.
    /// Classification rules (non-pişti is determined by played card value):
    /// 1. JackPişti: isPişti && isJackPişti (2 Jacks)
    /// 2. RegularPişti: isPişti && !isJackPişti (2 same non-Jack cards)
    /// 3. Jack: !isPişti && playedCardValue == 11 (played a Jack, not pişti)
    /// 4. Normal: !isPişti && playedCardValue != 11 (played non-Jack, not pişti)
    /// </summary>
    private static CaptureType DetermineCaptureType(CaptureTelemetry telemetry)
    {
        CaptureType result;
        
        if (telemetry.isPişti && telemetry.isJackPişti)
            result = CaptureType.JackPisti;
        else if (telemetry.isPişti && !telemetry.isJackPişti)
            result = CaptureType.RegularPisti;
        else if (!telemetry.isPişti && telemetry.playedCardValue == 11)
            result = CaptureType.Jack;
        else if (!telemetry.isPişti && telemetry.playedCardValue != 11)
            result = CaptureType.Normal;
        else
            result = CaptureType.None;
        
        Debug.Log($"[DamageSystem] Determined capture type: {result} for player {telemetry.playerNumber}");
        return result;
    }

    /// <summary>
    /// Get the base damage for a given capture type.
    /// Pişti captures (both Jack and Normal) are worth 20; regular captures 2-4.
    /// </summary>
    private static int GetBaseDamageForType(CaptureType captureType)
    {
        return captureType switch
        {
            CaptureType.JackPisti => 20,
            CaptureType.RegularPisti => 10,
            CaptureType.Jack => 2,
            CaptureType.Normal => 4,
            _ => 0
        };
    }

    /// <summary>
    /// Evaluate total damage from a capture, considering all registered conditions
    /// and passive modifiers (already applied to telemetry by PassiveManager).
    /// 
    /// NEW: Telemetry contains passive modifiers set by PassiveManager.OnCapture().
    /// Modifier application happens here using telemetry fields, not via jokerModifiers parameter.
    /// </summary>
    public static int EvaluateTotalDamage(CaptureTelemetry telemetry)
    {
        return EvaluateDamageBreakdown(telemetry).finalDamage;
    }

    /// <summary>
    /// Evaluate total damage and return a detailed breakdown of each calculation step.
    /// Uses exclusive capture type classification (not layered conditions).
    /// Useful for logging and debugging passive/joker effect application.
    /// </summary>
    public static DamageEvaluationBreakdown EvaluateDamageBreakdown(CaptureTelemetry telemetry)
    {
        if (telemetry == null)
        {
            return new DamageEvaluationBreakdown();
        }

        // Determine which exclusive capture type occurred
        CaptureType captureType = DetermineCaptureType(telemetry);
        int baseDamage = GetBaseDamageForType(captureType);

        // Apply passive modifiers already embedded in telemetry
        float modifiedDamage = baseDamage;

        // Apply damage multiplier (set by passives, default 1.0)
        modifiedDamage *= telemetry.damageMultiplier;

        // Apply extra damage bonus per capture (set by passives, default 0)
        modifiedDamage += telemetry.extraDamageBonus;

        int finalDamage = Mathf.RoundToInt(modifiedDamage);

        return new DamageEvaluationBreakdown
        {
            captureType = captureType,
            baseDamage = baseDamage,
            damageAfterMultiplier = baseDamage * telemetry.damageMultiplier,
            extraDamageBonusApplied = telemetry.extraDamageBonus,
            finalDamageBeforeRounding = modifiedDamage,
            finalDamage = finalDamage,
            hasAnyPassiveModifier = !Mathf.Approximately(telemetry.damageMultiplier, 1.0f)
                                   || telemetry.extraDamageBonus != 0
        };
    }
}
