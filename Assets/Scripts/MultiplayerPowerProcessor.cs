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

    public void ExecuteSwapCardWithOpponent(string selectedCardId)
    {
        networkRelay.UseDegisTokusWithSelectedCardServerRPC(selectedCardId);
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

    public void StartDegisTokusSelection()
    {
        GameManager.LocalInstance.StartDegisTokusSelectionPower();
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
        GameManager.LocalInstance.StartSunuDegisBunuTokusPower();
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
