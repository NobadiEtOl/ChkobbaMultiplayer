using UnityEngine;

/// <summary>
/// Central logic hub for all joker behaviors.
/// Implements joker effects as static methods organized by event type.
/// Follows SuperPowerController pattern: specific methods per joker, not generic interface.
/// 
/// Currently supports 5 placeholder damage-modifier jokers (0-4).
/// Future: Can extend with new joker types or complex multi-effect jokers.
/// </summary>
public static class JokerLogic
{
    /// <summary>
    /// Apply joker effects when a capture occurs.
    /// Routes to specific joker method based on jokerID.
    /// Returns modified telemetry with joker effects applied.
    /// </summary>
    public static DamageSystem.CaptureTelemetry ApplyJokerOnCapture(
        int jokerID,
        DamageSystem.CaptureTelemetry telemetry)
    {
        if (telemetry == null)
            return telemetry;

        switch (jokerID)
        {
            case 0:
                return OnCapture_JokerFlatPlus2(telemetry);
            case 1:
                return OnCapture_JokerJackCaptureDoubler(telemetry);
            case 2:
                return OnCapture_JokerNormalCaptureDoubler(telemetry);
            case 3:
                return OnCapture_JokerRegularPiştiDoubler(telemetry);
            case 4:
                return OnCapture_JokerJackPiştiDoubler(telemetry);
            case 5:
                return OnCapture_JokerTwoOfClubsBonus(telemetry);
            case 6:
                return OnCapture_JokerTwoOfClubsMultiplier(telemetry);
            case 7:
                return OnCapture_JokerTenOfDiamondsBonus(telemetry);
            case 8:
                return OnCapture_JokerTenOfDiamondsMultiplier(telemetry);
            case 9:
                return OnCapture_JokerActiveJokerBonus(telemetry);
            default:
                return telemetry;  // Unknown joker ID, no effect
        }
    }

    /// <summary>
    /// Called when a round ends (placeholder for future round-end joker effects).
    /// </summary>
    public static void OnRoundEnd(int jokerID, int opponentDifficulty, bool playerWon)
    {
        // Currently not implemented - jokers only affect capture damage for now
    }

    /// <summary>
    /// Called when a stage is cleared (placeholder for future stage-progression joker effects).
    /// </summary>
    public static void OnStageCleared(int jokerID, int stageNumber)
    {
        // Currently not implemented - jokers only affect capture damage for now
    }

    // ===== JOKER 0: Flat +2 =====
    /// <summary>
    /// Real Joker 0: Flat +2 to all damage dealt to opponent.
    /// Applies +2 extra damage bonus to every capture outcome, regardless of capture type.
    /// This bonus is additive and stacks with other future joker effects.
    /// </summary>
    private static DamageSystem.CaptureTelemetry OnCapture_JokerFlatPlus2(
        DamageSystem.CaptureTelemetry telemetry)
    {
        telemetry.extraDamageBonus += 2;  // Additive style to support future stacking
        Debug.Log($"[JokerLogic] Joker 0 (Flat +2) applied: extraDamageBonus={telemetry.extraDamageBonus}");
        return telemetry;
    }

    // ===== JOKER 1: Jack Capture Doubler =====
    /// <summary>
    /// Real Joker 1: Doubles the base damage of Jack captures only (not Jack pişti).
    /// When a Jack is captured without forming a pişti, applies 2x multiplier.
    /// </summary>
    private static DamageSystem.CaptureTelemetry OnCapture_JokerJackCaptureDoubler(
        DamageSystem.CaptureTelemetry telemetry)
    {
        // Check if played card was a Jack (11) and this is not a pişti
        bool playedJack = telemetry.playedCardValue == 11;
        bool isPisti = telemetry.isPişti;
        
        if (playedJack && !isPisti)
        {
            telemetry.damageMultiplier *= 2.0f;
            Debug.Log($"[JokerLogic] Joker 1 (Jack Capture Doubler) applied: damageMultiplier now {telemetry.damageMultiplier}x");
        }
        else if (playedJack && isPisti)
        {
            Debug.Log($"[JokerLogic] Joker 1 (Jack Capture Doubler) skipped: Jack pişti detected, effect does not apply to pişti");
        }
        else
        {
            Debug.Log($"[JokerLogic] Joker 1 (Jack Capture Doubler) skipped: Played card was not a Jack");
        }
        
        return telemetry;
    }

