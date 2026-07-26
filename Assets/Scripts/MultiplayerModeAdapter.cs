using UnityEngine;

/// <summary>
/// IModeAdapter implementation for multiplayer mode.
/// Routes all power transport via GameNetworkRelay ServerRPCs.
/// 
/// This is a thin wrapper — all business logic is in PowerOrchestrator.
/// This class only knows: "How do I send this power to the server?"
/// </summary>
public class MultiplayerModeAdapter : MonoBehaviour, IModeAdapter
{
    private GameNetworkRelay networkRelay;

    private void Awake()
    {
        networkRelay = FindObjectOfType<GameNetworkRelay>();
        if (networkRelay == null)
        {
            
        }
    }

    // ===== SIMPLE POWERS =====

    public void ExecutePeekOpponentCard()
    {
        if (networkRelay == null) return;
        networkRelay.UsePeekOpponentCardPowerServerRPC();
    }

    public void ExecuteBayaBayaBak()
    {
        if (networkRelay == null) return;
        networkRelay.UseBayaBayaBakServerRPC();
    }

    public void ExecuteValeArar()
    {
        // Vale Arar is client-local in multiplayer (no RPC needed).
        // Logic already handled in PowerOrchestrator; adapter does nothing.
    }

    public void ExecuteSwapCardWithOpponent()
    {
        if (networkRelay == null) return;
        networkRelay.UseRandomDegisTokusServerRPC();
    }

    public void ExecuteBomba()
    {
        if (networkRelay == null) return;
        networkRelay.BombaServerRPC();
    }

    public void ExecuteBlockNextPlayer()
    {
        if (networkRelay == null) return;
        networkRelay.ActivateOynayamazsinServerRPC();
    }

    public void ExecuteYapamazsın()
    {
        if (networkRelay == null) return;
        networkRelay.ActivateYapamazsınServerRPC();
    }

    public void ExecuteVerZehri()
    {
        if (networkRelay == null) return;
        networkRelay.ActivateVerZehriServerRPC();
    }

    public void ExecuteKutsalDeste()
    {
        if (networkRelay == null) return;
        networkRelay.ActivateKutsalDesteServerRPC();
    }

    public void ExecuteZaferPuani(int points)
    {
        if (networkRelay == null) return;
        networkRelay.ActivateZaferPuaniServerRPC(points);
    }

    // ===== COMPLEX POWERS: Selection-based =====

    public void PrepareForInteractivePowerSelection(string powerName, float timeoutSeconds)
    {
        if (networkRelay == null) return;
        // Pause turn timer so it doesn't expire during power selection
        networkRelay.PauseTurnTimerForPowerServerRPC();
        // Start power selection timeout; if it expires, power is cancelled
        networkRelay.StartPowerDurationTimerServerRPC();
    }

    public void CancelInteractivePowerSelection()
    {
        if (networkRelay == null) return;
        // Server will resume turn timer after cancellation
        // (CancelDualSelectionPower ClientRPC is called by server)
    }

    // --- Single-card selection powers ---

    public void ExecuteKapkacOnCard(string cardId)
    {
        if (networkRelay == null) return;
        networkRelay.ActivateKapkacOnCardServerRPC(cardId);
    }

    public void ExecuteYandimAnamOnCard(string cardId)
    {
        if (networkRelay == null) return;
        networkRelay.ActivateYandimAnamOnCardServerRPC(cardId);
    }

    public void ExecuteBuDahaIyi(string handCardId, string centerCardId)
    {
        if (networkRelay == null) return;
        networkRelay.UseBuDahaIyiServerRPC(handCardId, centerCardId);
    }

    // --- Dual-selection powers ---

    public void ExecuteKopyalaYapistir(string targetId, string sourceId)
    {
        if (networkRelay == null) return;
        networkRelay.KopyalaYapistirServerRPC(targetId, sourceId);
    }

    public void ExecuteSunuDegisTokus(string myCardId, string oppCardId)
    {
        if (networkRelay == null) return;
        networkRelay.UseSunuDegisTokusServerRPC(myCardId, oppCardId);
    }

    // --- Multi-swap sequential power ---

    public void ExecuteSunuDegisBunuTokus(string[] myCards, string[] oppCards)
    {
        if (networkRelay == null) return;
        if (myCards == null || oppCards == null || myCards.Length != oppCards.Length) return;

        // Send one RPC per swap pair, marking the last one with readyToExit=true
        for (int i = 0; i < myCards.Length; i++)
        {
            bool isLast = (i == myCards.Length - 1);
            networkRelay.UseSunuDegisBunuTokusServerRPC(myCards[i], oppCards[i], i, isLast);
        }
    }

    // ===== STATE PERSISTENCE =====

    public void PersistGameStateAfterPower()
    {
        if (Server.Singleton == null) return;
        Server.Singleton.SaveCurrentGameState();
    }
}
