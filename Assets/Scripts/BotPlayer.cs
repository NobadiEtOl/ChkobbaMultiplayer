using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Bot player system that runs on the host client
/// Manages bot behavior for 1v1 games using PlayerHand3
/// </summary>
public class BotPlayer : MonoBehaviour
{
    [Header("Bot Configuration")]
    [SerializeField] private bool isBotModeEnabled = true; // Auto-enabled when created by GameManager
    [SerializeField] private float botMoveDelay = 0.5f; // Reduced from 2.0f to 0.5f
    [SerializeField] private float botMoveDelayAfterDeal = 2.0f; // Extra delay after dealing animations
    [SerializeField] private int botPlayerNumber = 1; // Bot plays as player 2 (opponent)
    
    [Header("References")]
    private GameManager gameManager;
    private DeckController deckController;
    private NetworkRelay networkRelay;
    
    [Header("Bot State")]
    private bool botIsActive = false;
    private Coroutine botMoveCoroutine;
    private Transform playerHand3; // Bot's hand
    private bool isBotMoveInProgress = false; // Prevent multiple simultaneous moves
    private int botMoveCount = 0; // Track how many moves the bot has made
    private bool justFinishedWaitingForDeal = false; // Track if bot just finished waiting for dealing
    
    void Start()
    {
        Debug.Log("[Bot] BotPlayer Start() called");
        
        // Get references
        gameManager = GetComponent<GameManager>();
        deckController = FindObjectOfType<DeckController>();
        networkRelay = FindObjectOfType<NetworkRelay>();
        
        Debug.Log($"[Bot] References found - GameManager: {gameManager != null}, DeckController: {deckController != null}, NetworkRelay: {networkRelay != null}");
        
        // Find PlayerHand3 transform
        playerHand3 = GameObject.Find("PlayerHand3")?.GetComponent<Transform>();
        
        if (playerHand3 == null)
        {
            Debug.LogError("[Bot] Could not find PlayerHand3 transform!");
        }
        else
        {
            Debug.Log($"[Bot] PlayerHand3 found with {playerHand3.childCount} cards");
        }
        
        Debug.Log("[Bot] Bot system initialized");
        
        // Auto-setup when created by GameManager
        SetupBotForGameManager();
    }
    
    /// <summary>
    /// Sets up the bot when created by GameManager
    /// </summary>
    private void SetupBotForGameManager()
    {
        Debug.Log("[Bot] Setting up bot for GameManager creation");
        
        // Ensure bot mode is enabled
        isBotModeEnabled = true;
        
        // Set bot player number to 2 (opponent)
        botPlayerNumber = 1;
        
        Debug.Log($"[Bot] Bot setup complete - Mode: {isBotModeEnabled}, Player: {botPlayerNumber}");
    }
    
    // Update method removed - bot is now purely event-driven via UpdateCurrentPlayer
    
    /// <summary>
    /// Check if bot should be activated (called once when OnTurnChanged is first triggered)
    /// </summary>
    private void CheckAndActivateBot()
    {
        if (!isBotModeEnabled)
        {
            Debug.Log("[Bot] Bot mode disabled, not activating");
            return;
        }
        
        // Only activate in 1v1 games and when we're the host
        bool isServer = IsServer();
        bool isTwoPlayer = deckController != null && deckController.playerCount == 2;
        bool isHost = deckController != null && deckController.thisPlayerNumber == 0;
        bool shouldActivate = isServer && isTwoPlayer && isHost;
        
        Debug.Log($"[Bot] Activation check - IsServer: {isServer}, PlayerCount: {deckController?.playerCount ?? -1}, ThisPlayer: {deckController?.thisPlayerNumber ?? -1}, ShouldActivate: {shouldActivate}");
        
        if (shouldActivate)
        {
            Debug.Log("[Bot] Conditions met, activating bot");
            ActivateBot();
        }
        else
        {
            Debug.Log("[Bot] Conditions not met, bot will not activate");
        }
    }
    
