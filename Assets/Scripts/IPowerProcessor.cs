using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPowerProcessor
{
    void ExecutePeekOpponentCard();
    void ExecuteBayaBayaBak();
    void ExecuteSwapCardWithOpponent(string selectedCardId);
    void ExecuteValeArar();
    void ExecuteBomba();
    void ExecuteYapamazsın();
    void StartKapkacSelection();
    void ExecuteKapkacOnCard(string cardId);
    void StartYandimAnamSelection();
    void ExecuteYandimAnamOnCard(string cardId);
    void StartDegisTokusSelection();
    void ExecuteBlockNextPlayer();
    void StartKopyalaYapistirSelection();
    void ExecuteKopyalaYapistir(string targetId, string sourceId);
    void ExecuteVerZehri();
    void ExecuteKutsalDeste();
    void StartBuDahaIyiSelection();
    void ExecuteBuDahaIyi(string handCardId, string topCenterCardId);
    void StartSunuDegisTokusSelection();
    void ExecuteSunuDegisTokus(string myCardId, string oppCardId);
    void StartSunuDegisBunuTokusSelection();
    void ExecuteZaferPuani(int points);

    /// <summary>
    /// Called after any power effect completes to persist game state.
    /// Multiplayer: saves game state for reconnection sync.
    /// Singleplayer: saves run progress to disk (auto-checkpoint).
    /// </summary>
    void OnGameStateMutated();
}