    // ===== JOKER 2: Normal Capture Doubler =====
    /// <summary>
    /// Real Joker 2: Doubles the base damage of normal (non-Jack) captures only (not pişti).
    /// When a non-Jack card is captured without forming a pişti, applies 2x multiplier.
    /// </summary>
    private static DamageSystem.CaptureTelemetry OnCapture_JokerNormalCaptureDoubler(
        DamageSystem.CaptureTelemetry telemetry)
    {
        // Check if played card was NOT a Jack (11) and this is not a pişti
        bool playedNonJack = telemetry.playedCardValue != 11;
        bool isPisti = telemetry.isPişti;
        
        if (playedNonJack && !isPisti)
        {
            telemetry.damageMultiplier *= 2.0f;
            Debug.Log($"[JokerLogic] Joker 2 (Normal Capture Doubler) applied: damageMultiplier now {telemetry.damageMultiplier}x");
        }
        else if (playedNonJack && isPisti)
        {
            Debug.Log($"[JokerLogic] Joker 2 (Normal Capture Doubler) skipped: Pişti detected, effect does not apply to pişti");
        }
        else
        {
            Debug.Log($"[JokerLogic] Joker 2 (Normal Capture Doubler) skipped: Played card was a Jack");
        }
        
        return telemetry;
    }

    // ===== JOKER 3: Regular Pişti Doubler =====
    /// <summary>
    /// Real Joker 3: Doubles the base damage when a regular pişti occurs (2 same non-Jack cards).
    /// Only applies when isPişti is true AND isJackPişti is false.
    /// </summary>
    private static DamageSystem.CaptureTelemetry OnCapture_JokerRegularPiştiDoubler(
        DamageSystem.CaptureTelemetry telemetry)
    {
        // Check if this is a regular pişti (isPişti true but isJackPişti false)
        bool isRegularPisti = telemetry.isPişti && !telemetry.isJackPişti;
        
        if (isRegularPisti)
        {
            telemetry.damageMultiplier *= 2.0f;
            Debug.Log($"[JokerLogic] Joker 3 (Regular Pişti Doubler) applied: damageMultiplier now {telemetry.damageMultiplier}x");
        }
        else if (telemetry.isJackPişti)
        {
            Debug.Log($"[JokerLogic] Joker 3 (Regular Pişti Doubler) skipped: Jack pişti detected, effect only applies to regular pişti");
        }
        else
        {
            Debug.Log($"[JokerLogic] Joker 3 (Regular Pişti Doubler) skipped: Not a pişti");
        }
        
        return telemetry;
    }

    // ===== JOKER 4: Jack Pişti Doubler =====
    /// <summary>
    /// Real Joker 4: Doubles the base damage when a jack pişti occurs (2 Jacks).
    /// Only applies when isJackPişti is true.
    /// </summary>
    private static DamageSystem.CaptureTelemetry OnCapture_JokerJackPiştiDoubler(
        DamageSystem.CaptureTelemetry telemetry)
    {
        // Check if this is a jack pişti
        bool isJackPisti = telemetry.isJackPişti;
        
        if (isJackPisti)
        {
            telemetry.damageMultiplier *= 2.0f;
            Debug.Log($"[JokerLogic] Joker 4 (Jack Pişti Doubler) applied: damageMultiplier now {telemetry.damageMultiplier}x");
        }
        else
        {
            Debug.Log($"[JokerLogic] Joker 4 (Jack Pişti Doubler) skipped: Not a jack pişti");
        }
        
        return telemetry;
    }

    // ===== JOKER 5: Two of Clubs Bonus =====
    /// <summary>
    /// Real Joker 5: Adds +3 bonus damage when 2 of Clubs is captured.
    /// Scans fullCapturedCards directly since special card flags are set after joker processing.
    /// </summary>
    private static DamageSystem.CaptureTelemetry OnCapture_JokerTwoOfClubsBonus(
        DamageSystem.CaptureTelemetry telemetry)
    {
        bool hasTwoOfClubs = false;
        if (telemetry.capturedCardCount > 0)
        {
            foreach (int[] card in telemetry.fullCapturedCards)
            {
                if (card[0] == 1 && card[1] == 2)  // Clubs (1), Value 2
                {
                    hasTwoOfClubs = true;
                    break;
                }
            }
        }

        if (hasTwoOfClubs)
        {
            telemetry.extraDamageBonus += 3;
            Debug.Log($"[JokerLogic] Joker 5 (Two of Clubs Bonus) applied: extraDamageBonus={telemetry.extraDamageBonus}");
        }
        else
        {
            Debug.Log($"[JokerLogic] Joker 5 (Two of Clubs Bonus) skipped: 2 of Clubs not captured");
        }

        return telemetry;
    }

