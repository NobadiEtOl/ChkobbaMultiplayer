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
    // Card suit constants (matches Server.cs card creation: 1=Hearts, 2=Diamonds, 3=Clubs, 4=Spades)
    private const int Clubs = 1;
    private const int Diamonds = 2;
    private const int Hearts = 3;
    private const int Spades = 4;

    // Special card constants for bonus damage
    private const int TWO_OF_CLUBS_VALUE = 2;
    private const int TEN_OF_DIAMONDS_VALUE = 10;
    private const int TWO_OF_CLUBS_BONUS_DAMAGE = 2;
    private const int TEN_OF_DIAMONDS_BONUS_DAMAGE = 3;

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
        public int extraDamageBonusApplied;  // Bonus applied (from PassiveManager)
        public int twoOfClubsBonus;          // +2 bonus if 2 of Clubs captured
        public int tenOfDiamondsBonus;       // +3 bonus if 10 of Diamonds captured
        public float finalDamageBeforeRounding;
        public int finalDamage;
        public bool hasAnyPassiveModifier;
        public bool hasSpecialCardBonus;     // True if any special card bonus was applied
    }

    [System.Serializable]
    public class CaptureTelemetry
    {
        public List<int[]> fullCapturedCards; // Full card data {suit, value} for each captured card (e.g., {{3,2}, {2,10}, {1,11}})
        public int capturedCardCount;        // Total count of cards captured
        public bool isPişti;                 // Was this a pişti (2 cards, same value)?
        public bool isJackPişti;             // Was this a jack pişti (2 jacks)?
        public int playerNumber;             // Who captured (player 0 or 1)
        public int playedCardValue;          // Value of the card played to capture
        
        // Special card flags (populated by DetectSpecialCards)
        public bool hasTwoOfClubs = false;    // True if 2 of Clubs was among captured cards
        public bool hasTenOfDiamonds = false; // True if 10 of Diamonds was among captured cards
        
        // Passive effect modifiers (applied by PassiveManager before damage calculation)
        public float damageMultiplier = 1.0f;      // 1.0 = no change, 1.5 = +50%, 2.0 = x2, etc.
        public int extraDamageBonus = 0;           // +X to all captures

        public CaptureTelemetry()
        {
            fullCapturedCards = new List<int[]>();
            capturedCardCount = 0;
            isPişti = false;
            isJackPişti = false;
            playerNumber = 0;
            playedCardValue = 0;
            hasTwoOfClubs = false;
            hasTenOfDiamonds = false;
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
    /// Detect and flag special card captures (2 of Clubs, 10 of Diamonds).
    /// These cards grant bonus damage on top of the base capture type damage.
    /// Modifies telemetry in-place: sets hasTwoOfClubs and hasTenOfDiamonds flags.
    /// </summary>
    private static void DetectSpecialCards(CaptureTelemetry telemetry)
    {
        if (telemetry == null || telemetry.fullCapturedCards == null || telemetry.fullCapturedCards.Count == 0)
        {
            Debug.LogWarning($"[DamageSystem] No captured cards to evaluate for special cards for player {telemetry?.playerNumber ?? -1}"); 
            return;
        }

        telemetry.hasTwoOfClubs = false;
        telemetry.hasTenOfDiamonds = false;

        foreach (var card in telemetry.fullCapturedCards)
        {
            if (card == null || card.Length < 2) continue;

            int suit = card[0];
            int value = card[1];

            // Check for 2 of Clubs
            if (suit == Clubs && value == TWO_OF_CLUBS_VALUE)
            {
                telemetry.hasTwoOfClubs = true;
                Debug.Log($"[DamageSystem] Special card detected: 2 of Clubs for player {telemetry.playerNumber}");
            }

            // Check for 10 of Diamonds
            if (suit == Diamonds && value == TEN_OF_DIAMONDS_VALUE)
            {
                telemetry.hasTenOfDiamonds = true;
                Debug.Log($"[DamageSystem] Special card detected: 10 of Diamonds for player {telemetry.playerNumber}");
            }
        }
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
    /// Applies passive modifiers first, then adds special card bonuses on top.
    /// Useful for logging and debugging passive/joker/special card effect application.
    /// </summary>
    public static DamageEvaluationBreakdown EvaluateDamageBreakdown(CaptureTelemetry telemetry)
    {
        if (telemetry == null)
        {
            return new DamageEvaluationBreakdown();
        }

        // Step 1: Detect special cards (2 of Clubs, 10 of Diamonds) and flag them
        DetectSpecialCards(telemetry);
        Debug.Log($"[DamageSystem] Special card detection: hasTwoOfClubs={telemetry.hasTwoOfClubs}, hasTenOfDiamonds={telemetry.hasTenOfDiamonds} for player {telemetry.playerNumber}");

        // Step 2: Determine which exclusive capture type occurred
        CaptureType captureType = DetermineCaptureType(telemetry);
        int baseDamage = GetBaseDamageForType(captureType);

        // Step 3: Calculate special card bonuses
        int twoOfClubsBonus = telemetry.hasTwoOfClubs ? TWO_OF_CLUBS_BONUS_DAMAGE : 0;
        int tenOfDiamondsBonus = telemetry.hasTenOfDiamonds ? TEN_OF_DIAMONDS_BONUS_DAMAGE : 0;
        int totalSpecialCardBonus = twoOfClubsBonus + tenOfDiamondsBonus;

        // Step 4: Add special card bonuses to base damage before applying modifiers
        float modifiedDamage = baseDamage + totalSpecialCardBonus;

        // Step 5: Apply damage multiplier to the combined base + special bonus (set by passives, default 1.0)
        modifiedDamage *= telemetry.damageMultiplier;

        // Step 6: Apply extra damage bonus per capture (set by passives, default 0)
        modifiedDamage += telemetry.extraDamageBonus;

        Debug.Log($"[DamageSystem] Damage calculation for player {telemetry.playerNumber}: baseDamage={baseDamage}, specialCardBonus={totalSpecialCardBonus} (2C:{twoOfClubsBonus} + 10D:{tenOfDiamondsBonus}), baseWithSpecial={baseDamage + totalSpecialCardBonus}, afterMultiplier={(baseDamage + totalSpecialCardBonus) * telemetry.damageMultiplier}, passiveBonus={telemetry.extraDamageBonus} = totalBeforeRound={modifiedDamage}");

        int finalDamage = Mathf.RoundToInt(modifiedDamage);
        bool hasSpecialCardBonus = twoOfClubsBonus > 0 || tenOfDiamondsBonus > 0;

        return new DamageEvaluationBreakdown
        {
            captureType = captureType,
            baseDamage = baseDamage,
            damageAfterMultiplier = (baseDamage + totalSpecialCardBonus) * telemetry.damageMultiplier,
            extraDamageBonusApplied = telemetry.extraDamageBonus,
            twoOfClubsBonus = twoOfClubsBonus,
            tenOfDiamondsBonus = tenOfDiamondsBonus,
            finalDamageBeforeRounding = modifiedDamage,
            finalDamage = finalDamage,
            hasAnyPassiveModifier = !Mathf.Approximately(telemetry.damageMultiplier, 1.0f)
                                   || telemetry.extraDamageBonus != 0,
            hasSpecialCardBonus = hasSpecialCardBonus
        };
    }
}
