using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IPowerProcessor implementation for singleplayer mode.
/// Routes superpower activations directly to SinglePlayerModeController.
/// </summary>
public class SingleplayerPowerProcessor : IPowerProcessor
{
    private readonly SinglePlayerModeController controller;

    public SingleplayerPowerProcessor(SinglePlayerModeController controller)
    {
        this.controller = controller;
    }

    public void ExecutePeekOpponentCard()
    {
        controller.ExecutePeekOpponentCard();
    }

    public void ExecuteBayaBayaBak()
    {
        controller.ExecuteBayaBayaBak();
    }

    public void ExecuteSwapCardWithOpponent()
    {
        controller.ExecuteSwapCardWithOpponent();
    }

    public void ExecuteValeArar()
    {
        controller.ExecuteValeArar();
    }

    public void ExecuteBomba()
    {
        controller.ExecuteBomba();
    }

    public void ExecuteYapamazsın()
    {
        controller.ExecuteYapamazsın();
    }

    public void StartKapkacSelection()
    {
        controller.StartKapkacSelection();
    }

    public void ExecuteKapkacOnCard(string cardId)
    {
        controller.ExecuteKapkacOnCard(cardId);
    }

    public void StartYandimAnamSelection()
    {
        controller.StartYandimAnamSelection();
    }

    public void ExecuteYandimAnamOnCard(string cardId)
    {
        controller.ExecuteYandimAnamOnCard(cardId);
    }

    public void ExecuteBlockNextPlayer()
    {
        controller.ExecuteBlockNextPlayer();
    }

    public void StartKopyalaYapistirSelection()
    {
        controller.StartKopyalaYapistirSelection();
    }

    public void ExecuteKopyalaYapistir(string targetId, string sourceId)
    {
        controller.ExecuteKopyalaYapistir(targetId, sourceId);
    }

    public void ExecuteVerZehri()
    {
        controller.ExecuteVerZehri();
    }

    public void ExecuteKutsalDeste()
    {
        controller.ExecuteKutsalDeste();
    }

    public void StartBuDahaIyiSelection()
    {
        controller.StartBuDahaIyiSelection();
    }

    public void ExecuteBuDahaIyi(string handCardId, string topCenterCardId)
    {
        controller.ExecuteBuDahaIyi(handCardId, topCenterCardId);
    }

    public void StartSunuDegisTokusSelection()
    {
        controller.StartSunuDegisTokusSelection();
    }

    public void ExecuteSunuDegisTokus(string myCardId, string oppCardId)
    {
        controller.ExecuteSunuDegisTokus(myCardId, oppCardId);
    }

    public void StartSunuDegisBunuTokusSelection()
    {
        controller.StartSunuDegisBunuTokusSelection();
    }

    public void ExecuteSunuDegisBunuTokus(string[] myCards, string[] oppCards)
    {
        controller.ExecuteSunuDegisBunuTokus(myCards, oppCards);
    }

    public void ExecuteZaferPuani(int points)
    {
        controller.ExecuteZaferPuani(points);
    }

    public void OnGameStateMutated()
    {
        // Save run progress to disk as an auto-checkpoint after each power effect.
        controller.SaveRunState();
    }
}
