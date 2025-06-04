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

    [ClientRpc]
    public void UsePeekOpponentCardPowerClientRPC(int opponentPlayerNo, int cardIndex)
    {
        GameManager.LocalInstance.OnPeekOpponentCardSynced(opponentPlayerNo, cardIndex);
    }

    [ClientRpc]
    public void UseSwapCardWithOpponentPowerClientRPC(int myPlayerNo, int myCardIndex, int opponentPlayerNo, int oppCardIndex)
    {
        GameManager.LocalInstance.OnSwapCardWithOpponentSynced(myPlayerNo, myCardIndex, opponentPlayerNo, oppCardIndex);
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

    [ClientRpc]
    public void SetKapkacActiveClientRPC(bool isActive)
    {
        GameManager.LocalInstance.SetKapkacActive(isActive);
    }

    [ClientRpc]
    public void SetOynayamazsinActiveClientRPC(bool isActive)
    {
        GameManager.LocalInstance.SetOynayamazsinActive(isActive);
    }

    //ServerRPC
    [ServerRpc(RequireOwnership = false)]
    public void ActivateKapkacServerRPC()
    {
        server.ActivateKapkac();
    }

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
    public void UseSwapCardWithOpponentPowerServerRPC(int myPlayerNo, int myCardIndex, int opponentPlayerNo, int oppCardIndex)
    {
        if (!server.TryBlockPower())
        {
            server.SwapCardsBetweenPlayersOnServer(myPlayerNo, myCardIndex, opponentPlayerNo, oppCardIndex);

            UseSwapCardWithOpponentPowerClientRPC(myPlayerNo, myCardIndex, opponentPlayerNo, oppCardIndex);
        }
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

}
