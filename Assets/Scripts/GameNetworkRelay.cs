using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameNetworkRelay : NetworkBehaviour
{
    [SerializeField] public Server server;
    public static GameNetworkRelay Instance { get; private set; }

    private NetworkManagerUI networkManagerUI;

    private bool ShouldRejectPowerRpcDuringMove(string rpcName)
    {
        if (server != null && server.IsProcessingMove)
        {
            
            return true;
        }
        return false;
    }

    /// <summary>
    /// Validates that the RPC caller is the current player.
    /// Returns true (valid) and outputs the caller's player number.
    /// Returns false and logs a warning if the caller is not recognised or is not the current player.
    /// </summary>
    private bool ValidatePowerCaller(out int callerPlayerNo, ServerRpcParams rpcParams)
    {
        // SenderClientId is the actual network client that sent this RPC.
        // OwnerClientId would always be the host (NetworkObject owner), NOT the sender.
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        callerPlayerNo = server.GetPlayerNoForClient(senderClientId);
        if (callerPlayerNo == -1 || callerPlayerNo != server.currentPlayer)
        {
            Debug.LogWarning($"[GameNetworkRelay] Power RPC rejected: senderClient={senderClientId} " +
                             $"(callerPlayerNo={callerPlayerNo}, currentPlayer={server.currentPlayer})");
            return false;
        }
        return true;
    }
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
            
        }
    }
    [ClientRpc(RequireOwnership = false)]
    public void InitializeCardPrefabsClientRPC(bool isReconnection = false)
    {
        // Debug.Log($"[GameNetworkRelay] ===== ÖNEMLİ: INITIALIZE CARD PREFABS CLIENT RPC RECEIVED =====\n" +
        //          $"IsReconnection: {isReconnection}\n" +
        //          $"GameManager.LocalInstance: {(GameManager.LocalInstance != null ? "FOUND" : "NULL")}\n" +
        //          $"Calling GameManager.InitializeCardPrefabs({isReconnection})");
        
        StartCoroutine(GameManager.LocalInstance.InitializeCardPrefabs(isReconnection));
        
        // 
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void InitializeCardPrefabsForReconnectedClientClientRPC(bool isReconnection, ulong targetClientId)
    {
        // Only process if this is the target client
        if (NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            
            StartCoroutine(GameManager.LocalInstance.InitializeCardPrefabs(isReconnection));
        }
        else
        {
            
        }
    }

    [ClientRpc(RequireOwnership = false)]
    public void DealCardPrefabsToPlayersClientRPC(int playerCount, SerializableDictionary serializableDictionary)
    {
        
        GameManager.LocalInstance.CardPrefabsToPlayers(playerCount, serializableDictionary);
    }

    [ClientRpc(RequireOwnership = false)]
    public void DealCardPrefabsToCenterClientRPC(SerializableCard serializableCard)
    {
        // 
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
    public void UpdateScoreDisplayClientRPC(int point0, int point1, int p1Self = 0, int p2Self = 0, int p1Opp = 0, int p2Opp = 0)
    {
        if (GameManager.LocalInstance != null)
        {
            GameManager.LocalInstance.ApplyLiveScoreUpdate(point0, point1, p1Self, p2Self, p1Opp, p2Opp);
        }
        else
        {
            
        }
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
        // Debug.Log($"[GameNetworkRelay] ===== ÖNEMLİ: GIVE PLAYER COUNT CLIENT RPC RECEIVED =====\n" +
        //          $"PlayerCount: {playerCount}\n" +
        //          $"DeckController.LocalInstance: {(DeckController.LocalInstance != null ? "FOUND" : "NULL")}\n" +
        //          $"Calling DeckController.GetPlayerCount({playerCount})");
        
        DeckController.LocalInstance.GetPlayerCount(playerCount, false); // false = not reconnection
        
        // 
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void GivePlayerCountForReconnectedClientClientRPC(int playerCount, ulong targetClientId)
    {
        // Only process if this is the target client
        if (NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            
            DeckController.LocalInstance.GetPlayerCount(playerCount, true); // true = isReconnection
        }
        else
        {
            
        }
    }
    [ClientRpc(RequireOwnership = false)]
    public void GetPlayerNumberClientRPC(ulong clientID, int playerNumber)
    {
        // Include IsHost: the original host must also save their seat (0) to PlayerPrefs
        // so that GetSavedPlayerSeat() works correctly on reconnect after host migration.
        if (NetworkManager.Singleton.LocalClientId == clientID)
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
        // 
        
        // Track the RPC call
        DebugChainPrinter.LocalInstance?.TrackNetworkRPC("UseBayaBayaBakClientRPC", $"opponentPlayerNo={opponentPlayerNo}");
        DebugChainPrinter.LocalInstance?.TrackLocalAction($"UseBayaBayaBakClientRPC received for opponent {opponentPlayerNo}");
        
        GameManager.LocalInstance.OnBayaBayaBakSynced(opponentPlayerNo);
        // 
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
    public void UseBuDahaIyiClientRPC(int activatingPlayerNo, int handCardOwnerPlayerNo, string handCardID, string centerCardID)
    {
        
        GameManager.LocalInstance.OnBuDahaIyiSynced(activatingPlayerNo, handCardOwnerPlayerNo, handCardID, centerCardID);
        
    }

    [ClientRpc]
    public void UseSunuDegisTokusClientRPC(int myPlayerNo, int otherPlayerNo, string myHandCardID, string otherHandCardID)
    {
        StartCoroutine(GameManager.LocalInstance.OnSunuDegisTokusSynced(myPlayerNo, otherPlayerNo, myHandCardID, otherHandCardID));
    }

    [ClientRpc]
    public void UseSunuDegisBunuTokusClientRPC(int myPlayerNo, int otherPlayerNo, string myHandCardID, string otherHandCardID, int myHandIndex, bool readyToExit = false)
    {
        // Sequential flow now reuses the same synced handler as Şunu Değiş Tokuş.
        StartCoroutine(GameManager.LocalInstance.OnSunuDegisTokusSynced(myPlayerNo, otherPlayerNo, myHandCardID, otherHandCardID));
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
    [ClientRpc(RequireOwnership = false)]
    public void YandimAnamCardChangedClientRPC(string cardUniqueID)
    {
        GameManager.LocalInstance.OnYandimAnamCardChanged(cardUniqueID);
    }

    [ClientRpc(RequireOwnership = false)]
    public void ReceiveGoldFromTeammateClientRPC(int goldAmount)
    {
        
        if (SuperPowerSpawner.LocalInstance != null)
        {
            SuperPowerSpawner.LocalInstance.ReceiveGoldFromTeammate(goldAmount);
        }
    } 


    //ServerRPC
    [ServerRpc(RequireOwnership = false)]
    public void SyncGoldAndPowersServerRPC(int playerNo, int gold, string[] powers)
    {
        if (Server.Singleton != null)
        {
            Server.Singleton.UpdatePlayerGold(playerNo, gold);
            Server.Singleton.UpdatePlayerPowers(playerNo, new List<string>(powers));
        }
        else
        {
            
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyDealCenterFinishedServerRPC(ulong clientId)
    {
        server.OnClientDealCenterFinished(clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ActivateYandimAnamOnCardServerRPC(string cardUniqueID, ServerRpcParams rpcParams = default)
    {
        if (!ValidatePowerCaller(out int callerPlayerNo, rpcParams)) return;

        // Completing card selection ends interactive power flow.
        if (server != null)
        {
            server.StopPowerDurationTimerAndResumeTurn();
        }

        // Update the server's authoritative card data
        if (Server.Singleton != null && Server.Singleton.allCardLookup.ContainsKey(cardUniqueID))
        {
            Server.Singleton.allCardLookup[cardUniqueID][1] = 0; // Set value to 0
            Server.Singleton.BroadcastLiveScoreUpdate(); // Update scoreboard instantly!
        }
        ShowcaseSuperPowerClientRPC("Yandım Anam");
        YandimAnamCardChangedClientRPC(cardUniqueID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ShowcaseSuperPowerServerRPC(string powerName, float fadeDuration = 0.5f, float displayDuration = 2f)
    {
        if (ShouldRejectPowerRpcDuringMove(nameof(ShowcaseSuperPowerServerRPC))) return;
        ShowcaseSuperPowerClientRPC(powerName, fadeDuration, displayDuration);
    }

    [ServerRpc(RequireOwnership = false)]
    public void KopyalaYapistirServerRPC(string targetUniqueID, string sourceUniqueID, ServerRpcParams rpcParams = default)
    {
        if (!ValidatePowerCaller(out int callerPlayerNo, rpcParams)) return;

        

        // Completing Kopyala Yapıştır ends interactive selection, so stop power timer and resume turn timer.
        if (server != null)
        {
            server.StopPowerDurationTimerAndResumeTurn();
        }
        
        // Update the server's authoritative card data
        if (Server.Singleton != null && Server.Singleton.allCardLookup.ContainsKey(sourceUniqueID) && Server.Singleton.allCardLookup.ContainsKey(targetUniqueID))
        {
            var sourceID = Server.Singleton.allCardLookup[sourceUniqueID];
            
            Server.Singleton.allCardLookup[targetUniqueID][0] = sourceID[0]; // kind
            Server.Singleton.allCardLookup[targetUniqueID][1] = sourceID[1]; // value
            
            // Register copy mapping on the server so it is serialized in game state snapshots
            Server.Singleton.RegisterCopiedCard(targetUniqueID, sourceUniqueID);
            
            // Broadcast live score update to show copied points instantly!
            Server.Singleton.BroadcastLiveScoreUpdate();
        }
        else
        {
            
        }
        
        // Notify all clients to update visuals and local cardID
        
        ShowcaseSuperPowerClientRPC("Kopyala Yapıştır");
        KopyalaYapistirClientRPC(targetUniqueID, sourceUniqueID);
        
    }

    [ServerRpc(RequireOwnership = false)]
    public void ActivateKapkacOnCardServerRPC(string cardUniqueID, ServerRpcParams rpcParams = default)
    {
        if (!ValidatePowerCaller(out int callerPlayerNo, rpcParams)) return;

        // Completing card selection ends interactive power flow.
        if (server != null)
        {
            server.StopPowerDurationTimerAndResumeTurn();
        }

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

        // SIMPLIFIED ZAFER PUANI: Add 1 point to the player/team who activated Kapkaç
        // Commented out as points should only be gained at the end of the round when cards are evaluated.
        /*
        if (Server.Singleton != null)
        {
            Server.Singleton.AddZaferPuaniPoint(callerPlayerNo, 1);
        }
        */

        ShowcaseSuperPowerClientRPC("Kapkaç");
        KapkacCardChangedClientRPC(cardUniqueID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReportZaferPuaniServerRPC(int playerNo, int points)
    {
        server.AddZaferPuaniPoints(playerNo, points);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ActivateZaferPuaniServerRPC(int points, ServerRpcParams rpcParams = default)
    {
        if (!ValidatePowerCaller(out int callerPlayerNo, rpcParams)) return;
        if (ShouldRejectPowerRpcDuringMove(nameof(ActivateZaferPuaniServerRPC))) return;
        // DIRECT POINT ADDITION: Add points immediately to the player/team
        if (Server.Singleton != null)
        {
            Server.Singleton.AddZaferPuaniPoint(callerPlayerNo, points);
        }
        ShowcaseSuperPowerClientRPC("Zafer Puanı");
    }

    [ServerRpc(RequireOwnership = false)]
    public void UseRandomDegisTokusServerRPC(ServerRpcParams rpcParams = default)
    {
        if (!ValidatePowerCaller(out int callerPlayerNo, rpcParams)) return;
        if (ShouldRejectPowerRpcDuringMove(nameof(UseRandomDegisTokusServerRPC))) return;
        ShowcaseSuperPowerClientRPC("Değiş Tokuş");
        server.ExecuteRandomDegisTokus(callerPlayerNo);
    }

    [ServerRpc(RequireOwnership = false)]
    public void UseDegisTokusWithSelectedCardServerRPC(string selectedCardId, ServerRpcParams rpcParams = default)
    {
        if (!ValidatePowerCaller(out int callerPlayerNo, rpcParams)) return;
        if (ShouldRejectPowerRpcDuringMove(nameof(UseDegisTokusWithSelectedCardServerRPC))) return;

        if (string.IsNullOrEmpty(selectedCardId)) return;

        ShowcaseSuperPowerClientRPC("Değiş Tokuş");
        server.ExecuteDegisTokusWithSelectedCard(callerPlayerNo, selectedCardId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void UseSunuDegisTokusServerRPC(string firstHandCardID, string secondHandCardID, ServerRpcParams rpcParams = default)
    {
        if (!ValidatePowerCaller(out int callerPlayerNo, rpcParams)) return;

        if (string.IsNullOrEmpty(firstHandCardID) || string.IsNullOrEmpty(secondHandCardID)) return;
        if (firstHandCardID == secondHandCardID) return;

        int firstOwnerPlayerNo = server.FindOwnerOfCard(firstHandCardID);
        int secondOwnerPlayerNo = server.FindOwnerOfCard(secondHandCardID);
        if (firstOwnerPlayerNo == -1 || secondOwnerPlayerNo == -1) return;

        if (!server.IsCardInPlayerHand(firstOwnerPlayerNo, firstHandCardID)) return;
        if (!server.IsCardInPlayerHand(secondOwnerPlayerNo, secondHandCardID)) return;
        if (server.IsCardInCenter(firstHandCardID) || server.IsCardInCenter(secondHandCardID)) return;

        // Stop the power selection timer and resume the turn timer after animation
        server.StopPowerDurationTimerAndResumeTurn();

        ShowcaseSuperPowerClientRPC("Şunu Değiş Tokuş");
        server.SunuDegisTokusSwap(firstOwnerPlayerNo, secondOwnerPlayerNo, firstHandCardID, secondHandCardID);
        UseSunuDegisTokusClientRPC(firstOwnerPlayerNo, secondOwnerPlayerNo, firstHandCardID, secondHandCardID);
    }

    /// <summary>
    /// ServerRPC for non-host players to request a correction/undo from the host
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SendCorrectionRequestToHostServerRPC(ServerRpcParams rpcParams = default)
    {
        
        
        // Get the calling client's player number
        ulong callerClientId = rpcParams.Receive.SenderClientId;
        int callerPlayerNo = server.GetPlayerNoForClient(callerClientId);
        
        if (callerPlayerNo == -1)
        {
            
            return;
        }
        
        // Make sure the caller is NOT the host
        if (callerPlayerNo == 0)
        {
            
            return;
        }
        
        
        
        // Broadcast the correction request widget to the host
        NotifyHostOfCorrectionRequestClientRPC();
    }

    /// <summary>
    /// ClientRPC to notify all clients that a correction request widget should be shown on the host
    /// </summary>
    [ClientRpc(RequireOwnership = false)]
    public void NotifyHostOfCorrectionRequestClientRPC()
    {
        
        
        // Check if this is the host
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            
            
            // Get reference to MainUIScript and show the widget
            MainUIScript mainUI = FindObjectOfType<MainUIScript>();
            if (mainUI != null)
            {
                mainUI.ShowCorrectionRequestWidgetOnHost();
            }
            else
            {
                
            }
        }
    }

    // --- Power Duration Timer RPCs ---

    /// <summary>Pauses the server turn timer so it doesn't expire during interactive power selection.</summary>
    [ServerRpc(RequireOwnership = false)]
    public void PauseTurnTimerForPowerServerRPC()
    {
        if (server != null) server.PauseTurnTimerForPower();
    }

    /// <summary>Starts the server-side power selection timeout for the current player.</summary>
    [ServerRpc(RequireOwnership = false)]
    public void StartPowerDurationTimerServerRPC()
    {
        if (server != null) server.StartPowerDurationTimer();
    }

    /// <summary>Sent by server when the power selection timer expires — all clients cancel the active dual-selection power.</summary>
    [ClientRpc(RequireOwnership = false)]
    public void CancelPowerSelectionClientRPC()
    {
        if (GameManager.LocalInstance != null)
            GameManager.LocalInstance.CancelDualSelectionPower();
    }

    // --- End Power Duration Timer RPCs ---

    [ServerRpc(RequireOwnership = false)]
    public void UseBuDahaIyiServerRPC(string handCardID, string centerCardID, ServerRpcParams rpcParams = default)
    {
        if (!ValidatePowerCaller(out int callerPlayerNo, rpcParams)) return;

        

        // Completing card selection ends interactive power flow.
        if (server != null)
        {
            server.StopPowerDurationTimerAndResumeTurn();
        }

        ShowcaseSuperPowerClientRPC("Bu Daha İyi");
        if (!server.TryBlockPower())
        {
            int handCardOwnerPlayerNo = server.FindOwnerOfCard(handCardID);
            if (handCardOwnerPlayerNo == -1)
            {
                
                return;
            }

            // Require the provided center card to still exist on server for deterministic sync.
            if (!server.IsCardInCenter(centerCardID))
            {
                
                return;
            }

            
            server.BuDahaIyiSwap(handCardOwnerPlayerNo, handCardID, centerCardID);
            
            UseBuDahaIyiClientRPC(callerPlayerNo, handCardOwnerPlayerNo, handCardID, centerCardID);
            
        }
        else
        {
            
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ActivateVerZehriServerRPC(ServerRpcParams rpcParams = default)
    {
        if (!ValidatePowerCaller(out int callerPlayerNo, rpcParams)) return;
        if (ShouldRejectPowerRpcDuringMove(nameof(ActivateVerZehriServerRPC))) return;
        
        
        ShowcaseSuperPowerClientRPC("Ver Zehri");
        server.ActivateVerZehri();
        
    }

    [ServerRpc(RequireOwnership = false)]
    public void ActivateKutsalDesteServerRPC(ServerRpcParams rpcParams = default)
    {
        if (!ValidatePowerCaller(out int callerPlayerNo, rpcParams)) return;
        if (ShouldRejectPowerRpcDuringMove(nameof(ActivateKutsalDesteServerRPC))) return;
        
        
        ShowcaseSuperPowerClientRPC("Kutsal Deste");
        server.ActivateKutsalDeste();
        
    }

    /*[ServerRpc(RequireOwnership = false)]
    public void ActivateKapkacServerRPC()
    {
        server.ActivateKapkac();
    }*/

    [ServerRpc(RequireOwnership = false)]
    public void ActivateOynayamazsinServerRPC(ServerRpcParams rpcParams = default)
    {
        if (!ValidatePowerCaller(out int callerPlayerNo, rpcParams)) return;
        if (ShouldRejectPowerRpcDuringMove(nameof(ActivateOynayamazsinServerRPC))) return;
        
        
        ShowcaseSuperPowerClientRPC("Oynayamazsın");
        server.ActivateOynayamazsin();
        
    }

    [ServerRpc(RequireOwnership = false)]
    public void ActivateYapamazsınServerRPC(ServerRpcParams rpcParams = default)
    {
        if (!ValidatePowerCaller(out int callerPlayerNo, rpcParams)) return;
        if (ShouldRejectPowerRpcDuringMove(nameof(ActivateYapamazsınServerRPC))) return;
        ShowcaseSuperPowerClientRPC("Yapamazsın");
        if (!server.TryBlockPower()) server.ActivateYapamazsın();
    }

    [ServerRpc(RequireOwnership = false)]
    public void BombaServerRPC(ServerRpcParams rpcParams = default)
    {
        if (!ValidatePowerCaller(out int callerPlayerNo, rpcParams)) return;
        if (ShouldRejectPowerRpcDuringMove(nameof(BombaServerRPC))) return;
        ShowcaseSuperPowerClientRPC("Bomba");
        if (!server.TryBlockPower()) server.BombaCenter();
    }

    [ServerRpc(RequireOwnership = false)]
    public void UseBayaBayaBakServerRPC(ServerRpcParams rpcParams = default)
    {
        if (!ValidatePowerCaller(out int callerPlayerNo, rpcParams)) return;
        if (ShouldRejectPowerRpcDuringMove(nameof(UseBayaBayaBakServerRPC))) return;
        

        DebugChainPrinter.LocalInstance?.TrackNetworkRPC("UseBayaBayaBakServerRPC", $"callerPlayerNo={callerPlayerNo}");

        ShowcaseSuperPowerClientRPC("Baya Baya Bak");
        if (!server.TryBlockPower())
        {
            var opponents = server.GetOpponentPlayers(callerPlayerNo);
            if (opponents.Count == 0) return;
            int serverOpponentNo = opponents[UnityEngine.Random.Range(0, opponents.Count)];

            
            DebugChainPrinter.LocalInstance?.TrackLocalAction($"BayaBayaBak targeting server-selected opponent {serverOpponentNo}");
            UseBayaBayaBakClientRPC(serverOpponentNo);
        }
        else
        {
            
            DebugChainPrinter.LocalInstance?.TrackLocalAction("BayaBayaBak power was blocked by Yapamazsın");
        }

        
    }

    [ServerRpc(RequireOwnership = false)]
    public void UsePeekOpponentCardPowerServerRPC(ServerRpcParams rpcParams = default)
    {
        if (!ValidatePowerCaller(out int callerPlayerNo, rpcParams)) return;
        if (ShouldRejectPowerRpcDuringMove(nameof(UsePeekOpponentCardPowerServerRPC))) return;
        ShowcaseSuperPowerClientRPC("Ucundan Göz At");
        if (!server.TryBlockPower())
        {
            var opponents = server.GetOpponentPlayers(callerPlayerNo);
            if (opponents.Count == 0) return;
            int serverOpponentNo = opponents[UnityEngine.Random.Range(0, opponents.Count)];
            int handCount = server.GetHandCardCount(serverOpponentNo);
            if (handCount == 0) return;
            int serverCardIndex = UnityEngine.Random.Range(0, handCount);
            UsePeekOpponentCardPowerClientRPC(serverOpponentNo, serverCardIndex);
        }
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
        
        server.PrintMessage(message);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendMoveToServerRPC(string selectedHandCard, SerializableCard serializableCard, int playerNumber, int sumValue)
    {
        GameManager.AddToDebugLog($"[GameNetworkRelay] SendMoveToServerRPC called with selectedHandCard: {selectedHandCard}, playerNumber: {playerNumber}, sumValue: {sumValue}");
        GameManager.AddToDebugLog($"[GameNetworkRelay] serializableCard contains {serializableCard.ToDictionary().Count} cards");
        server.GetMove(selectedHandCard, serializableCard, playerNumber, sumValue);
        GameManager.AddToDebugLog($"[GameNetworkRelay] GetMove called on server");
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
    public void ReclaimSeatServerRPC(int playerNo, ulong clientId, string claimantPlayerId = null, ServerRpcParams rpcParams = default)
    {
        // Always use the authenticated sender id — never trust the client-passed value for auth decisions.
        ulong trustedClientId = rpcParams.Receive.SenderClientId;
        
        if (server != null)
        {
            bool accepted = server.TryHandleSeatReclaim(playerNo, trustedClientId, claimantPlayerId);
            if (!accepted)
            {
                
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyCientConnectedServerRPC(ulong clientId)
    {
        
        // Manual path disabled - AnotherPlayerConnected handles all reconnections
        return;
    }

    [ServerRpc(RequireOwnership = false)]
    public void DeckReadyServerRPC()
    {
        ulong clientId = OwnerClientId; // Use OwnerClientId to get the client that called this RPC
        
        Debug.Log($"[GameNetworkRelay] ===== ÖNEMLİ: DECK READY SERVER RPC RECEIVED =====\n" +
                 $"ClientId: {clientId}\n" +
                 $"IsServer: {IsServer}\n" +
                 $"Server instance: {(server != null ? "FOUND" : "NULL")}\n" +
                 $"This is for NORMAL GAME FLOW only - reconnection handled by ReconnectingClientCardsReadyServerRPC");
        
        // NORMAL GAME FLOW ONLY - proceed with initial deals
        
        server.InitialDealCoroutineCheck();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReconnectingClientCardsReadyServerRPC(ulong clientId, ServerRpcParams rpcParams = default)
    {
        // Trust the actual network sender ID
        ulong trustedClientId = rpcParams.Receive.SenderClientId;
        
        // Handshake state check
        if (server.clientReconnectPhases.TryGetValue(trustedClientId, out Server.ReconnectPhase phase))
        {
            if (phase != Server.ReconnectPhase.InitSent)
            {
                
                return;
            }
        }
        else
        {
            
            return;
        }

        server.clientReconnectPhases[trustedClientId] = Server.ReconnectPhase.SnapshotSent;

        // Build current game state and send to reconnected client
        var gameState = server.BuildGameStateSnapshot();
        
        
        
        // Single point of truth for state sync
        ApplySnapshotToClient(gameState, trustedClientId);
        
        server.clientReconnectPhases[trustedClientId] = Server.ReconnectPhase.Completed;

        // Update current player info for reconnected client
        server.CallUpdateCurrentPlayer();
        
        // Remove client from reconnecting set
        server.RemoveReconnectingClient(trustedClientId);
        
        // Trigger desync check
        TriggerDesyncCheckForReconnectedClientClientRPC(trustedClientId);

        // Authoritatively broadcast the live calculated score to update the reconnected player's UI immediately!
        server.BroadcastLiveScoreUpdate();
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyTurnIsReadyToEndServerRPC()
    {
        // Legacy — kept for compatibility. Use TurnProcessedServerRPC instead.
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        server.OnClientTurnProcessed(clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TurnProcessedServerRPC(ulong clientId)
    {
        server.OnClientTurnProcessed(clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyDealHandsFinishedServerRPC(ulong clientId)
    {
        server.OnClientDealHandsFinished(clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReconnectionReadyServerRPC(ulong clientId)
    {
        server.OnReconnectionReady(clientId);
    }

    /// <summary>
    /// Reconnecting client announces its saved player number so the server can rebind
    /// playerClientIds with the new network client ID. Uses SenderClientId to prevent spoofing.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void AnnounceReconnectedPlayerNumberServerRPC(int playerNo, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        
        server.RebindPlayerClientId(playerNo, senderClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void UseSunuDegisBunuTokusServerRPC(string myHandCardID, string otherHandCardID, int myHandIndex, bool readyToExit = false, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        int callerPlayerNo = server.GetPlayerNoForClient(senderClientId);
        

        if (callerPlayerNo == -1 || callerPlayerNo != server.currentPlayer)
        {
            
            return;
        }

        // Find the owner of the otherHandCardID
        int otherPlayerNo = server.FindOwnerOfCard(otherHandCardID);
        if (otherPlayerNo == -1 || otherPlayerNo == callerPlayerNo)
        {
            
            return;
        }

        

        server.SunuDegisBunuTokusSwap(callerPlayerNo, otherPlayerNo, myHandCardID, otherHandCardID, myHandIndex);
        

        if (readyToExit)
        {
            
            server.StopPowerDurationTimerAndResumeTurn();
            ShowcaseSuperPowerClientRPC("Şunu Değiş Bunu Tokuş");
        }

        
        UseSunuDegisBunuTokusClientRPC(callerPlayerNo, otherPlayerNo, myHandCardID, otherHandCardID, myHandIndex, readyToExit);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ShareGoldWithTeammateServerRPC(int teammateNumber, int goldAmount)
    {
        
        // Server validation - just debug print for now
        if (server != null)
        {
            // You can add server-side validation here if needed
            
        }
    }

    // ===== GAME STATE SYNC =====

    /// <summary>
    /// Client RPC to apply a complete game state snapshot to all clients
    /// </summary>
    [ClientRpc(RequireOwnership = false)]
    public void ApplyGameStateClientRPC(SerializableGameState snapshot, ClientRpcParams clientRpcParams = default)
    {
        
        
        GameManager.LocalInstance?.ApplyGameState(snapshot);
    }

    [ClientRpc(RequireOwnership = false)]
    public void ApplyGameStateToReconnectedClientClientRPC(SerializableGameState snapshot, ulong targetClientId)
    {
        // Only apply the game state if this is the target client
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            
            GameManager.LocalInstance?.ApplyGameState(snapshot);
        }
    }

    [ClientRpc(RequireOwnership = false)]
    public void TriggerDesyncCheckForReconnectedClientClientRPC(ulong targetClientId)
    {
        
        
        
        // Only trigger desync check if this is the target client
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            
            
            
            // Trigger desync check after a short delay to ensure game state is fully applied
            if (GameManager.LocalInstance != null)
            {
                
                GameManager.LocalInstance.TriggerDesyncCheckAfterReconnection();
            }
            else
            {
                
            }
        }
        else
        {
            
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
            
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestFullStateSyncServerRPC(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        
        
        if (Server.Singleton != null)
        {
            ApplyGameStateClientRPC(Server.Singleton.BuildGameStateSnapshot(), new ClientRpcParams {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
            });
        }
    }

    [ClientRpc(RequireOwnership = false)]
    public void OnPlayerDisconnectedClientRPC(ulong clientId, string reason)
    {
        
        
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
        
        
        if (Server.Singleton != null)
        {
            // Build current game state snapshot
            var gameState = Server.Singleton.BuildGameStateSnapshot();
            
            
            // Send game state to the requesting client
            ApplySnapshotToClient(gameState, clientId);
        }
        else
        {
            
        }
    }

    public void ApplySnapshotToClient(SerializableGameState snapshot, ulong targetClientId)
    {
        ApplyGameStateToReconnectedClientClientRPC(snapshot, targetClientId);
    }

    // Note: Reconnection now uses existing desync detection system

    // Note: Client sync completion notification removed - using desync detection system

    // ===== MOVE BUFFERING RPCs =====
    
    // ===== REDO SYSTEM RPCs =====
    
    /// <summary>
    /// Client RPC to apply a reverted game state to all clients
    /// </summary>
    [ClientRpc(RequireOwnership = false)]
    public void RedoRevertToStateClientRPC(SerializableGameState snapshot, string revertType)
    {
        
        
        // Apply the reverted state using existing game state system
        if (GameManager.LocalInstance != null)
        {
            GameManager.LocalInstance.OnRedoRevertToState(snapshot, revertType);
        }
        else
        {
            
        }
    }

    // --- Emote System RPCs ---

    [ServerRpc(RequireOwnership = false)]
    public void SendEmoteServerRPC(int playerNo, string emoji)
    {
        // Broadcast to all clients
        ShowEmoteClientRPC(playerNo, emoji);
    }

    [ClientRpc(RequireOwnership = false)]
    public void ShowEmoteClientRPC(int playerNo, string emoji)
    {
        if (EmoteManager.Instance != null)
        {
            int localPlayerNo = DeckController.LocalInstance != null ? DeckController.LocalInstance.thisPlayerNumber : -1;
            // Only show if it's NOT our own emote
            if (playerNo != localPlayerNo)
            {
                EmoteManager.Instance.ShowEmote(playerNo, emoji);
            }
        }
    }
    
    // --- End Emote System RPCs ---
    
    /// <summary>
    /// Server RPC for clients to request redo to previous state
/// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestRedoToPreviousStateServerRPC()
    {
        ulong clientId = OwnerClientId;
        
        
        if (server != null)
        {
            server.RevertToPreviousState();
        }
        else
        {
            
        }
    }
    
    /// <summary>
    /// Server RPC for clients to request redo to pre-previous state
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestRedoToPrePreviousStateServerRPC()
    {
        ulong clientId = OwnerClientId;
        
        
        if (server != null)
        {
            server.RevertToPrePreviousState();
        }
        else
        {
            
        }
    }
    
    /// <summary>
    /// Server RPC for clients to get redo state status
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestRedoStateStatusServerRPC()
    {
        ulong clientId = OwnerClientId;
        
        if (server != null)
        {
            string status = server.GetRedoStateStatus();
            
            
            // Send status back to requesting client
            SendRedoStateStatusClientRPC(status, clientId);
        }
        else
        {
            
        }
    }
    
    /// <summary>
    /// Client RPC to send redo state status to specific client
    /// </summary>
    [ClientRpc(RequireOwnership = false)]
    public void SendRedoStateStatusClientRPC(string status, ulong targetClientId)
    {
        // Only process if this is the target client
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            
            
            if (GameManager.LocalInstance != null)
            {
                GameManager.LocalInstance.OnRedoStateStatusReceived(status);
            }
        }
    }
    
    /// <summary>
    /// Server RPC for clients to confirm they've finished redo scene reconstruction
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ConfirmRedoSceneReconstructionFinishedServerRPC()
    {
        ulong clientId = OwnerClientId;
        
        
        if (server != null)
        {
            server.OnClientRedoSceneReconstructionFinished(clientId);
        }
        else
        {
            
        }
    }


}

namespace Unity.Netcode
{
    public static class NetworkSerializationExtensions
    {
        public static void WriteValueSafe(this FastBufferWriter writer, in string[] value)
        {
            if (value == null)
            {
                writer.WriteValueSafe(0);
                return;
            }
            writer.WriteValueSafe(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                writer.WriteValueSafe(value[i] ?? string.Empty);
            }
        }

        public static void ReadValueSafe(this FastBufferReader reader, out string[] value)
        {
            reader.ReadValueSafe(out int length);
            value = new string[length];
            for (int i = 0; i < length; i++)
            {
                reader.ReadValueSafe(out string element);
                value[i] = element;
            }
        }
    }
}
