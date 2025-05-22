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
    public void UpdateCurrentPlayerClientRPC(int currentPlayer)
    {
        if (GameManager.LocalInstance != null)
        {
            GameManager.LocalInstance.UpdateCurrentPlayer(currentPlayer);
        }
        else
        {
            Debug.LogError("GameManagers list is null or empty!");
        }
    }
    [ClientRpc(RequireOwnership = false)]
    public void UpdateCenterCardIDListClientRPC(SerializableList serializableList)
    {
        if (GameManager.LocalInstance != null)
        {
            GameManager.LocalInstance.UpdateCenterCardIDList(serializableList);
        }
        else
        {
            Debug.LogError("GameManagers list is null or empty!");
        }
    }
    [ClientRpc(RequireOwnership = true)]
    public void InitializeCardPrefabsClientRPC()
    {
        StartCoroutine(GameManager.LocalInstance.InitializeCardPrefabs());
    }

    [ClientRpc(RequireOwnership = false)]
    public void DealCardPrefabsToPlayersClientRPC(int playerCount, SerializableDictionary serializableDictionary)
    {
        GameManager.LocalInstance.CardPrefabsToPlayers(playerCount, serializableDictionary);
    }

    [ClientRpc(RequireOwnership = false)]
    public void DealCardPrefabsToCenterClientRPC(SerializableList serializableList)
    {
        GameManager.LocalInstance.CardPrefabsToCenter(serializableList);
    }

    [ClientRpc(RequireOwnership = false)]
    public void SendMoveToClientRPC(int[] selectedHandCard, SerializableList selectedCenterCards, int playerNumber)
    {
        GameManager.LocalInstance.GetCardThatCaptured(selectedHandCard, selectedCenterCards, playerNumber);
    }

    [ClientRpc(RequireOwnership = false)]
    public void SendCardAddedToCenterClientRPC(int[] cardID)
    {
        GameManager.LocalInstance.GetCardAddedToCenter(cardID);
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

    //ServerRPC
    [ServerRpc(RequireOwnership = false)]
    public void RemoveCenterCardsServerRPC(SerializableList serializableList)
    {

        server.RemoveCardsFromCenter(serializableList);

    }
    [ServerRpc(RequireOwnership = false)]
    public void PrintMessageServerRPC(string message)
    {
        Debug.Log("PrintMessageServerRPC called with message: " + message);
        server.PrintMessage(message);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendMoveToServerRPC(int[] selectedHandCard, SerializableList serializableList, int playerNumber, int sumValue)
    {
        server.GetMove(selectedHandCard, serializableList, playerNumber, sumValue);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddCenterCardServerRPC(int[] cardID)
    {
        server.AddCardIDToCenter(cardID);
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

}
