using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PowerOrchestrator is the single control point for ALL superpower logic.
/// 
/// Responsibilities:
/// - Validate power can be used (no conflicting selections, not during opponent turn, etc.)
/// - Manage power state (pending flags, active effects, timeouts)
/// - Orchestrate flow (single selection → dual selection → completion)
/// - Coordinate UI updates and animations
/// - Delegate transport to adapters (via IModeAdapter) — never branches on game mode
///
/// Pattern:
/// 1. SuperPowerController/CardInteraction calls powerOrchestrator.PowerMethod()
/// 2. Orchestrator validates + sets up state
/// 3. Orchestrator calls modeAdapter.TransportMethod() to execute (multiplayer RPC or singleplayer local)
/// 4. Orchestrator calls modeAdapter.PersistGameStateAfterPower() to save
///
/// This keeps business logic (validation, orchestration) MODE-AGNOSTIC.
/// Transport branching (RPC vs local) lives ONLY in adapters.
/// </summary>
public class PowerOrchestrator : MonoBehaviour
{
    private static PowerOrchestrator instance;
    public static PowerOrchestrator Instance => instance;

    private IModeAdapter modeAdapter;
    private GameManager gameManager;

    // ===== STATE: Pending Selections =====
    private bool isKapkacPending = false;
    private bool isYandimAnamPending = false;
    private bool isBuDahaIyiPending = false;
    private bool isDegisTokusPending = false;
    private bool isKopyalaYapistirPhase1Pending = false;  // Select source card
    private bool isKopyalaYapistirPhase2Pending = false;  // Select target card
    private bool isSunuDegisTokusPhase1Pending = false;   // Select my card
    private bool isSunuDegisTokusPhase2Pending = false;   // Select opponent card
    private bool isSunuDegisBunuTokusPending = false;     // Multi-swap pending (used by GameManager for loop state)

    // ===== STATE: Selection Data (for multi-phase powers) =====
    private string kopyalaYapistirSourceCardId;
    private string sunuDegisTokusMyCardId;     // Stores phase 1 selection for phase 2

