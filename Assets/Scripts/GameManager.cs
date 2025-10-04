using System;

using System.Collections;

using System.Collections.Generic;

using Unity.VisualScripting;

using System.Linq;

using UnityEngine;

using Unity.Netcode;

using UnityEngine.UI;

using UnityEngine.UIElements;

using TMPro;

using UnityEngine.EventSystems;



public class GameManager : MonoBehaviour

{

    // Static logging system for collecting debug messages

    private static string debugLog = "";

    private static bool loggingEnabled = true;



    public static void AddToDebugLog(string message)

    {

        if (loggingEnabled)

        {

            debugLog += $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n";

        }

    }



    public static void AddToDebugLogError(string message)

    {

        if (loggingEnabled)

        {

            debugLog += $"[{DateTime.Now:HH:mm:ss.fff}] ERROR: {message}\n";

        }

    }



    public static void AddToDebugLogWarning(string message)

    {

        if (loggingEnabled)

        {

            debugLog += $"[{DateTime.Now:HH:mm:ss.fff}] WARNING: {message}\n";

        }

    }



    [ContextMenu("Print All Debug Logs")]

    public void PrintAllDebugLogs()

    {

        if (string.IsNullOrEmpty(debugLog))

        {

            Debug.Log("No debug logs to print. Use 'Reset Debug Logs' to start fresh logging.");

            return;

        }

        

        Debug.Log("=== DEBUG LOG START ===\n" + debugLog + "=== DEBUG LOG END ===");

    }



    [ContextMenu("Reset Debug Logs")]

    public void ResetDebugLogs()

    {

        debugLog = "";

        Debug.Log("Debug logs reset. New logs will be collected.");

    }



    [ContextMenu("Toggle Debug Logging")]

    public void ToggleDebugLogging()

    {

        loggingEnabled = !loggingEnabled;

        Debug.Log($"Debug logging {(loggingEnabled ? "enabled" : "disabled")}");

    }



    // ===== SYNC LOGGING (separate buffer) =====

    private static string syncLog = "";

    private static bool syncLoggingEnabled = true;



    public static void SyncLog(string message)

    {

        if (!syncLoggingEnabled) return;

        string line = $"[SYNC {DateTime.Now:HH:mm:ss.fff}] {message}";

        syncLog += line + "\n";

        Debug.Log(line);

    }



    public static void SyncLogWarning(string message)

    {

        if (!syncLoggingEnabled) return;

        string line = $"[SYNC {DateTime.Now:HH:mm:ss.fff}] WARNING: {message}";

        syncLog += line + "\n";

        Debug.LogWarning(line);

    }



    public static void SyncLogError(string message)

    {

        if (!syncLoggingEnabled) return;

        string line = $"[SYNC {DateTime.Now:HH:mm:ss.fff}] ERROR: {message}";

        syncLog += line + "\n";

        Debug.LogError(line);

    }



    [ContextMenu("Print All Sync Logs")]

    public void PrintAllSyncLogs()

    {

        if (string.IsNullOrEmpty(syncLog))

        {

            Debug.Log("No sync logs to print. Use 'Reset Sync Logs' to start fresh logging.");

            return;

        }

        Debug.Log("=== SYNC LOG START ===\n" + syncLog + "=== SYNC LOG END ===");

    }



    [ContextMenu("Reset Sync Logs")]

    public void ResetSyncLogs()

    {

        syncLog = "";

        Debug.Log("Sync logs reset. New sync logs will be collected.");

    }



    [ContextMenu("Toggle Sync Logging")]

    public void ToggleSyncLogging()

    {

        syncLoggingEnabled = !syncLoggingEnabled;

        Debug.Log($"[SYNC] Logging enabled: {syncLoggingEnabled}");

    }



    //Scripts

    [SerializeField ] private bool editorMode = true;

    public static GameManager LocalInstance { get; private set; }

    [SerializeField] private DeckController deckController;

    public NetworkRelay networkRelay;

    //Card Variables

    [SerializeField] public GameObject cardBack;

    public string currentSelectedHandCard;//Represents the card current player chose to play with.

    public Dictionary<string, int[]> centerCards = new Dictionary<string, int[]>();//List of cards in the center

    public List<GameObject> centerCardsObjects = new List<GameObject>();//List of the card objects in the center

    private List<CardInteraction> cardInteractionsScripts = new List<CardInteraction>();//Reference to the scripts of every card.

    public List<GameObject> cardObjectsToBeDiscarted = new List<GameObject>();

    public static int currentPlayerNo = 0;

    private List<string> centerCardIDList;

    public List<string> myCards;

    private GameObject winScreen;

    Text roundOverText;

    private float turnTimer = 0;

    private List<Text> pointTexts = new List<Text>();

    private CanvasGroup superPowerTextGroup;

    private TextMeshProUGUI superPowerText;

    private TextMeshProUGUI superPowerTextB;

    private CanvasGroup pistiTextGroup;

    private TextMeshProUGUI pistiText;

    private TextMeshProUGUI pistiTextB;

    public Sprite cardBackSprite;

    [SerializeField] public GameObject cardIndicator;

    [SerializeField] private List<Transform> playerHandTransforms;

    [SerializeField] private List<Transform> playerPoolTransforms;

    [SerializeField] private List<Transform> playerPiştiPoolTransforms;

    [SerializeField] public Transform centerTransform;

    bool alreadySubbed = false;

    public bool movePlayedLocally = false;

    public bool isProcessingCapture = false;

    private bool hasAlreadySentRPC = false;

    private GameObject waitingScreen;

    private GameObject mainScreen;

    private GameObject explosionObject;

    private Animator explosionAnimator;

    private GameObject bombObject;

    private Animator bombAnimator;

    [SerializeField] private GameObject yandimAnamEffectPrefab;

    [SerializeField] private GameObject yandimAnamSpritePrefab;

    public List<GameObject> kapkacCardsToBeReset = new List<GameObject>();

    // Track cards that have been changed by powers (for save/load persistence)
    private Dictionary<string, string> cardPowerEffects = new Dictionary<string, string>();

    [SerializeField] public GameObject kapkacEffectPrefab; // Prefab with your PNG as a SpriteRenderers

    [SerializeField] private GameObject kapkacAnimEffectPrefab; // The animation prefab for Kapkaç

    [SerializeField] public GameObject oynayamazsinBlockPrefab;

    public GameObject oynayamazsinBlockInstance;

    public bool isKapkacPending = false;

    public bool isYandimAnamPending = false;
    
    // ENHANCED RECONNECTION: Sync mode and move buffering
    private bool isSyncMode = false;
    private MoveBuffer moveBuffer = new MoveBuffer();
    private List<GameMove> pendingMovesToApply = new List<GameMove>();
    
    // RECONNECTION TRACKING: Track if we're currently reconnecting
    private bool isReconnecting = false;







    public void ResetForNewRound()

    {

        //AddToDebugLog("[GameManager] ResetForNewRound called");

        //AddToDebugLog($"[GameManager] currentSelectedHandCard before reset: {currentSelectedHandCard}");

        //AddToDebugLog($"[GameManager] CardInteraction.currentlySelectedCard before reset: {CardInteraction.currentlySelectedCard?.gameObject.name}");

        

        // CRITICAL: Don't try to reset destroyed cards - they'll be recreated fresh
        // Clear the list since all card objects have been destroyed
        if (cardInteractionsScripts != null) cardInteractionsScripts.Clear();

        currentSelectedHandCard = null;

        centerCards.Clear();

        centerCardsObjects.Clear();

        cardObjectsToBeDiscarted.Clear();

        if (centerCardIDList != null) centerCardIDList.Clear();

        if (myCards != null) myCards.Clear();

        turnTimer = 0f;

        //AddToDebugLog($"[GameManager] ResetForNewRound: Setting movePlayedLocally to false");

        movePlayedLocally = false;

        hasAlreadySentRPC = false;

        isProcessingCapture = false;

        // Set currentPlayerNo to the



        //AddToDebugLog($"[GameManager] currentSelectedHandCard after reset: {currentSelectedHandCard}");

        //AddToDebugLog($"[GameManager] CardInteraction.currentlySelectedCard after reset: {CardInteraction.currentlySelectedCard?.gameObject.name}");

        if (roundCount != 0)

        {

            foreach (var cardScript in cardInteractionsScripts)

            {

                if (cardScript != null && cardScript.gameObject != null)

                {

                    string[] tagParts = cardScript.gameObject.tag.Split('_');

                    if (tagParts.Length == 2 && int.TryParse(tagParts[1], out int value) && value == 11)

                    {

                        var indicator = cardScript.transform.Find("CardIndicator(Clone)");

                        if (indicator != null)

                            indicator.gameObject.SetActive(false);

                    }

                }

            }

        }

        // NOTE: Do NOT clear PlayerPrefs on new round - this is still the same game/lobby
        // Player numbers and join codes should persist across rounds within the same game
        // Only clear PlayerPrefs when starting a completely new game (different lobby/relay)

    }

    /// <summary>
    /// Reset GameManager for a completely NEW GAME (not a new round).
    /// This resets all card values and power effects to original state.
    /// </summary>
    public void ResetForNewGame()
    {
        Debug.LogWarning("[GameManager] ===== RESETTING FOR NEW GAME =====");
        
        // CRITICAL: Destroy all card GameObjects first for clean slate
        if (deckController != null)
        {
            deckController.DestroyAllCards();
        }
        
        // Clear all tracking lists (cards are already destroyed)
        if (cardInteractionsScripts != null) cardInteractionsScripts.Clear();
        if (kapkacCardsToBeReset != null) kapkacCardsToBeReset.Clear();
        if (cardPowerEffects != null) cardPowerEffects.Clear();
        
        // CRITICAL: Reset event subscription flag so new cards can subscribe to events!
        alreadySubbed = false;
        
        // Reset essential game state only
        currentSelectedHandCard = null;
        if (centerCards != null) centerCards.Clear();
        if (centerCardsObjects != null) centerCardsObjects.Clear();
        if (cardObjectsToBeDiscarted != null) cardObjectsToBeDiscarted.Clear();
        if (centerCardIDList != null) centerCardIDList.Clear();
        if (myCards != null) myCards.Clear();
        turnTimer = 0f;
        movePlayedLocally = false;
        hasAlreadySentRPC = false;
        isProcessingCapture = false;
        
        // Reset power states
        verZehriActive = false;
        kutsalDesteActive = false;
        valeArarActive = false;
        isKopyalaActive = false;
        isSunuDegisTokusActive = false;
        isSunuDegisBunuTokusActive = false;
        kopyalaSourceCard = null;
        sunuDegisTokusFirstCard = null;
        sunuDegisBunuTokusMyHandSnapshot = null;
        sunuDegisBunuTokusSwapIndex = 0;
        isKapkacPending = false;
        isYandimAnamPending = false;
        
        Debug.LogWarning("[GameManager] GameManager reset complete - All cards destroyed, ready for NEW GAME");
    }



    void Awake()

    {

        Debug.Log($"GameManager Awake called on {gameObject.GetInstanceID()}");

        if (LocalInstance != null && LocalInstance != this)

        {

            Debug.LogWarning($"Destroying duplicate GameManager {gameObject.GetInstanceID()}");

            Destroy(this.gameObject);

            return;

        }

        LocalInstance = this;

        DontDestroyOnLoad(this.gameObject);

        Debug.Log($"GameManager LocalInstance set to {gameObject.GetInstanceID()}");

    }



    void Start()

    {

        Debug.Log("GameManager started");

        InitialGameManagerSetUp();//Identifies and assigns necessary variables and calls other functions



        SuperPowerSpawner.LocalInstance.InitializeSuperPowers(); // Initialize super powers

        
        // Instantiate DebugChainPrinter if it doesn't exist
        if (DebugChainPrinter.LocalInstance == null)
        {
            GameObject debugPrinterObj = new GameObject("DebugChainPrinter");
            debugPrinterObj.AddComponent<DebugChainPrinter>();
            DontDestroyOnLoad(debugPrinterObj);
            Debug.Log("[GameManager] Created DebugChainPrinter instance");
        }
    }



    public void NotifyConnection()

    {
        Debug.Log($"[GameManager] ===== ÖNEMLİ: NOTIFY CONNECTION CALLED =====\n" +
                 $"LocalClientId: {NetworkManager.Singleton.LocalClientId}\n" +
                 $"IsHost: {NetworkManager.Singleton.IsHost}\n" +
                 $"IsServer: {NetworkManager.Singleton.IsServer}\n" +
                 $"IsConnectedClient: {NetworkManager.Singleton.IsConnectedClient}\n" +
                 $"Calling NotifyCientConnectedServerRPC to notify server of client connection");

        networkRelay.NotifyCientConnectedServerRPC(NetworkManager.Singleton.LocalClientId);// Tells the server that a client is started
    }

    /// <summary>
    /// Called when a reconnected client has successfully applied game state and is ready to play
    /// </summary>
    public void OnReconnectionGameStateApplied()
    {
        Debug.Log("[GameManager] Reconnected client game state applied successfully");
        
        // Close any waiting screens that might still be open
        if (waitingScreen != null && waitingScreen.activeSelf)
        {
            waitingScreen.SetActive(false);
            Debug.Log("[GameManager] Closed waiting screen after reconnection");
        }
        
        // Ensure main screen is hidden and game is visible
        if (mainScreen != null && mainScreen.activeSelf)
        {
            mainScreen.SetActive(false);
        }
        
        Debug.Log("[GameManager] Reconnected client is now ready to play");
    }



    void OnDestroy()

    {

        if (LocalInstance == this)

        {

            Debug.LogWarning($"GameManager {gameObject.GetInstanceID()} destroyed, clearing LocalInstance");

            LocalInstance = null;

        }

    }





    private CardInteraction tempCard;

    void Update()

    {

        if (Input.GetKeyDown(KeyCode.W)) PlayAutomatically(3);

        if (Input.GetKeyDown(KeyCode.A)) PlayAutomatically(4);

        if (Input.GetKeyDown(KeyCode.S)) PlayAutomatically(1);

        if (Input.GetKeyDown(KeyCode.D)) PlayAutomatically(2);



        // ...existing code...

        // Handle touch input (mobile)

        /*if (Input.touchCount > 0)

        {

            Touch touch = Input.GetTouch(0);

            Vector3 touchPosition = touch.position;



            Ray ray = Camera.main.ScreenPointToRay(touchPosition);

            if (Physics.Raycast(ray, out RaycastHit hit))

            {

                CardInteraction card = hit.collider.GetComponent<CardInteraction>();

                if (card != null)

                {

                    switch (touch.phase)

                    {

                        case TouchPhase.Began:

                            card.OnCardTouched(touchPosition);

                            break;

                        case TouchPhase.Moved:

                            if (CardInteraction.currentlySelectedCard == card)

                            {

                                card.OnTouchDrag(touchPosition); // If you have this method/event

                            }

                            break;

                        case TouchPhase.Ended:

                        case TouchPhase.Canceled:

                            if (CardInteraction.currentlySelectedCard == card)

                            {

                                card.OnTouchUp(); // If you have this method/event

                                CardInteraction.currentlySelectedCard = null; // Reset the currently selected card

                            }

                            break;

                    }

                }

            }

        }*/

        



        if (Input.touchCount > 0)

        {

            Touch touch = Input.GetTouch(0);

            AddToDebugLog($"[GameManager] Touch detected - Phase: {touch.phase}, Position: {touch.position}");

            

            if (touch.phase == TouchPhase.Began)

            {

                //if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))

                    //return; // Don't raycast or close anything if over UI

            }

            Vector3 touchPosition = touch.position;

            Ray ray = Camera.main.ScreenPointToRay(touchPosition);

            if (Physics.Raycast(ray, out RaycastHit hit))

            {

                CardInteraction card = hit.collider.GetComponent<CardInteraction>();
                AddToDebugLog($"[GameManager] Found CardInteraction: {card?.gameObject?.name}, parent: {card?.gameObject?.transform?.parent?.name}");

                if (touch.phase == TouchPhase.Began && card != null)

                {

                    AddToDebugLog($"[GameManager] Touch OnCardTouched called for card: {card.gameObject.name}, parent: {card.gameObject.transform.parent?.name}");

                    card.OnCardTouched(touchPosition);

                    tempCard = card; // Store the card for later use

                }

                else if (card == null && touch.phase == TouchPhase.Began)

                {

                    AddToDebugLog($"[GameManager] Touch - No card found, calling TryStopAllShowcases");

                    //deckController.TryStopAllShowcases();

                }

                else if (touch.phase == TouchPhase.Moved && tempCard != null)

                {

                    if (CardInteraction.currentlySelectedCard == tempCard)

                    {

                        AddToDebugLog($"[GameManager] Touch OnTouchDrag called for card: {tempCard.gameObject.name}");

                        tempCard.OnTouchDrag(touchPosition);

                    }

                }

            }

            // Handle touch up/cancel outside the raycast hit

            if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) && tempCard != null)

