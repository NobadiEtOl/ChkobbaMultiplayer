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
        Debug.Log("DealCardPrefabsToCenterClientRPC called");
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
        Debug.Log($"[NetworkRelay] UseBayaBayaBakClientRPC received - Opponent: {opponentPlayerNo}, Time: {Time.time}");
        
        // Track the RPC call
        DebugChainPrinter.LocalInstance?.TrackNetworkRPC("UseBayaBayaBakClientRPC", $"opponentPlayerNo={opponentPlayerNo}");
        DebugChainPrinter.LocalInstance?.TrackLocalAction($"UseBayaBayaBakClientRPC received for opponent {opponentPlayerNo}");
        
        GameManager.LocalInstance.OnBayaBayaBakSynced(opponentPlayerNo);
        Debug.Log($"[NetworkRelay] UseBayaBayaBakClientRPC complete");
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
        server.AnotherPlayerConnected(clientId);
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
        server.InitialDealCoroutineCheck();
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


}
