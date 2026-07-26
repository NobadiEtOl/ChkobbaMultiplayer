using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IPowerProcessor implementation for multiplayer mode.
/// Routes superpower activations through the network relay RPCs.
/// </summary>
public class MultiplayerPowerProcessor : IPowerProcessor
{
    private readonly GameNetworkRelay networkRelay;

    public MultiplayerPowerProcessor(GameNetworkRelay relay)
    {
        this.networkRelay = relay;
    }

    public void ExecutePeekOpponentCard()
    {
        networkRelay.UsePeekOpponentCardPowerServerRPC();
    }

    public void ExecuteBayaBayaBak()
    {
        networkRelay.UseBayaBayaBakServerRPC();
    }

    public void ExecuteSwapCardWithOpponent()
    {
        networkRelay.UseRandomDegisTokusServerRPC();
    }

    public void ExecuteValeArar()
    {
        // Vale Arar is client-local in multiplayer, but we still define it here.
        // GameManager handles the local logic if networkRelay is null (or it can just do it here).
        // Since it doesn't need an RPC, we just call the local method.
        GameManager.LocalInstance.ActivateValeArarPower();
    }

    public void ExecuteBomba()
    {
        networkRelay.BombaServerRPC();
    }

    public void ExecuteYapamazsın()
    {
        networkRelay.ActivateYapamazsınServerRPC();
    }

    public void StartKapkacSelection()
    {
        GameManager.LocalInstance.StartKapkacSelectionPower();
    }

    public void ExecuteKapkacOnCard(string cardId)
    {
        networkRelay.ActivateKapkacOnCardServerRPC(cardId);
    }

    public void StartYandimAnamSelection()
    {
        GameManager.LocalInstance.StartYandimAnamSelectionPower();
    }

    public void ExecuteYandimAnamOnCard(string cardId)
    {
        networkRelay.ActivateYandimAnamOnCardServerRPC(cardId);
    }

    public void ExecuteBlockNextPlayer()
    {
        networkRelay.ActivateOynayamazsinServerRPC();
    }

    public void StartKopyalaYapistirSelection()
    {
        GameManager.LocalInstance.StartKopyalaYapistirDualSelection(null);
    }

    public void ExecuteKopyalaYapistir(string targetId, string sourceId)
    {
        networkRelay.KopyalaYapistirServerRPC(targetId, sourceId);
    }

    public void ExecuteVerZehri()
    {
        networkRelay.ActivateVerZehriServerRPC();
    }

    public void ExecuteKutsalDeste()
    {
        networkRelay.ActivateKutsalDesteServerRPC();
    }

    public void StartBuDahaIyiSelection()
    {
        GameManager.LocalInstance.StartBuDahaIyiSelectionPower();
    }

    public void ExecuteBuDahaIyi(string handCardId, string topCenterCardId)
    {
        networkRelay.UseBuDahaIyiServerRPC(handCardId, topCenterCardId);
    }

    public void StartSunuDegisTokusSelection()
    {
        GameManager.LocalInstance.StartSunuDegisTokusDualSelection(null, "Şunu Değiş Tokuş");
    }

    public void ExecuteSunuDegisTokus(string myCardId, string oppCardId)
    {
        networkRelay.UseSunuDegisTokusServerRPC(myCardId, oppCardId);
    }

    public void StartSunuDegisBunuTokusSelection()
    {
        GameManager.LocalInstance.ActivateSunuDegisBunuTokusPower();
    }

    public void ExecuteSunuDegisBunuTokus(string[] myCards, string[] oppCards)
    {
        // Şunu Değiş Bunu Tokuş sends multiple RPCs currently in GameManager.
        // In the processor, we'll need to adapt how it's called.
        // For multiplayer, the loop is currently in GameManager.TrySunuDegisBunuTokus selection logic.
        // I will let GameManager continue to call the RPCs directly or move them here.
        // Actually, the interface should probably match what the power needs.
        
        // Refactoring SunuDegisBunuTokus to use the array if possible, 
        // but current RPC takes individual pairs.
        for (int i = 0; i < myCards.Length; i++)
        {
            bool isLast = (i == myCards.Length - 1);
            networkRelay.UseSunuDegisBunuTokusServerRPC(myCards[i], oppCards[i], i, isLast);
        }
    }

    public void ExecuteZaferPuani(int points)
    {
        networkRelay.ActivateZaferPuaniServerRPC(points);
    }

    public void OnGameStateMutated()
    {
        // Save game state for reconnection sync after power completion.
        Server.Singleton?.SaveCurrentGameState();
    }
}
