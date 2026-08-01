using System.Collections.Generic;

/// <summary>
/// Unified transport interface for game mode branching.
/// Multiplayer: sends RPCs via GameNetworkRelay
/// Singleplayer: calls methods directly on SinglePlayerModeController
/// 
/// Business logic (validation, state, UI) lives in PowerOrchestrator.
/// Mode-specific transport (RPC vs local) lives here.
/// </summary>
public interface IModeAdapter
{
    // ===== SIMPLE POWERS (instant effects, no selection) =====
    
    void ExecutePeekOpponentCard();
    void ExecuteBayaBayaBak();
    void ExecuteValeArar();
    void ExecuteSwapCardWithOpponent(string selectedCardId);
    void ExecuteBomba();
    void ExecuteBlockNextPlayer();
    void ExecuteYapamazsın();
    void ExecuteVerZehri();
    void ExecuteKutsalDeste();
    void ExecuteZaferPuani(int points);
    
    // ===== COMPLEX POWERS: Selection-based =====
    
    /// <summary>
    /// Pause turn timer and start power selection timeout.
    /// Multiplayer: sends RPCs to server to pause/start timers.
    /// Singleplayer: does nothing (no server timers).
    /// </summary>
    void PrepareForInteractivePowerSelection(string powerName, float timeoutSeconds);
    
    /// <summary>
    /// Cancel the active interactive power selection.
    /// Multiplayer: tells server to resume turn timer.
    /// Singleplayer: does nothing.
    /// </summary>
    void CancelInteractivePowerSelection();
    
    // --- Single-card selection powers ---
    void ExecuteKapkacOnCard(string cardId);
    void ExecuteYandimAnamOnCard(string cardId);
    void ExecuteBuDahaIyi(string handCardId, string centerCardId);
    
    // --- Dual-selection powers ---
    void ExecuteKopyalaYapistir(string targetId, string sourceId);
    void ExecuteSunuDegisTokus(string myCardId, string oppCardId);
    
    // ===== STATE PERSISTENCE =====
    
    /// <summary>
    /// Save game state after power execution.
    /// Multiplayer: save to server for reconnection sync.
    /// Singleplayer: save run state to disk.
    /// </summary>
    void PersistGameStateAfterPower();
    void ExecuteSunuDegisTokuOnCard(string cardId);
}
