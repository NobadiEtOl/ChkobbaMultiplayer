using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Unity.Netcode;

public class DebugChainPrinter : MonoBehaviour
{
    public static DebugChainPrinter LocalInstance { get; private set; }

    // === TRACKING LISTS ===
    private List<string> localActionHistory = new List<string>();
    private List<string> cardMovementHistory = new List<string>();
    private List<string> powerUsageHistory = new List<string>();
    private List<string> networkRpcHistory = new List<string>();
    private List<string> moveChainHistory = new List<string>();

    private void Awake()
    {
        LocalInstance = this;
    }

    // === PUBLIC TRACKING METHODS ===
    public void TrackLocalAction(string action)
    {
        string timestampedAction = $"[{DateTime.Now:HH:mm:ss.fff}] {action}";
        localActionHistory.Add(timestampedAction);
        
    }

    public void TrackCardMovement(string cardId, string fromLocation, string toLocation, string reason)
    {
        string movement = $"Card {cardId}: {fromLocation} → {toLocation} ({reason})";
        cardMovementHistory.Add($"[{DateTime.Now:HH:mm:ss.fff}] {movement}");
        
    }

    public void TrackPowerUsage(string powerName, int playerNumber, string details)
    {
        string powerUsage = $"{powerName} by P{playerNumber}: {details}";
        powerUsageHistory.Add($"[{DateTime.Now:HH:mm:ss.fff}] {powerUsage}");
        
    }

    public void TrackNetworkRPC(string rpcName, string parameters)
    {
        string rpcCall = $"{rpcName}({parameters})";
        networkRpcHistory.Add($"[{DateTime.Now:HH:mm:ss.fff}] {rpcCall}");
        
    }

    public void TrackMoveChain(string chainInfo)
    {
        moveChainHistory.Add($"[{DateTime.Now:HH:mm:ss.fff}] {chainInfo}");
        
    }

    [ContextMenu("Print Debug Chain - Problematic Powers")]
    public void PrintDebugChainProblematicPowers()
    {
        string debugChain = BuildDebugChainProblematicPowers();
        
    }

    [ContextMenu("Print Debug Chain - All Powers")]
    public void PrintDebugChainAllPowers()
    {
        string debugChain = BuildDebugChainAllPowers();
        
    }

    [ContextMenu("Print Debug Chain - Card Details")]
    public void PrintDebugChainCardDetails()
    {
        string debugChain = BuildDebugChainCardDetails();
        
    }

    [ContextMenu("Print Debug Chain - Network RPCs")]
    public void PrintDebugChainNetworkRPCs()
    {
        string debugChain = BuildDebugChainNetworkRPCs();
        
    }

    [ContextMenu("Print Debug Chain - Complete State")]
    public void PrintDebugChainCompleteState()
    {
        string debugChain = BuildDebugChainCompleteState();
        
    }

    [ContextMenu("Print Debug Chain - Action History")]
    public void PrintDebugChainActionHistory()
    {
        string debugChain = BuildDebugChainActionHistory();
        
    }

    [ContextMenu("Print Debug Chain - Comprehensive Analysis")]
    public void PrintDebugChainComprehensiveAnalysis()
    {
        string debugChain = BuildDebugChainComprehensiveAnalysis();
        
    }