    // ===== STATE: Active Effects =====
    // NOTE: These mirror GameManager state flags. Use GameManager versions for truth.
    // PowerOrchestrator keeps them for quick validation checks.

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        gameManager = GameManager.LocalInstance;
        modeAdapter = gameManager?.ModeAdapter;
        if (modeAdapter == null)
        {
            
        }
    }

    // ===== VALIDATION HELPERS =====

    /// <summary>Check if any selection power is already pending (mutual exclusivity).</summary>
    private bool IsAnySelectionPending()
    {
        return isKapkacPending || isYandimAnamPending || isBuDahaIyiPending || isDegisTokusPending ||
               isKopyalaYapistirPhase1Pending || isKopyalaYapistirPhase2Pending ||
               isSunuDegisTokusPhase1Pending || isSunuDegisTokusPhase2Pending ||
               isSunuDegisBunuTokusPending;
    }

    /// <summary>Clear all selection pending flags (used when power is completed or cancelled).</summary>
    public void ClearAllSelectionFlags()
    {
        isKapkacPending = false;
        isYandimAnamPending = false;
        isBuDahaIyiPending = false;
        isDegisTokusPending = false;
        isKopyalaYapistirPhase1Pending = false;
        isKopyalaYapistirPhase2Pending = false;
        isSunuDegisTokusPhase1Pending = false;
        isSunuDegisTokusPhase2Pending = false;
        isSunuDegisBunuTokusPending = false;
        
        // Also clear stored card selections
        kopyalaYapistirSourceCardId = null;
        sunuDegisTokusMyCardId = null;
    }

    /// <summary>Ensure modeAdapter is available before transport.</summary>
    private bool ValidateAdapter()
    {
        if (modeAdapter == null)
        {
            
            return false;
        }
        return true;
    }

    // ===== SIMPLE POWERS (instant, no selection) =====

    public void ExecutePeekOpponentCard()
    {
        Debug.Log("[UcundanGözAt] PowerOrchestrator.ExecutePeekOpponentCard() called");
        
        if (!ValidateAdapter())
        {
            Debug.LogError("[UcundanGözAt] PowerOrchestrator: ValidateAdapter() failed - modeAdapter is null");
            return;
        }
        
        Debug.Log("[UcundanGözAt] PowerOrchestrator: Adapter validation passed, delegating to modeAdapter.ExecutePeekOpponentCard()");
        
        // Transport: send RPC (server picks random opponent card, sends back via ClientRPC)
        try
        {
            modeAdapter.ExecutePeekOpponentCard();
            Debug.Log("[UcundanGözAt] PowerOrchestrator: modeAdapter.ExecutePeekOpponentCard() completed successfully");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UcundanGözAt] PowerOrchestrator: modeAdapter.ExecutePeekOpponentCard() threw exception: {ex.Message}\n{ex.StackTrace}");
            return;
        }
        
        Debug.Log("[UcundanGözAt] PowerOrchestrator: Calling PersistGameStateAfterPower()");
        modeAdapter.PersistGameStateAfterPower();
        Debug.Log("[UcundanGözAt] PowerOrchestrator.ExecutePeekOpponentCard() execution complete");
    }

    public void ExecuteBayaBayaBak()
    {
        
        if (!ValidateAdapter()) return;
        
        // Transport: send RPC (server shows opponent's entire hand via ClientRPC)
        modeAdapter.ExecuteBayaBayaBak();
        modeAdapter.PersistGameStateAfterPower();
    }

    public void ExecuteValeArar()
    {
        
        if (!ValidateAdapter()) return;
        
        // Apply effect: show Jack indicators
        gameManager?.ActivateValeArarPower();
        gameManager?.ShowcaseSuperPower("Vale Arar");
        
        // Persistence (SP: save state, MP: N/A)
        modeAdapter.ExecuteValeArar();
        modeAdapter.PersistGameStateAfterPower();
    }

    public void ExecuteSwapCardWithOpponent(string selectedCardId)
    {
        
        if (!ValidateAdapter()) return;
        
        isDegisTokusPending = false;
        
        // Transport: send RPC (server validates and swaps)
        modeAdapter.ExecuteSwapCardWithOpponent(selectedCardId);
        modeAdapter.PersistGameStateAfterPower();
    }

    public void ExecuteBomba()
    {
        
        if (!ValidateAdapter()) return;
        
        // Local effect: clear center cards locally (server-authoritative in MP)
        gameManager?.OnBombaCenter();
        gameManager?.ShowcaseSuperPower("Bomba");
        
        // Transport: send RPC for MP validation
        modeAdapter.ExecuteBomba();
        modeAdapter.PersistGameStateAfterPower();
    }

    public void ExecuteBlockNextPlayer()
    {
        
        if (!ValidateAdapter()) return;
        
        // Apply effect: set block flag
        gameManager?.SetOynayamazsinActive(true);
        gameManager?.ShowcaseSuperPower("Oynayamazsın");
        
        // Transport: send RPC
        modeAdapter.ExecuteBlockNextPlayer();
        modeAdapter.PersistGameStateAfterPower();
    }

    public void ExecuteYapamazsın()
    {
        
        if (!ValidateAdapter()) return;
        
        // Transport: send RPC (server handles blocking logic)
        modeAdapter.ExecuteYapamazsın();
        modeAdapter.PersistGameStateAfterPower();
    }

    public void ExecuteVerZehri()
    {
        
        if (!ValidateAdapter()) return;
        
        // Apply effect: set poison flag
        gameManager?.SetVerZehriActive(true);
        gameManager?.ShowcaseSuperPower("Ver Zehri");
        
        // Transport: send RPC
        modeAdapter.ExecuteVerZehri();
        modeAdapter.PersistGameStateAfterPower();
    }

    public void ExecuteKutsalDeste()
    {
        
        if (!ValidateAdapter()) return;
        
        // Apply effect: set holy flag
        gameManager?.SetKutsalDesteActive(true);
        gameManager?.ShowcaseSuperPower("Kutsal Deste");
        
        // Transport: send RPC
        modeAdapter.ExecuteKutsalDeste();
        modeAdapter.PersistGameStateAfterPower();
    }

    public void ExecuteZaferPuani(int points)
    {
        
        if (!ValidateAdapter()) return;
        
        // Transport: send RPC with points
        modeAdapter.ExecuteZaferPuani(points);
        modeAdapter.PersistGameStateAfterPower();
    }

    // ===== COMPLEX POWERS: Single-Card Selection =====

    public void StartKapkacSelection()
    {
        
        if (IsAnySelectionPending())
        {
            
            return;
        }

        isKapkacPending = true;
        if (ValidateAdapter())
        {
            modeAdapter.PrepareForInteractivePowerSelection("Kapkaç", 30f);
        }
        
        // UI setup (kept in GameManager for now, as it owns deck visualization)
        gameManager?.StartKapkacSelectionPower();
    }

    public void ExecuteKapkacOnCard(string cardId)
    {
        
        if (!isKapkacPending)
        {
            
            return;
        }

        isKapkacPending = false;
        if (ValidateAdapter())
        {
            modeAdapter.ExecuteKapkacOnCard(cardId);
            modeAdapter.PersistGameStateAfterPower();
        }
    }

    public void StartYandimAnamSelection()
    {
        
        if (IsAnySelectionPending())
        {
            
            return;
        }

        isYandimAnamPending = true;
        if (ValidateAdapter())
        {
            modeAdapter.PrepareForInteractivePowerSelection("Yandım Anam", 30f);
        }

        // UI setup (kept in GameManager for now)
        gameManager?.StartYandimAnamSelectionPower();
    }

    public void ExecuteYandimAnamOnCard(string cardId)
    {
        
        if (!isYandimAnamPending)
        {
            
            return;
        }

        isYandimAnamPending = false;
        if (ValidateAdapter())
        {
            modeAdapter.ExecuteYandimAnamOnCard(cardId);
            modeAdapter.PersistGameStateAfterPower();
        }
    }

    public void StartDegisTokusSelection()
    {
        
        if (IsAnySelectionPending())
        {
            
            return;
        }

        isDegisTokusPending = true;
        if (ValidateAdapter())
        {
            modeAdapter.PrepareForInteractivePowerSelection("Değiş Tokuş", 30f);
        }

        // UI setup (kept in GameManager for now)
        gameManager?.StartDegisTokusSelectionPower();
    }

    public void StartBuDahaIyiSelection()
    {
        
        if (IsAnySelectionPending())
        {
            
            return;
        }

        isBuDahaIyiPending = true;
        if (ValidateAdapter())
        {
            modeAdapter.PrepareForInteractivePowerSelection("Bu Daha İyi", 30f);
        }

        // UI setup (kept in GameManager for now)
        gameManager?.StartBuDahaIyiSelectionPower();
    }

    public void ExecuteBuDahaIyi(string handCardId, string centerCardId)
    {
        
        if (!isBuDahaIyiPending)
        {
            
            return;
        }

        isBuDahaIyiPending = false;
        if (ValidateAdapter())
        {
            modeAdapter.ExecuteBuDahaIyi(handCardId, centerCardId);
            modeAdapter.PersistGameStateAfterPower();
        }
    }

    // ===== COMPLEX POWERS: Two-Phase Selection (Kopyala Yapıştır) =====

    public void StartKopyalaYapistirSelection()
    {
        
        if (IsAnySelectionPending())
        {
            
            return;
        }

        isKopyalaYapistirPhase1Pending = true;
        kopyalaYapistirSourceCardId = null;
        if (ValidateAdapter())
        {
            modeAdapter.PrepareForInteractivePowerSelection("Kopyala Yapıştır (Phase 1)", 30f);
        }

        // UI setup
        gameManager?.StartKopyalaYapistirDualSelection(null);
    }

    public void ExecuteKopyalaYapistir(string selectedCardId, string phaseHint = null)
    {
        // Phase 1: Select source card
        if (isKopyalaYapistirPhase1Pending && selectedCardId != null)
        {
            
            kopyalaYapistirSourceCardId = selectedCardId;
            isKopyalaYapistirPhase1Pending = false;
            isKopyalaYapistirPhase2Pending = true;
            if (ValidateAdapter())
            {
                modeAdapter.PrepareForInteractivePowerSelection("Kopyala Yapıştır (Phase 2)", 30f);
            }
            return;
        }

        // Phase 2: Select target card and execute
        if (isKopyalaYapistirPhase2Pending && selectedCardId != null && kopyalaYapistirSourceCardId != null)
        {
            
            isKopyalaYapistirPhase2Pending = false;
            if (ValidateAdapter())
            {
                modeAdapter.ExecuteKopyalaYapistir(selectedCardId, kopyalaYapistirSourceCardId);
                modeAdapter.PersistGameStateAfterPower();
            }
            kopyalaYapistirSourceCardId = null;
            return;
        }

        
    }

    // ===== COMPLEX POWERS: Two-Phase Selection (Şunu Değiş Tokuş) =====

    public void StartSunuDegisTokusSelection()
    {
        
        if (IsAnySelectionPending())
        {
            
            return;
        }

        isSunuDegisTokusPhase1Pending = true;
        if (ValidateAdapter())
        {
            modeAdapter.PrepareForInteractivePowerSelection("Şunu Değiş Tokuş (Phase 1)", 30f);
        }

        // UI setup
        gameManager?.StartSunuDegisTokusDualSelection(null, "Şunu Değiş Tokuş");
    }

    public void ExecuteSunuDegisTokus(string myCardId, string oppCardId = null)
    {
        // DIRECT EXECUTION: Both cards provided (used by ŞunuDeğişBunuTokuş sequential loop)
        if (oppCardId != null && myCardId != null && !isSunuDegisTokusPhase1Pending && !isSunuDegisTokusPhase2Pending)
        {
            if (ValidateAdapter())
            {
                modeAdapter.ExecuteSunuDegisTokus(myCardId, oppCardId);
                modeAdapter.PersistGameStateAfterPower();
            }
            return;
        }
        
        // TWO-PHASE EXECUTION: Used by ŞunuDeğişTokuş dual selection
        // Phase 1: Select my card (oppCardId is null)
        if (isSunuDegisTokusPhase1Pending && myCardId != null && oppCardId == null)
        {
            sunuDegisTokusMyCardId = myCardId;
            isSunuDegisTokusPhase1Pending = false;
            isSunuDegisTokusPhase2Pending = true;
            if (ValidateAdapter())
            {
                modeAdapter.PrepareForInteractivePowerSelection("Şunu Değiş Tokuş (Phase 2)", 30f);
            }
            return;
        }

        // Phase 2: Select opponent card and execute (oppCardId is provided)
        if (isSunuDegisTokusPhase2Pending && oppCardId != null && sunuDegisTokusMyCardId != null)
        {
            isSunuDegisTokusPhase2Pending = false;
            if (ValidateAdapter())
            {
                modeAdapter.ExecuteSunuDegisTokus(sunuDegisTokusMyCardId, oppCardId);
                modeAdapter.PersistGameStateAfterPower();
            }
            sunuDegisTokusMyCardId = null;
            return;
        }
    }

    // ===== COMPLEX POWERS: Multi-Swap Sequential (Şunu Değiş Bunu Tokuş) =====

    public void StartSunuDegisBunuTokusSelection()
    {
        if (IsAnySelectionPending())
        {
            return;
        }

        isSunuDegisBunuTokusPending = true;
        
        if (ValidateAdapter())
        {
            modeAdapter.PrepareForInteractivePowerSelection("Şunu Değiş Bunu Tokuş", 60f);  // Longer timeout for multi-swap
        }

        gameManager?.StartSunuDegisBunuTokusPower();
    }

    // ===== POWER CANCELLATION =====

    /// <summary>
    /// Called when power selection times out or is explicitly cancelled.
    /// Clears pending flags and resumes turn.
    /// </summary>
    public void CancelInteractivePowerSelection()
    {
        
        if (!IsAnySelectionPending())
        {
            return;  // No selection active; nothing to cancel
        }

        ClearAllSelectionFlags();
        if (ValidateAdapter())
        {
            modeAdapter.CancelInteractivePowerSelection();
        }

        // UI cleanup (kept in GameManager for now)
        gameManager?.CancelDualSelectionPower();
    }

    // ===== QUERY METHODS (for external code that needs to check state) =====

    public bool IsKapkacPending => isKapkacPending;
    public bool IsYandimAnamPending => isYandimAnamPending;
    public bool IsBuDahaIyiPending => isBuDahaIyiPending;
    public bool IsKopyalaYapistirPhase1Pending => isKopyalaYapistirPhase1Pending;
    public bool IsKopyalaYapistirPhase2Pending => isKopyalaYapistirPhase2Pending;
    public bool IsSunuDegisTokusPhase1Pending => isSunuDegisTokusPhase1Pending;
    public bool IsSunuDegisTokusPhase2Pending => isSunuDegisTokusPhase2Pending;
    public bool IsSunuDegisBunuTokusPending => isSunuDegisBunuTokusPending;

    // Active effects are now in GameManager (source of truth)
    public bool IsValeArarActive => gameManager?.valeArarActive ?? false;
    public bool IsOynayamazsinActive => gameManager?.oynayamazsinActive ?? false;
    public bool IsVerZehriActive => gameManager?.verZehriActive ?? false;
    public bool IsKutsalDesteActive => gameManager?.kutsalDesteActive ?? false;
}
