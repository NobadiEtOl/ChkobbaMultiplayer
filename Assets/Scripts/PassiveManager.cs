using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central orchestrator for passive effects (jokers, relics, etc.).
/// Receives game events and applies passive effects to modify game state.
/// 
/// Pattern:
/// 1. PassiveManager is initialized with an active passive ID (joker, relic, etc.)
/// 2. When game events occur, systems call PassiveManager event methods
/// 3. PassiveManager routes to specific passive logic (e.g., JokerLogic)
/// 4. Passive logic modifies the event state and returns it
/// 5. Systems use the modified state for their calculations
/// 
/// This keeps game systems (DamageSystem, turn logic, etc.) independent of passive effects.
/// </summary>
public class PassiveManager : MonoBehaviour
{
    public static PassiveManager Instance { get; private set; }

    private List<int> activeJokerIds = new List<int>();  // All active jokers for this run

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Initialize PassiveManager with a specific passive (joker, relic, etc.).
    /// Called when player selects a joker or at game start when resuming a run.
    /// </summary>
    public void SetActivePassive(int passiveId)
    {
        activeJokerIds.Clear();
        if (passiveId >= 0)
            activeJokerIds.Add(passiveId);

        Debug.Log($"[PassiveManager] SetActivePassive: jokerIds=[{string.Join(",", activeJokerIds)}]");
    }

    /// <summary>
    /// Returns the first active joker ID for compatibility with single-joker systems.
    /// </summary>
    public int GetActivePassiveId()
    {
        return activeJokerIds.Count > 0 ? activeJokerIds[0] : -1;
    }

    /// <summary>
    /// Returns the count of currently active jokers.
    /// Used by joker effects that scale with the number of active jokers.
    /// </summary>
    public int GetActiveJokerCount()
    {
        return activeJokerIds.Count;
    }

    /// <summary>
    /// EVENT: Called when a capture occurs.
    /// Passives can modify the telemetry before damage calculation.
    /// Returns modified telemetry.
    /// </summary>
    public DamageSystem.CaptureTelemetry OnCapture(DamageSystem.CaptureTelemetry telemetry)
    {
        if (telemetry == null)
        {
            Debug.LogWarning("[PassiveManager] OnCapture called with null telemetry.");
            return telemetry;
        }

        // Reset modifiers to neutral each capture; jokers build them up from scratch
        telemetry.damageMultiplier = 1.0f;
        telemetry.extraDamageBonus = 0;

        Debug.Log($"[PassiveManager] OnCapture START | jokers=[{string.Join(",", activeJokerIds)}]");

        foreach (int jokerId in activeJokerIds)
        {
            telemetry = JokerLogic.ApplyJokerOnCapture(jokerId, telemetry);
            Debug.Log($"[PassiveManager] Joker {jokerId} processed");
        }

        Debug.Log($"[PassiveManager] OnCapture END | FinalMods=[Mult:{telemetry.damageMultiplier:0.##}x, Extra:{telemetry.extraDamageBonus}]");
        return telemetry;
    }

    /// <summary>
    /// EVENT: Called when a round ends.
    /// Passives can react to round completion (e.g., bonus effects, state resets).
    /// </summary>
    public void OnRoundEnd(int opponentDifficulty, bool playerWon)
    {
        foreach (int jokerId in activeJokerIds)
            JokerLogic.OnRoundEnd(jokerId, opponentDifficulty, playerWon);

        Debug.Log($"[PassiveManager] OnRoundEnd: difficulty={opponentDifficulty}, playerWon={playerWon}");
    }

    /// <summary>
    /// EVENT: Called when a stage is cleared.
    /// Passives can react to stage progression.
    /// </summary>
    public void OnStageCleared(int stageNumber)
    {
        foreach (int jokerId in activeJokerIds)
            JokerLogic.OnStageCleared(jokerId, stageNumber);

        Debug.Log($"[PassiveManager] OnStageCleared: Stage {stageNumber}");
    }

    /// <summary>
    /// Clear the active passive (called at run end or when restarting).
    /// </summary>
    public void ClearActivePassive()
    {
        activeJokerIds.Clear();
    }
}