    private string BuildDebugChainProblematicPowers()
    {
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine($"TIMESTAMP: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine($"GAME STATE: Player {DeckController.LocalInstance?.thisPlayerNumber}, Turn {GameManager.LocalInstance?.turnCounter}, Current Player {GameManager.currentPlayerNo}");
        sb.AppendLine();
        
        // === OYNAYAMAZSIN DEBUG ===
        sb.AppendLine("=== OYNAYAMAZSIN DEBUG ===");
        sb.AppendLine($"oynayamazsinActive: {GameManager.LocalInstance?.oynayamazsinActive}");
        sb.AppendLine($"oynayamazsinBlockInstance: {(GameManager.LocalInstance?.oynayamazsinBlockInstance != null ? "EXISTS" : "NULL")}");
        sb.AppendLine($"oynayamazsinBlockPrefab: {(GameManager.LocalInstance?.oynayamazsinBlockPrefab != null ? "EXISTS" : "NULL")}");
        sb.AppendLine();
        
        // === VERZEHRI DEBUG ===
        sb.AppendLine("=== VERZEHRI DEBUG ===");
        sb.AppendLine($"verZehriActive: {GameManager.LocalInstance?.verZehriActive}");
        sb.AppendLine($"verZehriObject: {(GameManager.LocalInstance?.verZehriObject != null ? "EXISTS" : "NULL")}");
        sb.AppendLine();
        
        // === KUTSAL DESTE DEBUG ===
        sb.AppendLine("=== KUTSAL DESTE DEBUG ===");
        sb.AppendLine($"kutsalDesteActive: {GameManager.LocalInstance?.kutsalDesteActive}");
        sb.AppendLine($"kutsalDesteObject: {(GameManager.LocalInstance?.kutsalDesteObject != null ? "EXISTS" : "NULL")}");
        sb.AppendLine();
        
        // === KOPYALA YAPISTIR DEBUG ===
        sb.AppendLine("=== KOPYALA YAPISTIR DEBUG ===");
        sb.AppendLine($"isKopyalaActive: {GameManager.LocalInstance?.isKopyalaActive}");
        sb.AppendLine($"kopyalaSourceCard: {(GameManager.LocalInstance?.kopyalaSourceCard != null ? GameManager.LocalInstance.kopyalaSourceCard.uniqueCardInstanceID : "NULL")}");
        sb.AppendLine($"currentlySelectedCard: {(CardInteraction.currentlySelectedCard != null ? CardInteraction.currentlySelectedCard.uniqueCardInstanceID : "NULL")}");
        sb.AppendLine();
        
        // === BU DAHA IYI DEBUG ===
        sb.AppendLine("=== BU DAHA IYI DEBUG ===");
        sb.AppendLine($"currentSelectedHandCard: {(GameManager.LocalInstance?.currentSelectedHandCard != null ? GameManager.LocalInstance.currentSelectedHandCard : "NULL")}");
        sb.AppendLine($"centerCards count: {GameManager.LocalInstance?.centerCards.Count ?? 0}");
        if (GameManager.LocalInstance?.centerCards.Count > 0)
        {
            sb.AppendLine($"Top center card: {GameManager.LocalInstance.centerCards.Keys.Last()}");
        }
        sb.AppendLine();
        
        // === CARD INTERACTION DEBUG ===
        sb.AppendLine("=== CARD INTERACTION DEBUG ===");
        sb.AppendLine($"cardLookup count: {CardInteraction.cardLookup.Count}");
        sb.AppendLine($"myCards count: {GameManager.LocalInstance?.myCards.Count ?? 0}");
        sb.AppendLine();
        
        // === NETWORK RELAY DEBUG ===
        sb.AppendLine("=== NETWORK RELAY DEBUG ===");
        sb.AppendLine($"networkRelay: {(GameManager.LocalInstance?.networkRelay != null ? "EXISTS" : "NULL")}");
        sb.AppendLine($"IsHost: {NetworkManager.Singleton?.IsHost}");
        sb.AppendLine($"IsClient: {NetworkManager.Singleton?.IsClient}");
        sb.AppendLine();
        
        // === SERVER STATE DEBUG ===
        if (Server.Singleton != null)
        {
            sb.AppendLine("=== SERVER STATE DEBUG ===");
            sb.AppendLine($"Server.allCardLookup count: {Server.Singleton.allCardLookup?.Count() ?? 0}");
            sb.AppendLine($"Server.centerCardsDict count: {Server.Singleton.centerCardsDict?.Count ?? 0}");
            sb.AppendLine($"Server.playersHandCardsIDs count: {Server.Singleton.GetPlayerHand(DeckController.LocalInstance?.thisPlayerNumber ?? 0)?.Count ?? 0}");
            sb.AppendLine();
        }
        
        // === MOVE CHAIN DEBUG ===
        sb.AppendLine("=== MOVE CHAIN DEBUG ===");
        if (MoveChainTracker.ClientInstance != null)
        {
            var clientChain = MoveChainTracker.ClientInstance.GetCurrentChain();
            sb.AppendLine($"Client Chain Version: {clientChain.chainVersion}");
            sb.AppendLine($"Client Chain Move Count: {clientChain.moves?.Count() ?? 0}");
        }
        if (MoveChainTracker.ServerInstance != null)
        {
            var serverChain = MoveChainTracker.ServerInstance.GetCurrentChain();
            sb.AppendLine($"Server Chain Version: {serverChain.chainVersion}");
            sb.AppendLine($"Server Chain Move Count: {serverChain.moves?.Count() ?? 0}");
        }
        sb.AppendLine();
        
        return sb.ToString();
    }

    private string BuildDebugChainAllPowers()
    {
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine($"TIMESTAMP: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine($"GAME STATE: Player {DeckController.LocalInstance?.thisPlayerNumber}, Turn {GameManager.LocalInstance?.turnCounter}, Current Player {GameManager.currentPlayerNo}");
        sb.AppendLine();
        
        // === ALL POWER STATES ===
        sb.AppendLine("=== ALL POWER STATES ===");
        sb.AppendLine($"isKapkacPending: {GameManager.LocalInstance?.isKapkacPending}");
        sb.AppendLine($"isYandimAnamPending: {GameManager.LocalInstance?.isYandimAnamPending}");
        sb.AppendLine($"isKopyalaActive: {GameManager.LocalInstance?.isKopyalaActive}");
        sb.AppendLine($"isSunuDegisTokusActive: {GameManager.LocalInstance?.isSunuDegisTokusActive}");
        sb.AppendLine($"isSunuDegisBunuTokusActive: {GameManager.LocalInstance?.isSunuDegisBunuTokusActive}");
        sb.AppendLine($"oynayamazsinActive: {GameManager.LocalInstance?.oynayamazsinActive}");
        sb.AppendLine($"verZehriActive: {GameManager.LocalInstance?.verZehriActive}");
        sb.AppendLine($"kutsalDesteActive: {GameManager.LocalInstance?.kutsalDesteActive}");
        sb.AppendLine();
        
        // === CARD SELECTION STATES ===
        sb.AppendLine("=== CARD SELECTION STATES ===");
        sb.AppendLine($"CardInteraction.currentlySelectedCard: {(CardInteraction.currentlySelectedCard != null ? CardInteraction.currentlySelectedCard.uniqueCardInstanceID : "NULL")}");
        sb.AppendLine($"currentSelectedHandCard: {(GameManager.LocalInstance?.currentSelectedHandCard != null ? GameManager.LocalInstance.currentSelectedHandCard : "NULL")}");
        sb.AppendLine($"kopyalaSourceCard: {(GameManager.LocalInstance?.kopyalaSourceCard != null ? GameManager.LocalInstance.kopyalaSourceCard.uniqueCardInstanceID : "NULL")}");
        sb.AppendLine();
        
        // === GAME STATE COUNTS ===
        sb.AppendLine("=== GAME STATE COUNTS ===");
        sb.AppendLine($"centerCards count: {GameManager.LocalInstance?.centerCards.Count ?? 0}");
        sb.AppendLine($"myCards count: {GameManager.LocalInstance?.myCards.Count ?? 0}");
        sb.AppendLine($"cardLookup count: {CardInteraction.cardLookup.Count}");
        sb.AppendLine();
        
        // === NETWORK STATE ===
        sb.AppendLine("=== NETWORK STATE ===");
        sb.AppendLine($"networkRelay: {(GameManager.LocalInstance?.networkRelay != null ? "EXISTS" : "NULL")}");
        sb.AppendLine($"IsHost: {NetworkManager.Singleton?.IsHost}");
        sb.AppendLine($"IsClient: {NetworkManager.Singleton?.IsClient}");
        sb.AppendLine($"IsConnected: {NetworkManager.Singleton?.IsConnectedClient}");
        sb.AppendLine();
        
        return sb.ToString();
    }

    private string BuildDebugChainCardDetails()
    {
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine($"TIMESTAMP: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine();
        
        // === SELECTED CARDS ===
        sb.AppendLine("=== SELECTED CARDS ===");
        if (CardInteraction.currentlySelectedCard != null)
        {
            var card = CardInteraction.currentlySelectedCard;
            sb.AppendLine($"currentlySelectedCard: {card.uniqueCardInstanceID}");
            sb.AppendLine($"  - Card ID: [{card.GetCardID()[0]}, {card.GetCardID()[1]}]");
            sb.AppendLine($"  - Active Power Effect: {card.activePowerEffect}");
            sb.AppendLine($"  - GameObject: {card.gameObject.name}");
        }
        else
        {
            sb.AppendLine("currentlySelectedCard: NULL");
        }
        
        if (GameManager.LocalInstance?.kopyalaSourceCard != null)
        {
            sb.AppendLine($"kopyalaSourceCard: {GameManager.LocalInstance.kopyalaSourceCard.uniqueCardInstanceID}");
            sb.AppendLine($"  - Card ID: [{GameManager.LocalInstance.kopyalaSourceCard.GetCardID()[0]}, {GameManager.LocalInstance.kopyalaSourceCard.GetCardID()[1]}]");
            sb.AppendLine($"  - Active Power Effect: {GameManager.LocalInstance.kopyalaSourceCard.activePowerEffect}");
        }
        else
        {
            sb.AppendLine("kopyalaSourceCard: NULL");
        }
        sb.AppendLine();
        
        // === MY CARDS ===
        sb.AppendLine("=== MY CARDS ===");
        sb.AppendLine($"myCards count: {GameManager.LocalInstance?.myCards.Count ?? 0}");
        if (GameManager.LocalInstance?.myCards != null)
        {
            for (int i = 0; i < GameManager.LocalInstance.myCards.Count && i < 10; i++) // Limit to first 10 cards
            {
                string cardId = GameManager.LocalInstance.myCards[i];
                if (CardInteraction.cardLookup.TryGetValue(cardId, out var card))
                {
                    sb.AppendLine($"  {i}: {cardId} -> [{card.GetCardID()[0]}, {card.GetCardID()[1]}]");
                }
                else
                {
                    sb.AppendLine($"  {i}: {cardId} -> NOT FOUND IN CARDLOOKUP");
                }
            }
            if (GameManager.LocalInstance.myCards.Count > 10)
            {
                sb.AppendLine($"  ... and {GameManager.LocalInstance.myCards.Count - 10} more cards");
            }
        }
        sb.AppendLine();
        
        // === CENTER CARDS ===
        sb.AppendLine("=== CENTER CARDS ===");
        sb.AppendLine($"centerCards count: {GameManager.LocalInstance?.centerCards.Count ?? 0}");
        if (GameManager.LocalInstance?.centerCards != null)
        {
            int centerIndex = 0;
            foreach (var kvp in GameManager.LocalInstance.centerCards)
            {
                if (centerIndex >= 10) break; // Limit to first 10 cards
                sb.AppendLine($"  {centerIndex}: {kvp.Key} -> [{kvp.Value[0]}, {kvp.Value[1]}]");
                centerIndex++;
            }
            if (GameManager.LocalInstance.centerCards.Count > 10)
            {
                sb.AppendLine($"  ... and {GameManager.LocalInstance.centerCards.Count - 10} more cards");
            }
        }
        sb.AppendLine();
        
        // === SERVER CARD LOOKUP ===
        if (Server.Singleton != null)
        {
            sb.AppendLine("=== SERVER CARD LOOKUP SAMPLE ===");
            sb.AppendLine($"Server.allCardLookup count: {Server.Singleton.allCardLookup.Count()}");
            int serverIndex = 0;
            foreach (var kvp in Server.Singleton.allCardLookup)
            {
                if (serverIndex >= 5) break; // Limit to first 5 cards
                sb.AppendLine($"  {serverIndex}: {kvp.Key} -> [{kvp.Value[0]}, {kvp.Value[1]}]");
                serverIndex++;
            }
            if (Server.Singleton.allCardLookup.Count() > 5)
            {
                sb.AppendLine($"  ... and {Server.Singleton.allCardLookup.Count() - 5} more cards");
            }
            sb.AppendLine();
        }
        
        return sb.ToString();
    }

    private string BuildDebugChainNetworkRPCs()
    {
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine($"TIMESTAMP: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine();
        
        // === NETWORK MANAGER STATE ===
        sb.AppendLine("=== NETWORK MANAGER STATE ===");
        if (NetworkManager.Singleton != null)
        {
            sb.AppendLine($"IsHost: {NetworkManager.Singleton.IsHost}");
            sb.AppendLine($"IsClient: {NetworkManager.Singleton.IsClient}");
            sb.AppendLine($"IsConnected: {NetworkManager.Singleton.IsConnectedClient}");
            sb.AppendLine($"IsListening: {NetworkManager.Singleton.IsListening}");
            sb.AppendLine($"LocalClientId: {NetworkManager.Singleton.LocalClientId}");
            sb.AppendLine($"ConnectedClientsCount: {NetworkManager.Singleton.ConnectedClients.Count}");
        }
        else
        {
            sb.AppendLine("NetworkManager.Singleton: NULL");
        }
        sb.AppendLine();
        
        // === NETWORK RELAY STATE ===
        sb.AppendLine("=== NETWORK RELAY STATE ===");
        sb.AppendLine($"networkRelay: {(GameManager.LocalInstance?.networkRelay != null ? "EXISTS" : "NULL")}");
        if (GameManager.LocalInstance?.networkRelay != null)
        {
            sb.AppendLine($"GameNetworkRelay.Instance: {(GameNetworkRelay.Instance != null ? "EXISTS" : "NULL")}");
        }
        sb.AppendLine();
        
        // === SERVER STATE ===
        sb.AppendLine("=== SERVER STATE ===");
        sb.AppendLine($"Server.Singleton: {(Server.Singleton != null ? "EXISTS" : "NULL")}");
        if (Server.Singleton != null)
        {
            sb.AppendLine($"Server.currentPlayer: {Server.Singleton.currentPlayer}");
            sb.AppendLine($"Server.turnCounter: {Server.Singleton.turnCounter}");
            sb.AppendLine($"Server.lastPlayerToCapture: {Server.Singleton.lastPlayerToCapture}");
        }
        sb.AppendLine();
        
        // === MOVE CHAIN STATE ===
        sb.AppendLine("=== MOVE CHAIN STATE ===");
        sb.AppendLine($"MoveChainTracker.ClientInstance: {(MoveChainTracker.ClientInstance != null ? "EXISTS" : "NULL")}");
        sb.AppendLine($"MoveChainTracker.ServerInstance: {(MoveChainTracker.ServerInstance != null ? "EXISTS" : "NULL")}");
        sb.AppendLine();
        
        return sb.ToString();
    }

    private string BuildDebugChainCompleteState()
    {
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine($"TIMESTAMP: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine($"GAME STATE: Player {DeckController.LocalInstance?.thisPlayerNumber}, Turn {GameManager.LocalInstance?.turnCounter}, Current Player {GameManager.currentPlayerNo}");
        sb.AppendLine();
        
        // Combine all debug chains
        sb.Append(BuildDebugChainProblematicPowers());
        sb.AppendLine();
        sb.Append(BuildDebugChainAllPowers());
        sb.AppendLine();
        sb.Append(BuildDebugChainCardDetails());
        sb.AppendLine();
        sb.Append(BuildDebugChainNetworkRPCs());
        
        return sb.ToString();
    }

    private string BuildDebugChainActionHistory()
    {
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine($"TIMESTAMP: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine();
        
        // === LOCAL ACTION HISTORY ===
        sb.AppendLine("=== LOCAL ACTION HISTORY ===");
        if (localActionHistory.Count > 0)
        {
            foreach (var action in localActionHistory.TakeLast(20)) // Last 20 actions
            {
                sb.AppendLine(action);
            }
            if (localActionHistory.Count > 20)
            {
                sb.AppendLine($"... and {localActionHistory.Count - 20} more actions");
            }
        }
        else
        {
            sb.AppendLine("No local actions tracked");
        }
        sb.AppendLine();
        
        // === CARD MOVEMENT HISTORY ===
        sb.AppendLine("=== CARD MOVEMENT HISTORY ===");
        if (cardMovementHistory.Count > 0)
        {
            foreach (var movement in cardMovementHistory.TakeLast(15)) // Last 15 movements
            {
                sb.AppendLine(movement);
            }
            if (cardMovementHistory.Count > 15)
            {
                sb.AppendLine($"... and {cardMovementHistory.Count - 15} more movements");
            }
        }
        else
        {
            sb.AppendLine("No card movements tracked");
        }
        sb.AppendLine();
        
        // === POWER USAGE HISTORY ===
        sb.AppendLine("=== POWER USAGE HISTORY ===");
        if (powerUsageHistory.Count > 0)
        {
            foreach (var powerUsage in powerUsageHistory.TakeLast(10)) // Last 10 power usages
            {
                sb.AppendLine(powerUsage);
            }
            if (powerUsageHistory.Count > 10)
            {
                sb.AppendLine($"... and {powerUsageHistory.Count - 10} more power usages");
            }
        }
        else
        {
            sb.AppendLine("No power usages tracked");
        }
        sb.AppendLine();
        
        // === NETWORK RPC HISTORY ===
        sb.AppendLine("=== NETWORK RPC HISTORY ===");
        if (networkRpcHistory.Count > 0)
        {
            foreach (var rpc in networkRpcHistory.TakeLast(15)) // Last 15 RPCs
            {
                sb.AppendLine(rpc);
            }
            if (networkRpcHistory.Count > 15)
            {
                sb.AppendLine($"... and {networkRpcHistory.Count - 15} more RPCs");
            }
        }
        else
        {
            sb.AppendLine("No network RPCs tracked");
        }
        sb.AppendLine();
        
        // === MOVE CHAIN HISTORY ===
        sb.AppendLine("=== MOVE CHAIN HISTORY ===");
        if (moveChainHistory.Count > 0)
        {
            foreach (var chain in moveChainHistory.TakeLast(10)) // Last 10 chain events
            {
                sb.AppendLine(chain);
            }
            if (moveChainHistory.Count > 10)
            {
                sb.AppendLine($"... and {moveChainHistory.Count - 10} more chain events");
            }
        }
        else
        {
            sb.AppendLine("No move chain events tracked");
        }
        sb.AppendLine();
        
        return sb.ToString();
    }

    private string BuildDebugChainComprehensiveAnalysis()
    {
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine($"TIMESTAMP: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine($"GAME STATE: Player {DeckController.LocalInstance?.thisPlayerNumber}, Turn {GameManager.LocalInstance?.turnCounter}, Current Player {GameManager.currentPlayerNo}");
        sb.AppendLine();
        
        // === COMPREHENSIVE STATE ===
        sb.Append(BuildDebugChainCompleteState());
        sb.AppendLine();
        
        // === ACTION HISTORY ===
        sb.Append(BuildDebugChainActionHistory());
        sb.AppendLine();
        
        // === DETAILED MOVE CHAIN COMPARISON ===
        sb.AppendLine("=== DETAILED MOVE CHAIN COMPARISON ===");
        if (MoveChainTracker.ClientInstance != null && MoveChainTracker.ServerInstance != null)
        {
            var clientChain = MoveChainTracker.ClientInstance.GetCurrentChain();
            var serverChain = MoveChainTracker.ServerInstance.GetCurrentChain();
            
            sb.AppendLine($"Client Chain Version: {clientChain.chainVersion}");
            sb.AppendLine($"Server Chain Version: {serverChain.chainVersion}");
            sb.AppendLine($"Version Match: {clientChain.chainVersion == serverChain.chainVersion}");
            
            sb.AppendLine($"Client Moves Count: {clientChain.moves?.Count() ?? 0}");
            sb.AppendLine($"Server Moves Count: {serverChain.moves?.Count() ?? 0}");
            sb.AppendLine($"Move Count Match: {(clientChain.moves?.Count() ?? 0) == (serverChain.moves?.Count() ?? 0)}");
            
            // Compare individual moves
            if (clientChain.moves != null && serverChain.moves != null)
            {
                int maxMoves = Math.Min(clientChain.moves.Count(), serverChain.moves.Count());
                for (int i = 0; i < maxMoves; i++)
                {
                    var clientMove = clientChain.moves.ElementAt(i);
                    var serverMove = serverChain.moves.ElementAt(i);
                    
                    bool movesMatch = clientMove.moveType == serverMove.moveType && 
                                    clientMove.playerNumber == serverMove.playerNumber &&
                                    clientMove.cardId == serverMove.cardId;
                    
                    sb.AppendLine($"Move {i}: Client={clientMove.moveType}(P{clientMove.playerNumber},{clientMove.cardId}) vs Server={serverMove.moveType}(P{serverMove.playerNumber},{serverMove.cardId}) - Match: {movesMatch}");
                }
            }
        }
        else
        {
            sb.AppendLine("MoveChainTracker instances not available for detailed comparison");
        }
        sb.AppendLine();
        
        // === CARD STATE COMPARISON ===
        sb.AppendLine("=== CARD STATE COMPARISON ===");
        if (Server.Singleton != null && GameManager.LocalInstance != null)
        {
            // Center cards comparison
            int serverCenterCount = Server.Singleton.centerCardsDict?.Count ?? 0;
            int clientCenterCount = GameManager.LocalInstance.centerCards.Count;
            sb.AppendLine($"Center Cards - Server: {serverCenterCount}, Client: {clientCenterCount}, Match: {serverCenterCount == clientCenterCount}");
            
            // My cards comparison
            var serverHand = Server.Singleton.GetPlayerHand(DeckController.LocalInstance?.thisPlayerNumber ?? 0);
            var clientHand = GameManager.LocalInstance.myCards ?? new List<string>();
            sb.AppendLine($"My Cards - Server: {serverHand?.Count ?? 0}, Client: {clientHand.Count}, Match: {(serverHand?.Count ?? 0) == clientHand.Count}");
            
            // Card lookup comparison
            int serverLookupCount = Server.Singleton.allCardLookup?.Count() ?? 0;
            int clientLookupCount = CardInteraction.cardLookup.Count;
            sb.AppendLine($"Card Lookup - Server: {serverLookupCount}, Client: {clientLookupCount}, Match: {serverLookupCount == clientLookupCount}");
        }
        sb.AppendLine();
        
        // === RECENT GAME EVENTS ===
        sb.AppendLine("=== RECENT GAME EVENTS (Last 10) ===");
        var allEvents = new List<string>();
        allEvents.AddRange(localActionHistory.TakeLast(5));
        allEvents.AddRange(cardMovementHistory.TakeLast(3));
        allEvents.AddRange(powerUsageHistory.TakeLast(2));
        
        foreach (var evt in allEvents.OrderByDescending(x => x).Take(10))
        {
            sb.AppendLine(evt);
        }
        sb.AppendLine();
        
        return sb.ToString();
    }

    // === ADDITIONAL DEBUG METHODS ===

    [ContextMenu("Print Debug Chain - Superpower Activation Flow")]
    public void PrintDebugChainSuperpowerActivationFlow()
    {
        string debugChain = BuildDebugChainSuperpowerActivationFlow();
        
    }

    private string BuildDebugChainSuperpowerActivationFlow()
    {
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine($"TIMESTAMP: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine();
        
        // === SUPERPOWER SPAWNER STATE ===
        sb.AppendLine("=== SUPERPOWER SPAWNER STATE ===");
        sb.AppendLine($"SuperPowerSpawner.LocalInstance: {(SuperPowerSpawner.LocalInstance != null ? "EXISTS" : "NULL")}");
        if (SuperPowerSpawner.LocalInstance != null)
        {
            sb.AppendLine($"Available Powers Count: {SuperPowerSpawner.LocalInstance.GetAllTokenData().Count}");
            sb.AppendLine($"Gold Amount: {SuperPowerSpawner.LocalInstance.GetCurrentGold()}");
        }
        sb.AppendLine();
        
        // === RECENT SUPERPOWER ACTIVATIONS ===
        sb.AppendLine("=== RECENT SUPERPOWER ACTIVATIONS ===");
        var recentPowers = powerUsageHistory.TakeLast(5);
        if (recentPowers.Any())
        {
            foreach (var power in recentPowers)
            {
                sb.AppendLine(power);
            }
        }
        else
        {
            sb.AppendLine("No recent power activations tracked");
        }
        sb.AppendLine();
        
        // === POWER BLOCKING STATE ===
        sb.AppendLine("=== POWER BLOCKING STATE ===");
        sb.AppendLine($"Power Block Count: {Server.Singleton?.blockCount ?? 0}");
        sb.AppendLine();
        
        // === NETWORK RPC CALLS ===
        sb.AppendLine("=== NETWORK RPC CALLS ===");
        var recentRPCs = networkRpcHistory.TakeLast(5);
        if (recentRPCs.Any())
        {
            foreach (var rpc in recentRPCs)
            {
                sb.AppendLine(rpc);
            }
        }
        else
        {
            sb.AppendLine("No recent RPC calls tracked");
        }
        sb.AppendLine();
        
        return sb.ToString();
    }

    [ContextMenu("Print Debug Chain - Desync Analysis")]
    public void PrintDebugChainDesyncAnalysis()
    {
        string debugChain = BuildDebugChainDesyncAnalysis();
        
    }

    private string BuildDebugChainDesyncAnalysis()
    {
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine($"TIMESTAMP: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine();
        
        // === MOVE CHAIN COMPARISON ===
        sb.AppendLine("=== MOVE CHAIN COMPARISON ===");
        if (MoveChainTracker.ClientInstance != null && MoveChainTracker.ServerInstance != null)
        {
            var clientChain = MoveChainTracker.ClientInstance.GetCurrentChain();
            var serverChain = MoveChainTracker.ServerInstance.GetCurrentChain();
            
            sb.AppendLine($"Client Chain Version: {clientChain.chainVersion}");
            sb.AppendLine($"Server Chain Version: {serverChain.chainVersion}");
            sb.AppendLine($"Chain Versions Match: {clientChain.chainVersion == serverChain.chainVersion}");
            
            if (clientChain.chainVersion != serverChain.chainVersion)
            {
                sb.AppendLine("❌ DESYNC DETECTED: Chain versions don't match!");
            }
            else
            {
                sb.AppendLine("✅ Chain versions match");
            }
            
            sb.AppendLine($"Client Moves Count: {clientChain.moves?.Count() ?? 0}");
            sb.AppendLine($"Server Moves Count: {serverChain.moves?.Count() ?? 0}");
            
            if ((clientChain.moves?.Count() ?? 0) != (serverChain.moves?.Count() ?? 0))
            {
                sb.AppendLine("❌ DESYNC DETECTED: Move counts don't match!");
            }
            else
            {
                sb.AppendLine("✅ Move counts match");
            }
        }
        else
        {
            sb.AppendLine("MoveChainTracker instances not available");
        }
        sb.AppendLine();
        
        // === CARD STATE COMPARISON ===
        sb.AppendLine("=== CARD STATE COMPARISON ===");
        if (Server.Singleton != null && GameManager.LocalInstance != null)
        {
            int serverCenterCount = Server.Singleton.centerCardsDict?.Count ?? 0;
            int clientCenterCount = GameManager.LocalInstance.centerCards.Count;
            
            sb.AppendLine($"Server Center Cards: {serverCenterCount}");
            sb.AppendLine($"Client Center Cards: {clientCenterCount}");
            sb.AppendLine($"Center Cards Match: {serverCenterCount == clientCenterCount}");
            
            if (serverCenterCount != clientCenterCount)
            {
                sb.AppendLine("❌ DESYNC DETECTED: Center card counts don't match!");
            }
            else
            {
                sb.AppendLine("✅ Center card counts match");
            }
        }
        sb.AppendLine();
        
        // === PLAYER HAND COMPARISON ===
        sb.AppendLine("=== PLAYER HAND COMPARISON ===");
        if (Server.Singleton != null && DeckController.LocalInstance != null)
        {
            var serverHand = Server.Singleton.GetPlayerHand(DeckController.LocalInstance.thisPlayerNumber);
            var clientHand = GameManager.LocalInstance?.myCards ?? new List<string>();
            
            sb.AppendLine($"Server Hand Count: {serverHand.Count}");
            sb.AppendLine($"Client Hand Count: {clientHand.Count}");
            sb.AppendLine($"Hand Counts Match: {serverHand.Count == clientHand.Count}");
            
            if (serverHand.Count != clientHand.Count)
            {
                sb.AppendLine("❌ DESYNC DETECTED: Hand counts don't match!");
            }
            else
            {
                sb.AppendLine("✅ Hand counts match");
            }
        }
        sb.AppendLine();
        
        return sb.ToString();
    }

    // === CLEAR HISTORY METHODS ===
    [ContextMenu("Clear All History")]
    public void ClearAllHistory()
    {
        localActionHistory.Clear();
        cardMovementHistory.Clear();
        powerUsageHistory.Clear();
        networkRpcHistory.Clear();
        moveChainHistory.Clear();
        
    }

    [ContextMenu("Clear Action History")]
    public void ClearActionHistory()
    {
        localActionHistory.Clear();
        
    }

    [ContextMenu("Clear Card Movement History")]
    public void ClearCardMovementHistory()
    {
        cardMovementHistory.Clear();
        
    }

    [ContextMenu("Clear Power Usage History")]
    public void ClearPowerUsageHistory()
    {
        powerUsageHistory.Clear();
        
    }

    [ContextMenu("Clear Network RPC History")]
    public void ClearNetworkRpcHistory()
    {
        networkRpcHistory.Clear();
        
    }

    [ContextMenu("Clear Move Chain History")]
    public void ClearMoveChainHistory()
    {
        moveChainHistory.Clear();
        
    }
}