            {

                if (CardInteraction.currentlySelectedCard == tempCard)

                {

                    AddToDebugLog($"[GameManager] Touch OnTouchUp called for card: {tempCard.gameObject.name}");

                    tempCard.OnTouchUp();

                }

                tempCard = null;

            }

        }

        

        // Mouse input handling removed - touch system handles both mobile and mouse input

        // This prevents double processing of the same interaction

    }



    void FixedUpdate()

    {

        if (turnTimer > 15)

        {

            turnTimer -= Time.deltaTime;

        }

        else if (turnTimer > 0)

        {

            turnTimer -= Time.deltaTime;

            //turnTimerText.text = Mathf.RoundToInt(turnTimer).ToString();

        }

    }



    private int roundCount = 0;

    //Gets message from the server to start the deck and the cards

    [ContextMenu("Initialize Card Prefabs")]

    public IEnumerator InitializeCardPrefabs(bool isReconnection = false)

    {
        Debug.Log($"[GameManager] ===== ÖNEMLİ: INITIALIZE CARD PREFABS START =====\n" +
                 $"IsReconnection: {isReconnection}\n" +
                 $"DeckController: {(deckController != null ? "FOUND" : "NULL")}\n" +
                 $"WaitingScreen: {(waitingScreen != null ? "FOUND" : "NULL")}\n" +
                 $"WinScreen: {(winScreen != null ? "FOUND" : "NULL")}\n" +
                 $"MainScreen: {(mainScreen != null ? "FOUND" : "NULL")}");

        if (!isReconnection)
        {
            Debug.Log("[GameManager] InitializeCardPrefabs called for new game - resetting game state");
            ResetForNewRound();
            roundCount++;
            isReconnecting = false;
        }
        else
        {
            // SAFETY CHECK: Only process reconnection logic if we're not the host
            if (NetworkManager.Singleton.IsHost)
            {
                Debug.LogError("[GameManager] SAFETY CHECK: Host received reconnection InitializeCardPrefabs - this should not happen! Treating as new game instead.");
                ResetForNewRound();
                roundCount++;
                isReconnecting = false;
            }
            else
            {
                Debug.Log("[GameManager] InitializeCardPrefabs called for reconnection - following normal StartGame flow");
                isReconnecting = true;
            }
            
            // RECONNECTION FIX: Do NOT clear cardLookup - let it be populated normally
            // The card IDs will be deterministic and match the server
            
            // CRITICAL: Reset event subscription flag for reconnection
            Debug.Log("[GameManager] Resetting alreadySubbed flag for reconnection");
            alreadySubbed = false;
            
            // CRITICAL FIX: Restore player number from PlayerPrefs for reconnection
            RestorePlayerNumberFromPrefs();
        }

        Debug.Log("[GameManager] Checking and managing UI screens...");
        if (waitingScreen.activeSelf) 
        {
            Debug.Log("[GameManager] Closing waiting screen");
            waitingScreen.SetActive(false);
        }

        Debug.Log("[GameManager] Starting DeckController.DeckStart() coroutine...");
        yield return StartCoroutine(deckController.DeckStart());
        Debug.Log("[GameManager] DeckController.DeckStart() coroutine completed");
        
        // RECONNECTION: Let the normal flow handle everything
        // The card IDs will be deterministic and match the server
        // The existing desync detection will handle synchronization

        if (winScreen.activeSelf) 
        {
            Debug.Log("[GameManager] Closing win screen");
            winScreen.SetActive(false);
        }

        if (mainScreen.activeSelf) 
        {
            Debug.Log("[GameManager] Closing main screen");
            mainScreen.SetActive(false);
        }

        Debug.Log($"[GameManager] ===== ÖNEMLİ: INITIALIZE CARD PREFABS COMPLETED =====\n" +
                 $"IsReconnection: {isReconnection}\n" +
                 $"IsReconnecting flag: {isReconnecting}");

    }



    public void DeckReady()

    {
        Debug.LogError("[Visual Sync] ===== DECK READY CALLED =====");
        Debug.Log($"[GameManager] ===== ÖNEMLİ: DECK READY CALLED =====\n" +
                 $"IsReconnecting: {isReconnecting}\n" +
                 $"NetworkRelay: {(networkRelay != null ? "FOUND" : "NULL")}\n" +
                 $"IsConnectedClient: {NetworkManager.Singleton.IsConnectedClient}\n" +
                 $"IsHost: {NetworkManager.Singleton.IsHost}\n" +
                 $"IsServer: {NetworkManager.Singleton.IsServer}\n" +
                 $"LocalClientId: {NetworkManager.Singleton.LocalClientId}");

        Debug.Log("Deck is ready, notifying server.");

        if (isReconnecting)
        {
            // SAFETY CHECK: Only process reconnection logic if we're not the host
            if (NetworkManager.Singleton.IsHost)
            {
                Debug.LogError("[GameManager] SAFETY CHECK: Host is in reconnecting state - this should not happen! Using normal flow instead.");
                networkRelay.DeckReadyServerRPC();
            }
            else
            {
                // For reconnecting clients, use the specific RPC that passes the client ID explicitly
                Debug.LogError("[Visual Sync] ===== CALLING RECONNECTING CLIENT CARDS READY SERVER RPC =====");
                Debug.Log($"[GameManager] Calling networkRelay.ReconnectingClientCardsReadyServerRPC({NetworkManager.Singleton.LocalClientId})...");
                networkRelay.ReconnectingClientCardsReadyServerRPC(NetworkManager.Singleton.LocalClientId);
                Debug.LogError("[Visual Sync] ===== RECONNECTING CLIENT CARDS READY SERVER RPC CALL COMPLETED =====");
                Debug.Log($"[GameManager] networkRelay.ReconnectingClientCardsReadyServerRPC() call completed");
            }
        }
        else
        {
            // For normal game flow, use the regular RPC
            Debug.Log($"[GameManager] Calling networkRelay.DeckReadyServerRPC()...");
            networkRelay.DeckReadyServerRPC();
            Debug.Log($"[GameManager] networkRelay.DeckReadyServerRPC() call completed");
        }
        
        // Note: For reconnection, game state sync is now handled directly in the RPC
        // No need for desync detection since we call ApplyGameStateClientRPC() directly

    }



    //Gets all the scripts of the cards from the deckController

    public void GetCardInteractionScripts(List<CardInteraction> cardScripts)

    {

        cardInteractionsScripts = cardScripts;

        SubscribeToEvents();

    }



    //Subscribes to each of the cards events

    private void SubscribeToEvents()

    {

        // Iterate through the list of CardInteraction scripts

        if (alreadySubbed) return;

        foreach (var cardInteraction in cardInteractionsScripts)

        {

            cardInteraction.OnCardSelected += CardSelected;

            cardInteraction.OnCardsPlayed += CardsPlayed;

            alreadySubbed = true;

        }

    }



    //Gets message from the server to start dealing cards to players   

    public void CardPrefabsToPlayers(int playerCount, SerializableDictionary serializableDictionary)

    {
        //Converts serializablelist to a normal dictionary
        Dictionary<int, List<string>> playerHands = serializableDictionary.ToDictionary();

        // RECONNECTION FIX: Skip dealing during reconnection - let ApplyGameState handle it
        if (isReconnecting)
        {
            Debug.Log("[GameManager] CardPrefabsToPlayers called during reconnection - skipping deal, will be handled by ApplyGameState");
            return;
        }

        //Informs the deckController to deal the players' cards
        StartCoroutine(DelayedDealPlayers(playerCount, playerHands));
    }



    private IEnumerator DelayedDealPlayers(int playerCount, Dictionary<int, List<string>> playerHands)
    {
        deckController.DealPlayers(playerCount, playerHands);

        yield return new WaitForSeconds(0f);

        // Refresh Vale Arar indicators on new cards if the power is active
        if (valeArarActive)
        {
            RefreshValeArarIndicators();
            Debug.Log("[GameManager] Vale Arar indicators refreshed on new cards");
        }

        SuperPowerSpawner.LocalInstance.ReadyToSpawnSuperPowers(1);
        
        // Notify bot that dealing is complete
        NotifyBotDealingComplete();
    }
    
    /// <summary>
    /// Notifies the bot that dealing is complete
    /// </summary>
    private void NotifyBotDealingComplete()
    {
        AddToDebugLog($"[GameManager] NotifyBotDealingComplete called");
        
        // Find BotPlayer instance and notify it
        BotPlayer botPlayer = FindObjectOfType<BotPlayer>();
        if (botPlayer != null)
        {
            AddToDebugLog($"[GameManager] Notifying BotPlayer that dealing is complete");
            botPlayer.OnDealingComplete();
        }
        else
        {
            AddToDebugLog($"[GameManager] No BotPlayer found - no notification needed");
        }
    }



    //Gets message from the server to start dealing cards to center

    public void CardPrefabsToCenter(SerializableCard serializableCard)
    {
        Debug.Log("CardPrefabsToCenter called with serializableCard: " + serializableCard.ToString());
        //Converts serializablelist to a normal list
        List<string> centerCardIDs = serializableCard.ToDictionary().Keys.ToList();

        // RECONNECTION FIX: Skip dealing during reconnection - let ApplyGameState handle it
        if (isReconnecting)
        {
            Debug.Log("[GameManager] CardPrefabsToCenter called during reconnection - skipping deal, will be handled by ApplyGameState");
            return;
        }

        //Informs the deckController to deal the center cards
        if (deckController) StartCoroutine(deckController.DealCenter(centerCardIDs));
        else Debug.LogError("DeckController is not assigned in GameManager.");  

        // Refresh Vale Arar indicators on new center cards if the power is active
        if (valeArarActive)
        {
            // Use a coroutine to wait for cards to be spawned before refreshing
            StartCoroutine(RefreshValeArarAfterCenterDeal());
        }
        
        //turnTimerText.text = "";
    }
    
    private IEnumerator RefreshValeArarAfterCenterDeal()
    {
        // Wait a bit for center cards to be spawned
        yield return new WaitForSeconds(0.5f);
        RefreshValeArarIndicators();
        Debug.Log("[GameManager] Vale Arar indicators refreshed on new center cards");
    }



    //Called when a card is selected in the player's hand

    private void CardSelected(string cardID)

    {

        AddToDebugLog($"[GameManager] CardSelected called with cardID: {cardID}");

        AddToDebugLog($"[GameManager] currentSelectedHandCard before setting: {currentSelectedHandCard}");

        

        currentSelectedHandCard = cardID;

        

        AddToDebugLog($"[GameManager] currentSelectedHandCard after setting: {currentSelectedHandCard}");



        if (isSunuDegisTokusActive)
        {
            AddToDebugLog($"[GameManager] ŞunuDeğişTokuş is active, card selection unrestricted");

            // If first card not selected, set it
            if (string.IsNullOrEmpty(sunuDegisTokusFirstCard))
            {
                sunuDegisTokusFirstCard = cardID;
                AddToDebugLog($"[GameManager] ŞunuDeğişTokuş: First card selected: {cardID}");
                return;
            }

            // If second card is same as first, ignore
            if (sunuDegisTokusFirstCard == cardID)
            {
                AddToDebugLogWarning("ŞunuDeğişTokuş: Cannot select the same card twice.");
                return;
            }

            // 1. Call PowerActivated for local effects
            if (DeckController.LocalInstance != null)
            {
                Debug.Log("[GameManager] Calling PowerActivated() for Şunu Değiş Tokuş since both cards are now selected");
                var tempPower = ScriptableObject.CreateInstance<SunuDegisTokus>();
                tempPower.PowerActivated();
                DestroyImmediate(tempPower);
            }

            // 2. Perform local swap and showcase exit immediately for power player
            int myPlayerNo = deckController.thisPlayerNumber;
            int otherPlayerNo = -1;
            // Try to find owner of second card, but allow swap regardless
            if (Server.Singleton != null)
            {
                otherPlayerNo = Server.Singleton.FindOwnerOfCard(cardID);
            }
            else if (NetworkRelay.Instance != null && NetworkRelay.Instance.server != null)
            {
                otherPlayerNo = NetworkRelay.Instance.server.FindOwnerOfCard(cardID);
            }
            if (otherPlayerNo == -1)
            {
                Debug.LogWarning($"[GameManager] Could not find owner for cardID {cardID}, proceeding with swap anyway.");
            }
            Debug.Log($"[GameManager] Performing local swap for Şunu Değiş Tokuş: {myPlayerNo} <-> {otherPlayerNo}, {sunuDegisTokusFirstCard} <-> {cardID}");
            StartCoroutine(OnSunuDegisTokusSynced(myPlayerNo, otherPlayerNo, sunuDegisTokusFirstCard, cardID));

            // 3. Send swap request to server for other clients
            networkRelay.UseSunuDegisTokusServerRPC(myPlayerNo, sunuDegisTokusFirstCard, cardID);

            // 4. Stop hand showcasing
            Debug.Log("[Showcase] GameManager: Stopping Şunu Değiş Tokuş dual selection showcase");
            if (SuperPowerSpawner.LocalInstance != null)
            {
                SuperPowerSpawner.LocalInstance.StopHandShowcase();
                Debug.Log("[Showcase] GameManager: Called StopHandShowcase for Şunu Değiş Tokuş");
            }
            else
            {
                Debug.LogError("[Showcase] GameManager: ERROR - SuperPowerSpawner.LocalInstance is null!");
            }

            isSunuDegisTokusActive = false;
            sunuDegisTokusFirstCard = null;
            return;
        }



        // ŞunuDeğişBunuTokuş logic

        if (isSunuDegisBunuTokusActive)

        {

            AddToDebugLog($"[GameManager] ŞunuDeğişBunuTokuş is active, checking if card is from own hand");

            // Only allow selecting a card from another player's hand

            if (myCards.Contains(cardID))

            {

                AddToDebugLogWarning("You must select a card from another player's hand for ŞunuDeğişBunuTokuş.");

                return;

            }

            // Get my hand snapshot and current swap index

            if (sunuDegisBunuTokusMyHandSnapshot == null || sunuDegisBunuTokusSwapIndex >= sunuDegisBunuTokusMyHandSnapshot.Count)

            {

            AddToDebugLogWarning("No more cards to swap for ŞunuDeğişBunuTokuş.");

            // Stop hand showcasing
            Debug.Log("[Showcase] GameManager: Stopping Şunu Değiş Bunu Tokuş multi-swap showcase - no more cards");
            if (SuperPowerSpawner.LocalInstance != null)
            {
                SuperPowerSpawner.LocalInstance.StopHandShowcase();
                Debug.Log("[Showcase] GameManager: Called StopHandShowcase for Şunu Değiş Bunu Tokuş - no more cards");
            }
            else
            {
                Debug.LogError("[Showcase] GameManager: ERROR - SuperPowerSpawner.LocalInstance is null!");
            }

            isSunuDegisBunuTokusActive = false;

            return;

            }

            string myHandCardID = sunuDegisBunuTokusMyHandSnapshot[sunuDegisBunuTokusSwapIndex];

            // Send swap request to server

            bool flag = false;

            if (sunuDegisBunuTokusSwapIndex >= sunuDegisBunuTokusMyHandSnapshot.Count -1)

            {

                flag = true;

            }

            networkRelay.UseSunuDegisBunuTokusServerRPC(deckController.thisPlayerNumber, myHandCardID, cardID, sunuDegisBunuTokusSwapIndex,flag);

            sunuDegisBunuTokusSwapIndex++;

            if (sunuDegisBunuTokusSwapIndex >= sunuDegisBunuTokusMyHandSnapshot.Count)
            {
                // NOW call PowerActivated() since all swaps are complete
                if (DeckController.LocalInstance != null)
                {
                    Debug.Log("[GameManager] Calling PowerActivated() for Şunu Değiş Bunu Tokuş since all swaps are complete");
                    // Create a temporary power instance to call PowerActivated
                    var tempPower = ScriptableObject.CreateInstance<SunuDegisBunuTokus>();
                    tempPower.PowerActivated();
                    DestroyImmediate(tempPower);
                }

                // Stop hand showcasing
                Debug.Log("[Showcase] GameManager: Stopping Şunu Değiş Bunu Tokuş multi-swap showcase");
                if (SuperPowerSpawner.LocalInstance != null)
                {
                    SuperPowerSpawner.LocalInstance.StopHandShowcase();
                    Debug.Log("[Showcase] GameManager: Called StopHandShowcase for Şunu Değiş Bunu Tokuş");
                }
                else
                {
                    Debug.LogError("[Showcase] GameManager: ERROR - SuperPowerSpawner.LocalInstance is null!");
                }

                isSunuDegisBunuTokusActive = false;
                sunuDegisBunuTokusMyHandSnapshot = null;
                sunuDegisBunuTokusSwapIndex = 0;
                AddToDebugLog("ŞunuDeğişBunuTokuş: All swaps done.");
            }

            else

            {

                AddToDebugLog($"ŞunuDeğişBunuTokuş: Select card {sunuDegisBunuTokusSwapIndex + 1} to swap.");

            }

            return;

        }



        AddToDebugLog($"[GameManager] CardSelected completed, calling SuperPowerSpawner.SetActiveActivateButtonTrue()");

        SuperPowerSpawner.LocalInstance.SetActiveActivateButtonTrue();

    }



    //Called when the player tries to play the selected card with one or two center cards

    private void CardsPlayed(string cardID, GameObject cardObject, int playerNumber)

    {

        AddToDebugLog($"[GameManager] CardsPlayed called with cardID: {cardID}, playerNumber: {playerNumber}");

        AddToDebugLog($"[GameManager] currentSelectedHandCard at start of CardsPlayed: {currentSelectedHandCard}");

        AddToDebugLog($"[GameManager] CardInteraction.currentlySelectedCard at start of CardsPlayed: {CardInteraction.currentlySelectedCard?.gameObject.name}");

        

        // Check if player can play on their turn

        if(!editorMode)

        {

            if (!CanPlayerPlay(cardObject))

            {

                AddToDebugLogWarning($"[GameManager] Player cannot play - not their turn or restrictions active");

                AddToDebugLogWarning($"[GameManager] Current turn: Player {currentPlayerNo}, This player: {deckController.thisPlayerNumber}");

                OnPlayerTriedToPlayOutOfTurn();

                return;

            }

        }



        AddToDebugLog($"[GameManager] Player can play - proceeding with move validation");

        

        CheckIfLegal(playerNumber);

    }



    //Checks if played move is legal before sending it to the server

    public void CheckIfLegal(int playerNumber)

    {

        AddToDebugLog($"[GameManager] CheckIfLegal called with playerNumber: {playerNumber}");

        AddToDebugLog($"[GameManager] hasAlreadySentRPC: {hasAlreadySentRPC}");

        
        if (hasAlreadySentRPC)

        {

            AddToDebugLog($"[GameManager] RPC already sent, skipping duplicate call");

            return;

        }

        

        AddToDebugLog($"[GameManager] currentSelectedHandCard at start of CheckIfLegal: {currentSelectedHandCard}");

        AddToDebugLog($"[GameManager] CardInteraction.currentlySelectedCard at start of CheckIfLegal: {CardInteraction.currentlySelectedCard?.gameObject.name}");

        

        //if(currentSelectedHandCard[0] == 0 || currentSelectedHandCard[1]==0)return false;



        Dictionary<string, int[]> cardsToRemove = new Dictionary<string, int[]>();



        // Iterate over the selected center cards and add them to the removal list

        foreach (var cardToBeRemoved in centerCards)

        {

            cardsToRemove.Add(cardToBeRemoved.Key, cardToBeRemoved.Value);

        }



        SerializableCard serializableCard = new SerializableCard(centerCards);



        int sumValue = centerCards.Count > 0 ? centerCards.Last().Value[1] : 0;

        

        AddToDebugLog($"[GameManager] About to access CardInteraction.cardLookup[currentSelectedHandCard]");

        AddToDebugLog($"[GameManager] currentSelectedHandCard value: {currentSelectedHandCard}");

        AddToDebugLog($"[GameManager] CardInteraction.cardLookup contains key: {CardInteraction.cardLookup.ContainsKey(currentSelectedHandCard)}");

        

        if (currentSelectedHandCard == null)

        {

            AddToDebugLogError($"[GameManager] ERROR: currentSelectedHandCard is NULL!");

            return;

        }

        

        if (!CardInteraction.cardLookup.ContainsKey(currentSelectedHandCard))

        {

            AddToDebugLogError($"[GameManager] ERROR: CardInteraction.cardLookup does not contain key: {currentSelectedHandCard}");

            AddToDebugLogError($"[GameManager] Available keys in cardLookup: {string.Join(", ", CardInteraction.cardLookup.Keys)}");

            return;

        }

        

        int cardValue = CardInteraction.cardLookup[currentSelectedHandCard].GetCardID()[1];

        AddToDebugLog($"CardValue: {cardValue}, SumValue: {sumValue}");



        if (oynayamazsinActive)

        {

            AddToDebugLog($"[GameManager] oynayamazsinActive is true, playing card to center");

            movePlayedLocally = true;

            PlayCardToCenter(currentSelectedHandCard, CardInteraction.cardLookup[currentSelectedHandCard].GetCardID());

            

        }

        else if (cardValue == sumValue || (cardValue == 11 && sumValue != 0))

        {

            AddToDebugLog($"[GameManager] Card can capture, calling DiscardCapturedCards");

            AddToDebugLog($"[GameManager] currentSelectedHandCard before calling DiscardCapturedCards: {currentSelectedHandCard}");

            AddToDebugLog($"[GameManager] movePlayedLocally before calling DiscardCapturedCards: {movePlayedLocally}");

            isProcessingCapture = true;

            AddToDebugLog($"[GameManager] isProcessingCapture set to true");

            AddToDebugLog($"[GameManager] About to call DiscardCapturedCards with playedCard: {currentSelectedHandCard}");

            DiscardCapturedCards(currentSelectedHandCard, serializableCard, playerNumber);

            AddToDebugLog($"[GameManager] DiscardCapturedCards completed, currentSelectedHandCard: {currentSelectedHandCard}");

            AddToDebugLog($"[GameManager] movePlayedLocally after DiscardCapturedCards: {movePlayedLocally}");

            isProcessingCapture = false;

            AddToDebugLog($"[GameManager] isProcessingCapture set to false");

            

        }

        else

        {

            AddToDebugLog($"[GameManager] Card cannot capture, playing to center");

            movePlayedLocally = true;

            PlayCardToCenter(currentSelectedHandCard, CardInteraction.cardLookup[currentSelectedHandCard].GetCardID());

            

        }



        AddToDebugLog($"[GameManager] About to call SendMoveToServerRPC");

        AddToDebugLog($"[GameManager] currentSelectedHandCard before RPC call: {currentSelectedHandCard}");

        AddToDebugLog($"[GameManager] movePlayedLocally before RPC call: {movePlayedLocally}");

        AddToDebugLog($"[GameManager] serializableCard contains {serializableCard.ToDictionary().Count} cards");

        hasAlreadySentRPC = true;

        AddToDebugLog($"[GameManager] Set hasAlreadySentRPC to true");

        networkRelay.SendMoveToServerRPC(currentSelectedHandCard, serializableCard, playerNumber, sumValue);

        AddToDebugLog($"[GameManager] SendMoveToServerRPC completed");

        AddToDebugLog($"[GameManager] currentSelectedHandCard after RPC call: {currentSelectedHandCard}");

        myCards.Remove(currentSelectedHandCard);



        AddToDebugLog($"[GameManager] About to set CardInteraction.currentlySelectedCard to null");

        CardInteraction.currentlySelectedCard = null;

        AddToDebugLog($"[GameManager] About to call SetCurrentSelectedHandCardNull()");

        SetCurrentSelectedHandCardNull();

        AddToDebugLog($"[GameManager] About to call SuperPowerSpawner.CheckIfBackgroundPanelOpen()");

        SuperPowerSpawner.LocalInstance.CheckIfBackgroundPanelOpen();

        

        AddToDebugLog($"[GameManager] CheckIfLegal completed successfully");



    }



    private IEnumerator KapkacCourotine(string playedCard)

    {

        GameObject cardObj = CardInteraction.cardLookup[playedCard].gameObject;

        var cardInteraction = CardInteraction.cardLookup[playedCard];



        // --- Reset visuals before applying Kapkaç effect ---

        ResetVisuals(cardInteraction);



        // 1. Play animation on top of the card

        GameObject anim = null;

        if (kapkacAnimEffectPrefab != null)

        {

            anim = Instantiate(kapkacAnimEffectPrefab, cardObj.transform);

            anim.transform.localPosition = new Vector3(0, 0, -0.02f); // Above the sprite

            anim.SetActive(true);

        }



        // 3. Add the overlay sprite, fade in

        GameObject effect = null;

        if (kapkacEffectPrefab != null)

        {

            effect = Instantiate(kapkacEffectPrefab, cardObj.transform);

            effect.transform.localPosition = new Vector3(0, 0, -0.001f); // Below the animation

            kapkacCardsToBeReset.Add(cardObj);



            // Start fade-in

            SpriteRenderer sr = effect.GetComponent<SpriteRenderer>();

            if (sr != null)

            {

                Color c = sr.color;

                c.a = 0f;

                sr.color = c;

                float fadeDuration = 0.5f;

                float elapsed = 0f;

                while (elapsed < fadeDuration)

                {

                    elapsed += Time.deltaTime;

                    c.a = Mathf.Clamp01(elapsed / fadeDuration);

                    sr.color = c;

                    yield return null;

                }

                c.a = 1f;

                sr.color = c;

            }

        }



        // 2. Wait for animation to finish (if it's a ParticleSystem or Animator)

        if (anim != null)

        {

            var ps = anim.GetComponent<ParticleSystem>();

            if (ps != null)

            {

                yield return new WaitForSeconds(ps.main.duration);

                Destroy(anim);

            }

            else

            {

                yield return new WaitForSeconds(1f); // fallback duration

                Destroy(anim);

            }

        }



        // 4. Change the value of the card to 11 (Jack)

        cardInteraction.SetCardValue(11);



        // 5. Update the activePowerEffect

        cardInteraction.activePowerEffect = "Kapkaç";
        
        // 6. IMPORTANT: Clear any selection state to prevent automatic playing
        // This prevents the card from being automatically played after Kapkaç
        if (CardInteraction.currentlySelectedCard == cardInteraction)
        {
            CardInteraction.currentlySelectedCard = null;
            CardInteraction.isOneCardSelected = false;
            if (GameManager.LocalInstance != null)
            {
                GameManager.LocalInstance.SetCurrentSelectedHandCardNull();
            }
        }

    }



    private IEnumerator FadeInSprite(GameObject effectObj, float duration = 0.5f)

    {

        var sr = effectObj.GetComponent<SpriteRenderer>();

        if (sr == null)

            yield break;



        Color color = sr.color;

        color.a = 0f;

        sr.color = color;



        float elapsed = 0f;

        while (elapsed < duration)

        {

            elapsed += Time.deltaTime;

            color.a = Mathf.Clamp01(elapsed / duration);

            sr.color = color;

            yield return null;

        }

        color.a = 1f;

        sr.color = color;

    }





    //Called when the player decides to put the selected hand card to the center

    private void CardAddedToCenter()

    {

        networkRelay.AddCenterCardServerRPC(currentSelectedHandCard, CardInteraction.cardLookup[currentSelectedHandCard].GetCardID());

        myCards.Remove(currentSelectedHandCard);

        currentSelectedHandCard = null;

    }



    public void GetCardThatCaptured(string playedCard, SerializableCard serializedCard, int playerNumber)

    {

        AddToDebugLog($"[GameManager] GetCardThatCaptured called with playedCard: {playedCard}, playerNumber: {playerNumber}");

        AddToDebugLog($"[GameManager] This is local player: {playerNumber == deckController.thisPlayerNumber}");

        

        Debug.Log($"GameManager: Card {playedCard} captured cards for player {playerNumber}");

        var cardDictionary = serializedCard.ToDictionary();



        // Add captured cards to player's pool

        if (cardDictionary != null)

        {

            // Convert SerializableCard to dictionary and get the card ID for the played card

            

            if (cardDictionary.ContainsKey(playedCard))

            {

                int[] cardID = cardDictionary[playedCard];

                

                // Create a GameObject for the captured card

                GameObject capturedCard = Instantiate(cardBack, Vector3.zero, Quaternion.identity);

                capturedCard.transform.SetParent(playerPoolTransforms[playerNumber]);

                capturedCard.transform.localPosition = Vector3.zero;

                capturedCard.transform.localScale = Vector3.one;

                

                // Add to center cards objects list

                centerCardsObjects.Add(capturedCard);

                

                // Add to center cards dictionary

                centerCards[playedCard] = cardID;

                

                AddToDebugLog($"[GameManager] Added capture card {playedCard} with value [{cardID[0]},{cardID[1]}] to center cards");

                Debug.Log($"[GameManager] Added capture card {playedCard} with value [{cardID[0]},{cardID[1]}] to center cards");

            }

            

            // Add all captured cards to center cards dictionary for gold calculation

            foreach (var kvp in cardDictionary)

            {

                if (!centerCards.ContainsKey(kvp.Key))

                {

                    centerCards[kvp.Key] = kvp.Value;

                    AddToDebugLog($"[GameManager] Added captured card {kvp.Key} with value [{kvp.Value[0]},{kvp.Value[1]}] to center cards");

                    Debug.Log($"[GameManager] Added captured card {kvp.Key} with value [{kvp.Value[0]},{kvp.Value[1]}] to center cards");

                }

            }

        }

        

        // For non-local players, we need to call DiscardCapturedCards to move the cards to their pool

        if (playerNumber != deckController.thisPlayerNumber)

        {

            AddToDebugLog($"[GameManager] Calling DiscardCapturedCards for non-local player {playerNumber}");

            DiscardCapturedCards(playedCard, serializedCard, playerNumber);

        }

        

        // Handle gold gain for local player

        if (playerNumber == deckController.thisPlayerNumber && SuperPowerSpawner.LocalInstance != null)

        {

            // Ensure the capture card is included in the center cards before calculating gold

            // The capture card should already be in centerCards by now

            AddToDebugLog($"[GameManager] Center cards before gold calculation: {centerCards.Count} cards");

            Debug.Log($"[GameManager] Center cards before gold calculation: {centerCards.Count} cards");

            foreach (var kvp in centerCards)

            {

                AddToDebugLog($"[GameManager] Center card: {kvp.Key} -> [{kvp.Value[0]}, {kvp.Value[1]}]");

                Debug.Log($"[GameManager] Center card: {kvp.Key} -> [{kvp.Value[0]}, {kvp.Value[1]}]");

            }

            SuperPowerSpawner.LocalInstance.OnLocalCapture(playedCard);

        }

    }





    public void DiscardCapturedCards(string playedCard, SerializableCard serializedCard, int playerNumber)

    {

        AddToDebugLog($"[GameManager] DiscardCapturedCards called with playedCard: {playedCard}, playerNumber: {playerNumber}");

        AddToDebugLog($"[GameManager] currentPlayerNo: {currentPlayerNo}, deckController.thisPlayerNumber: {deckController.thisPlayerNumber}");

        AddToDebugLog($"[GameManager] This is local player: {playerNumber == deckController.thisPlayerNumber}");

        AddToDebugLog($"[GameManager] serializedCard contains {serializedCard.ToDictionary().Count} cards");

        

        if (playerNumber == deckController.thisPlayerNumber)

        {

            AddToDebugLog($"[GameManager] Processing local player's capture, calling deckController.DiscardCapturedCards for visual processing");

            AddToDebugLog($"[GameManager] Setting movePlayedLocally to true before visual processing");

            movePlayedLocally = true;

            StartCoroutine(deckController.DiscardCapturedCards(playedCard, serializedCard, playerNumber));

        }

        else

        {

            AddToDebugLog($"[GameManager] This is non-local player's capture, calling deckController.DiscardCapturedCards");

            StartCoroutine(deckController.DiscardCapturedCards(playedCard, serializedCard, playerNumber));

        }

    }



    public void GetCardAddedToCenter(string uniqueCardID, int[] cardID)

    {

        AddToDebugLog($"[GameManager] GetCardAddedToCenter called with uniqueCardID: {uniqueCardID}");

        AddToDebugLog($"[GameManager] GetCardAddedToCenter: movePlayedLocally = {movePlayedLocally}");

        AddToDebugLog($"[GameManager] GetCardAddedToCenter: isProcessingCapture = {isProcessingCapture}");

        Debug.Log("Discarding hand cards: " + movePlayedLocally);

        if (!movePlayedLocally)

        {

            PlayCardToCenter(uniqueCardID, cardID);

        }

        else if (!isProcessingCapture)

        {

            AddToDebugLog($"[GameManager] GetCardAddedToCenter: Setting movePlayedLocally to false");

            movePlayedLocally = false;

        }

        else

        {

            AddToDebugLog($"[GameManager] GetCardAddedToCenter: Skipping movePlayedLocally reset because isProcessingCapture is true");

        }

    }



    //To remove the played card from the hand when it played to the center

    public void PlayCardToCenter(string uniqueCardID, int[] cardID)

    {

        StartCoroutine(deckController.PlayHandCardToCenter(uniqueCardID, cardID));

    }



    public int turnCounter;

    public void UpdateCurrentPlayer(int playerNumber, int turnC)

    {

        AddToDebugLog($"[GameManager] UpdateCurrentPlayer called with playerNumber: {playerNumber}, turnC: {turnC}");

        AddToDebugLog($"[GameManager] Previous currentPlayerNo: {currentPlayerNo}, deckController.thisPlayerNumber: {deckController.thisPlayerNumber}");

        

        // --- Use relative index logic for turn indication ---

        int relativeIndex = (playerNumber - DeckController.LocalInstance.thisPlayerNumber + DeckController.LocalInstance.playerCount) % DeckController.LocalInstance.playerCount;

        if (ElHolderScript.LocalInstance != null)

        {

            ElHolderScript.LocalInstance.UpdateCurrentPlayer(relativeIndex);

        }



        // Reset card selection when turn changes
        bool wasMyTurn = IsLocalPlayerTurn();
        
        // Then update the current player

        currentPlayerNo = playerNumber;

        AddToDebugLog($"[GameManager] Updated currentPlayerNo to: {currentPlayerNo}");

        turnCounter = turnC;
        
        // Reset card selection if it was my turn before but isn't now
        if (wasMyTurn && !IsLocalPlayerTurn())
        {
            Debug.Log("[CardSelection] Turn changed away from local player, resetting card selection");
            CardInteraction.ResetCardSelection();
        }
        
        // Update superpower activate button state based on turn
        if (SuperPowerSpawner.LocalInstance != null)
        {
            SuperPowerSpawner.LocalInstance.UpdateActivateButtonForTurn();
        }

    }

    /// <summary>
    /// Check if it's currently the local player's turn
    /// </summary>
    /// <returns>True if it's the local player's turn, false otherwise</returns>
    public bool IsLocalPlayerTurn()
    {
        if (deckController == null)
        {
            Debug.LogWarning("[GameManager] IsLocalPlayerTurn: deckController is null");
            return false;
        }
        
        bool isMyTurn = currentPlayerNo == deckController.thisPlayerNumber;
        AddToDebugLog($"[GameManager] IsLocalPlayerTurn: currentPlayerNo={currentPlayerNo}, thisPlayerNumber={deckController.thisPlayerNumber}, isMyTurn={isMyTurn}");
        return isMyTurn;
    }





    public void UpdateCenterCardIDList(SerializableCard serializableCard)

    {

        centerCardIDList = serializableCard.ToDictionary().Keys.ToList();

    }



    public static string TurnCardIdToString(int[] tempCardID)

    {

        return tempCardID[0] + "_" + tempCardID[1];

    }



    public void PrintCenterCards()

    {

        foreach (var centerCardID in centerCards)

        {

            print(centerCardID.Value[0]);

            print(centerCardID.Value[1]);

        }

    }



    //!!!! Maybe can be deleted later or deactivated.

    public void PrintPlayerPools(SerializableDictionary serializableDictionary, int chkobbaPlayer)

    {

        Dictionary<int, List<string>> playersPooledCardsIDs = serializableDictionary.ToDictionary();



        foreach (var kvp in playersPooledCardsIDs)

        {

            int playerKey = kvp.Key;

            Debug.Log("Player " + playerKey);

            List<string> cardList = kvp.Value;



            foreach (var card in cardList)

            {

                string cardRepresentation = string.Join(", ", card);

                Debug.Log("Card: " + cardRepresentation);

            }

        }

    }



    public void ShowWinScreen(string message, int winnerSide, int point0, int point1)

    {



        ElHolderScript.LocalInstance.ReturnAllHandsToIdle();



        winScreen.SetActive(true);

        roundOverText.text = message;



        UpdatePointText(point0, point1);



        if (winnerSide != -1)

        {

            if (winnerSide == 3)

            {

                //Draw

                AudioManager.Instance.PlayAudio(2, 0.5f, false);

            }

            else if (deckController.thisPlayerNumber == 0 || deckController.thisPlayerNumber == 2)

            {

                //Side 0 win or lose

                if (winnerSide == 0) AudioManager.Instance.PlayAudio(2, 0.5f, false);

                else AudioManager.Instance.PlayAudio(1, 0.5f, false);

            }

            else if (deckController.thisPlayerNumber == 1 || deckController.thisPlayerNumber == 3)

            {

                //Side 1 win or lose

                if (winnerSide == 1) AudioManager.Instance.PlayAudio(2, 0.5f, false);

                else AudioManager.Instance.PlayAudio(1, 0.5f, false);

            }

        }

    }



    private void UpdatePointText(int point0, int point1)

    {

        if (deckController.thisPlayerNumber == 0 || deckController.thisPlayerNumber == 2)

        {

            pointTexts[0].text = point0.ToString();

            pointTexts[1].text = point1.ToString();

            GameObject.Find("TallyContainer").GetComponent<TallyMarkDisplay>().UpdateTallyDisplay(point0);

            GameObject.Find("TallyContainer1").GetComponent<TallyMarkDisplay>().UpdateTallyDisplay(point1);

        }



        else if (deckController.thisPlayerNumber == 1 || deckController.thisPlayerNumber == 3)

        {

            pointTexts[1].text = point0.ToString();

            pointTexts[0].text = point1.ToString();

            GameObject.Find("TallyContainer").GetComponent<TallyMarkDisplay>().UpdateTallyDisplay(point1);

            GameObject.Find("TallyContainer1").GetComponent<TallyMarkDisplay>().UpdateTallyDisplay(point0);

        }

    }



    public void GetPlayerNumber(int playerNumber)

    {
        Debug.LogError($"[PLAYER NUMBER] GetPlayerNumber called with playerNumber: {playerNumber}");
        deckController.SetPlayerNumber(playerNumber);

    }



    public void AskPlayerNumber()

    {

        networkRelay.AskPlayerNumberServerRPC();

    }



    private void PlayAfterTimeOut()

    {

        /*int[] playableCardID = { 0, 0 };



        List<int[]> myCards = new List<int[]>();



        foreach (Transform childTransform in GameObject.Find("PlayerHand" + (deckController.thisPlayerNumber + 1)).transform)

        {

            string[] tagStrings = childTransform.gameObject.tag.Split('_');

            if (tagStrings.Length == 2)

            {

                int[] cardID = { 0, 0 };



                int.TryParse(tagStrings[0], out cardID[0]);

                int.TryParse(tagStrings[1], out cardID[1]);

                myCards.Add(cardID);

            }



        }



        bool addedToCenterFlag = false;



        foreach (int[] cardID in myCards)

        {

            if (cardID[1] != centerCards[centerCards.Count - 1][1])

            {

                currentSelectedHandCard = cardID;

                CardAddedToCenter();

                addedToCenterFlag = true;

                return;

            }

            else playableCardID = cardID;

        }



        if (!addedToCenterFlag)

        {

            currentSelectedHandCard = playableCardID;

            CheckIfLegal(currentPlayerNo);

        }*/

    }



    public void GetTurnTime(float turnTime)

    {

        turnTimer = turnTime;

    }



    private void InitialGameManagerSetUp()

    {

        AudioManager.Instance.PlayAudio(4, 0.04f, true);



        //Setting the networkRealy script to sen ServerRPCs

        networkRelay = FindObjectOfType<NetworkRelay>();





        //Setting the localInstances for ClientRPC messages

        if (LocalInstance == null)

        {

            LocalInstance = this;

        }



        ProfileScript profileScript = GameObject.Find("MainUI").GetComponent<ProfileScript>();

        cardBackSprite = profileScript.cardBackSprites[profileScript.cardBackIndex];



        pointTexts.Add(GameObject.Find("PlayerPointText1").GetComponent<Text>());

        GameObject.Find("PlayerPointText1").GetComponent<Text>().text = "0 ";

        pointTexts.Add(GameObject.Find("PlayerPointText2").GetComponent<Text>());

        GameObject.Find("PlayerPointText2").GetComponent<Text>().text = "0 ";



        waitingScreen = GameObject.Find("WaitingScreen");

        waitingScreen.SetActive(false);

        mainScreen = GameObject.Find("MainScreen");



        winScreen = GameObject.Find("WinScreen");

        roundOverText = GameObject.Find("RoundOverText").GetComponent<Text>();



        superPowerTextGroup = GameObject.Find("SuperPowerTextContainer").GetComponent<CanvasGroup>();

        superPowerText = GameObject.Find("SuperPowerText").GetComponent<TextMeshProUGUI>();

        superPowerTextB = GameObject.Find("SuperPowerTextB").GetComponent<TextMeshProUGUI>();

        superPowerTextGroup.gameObject.SetActive(false);

        pistiTextGroup = GameObject.Find("PiştiTextContainer").GetComponent<CanvasGroup>();

        pistiText = GameObject.Find("PiştiText").GetComponent<TextMeshProUGUI>();

        pistiTextB = GameObject.Find("PiştiTextB").GetComponent<TextMeshProUGUI>();

        pistiTextGroup.gameObject.SetActive(false);



        GetNecessaryTransforms();





        centerTransform = GameObject.Find("Center").GetComponent<Transform>();

        deckController.GetPlayerHandTransforms(playerHandTransforms, playerPoolTransforms, centerTransform, playerPiştiPoolTransforms);



        var centerObj = GameObject.Find("Center");

        if (centerObj != null)

        {

            bombObject = centerObj.transform.Find("Bomb")?.gameObject;

            if (bombObject != null)

            {

                bombAnimator = bombObject.GetComponent<Animator>();

                bombObject.SetActive(false);

            }



            explosionObject = centerObj.transform.Find("Explosion")?.gameObject;

            if (explosionObject != null)

            {

                explosionAnimator = explosionObject.GetComponent<Animator>();

                explosionObject.SetActive(false);



                // Find Bomb child and its animator



            }

        }





    }



    public void SkipTurn()

    {

        if (currentPlayerNo == deckController.thisPlayerNumber) Invoke("PlayAfterTimeOut", 1);

    }



    /// <summary>

    /// Checks if the current player can play a card from their hand.

    /// Returns false if it's not their turn, unless special powers allow it.

    /// </summary>

    public bool CanPlayerPlay(GameObject cardObj)

    {

        AddToDebugLog($"[GameManager] CanPlayerPlay check - currentPlayerNo: {currentPlayerNo}, thisPlayerNumber: {deckController.thisPlayerNumber}");



        // Check if it's the player's turn

        bool isMyTurn = (currentPlayerNo == deckController.thisPlayerNumber);

        AddToDebugLog($"[GameManager] Is my turn: {isMyTurn}");



        // Check if the card's parent is "PlayerHand1" or "PlayerHand3" (for bot)

        bool isInPlayerHand1 = false;
        bool isInPlayerHand3 = false;

        if (cardObj != null && cardObj.transform.parent != null)

        {

            isInPlayerHand1 = cardObj.transform.parent.name == "PlayerHand1";
            isInPlayerHand3 = cardObj.transform.parent.name == "PlayerHand3";

        }

        AddToDebugLog($"[GameManager] Card parent is PlayerHand1: {isInPlayerHand1}");
        AddToDebugLog($"[GameManager] Card parent is PlayerHand3: {isInPlayerHand3}");



        // Check if this is a center card (should not be playable)
        if (cardObj != null && cardObj.transform.parent != null && cardObj.transform.parent.name == "Center")
        {
            AddToDebugLog($"[GameManager] Center card cannot be played - showcasing only");
            return false;
        }

        // Check if this is a bot playing from PlayerHand3
        bool isBotPlaying = (isInPlayerHand3 && currentPlayerNo == 1); // Bot is player 1
        AddToDebugLog($"[GameManager] Is bot playing: {isBotPlaying}");

        // Player can play if:
        // 1. It's their turn AND the card is in PlayerHand1, OR
        // 2. It's the bot's turn AND the card is in PlayerHand3
        if ((isMyTurn && isInPlayerHand1) || isBotPlaying)
        {
            AddToDebugLog($"[GameManager] Player can play - isMyTurn: {isMyTurn}, isInPlayerHand1: {isInPlayerHand1}, isBotPlaying: {isBotPlaying}");
            return true;
        }



        // If it's not their turn, check if any special powers allow playing

        if (!isMyTurn)

        {

            bool specialPowerAllowsPlay = IsSpecialPowerAllowingPlay();

            AddToDebugLog($"[GameManager] Special power allows play: {specialPowerAllowsPlay}");

            return specialPowerAllowsPlay;

        }



        // If it's their turn but the card is not in PlayerHand1, do not allow play

        return false;

    }



    /// <summary>

    /// Checks if any active special powers allow the player to play out of turn.

    /// Override this method to add specific power logic.

    /// </summary>

    public bool IsSpecialPowerAllowingPlay()

    {

        AddToDebugLog($"[GameManager] Checking special powers that allow out-of-turn play");

        

        // Check for powers that allow selecting other players' cards or playing out of turn

        if (isKapkacPending)

        {

            AddToDebugLog($"[GameManager] Kapkaç pending - allowing out-of-turn interaction");

            return true;

        }

        

        if (isYandimAnamPending)

        {

            AddToDebugLog($"[GameManager] Yandım Anam pending - allowing out-of-turn interaction");

            return true;

        }

        

        if (isKopyalaActive)

        {

            AddToDebugLog($"[GameManager] Kopyala active - allowing out-of-turn interaction");

            return true;

        }

        

        // Add other special powers here as needed

        // if (someOtherSpecialPowerActive) return true;

        

        return false;

    }



    /// <summary>

    /// Called when a player tries to play a card but it's not their turn.

    /// This method can be used to show feedback to the player or handle special cases.

    /// </summary>

    public void OnPlayerTriedToPlayOutOfTurn()

    {

        AddToDebugLog($"[GameManager] Player tried to play out of turn!");

        

        // You can add feedback here, such as:

        // - Show a message to the player

        // - Play a sound effect

        // - Highlight whose turn it is

        // - Return the card to its original position

        

        // Reset the card selection and position

        if (CardInteraction.currentlySelectedCard != null)

        {

            AddToDebugLog($"[GameManager] Resetting card selection due to out-of-turn play attempt");

            CardInteraction.currentlySelectedCard.SnapBackToOriginalPosition();

        }

        

        // Reset selection state

        currentSelectedHandCard = null;

        

        // TODO: Add visual/audio feedback for the player

        // TODO: Optionally highlight whose turn it is

    }



    public void TellServerTurnEnded()

    {

        AddToDebugLog($"[GameManager] TellServerTurnEnded called, currentPlayerNo: {currentPlayerNo}, thisPlayerNumber: {deckController.thisPlayerNumber}");

        AddToDebugLog($"[GameManager] hasAlreadySentRPC: {hasAlreadySentRPC}");

        networkRelay.NotifyTurnIsReadyToEndServerRPC();

        AddToDebugLog($"[GameManager] NotifyTurnIsReadyToEndServerRPC called");

    }



    private void GetNecessaryTransforms()

    {

        //All the necessary transforms for card placements



        playerHandTransforms.Add(GameObject.Find("PlayerHand1").GetComponent<Transform>());

        playerHandTransforms.Add(GameObject.Find("PlayerHand2").GetComponent<Transform>());

        playerHandTransforms.Add(GameObject.Find("PlayerHand3").GetComponent<Transform>());

        playerHandTransforms.Add(GameObject.Find("PlayerHand4").GetComponent<Transform>());



        playerPoolTransforms.Add(GameObject.Find("PlayerPool1").GetComponent<Transform>());

        playerPoolTransforms.Add(GameObject.Find("PlayerPool2").GetComponent<Transform>());

        playerPoolTransforms.Add(GameObject.Find("PlayerPool3").GetComponent<Transform>());

        playerPoolTransforms.Add(GameObject.Find("PlayerPool4").GetComponent<Transform>());



        playerPiştiPoolTransforms.Add(GameObject.Find("PlayerPiştiPool1").GetComponent<Transform>());

        playerPiştiPoolTransforms.Add(GameObject.Find("PlayerPiştiPool2").GetComponent<Transform>());

        playerPiştiPoolTransforms.Add(GameObject.Find("PlayerPiştiPool3").GetComponent<Transform>());

        playerPiştiPoolTransforms.Add(GameObject.Find("PlayerPiştiPool4").GetComponent<Transform>());



        verZehriObject = GameObject.Find("VerZehriFog");

        kutsalDesteObject = GameObject.Find("KutsalDesteFog");

    }



    /// <summary>

    /// Activates the "peek at a random opponent card" super power locally and sends the move to the server.

    /// </summary>

    public void UsePeekOpponentCardPower()

    {

        Debug.Log("Using Peek Opponent Card Power");

        int opponentPlayerNo = GetRandomOpponentPlayerNo();

        int cardIndex = deckController.GetRandomHandCardIndex(opponentPlayerNo);



        // Send to server for sync

        networkRelay.UsePeekOpponentCardPowerServerRPC(opponentPlayerNo, cardIndex);

    }



    /// <summary>

    /// Called by the server to sync the peek effect to all clients.

    /// </summary>

    public void OnPeekOpponentCardSynced(int opponentPlayerNo, int cardIndex)

    {

        deckController.PeekOpponentCard(opponentPlayerNo, cardIndex);



    }



    public void UseBayaBayaBakPower()

    {

        Debug.Log($"[GameManager] UseBayaBayaBakPower() called - Player: {deckController.thisPlayerNumber}, Time: {Time.time}");

        int opponentPlayerNo = GetRandomOpponentPlayerNo(); // You can reuse your existing logic

        Debug.Log($"[GameManager] Selected opponent: {opponentPlayerNo}, calling UseBayaBayaBakServerRPC()");

        
        // Track the power usage
        DebugChainPrinter.LocalInstance?.TrackPowerUsage("BayaBayaBak", deckController.thisPlayerNumber, $"Target opponent: {opponentPlayerNo}");
        DebugChainPrinter.LocalInstance?.TrackLocalAction($"UseBayaBayaBakPower called for opponent {opponentPlayerNo}");

        networkRelay.UseBayaBayaBakServerRPC(opponentPlayerNo);

        Debug.Log($"[GameManager] UseBayaBayaBakPower() complete - Server will process the power");

    }



    public void OnBayaBayaBakSynced(int opponentPlayerNo)

    {

        Debug.Log($"[GameManager] OnBayaBayaBakSynced() called - Opponent: {opponentPlayerNo}, Time: {Time.time}");

        
        // Track the power effect
        DebugChainPrinter.LocalInstance?.TrackLocalAction($"OnBayaBayaBakSynced received for opponent {opponentPlayerNo}");
        DebugChainPrinter.LocalInstance?.TrackPowerUsage("BayaBayaBak", deckController.thisPlayerNumber, $"Effect applied - revealed opponent {opponentPlayerNo} cards");
        
        // Track power completion
        MoveChainIntegrator.ReportPowerCompletion("BayaBayaBak", deckController.thisPlayerNumber, $"Revealed opponent {opponentPlayerNo} cards");
        
        // CRITICAL FIX: Force game state save after power completion to ensure reconnection sync
        if (Server.Singleton != null)
        {
            Debug.Log("[GameManager] Power effect completed - forcing game state save for reconnection sync");
            Server.Singleton.SaveCurrentGameState();
        }

        deckController.PeekOpponentCardAll(opponentPlayerNo);

        Debug.Log($"[GameManager] OnBayaBayaBakSynced() complete - Opponent cards revealed");

    }



    /// <summary>

    /// Returns a random opponent player number (not self or teammate in 2v2).

    /// </summary>

    private int GetRandomOpponentPlayerNo()

    {

        List<int> possibleOpponents = new List<int>();

        int myNo = deckController.thisPlayerNumber;

        int playerCount = deckController.playerCount;



        if (playerCount == 2)

        {

            possibleOpponents.Add((myNo + 1) % 2);

        }

        else if (playerCount == 4)

        {

            // In 2v2, teammates are 0/2 and 1/3

            if (myNo == 0 || myNo == 2)

                possibleOpponents.AddRange(new int[] { 1, 3 });

            else

                possibleOpponents.AddRange(new int[] { 0, 2 });

        }

        return possibleOpponents[UnityEngine.Random.Range(0, possibleOpponents.Count)];

    }



    /// <summary>

    /// Activates the "swap a card with an opponent" super power locally and sends the move to the server.

    /// </summary>

    public void UseSwapCardWithOpponentPower()

    {

        Debug.Log("Using Swap Card With Opponent Power (randomized, new logic)");

        int myPlayerNo = deckController.thisPlayerNumber;

        int opponentPlayerNo = GetRandomOpponentPlayerNo();



        if (myCards == null || myCards.Count == 0)

        {

            Debug.LogWarning("No cards in hand for DeğişTokuş!");

            return;

        }

        string myHandCardID = myCards[UnityEngine.Random.Range(0, myCards.Count)];



        List<string> oppHand = deckController.GetOpponentHandCardIDs(opponentPlayerNo);

        if (oppHand == null || oppHand.Count == 0)

        {

            Debug.LogWarning("No cards in opponent's hand for DeğişTokuş!");

            return;

        }

        string oppHandCardID = oppHand[UnityEngine.Random.Range(0, oppHand.Count)];



        // Add a short delay before swapping

        StartCoroutine(DelayedSwap(myPlayerNo, myHandCardID, opponentPlayerNo, oppHandCardID));

    }



    private IEnumerator DelayedSwap(int myPlayerNo, string myHandCardID, int opponentPlayerNo, string oppHandCardID)

    {

        yield return new WaitForSeconds(0.5f); // Adjust as needed

        networkRelay.UseSunuDegisTokusServerRPC(myPlayerNo, myHandCardID, oppHandCardID);

    }



    public void ActivateValeArarPower()
    {
        Debug.Log("ValeArar power activated! Showing indicators for all Jacks for the entire round.");
        
        valeArarActive = true;
        
        foreach (var cardScript in cardInteractionsScripts)
        {
            // Assuming cardID[1] is the value, and 11 is Jack
            if (cardScript != null && cardScript.gameObject != null)
            {
                string[] tagParts = cardScript.gameObject.tag.Split('_');
                if (tagParts.Length == 2 && int.TryParse(tagParts[1], out int value) && value == 11)
                {
                    // Activate the indicator for this card
                    var indicator = cardScript.transform.Find("SelectedCardIndicator(Clone)");
                    if (indicator != null)
                    {
                        indicator.gameObject.SetActive(true);
                        indicator.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0f, 0.5f); // Red with 50% transparency
                    }
                }
            }
        }
    }



    public void DeactivateValeArarPower()
    {
        Debug.Log("Deactivating ValeArar power: hiding indicators for all Jacks.");
        
        valeArarActive = false;
        
        foreach (var cardScript in cardInteractionsScripts)
        {
            if (cardScript != null && cardScript.gameObject != null)
            {
                string[] tagParts = cardScript.gameObject.tag.Split('_');
                if (tagParts.Length == 2 && int.TryParse(tagParts[1], out int value) && value == 11)
                {
                    var indicator = cardScript.transform.Find("SelectedCardIndicator(Clone)");
                    if (indicator != null)
                        indicator.gameObject.SetActive(false);
                }
            }
        }
    }
    
    public void RefreshValeArarIndicators()
    {
        if (!valeArarActive) return;
        
        Debug.Log("Refreshing Vale Arar indicators for all Jacks.");
        
        foreach (var cardScript in cardInteractionsScripts)
        {
            if (cardScript != null && cardScript.gameObject != null)
            {
                string[] tagParts = cardScript.gameObject.tag.Split('_');
                if (tagParts.Length == 2 && int.TryParse(tagParts[1], out int value) && value == 11)
                {
                    // Activate the indicator for this card
                    var indicator = cardScript.transform.Find("SelectedCardIndicator(Clone)");
                    if (indicator != null)
                    {
                        indicator.gameObject.SetActive(true);
                        indicator.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0f, 0.5f); // Red with 50% transparency
                    }
                }
            }
        }
    }





    public CardInteraction kopyalaSourceCard = null;

    public bool isKopyalaActive = false;

    // Call this when the power is activated

    // New dual selection method for Kopyala Yapıştır
    public void StartKopyalaYapistirDualSelection(CardInteraction sourceCard)
    {
        Debug.Log($"[GameManager] StartKopyalaYapistirDualSelection - Source: {sourceCard.uniqueCardInstanceID}");
        
        isKopyalaActive = true;
        kopyalaSourceCard = sourceCard;
        
        // Allow selection from all hands for target card
        CardInteraction.AllowSelectionForParents(
            new[] { "PlayerHand1", "PlayerHand2", "PlayerHand3", "PlayerHand4" },
            allowOwnHandCards: true,
            maxSelections: 1
        );
        
        // Start hand showcasing for dual selection
        Debug.Log("[Showcase] GameManager: Starting Kopyala Yapıştır dual selection showcase");
        if (SuperPowerSpawner.LocalInstance != null)
        {
            SuperPowerSpawner.LocalInstance.StartHandShowcaseForDualSelection("Kopyala Yapıştır");
            Debug.Log("[Showcase] GameManager: Called StartHandShowcaseForDualSelection for Kopyala Yapıştır");
        }
        else
        {
            Debug.LogError("[Showcase] GameManager: ERROR - SuperPowerSpawner.LocalInstance is null!");
        }
        
        Debug.Log("[GameManager] Dual selection mode active - select target card to complete power");
    }
    
    public void ActivateKopyalaYapistirPower()
    {
        // This method is now obsolete - Kopyala Yapıştır uses dual selection
        Debug.LogWarning("[GameManager] ActivateKopyalaYapistirPower() called but this method is obsolete. Kopyala Yapıştır now uses dual selection.");
    }





    // Call this from CardInteraction when a card is clicked and isKopyalaActive is true

    public void TryKopyalaYapistir(CardInteraction targetCard)
    {
        Debug.Log($"[GameManager] TryKopyalaYapistir called - Target: {targetCard.gameObject.name}");

        if (!isKopyalaActive || kopyalaSourceCard == null || targetCard == null || targetCard == kopyalaSourceCard)
        {
            Debug.LogWarning("[GameManager] TryKopyalaYapistir - Invalid state or same card selected");
            return;
        }

        Debug.Log($"[GameManager] KopyalaYapıştır: {kopyalaSourceCard.gameObject.name} -> {targetCard.gameObject.name}");

        // NOW call PowerActivated() since both cards are selected and we're executing the power
        if (DeckController.LocalInstance != null)
        {
            Debug.Log("[GameManager] Calling PowerActivated() for Kopyala Yapıştır since both cards are now selected");
            // Create a temporary power instance to call PowerActivated
            var tempPower = ScriptableObject.CreateInstance<KopyalaYapistir>();
            tempPower.PowerActivated();
            DestroyImmediate(tempPower);
        }

        // Network the change to server and all clients
        networkRelay.KopyalaYapistirServerRPC(targetCard.uniqueCardInstanceID, kopyalaSourceCard.uniqueCardInstanceID);

        // Stop hand showcasing
        Debug.Log("[Showcase] GameManager: Stopping Kopyala Yapıştır dual selection showcase");
        if (SuperPowerSpawner.LocalInstance != null)
        {
            SuperPowerSpawner.LocalInstance.StopHandShowcase();
            Debug.Log("[Showcase] GameManager: Called StopHandShowcase for Kopyala Yapıştır");
        }
        else
        {
            Debug.LogError("[Showcase] GameManager: ERROR - SuperPowerSpawner.LocalInstance is null!");
        }
        
        // Reset state
        isKopyalaActive = false;
        kopyalaSourceCard = null;
        CardInteraction.currentlySelectedCard = null;
        SetCurrentSelectedHandCardNull();
    }



    public void OnKopyalaYapistir(string targetUniqueID, string sourceUniqueID)

    {

        if (CardInteraction.cardLookup.TryGetValue(targetUniqueID, out var targetCard) &&

            CardInteraction.cardLookup.TryGetValue(sourceUniqueID, out var sourceCard))

        {

            // Track the superpower effect BEFORE applying it
            var effectData = new Dictionary<string, string>
            {
                ["sourceCard"] = sourceUniqueID,
                ["targetCard"] = targetUniqueID,
                ["oldValue"] = targetCard.GetCardID()[1].ToString(),
                ["newValue"] = sourceCard.GetCardID()[1].ToString(),
                ["effectType"] = "cardCopy"
            };
            MoveChainIntegrator.TrackSuperpowerEffect(currentPlayerNo, "Kopyala Yapıştır", new[] { targetUniqueID, sourceUniqueID }, effectData);
            
            // Track power completion
            MoveChainIntegrator.ReportPowerCompletion("Kopyala Yapıştır", currentPlayerNo, $"Copied {sourceUniqueID} to {targetUniqueID}");
            
            // CRITICAL FIX: Force game state save after power completion to ensure reconnection sync
            if (Server.Singleton != null)
            {
                Debug.Log("[GameManager] Power effect completed - forcing game state save for reconnection sync");
                Server.Singleton.SaveCurrentGameState();
            }

            // Copy cardID and sprite

            int[] newCardID = sourceCard.GetCardID();

            Sprite newSprite = sourceCard.GetComponent<SpriteRenderer>().sprite;

            targetCard.activePowerEffect = sourceCard.activePowerEffect; // Copy active power effect



            // Set cardID and sprite with fade-in

            StartCoroutine(SetCardIDAndSpriteWithFade(targetCard, newCardID, newSprite));



            ResetVisuals(targetCard);

            if (targetCard.activePowerEffect != "none")

            {

                CopyEffectVisuals(targetCard);

            }

        }

    }



    private void CopyEffectVisuals(CardInteraction targetCard)

    {

        string effectName = targetCard.activePowerEffect;

        if (effectName == "Kapkaç")

        {

            StartCoroutine(KapkacCourotine(targetCard.uniqueCardInstanceID));

        }

        else if (effectName == "YandımAnam")

        {

            StartCoroutine(YandimAnamCoroutine(targetCard.uniqueCardInstanceID));

        }

    

    }



    private void ResetVisuals(CardInteraction targetCard)

    {

        int counter = 0;

        foreach (Transform child in targetCard.gameObject.transform)

        {

            if (counter > 1)

            {

                Destroy(child.gameObject);

            }

            counter++;

        }

    }



    private IEnumerator SetCardIDAndSpriteWithFade(CardInteraction card, int[] newCardID, Sprite newSprite)

    {

        var sr = card.GetComponent<SpriteRenderer>();

        if (sr == null)

            yield break;



        // Fade out

        Color color = sr.color;

        float duration = 0.3f;

        float elapsed = 0f;

        while (elapsed < duration)

        {

            elapsed += Time.deltaTime;

            color.a = Mathf.Clamp01(1f - (elapsed / duration));

            sr.color = color;

            yield return null;

        }

        color.a = 0f;

        sr.color = color;



        // Change cardID and sprite

        card.SetCardIDAndSprite(newCardID, newSprite);



        // Fade in

        elapsed = 0f;

        while (elapsed < duration)

        {

            elapsed += Time.deltaTime;

            color.a = Mathf.Clamp01(elapsed / duration);

            sr.color = color;

            yield return null;

        }

        color.a = 1f;

        sr.color = color;

    }



    // Call this at the end of the round to reset all cards

    public void ResetAllKopyalaCards()

    {

        /*foreach (var cardScript in cardInteractionsScripts)

            cardScript.ResetToOriginalCard();*/

    }



    public void PlayAutomatically(int playerIndex)

    {

        // Defensive: Check if playerHandTransforms is valid and has children

        if (playerHandTransforms == null || playerHandTransforms.Count <= playerIndex - 1)

        {

            Debug.LogError($"PlayAutomatically: playerHandTransforms missing for player {playerIndex}");

            return;

        }



        var handTransform = playerHandTransforms[playerIndex - 1];

        if (handTransform.childCount == 0)

        {

            Debug.LogWarning($"PlayAutomatically: No cards left in hand for player {playerIndex}");

            return;

        }



        // Get the first card in the hand (change index if you want a different card)

        var cardObj = handTransform.GetChild(2).gameObject;

        var cardScript = cardObj.GetComponent<CardInteraction>();

        if (cardScript == null)

        {

            Debug.LogError("PlayAutomatically: CardInteraction not found on hand card.");

            return;

        }



        // Set as if this card was selected

        currentSelectedHandCard = cardScript.uniqueCardInstanceID;

        CardInteraction.currentlySelectedCard = cardScript;



        // Optionally, invoke selection event if you want to mimic UI

        cardScript.TriggerOnCardSelected();



        // Play the card as if the player played it

        CheckIfLegal(playerIndex - 1);

    }



    public void ActivateBombaPower()

    {

        // Tell the server to bomb the center

        networkRelay.BombaServerRPC();

    }



    public void OnBombaCenter()

    {

        // Track bomba effect BEFORE applying it
        var bombedCardIds = centerCardsObjects.Where(c => c != null)
            .Select(c => c.GetComponent<CardInteraction>()?.uniqueCardInstanceID)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToArray();
        
        var effectData = new Dictionary<string, string>
        {
            ["bombedCardCount"] = bombedCardIds.Length.ToString(),
            ["effectType"] = "centerClear"
        };
        MoveChainIntegrator.TrackSuperpowerEffect(currentPlayerNo, "Bomba", bombedCardIds, effectData);

        StartCoroutine(BombThenExplosionSequence());

    }



    private IEnumerator BombThenExplosionSequence()

    {

        centerCards.Clear();

        // 1. Ensure both are inactive at the start

        if (bombObject != null) bombObject.SetActive(false);

        if (explosionObject != null) explosionObject.SetActive(false);



        // 2. Activate and play Bomb animation

        if (bombObject != null && bombAnimator != null)

        {

            bombObject.SetActive(true);

            bombAnimator.Play("BombAnimationClip", 0, 0f);



            // Wait for bomb animation to finish

            float bombAnimLength = 1.0f;

            AnimatorStateInfo bombStateInfo = bombAnimator.GetCurrentAnimatorStateInfo(0);

            if (bombStateInfo.length > 0)

                bombAnimLength = bombStateInfo.length;

            else if (bombAnimator.runtimeAnimatorController != null && bombAnimator.runtimeAnimatorController.animationClips.Length > 0)

                bombAnimLength = bombAnimator.runtimeAnimatorController.animationClips[0].length;



            yield return new WaitForSeconds(bombAnimLength);



            // Optionally, hide the bomb sprite after animation

            var bombSpriteRenderer = bombObject.GetComponent<SpriteRenderer>();

            if (bombSpriteRenderer != null)

                bombSpriteRenderer.sprite = null;



            bombObject.SetActive(false);

        }

        else

        {

            Debug.LogWarning("Bomb object or animator not found!");

        }



        GameObject bombedStack = GameObject.Find("BombedStack");

        if (bombedStack == null)

        {

            Debug.LogError("BombedStack GameObject not found in scene!");

            yield break;

        }



        foreach (var cardObj in centerCardsObjects)

        {

            if (cardObj != null)

            {

                cardObj.transform.SetParent(bombedStack.transform, true);

                cardObj.transform.localPosition = Vector3.zero;

            }

        }



        // 3. Activate and play Explosion animation

        if (explosionObject != null && explosionAnimator != null)

        {

            explosionObject.SetActive(true);

            explosionAnimator.Play("ExplosionAnimationClip", 0, 0f);



            // Wait for explosion animation to finish

            float explosionAnimLength = 1.0f;

            AnimatorStateInfo explosionStateInfo = explosionAnimator.GetCurrentAnimatorStateInfo(0);

            if (explosionStateInfo.length > 0)

                explosionAnimLength = explosionStateInfo.length;

            else if (explosionAnimator.runtimeAnimatorController != null && explosionAnimator.runtimeAnimatorController.animationClips.Length > 0)

                explosionAnimLength = explosionAnimator.runtimeAnimatorController.animationClips[0].length;



            yield return new WaitForSeconds(explosionAnimLength);



            explosionObject.SetActive(false);

        }

        else

        {

            Debug.LogWarning("Explosion object or animator not found!");

        }



        // 5. Clear all center-related lists/dictionaries (as before)

        centerCardsObjects.Clear();

        if (centerCardIDList != null) centerCardIDList.Clear();



        Debug.Log("Bomba: Center cleared and cards moved to BombedStack.");

    }











    private IEnumerator DisableExplosionAfterAnimation()

    {

        // Wait for the animation to finish

        float animLength = 1.0f;

        if (explosionAnimator != null)

        {

            AnimatorStateInfo stateInfo = explosionAnimator.GetCurrentAnimatorStateInfo(0);

            animLength = stateInfo.length;

        }

        yield return new WaitForSeconds(animLength);



        if (explosionObject != null)

            explosionObject.SetActive(false);

    }







    private bool isYapamazsınActive = false;



    public void ActivateYapamazsınPower()

    {

        networkRelay.ActivateYapamazsınServerRPC();

    }



    public void SetYapamazsınActive(bool isActive)

    {

        isYapamazsınActive = isActive;

        // Optionally update UI here

    }



    public bool oynayamazsinActive = false;

    public void SetOynayamazsinActive(bool isActive)

    {

        oynayamazsinActive = isActive;



        if (isActive)

        {

            // CRITICAL: This is called when the power is TRULY activated (after card play), not when button is pressed
            Debug.LogWarning("Oynayamazsin power TRULY activated! Showing block prefab above center.");

            // Show the block prefab above the center

            if (oynayamazsinBlockInstance == null && oynayamazsinBlockPrefab != null)

            {

                oynayamazsinBlockInstance = Instantiate(oynayamazsinBlockPrefab, centerTransform.GetChild(centerTransform.childCount - 1));

                oynayamazsinBlockInstance.transform.localPosition = new Vector3(0, 0, -0.05f); // Slightly above center



                StartCoroutine(FadeInSprite(oynayamazsinBlockInstance, 0.5f));

            }

        }

        else

        {

            // Hide and destroy the block prefab

            if (oynayamazsinBlockInstance != null)

            {

                Destroy(oynayamazsinBlockInstance);

                oynayamazsinBlockInstance = null;

            }

        }

    }



    public void ActivateKapkacPower()
    {
        // This method is now obsolete - Kapkaç uses pre-selected cards
        Debug.LogWarning("[GameManager] ActivateKapkacPower() called but this method is obsolete. Kapkaç now uses pre-selected cards.");
    }





    public void ActivateBlockNextPlayerPower()

    {

        networkRelay.ActivateOynayamazsinServerRPC();

    }



    public bool verZehriActive = false;

    public bool kutsalDesteActive = false;

    public bool valeArarActive = false;



    public void SetVerZehriActive(bool isActive)

    {

        verZehriActive = isActive;

        if (isActive)

        {

            // CRITICAL: This is called when the power is TRULY activated (after card play), not when button is pressed
            StartVerZehriEffect();

        }



        else

        {

            StopVerZehriEffect();

        }



    }



    public void SetKutsalDesteActive(bool isActive)

    {

        kutsalDesteActive = isActive;

        if (isActive)

        {
            // CRITICAL: This is called when the power is TRULY activated (after card play), not when button is pressed
            StartKutsalDesteEffect();
        }

        else

            StopKutsalDesteEffect();

    }



    [SerializeField] public GameObject verZehriObject;

    private void StartVerZehriEffect()

    {

        // CRITICAL: This is called when Ver Zehri is TRULY activated (after card play), not when button is pressed

        Debug.Log("VerZehri effect TRULY started (after card play)!");

        // TODO: Add your visual effect here

        foreach (Transform child in verZehriObject.transform)

        {

            child.GetComponent<FogController>().StartFog();

        }



    }



    private void StopVerZehriEffect()

    {

        // Hide UI/animation for VerZehri

        Debug.Log("VerZehri effect stopped!");

        // TODO: Remove your visual effect here

        foreach (Transform child in verZehriObject.transform)

        {

            child.GetComponent<FogController>().StopFog();

        }

    }



    [SerializeField] public GameObject kutsalDesteObject;

    private void StartKutsalDesteEffect()

    {

        // CRITICAL: This is called when Kutsal Deste is TRULY activated (after card play), not when button is pressed

        Debug.Log("KutsalDeste effect TRULY started (after card play)!");

        // TODO: Add your visual effect here

        foreach (Transform child in kutsalDesteObject.transform)

        {

            child.GetComponent<FogController>().StartFog();

        }

    }



    private void StopKutsalDesteEffect()

    {

        Debug.Log("KutsalDeste effect stopped!");

        // TODO: Remove your visual effect here

        foreach (Transform child in kutsalDesteObject.transform)

        {

            child.GetComponent<FogController>().StopFog();

        }

    }



    public void ShowVerZehriEffect(int playerNumber, int points)

    {

        // Show effect/animation/notification for VerZehri

    }

    public void ShowKutsalDesteEffect(int playerNumber, int points)

    {

        // Show effect/animation/notification for KutsalDeste

    }



    public void UseBuDahaIyiPower()

    {

        if (currentSelectedHandCard == null || centerCards.Count == 0)

        {

            Debug.LogWarning("No card selected or center is empty!");

            return;

        }

        // Get the top card of the center pile (last added)

        string topCenterCardID = centerCards.Keys.Last();

        networkRelay.UseBuDahaIyiServerRPC(deckController.thisPlayerNumber, currentSelectedHandCard, topCenterCardID);

    }



    public void OnBuDahaIyiSynced(int playerNo, string handCardID, string centerCardID)

    {

        // Swap in myCards

        if (myCards.Contains(handCardID))

        {

            myCards.Remove(handCardID);

            myCards.Add(centerCardID);

        }

        // Swap in centerCards

        if (centerCards.ContainsKey(centerCardID))

        {

            int[] temp = centerCards[centerCardID];

            centerCards.Remove(centerCardID);

            centerCards[handCardID] = CardInteraction.cardLookup[handCardID].GetCardID();

        }

        else if (centerCards.ContainsKey(handCardID))

        {

            int[] temp = centerCards[handCardID];

            centerCards.Remove(handCardID);

            centerCards[centerCardID] = CardInteraction.cardLookup[centerCardID].GetCardID();

        }

        else

        {

            // Fallback: just swap the last card

            var lastKey = centerCards.Keys.Last();

            int[] temp = centerCards[lastKey];

            centerCards.Remove(lastKey);

            centerCards[handCardID] = CardInteraction.cardLookup[handCardID].GetCardID();

        }



        // Track power completion
        MoveChainIntegrator.ReportPowerCompletion("Bu Daha İyi", playerNo, $"Swapped hand card {handCardID} with center card {centerCardID}");
        
        // CRITICAL FIX: Force game state save after power completion to ensure reconnection sync
        if (Server.Singleton != null)
        {
            Debug.Log("[GameManager] Power effect completed - forcing game state save for reconnection sync");
            Server.Singleton.SaveCurrentGameState();
        }

        // Swap card objects visually

        deckController.SwapHandCardWithCenterCard(handCardID, centerCardID, playerNo);

    }



    // Add at the top of GameManager.cs

    public bool isSunuDegisTokusActive = false;

    public string sunuDegisTokusFirstCard = null;



    // Call this to activate the power

    // New dual selection method for Şunu Değiş Tokuş
    public void StartSunuDegisTokusDualSelection(string myHandCard)
    {
        Debug.Log($"[GameManager] StartSunuDegisTokusDualSelection - My card: {myHandCard}");
        
        isSunuDegisTokusActive = true;
        sunuDegisTokusFirstCard = myHandCard;
        
        // Start hand showcasing for dual selection
        Debug.Log("[Showcase] GameManager: Starting Şunu Değiş Tokuş dual selection showcase");
        if (SuperPowerSpawner.LocalInstance != null)
        {
            SuperPowerSpawner.LocalInstance.StartHandShowcaseForDualSelection("Şunu Değiş Tokuş");
            Debug.Log("[Showcase] GameManager: Called StartHandShowcaseForDualSelection for Şunu Değiş Tokuş");
        }
        else
        {
            Debug.LogError("[Showcase] GameManager: ERROR - SuperPowerSpawner.LocalInstance is null!");
        }
        
        Debug.Log("[GameManager] ŞunuDeğişTokuş: Select a card from another player's hand to swap with.");
    }
    
    public void ActivateSunuDegisTokusPower()
    {
        // This method is now obsolete - Şunu Değiş Tokuş uses dual selection
        Debug.LogWarning("[GameManager] ActivateSunuDegisTokusPower() called but this method is obsolete. Şunu Değiş Tokuş now uses dual selection.");
    }



    public IEnumerator OnSunuDegisTokusSynced(int myPlayerNo, int otherPlayerNo, string myHandCardID, string otherHandCardID)
    {
        // Track the card swap in move chain
        MoveChainIntegrator.TrackCardSwap(myPlayerNo, otherPlayerNo, myHandCardID, otherHandCardID, "Şunu Değiş Tokuş power");
        
        // Track power completion
        MoveChainIntegrator.ReportPowerCompletion("Şunu Değiş Tokuş", myPlayerNo, $"Swapped {myHandCardID} with {otherHandCardID} from P{otherPlayerNo}");
        
        // CRITICAL FIX: Force game state save after power completion to ensure reconnection sync
        if (Server.Singleton != null)
        {
            Debug.Log("[GameManager] Power effect completed - forcing game state save for reconnection sync");
            Server.Singleton.SaveCurrentGameState();
        }
        
        // Swap in myCards if relevant
        if (deckController.thisPlayerNumber == myPlayerNo)
        {
            int idx = myCards.IndexOf(myHandCardID);
            if (idx != -1)
            {
                myCards[idx] = otherHandCardID;
            }
        }
        else if (deckController.thisPlayerNumber == otherPlayerNo)
        {
            int idx = myCards.IndexOf(otherHandCardID);
            if (idx != -1)
            {
                myCards[idx] = myHandCardID;
            }
        }

        // Visual swap
        yield return StartCoroutine(deckController.SwapCardsBetweenPlayersByID(myPlayerNo, myHandCardID, otherPlayerNo, otherHandCardID, true));
        DeckController.LocalInstance.ExitShowcaseAllOtherHands();
        DeckController.LocalInstance.TryStopShowcaseCenterCards();
    }



    // Add at the top of GameManager.cs

    public bool isSunuDegisBunuTokusActive = false;

    private int sunuDegisBunuTokusSwapIndex = 0;

    private List<string> sunuDegisBunuTokusMyHandSnapshot = null;



    // Call this to activate the power

    public void ActivateSunuDegisBunuTokusPower()

    {

        if (myCards == null || myCards.Count == 0)

        {

            Debug.LogWarning("No cards in hand for ŞunuDeğişBunuTokuş!");

            return;

        }

        isSunuDegisBunuTokusActive = true;

        sunuDegisBunuTokusSwapIndex = 0;

        sunuDegisBunuTokusMyHandSnapshot = new List<string>(myCards);

        Debug.Log("ŞunuDeğişBunuTokuş: Select a card from another player's hand to swap with your first card.");

        // Start hand showcasing for multi-swap selection
        Debug.Log("[Showcase] GameManager: Starting Şunu Değiş Bunu Tokuş multi-swap showcase");
        if (SuperPowerSpawner.LocalInstance != null)
        {
            SuperPowerSpawner.LocalInstance.StartHandShowcaseForDualSelection("Şunu Değiş Bunu Tokuş");
            Debug.Log("[Showcase] GameManager: Called StartHandShowcaseForDualSelection for Şunu Değiş Bunu Tokuş");
        }
        else
        {
            Debug.LogError("[Showcase] GameManager: ERROR - SuperPowerSpawner.LocalInstance is null!");
        }

    }



    // Add this method to handle the synced swap

    public IEnumerator OnSunuDegisBunuTokusSynced(int myPlayerNo, int otherPlayerNo, string myHandCardID, string otherHandCardID, int myHandIndex, bool readyToExit = false)

    {
        // Track power completion for move chain synchronization
        MoveChainIntegrator.ReportPowerCompletion("Şunu Değiş Bunu Tokuş", myPlayerNo, $"Swapped {myHandCardID} with {otherHandCardID} from P{otherPlayerNo}");

        // Update myCards if relevant

        if (deckController.thisPlayerNumber == myPlayerNo)

        {

            int idx = myCards.IndexOf(myHandCardID);

            if (idx != -1)

            {

                myCards[idx] = otherHandCardID;

            }

        }

        else if (deckController.thisPlayerNumber == otherPlayerNo)

        {

            int idx = myCards.IndexOf(otherHandCardID);

            if (idx != -1)

            {

                myCards[idx] = myHandCardID;

            }

        }

        // Visual swap at the correct index

        yield return StartCoroutine(deckController.SwapCardsBetweenPlayersByID(myPlayerNo, myHandCardID, otherPlayerNo, otherHandCardID, true));

        if(readyToExit) 
        {
            //deckController.ExitShowcaseAllOtherHands();
            deckController.TryStopShowcaseCenterCards();
            
            // CRITICAL FIX: Force game state save after power completion to ensure reconnection sync
            if (Server.Singleton != null)
            {
                Debug.Log("[GameManager] Power effect completed - forcing game state save for reconnection sync");
                Server.Singleton.SaveCurrentGameState();
            }
        }

    }



    // Add this public method to allow CardInteraction to check swap power activeness

    public bool IsAnySwapPowerActive()

    {

        return isSunuDegisTokusActive || isSunuDegisBunuTokusActive;

    }



    public void SetCurrentSelectedHandCardNull()

    {

        AddToDebugLog($"[GameManager] SetCurrentSelectedHandCardNull called");

        AddToDebugLog($"[GameManager] currentSelectedHandCard before setting to null: {currentSelectedHandCard}");

        currentSelectedHandCard = null;

        hasAlreadySentRPC = false;

        AddToDebugLog($"[GameManager] currentSelectedHandCard after setting to null: {currentSelectedHandCard}");

        AddToDebugLog($"[GameManager] hasAlreadySentRPC reset to false");

    }

    /// <summary>
    /// Sets the current selected hand card (used by BotPlayer)
    /// </summary>
    public void SetCurrentSelectedHandCard(string cardID)
    {
        AddToDebugLog($"[GameManager] SetCurrentSelectedHandCard called with cardID: {cardID}");
        AddToDebugLog($"[GameManager] currentSelectedHandCard before setting: {currentSelectedHandCard}");
        
        currentSelectedHandCard = cardID;
        
        AddToDebugLog($"[GameManager] currentSelectedHandCard after setting: {currentSelectedHandCard}");
    }



    public string GetCurrentSelectedHandCard()

    {

        return currentSelectedHandCard;

    }



    public void NotifyZaferPuaniAtRoundEnd()

    {

        SuperPowerSpawner.LocalInstance.ReportZaferPuaniToServer();

    }



    public void OnKapkacCardChanged(string cardUniqueID)

    {

        // Set the card's value to 11 (Jack)

        if (CardInteraction.cardLookup.TryGetValue(cardUniqueID, out var cardInteraction))

        {

            // Track the superpower effect BEFORE applying it
            var effectData = new Dictionary<string, string>
            {
                ["oldValue"] = cardInteraction.GetCardID()[1].ToString(),
                ["newValue"] = "11",
                ["effectType"] = "cardValueChange"
            };
            MoveChainIntegrator.TrackSuperpowerEffect(currentPlayerNo, "Kapkaç", new[] { cardUniqueID }, effectData);

            int[] cardID = cardInteraction.GetCardID();

            cardID[1] = 11;

            cardInteraction.SetCardID(cardID);

            cardInteraction.activePowerEffect = "Kapkaç";
            // Track this card change for save/load persistence
            cardPowerEffects[cardUniqueID] = "Kapkaç";



            // Optionally, update the card's visual to indicate Kapkaç (e.g., highlight, effect)

            StartCoroutine(KapkacCourotine(cardUniqueID));

        }

    }

    /// <summary>
    /// Called when a player disconnects from the game
    /// </summary>
    public void OnPlayerDisconnected(ulong clientId, string reason)
    {
        Debug.LogWarning($"[GameManager] Player {clientId} disconnected: {reason}");
        
        // TODO: Implement bot placeholder system here
        // This is where you would:
        // 1. Create a bot to replace the disconnected player
        // 2. Update the UI to show the player is now a bot
        // 3. Handle the bot's turns automatically
        
        Debug.LogWarning($"[GameManager] Bot placeholder system should activate for disconnected player {clientId}");
    }

    public void ShowcaseSuperPower(string powerName, float fadeDuration = 0.5f, float displayDuration = 1f)

    {

        StartCoroutine(ShowcaseSuperPowerCoroutine(powerName, fadeDuration, displayDuration));

        ElHolderScript.LocalInstance.PlayTokenAnimationOnce(currentPlayerNo);   

        ElHolderScript.LocalInstance.ShowcasePower(currentPlayerNo, powerName);

    }



    private IEnumerator ShowcaseSuperPowerCoroutine(string powerName, float fadeDuration, float displayDuration)

    {

        yield return new WaitForSeconds(0f);

        if (superPowerText == null || superPowerTextGroup == null)

            yield break;



        // Assign text to both components

        superPowerText.text = powerName;

        if (superPowerTextB != null)

            superPowerTextB.text = powerName;



        superPowerTextGroup.gameObject.SetActive(true);



        // Fade in

        float elapsed = 0f;

        while (elapsed < fadeDuration)

        {

            elapsed += Time.deltaTime;

            superPowerTextGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);

            yield return null;

        }

        superPowerTextGroup.alpha = 1f;



        // Wait

        yield return new WaitForSeconds(displayDuration);



        // Fade out

        elapsed = 0f;

        while (elapsed < fadeDuration)

        {

            elapsed += Time.deltaTime;

            superPowerTextGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));

            yield return null;

        }

        superPowerTextGroup.alpha = 0f;

        superPowerTextGroup.gameObject.SetActive(false);

    }





    public void ShowPistiText(string message, float fadeDuration = 0.5f, float displayDuration = 2f)

    {

        StartCoroutine(ShowPistiTextCoroutine(message, fadeDuration, displayDuration));

    }



    private IEnumerator ShowPistiTextCoroutine(string message, float fadeDuration, float displayDuration)

    {

        if (pistiText == null || pistiTextGroup == null)

            yield break;



        // Assign text to both components

        pistiText.text = message;

        if (pistiTextB != null)

            pistiTextB.text = message;



        pistiTextGroup.gameObject.SetActive(true);



        // Fade in

        float elapsed = 0f;

        while (elapsed < fadeDuration)

        {

            elapsed += Time.deltaTime;

            pistiTextGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);

            yield return null;

        }

        pistiTextGroup.alpha = 1f;



        // Wait

        yield return new WaitForSeconds(displayDuration);



        // Fade out

        elapsed = 0f;

        while (elapsed < fadeDuration)

        {

            elapsed += Time.deltaTime;

            pistiTextGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));

            yield return null;

        }

        pistiTextGroup.alpha = 0f;

        pistiTextGroup.gameObject.SetActive(false);

    }





    public void ActivateYandimAnamPower()
    {
        // This method is now obsolete - Yandım Anam uses pre-selected cards
        Debug.LogWarning("[GameManager] ActivateYandimAnamPower() called but this method is obsolete. Yandım Anam now uses pre-selected cards.");
    }





    public void OnYandimAnamCardChanged(string cardUniqueID)

    {

        if (CardInteraction.cardLookup.TryGetValue(cardUniqueID, out var cardInteraction))

        {

            // Track the superpower effect BEFORE applying it
            var effectData = new Dictionary<string, string>
            {
                ["oldValue"] = cardInteraction.GetCardID()[1].ToString(),
                ["newValue"] = "0",
                ["effectType"] = "cardValueChange"
            };
            MoveChainIntegrator.TrackSuperpowerEffect(currentPlayerNo, "Yandım Anam", new[] { cardUniqueID }, effectData);

            StartCoroutine(YandimAnamCoroutine(cardUniqueID));

        }

    }



    private IEnumerator YandimAnamCoroutine(string cardUniqueID)

    {

        GameObject cardObj = CardInteraction.cardLookup[cardUniqueID].gameObject;

        var cardScript = CardInteraction.cardLookup[cardUniqueID];



        // --- Reset visuals before applying Yandım Anam effect ---

        ResetVisuals(cardScript);



        // 1. Play animation on top of the card

        GameObject anim = null;

        if (yandimAnamEffectPrefab != null)

        {

            anim = Instantiate(yandimAnamEffectPrefab, cardObj.transform);

            anim.transform.localPosition = new Vector3(0, 0, -0.02f); // Above the sprite

            anim.SetActive(true);

        }



        // 2. Add the overlay sprite below the animation, fade in

        GameObject effect = null;

        if (yandimAnamSpritePrefab != null)

        {

            effect = Instantiate(yandimAnamSpritePrefab, cardObj.transform);

            effect.transform.localPosition = new Vector3(0, 0, -0.002f); // Below the animation

            SpriteRenderer sr = effect.GetComponent<SpriteRenderer>();

            if (sr != null)

            {

                Color c = sr.color;

                c.a = 0f;

                sr.color = c;

                float fadeDuration = 0.5f;

                float elapsed = 0f;

                while (elapsed < fadeDuration)

                {

                    elapsed += Time.deltaTime;

                    c.a = Mathf.Clamp01(elapsed / fadeDuration);

                    sr.color = c;

                    yield return null;

                }

                c.a = 1f;

                sr.color = c;

            }

        }



        // 3. Change the value of the card to 0 (kind stays the same)

        cardScript.SetCardValue(0);



        // 4. Update the activePowerEffect

        cardScript.activePowerEffect = "YandımAnam";
        // Track this card change for save/load persistence
        cardPowerEffects[cardUniqueID] = "YandımAnam";



        // 5. Store references for copying visuals

        cardScript.yandimAnamEffectInstance = anim;

        cardScript.yandimAnamSpriteInstance = effect;



        // 6. Optionally, destroy the animation after it plays (if it's not looping)

        if (anim != null)

        {

            var ps = anim.GetComponent<ParticleSystem>();

            if (ps != null)

            {

                yield return new WaitForSeconds(ps.main.duration);

                Destroy(anim);

            }

            else

            {

                yield return new WaitForSeconds(1f);

                Destroy(anim);

            }

        }
        
        // 7. IMPORTANT: Clear any selection state to prevent automatic playing
        // This prevents the card from being automatically played after Yandım Anam
        if (CardInteraction.currentlySelectedCard == cardScript)
        {
            CardInteraction.currentlySelectedCard = null;
            CardInteraction.isOneCardSelected = false;
            if (GameManager.LocalInstance != null)
            {
                GameManager.LocalInstance.SetCurrentSelectedHandCardNull();
            }
        }

    }



    // ===== GAME STATE SYNC =====

    /// <summary>
    /// Applies a complete game state snapshot to synchronize the local client view
    /// </summary>
    public void ApplyGameState(SerializableGameState snapshot)
    {
        Debug.LogError("[Visual Sync] ===== APPLY GAME STATE CALLED =====");
        // Single comprehensive log for game state application start
        Debug.Log($"[GameManager] ===== ÖNEMLİ: APPLYING GAME STATE START =====\n" +
                 $"Snapshot version: {snapshot.snapshotVersion}\n" +
                 $"Game state details: turn={snapshot.turnCounter}, players={snapshot.playerCount}, centerCards={snapshot.center.items?.Length ?? 0}, hands={snapshot.hands.Count}\n" +
                 $"Current local state: turnCounter={turnCounter}, currentPlayerNo={currentPlayerNo}\n" +
                 $"Player number: {deckController?.thisPlayerNumber ?? -1}, isReconnecting: {isReconnecting}\n" +
                 $"DeckController found: {(deckController != null ? "YES" : "NO")}");
        
        // Add header entry to sync log without clearing it
        SyncLog($"=== APPLYING GAME STATE (snapshot v{snapshot.snapshotVersion}) ===");
        // Log current client-local state BEFORE applying snapshot for A/B comparison
        LogDetailedLocalState("CLIENT BEFORE APPLY");
        
        // Ensure a clean slate immediately before we start rebuild
        if (deckController != null)
        {
            Debug.LogError("[Visual Sync] Starting ApplyGameStateWithReset coroutine");
            StartCoroutine(ApplyGameStateWithReset(snapshot));
        }
        else
        {
            Debug.LogError("[GameManager] CRITICAL ERROR: deckController is null; cannot apply game state");
            SyncLogError("deckController is null; cannot apply game state");
        }
    }

    private IEnumerator ApplyGameStateWithReset(SerializableGameState snapshot)
    {
        Debug.LogError("[Visual Sync] ===== APPLY GAME STATE WITH RESET STARTED =====");
        // Hard reset the scene cards first so we do not stack
        SyncLog("Starting ResetCards to clear scene");
        yield return StartCoroutine(deckController.ResetCards());
        SyncLog("ResetCards completed");
        
        Debug.LogError("[Visual Sync] About to call ApplyGameStateCoroutine");
        // Then continue the usual coroutine
        yield return StartCoroutine(ApplyGameStateCoroutine(snapshot));
        Debug.LogError("[Visual Sync] ApplyGameStateCoroutine completed");
    }

    /// <summary>
    /// Coroutine to apply game state with proper sequencing and error handling
    /// </summary>
    private IEnumerator ApplyGameStateCoroutine(SerializableGameState snapshot)
    {
        SyncLog($"ApplyGameStateCoroutine started for snapshot v{snapshot.snapshotVersion}");
        LogSnapshotSummary(snapshot);

        // 1. Freeze input and stop animations (cannot yield in try with catch)
        SyncLog("Step 1: Freezing client for resync");
        FreezeClientForResync();

        bool success = false;
        try
        {
            // 2. Reset transient client state
            SyncLog("Step 2: Resetting transient client state");
            ResetTransientClientState();

            // 3. Apply core game state
            SyncLog("Step 3: Applying core game state");
            ApplyCoreGameState(snapshot);

            success = true;
        }
        finally
        {
            if (!success)
            {
                SyncLogError("Early failure during initial apply phase");
            }
        }

        // 4. Rebuild card containers
        SyncLog("Step 4: Rebuilding card containers");
        yield return StartCoroutine(RebuildCardContainers(snapshot));

        // NOTE: Visual restoration moved to AFTER desync detection completes
        // This ensures the move chain is properly synchronized before restoring visuals
        Debug.LogError("[Visual Sync] ===== SKIPPING VISUAL RESTORATION IN APPLY GAME STATE =====");
        Debug.LogError("[Visual Sync] Visual restoration will happen after desync detection completes");

        // 5. Apply effects and flags
        SyncLog("Step 5: Applying effects and flags");
        ApplyEffectsAndFlags(snapshot);

        // 6. Update UI and layout
        SyncLog("Step 6: Updating UI and layout");
        yield return StartCoroutine(UpdateUIAndLayout());

        // 7. Unfreeze input (ensure this always runs)
        SyncLog("Step 7: Unfreezing client after resync");
        UnfreezeClientAfterResync();

        // 8. Handle reconnection completion and sync mode
        SyncLog("Step 8: Checking if this was a reconnection and handling sync mode");
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            // Check if this was a reconnection by looking for saved game info
            var networkManagerUI = FindObjectOfType<NetworkManagerUI>();
            if (networkManagerUI != null && !string.IsNullOrEmpty(networkManagerUI.GetLastGameJoinCode()))
            {
                SyncLog("Step 8: This was a reconnection - notifying completion");
                OnReconnectionGameStateApplied();
            }
        }
        
        // 9. Stop sync mode if it was active (this will apply buffered moves)
        if (isSyncMode)
        {
            SyncLog("Step 9: Stopping sync mode and applying buffered moves");
            StopSyncMode();
        }

        // Single comprehensive log for game state application completion
        Debug.Log($"[GameManager] ===== ÖNEMLİ: APPLYING GAME STATE COMPLETED =====\n" +
                 $"Snapshot version: {snapshot.snapshotVersion}\n" +
                 $"Final state: turnCounter={turnCounter}, currentPlayerNo={currentPlayerNo}\n" +
                 $"Center cards: {centerCards.Count}, My cards: {myCards.Count}\n" +
                 $"Sync mode active: {isSyncMode}, Reconnecting: {isReconnecting}\n" +
                 $"Game state application process finished successfully");
        
        SyncLog($"ApplyGameStateCoroutine completed successfully for snapshot v{snapshot.snapshotVersion}");
        // Final detailed dump AFTER apply for comparison
        LogDetailedLocalState("CLIENT AFTER APPLY");
    }

    /// <summary>
    /// Freezes client input and animations during resync
    /// </summary>
    private void FreezeClientForResync()
    {
        Debug.Log("[GameManager] Freezing client for resync");
        
        // Stop all card tweens
        var allCards = FindObjectsOfType<CardInteraction>();
        foreach (var card in allCards)
        {
            if (card != null)
            {
                card.StopAutoRotate();
                // Kill any ongoing tweens if you have a method for this
                // card.KillAllTweens();
            }
        }

        // Disable input temporarily
        // You might want to add a flag to prevent card interactions during resync
    }

    /// <summary>
    /// Resets transient client-only state that shouldn't persist through resync
    /// </summary>
    private void ResetTransientClientState()
    {
        Debug.Log("[GameManager] Resetting transient client state");
        
        // Reset selection state
        currentSelectedHandCard = null;
        CardInteraction.currentlySelectedCard = null;
        CardInteraction.isOneCardSelected = false;

        // Reset movement flags
        movePlayedLocally = false;
        isProcessingCapture = false;

        // Reset special power states
        isKopyalaActive = false;
        kopyalaSourceCard = null;
        isSunuDegisTokusActive = false;
        sunuDegisTokusFirstCard = null;
        isSunuDegisBunuTokusActive = false;
        
        // Deactivate Vale Arar power at the end of round
        if (valeArarActive)
        {
            DeactivateValeArarPower();
            Debug.Log("[GameManager] Vale Arar deactivated at end of round");
        }
        
        // Close any open info boxes
        if (SuperPowerSpawner.LocalInstance != null)
        {
            StartCoroutine(SuperPowerSpawner.LocalInstance.CloseInfoBoxImmediate());
        }

        // Clear any pending lists
        cardObjectsToBeDiscarted.Clear();
        kapkacCardsToBeReset.Clear();
        cardPowerEffects.Clear();
        
        // CRITICAL: Reset move chains to prevent infinite desync loops
        // When we apply a full game state, the chains should be reset to match the new state
        // BUT: For reconnection, let the desync detection handle the synchronization naturally
        if (!isReconnecting)
        {
            MoveChainIntegrator.ResetChains();
            Debug.Log("[GameManager] Reset all move chains to prevent desync loops after full state sync");
        }
        else
        {
            Debug.Log("[GameManager] Skipping move chain reset for reconnection - letting desync detection handle synchronization");
        }
    }

    /// <summary>
    /// Applies core game state values
    /// </summary>
    private void ApplyCoreGameState(SerializableGameState snapshot)
    {
        Debug.Log("[GameManager] Applying core game state");
        
        // Update turn and player info from server snapshot
        currentPlayerNo = snapshot.currentPlayer;
        turnCounter = snapshot.turnCounter;
        roundCount = snapshot.roundCount;
        
        // CRITICAL FIX: Sync startingPlayerNoCounter with roundCount for reconnection
        // Both represent "how many rounds have been completed" and should be equal
        if (deckController != null)
        {
            deckController.SetStartingPlayerNoCounter(snapshot.roundCount);
            Debug.LogError($"[GAME STATE] Applied roundCount {snapshot.roundCount} to startingPlayerNoCounter for reconnection");
        }
        
        Debug.Log($"[GameManager] Applied server game state - currentPlayer: {currentPlayerNo}, turnCounter: {turnCounter}, roundCount: {roundCount}");
        
        // The server will handle sending the updated current player via existing RPC
        // We just need to trust that the snapshot is correct
    }

    /// <summary>
    /// NEW: Restores visual states by parsing move chain for power effects
    /// This runs after card containers are rebuilt but before applying current effects
    /// </summary>
    private IEnumerator RestoreVisualStatesFromMoveChain()
    {
        Debug.LogError("[Visual Sync] ===== RESTORE VISUAL STATES FROM MOVE CHAIN STARTED =====");
        SyncLog("RestoreVisualStatesFromMoveChain started");
        
        // Get the current move chain to parse for visual effects
        if (MoveChainTracker.ClientInstance == null)
        {
            Debug.LogError("[Visual Sync] ERROR: MoveChainTracker.ClientInstance is null - cannot restore visual states");
            SyncLogWarning("MoveChainTracker.ClientInstance is null - cannot restore visual states");
            yield break;
        }
        
        var moveChain = MoveChainTracker.ClientInstance.GetCurrentChain();
        Debug.LogError($"[Visual Sync] Move chain retrieved - version: {moveChain.chainVersion}, moves count: {moveChain.moves?.Length ?? 0}");
        
        if (moveChain.moves == null || moveChain.moves.Length == 0)
        {
            Debug.LogError("[Visual Sync] No moves in chain to process for visual state restoration");
            SyncLog("No moves in chain to process for visual state restoration");
            yield break;
        }
        
        Debug.LogError($"[Visual Sync] Processing {moveChain.moves.Length} moves from chain v{moveChain.chainVersion} for visual effects");
        SyncLog($"Processing {moveChain.moves.Length} moves from chain v{moveChain.chainVersion} for visual effects");
        
        int visualEffectsRestored = 0;
        int superPowerEffectMoves = 0;
        
        // Parse each move in the chain for visual-changing powers
        for (int i = 0; i < moveChain.moves.Length; i++)
        {
            var move = moveChain.moves[i];
            Debug.LogError($"[Visual Sync] Move {i}: Type={move.moveType}, Power={move.superPowerName}, Cards={string.Join(",", move.affectedCardIds ?? new string[0])}");
            
            if (move.moveType == GameMove.MoveType.SuperPower_Effect)
            {
                superPowerEffectMoves++;
                Debug.LogError($"[Visual Sync] Found SuperPower_Effect move: {move.superPowerName}");
                
                // This is a power effect - check if it changes visuals
                if (IsVisualChangingPower(move.superPowerName))
                {
                    Debug.LogError($"[Visual Sync] Power {move.superPowerName} is visual-changing - restoring effect");
                    yield return StartCoroutine(RestoreVisualEffectFromMove(move));
                    visualEffectsRestored++;
                    Debug.LogError($"[Visual Sync] Completed restoration for {move.superPowerName}");
                }
                else
                {
                    Debug.LogError($"[Visual Sync] Power {move.superPowerName} is NOT visual-changing - skipping");
                }
            }
        }
        
        Debug.LogError($"[Visual Sync] ===== RESTORE VISUAL STATES COMPLETED =====");
        Debug.LogError($"[Visual Sync] Total moves: {moveChain.moves.Length}, SuperPower_Effect moves: {superPowerEffectMoves}, Visual effects restored: {visualEffectsRestored}");
        SyncLog($"RestoreVisualStatesFromMoveChain completed - restored {visualEffectsRestored} visual effects");
    }
    
    /// <summary>
    /// Checks if a power changes card visuals and needs restoration
    /// </summary>
    private bool IsVisualChangingPower(string powerName)
    {
        bool isVisual = powerName == "Kopyala Yapıştır" || 
                       powerName == "Kapkaç" || 
                       powerName == "Yandım Anam";
        
        Debug.LogError($"[Visual Sync] IsVisualChangingPower({powerName}) = {isVisual}");
        return isVisual;
        // Add more powers here as needed
    }
    
    /// <summary>
    /// Restores visual effect from a specific move
    /// </summary>
    private IEnumerator RestoreVisualEffectFromMove(GameMove move)
    {
        Debug.LogError($"[Visual Sync] RestoreVisualEffectFromMove called for power: {move.superPowerName}");
        SyncLog($"Restoring visual effect for power: {move.superPowerName}");
        
        switch (move.superPowerName)
        {
            case "Kopyala Yapıştır":
                Debug.LogError("[Visual Sync] Calling RestoreKopyalaYapistirVisual");
                yield return StartCoroutine(RestoreKopyalaYapistirVisual(move));
                break;
                
            case "Kapkaç":
                Debug.LogError("[Visual Sync] Calling RestoreKapkacVisual");
                yield return StartCoroutine(RestoreKapkacVisual(move));
                break;
                
            case "Yandım Anam":
                Debug.LogError("[Visual Sync] Calling RestoreYandimAnamVisual");
                yield return StartCoroutine(RestoreYandimAnamVisual(move));
                break;
                
            default:
                Debug.LogError($"[Visual Sync] Unknown visual power: {move.superPowerName}");
                SyncLogWarning($"Unknown visual power: {move.superPowerName}");
                break;
        }
        
        Debug.LogError($"[Visual Sync] RestoreVisualEffectFromMove completed for {move.superPowerName}");
    }
    
    /// <summary>
    /// Restores Kopyala Yapıştır visual effect from move data
    /// </summary>
    private IEnumerator RestoreKopyalaYapistirVisual(GameMove move)
    {
        var effectData = move.superPowerData.ToDictionary();
        if (effectData == null || !effectData.ContainsKey("targetCard") || !effectData.ContainsKey("sourceCard"))
        {
            SyncLogWarning("Kopyala Yapıştır move missing target/source card data");
            yield break;
        }
        
        string targetCardId = effectData["targetCard"];
        string sourceCardId = effectData["sourceCard"];
        
        SyncLog($"Restoring Kopyala Yapıştır: {sourceCardId} -> {targetCardId}");
        
        // Use existing OnKopyalaYapistir method to restore visual state
        OnKopyalaYapistir(targetCardId, sourceCardId);
        
        yield return null; // Allow one frame for processing
    }
    
    /// <summary>
    /// Restores Kapkaç visual effect from move data
    /// </summary>
    private IEnumerator RestoreKapkacVisual(GameMove move)
    {
        Debug.LogError($"[Visual Sync] RestoreKapkacVisual called - affectedCardIds: {string.Join(",", move.affectedCardIds ?? new string[0])}");
        
        if (move.affectedCardIds == null || move.affectedCardIds.Length == 0)
        {
            Debug.LogError("[Visual Sync] ERROR: Kapkaç move missing affected card data");
            SyncLogWarning("Kapkaç move missing affected card data");
            yield break;
        }
        
        string cardId = move.affectedCardIds[0];
        Debug.LogError($"[Visual Sync] Restoring Kapkaç visual effect for card: {cardId}");
        SyncLog($"Restoring Kapkaç visual effect for card: {cardId}");
        
        // Check if card exists in lookup before calling
        if (!CardInteraction.cardLookup.ContainsKey(cardId))
        {
            Debug.LogError($"[Visual Sync] ERROR: Card {cardId} not found in cardLookup during Kapkaç restoration");
            yield break;
        }
        
        Debug.LogError($"[Visual Sync] Card {cardId} found in lookup - calling OnKapkacCardChanged");
        
        // Use existing OnKapkacCardChanged method to restore visual state
        OnKapkacCardChanged(cardId);
        
        Debug.LogError($"[Visual Sync] OnKapkacCardChanged completed for card: {cardId}");
        
        yield return null; // Allow one frame for processing
    }
    
    /// <summary>
    /// Restores Yandım Anam visual effect from move data
    /// </summary>
    private IEnumerator RestoreYandimAnamVisual(GameMove move)
    {
        if (move.affectedCardIds == null || move.affectedCardIds.Length == 0)
        {
            SyncLogWarning("Yandım Anam move missing affected card data");
            yield break;
        }
        
        string cardId = move.affectedCardIds[0];
        SyncLog($"Restoring Yandım Anam visual effect for card: {cardId}");
        
        // Use existing OnYandimAnamCardChanged method to restore visual state
        OnYandimAnamCardChanged(cardId);
        
        yield return null; // Allow one frame for processing
    }

    /// <summary>
    /// Rebuilds all card containers based on snapshot
    /// </summary>
    private IEnumerator RebuildCardContainers(SerializableGameState snapshot)
    {
        SyncLog("RebuildCardContainers started");

        var deckCtrl = deckController;
        if (deckCtrl == null)
        {
            SyncLogError("deckController is null during rebuild");
            yield break;
        }

        int playerCountLocal = deckCtrl.playerCount;
        int myNo = deckCtrl.thisPlayerNumber;
        SyncLog($"Local info: playerCount={playerCountLocal}, myNo={myNo}");
        
        if (playerCountLocal <= 0)
        {
            SyncLogWarning("playerCount is not set; skipping rebuild");
            yield break;
        }

        // 0) Clear current center tracking (we will rebuild it exactly)
        SyncLog("Clearing current center tracking");
        centerCards.Clear();
        centerCardsObjects.Clear();

        // 1) Rebuild center cards (order preserved from snapshot)
        var centerList = snapshot.center.ToList();
        SyncLog($"Rebuilding center: {centerList.Count} cards");
        
        // Ensure centerTransform is in correct position
        centerTransform.position = new Vector3(0, 50, 0);
        
        for (int i = 0; i < centerList.Count; i++)
        {
            string cardId = centerList[i];
            CardInteraction cardInteraction = null;
            
            // SIMPLIFIED: Just use the cardLookup directly - no mapping needed
            if (CardInteraction.cardLookup.TryGetValue(cardId, out cardInteraction))
            {
                // Card found in cardLookup
            }
            else
            {
                SyncLogWarning($"Center rebuild: card not found {cardId}");
                continue;
            }
            
            if (cardInteraction != null)
            {
                var cardObject = cardInteraction.gameObject;
                cardObject.transform.SetParent(centerTransform, false);
                centerCardsObjects.Add(cardObject);
                centerCards[cardId] = cardInteraction.GetCardID();
                
                // Apply proper positioning and rotation like DealCenter does
                Vector3 centerPosition = centerTransform.position;
                Vector3 centerRotation = centerTransform.rotation.eulerAngles;
                
                if (i == centerList.Count - 1) // Last card (top card) should be face up
                {
                    cardObject.transform.rotation = Quaternion.Euler(centerRotation.x + 180, centerRotation.y, UnityEngine.Random.Range(-12, 12));
                    cardObject.transform.position = new Vector3(centerPosition.x, centerPosition.y + 10, centerPosition.z);
                }
                else
                {
                    cardObject.transform.rotation = Quaternion.Euler(centerRotation.x, centerRotation.y, UnityEngine.Random.Range(-12, 12));
                    cardObject.transform.position = new Vector3(centerPosition.x, centerPosition.y, centerPosition.z);
                }
                
                cardObject.transform.localScale = new Vector3(deckCtrl.centerScale, deckCtrl.centerScale, deckCtrl.centerScale);
                
                SyncLog($"Center card rebuilt: {cardId} -> [{cardInteraction.GetCardID()[0]},{cardInteraction.GetCardID()[1]}] (face up: {i == centerList.Count - 1})");
            }
            else
            {
                SyncLogWarning($"Center rebuild: card not found {cardId}");
            }
        }
        SyncLog($"Center rebuild complete: {centerCardsObjects.Count} cards placed");

        // 2) Remap hands from absolute player numbers to relative indices for this client
        var absHands = snapshot.hands.ToDictionary();
        var relHands = new Dictionary<int, List<string>>();
        SyncLog("Remapping hands from absolute to relative indices");
        foreach (var kvp in absHands)
        {
            int absPlayerNo = kvp.Key;
            int relIndex = (absPlayerNo - myNo + playerCountLocal) % playerCountLocal;
            relHands[relIndex] = new List<string>(kvp.Value);
            SyncLog($"Hand mapping: abs P{absPlayerNo} -> rel {relIndex} ({kvp.Value.Count} cards)");
        }

        // 3) Rebuild player hands via existing helper
        SyncLog("Calling AssignCardsToPlayerHands");
        deckCtrl.AssignCardsToPlayerHands(relHands);
        SyncLog("AssignCardsToPlayerHands completed");

        // 3.1) Force local player's cards to face up (crucial for proper display)
        if (relHands.ContainsKey(0))
        {
            var myCardObjects = new List<GameObject>();
            foreach (var uid in relHands[0])
            {
                if (CardInteraction.cardLookup.TryGetValue(uid, out var ci))
                {
                    myCardObjects.Add(ci.gameObject);
                }
            }
            if (myCardObjects.Count > 0)
            {
                SyncLog($"Starting auto-rotate for {myCardObjects.Count} local player cards");
                StartCoroutine(deckCtrl.SetAutoRotateFlagTrue(myCardObjects));
            }
        }

        // 4) Remap pools similarly (absolute -> relative) and assign
        var absPools = snapshot.pools.ToDictionary();
        var relPools = new Dictionary<int, List<string>>();
        SyncLog("Remapping pools from absolute to relative indices");
        foreach (var kvp in absPools)
        {
            int absPlayerNo = kvp.Key;
            int relIndex = (absPlayerNo - myNo + playerCountLocal) % playerCountLocal;
            relPools[relIndex] = new List<string>(kvp.Value);
            SyncLog($"Pool mapping: abs P{absPlayerNo} -> rel {relIndex} ({kvp.Value.Count} cards)");
        }
        
        SyncLog("Calling AssignCardsToPlayerPools");
        deckCtrl.AssignCardsToPlayerPools(new SerializableDictionary(relPools));
        SyncLog("AssignCardsToPlayerPools completed");

        // 5) Handle bombed cards (cards outside normal game flow)
        var bombedList = snapshot.bombStack.ToList();
        SyncLog($"Rebuilding bombed cards: {bombedList.Count} cards");
        foreach (string cardId in bombedList)
        {
            if (CardInteraction.cardLookup.ContainsKey(cardId))
            {
                var cardInteraction = CardInteraction.cardLookup[cardId];
                var cardObject = cardInteraction.gameObject;
                
                // Move to BombedStack GameObject (outside normal game flow)
                GameObject bombedStack = GameObject.Find("BombedStack");
                if (bombedStack != null)
                {
                    cardObject.transform.SetParent(bombedStack.transform, false);
                    cardObject.transform.localPosition = Vector3.zero;
                    cardObject.transform.localScale = new Vector3(deckCtrl.centerScale, deckCtrl.centerScale, deckCtrl.centerScale);
                    SyncLog($"Bombed card rebuilt: {cardId} -> BombedStack");
                }
                else
                {
                    SyncLogWarning($"BombedStack GameObject not found, card {cardId} left unparented");
                }
            }
            else
            {
                SyncLogWarning($"Bombed card rebuild: card not found {cardId}");
            }
        }
        SyncLog($"Bombed cards rebuild complete: {bombedList.Count} cards placed");

        // 6) Layout update after parenting
        SyncLog("Calling UpdateCurrentPlayerHandLayout");
        deckCtrl.UpdateCurrentPlayerHandLayout();
        SyncLog("UpdateCurrentPlayerHandLayout completed");

        // Log final state
        LogLocalStateSummary("AFTER rebuild");

        yield return null; // settle one frame
        SyncLog("RebuildCardContainers completed");
    }

    /// <summary>
    /// Applies effect flags and special states
    /// </summary>
    private void ApplyEffectsAndFlags(SerializableGameState snapshot)
    {
        Debug.Log("[GameManager] Applying effects and flags");
        
        // Apply special power states via existing methods
        SetYapamazsınActive(snapshot.isYapamazsınActive);
        SetOynayamazsinActive(snapshot.oynayamazsinActive);
        SetVerZehriActive(snapshot.verZehriActive);
        SetKutsalDesteActive(snapshot.kutsalDesteActive);
        
        // Apply client-side superpower states
        isKapkacPending = snapshot.isKapkacPending;
        isYandimAnamPending = snapshot.isYandimAnamPending;
        isKopyalaActive = snapshot.isKopyalaActive;
        isSunuDegisTokusActive = snapshot.isSunuDegisTokusActive;
        isSunuDegisBunuTokusActive = snapshot.isSunuDegisBunuTokusActive;

        // Apply card power effects (Kapkaç, Yandım Anam, etc.)
        cardPowerEffects = snapshot.cardPowerEffects.ToDictionary();
        ApplyCardPowerEffects();

        // CRITICAL FIX: Apply copied card map to restore Kopyala Yapıştır visual changes
        var copiedCards = snapshot.copiedCardMap.ToDictionary();
        ApplyCopiedCardMap(copiedCards);
    }

    /// <summary>
    /// Applies copied card map to restore Kopyala Yapıştır visual changes after reconnection
    /// </summary>
    private void ApplyCopiedCardMap(Dictionary<string, string> copiedCards)
    {
        if (copiedCards == null || copiedCards.Count == 0) return;
        
        Debug.Log($"[GameManager] Applying copied card map with {copiedCards.Count} copied cards");
        
        foreach (var kvp in copiedCards)
        {
            string targetCardID = kvp.Key;
            string sourceCardID = kvp.Value;
            
            Debug.Log($"[GameManager] Restoring copied card: {targetCardID} <- {sourceCardID}");
            
            // Find the target and source cards
            if (CardInteraction.cardLookup.TryGetValue(targetCardID, out var targetCard) &&
                CardInteraction.cardLookup.TryGetValue(sourceCardID, out var sourceCard))
            {
                // Restore the visual appearance
                int[] newCardID = sourceCard.GetCardID();
                Sprite newSprite = sourceCard.GetComponent<SpriteRenderer>().sprite;
                
                // Apply the visual change
                targetCard.SetCardIDAndSprite(newCardID, newSprite);
                targetCard.activePowerEffect = sourceCard.activePowerEffect;
                
                Debug.Log($"[GameManager] Successfully restored copied card {targetCardID} to look like {sourceCardID}");
            }
            else
            {
                Debug.LogWarning($"[GameManager] Could not find cards for copied mapping: {targetCardID} <- {sourceCardID}");
            }
        }
        
        Debug.Log("[GameManager] Copied card map application complete");
    }

    /// <summary>
    /// Applies card power effects (Kapkaç, Yandım Anam) to cards after loading game state
    /// </summary>
    private void ApplyCardPowerEffects()
    {
        if (cardPowerEffects == null) return;

        foreach (var kvp in cardPowerEffects)
        {
            string cardUniqueID = kvp.Key;
            string powerEffect = kvp.Value;

            if (CardInteraction.cardLookup.TryGetValue(cardUniqueID, out var cardInteraction))
            {
                cardInteraction.activePowerEffect = powerEffect;

                switch (powerEffect)
                {
                    case "Kapkaç":
                        // Set card value to 11 (Jack)
                        int[] cardID = cardInteraction.GetCardID();
                        cardID[1] = 11;
                        cardInteraction.SetCardID(cardID);
                        // Note: Visual effects will be handled by the card's existing logic
                        break;

                    case "YandımAnam":
                        // Set card value to 0
                        cardInteraction.SetCardValue(0);
                        // Note: Visual effects will be handled by the card's existing logic
                        break;

                    default:
                        Debug.LogWarning($"Unknown power effect: {powerEffect} for card {cardUniqueID}");
                        break;
                }
            }
            else
            {
                Debug.LogWarning($"Card {cardUniqueID} not found in cardLookup when applying power effect {powerEffect}");
            }
        }
    }

    /// <summary>
    /// Updates UI and layout after state application
    /// </summary>
    private IEnumerator UpdateUIAndLayout()
    {
        Debug.Log("[GameManager] Updating UI and layout");
        
        // Update hand layouts
        if (deckController != null)
        {
            deckController.UpdateCurrentPlayerHandLayout();
        }

        // Update any UI elements that depend on game state
        // (score displays, turn indicators, etc.)

        yield return new WaitForSeconds(0.1f); // Give time for layout to settle
    }

    /// <summary>
    /// Unfreezes client after successful resync
    /// </summary>
    private void UnfreezeClientAfterResync()
    {
        Debug.Log("[GameManager] Unfreezing client after resync");
        
        // Re-enable auto-rotation for local player's cards
        if (deckController != null)
        {
            // The existing layout methods should handle auto-rotation setup
            deckController.UpdateCurrentPlayerHandLayout();
        }

        // Re-enable input and interactions
        // You might want to add any additional cleanup here
    }

    // ===== SYNC LOGGING HELPERS =====
    
    private void LogSnapshotSummary(SerializableGameState snapshot)
    {
        var deckCtrl = deckController;
        int myNo = deckCtrl?.thisPlayerNumber ?? -1;
        int playerCountLocal = deckCtrl?.playerCount ?? 0;
        
        var centerList = snapshot.center.ToList();
        SyncLog($"Snapshot v{snapshot.snapshotVersion} (myNo={myNo}, playerCount={playerCountLocal})");
        SyncLog($"Snapshot center({centerList.Count}): [{string.Join(", ", centerList)}]");

        var handsAbs = snapshot.hands.ToDictionary();
        foreach (var kvp in handsAbs)
        {
            SyncLog($"Snapshot hands abs P{kvp.Key} ({kvp.Value.Count}): [{string.Join(", ", kvp.Value)}]");
        }
        
        var poolsAbs = snapshot.pools.ToDictionary();
        foreach (var kvp in poolsAbs)
        {
            SyncLog($"Snapshot pools abs P{kvp.Key} ({kvp.Value.Count}): [{string.Join(", ", kvp.Value)}]");
        }
        
        var bombedList = snapshot.bombStack.ToList();
        SyncLog($"Snapshot bombed ({bombedList.Count}): [{string.Join(", ", bombedList)}]");
        
        // Superpower states
        SyncLog($"Snapshot superpowers: verZehri={snapshot.verZehriActive}, kutsalDeste={snapshot.kutsalDesteActive}, oynayamazsin={snapshot.oynayamazsinPending}, blockCount={snapshot.blockCount}");
        SyncLog($"Snapshot client powers: kapkac={snapshot.isKapkacPending}, yandimAnam={snapshot.isYandimAnamPending}, kopyala={snapshot.isKopyalaActive}, sunuDegis={snapshot.isSunuDegisTokusActive}, sunuDegisBunu={snapshot.isSunuDegisBunuTokusActive}");
        
        // Card power effects
        var cardEffects = snapshot.cardPowerEffects.ToDictionary();
        if (cardEffects.Count > 0)
        {
            SyncLog($"Snapshot card power effects ({cardEffects.Count}): [{string.Join(", ", cardEffects.Select(kvp => $"{kvp.Key}:{kvp.Value}"))}]");
        }
    }

    private void LogLocalStateSummary(string label)
    {
        var deckCtrl = deckController;
        if (deckCtrl == null) return;
        
        int playerCountLocal = deckCtrl.playerCount;
        SyncLog($"Local state {label}");
        
        // Center
        var centerNow = CollectCenterUniqueIds();
        SyncLog($"Local center({centerNow.Count}): [{string.Join(", ", centerNow)}]");
        
        // Hands and pools by relative index
        for (int rel = 0; rel < playerCountLocal; rel++)
        {
            var hand = CollectHandUniqueIdsByRel(rel);
            SyncLog($"Local hand rel{rel} ({hand.Count}): [{string.Join(", ", hand)}]");
            
            var pool = CollectPoolUniqueIdsByRel(rel);
            SyncLog($"Local pool rel{rel} ({pool.Count}): [{string.Join(", ", pool)}]");
        }
    }

    // ===== EXTRA DETAILED LOGGING FOR SYNC DIAGNOSTICS =====

    public void LogSnapshotForSyncLogs(SerializableGameState snapshot, string label)
    {
        SyncLog($"=== {label} ===");
        LogSnapshotSummary(snapshot);
    }

    /// <summary>
    /// Captures current client-side superpower states and sends them to server for saving
    /// </summary>
    public void CaptureClientSuperpowerStatesForSave()
    {
        // This method can be called before saving to ensure client states are captured
        // For now, the states are captured during ApplyGameState, but this could be enhanced
        // to send current states to server before save if needed
        Debug.Log($"[GameManager] Current client superpower states: kapkac={isKapkacPending}, yandimAnam={isYandimAnamPending}, kopyala={isKopyalaActive}, sunuDegis={isSunuDegisTokusActive}, sunuDegisBunu={isSunuDegisBunuTokusActive}");
    }

    private void LogDetailedLocalState(string label)
    {
        var deckCtrl = deckController;
        SyncLog($"--- DETAILED LOCAL STATE: {label} ---");

        // Basic info
        SyncLog($"currentPlayerNo={currentPlayerNo}, turnCounter={turnCounter}");

        // Center (dictionary + objects order)
        try
        {
            SyncLog($"centerCards dict ({centerCards.Count}): [{string.Join(", ", centerCards.Select(kvp => kvp.Key + ":" + kvp.Value[0] + "_" + kvp.Value[1]))}]");
        }
        catch (Exception ex)
        {
            SyncLogWarning($"centerCards dict log failed: {ex.Message}");
        }
        try
        {
            var centerOrder = CollectCenterUniqueIds();
            SyncLog($"centerCardsObjects order ({centerOrder.Count}): [{string.Join(", ", centerOrder)}]");
        }
        catch (Exception ex)
        {
            SyncLogWarning($"centerCardsObjects log failed: {ex.Message}");
        }

        // My cards (client's known hand list)
        try
        {
            if (myCards != null)
            {
                var myCardsWithVals = myCards.Select(id =>
                {
                    if (CardInteraction.cardLookup.TryGetValue(id, out var ci))
                    {
                        var cid = ci.GetCardID();
                        return id + "(" + cid[0] + "_" + cid[1] + ")";
                    }
                    return id + "(n/a)";
                }).ToList();
                SyncLog($"myCards ({myCards.Count}): [{string.Join(", ", myCardsWithVals)}]");
            }
            else
            {
                SyncLog("myCards is null");
            }
        }
        catch (Exception ex)
        {
            SyncLogWarning($"myCards log failed: {ex.Message}");
        }

        // Player hand transforms (children that actually hold cards)
        try
        {
            if (deckCtrl != null && deckCtrl.playerHandTransforms != null)
            {
                for (int i = 0; i < deckCtrl.playerHandTransforms.Count; i++)
                {
                    var t = deckCtrl.playerHandTransforms[i];
                    var children = CollectCardsUnderTransform(t);
                    SyncLog($"handTransform[{i}] '{t?.name}' cards ({children.Count}): [{string.Join(", ", children)}]");
                }
            }
        }
        catch (Exception ex)
        {
            SyncLogWarning($"playerHandTransforms log failed: {ex.Message}");
        }

        // Player pool transforms
        try
        {
            if (deckCtrl != null && deckCtrl.playerPoolTransforms != null)
            {
                for (int i = 0; i < deckCtrl.playerPoolTransforms.Count; i++)
                {
                    var t = deckCtrl.playerPoolTransforms[i];
                    var children = CollectCardsUnderTransform(t);
                    SyncLog($"poolTransform[{i}] '{t?.name}' cards ({children.Count}): [{string.Join(", ", children)}]");
                }
            }
        }
        catch (Exception ex)
        {
            SyncLogWarning($"playerPoolTransforms log failed: {ex.Message}");
        }

        // Pişti pool transforms (if used)
        try
        {
            if (playerPiştiPoolTransforms != null)
            {
                for (int i = 0; i < playerPiştiPoolTransforms.Count; i++)
                {
                    var t = playerPiştiPoolTransforms[i];
                    var children = CollectCardsUnderTransform(t);
                    SyncLog($"pistiPoolTransform[{i}] '{t?.name}' cards ({children.Count}): [{string.Join(", ", children)}]");
                }
            }
        }
        catch (Exception ex)
        {
            SyncLogWarning($"playerPiştiPoolTransforms log failed: {ex.Message}");
        }

        // Sanity: card lookup count
        SyncLog($"CardInteraction.cardLookup count={CardInteraction.cardLookup?.Count ?? 0}");

        // Client-side superpower states
        SyncLog($"Client superpowers: kapkac={isKapkacPending}, yandimAnam={isYandimAnamPending}, kopyala={isKopyalaActive}, sunuDegis={isSunuDegisTokusActive}, sunuDegisBunu={isSunuDegisBunuTokusActive}");

        // Card power effects
        if (cardPowerEffects != null && cardPowerEffects.Count > 0)
        {
            SyncLog($"Client card power effects ({cardPowerEffects.Count}): [{string.Join(", ", cardPowerEffects.Select(kvp => $"{kvp.Key}:{kvp.Value}"))}]");
        }

        // Bombed cards (outside normal game flow)
        try
        {
            GameObject bombedStack = GameObject.Find("BombedStack");
            if (bombedStack != null)
            {
                var bombedChildren = CollectCardsUnderTransform(bombedStack.transform);
                SyncLog($"bombedStack '{bombedStack.name}' cards ({bombedChildren.Count}): [{string.Join(", ", bombedChildren)}]");
            }
            else
            {
                SyncLog("bombedStack GameObject not found");
            }
        }
        catch (Exception ex)
        {
            SyncLogWarning($"bombedStack log failed: {ex.Message}");
        }
    }

    private List<string> CollectCardsUnderTransform(Transform parent)
    {
        var ids = new List<string>();
        if (parent == null) return ids;
        int idx = 0;
        foreach (Transform child in parent)
        {
            var ci = child.GetComponent<CardInteraction>();
            if (ci != null)
            {
                var cardID = ci.GetCardID();
                ids.Add($"{idx}:{ci.uniqueCardInstanceID}({cardID[0]}_{cardID[1]})");
            }
            idx++;
        }
        return ids;
    }

    private List<string> CollectCenterUniqueIds()
    {
        var result = new List<string>();
        foreach (var go in centerCardsObjects)
        {
            if (go == null) continue;
            var ci = go.GetComponent<CardInteraction>();
            if (ci != null) result.Add(ci.uniqueCardInstanceID);
        }
        return result;
    }

    private List<string> CollectHandUniqueIdsByRel(int relIndex)
    {
        var result = new List<string>();
        var deckCtrl = deckController;
        if (deckCtrl == null) return result;
        
        int poolIdx = (deckCtrl.playerCount == 4) ? relIndex : (relIndex == 0 ? 0 : 2);
        if (poolIdx < 0 || poolIdx >= deckCtrl.playerHandTransforms.Count) return result;
        
        var hand = deckCtrl.playerHandTransforms[poolIdx];
        int c = 0;
        foreach (Transform child in hand)
        {
            // Skip pool/extra first two children as per layout logic
            if (c > 1)
            {
                var ci = child.GetComponent<CardInteraction>();
                if (ci != null) result.Add(ci.uniqueCardInstanceID);
            }
            c++;
        }
        return result;
    }

    private List<string> CollectPoolUniqueIdsByRel(int relIndex)
    {
        var result = new List<string>();
        var deckCtrl = deckController;
        if (deckCtrl == null) return result;
        
        int poolIdx = (deckCtrl.playerCount == 4) ? relIndex : (relIndex == 0 ? 0 : 2);
        if (poolIdx < 0 || poolIdx >= deckCtrl.playerPoolTransforms.Count) return result;
        
        var pool = deckCtrl.playerPoolTransforms[poolIdx];
        foreach (Transform child in pool)
        {
            var ci = child.GetComponent<CardInteraction>();
            if (ci != null) result.Add(ci.uniqueCardInstanceID);
        }
        return result;
    }

    // === DESYNC TESTING FUNCTIONS ===
    
    /// <summary>
    /// Test function: Plays first card locally without sending to server (creates desync)
    /// </summary>
    [ContextMenu("Test Desync - Play Card Locally Only")]
    public void TestDesyncLocalCardPlay()
    {
        if (myCards == null || myCards.Count == 0)
        {
            Debug.LogWarning("[GameManager] No cards in hand to test with!");
            return;
        }

        string testCardId = myCards[0];
        Debug.Log($"[GameManager] Testing desync: Playing card {testCardId} locally only");
        
        // Record the move locally (this will create a desync)
        if (MoveChainTracker.ClientInstance != null)
        {
            var cardData = CardInteraction.cardLookup[testCardId].GetCardID();
            MoveChainTracker.ClientInstance.RecordCardPlay(deckController.thisPlayerNumber, testCardId, cardData, new string[0], 0);
            
            // Actually play the card visually in the scene (add to center)
            if (CardInteraction.cardLookup.ContainsKey(testCardId))
            {
                var cardInteraction = CardInteraction.cardLookup[testCardId];
                var cardObject = cardInteraction.gameObject;
                
                // Move card to center visually
                cardObject.transform.SetParent(centerTransform, false);
                centerCardsObjects.Add(cardObject);
                centerCards[testCardId] = cardData;
                
                // Position the card in center
                Vector3 centerPosition = centerTransform.position;
                Vector3 centerRotation = centerTransform.rotation.eulerAngles;
                cardObject.transform.rotation = Quaternion.Euler(centerRotation.x + 180, centerRotation.y, UnityEngine.Random.Range(-12, 12));
                cardObject.transform.position = new Vector3(centerPosition.x, centerPosition.y + 10, centerPosition.z);
                cardObject.transform.localScale = new Vector3(deckController.centerScale, deckController.centerScale, deckController.centerScale);
            }
            
            // Remove card from hand locally
            myCards.RemoveAt(0);
            
            // Update hand layout to reflect the removed card
            if (deckController != null)
            {
                deckController.UpdateCurrentPlayerHandLayout();
            }
            
            Debug.LogWarning($"[GameManager] DESYNC TEST: Card {testCardId} played locally but NOT sent to server!");
            Debug.LogWarning("[GameManager] This will create a desync when server and client chains are compared.");
            Debug.LogWarning("[GameManager] The card should now be visible in the center of the screen.");
        }
        else
        {
            Debug.LogError("[GameManager] MoveChainTracker not found! Cannot test desync.");
        }
    }
    
    /// <summary>
    /// Test function: Simulates a superpower effect locally without server sync
    /// </summary>
    [ContextMenu("Test Desync - Superpower Effect Locally Only")]
    public void TestDesyncLocalSuperpowerEffect()
    {
        if (myCards == null || myCards.Count == 0)
        {
            Debug.LogWarning("[GameManager] No cards in hand to test with!");
            return;
        }

        string testCardId = myCards[0];
        Debug.Log($"[GameManager] Testing desync: Applying Kapkaç effect to {testCardId} locally only");
        
        // Record the superpower effect locally (this will create a desync)
        if (MoveChainTracker.ClientInstance != null)
        {
            var effectData = new Dictionary<string, string>
            {
                ["oldValue"] = "5", // Fake old value
                ["newValue"] = "11",
                ["effectType"] = "cardValueChange"
            };
            MoveChainTracker.ClientInstance.RecordSuperpowerEffect(deckController.thisPlayerNumber, "Kapkaç", new[] { testCardId }, effectData);
            
            Debug.LogWarning($"[GameManager] DESYNC TEST: Kapkaç effect applied to {testCardId} locally but NOT synced with server!");
            Debug.LogWarning("[GameManager] This will create a desync when server and client chains are compared.");
        }
        else
        {
            Debug.LogError("[GameManager] MoveChainTracker not found! Cannot test desync.");
        }
    }
    
    /// <summary>
    /// Test function: Validates current client chain against server chain
    /// </summary>
    [ContextMenu("Test Chain Validation")]
    public void TestChainValidation()
    {
        if (MoveChainTracker.ClientInstance == null)
        {
            Debug.LogError("[GameManager] Client MoveChainTracker not found!");
            return;
        }

        if (MoveChainTracker.ServerInstance == null)
        {
            Debug.LogError("[GameManager] Server MoveChainTracker not found!");
            return;
        }

        var clientChain = MoveChainTracker.ClientInstance.GetCurrentChain();
        var serverChain = MoveChainTracker.ServerInstance.GetCurrentChain();
        
        Debug.Log($"[GameManager] === CHAIN VALIDATION TEST ===");
        Debug.Log($"[GameManager] Client chain: {clientChain.chainVersion} moves");
        Debug.Log($"[GameManager] Server chain: {serverChain.chainVersion} moves");
        
        var validationResult = clientChain.ValidateAgainst(serverChain, out int mismatchIndex);
        bool isValid = validationResult == MoveChain.ValidationResult.Valid;
        
        if (isValid)
        {
            Debug.Log("[GameManager] ✅ Chains are in sync!");
        }
        else
        {
            Debug.LogError($"[GameManager] ❌ DESYNC DETECTED at move index {mismatchIndex}!");
            Debug.LogError($"[GameManager] Client chain version: {clientChain.chainVersion}");
            Debug.LogError($"[GameManager] Server chain version: {serverChain.chainVersion}");
            Debug.LogError($"[GameManager] Validation result: {validationResult}");
        }
    }
    
    /// <summary>
    /// Test function: Manually reset move chains (useful for testing)
    /// </summary>
    [ContextMenu("Reset Move Chains")]
    public void ResetMoveChains()
    {
        MoveChainIntegrator.ResetChains();
        Debug.Log("[GameManager] Manually reset all move chains");
    }

    // ===== ENHANCED RECONNECTION: SYNC MODE & MOVE BUFFERING =====
    
    /// <summary>
    /// Starts sync mode - moves will be buffered instead of applied immediately
    /// </summary>
    public void StartSyncMode()
    {
        Debug.Log("[GameManager] Starting sync mode - moves will be buffered");
        isSyncMode = true;
        moveBuffer.StartBuffering();
        
        // Subscribe to buffer events
        moveBuffer.OnMoveBuffered += OnMoveBuffered;
        moveBuffer.OnBufferedMovesApplied += OnBufferedMovesApplied;
        moveBuffer.OnMoveTimedOut += OnMoveTimedOut;
        
        // ENHANCED RECONNECTION: Ensure UI is in correct state during sync
        EnsureCorrectUIStateForReconnection();
    }
    
    /// <summary>
    /// Ensures UI is in correct state for reconnection (closes win screens, etc.)
    /// This replicates the normal game initialization flow
    /// </summary>
    public void EnsureCorrectUIStateForReconnection()
    {
        Debug.Log("[GameManager] ===== ENSURING UI STATE FOR RECONNECTION =====");
        Debug.Log("[GameManager] Replicating normal game initialization flow");
        
        // STEP 1: Close waiting screen (like in InitialGameManagerSetUp)
        if (waitingScreen != null)
        {
            if (waitingScreen.activeSelf)
            {
                Debug.Log("[GameManager] ✓ Closing waiting screen during reconnection");
                waitingScreen.SetActive(false);
            }
            else
            {
                Debug.Log("[GameManager] ✓ Waiting screen already closed");
            }
        }
        else
        {
            Debug.LogError("[GameManager] CRITICAL ERROR: waitingScreen is null!");
        }
        
        // STEP 2: Close win screen (like in InitializeCardPrefabs)
        if (winScreen != null)
        {
            if (winScreen.activeSelf)
            {
                Debug.Log("[GameManager] ✓ Closing win screen during reconnection");
                winScreen.SetActive(false);
            }
            else
            {
                Debug.Log("[GameManager] ✓ Win screen already closed");
            }
        }
        else
        {
            Debug.LogError("[GameManager] CRITICAL ERROR: winScreen is null!");
        }
        
        // STEP 3: Ensure main screen is active (game view)
        if (mainScreen != null)
        {
            if (!mainScreen.activeSelf)
            {
                Debug.Log("[GameManager] ✓ Activating main screen during reconnection");
                mainScreen.SetActive(true);
            }
            else
            {
                Debug.Log("[GameManager] ✓ Main screen already active");
            }
        }
        else
        {
            Debug.LogError("[GameManager] CRITICAL ERROR: mainScreen is null!");
        }
        
        // STEP 4: Reset point texts to current values (not "0" since game is ongoing)
        if (pointTexts != null && pointTexts.Count >= 2)
        {
            Debug.Log($"[GameManager] ✓ Point texts ready (count: {pointTexts.Count})");
            // Don't reset to "0" - the game state will set the correct values
            // Just ensure they exist and are ready
        }
        else
        {
            Debug.LogError("[GameManager] CRITICAL ERROR: pointTexts is null or empty!");
        }
        
        // STEP 5: Reset any game state flags that might interfere
        movePlayedLocally = false;
        isProcessingCapture = false;
        Debug.Log("[GameManager] ✓ Game state flags reset");
        
        // STEP 6: Ensure ElHolderScript is in correct state
        if (ElHolderScript.LocalInstance != null)
        {
            Debug.Log("[GameManager] ✓ ElHolderScript found and ready");
            // Don't call ReturnAllHandsToIdle() as it might interfere with game state
        }
        else
        {
            Debug.LogWarning("[GameManager] ElHolderScript.LocalInstance is null");
        }
        
        Debug.Log("[GameManager] ===== UI STATE CORRECTED FOR RECONNECTION =====");
    }
    
    /// <summary>
    /// Stops sync mode and applies any buffered moves
    /// </summary>
    public void StopSyncMode()
    {
        if (!isSyncMode) return;
        
        Debug.Log("[GameManager] Stopping sync mode");
        isSyncMode = false;
        
        // Get all buffered moves
        var bufferedMoves = moveBuffer.StopBuffering();
        
        // Apply them in order
        if (bufferedMoves.Length > 0)
        {
            Debug.Log($"[GameManager] Applying {bufferedMoves.Length} buffered moves after sync");
            ApplyBufferedMoves(bufferedMoves);
        }
        
        // Unsubscribe from buffer events
        moveBuffer.OnMoveBuffered -= OnMoveBuffered;
        moveBuffer.OnBufferedMovesApplied -= OnBufferedMovesApplied;
        moveBuffer.OnMoveTimedOut -= OnMoveTimedOut;
        
        // Note: Server notification removed - using desync detection system
    }
    
    /// <summary>
    /// Buffers a move during sync mode
    /// </summary>
    public void BufferMoveForSync(GameMove move)
    {
        if (!isSyncMode)
        {
            Debug.LogWarning($"[GameManager] Not in sync mode - cannot buffer move {move.moveType} by P{move.playerNumber}");
            return;
        }
        
        Debug.Log($"[GameManager] Buffering move for sync: {move.moveType} by P{move.playerNumber}");
        moveBuffer.BufferMove(move);
    }
    
    /// <summary>
    /// Applies buffered moves after sync is complete
    /// </summary>
    public void ApplyBufferedMoves(GameMove[] moves)
    {
        Debug.Log($"[GameManager] Applying {moves.Length} buffered moves");
        
        // Add to pending moves list
        pendingMovesToApply.AddRange(moves);
        
        // Start applying moves one by one
        StartCoroutine(ApplyPendingMovesCoroutine());
    }
    
    /// <summary>
    /// Coroutine to apply pending moves in sequence
    /// </summary>
    private IEnumerator ApplyPendingMovesCoroutine()
    {
        while (pendingMovesToApply.Count > 0)
        {
            var move = pendingMovesToApply[0];
            pendingMovesToApply.RemoveAt(0);
            
            Debug.Log($"[GameManager] Applying buffered move: {move.moveType} by P{move.playerNumber}");
            
            // Apply the move based on its type
            yield return StartCoroutine(ApplyIndividualMove(move));
            
            // Small delay between moves to prevent overwhelming the system
            yield return new WaitForSeconds(0.1f);
        }
        
        Debug.Log("[GameManager] All buffered moves applied");
    }
    
    /// <summary>
    /// Applies an individual move based on its type
    /// </summary>
    private IEnumerator ApplyIndividualMove(GameMove move)
    {
        try
        {
            switch (move.moveType)
            {
                case GameMove.MoveType.PlayToCenter:
                    Debug.Log($"[GameManager] Applying PlayToCenter move: {move.cardId}");
                    // The move will be processed by the normal game flow when it arrives via RPC
                    break;
                    
                case GameMove.MoveType.Capture:
                    Debug.Log($"[GameManager] Applying Capture move: {move.cardId} captures {move.capturedCardIds?.Length ?? 0} cards");
                    // The move will be processed by the normal game flow when it arrives via RPC
                    break;
                    
                case GameMove.MoveType.SuperPower_Activation:
                    Debug.Log($"[GameManager] Applying SuperPower activation: {move.superPowerName}");
                    // Superpower moves will be processed by their respective systems
                    break;
                    
                case GameMove.MoveType.SuperPower_Effect:
                    Debug.Log($"[GameManager] Applying SuperPower effect: {move.superPowerName}");
                    // Effects will be applied when the RPC arrives
                    break;
                    
                default:
                    Debug.LogWarning($"[GameManager] Unknown move type: {move.moveType}");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameManager] Error applying buffered move {move.moveId}: {e.Message}");
        }
        
        yield return null;
    }
    
    /// <summary>
    /// Checks if we're currently in sync mode
    /// </summary>
    public bool IsSyncMode => isSyncMode;
    
    /// <summary>
    /// Gets the current buffer statistics for debugging
    /// </summary>
    public string GetBufferStats()
    {
        return moveBuffer.GetBufferStats();
    }
    
    /// <summary>
    /// Event handler for when a move is buffered
    /// </summary>
    private void OnMoveBuffered(GameMove move)
    {
        Debug.Log($"[GameManager] Move buffered: {move.moveType} by P{move.playerNumber}");
    }
    
    /// <summary>
    /// Event handler for when buffered moves are applied
    /// </summary>
    private void OnBufferedMovesApplied(GameMove[] moves)
    {
        Debug.Log($"[GameManager] {moves.Length} buffered moves were applied");
    }
    
    /// <summary>
    /// Event handler for when a buffered move times out
    /// </summary>
    private void OnMoveTimedOut(GameMove move)
    {
        Debug.LogWarning($"[GameManager] Buffered move timed out: {move.moveType} by P{move.playerNumber}");
    }
    
    /// <summary>
    /// Rebuilds cardLookup for reconnection by mapping newly created cards to server IDs
    /// </summary>
    private void RebuildCardLookupForReconnection()
    {
        Debug.Log($"[GameManager] Rebuilding cardLookup for reconnection");
        Debug.Log($"[GameManager] Current cardLookup count: {CardInteraction.cardLookup.Count}");
        
        // Clear the existing cardLookup (it has wrong IDs)
        CardInteraction.cardLookup.Clear();
        
        // Get all card interactions from DeckController
        if (deckController != null && deckController.cardInteractionList != null)
        {
            Debug.Log($"[GameManager] Found {deckController.cardInteractionList.Count} card interactions");
            
            // For each card interaction, we need to map it to the correct server ID
            // The issue is that we don't know which server ID corresponds to which card
            // We need to wait for the game state to be applied to know the correct mapping
            
            // For now, just log that we're ready to rebuild
            Debug.Log($"[GameManager] Card interactions ready for server ID mapping");
        }
        else
        {
            Debug.LogError("[GameManager] DeckController or cardInteractionList is null - cannot rebuild cardLookup");
        }
    }
    
    
    /// <summary>
    /// Ensures the move chain system is properly initialized for reconnected clients
    /// </summary>
    private void EnsureMoveChainSystemInitialized()
    {
        // Check if MoveChainIntegrator exists and is properly initialized
        if (MoveChainIntegrator.LocalInstance == null)
        {
            Debug.LogError("[GameManager] MoveChainIntegrator.LocalInstance is null - move chain system not initialized!");
            return;
        }
        
        // Check if MoveChainTracker is properly set up
        var moveChainTracker = GetComponent<MoveChainTracker>();
        if (moveChainTracker == null)
        {
            Debug.LogWarning("[GameManager] MoveChainTracker not found on GameManager - this should be added by MoveChainIntegrator");
        }
        else
        {
            Debug.Log("[GameManager] MoveChainTracker found and ready");
        }
        
        // Ensure move tracking is enabled
        Debug.Log("[GameManager] Move chain system initialization check completed");
    }

    /// <summary>
    /// Triggers a desync check after reconnection to ensure move chains are synchronized
    /// </summary>
    public void TriggerDesyncCheckAfterReconnection()
    {
        Debug.LogError("[Visual Sync] ===== TRIGGER DESYNC CHECK AFTER RECONNECTION CALLED =====");
        Debug.Log("[GameManager] TriggerDesyncCheckAfterReconnection called - starting coroutine");
        
        // Start the coroutine on this GameManager instance
        StartCoroutine(TriggerDesyncCheckAfterReconnectionCoroutine());
    }
    
    /// <summary>
    /// Coroutine that handles the actual desync check after reconnection
    /// </summary>
    private IEnumerator TriggerDesyncCheckAfterReconnectionCoroutine()
    {
        Debug.LogError("[Visual Sync] ===== TRIGGER DESYNC CHECK AFTER RECONNECTION COROUTINE STARTED =====");
        Debug.Log("[GameManager] TriggerDesyncCheckAfterReconnection coroutine - waiting for game state to be fully applied");
        
        // Wait for the game state to be fully applied
        yield return new WaitForSeconds(1.0f);
        
        // CRITICAL FIX: Reset isReconnecting flag after reconnection sync is complete
        // This allows normal dealing to work again
        isReconnecting = false;
        Debug.LogError("[RECONNECTION] isReconnecting flag reset to false - normal dealing can now work");
        
        // Check if MoveChainIntegrator is available
        if (MoveChainIntegrator.LocalInstance != null)
        {
            Debug.LogError("[Visual Sync] MoveChainIntegrator found - triggering desync check");
            Debug.Log("[GameManager] Triggering forced desync check for reconnected client");
            MoveChainIntegrator.LocalInstance.ForceImmediateDesyncCheck();
            
            // NEW: After desync check, trigger visual restoration
            Debug.LogError("[Visual Sync] ===== TRIGGERING VISUAL RESTORATION AFTER DESYNC CHECK =====");
            StartCoroutine(TriggerVisualRestorationAfterDesyncCheck());
        }
        else
        {
            Debug.LogError("[Visual Sync] ERROR: MoveChainIntegrator.LocalInstance is null - cannot force desync check");
            Debug.LogWarning("[GameManager] MoveChainIntegrator.LocalInstance is null - cannot force desync check");
        }
    }
    
    /// <summary>
    /// NEW: Triggers visual restoration after desync check completes
    /// This ensures the move chain is properly synchronized before restoring visuals
    /// </summary>
    private IEnumerator TriggerVisualRestorationAfterDesyncCheck()
    {
        Debug.LogError("[Visual Sync] ===== TRIGGER VISUAL RESTORATION AFTER DESYNC CHECK STARTED =====");
        
        // Wait for desync check to complete and move chain to be synchronized
        yield return new WaitForSeconds(2.0f);
        
        Debug.LogError("[Visual Sync] Desync check should be complete - starting visual restoration");
        
        // Now restore visual states from the properly synchronized move chain
        yield return StartCoroutine(RestoreVisualStatesFromMoveChain());
        
        Debug.LogError("[Visual Sync] ===== VISUAL RESTORATION AFTER DESYNC CHECK COMPLETED =====");
    }
    
    /// <summary>
    /// Forces a desync check after reconnection to ensure move chains are synchronized
    /// </summary>
    private IEnumerator ForceDesyncCheckAfterReconnection()
    {
        // Wait for the game state to be fully applied
        yield return new WaitForSeconds(0.5f);
        
        // Check if MoveChainIntegrator is available
        if (MoveChainIntegrator.LocalInstance != null)
        {
            Debug.Log("[GameManager] Triggering forced desync check for reconnected client");
            MoveChainIntegrator.LocalInstance.ForceImmediateDesyncCheck();
        }
        else
        {
            Debug.LogWarning("[GameManager] MoveChainIntegrator.LocalInstance is null - cannot force desync check");
        }
    }
    
    /// <summary>
    /// Restores player number from PlayerPrefs during reconnection
    /// </summary>
    private void RestorePlayerNumberFromPrefs()
    {
        Debug.LogError($"[PLAYER NUMBER] ===== RESTORING PLAYER NUMBER FROM PREFS =====");
        Debug.LogError($"[PLAYER NUMBER] PlayerPrefs.HasKey('PlayerNumber'): {PlayerPrefs.HasKey("PlayerNumber")}");
        
        if (PlayerPrefs.HasKey("PlayerNumber"))
        {
            int savedPlayerNumber = PlayerPrefs.GetInt("PlayerNumber");
            Debug.LogError($"[PLAYER NUMBER] Restoring player number {savedPlayerNumber} from PlayerPrefs during reconnection");
            
            // Set the player number directly on DeckController
            if (deckController != null)
            {
                Debug.LogError($"[PLAYER NUMBER] DeckController found, calling SetPlayerNumber({savedPlayerNumber})");
                deckController.SetPlayerNumber(savedPlayerNumber);
                Debug.LogError($"[PLAYER NUMBER] Successfully restored player number {savedPlayerNumber} to DeckController");
                Debug.LogError($"[PLAYER NUMBER] DeckController.thisPlayerNumber is now: {deckController.thisPlayerNumber}");
            }
            else
            {
                Debug.LogError($"[PLAYER NUMBER] ERROR: DeckController is null, cannot restore player number");
            }
        }
        else
        {
            Debug.LogError($"[PLAYER NUMBER] WARNING: No saved player number found in PlayerPrefs during reconnection");
            Debug.LogError($"[PLAYER NUMBER] Available PlayerPrefs keys: {string.Join(", ", GetAllPlayerPrefsKeys())}");
        }
        
        Debug.LogError($"[PLAYER NUMBER] ===== END RESTORING PLAYER NUMBER =====");
    }
    
    private string[] GetAllPlayerPrefsKeys()
    {
        // This is a helper method to debug what's in PlayerPrefs
        // Note: Unity doesn't provide a direct way to get all keys, so we'll check common ones
        var keys = new List<string>();
        if (PlayerPrefs.HasKey("PlayerNumber")) keys.Add("PlayerNumber");
        if (PlayerPrefs.HasKey("JoinCode")) keys.Add("JoinCode");
        if (PlayerPrefs.HasKey("LobbyCode")) keys.Add("LobbyCode");
        return keys.ToArray();
    }

    // ===== REDO SYSTEM =====

    /// <summary>
    /// Called when a redo revert state is received from server
    /// </summary>
    public void OnRedoRevertToState(SerializableGameState snapshot, string revertType)
    {
        Debug.Log($"[GameManager] Redo: Reverting to {revertType} - applying snapshot v{snapshot.snapshotVersion}");
        Debug.Log($"[GameManager] Redo: State contains currentPlayer={snapshot.currentPlayer}, turnCounter={snapshot.turnCounter}");
        
        // CRITICAL FIX: Explicitly apply current player and turn counter after redo
        // This ensures the UI reflects the correct turn immediately
        currentPlayerNo = snapshot.currentPlayer;
        turnCounter = snapshot.turnCounter;
        
        Debug.Log($"[GameManager] Redo: Applied currentPlayer={currentPlayerNo}, turnCounter={turnCounter}");
        
        // Show a message to the user about the revert
        ShowcaseSuperPower($"Reverted to {revertType}", 0.5f, 3f);
        
        // CRITICAL FIX: Start coroutine that waits for scene reconstruction to complete
        // This ensures the server waits for all clients before sending current player update
        StartCoroutine(WaitForSceneReconstructionAndConfirm(snapshot, revertType));
        
        Debug.Log($"[GameManager] Redo revert to {revertType} initiated");
    }

    /// <summary>
    /// Waits for scene reconstruction to complete and then confirms to server
    /// </summary>
    private IEnumerator WaitForSceneReconstructionAndConfirm(SerializableGameState snapshot, string revertType)
    {
        Debug.Log($"[GameManager] Starting scene reconstruction for {revertType}");
        
        // Use existing game state application system and wait for it to complete
        if (deckController != null)
        {
            Debug.Log($"[GameManager] Waiting for scene reconstruction coroutines to complete");
            yield return StartCoroutine(ApplyGameStateWithReset(snapshot));
            Debug.Log($"[GameManager] Scene reconstruction coroutines completed");
        }
        else
        {
            Debug.LogError("[GameManager] deckController is null - falling back to ApplyGameState");
            ApplyGameState(snapshot);
            // Wait a bit as fallback
            yield return new WaitForSeconds(2.0f);
        }
        
        // Additional safety wait to ensure all visual updates are complete
        yield return new WaitForSeconds(1.0f);
        
        Debug.Log($"[GameManager] Confirming redo scene reconstruction is complete for {revertType}");
        
        if (networkRelay != null)
        {
            networkRelay.ConfirmRedoSceneReconstructionFinishedServerRPC();
        }
        else
        {
            Debug.LogError("[GameManager] NetworkRelay is null - cannot confirm redo completion");
        }
        
        Debug.Log($"[GameManager] Redo scene reconstruction confirmation sent for {revertType}");
    }

    /// <summary>
    /// Called when redo state status is received from server
    /// </summary>
    public void OnRedoStateStatusReceived(string status)
    {
        Debug.Log($"[GameManager] Redo state status: {status}");
        // You could display this in UI if needed
    }

    /// <summary>
    /// Request redo to previous state (1 turn back)
    /// </summary>
    [ContextMenu("Redo: Request Previous State")]
    public void RequestRedoToPreviousState()
    {
        if (networkRelay != null)
        {
            Debug.Log("[GameManager] Requesting redo to previous state");
            networkRelay.RequestRedoToPreviousStateServerRPC();
        }
        else
        {
            Debug.LogError("[GameManager] NetworkRelay is null - cannot request redo");
        }
    }

    /// <summary>
    /// Request redo to pre-previous state (2 turns back)
    /// </summary>
    [ContextMenu("Redo: Request Pre-Previous State")]
    public void RequestRedoToPrePreviousState()
    {
        if (networkRelay != null)
        {
            Debug.Log("[GameManager] Requesting redo to pre-previous state");
            networkRelay.RequestRedoToPrePreviousStateServerRPC();
        }
        else
        {
            Debug.LogError("[GameManager] NetworkRelay is null - cannot request redo");
        }
    }

    /// <summary>
    /// Request redo state status from server
    /// </summary>
    [ContextMenu("Redo: Check State Status")]
    public void RequestRedoStateStatus()
    {
        if (networkRelay != null)
        {
            Debug.Log("[GameManager] Requesting redo state status");
            networkRelay.RequestRedoStateStatusServerRPC();
        }
        else
        {
            Debug.LogError("[GameManager] NetworkRelay is null - cannot request redo status");
        }
    }

}