    /// <summary>
    /// Activate the bot for the current game
    /// </summary>
    private void ActivateBot()
    {
        botIsActive = true;
        Debug.Log($"[Bot] Bot activated! Bot plays as player {botPlayerNumber} (opponent)");
        Debug.Log($"[Bot] Host is player {deckController.thisPlayerNumber}, bot will use PlayerHand3 for opponent");
    }
    
    
    /// <summary>
    /// Check if it's currently the bot's turn
    /// </summary>
    private bool IsMyTurn()
    {
        return GameManager.currentPlayerNo == botPlayerNumber;
    }
    
    /// <summary>
    /// Called by GameManager when the current player changes
    /// </summary>
    public void OnTurnChanged(int newPlayerNumber)
    {
        Debug.Log($"[Bot] Turn changed to player {newPlayerNumber}");
        
        // Check if bot should be activated (only once when first called)
        if (!botIsActive)
        {
            CheckAndActivateBot();
        }
        
        Debug.Log($"[Bot] Bot active: {botIsActive}, Bot player number: {botPlayerNumber}, Move in progress: {isBotMoveInProgress}");
        
        if (botIsActive && newPlayerNumber == botPlayerNumber && !isBotMoveInProgress)
        {
            Debug.Log("[Bot] It's bot's turn! Scheduling move");
            ScheduleBotMove();
        }
        else
        {
            Debug.Log($"[Bot] Not bot's turn - Active: {botIsActive}, NewPlayer: {newPlayerNumber}, BotPlayer: {botPlayerNumber}, MoveInProgress: {isBotMoveInProgress}");
        }
    }
    
