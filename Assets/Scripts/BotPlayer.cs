using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// BotPlayer handles AI decision-making for the bot player.
/// This script is responsible for selecting cards and playing them through the normal game flow.
/// </summary>
public class BotPlayer : MonoBehaviour
{
    [Header("Bot Configuration")]
    [SerializeField] private int botPlayerNumber = 1; // Bot plays as player 1 (opponent)
    [SerializeField] private float moveDelay = 2.0f; // Delay before bot makes a move
    
    [Header("References")]
    private GameManager gameManager;
    private Server server;
    private NetworkRelay networkRelay;
    
    [Header("Bot State")]
    private bool isActive = false;
    private Coroutine moveCoroutine;
    
    // Bot waiting mechanism for dealing
    private bool isWaitingForDealing = false;
    private bool hasQueuedMove = false;
    private Coroutine waitingForDealingCoroutine;
    
    // Bot log collection system
    private static string botLogs = "";
    private static bool botLoggingEnabled = true;
    
    // Singleton pattern
    public static BotPlayer Instance { get; private set; }
    
    /// <summary>
    /// Adds a log message to the bot logs collection
    /// </summary>
    public static void AddBotLog(string message)
    {
        if (botLoggingEnabled)
        {
            botLogs += message + "\n";
        }
    }
    
    /// <summary>
    /// Clears all collected bot logs
    /// </summary>
    public static void ClearBotLogs()
    {
        botLogs = "";
    }
    
    /// <summary>
    /// Toggles bot logging on/off
    /// </summary>
    public static void ToggleBotLogging()
    {
        botLoggingEnabled = !botLoggingEnabled;
        Debug.Log($"[Bot] Bot logging {(botLoggingEnabled ? "enabled" : "disabled")}");
    }
    
    /// <summary>
    /// Helper method to log both to console and bot logs collection
    /// </summary>
    private static void BotLog(string message)
    {
        Debug.Log(message);
        AddBotLog(message);
    }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Get references to other components
        gameManager = GameManager.LocalInstance;
        server = Server.Singleton;
        networkRelay = NetworkRelay.Instance;
        
        if (gameManager == null)
        {
            Debug.LogError("[BotPlayer] GameManager not found!");
        }
        
        if (server == null)
        {
            Debug.LogError("[BotPlayer] Server not found!");
        }
        