    // ===== JOKER 6: Two of Clubs Multiplier =====
    /// <summary>
    /// Real Joker 6: Doubles the base damage when 2 of Clubs is captured.
    /// Scans fullCapturedCards directly since special card flags are set after joker processing.
    /// </summary>
    private static DamageSystem.CaptureTelemetry OnCapture_JokerTwoOfClubsMultiplier(
        DamageSystem.CaptureTelemetry telemetry)
    {
        bool hasTwoOfClubs = false;
        if (telemetry.capturedCardCount > 0)
        {
            foreach (int[] card in telemetry.fullCapturedCards)
            {
                if (card[0] == 1 && card[1] == 2)  // Clubs (1), Value 2
                {
                    hasTwoOfClubs = true;
                    break;
                }
            }
        }

        if (hasTwoOfClubs)
        {
            telemetry.damageMultiplier *= 2.0f;
            Debug.Log($"[JokerLogic] Joker 6 (Two of Clubs Multiplier) applied: damageMultiplier={telemetry.damageMultiplier}x");
        }
        else
        {
            Debug.Log($"[JokerLogic] Joker 6 (Two of Clubs Multiplier) skipped: 2 of Clubs not captured");
        }

        return telemetry;
    }

    // ===== JOKER 7: Ten of Diamonds Bonus =====
    /// <summary>
    /// Real Joker 7: Adds +3 bonus damage when 10 of Diamonds is captured.
    /// Scans fullCapturedCards directly since special card flags are set after joker processing.
    /// </summary>
    private static DamageSystem.CaptureTelemetry OnCapture_JokerTenOfDiamondsBonus(
        DamageSystem.CaptureTelemetry telemetry)
    {
        bool hasTenOfDiamonds = false;
        if (telemetry.capturedCardCount > 0)
        {
            foreach (int[] card in telemetry.fullCapturedCards)
            {
                if (card[0] == 2 && card[1] == 10)  // Diamonds (2), Value 10
                {
                    hasTenOfDiamonds = true;
                    break;
                }
            }
        }

        if (hasTenOfDiamonds)
        {
            telemetry.extraDamageBonus += 3;
            Debug.Log($"[JokerLogic] Joker 7 (Ten of Diamonds Bonus) applied: extraDamageBonus={telemetry.extraDamageBonus}");
        }
        else
        {
            Debug.Log($"[JokerLogic] Joker 7 (Ten of Diamonds Bonus) skipped: 10 of Diamonds not captured");
        }

        return telemetry;
    }

    // ===== JOKER 8: Ten of Diamonds Multiplier =====
    /// <summary>
    /// Real Joker 8: Doubles the base damage when 10 of Diamonds is captured.
    /// Scans fullCapturedCards directly since special card flags are set after joker processing.
    /// </summary>
    private static DamageSystem.CaptureTelemetry OnCapture_JokerTenOfDiamondsMultiplier(
        DamageSystem.CaptureTelemetry telemetry)
    {
        bool hasTenOfDiamonds = false;
        if (telemetry.capturedCardCount > 0)
        {
            foreach (int[] card in telemetry.fullCapturedCards)
            {
                if (card[0] == 2 && card[1] == 10)  // Diamonds (2), Value 10
                {
                    hasTenOfDiamonds = true;
                    break;
                }
            }
        }

        if (hasTenOfDiamonds)
        {
            telemetry.damageMultiplier *= 2.0f;
            Debug.Log($"[JokerLogic] Joker 8 (Ten of Diamonds Multiplier) applied: damageMultiplier={telemetry.damageMultiplier}x");
        }
        else
        {
            Debug.Log($"[JokerLogic] Joker 8 (Ten of Diamonds Multiplier) skipped: 10 of Diamonds not captured");
        }

        return telemetry;
    }

    // ===== JOKER 9: Active Joker Bonus =====
    /// <summary>
    /// Real Joker 9: Adds bonus damage equal to the number of active jokers the player has.
    /// Scales with the total count of active jokers (including itself).
    /// Example: If player has 2 active jokers, this joker adds +2 bonus damage.
    /// </summary>
    private static DamageSystem.CaptureTelemetry OnCapture_JokerActiveJokerBonus(
        DamageSystem.CaptureTelemetry telemetry)
    {
        if (PassiveManager.Instance == null)
        {
            Debug.LogWarning("[JokerLogic] Joker 9 (Active Joker Bonus) skipped: PassiveManager not found");
            return telemetry;
        }

        int activeJokerCount = PassiveManager.Instance.GetActiveJokerCount();
        telemetry.extraDamageBonus += activeJokerCount;
        
        Debug.Log($"[JokerLogic] Joker 9 (Active Joker Bonus) applied: +{activeJokerCount} bonus damage (active jokers: {activeJokerCount}), extraDamageBonus now={telemetry.extraDamageBonus}");
        return telemetry;
    }
}
