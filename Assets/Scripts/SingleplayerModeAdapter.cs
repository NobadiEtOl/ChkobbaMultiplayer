using UnityEngine;

/// <summary>
/// IModeAdapter implementation for singleplayer mode.
/// Routes all power transport via SinglePlayerModeController method calls.
/// 
/// This is a thin wrapper — all business logic is in PowerOrchestrator.
/// This class only knows: "How do I execute this power in singleplayer?"
/// </summary>
public class SingleplayerModeAdapter : MonoBehaviour, IModeAdapter
{
    private SinglePlayerModeController controller;

    private void Awake()
    {
        controller = SinglePlayerModeController.Instance;
        if (controller == null)
        {
            
        }
    }

    /// <summary>Lazy-loads controller if not yet initialized.</summary>
    private SinglePlayerModeController GetController()
    {
        if (controller == null)
        {
            controller = SinglePlayerModeController.Instance;
        }
        return controller;
    }

    // ===== SIMPLE POWERS =====

    public void ExecutePeekOpponentCard()
    {
        var ctrl = GetController();
        if (ctrl == null) return;
        ctrl.ExecutePeekOpponentCard();
    }

    public void ExecuteBayaBayaBak()
    {
        var ctrl = GetController();
        if (ctrl == null) return;
        ctrl.ExecuteBayaBayaBak();
    }

    public void ExecuteValeArar()
    {
        var ctrl = GetController();
        if (ctrl == null) return;
        ctrl.ExecuteValeArar();
    }

    public void ExecuteSwapCardWithOpponent()
    {
        var ctrl = GetController();
        if (ctrl == null) return;
        ctrl.ExecuteSwapCardWithOpponent();
    }

    public void ExecuteBomba()
    {
        var ctrl = GetController();
        if (ctrl == null) return;
        ctrl.ExecuteBomba();
    }

    public void ExecuteBlockNextPlayer()
    {
        var ctrl = GetController();
        if (ctrl == null) return;
        ctrl.ExecuteBlockNextPlayer();
    }

    public void ExecuteYapamazsın()
    {
        var ctrl = GetController();
        if (ctrl == null) return;
        ctrl.ExecuteYapamazsın();
    }

    public void ExecuteVerZehri()
    {
        var ctrl = GetController();
        if (ctrl == null) return;
        ctrl.ExecuteVerZehri();
    }

    public void ExecuteKutsalDeste()
    {
        var ctrl = GetController();
        if (ctrl == null) return;
        ctrl.ExecuteKutsalDeste();
    }

    public void ExecuteZaferPuani(int points)
    {
        var ctrl = GetController();
        if (ctrl == null) return;
        ctrl.ExecuteZaferPuani(points);
    }

    // ===== COMPLEX POWERS: Selection-based =====

    public void PrepareForInteractivePowerSelection(string powerName, float timeoutSeconds)
    {
        // Singleplayer: no server timers to pause/start.
        // PowerOrchestrator handles UI setup (showcase, info box, etc).
        // This method is a no-op for singleplayer.
    }

    public void CancelInteractivePowerSelection()
    {
        // Singleplayer: no server timers to resume.
        // PowerOrchestrator handles UI teardown (exit showcase, close info box, etc).
        // This method is a no-op for singleplayer.
    }

    // --- Single-card selection powers ---

    public void ExecuteKapkacOnCard(string cardId)
    {
        var ctrl = GetController();
        if (ctrl == null) return;
        ctrl.ExecuteKapkacOnCard(cardId);
    }

    public void ExecuteYandimAnamOnCard(string cardId)
    {
        var ctrl = GetController();
        if (ctrl == null) return;
        ctrl.ExecuteYandimAnamOnCard(cardId);
    }

    public void ExecuteBuDahaIyi(string handCardId, string centerCardId)
    {
        var ctrl = GetController();
        if (ctrl == null) return;
        ctrl.ExecuteBuDahaIyi(handCardId, centerCardId);
    }

    // --- Dual-selection powers ---

    public void ExecuteKopyalaYapistir(string targetId, string sourceId)
    {
        var ctrl = GetController();
        if (ctrl == null) return;
        ctrl.ExecuteKopyalaYapistir(targetId, sourceId);
    }

    public void ExecuteSunuDegisTokus(string myCardId, string oppCardId)
    {
        var ctrl = GetController();
        if (ctrl == null) return;
        ctrl.ExecuteSunuDegisTokus(myCardId, oppCardId);
    }

    // --- Multi-swap sequential power ---

    public void ExecuteSunuDegisBunuTokus(string[] myCards, string[] oppCards)
    {
        var ctrl = GetController();
        if (ctrl == null) return;
        ctrl.ExecuteSunuDegisBunuTokus(myCards, oppCards);
    }

    // ===== STATE PERSISTENCE =====

    public void PersistGameStateAfterPower()
    {
        var ctrl = GetController();
        if (ctrl == null) return;
        ctrl.SaveRunState();
    }
}