        if (networkRelay == null)
        {
            Debug.LogError("[BotPlayer] NetworkRelay not found!");
        }
    }
    
    /// <summary>
    /// Activates the bot player
    /// </summary>
    public void ActivateBot()
    {
        BotLog($"[Bot] ===== BOT ACTIVATION =====");
        BotLog($"[Bot] ActivateBot called for player {botPlayerNumber}");
        isActive = true;
        BotLog($"[Bot] ✓ Bot activated for player {botPlayerNumber}");
        BotLog($"[Bot] ===== BOT ACTIVATION COMPLETE =====");
    }
    
    /// <summary>
    /// Deactivates the bot player
    /// </summary>
    public void DeactivateBot()
    {
        isActive = false;
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        Debug.Log("[BotPlayer] Bot deactivated");
    }
    
    /// <summary>
    /// Called by Server when it's the bot's turn
    /// </summary>
    public void OnBotTurn()
    {
        BotLog($"[Bot] ===== BOT TURN NOTIFICATION =====");
        BotLog($"[Bot] OnBotTurn called for player {botPlayerNumber}");
        
        if (!isActive)
        {
            Debug.LogWarning("[Bot] WARNING: Bot turn called but bot is not active");
            AddBotLog("[Bot] WARNING: Bot turn called but bot is not active");
            return;
        }
        
        // Check if bot's hand is empty (needs dealing)
        if (IsBotHandEmpty())
        {
            BotLog($"[Bot] Bot hand is empty - waiting for dealing to complete...");
            isWaitingForDealing = true;
            hasQueuedMove = true;
            
            // Start waiting coroutine
            if (waitingForDealingCoroutine != null)
            {
                StopCoroutine(waitingForDealingCoroutine);
            }
            waitingForDealingCoroutine = StartCoroutine(WaitForDealingComplete());
            
            BotLog($"[Bot] ===== BOT TURN NOTIFICATION COMPLETE (WAITING FOR DEALING) =====");
            return;
        }
        
        BotLog($"[Bot] ✓ Bot is active and has cards, scheduling move...");
        ScheduleBotMove();
        BotLog($"[Bot] ===== BOT TURN NOTIFICATION COMPLETE =====");
    }
    
    /// <summary>
    /// Schedules a bot move after a delay
    /// </summary>
    private void ScheduleBotMove()
    {
        BotLog($"[Bot] ScheduleBotMove called");
        
        if (moveCoroutine != null)
        {
            BotLog($"[Bot] Stopping existing move coroutine...");
            StopCoroutine(moveCoroutine);
        }
        
        BotLog($"[Bot] Starting new move coroutine with delay: {moveDelay} seconds");
        moveCoroutine = StartCoroutine(ExecuteBotMoveCoroutine());
        BotLog($"[Bot] ✓ Move coroutine started");
    }
    
    /// <summary>
    /// Coroutine that waits for a delay then executes a bot move
    /// </summary>
    private IEnumerator ExecuteBotMoveCoroutine()
    {
        BotLog($"[Bot] ExecuteBotMoveCoroutine started, waiting {moveDelay} seconds...");
        yield return new WaitForSeconds(moveDelay);
        
        BotLog($"[Bot] Delay complete, checking if bot is still active...");
        if (isActive)
        {
            BotLog($"[Bot] ✓ Bot is still active, executing move...");
            ExecuteBotMove();
        }
        else
        {
            BotLog($"[Bot] Bot is no longer active, skipping move execution");
        }
        
        BotLog($"[Bot] Move coroutine finished, cleaning up...");
        moveCoroutine = null;
        BotLog($"[Bot] ✓ Move coroutine cleanup complete");
    }
    
    /// <summary>
    /// Executes the bot's move by selecting and playing a card using the proper game flow
    /// </summary>
    private void ExecuteBotMove()
    {
        BotLog($"[Bot] ===== EXECUTE BOT MOVE START =====");
        BotLog($"[Bot] ExecuteBotMove called for bot player {botPlayerNumber}");
        
        // Get the first card from PlayerHand3 (skipping non-card children)
        BotLog($"[Bot] Getting first card from PlayerHand3...");
        CardInteraction selectedCard = GetFirstCardFromPlayerHand3();
        
        if (selectedCard == null)
        {
            Debug.LogError($"[Bot] ERROR: No card found in PlayerHand3 for bot player {botPlayerNumber}");
            AddBotLog($"[Bot] ERROR: No card found in PlayerHand3 for bot player {botPlayerNumber}");
            return;
        }
        
        BotLog($"[Bot] ✓ Card found: {selectedCard.uniqueCardInstanceID}");
        BotLog($"[Bot] Card data: {selectedCard.GetCardID()[0]}_{selectedCard.GetCardID()[1]}");
        
        // STEP 1: Simulate card selection (same as human player clicking a card)
        BotLog($"[Bot] STEP 1: Simulating card selection...");
        SimulateCardSelection(selectedCard);
        BotLog($"[Bot] ✓ Card selection simulated");
        
        // STEP 2: Send move directly to server (bypass local OnCardsPlayed processing)
        BotLog($"[Bot] STEP 2: Sending move directly to server...");
        BotLog($"[Bot] About to send move to server with card: {selectedCard.uniqueCardInstanceID}, player: {botPlayerNumber}");
        
        // Send move directly to server to avoid double processing
        SendBotMoveToServer(selectedCard);
        
        BotLog($"[Bot] ✓ Move sent to server successfully");
        BotLog($"[Bot] ===== EXECUTE BOT MOVE COMPLETE =====");
    }
    
    /// <summary>
    /// Sends the bot's move directly to the server, bypassing local OnCardsPlayed processing
    /// </summary>
    /// <param name="selectedCard">The card the bot wants to play</param>
    private void SendBotMoveToServer(CardInteraction selectedCard)
    {
        BotLog($"[Bot] SendBotMoveToServer called for card: {selectedCard.uniqueCardInstanceID}");
        
        // Get the current center cards (what the bot would capture)
        Dictionary<string, int[]> centerCards = gameManager.centerCards;
        SerializableCard serializableCard = new SerializableCard(centerCards);
        
        // Calculate sum value for capture logic
        int sumValue = centerCards.Count > 0 ? centerCards.Values.Last()[1] : 0;
        int cardValue = selectedCard.GetCardID()[1];
        
        BotLog($"[Bot] Card value: {cardValue}, Sum value: {sumValue}");
        BotLog($"[Bot] Center cards count: {centerCards.Count}");
        
        // Send the move to server
        BotLog($"[Bot] Sending move to server via NetworkRelay...");
        gameManager.networkRelay.SendMoveToServerRPC(selectedCard.uniqueCardInstanceID, serializableCard, botPlayerNumber, sumValue);
        
        BotLog($"[Bot] ✓ Move sent to server successfully");
    }
    
    /// <summary>
    /// Checks if the bot's hand (PlayerHand3) is empty
    /// </summary>
    /// <returns>True if the bot has no cards in its hand</returns>
    private bool IsBotHandEmpty()
    {
        BotLog($"[Bot] IsBotHandEmpty called");
        
        GameObject playerHand3 = GameObject.Find("PlayerHand3");
        if (playerHand3 == null)
        {
            BotLog($"[Bot] ERROR: PlayerHand3 not found - assuming hand is empty");
            return true;
        }
        
        // Count actual cards (skip first 2 non-card children)
        int cardCount = 0;
        for (int i = 2; i < playerHand3.transform.childCount; i++)
        {
            Transform child = playerHand3.transform.GetChild(i);
            if (child.GetComponent<CardInteraction>() != null)
            {
                cardCount++;
            }
        }
        
        BotLog($"[Bot] PlayerHand3 has {cardCount} cards (total children: {playerHand3.transform.childCount})");
        return cardCount == 0;
    }
    
    /// <summary>
    /// Waits for dealing to complete, then plays the queued move
    /// </summary>
    private IEnumerator WaitForDealingComplete()
    {
        BotLog($"[Bot] ===== WAITING FOR DEALING COMPLETE =====");
        BotLog($"[Bot] WaitForDealingComplete coroutine started");
        
        float maxWaitTime = 10f; // Maximum wait time in seconds
        float checkInterval = 0.5f; // Check every 0.5 seconds
        float elapsedTime = 0f;
        
        while (elapsedTime < maxWaitTime)
        {
            BotLog($"[Bot] Checking if dealing is complete... (elapsed: {elapsedTime:F1}s)");
            
            // Check if bot now has cards
            if (!IsBotHandEmpty())
            {
                BotLog($"[Bot] ✓ Dealing complete! Bot now has cards");
                break;
            }
            
            yield return new WaitForSeconds(checkInterval);
            elapsedTime += checkInterval;
        }
        
        if (elapsedTime >= maxWaitTime)
        {
            BotLog($"[Bot] WARNING: Timeout waiting for dealing to complete");
        }
        
        // Reset waiting state
        isWaitingForDealing = false;
        waitingForDealingCoroutine = null;
        
        // Play the queued move if we have one
        if (hasQueuedMove)
        {
            BotLog($"[Bot] Playing queued move after dealing complete");
            hasQueuedMove = false;
            ScheduleBotMove();
        }
        
        BotLog($"[Bot] ===== WAITING FOR DEALING COMPLETE FINISHED =====");
    }
    
    /// <summary>
    /// Called by GameManager when dealing is complete
    /// </summary>
    public void OnDealingComplete()
    {
        BotLog($"[Bot] ===== DEALING COMPLETE NOTIFICATION =====");
        BotLog($"[Bot] OnDealingComplete called");
        
        if (isWaitingForDealing)
        {
            BotLog($"[Bot] Bot was waiting for dealing - stopping wait coroutine");
            
            if (waitingForDealingCoroutine != null)
            {
                StopCoroutine(waitingForDealingCoroutine);
                waitingForDealingCoroutine = null;
            }
            
            isWaitingForDealing = false;
            
            // Play the queued move if we have one
            if (hasQueuedMove)
            {
                BotLog($"[Bot] Playing queued move after dealing notification");
                hasQueuedMove = false;
                ScheduleBotMove();
            }
        }
        else
        {
            BotLog($"[Bot] Bot was not waiting for dealing - no action needed");
        }
        
        BotLog($"[Bot] ===== DEALING COMPLETE NOTIFICATION FINISHED =====");
    }
    
    /// <summary>
    /// Gets the first card from PlayerHand3, skipping non-card children
    /// </summary>
    /// <returns>The CardInteraction component of the first card, or null if no card found</returns>
    public CardInteraction GetFirstCardFromPlayerHand3()
    {
        BotLog($"[Bot] GetFirstCardFromPlayerHand3 called");
        
        // Find PlayerHand3 GameObject
        BotLog($"[Bot] Looking for PlayerHand3 GameObject...");
        GameObject playerHand3 = GameObject.Find("PlayerHand3");
        if (playerHand3 == null)
        {
            Debug.LogError("[Bot] ERROR: PlayerHand3 GameObject not found!");
            AddBotLog("[Bot] ERROR: PlayerHand3 GameObject not found!");
            return null;
        }
        
        // PlayerHand3 structure: first 2 children are non-cards, rest are cards
        // Skip the first 2 children and get the first actual card
        int childCount = playerHand3.transform.childCount;
        BotLog($"[Bot] ✓ PlayerHand3 found, total children: {childCount}");
        
        BotLog($"[Bot] Searching for first card (skipping first 2 children)...");
        for (int i = 2; i < childCount; i++) // Start from index 2 to skip first 2 non-card children
        {
            Transform child = playerHand3.transform.GetChild(i);
            CardInteraction cardInteraction = child.GetComponent<CardInteraction>();
            
            if (cardInteraction != null)
            {
                BotLog($"[Bot] ✓ Found first card at index {i}: {cardInteraction.uniqueCardInstanceID}");
                return cardInteraction;
            }
            else
            {
                BotLog($"[Bot] Child {i} has no CardInteraction component");
            }
        }
        
        Debug.LogWarning("[Bot] WARNING: No cards found in PlayerHand3");
        AddBotLog("[Bot] WARNING: No cards found in PlayerHand3");
        return null;
    }
    
    /// <summary>
    /// Simulates the human player clicking on a card to select it
    /// </summary>
    private void SimulateCardSelection(CardInteraction card)
    {
        BotLog($"[Bot] SimulateCardSelection called for card: {card.uniqueCardInstanceID}");
        
        // Set the selected card in CardInteraction (global state)
        BotLog($"[Bot] Setting CardInteraction.currentlySelectedCard...");
        CardInteraction.currentlySelectedCard = card;
        CardInteraction.isOneCardSelected = true;
        BotLog($"[Bot] ✓ CardInteraction selection state set");
        
        // Set the selected card in GameManager
        if (gameManager != null)
        {
            BotLog($"[Bot] Setting GameManager.currentSelectedHandCard...");
            gameManager.SetCurrentSelectedHandCard(card.uniqueCardInstanceID);
            BotLog($"[Bot] ✓ GameManager selection state set");
        }
        else
        {
            Debug.LogError($"[Bot] ERROR: GameManager is null!");
            AddBotLog($"[Bot] ERROR: GameManager is null!");
        }
        
        BotLog($"[Bot] ✓ Card selection simulated successfully");
    }
    
    
    /// <summary>
    /// Gets the bot player number
    /// </summary>
    public int GetBotPlayerNumber()
    {
        return botPlayerNumber;
    }
    
    /// <summary>
    /// Checks if the bot is currently active
    /// </summary>
    public bool IsActive()
    {
        return isActive;
    }
    
    /// <summary>
    /// Force a bot move (for testing purposes)
    /// </summary>
    [ContextMenu("Force Bot Move")]
    public void ForceBotMove()
    {
        if (isActive)
        {
            Debug.Log("[BotPlayer] Forcing bot move via context menu");
            ExecuteBotMove();
        }
        else
        {
            Debug.LogWarning("[BotPlayer] Cannot force bot move - bot is not active");
        }
    }
    
    /// <summary>
    /// Sets the move delay for the bot
    /// </summary>
    public void SetMoveDelay(float delay)
    {
        moveDelay = delay;
    }
    
    /// <summary>
    /// Resets the bot for a new round
    /// </summary>
    public void ResetForNewRound()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        Debug.Log("[BotPlayer] Reset for new round");
    }
    
    /// <summary>
    /// Context menu function to print all collected bot logs
    /// </summary>
    [ContextMenu("Print All Bot Logs")]
    public void PrintAllBotLogs()
    {
        if (string.IsNullOrEmpty(botLogs))
        {
            Debug.Log("[Bot] No bot logs collected yet.");
            return;
        }
        
        Debug.Log($"[Bot] ===== ALL BOT LOGS =====");
        Debug.Log(botLogs);
        Debug.Log($"[Bot] ===== END BOT LOGS =====");
    }
    
    /// <summary>
    /// Context menu function to clear all collected bot logs
    /// </summary>
    [ContextMenu("Clear Bot Logs")]
    public void ClearBotLogsContextMenu()
    {
        ClearBotLogs();
        Debug.Log("[Bot] Bot logs cleared.");
    }
    
    /// <summary>
    /// Context menu function to toggle bot logging
    /// </summary>
    [ContextMenu("Toggle Bot Logging")]
    public void ToggleBotLoggingContextMenu()
    {
        ToggleBotLogging();
    }
}
