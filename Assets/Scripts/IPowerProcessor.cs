using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPowerProcessor
{
    void ExecutePeekOpponentCard();
    void ExecuteBayaBayaBak();
    void ExecuteSwapCardWithOpponent();
    void ExecuteValeArar();
    void ExecuteBomba();
    void ExecuteYapamazsın();
    void StartKapkacSelection();
    void ExecuteKapkacOnCard(string cardId);
    void StartYandimAnamSelection();
    void ExecuteYandimAnamOnCard(string cardId);
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
    void ExecuteSunuDegisBunuTokus(string[] myCards, string[] oppCards);
    void ExecuteZaferPuani(int points);
}