    /// <summary>
    /// Check if we're running on the server/host
    /// </summary>
    private bool IsServer()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
    }
    
    /// <summary>
    /// Schedule a bot move after a delay
    /// </summary>
    private void ScheduleBotMove()
    {
        // Determine appropriate delay based on whether bot just finished waiting for dealing
        float delayToUse = justFinishedWaitingForDeal ? botMoveDelayAfterDeal : botMoveDelay;
        
        Debug.Log($"[Bot] ScheduleBotMove called, delay: {delayToUse} seconds (after deal: {justFinishedWaitingForDeal})");
        
        // Prevent multiple simultaneous moves
        if (isBotMoveInProgress)
        {
            Debug.Log("[Bot] Bot move already in progress, skipping");
            return;
        }
        
        if (botMoveCoroutine != null)
        {
            Debug.Log("[Bot] Stopping existing bot move coroutine");
            StopCoroutine(botMoveCoroutine);
        }
        
        // Set flag and start new move coroutine
        isBotMoveInProgress = true;
        botMoveCoroutine = StartCoroutine(ExecuteBotMoveCoroutine(delayToUse));
        Debug.Log("[Bot] Bot move coroutine started");
    }
    
    /// <summary>
    /// Coroutine that waits then executes bot move
    /// </summary>
    private IEnumerator ExecuteBotMoveCoroutine(float delay = -1f)
    {
        // Use provided delay or default to botMoveDelay
        float actualDelay = delay >= 0f ? delay : botMoveDelay;
        
        Debug.Log($"[Bot] Bot move coroutine started, waiting {actualDelay} seconds");
        yield return new WaitForSeconds(actualDelay);
        
        Debug.Log($"[Bot] Bot move delay finished, checking conditions");
        Debug.Log($"[Bot] Bot active: {botIsActive}, Is my turn: {IsMyTurn()}");
        
        if (botIsActive && IsMyTurn())
        {
            Debug.Log("[Bot] Conditions met, executing bot move");
            ExecuteBotMove();
        }
        else
        {
            Debug.Log("[Bot] Conditions not met, skipping bot move");
        }
        
        // Reset the flags when move is complete
        isBotMoveInProgress = false;
        justFinishedWaitingForDeal = false; // Reset the deal waiting flag
        botMoveCoroutine = null;
        Debug.Log("[Bot] Bot move coroutine finished, flags reset");
    }
    
    /// <summary>
    /// Execute a bot move by selecting a card from PlayerHand3 and calling OnCardsPlayed
    /// </summary>
    private void ExecuteBotMove()
    {
        botMoveCount++;
        Debug.Log($"[Bot] Executing bot move #{botMoveCount}");
        
        if (playerHand3 == null)
        {
            Debug.LogError("[Bot] PlayerHand3 is null!");
            return;
        }
        
        if (playerHand3.childCount <= 2)
        {
            Debug.LogWarning("[Bot] PlayerHand3 has no actual cards (only 2 or fewer children)");
            return;
        }
        
        Debug.Log($"[Bot] PlayerHand3 has {playerHand3.childCount} children");
        
        // Skip first 2 children (they're not cards) and get first actual card
        Transform firstCard = playerHand3.GetChild(2); // Skip first 2 children
        CardInteraction cardScript = firstCard.GetComponent<CardInteraction>();
        
        if (cardScript == null)
        {
            Debug.LogError("[Bot] Card doesn't have CardInteraction script");
            return;
        }
        
        Debug.Log($"[Bot] Bot selected card: {firstCard.name}");
        
        // Check if this card will capture before playing it
        bool willCapture = WillCardCapture(cardScript);
        Debug.Log($"[Bot] Card will capture: {willCapture}");
        
        // Instead of using PlayCardForBot (which triggers local play), 
        // directly send the move to server to avoid double execution
        Debug.Log("[Bot] Sending move directly to server to avoid double execution");
        SendBotMoveToServer(cardScript);
        
        Debug.Log($"[Bot] Bot move #{botMoveCount} completed");
    }
    
    /// <summary>
    /// Check if the selected card will capture any center cards
    /// This mimics the logic from CheckIfLegal to predict captures
    /// </summary>
    private bool WillCardCapture(CardInteraction cardScript)
    {
        if (cardScript == null || gameManager == null)
        {
            Debug.LogWarning("[Bot] Cannot check capture - missing references");
            return false;
        }
        
        // Get the card value
        if (!CardInteraction.cardLookup.ContainsKey(cardScript.uniqueCardInstanceID))
        {
            Debug.LogWarning($"[Bot] Card {cardScript.uniqueCardInstanceID} not found in cardLookup");
            return false;
        }
        
        int cardValue = CardInteraction.cardLookup[cardScript.uniqueCardInstanceID].GetCardID()[1];
        Debug.Log($"[Bot] Checking capture for card value: {cardValue}");
        
        // Calculate sumValue (value of top center card) - same logic as CheckIfLegal
        int sumValue = gameManager.centerCards.Count > 0 ? gameManager.centerCards.Values.Last()[1] : 0;
        Debug.Log($"[Bot] Center cards count: {gameManager.centerCards.Count}, sumValue: {sumValue}");
        
        // Check capture conditions (same as CheckIfLegal)
        if (cardValue == sumValue || (cardValue == 11 && sumValue != 0))
        {
            Debug.Log($"[Bot] Card value {cardValue} will capture (sumValue: {sumValue})");
            return true;
        }
        
        Debug.Log($"[Bot] Card value {cardValue} will NOT capture (sumValue: {sumValue})");
        return false;
    }
    
    /// <summary>
    /// Send bot move directly to server without local play to avoid double execution
    /// </summary>
    private void SendBotMoveToServer(CardInteraction cardScript)
    {
        if (cardScript == null || gameManager == null)
        {
            Debug.LogError("[Bot] Cannot send move - missing references");
            return;
        }
        
        // Get card data
        if (!CardInteraction.cardLookup.ContainsKey(cardScript.uniqueCardInstanceID))
        {
            Debug.LogError($"[Bot] Card {cardScript.uniqueCardInstanceID} not found in cardLookup");
            return;
        }
        
        int[] cardData = CardInteraction.cardLookup[cardScript.uniqueCardInstanceID].GetCardID();
        int cardValue = cardData[1];
        
        // Calculate sumValue (same as CheckIfLegal)
        int sumValue = gameManager.centerCards.Count > 0 ? gameManager.centerCards.Values.Last()[1] : 0;
        
        // Create serializable card for captured cards
        Dictionary<string, int[]> capturedCards = new Dictionary<string, int[]>();
        
        // If this card will capture, add the center cards to capturedCards
        if (cardValue == sumValue || (cardValue == 11 && sumValue != 0))
        {
            Debug.Log($"[Bot] Card will capture - adding center cards to capturedCards");
            foreach (var kvp in gameManager.centerCards)
            {
                capturedCards[kvp.Key] = kvp.Value;
                Debug.Log($"[Bot] Adding center card to capture: {kvp.Key} = [{kvp.Value[0]}, {kvp.Value[1]}]");
            }
        }
        else
        {
            Debug.Log($"[Bot] Card will not capture - capturedCards remains empty");
        }
        
        SerializableCard serializableCard = new SerializableCard(capturedCards);
        
        Debug.Log($"[Bot] Sending move to server: card={cardScript.uniqueCardInstanceID}, player={botPlayerNumber}, sumValue={sumValue}");
        
        // Send directly to server via NetworkRelay
        NetworkRelay networkRelay = FindObjectOfType<NetworkRelay>();
        if (networkRelay != null)
        {
            networkRelay.SendMoveToServerRPC(cardScript.uniqueCardInstanceID, serializableCard, botPlayerNumber, sumValue);
            Debug.Log("[Bot] Move sent to server successfully");
        }
        else
        {
            Debug.LogError("[Bot] NetworkRelay not found - cannot send move to server");
        }
    }
    
    
    // ===== PUBLIC INTERFACE =====
    
    /// <summary>
    /// Called by GameManager when bot finishes waiting for dealing
    /// This will make the bot use a longer delay for its next move
    /// </summary>
    public void SetJustFinishedWaitingForDeal(bool finished)
    {
        justFinishedWaitingForDeal = finished;
        Debug.Log($"[Bot] SetJustFinishedWaitingForDeal: {finished}");
    }
    
    /// <summary>
    /// Enable/disable bot mode
    /// </summary>
    [ContextMenu("Toggle Bot Mode")]
    public void ToggleBotMode()
    {
        isBotModeEnabled = !isBotModeEnabled;
        Debug.Log($"[BotPlayer] Bot mode {(isBotModeEnabled ? "ENABLED" : "DISABLED")}");
    }
    
    /// <summary>
    /// Force bot to make a move (for testing)
    /// </summary>
    [ContextMenu("Force Bot Move")]
    public void ForceBotMove()
    {
        if (botIsActive)
        {
            Debug.Log("[BotPlayer] Forcing bot move via context menu");
            ExecuteBotMove();
        }
        else
        {
            Debug.LogWarning("[BotPlayer] Bot is not active - cannot force move");
        }
    }
    
    /// <summary>
    /// Check bot status
    /// </summary>
    [ContextMenu("Check Bot Status")]
    public void CheckBotStatus()
    {
        Debug.Log($"[Bot] Bot Status:" +
                 $"\n- Mode Enabled: {isBotModeEnabled}" +
                 $"\n- Bot Active: {botIsActive}" +
                 $"\n- Is Server: {IsServer()}" +
                 $"\n- Player Count: {deckController?.playerCount ?? 0}" +
                 $"\n- Current Player: {GameManager.currentPlayerNo}" +
                 $"\n- This Player: {deckController?.thisPlayerNumber ?? -1}" +
                 $"\n- PlayerHand3 Found: {playerHand3 != null}" +
                 $"\n- PlayerHand3 Cards: {playerHand3?.childCount ?? 0}");
    }
    
    /// <summary>
    /// Test method to simulate a turn change
    /// </summary>
    [ContextMenu("Test Bot Turn")]
    public void TestBotTurn()
    {
        Debug.Log("[Bot] Testing bot turn manually");
        OnTurnChanged(1); // Simulate player 2's turn
    }
}
