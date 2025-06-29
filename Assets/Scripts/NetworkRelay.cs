using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkRelay : NetworkBehaviour
{
    [SerializeField] private Server server;
    public static NetworkRelay Instance { get; private set; }

    private NetworkManagerUI networkManagerUI;
    // Start is called before the first frame update
    void Awake()
    {
        Instance = this;
        networkManagerUI = FindObjectOfType<NetworkManagerUI>();
    }

    //ClientRpc
    [ClientRpc(RequireOwnership = false)]
    public void UpdateCurrentPlayerClientRPC(int currentPlayer, int turnCounter)
    {
        if (GameManager.LocalInstance != null)
        {
            GameManager.LocalInstance.UpdateCurrentPlayer(currentPlayer, turnCounter);
        }
        else
        {
            Debug.LogError("GameManagers list is null or empty!");
        }
    }
    [ClientRpc(RequireOwnership = false)]
    public void UpdateCenterCardIDListClientRPC(SerializableCard serializableCard)
    {
        if (GameManager.LocalInstance != null)
        {
            GameManager.LocalInstance.UpdateCenterCardIDList(serializableCard);
        }
        else
        {
            Debug.LogError("GameManagers list is null or empty!");
        }
    }
    [ClientRpc(RequireOwnership = false)]
    public void InitializeCardPrefabsClientRPC()
    {
        Debug.Log("InitializeCardPrefabsClientRPC called");
        StartCoroutine(GameManager.LocalInstance.InitializeCardPrefabs());
    }

    [ClientRpc(RequireOwnership = false)]
    public void DealCardPrefabsToPlayersClientRPC(int playerCount, SerializableDictionary serializableDictionary)
    {
        GameManager.LocalInstance.CardPrefabsToPlayers(playerCount, serializableDictionary);
    }

    [ClientRpc(RequireOwnership = false)]
    public void DealCardPrefabsToCenterClientRPC(SerializableCard serializableCard)
    {
        GameManager.LocalInstance.CardPrefabsToCenter(serializableCard);
    }

    [ClientRpc(RequireOwnership = false)]
    public void SendMoveToClientRPC(string selectedHandCard, SerializableCard selectedCenterCards, int playerNumber)
    {
        GameManager.LocalInstance.GetCardThatCaptured(selectedHandCard, selectedCenterCards, playerNumber);
    }

    [ClientRpc(RequireOwnership = false)]
    public void SendCardAddedToCenterClientRPC(string uniqueID, int[] cardID)
    {
        GameManager.LocalInstance.GetCardAddedToCenter(uniqueID, cardID);
    }

    [ClientRpc(RequireOwnership = false)]
    public void PrintPlayerPoolsClientRPC(SerializableDictionary serializableDictionary, int chkobbaPlayer)
    {
        GameManager.LocalInstance.PrintPlayerPools(serializableDictionary, chkobbaPlayer);
    }

    [ClientRpc(RequireOwnership = false)]
    public void ShowWinScreenClientRPC(string message, int winnerSide, int point0, int point1)
    {
        GameManager.LocalInstance.ShowWinScreen(message, winnerSide, point0, point1);
    }

    [ClientRpc(RequireOwnership = false)]
    public void GetPlayerNumberClientRPC(int playerNumber)
    {
        if (DeckController.LocalInstance.thisPlayerNumber == -1)
        {
            DeckController.LocalInstance.thisPlayerNumber = playerNumber;
        }
    }

    [ClientRpc(RequireOwnership = false)]
    public void AddRemainingCardsToPoolClientRPC(int playerNumber)
    {
        DeckController.LocalInstance.AddCardsToPlayerPool(playerNumber);
    }

    [ClientRpc(RequireOwnership = false)]
    public void SkipTurnClientRPC()
    {
        GameManager.LocalInstance.SkipTurn();
    }

    [ClientRpc(RequireOwnership = false)]
    public void GivePlayerCountClientRPC(int playerCount)
    {
        DeckController.LocalInstance.GetPlayerCount(playerCount);
    }
    [ClientRpc(RequireOwnership = false)]
    public void GetPlayerNumberClientRPC(ulong clientID, int playerNumber)
    {
        if (NetworkManager.Singleton.LocalClientId == clientID && !IsHost)
        {
            GameManager.LocalInstance.GetPlayerNumber(playerNumber);
        }
    }
    [ClientRpc(RequireOwnership = false)]
    public void AssignCardsToPlayerPoolsClientRPC(SerializableDictionary serializableDictionary)
    {
        DeckController.LocalInstance.AssignCardsToPlayerPools(serializableDictionary);
    }
    

    [ClientRpc]
    public void UsePeekOpponentCardPowerClientRPC(int opponentPlayerNo, int cardIndex)
    {
        GameManager.LocalInstance.OnPeekOpponentCardSynced(opponentPlayerNo, cardIndex);
    }
    
    [ClientRpc]
    public void UseBayaBayaBakClientRPC(int opponentPlayerNo)
    {
        GameManager.LocalInstance.OnBayaBayaBakSynced(opponentPlayerNo);
    }

    [ClientRpc]
    public void BombaClientRPC()
    {
        GameManager.LocalInstance.OnBombaCenter();
    }

    [ClientRpc]
    public void SetYapamazsınActiveClientRPC(bool isActive)
    {
        GameManager.LocalInstance.SetYapamazsınActive(isActive);
    }

    /*[ClientRpc]
    public void SetKapkacActiveClientRPC(bool isActive)
    {
        GameManager.LocalInstance.SetKapkacActive(isActive);
    }*/

    [ClientRpc]
    public void SetOynayamazsinActiveClientRPC(bool isActive)
    {
        GameManager.LocalInstance.SetOynayamazsinActive(isActive);
    }

    [ClientRpc]
    public void SetVerZehriActiveClientRPC(bool isActive)
    {
        GameManager.LocalInstance.SetVerZehriActive(isActive);
    }

    [ClientRpc]
    public void SetKutsalDesteActiveClientRPC(bool isActive)
    {
        GameManager.LocalInstance.SetKutsalDesteActive(isActive);
    }

    [ClientRpc]
    public void ShowVerZehriEffectClientRPC(int playerNumber, int points)
    {
        GameManager.LocalInstance.ShowVerZehriEffect(playerNumber, points);
    }

    [ClientRpc]
    public void ShowKutsalDesteEffectClientRPC(int playerNumber, int points)
    {
        GameManager.LocalInstance.ShowKutsalDesteEffect(playerNumber, points);
    }

    [ClientRpc]
    public void UseBuDahaIyiClientRPC(int playerNo, string handCardID, string centerCardID)
    {
        GameManager.LocalInstance.OnBuDahaIyiSynced(playerNo, handCardID, centerCardID);
    }

    [ClientRpc]
    public void UseSunuDegisTokusClientRPC(int myPlayerNo, int otherPlayerNo, string myHandCardID, string otherHandCardID)
    {
        GameManager.LocalInstance.OnSunuDegisTokusSynced(myPlayerNo, otherPlayerNo, myHandCardID, otherHandCardID);
    }

    [ClientRpc]
    public void UseSunuDegisBunuTokusClientRPC(int myPlayerNo, int otherPlayerNo, string myHandCardID, string otherHandCardID, int myHandIndex)
    {
        GameManager.LocalInstance.OnSunuDegisBunuTokusSynced(myPlayerNo, otherPlayerNo, myHandCardID, otherHandCardID, myHandIndex);
    }
    [ClientRpc(RequireOwnership = false)]
    public void KapkacCardChangedClientRPC(string cardUniqueID)
    {
        GameManager.LocalInstance.OnKapkacCardChanged(cardUniqueID);
    }
    [ClientRpc(RequireOwnership = false)]
    public void KopyalaYapistirClientRPC(string targetUniqueID, string sourceUniqueID)
    {
        GameManager.LocalInstance.OnKopyalaYapistir(targetUniqueID, sourceUniqueID);
    }
    [ClientRpc(RequireOwnership = false)]
    public void ShowcaseSuperPowerClientRPC(string powerName, float fadeDuration = 0.5f, float displayDuration = 2f)
    {
        GameManager.LocalInstance.ShowcaseSuperPower(powerName, fadeDuration, displayDuration);
    }
    [ClientRpc(RequireOwnership = false)]
    public void ShowPistiTextClientRPC(int playerNo, bool isJack)
    {
        string msg = isJack ? $"Player {playerNo + 1} made a Jack PISTI!" : $"Player {playerNo + 1} made a Pişti!";
        GameManager.LocalInstance.ShowPistiText(msg);
    }





    //ServerRPC
    [ServerRpc(RequireOwnership = false)]
    public void ShowcaseSuperPowerServerRPC(string powerName, float fadeDuration = 0.5f, float displayDuration = 2f)
    {
        ShowcaseSuperPowerClientRPC(powerName, fadeDuration, displayDuration);
    }

    [ServerRpc(RequireOwnership = false)]
    public void KopyalaYapistirServerRPC(string targetUniqueID, string sourceUniqueID)
    {
        // Update the server's authoritative card data
        if (Server.Singleton != null && Server.Singleton.allCardLookup.ContainsKey(sourceUniqueID) && Server.Singleton.allCardLookup.ContainsKey(targetUniqueID))
        {
            var sourceID = Server.Singleton.allCardLookup[sourceUniqueID];
            Server.Singleton.allCardLookup[targetUniqueID][0] = sourceID[0]; // kind
            Server.Singleton.allCardLookup[targetUniqueID][1] = sourceID[1]; // value
        }
        // Notify all clients to update visuals and local cardID
        KopyalaYapistirClientRPC(targetUniqueID, sourceUniqueID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ActivateKapkacOnCardServerRPC(string cardUniqueID)
    {
        // Update the server's authoritative card data
        if (Server.Singleton != null && Server.Singleton.allCardLookup.ContainsKey(cardUniqueID))
        {
            Server.Singleton.allCardLookup[cardUniqueID][1] = 11; // Set value to 11 (Jack)
        }

        // Optionally, also update CardInteraction on the server (for host visuals)
        if (CardInteraction.cardLookup.ContainsKey(cardUniqueID))
        {
            CardInteraction.cardLookup[cardUniqueID].SetCardValue(11);
        }

        KapkacCardChangedClientRPC(cardUniqueID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReportZaferPuaniServerRPC(int playerNo, int points)
    {
        server.AddZaferPuaniPoints(playerNo, points);
    }

    [ServerRpc(RequireOwnership = false)]
    public void UseSunuDegisTokusServerRPC(int myPlayerNo, string myHandCardID, string otherHandCardID)
    {
        // Find the owner of the otherHandCardID
        int otherPlayerNo = server.FindOwnerOfCard(otherHandCardID);
        if (otherPlayerNo == -1 || otherPlayerNo == myPlayerNo) return;

        server.SunuDegisTokusSwap(myPlayerNo, otherPlayerNo, myHandCardID, otherHandCardID);
        UseSunuDegisTokusClientRPC(myPlayerNo, otherPlayerNo, myHandCardID, otherHandCardID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void UseBuDahaIyiServerRPC(int playerNo, string handCardID, string centerCardID)
    {
        if (!server.TryBlockPower())
        {
            server.BuDahaIyiSwap(playerNo, handCardID, centerCardID);
            UseBuDahaIyiClientRPC(playerNo, handCardID, centerCardID);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ActivateVerZehriServerRPC()
    {
        server.ActivateVerZehri();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ActivateKutsalDesteServerRPC()
    {
        server.ActivateKutsalDeste();
    }

    /*[ServerRpc(RequireOwnership = false)]
    public void ActivateKapkacServerRPC()
    {
        server.ActivateKapkac();
    }*/

    [ServerRpc(RequireOwnership = false)]
    public void ActivateOynayamazsinServerRPC()
    {
        server.ActivateOynayamazsin();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ActivateYapamazsınServerRPC()
    {
        if (!server.TryBlockPower())server.ActivateYapamazsın();
    }

    [ServerRpc(RequireOwnership = false)]
    public void BombaServerRPC()
    {
        if (!server.TryBlockPower())server.BombaCenter();
    }

    [ServerRpc(RequireOwnership = false)]
    public void UseBayaBayaBakServerRPC(int opponentPlayerNo)
    {
        // Get the hand from the server
        if (!server.TryBlockPower())UseBayaBayaBakClientRPC(opponentPlayerNo);
    }

    [ServerRpc(RequireOwnership = false)]
    public void UsePeekOpponentCardPowerServerRPC(int opponentPlayerNo, int cardIndex)
    {
        if (!server.TryBlockPower())UsePeekOpponentCardPowerClientRPC(opponentPlayerNo, cardIndex);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RegisterCardCopyServerRPC(string targetUniqueID, string sourceUniqueID)
    {
        if (!server.TryBlockPower())server.RegisterCopiedCard(targetUniqueID, sourceUniqueID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RemoveCenterCardsServerRPC(SerializableCard serializableCard)
    {

        server.RemoveCardsFromCenter(serializableCard);

    }
    [ServerRpc(RequireOwnership = false)]
    public void PrintMessageServerRPC(string message)
    {
        Debug.Log("PrintMessageServerRPC called with message: " + message);
        server.PrintMessage(message);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendMoveToServerRPC(string selectedHandCard, SerializableCard serializableCard, int playerNumber, int sumValue)
    {
        server.GetMove(selectedHandCard, serializableCard, playerNumber, sumValue);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddCenterCardServerRPC(string uniqueCardID, int[] cardID)
    {
        server.AddCardIDToCenter(uniqueCardID, cardID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AskPlayerNumberServerRPC()
    {
        GetPlayerNumberClientRPC(server.SendPlayerNumber());
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyCientConnectedServerRPC(ulong clientId)
    {
        server.AnotherPlayerConnected(clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyTurnIsReadyToEndServerRPC()
    {
        server.EndTurnCheck();
    }

    [ServerRpc(RequireOwnership = false)]
    public void DeckReadyServerRPC()
    {
        server.InitialDealCoroutineCheck();
    }

    [ServerRpc(RequireOwnership = false)]
    public void UseSunuDegisBunuTokusServerRPC(int myPlayerNo, string myHandCardID, string otherHandCardID, int myHandIndex)
    {
        // Find the owner of the otherHandCardID
        int otherPlayerNo = server.FindOwnerOfCard(otherHandCardID);
        if (otherPlayerNo == -1 || otherPlayerNo == myPlayerNo) return;

        server.SunuDegisBunuTokusSwap(myPlayerNo, otherPlayerNo, myHandCardID, otherHandCardID, myHandIndex);
        UseSunuDegisBunuTokusClientRPC(myPlayerNo, otherPlayerNo, myHandCardID, otherHandCardID, myHandIndex);
    }

}
