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
    public void InitializeCardPrefabsClientRPC(bool isReconnection = false)
    {
        // Debug.Log($"[NetworkRelay] ===== ÖNEMLİ: INITIALIZE CARD PREFABS CLIENT RPC RECEIVED =====\n" +
        //          $"IsReconnection: {isReconnection}\n" +
        //          $"GameManager.LocalInstance: {(GameManager.LocalInstance != null ? "FOUND" : "NULL")}\n" +
        //          $"Calling GameManager.InitializeCardPrefabs({isReconnection})");
        
        StartCoroutine(GameManager.LocalInstance.InitializeCardPrefabs(isReconnection));
        
        // Debug.Log($"[NetworkRelay] ===== ÖNEMLİ: INITIALIZE CARD PREFABS CLIENT RPC COMPLETED =====");
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void InitializeCardPrefabsForReconnectedClientClientRPC(bool isReconnection, ulong targetClientId)
    {
        // Only process if this is the target client
        if (NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            Debug.LogError($"[RECONNECTION] InitializeCardPrefabsForReconnectedClientClientRPC received by target client {targetClientId}");
            StartCoroutine(GameManager.LocalInstance.InitializeCardPrefabs(isReconnection));
        }
        else
        {
            Debug.LogError($"[RECONNECTION] InitializeCardPrefabsForReconnectedClientClientRPC received by non-target client {NetworkManager.Singleton.LocalClientId}, target was {targetClientId}");
        }
    }

    [ClientRpc(RequireOwnership = false)]
    public void DealCardPrefabsToPlayersClientRPC(int playerCount, SerializableDictionary serializableDictionary)
    {
        Debug.LogError($"[DEALING RPC] DealCardPrefabsToPlayersClientRPC received - playerCount: {playerCount}, hands: {serializableDictionary.Count}");
        GameManager.LocalInstance.CardPrefabsToPlayers(playerCount, serializableDictionary);
    }

    [ClientRpc(RequireOwnership = false)]
    public void DealCardPrefabsToCenterClientRPC(SerializableCard serializableCard)
    {
        // Debug.Log("DealCardPrefabsToCenterClientRPC called");
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
        // Debug.Log($"[NetworkRelay] ===== ÖNEMLİ: GIVE PLAYER COUNT CLIENT RPC RECEIVED =====\n" +
        //          $"PlayerCount: {playerCount}\n" +
        //          $"DeckController.LocalInstance: {(DeckController.LocalInstance != null ? "FOUND" : "NULL")}\n" +
        //          $"Calling DeckController.GetPlayerCount({playerCount})");
        
        DeckController.LocalInstance.GetPlayerCount(playerCount, false); // false = not reconnection
        
        // Debug.Log($"[NetworkRelay] ===== ÖNEMLİ: GIVE PLAYER COUNT CLIENT RPC COMPLETED =====");
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void GivePlayerCountForReconnectedClientClientRPC(int playerCount, ulong targetClientId)
    {
        // Only process if this is the target client
        if (NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            Debug.LogError($"[RECONNECTION] GivePlayerCountForReconnectedClientClientRPC received by target client {targetClientId}");
            DeckController.LocalInstance.GetPlayerCount(playerCount, true); // true = isReconnection
        }
        else
        {
            Debug.LogError($"[RECONNECTION] GivePlayerCountForReconnectedClientClientRPC received by non-target client {NetworkManager.Singleton.LocalClientId}, target was {targetClientId}");
        }
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
        // Debug.Log($"[NetworkRelay] UseBayaBayaBakClientRPC received - Opponent: {opponentPlayerNo}, Time: {Time.time}");
        
        // Track the RPC call
        DebugChainPrinter.LocalInstance?.TrackNetworkRPC("UseBayaBayaBakClientRPC", $"opponentPlayerNo={opponentPlayerNo}");
        DebugChainPrinter.LocalInstance?.TrackLocalAction($"UseBayaBayaBakClientRPC received for opponent {opponentPlayerNo}");
        
        GameManager.LocalInstance.OnBayaBayaBakSynced(opponentPlayerNo);
        // Debug.Log($"[NetworkRelay] UseBayaBayaBakClientRPC complete");
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
        Debug.Log($"[NetworkRelay] SetOynayamazsinActiveClientRPC received - isActive: {isActive}, Time: {Time.time}");
        GameManager.LocalInstance.SetOynayamazsinActive(isActive);
        Debug.Log($"[NetworkRelay] SetOynayamazsinActiveClientRPC complete");
    }

    [ClientRpc]
    public void SetVerZehriActiveClientRPC(bool isActive)
    {
        Debug.Log($"[NetworkRelay] SetVerZehriActiveClientRPC received - isActive: {isActive}, Time: {Time.time}");
        GameManager.LocalInstance.SetVerZehriActive(isActive);
        Debug.Log($"[NetworkRelay] SetVerZehriActiveClientRPC complete");
    }

    [ClientRpc]
    public void SetKutsalDesteActiveClientRPC(bool isActive)
    {
        Debug.Log($"[NetworkRelay] SetKutsalDesteActiveClientRPC received - isActive: {isActive}, Time: {Time.time}");
        GameManager.LocalInstance.SetKutsalDesteActive(isActive);
        Debug.Log($"[NetworkRelay] SetKutsalDesteActiveClientRPC complete");
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
        Debug.Log($"[NetworkRelay] UseBuDahaIyiClientRPC received - Player: {playerNo}, HandCard: {handCardID}, CenterCard: {centerCardID}, Time: {Time.time}");
        GameManager.LocalInstance.OnBuDahaIyiSynced(playerNo, handCardID, centerCardID);
        Debug.Log($"[NetworkRelay] UseBuDahaIyiClientRPC complete");
    }

    [ClientRpc]
    public void UseSunuDegisTokusClientRPC(int myPlayerNo, int otherPlayerNo, string myHandCardID, string otherHandCardID)
    {
        StartCoroutine(GameManager.LocalInstance.OnSunuDegisTokusSynced(myPlayerNo, otherPlayerNo, myHandCardID, otherHandCardID));
    }

    [ClientRpc]
    public void UseSunuDegisBunuTokusClientRPC(int myPlayerNo, int otherPlayerNo, string myHandCardID, string otherHandCardID, int myHandIndex, bool readyToExit = false)
    {
        StartCoroutine(GameManager.LocalInstance.OnSunuDegisBunuTokusSynced(myPlayerNo, otherPlayerNo, myHandCardID, otherHandCardID, myHandIndex, readyToExit));
    }
    [ClientRpc(RequireOwnership = false)]
    public void KapkacCardChangedClientRPC(string cardUniqueID)
    {
        GameManager.LocalInstance.OnKapkacCardChanged(cardUniqueID);
    }
    [ClientRpc(RequireOwnership = false)]
    public void KopyalaYapistirClientRPC(string targetUniqueID, string sourceUniqueID)
    {
        Debug.Log($"[NetworkRelay] KopyalaYapistirClientRPC received - Target: {targetUniqueID}, Source: {sourceUniqueID}, Time: {Time.time}");
        GameManager.LocalInstance.OnKopyalaYapistir(targetUniqueID, sourceUniqueID);
        Debug.Log($"[NetworkRelay] KopyalaYapistirClientRPC complete");
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
    [ClientRpc(RequireOwnership = false)]
    public void YandimAnamCardChangedClientRPC(string cardUniqueID)
    {
        GameManager.LocalInstance.OnYandimAnamCardChanged(cardUniqueID);
    }

    [ClientRpc(RequireOwnership = false)]
    public void ReceiveGoldFromTeammateClientRPC(int goldAmount)
    {
        Debug.Log($"[NetworkRelay] Received {goldAmount} gold from teammate");
        if (SuperPowerSpawner.LocalInstance != null)
        {
            SuperPowerSpawner.LocalInstance.ReceiveGoldFromTeammate(goldAmount);
        }
    } 


    //ServerRPC
    [ServerRpc(RequireOwnership = false)]
    public void NotifyDealCenterFinishedServerRPC(ulong clientId)
    {
        server.OnClientDealCenterFinished(clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ActivateYandimAnamOnCardServerRPC(string cardUniqueID)
    {
        // Update the server's authoritative card data
        if (Server.Singleton != null && Server.Singleton.allCardLookup.ContainsKey(cardUniqueID))
        {
            Server.Singleton.allCardLookup[cardUniqueID][1] = 0; // Set value to 0
        }
        YandimAnamCardChangedClientRPC(cardUniqueID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ShowcaseSuperPowerServerRPC(string powerName, float fadeDuration = 0.5f, float displayDuration = 2f)
    {
        ShowcaseSuperPowerClientRPC(powerName, fadeDuration, displayDuration);
    }

    [ServerRpc(RequireOwnership = false)]
    public void KopyalaYapistirServerRPC(string targetUniqueID, string sourceUniqueID)
    {
        Debug.Log($"[NetworkRelay] KopyalaYapistirServerRPC called - Target: {targetUniqueID}, Source: {sourceUniqueID}, Time: {Time.time}");
        
        // Update the server's authoritative card data
        if (Server.Singleton != null && Server.Singleton.allCardLookup.ContainsKey(sourceUniqueID) && Server.Singleton.allCardLookup.ContainsKey(targetUniqueID))
        {
            var sourceID = Server.Singleton.allCardLookup[sourceUniqueID];
            Debug.Log($"[NetworkRelay] Updating server card data - Target card [{targetUniqueID}] will become [{sourceID[0]}, {sourceID[1]}]");
            Server.Singleton.allCardLookup[targetUniqueID][0] = sourceID[0]; // kind
            Server.Singleton.allCardLookup[targetUniqueID][1] = sourceID[1]; // value
        }
        else
        {
            Debug.LogWarning($"[NetworkRelay] KopyalaYapistirServerRPC - Card lookup failed - Source exists: {Server.Singleton?.allCardLookup.ContainsKey(sourceUniqueID)}, Target exists: {Server.Singleton?.allCardLookup.ContainsKey(targetUniqueID)}");
        }
        
        // Notify all clients to update visuals and local cardID
        Debug.Log($"[NetworkRelay] Broadcasting KopyalaYapistirClientRPC to all clients");
        KopyalaYapistirClientRPC(targetUniqueID, sourceUniqueID);
        Debug.Log($"[NetworkRelay] KopyalaYapistirServerRPC complete");
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
        Debug.Log($"[NetworkRelay] UseBuDahaIyiServerRPC called - Player: {playerNo}, HandCard: {handCardID}, CenterCard: {centerCardID}, Time: {Time.time}");
        
        if (!server.TryBlockPower())
        {
            Debug.Log($"[NetworkRelay] Power not blocked, calling server.BuDahaIyiSwap()");
            server.BuDahaIyiSwap(playerNo, handCardID, centerCardID);
            Debug.Log($"[NetworkRelay] Broadcasting UseBuDahaIyiClientRPC to all clients");
            UseBuDahaIyiClientRPC(playerNo, handCardID, centerCardID);
            Debug.Log($"[NetworkRelay] UseBuDahaIyiServerRPC complete");
        }
        else
        {
            Debug.Log($"[NetworkRelay] UseBuDahaIyiServerRPC blocked by Yapamazsın power");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ActivateVerZehriServerRPC()
    {
        Debug.Log($"[NetworkRelay] ActivateVerZehriServerRPC called - Time: {Time.time}");
        Debug.Log($"[NetworkRelay] Calling server.ActivateVerZehri() - This will set verZehriPending=true");
        server.ActivateVerZehri();
        Debug.Log($"[NetworkRelay] ActivateVerZehriServerRPC complete - Effect will activate at end of turn");
    }

    [ServerRpc(RequireOwnership = false)]
    public void ActivateKutsalDesteServerRPC()
    {
        Debug.Log($"[NetworkRelay] ActivateKutsalDesteServerRPC called - Time: {Time.time}");
        Debug.Log($"[NetworkRelay] Calling server.ActivateKutsalDeste() - This will set kutsalDestePending=true");
        server.ActivateKutsalDeste();
        Debug.Log($"[NetworkRelay] ActivateKutsalDesteServerRPC complete - Effect will activate at end of turn");
    }

    /*[ServerRpc(RequireOwnership = false)]
    public void ActivateKapkacServerRPC()
    {
        server.ActivateKapkac();
    }*/

    [ServerRpc(RequireOwnership = false)]
    public void ActivateOynayamazsinServerRPC()
    {
        Debug.Log($"[NetworkRelay] ActivateOynayamazsinServerRPC called - Time: {Time.time}");
        Debug.Log($"[NetworkRelay] Calling server.ActivateOynayamazsin() - This will set oynayamazsinPending=true");
        server.ActivateOynayamazsin();
        Debug.Log($"[NetworkRelay] ActivateOynayamazsinServerRPC complete - Effect will activate at end of turn");
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
        Debug.Log($"[NetworkRelay] UseBayaBayaBakServerRPC called - Opponent: {opponentPlayerNo}, Time: {Time.time}");
        
        // Track the RPC call
        DebugChainPrinter.LocalInstance?.TrackNetworkRPC("UseBayaBayaBakServerRPC", $"opponentPlayerNo={opponentPlayerNo}");
        
        // Get the hand from the server
        if (!server.TryBlockPower())
        {
            Debug.Log($"[NetworkRelay] Power not blocked, calling UseBayaBayaBakClientRPC()");
            UseBayaBayaBakClientRPC(opponentPlayerNo);
        }
        else
        {
            Debug.Log($"[NetworkRelay] Power was blocked by Yapamazsın");
            DebugChainPrinter.LocalInstance?.TrackLocalAction("BayaBayaBak power was blocked by Yapamazsın");
        }
        
        Debug.Log($"[NetworkRelay] UseBayaBayaBakServerRPC complete");
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
        GameManager.AddToDebugLog($"[NetworkRelay] SendMoveToServerRPC called with selectedHandCard: {selectedHandCard}, playerNumber: {playerNumber}, sumValue: {sumValue}");
        GameManager.AddToDebugLog($"[NetworkRelay] serializableCard contains {serializableCard.ToDictionary().Count} cards");
        server.GetMove(selectedHandCard, serializableCard, playerNumber, sumValue);
        GameManager.AddToDebugLog($"[NetworkRelay] GetMove called on server");
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
        Debug.Log($"[NetworkRelay] ===== ÖNEMLİ: NOTIFY CLIENT CONNECTED SERVER RPC RECEIVED =====\n" +
                 $"ClientId from RPC: {clientId}\n" +
                 $"OwnerClientId: {OwnerClientId}\n" +
                 $"IsServer: {IsServer}\n" +
                 $"Server instance: {(server != null ? "FOUND" : "NULL")}\n" +
                 $"Calling server.AnotherPlayerConnected({clientId})");
        
        server.AnotherPlayerConnected(clientId);
        
        Debug.Log($"[NetworkRelay] ===== ÖNEMLİ: ANOTHER PLAYER CONNECTED CALL COMPLETED =====");
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyTurnIsReadyToEndServerRPC()
    {
        Debug.Log($"[NetworkRelay] NotifyTurnIsReadyToEndServerRPC called by client {OwnerClientId}");
        server.EndTurnCheck();
    }

    [ServerRpc(RequireOwnership = false)]
    public void DeckReadyServerRPC()
    {
        ulong clientId = OwnerClientId; // Use OwnerClientId to get the client that called this RPC
        
        Debug.Log($"[NetworkRelay] ===== ÖNEMLİ: DECK READY SERVER RPC RECEIVED =====\n" +
                 $"ClientId: {clientId}\n" +
                 $"IsServer: {IsServer}\n" +
                 $"Server instance: {(server != null ? "FOUND" : "NULL")}\n" +
                 $"This is for NORMAL GAME FLOW only - reconnection handled by ReconnectingClientCardsReadyServerRPC");
        
        // NORMAL GAME FLOW ONLY - proceed with initial deals
        Debug.Log($"[NetworkRelay] Normal game flow - proceeding with initial deals for client {clientId}");
        server.InitialDealCoroutineCheck();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReconnectingClientCardsReadyServerRPC(ulong clientId)
    {
        Debug.LogError("[Visual Sync] ===== RECONNECTING CLIENT CARDS READY SERVER RPC RECEIVED =====");
        Debug.Log($"[NetworkRelay] ===== ÖNEMLİ: RECONNECTING CLIENT CARDS READY RPC RECEIVED =====\n" +
                 $"ClientId: {clientId}\n" +
                 $"IsServer: {IsServer}\n" +
                 $"Server instance: {(server != null ? "FOUND" : "NULL")}\n" +
                 $"This RPC is specifically for reconnection - no client ID check needed");
        
        // RECONNECTION: Always proceed with reconnection sync (no client ID check)
        // Build current game state and send to reconnected client
        var gameState = server.BuildGameStateSnapshot();
        
        // Single comprehensive log for reconnection sync start
        Debug.Log($"[NetworkRelay] ===== ÖNEMLİ: RECONNECTION SYNC START =====\n" +
                 $"Client {clientId} cards are ready - applying game state for reconnection\n" +
                 $"Server game state: turn={server.turnCounter}, currentPlayer={server.currentPlayer}\n" +
                 $"Built game state snapshot: version={gameState.snapshotVersion}, centerCards={gameState.center.items?.Length ?? 0}, hands={gameState.hands.Count}\n" +
                 $"Sending ApplyGameStateClientRPC to all clients");
        
        // RECONNECTION FIX: Send game state only to the reconnecting client, not all clients
        // This prevents triggering normal game flow on other clients
        ApplyGameStateToReconnectedClientClientRPC(gameState, clientId);
        
        // Update current player info for reconnected client
        server.CallUpdateCurrentPlayer();
        
        // Remove client from reconnecting set (if it exists)
        server.RemoveReconnectingClient(clientId);
        
        // RECONNECTION FIX: Trigger desync check after game state is applied
        // This ensures the reconnecting client's move chain is synchronized
        TriggerDesyncCheckForReconnectedClientClientRPC(clientId);
        
        // RECONNECTION FIX: Do NOT call InitialDealCoroutineCheck() during reconnection
        // The reconnecting client will sync via existing desync detection system
        
        // Single comprehensive log for reconnection sync end
        Debug.Log($"[NetworkRelay] ===== ÖNEMLİ: RECONNECTION SYNC COMPLETED =====\n" +
                 $"Game state applied to reconnected client {clientId}\n" +
                 $"Client removed from reconnecting set\n" +
                 $"Current player updated and sync process finished\n" +
                 $"InitialDealCoroutineCheck() called to ensure normal game flow continues");
    }

    [ServerRpc(RequireOwnership = false)]
    public void UseSunuDegisBunuTokusServerRPC(int myPlayerNo, string myHandCardID, string otherHandCardID, int myHandIndex, bool readyToExit = false)
    {
        // Find the owner of the otherHandCardID
        int otherPlayerNo = server.FindOwnerOfCard(otherHandCardID);
        if (otherPlayerNo == -1 || otherPlayerNo == myPlayerNo) return;

        server.SunuDegisBunuTokusSwap(myPlayerNo, otherPlayerNo, myHandCardID, otherHandCardID, myHandIndex);
        UseSunuDegisBunuTokusClientRPC(myPlayerNo, otherPlayerNo, myHandCardID, otherHandCardID, myHandIndex, readyToExit);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ShareGoldWithTeammateServerRPC(int teammateNumber, int goldAmount)
    {
        Debug.Log($"[NetworkRelay] Player sharing {goldAmount} gold with teammate {teammateNumber}");
        // Server validation - just debug print for now
        if (server != null)
        {
            // You can add server-side validation here if needed
            Debug.Log($"[NetworkRelay] Server validated gold sharing: {goldAmount} to player {teammateNumber}");
        }
    }

    // ===== GAME STATE SYNC =====

    /// <summary>
    /// Client RPC to apply a complete game state snapshot to all clients
    /// </summary>
    [ClientRpc(RequireOwnership = false)]
    public void ApplyGameStateClientRPC(SerializableGameState snapshot)
    {
        Debug.Log($"[NetworkRelay] Received game state snapshot version {snapshot.snapshotVersion}");
        GameManager.LocalInstance?.ApplyGameState(snapshot);
    }

    [ClientRpc(RequireOwnership = false)]
    public void ApplyGameStateToReconnectedClientClientRPC(SerializableGameState snapshot, ulong targetClientId)
    {
        // Only apply the game state if this is the target client
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            Debug.Log($"[NetworkRelay] Applying game state snapshot v{snapshot.snapshotVersion} to reconnected client {targetClientId}");
            GameManager.LocalInstance?.ApplyGameState(snapshot);
        }
    }

    [ClientRpc(RequireOwnership = false)]
    public void TriggerDesyncCheckForReconnectedClientClientRPC(ulong targetClientId)
    {
        Debug.LogError($"[Visual Sync] ===== TRIGGER DESYNC CHECK FOR RECONNECTED CLIENT RPC CALLED =====");
        Debug.LogError($"[Visual Sync] Target client ID: {targetClientId}, Local client ID: {NetworkManager.Singleton?.LocalClientId}");
        
        // Only trigger desync check if this is the target client
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            Debug.LogError($"[Visual Sync] Client ID matches - proceeding with desync check");
            Debug.Log($"[NetworkRelay] Triggering desync check for reconnected client {targetClientId}");
            
            // Trigger desync check after a short delay to ensure game state is fully applied
            if (GameManager.LocalInstance != null)
            {
                Debug.LogError($"[Visual Sync] GameManager found - calling TriggerDesyncCheckAfterReconnection method");
                GameManager.LocalInstance.TriggerDesyncCheckAfterReconnection();
            }
            else
            {
                Debug.LogError($"[Visual Sync] ERROR: GameManager.LocalInstance is null");
            }
        }
        else
        {
            Debug.LogError($"[Visual Sync] Client ID does not match - skipping desync check");
        }
    }


    // Debug-only: broadcast a snapshot to be logged into clients' sync logs
    [ClientRpc(RequireOwnership = false)]
    public void LogSnapshotClientRPC(SerializableGameState snapshot, string label)
    {
        GameManager.LocalInstance?.LogSnapshotForSyncLogs(snapshot, label);
    }

    // === MOVE CHAIN VALIDATION RPCs ===
    
    [ClientRpc(RequireOwnership = false)]
    public void BroadcastMoveForValidationClientRPC(GameMove move)
    {
        // Clients receive moves from server for validation
        if (MoveChainTracker.ClientInstance != null)
        {
            // TODO: Implement move validation logic
            Debug.Log($"[NetworkRelay] Client received move for validation: {move.moveType} by P{move.playerNumber}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestFullStateSyncServerRPC()
    {
        // Client requests full state sync due to desync
        Debug.Log("[NetworkRelay] Client requested full state sync due to desync");
        
        if (Server.Singleton != null)
        {
            var gameState = Server.Singleton.BuildGameStateSnapshot();
            ApplyGameStateClientRPC(gameState);
        }
    }

    // === HEARTBEAT & DISCONNECT DETECTION ===
    
    [ServerRpc(RequireOwnership = false)]
    public void SendHeartbeatServerRPC()
    {
        // Client sends heartbeat to server
        ulong clientId = OwnerClientId;
        Debug.Log($"[NetworkRelay] Heartbeat received from client {clientId}");
        
        // Update the server's heartbeat tracking
        if (Server.Singleton != null)
        {
            Server.Singleton.OnClientHeartbeat(clientId);
        }
    }

    [ClientRpc(RequireOwnership = false)]
    public void SendHostHeartbeatClientRPC()
    {
        // Host sends heartbeat to all clients for timeout detection
        var networkManagerUI = FindObjectOfType<NetworkManagerUI>();
        if (networkManagerUI != null)
        {
            networkManagerUI.UpdateHostHeartbeat();
        }
    }

    [ClientRpc(RequireOwnership = false)]
    public void OnPlayerDisconnectedClientRPC(ulong clientId, string reason)
    {
        Debug.LogWarning($"[NetworkRelay] Player {clientId} disconnected: {reason}");
        
        // Notify GameManager about the disconnect
        if (GameManager.LocalInstance != null)
        {
            GameManager.LocalInstance.OnPlayerDisconnected(clientId, reason);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyDisconnectServerRPC(string reason)
    {
        // Client notifies server about impending disconnect
        ulong clientId = OwnerClientId;
        Debug.LogWarning($"[NetworkRelay] Client {clientId} notifying disconnect: {reason}");
        
        // Notify all clients about the disconnect
        OnPlayerDisconnectedClientRPC(clientId, reason);
        
        // Update server state
        if (Server.Singleton != null)
        {
            Server.Singleton.OnClientDisconnected(clientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestGameStateSyncForReconnectedClientServerRPC()
    {
        // Reconnected client requests current game state
        ulong clientId = OwnerClientId;
        Debug.Log($"[NetworkRelay] Reconnected client {clientId} requesting game state sync");
        
        if (Server.Singleton != null)
        {
            // Build current game state snapshot
            var gameState = Server.Singleton.BuildGameStateSnapshot();
            Debug.Log($"[NetworkRelay] Sending game state snapshot v{gameState.snapshotVersion} to reconnected client {clientId}");
            
            // Send game state to the requesting client
            ApplyGameStateToReconnectedClientClientRPC(gameState, clientId);
        }
        else
        {
            Debug.LogError("[NetworkRelay] Server.Singleton is null - cannot provide game state");
        }
    }

    // Note: Reconnection now uses existing desync detection system

    // Note: Client sync completion notification removed - using desync detection system

    // ===== MOVE BUFFERING RPCs =====
    
    /// <summary>
    /// Buffers a move during client synchronization
    /// </summary>
    [ClientRpc(RequireOwnership = false)]
    public void BufferMoveForSyncingClientClientRPC(GameMove move, ulong targetClientId)
    {
        // Only buffer if this is the target client and we're in sync mode
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            Debug.Log($"[NetworkRelay] Buffering move for syncing client {targetClientId}: {move.moveType} by P{move.playerNumber}");
            
            if (GameManager.LocalInstance != null)
            {
                GameManager.LocalInstance.BufferMoveForSync(move);
            }
        }
    }
    
    /// <summary>
    /// Applies all buffered moves after sync is complete
    /// </summary>
    [ClientRpc(RequireOwnership = false)]
    public void ApplyBufferedMovesClientRPC(GameMove[] bufferedMoves, ulong targetClientId)
    {
        // Only apply if this is the target client
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            Debug.Log($"[NetworkRelay] Applying {bufferedMoves.Length} buffered moves for client {targetClientId}");
            
            if (GameManager.LocalInstance != null)
            {
                GameManager.LocalInstance.ApplyBufferedMoves(bufferedMoves);
            }
        }
    }

    [ClientRpc(RequireOwnership = false)]
    public void NotifyHostDisconnectedClientRPC()
    {
        Debug.Log("[NetworkRelay] Host disconnected - notifying client to return to main page");
        
        // Notify NetworkManagerUI to handle host disconnection
        var networkManagerUI = FindObjectOfType<NetworkManagerUI>();
        if (networkManagerUI != null)
        {
            networkManagerUI.OnHostDisconnected();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyClientsToDisconnectServerRPC()
    {
        Debug.Log("[NetworkRelay] Host requesting all clients to disconnect");
        
        // Send RPC to all clients to disconnect
        NotifyClientsToDisconnectClientRPC();
    }

    [ClientRpc(RequireOwnership = false)]
    public void NotifyClientsToDisconnectClientRPC()
    {
        Debug.Log("[NetworkRelay] Host requested client to disconnect - performing disconnection");
        
        // Notify NetworkManagerUI to handle client disconnection
        var networkManagerUI = FindObjectOfType<NetworkManagerUI>();
        if (networkManagerUI != null)
        {
            networkManagerUI.OnClientRequestedToDisconnect();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ConfirmClientDisconnectionServerRPC(ulong clientId)
    {
        Debug.Log($"[NetworkRelay] Client {clientId} confirmed disconnection");
        
        // Notify server that this client has confirmed disconnection
        if (server != null)
        {
            server.OnClientConfirmedDisconnection(clientId);
        }
        else
        {
            Debug.LogError("[NetworkRelay] Server reference is null - cannot confirm client disconnection");
        }
    }


}
