using System;

using System.Collections;

using System.Collections.Generic;

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

            

            return;

        }

        

        

    }



    [ContextMenu("Reset Debug Logs")]

    public void ResetDebugLogs()

    {

        debugLog = "";

        

    }



    [ContextMenu("Toggle Debug Logging")]

    public void ToggleDebugLogging()

    {

        loggingEnabled = !loggingEnabled;

        

    }



    // ===== SYNC LOGGING (separate buffer) =====

    private static string syncLog = "";

    private static bool syncLoggingEnabled = true;



    public static void SyncLog(string message)

    {

        if (!syncLoggingEnabled) return;

        string line = $"[SYNC {DateTime.Now:HH:mm:ss.fff}] {message}";

        syncLog += line + "\n";

        

    }



    public static void SyncLogWarning(string message)

    {

        if (!syncLoggingEnabled) return;

        string line = $"[SYNC {DateTime.Now:HH:mm:ss.fff}] WARNING: {message}";

        syncLog += line + "\n";

        

    }



    public static void SyncLogError(string message)

    {

        if (!syncLoggingEnabled) return;

        string line = $"[SYNC {DateTime.Now:HH:mm:ss.fff}] ERROR: {message}";

        syncLog += line + "\n";

        

    }



    [ContextMenu("Print All Sync Logs")]

    public void PrintAllSyncLogs()

    {

        if (string.IsNullOrEmpty(syncLog))

        {

            

            return;

        }

        

    }



    [ContextMenu("Reset Sync Logs")]

    public void ResetSyncLogs()

    {

        syncLog = "";

        

    }



    [ContextMenu("Toggle Sync Logging")]

    public void ToggleSyncLogging()

    {

        syncLoggingEnabled = !syncLoggingEnabled;

        

    }



    //Scripts

    [SerializeField ] private bool editorMode = true;

    public static GameManager LocalInstance { get; private set; }

    public static SerializableGameState BuildEmptySerializableGameState()
    {
        return new SerializableGameState
        {
            snapshotVersion = 1,
            timestamp = DateTime.UtcNow.Ticks,
            deck = new SerializableStringList(new List<string>()),
            center = new SerializableStringList(new List<string>()),
            hands = new SerializableDictionary(new Dictionary<int, List<string>>()),
            pools = new SerializableDictionary(new Dictionary<int, List<string>>()),
            pistiPools = new SerializableDictionary(new Dictionary<int, List<string>>()),
            bombStack = new SerializableStringList(new List<string>()),
            points = new SerializableIntArray(new int[4]),
            pistiCounts = new SerializableIntArray(new int[4]),
            copiedCardMap = new SerializableStringDictionary(new Dictionary<string, string>()),
            cardPowerEffects = new SerializableStringDictionary(new Dictionary<string, string>()),
            playerGold = new SerializableIntDictionary(new Dictionary<int, int>()),
            playerSuperPowers = new SerializableDictionary(new Dictionary<int, List<string>>()),
            botControlledPlayers = new SerializableIntArray(new int[0]),
            cardLookup = new CardLookupEntry[0],
            failoverChain = new int[0]
        };
    }

    [SerializeField] private DeckController deckController;

    public GameNetworkRelay networkRelay;

    private IMoveProcessor moveProcessor;
    private IPowerProcessor powerProcessor;
    public IPowerProcessor PowerProcessor => powerProcessor;
    private IModeAdapter modeAdapter;
    public IModeAdapter ModeAdapter => modeAdapter;
    private PowerOrchestrator powerOrchestrator;
    public PowerOrchestrator PowerOrchestrator => powerOrchestrator;

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

    private int lastAuthoritativeScore0 = 0;
    private int lastAuthoritativeScore1 = 0;
    private int lastP1SelfPointReduction = 0;
    private int lastP2SelfPointReduction = 0;
    private int lastP1OppPointReduction = 0;
    private int lastP2OppPointReduction = 0;

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

    // Client-side sequencing lock for gameplay-affecting actions.
    private bool isGameplayActionInProgress = false;
    private string activeGameplayActionTag = string.Empty;
    private float gameplayActionStartTime = -1f;

    private bool hasAlreadySentRPC = false;

    private GameObject waitingScreen;

    private GameObject mainScreen;

    private GameObject explosionObject;

    private Animator explosionAnimator;

    [SerializeField] private GameObject yandimAnamEffectPrefab;

    [SerializeField] private GameObject yandimAnamSpritePrefab;

    public List<GameObject> kapkacCardsToBeReset = new List<GameObject>();

    // Track cards that have been changed by powers (for save/load persistence)
    private Dictionary<string, string> cardPowerEffects = new Dictionary<string, string>();
    private Dictionary<string, string> copiedCardMap = new Dictionary<string, string>();

    [SerializeField] public GameObject kapkacEffectPrefab; // Prefab with your PNG as a SpriteRenderers

    [SerializeField] private GameObject kapkacAnimEffectPrefab; // The animation prefab for Kapkaç

    [SerializeField] public GameObject oynayamazsinBlockPrefab;

    public GameObject oynayamazsinBlockInstance;

    public bool isKapkacPending = false;

    public bool isYandimAnamPending = false;

    public bool isBuDahaIyiPending = false;
    
    // RECONNECTION TRACKING: Track if we're currently reconnecting
    private bool isReconnecting = false;
    
    // BOT PLACEHOLDER SYSTEM: Track which players are currently controlled by bots
    public List<int> botControlledPlayers = new List<int>();







    public void ResetForNewRound()

    {

        //AddToDebugLog("[GameManager] ResetForNewRound called");

        //AddToDebugLog($"[GameManager] currentSelectedHandCard before reset: {currentSelectedHandCard}");

        //AddToDebugLog($"[GameManager] CardInteraction.currentlySelectedCard before reset: {CardInteraction.currentlySelectedCard?.gameObject.name}");

        

        // CRITICAL: Don't try to reset destroyed cards - they'll be recreated fresh
        // Clear the list since all card objects have been destroyed
        if (cardInteractionsScripts != null) cardInteractionsScripts.Clear();
        if (copiedCardMap != null) copiedCardMap.Clear();

        foreach (var kvp in CardInteraction.cardLookup)
        {
            if (kvp.Value != null)
            {
                kvp.Value.isFirstThreeDealtCard = false;
            }
        }

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

        // Reset all power-selection flags so they don't leak into the new round
        isKapkacPending = false;
        isYandimAnamPending = false;
        isBuDahaIyiPending = false;
        isKopyalaActive = false;
        isKopyalaSelectingSource = false;
        kopyalaSourceCard = null;
        isSunuDegisTokusActive = false;
        isSunuDegisTokusSelectingOpponent = false;
        sunuDegisTokusFirstCard = null;
        ResetSunuDegisBunuTokusState(true);

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
        
        
        // CRITICAL: Destroy all card GameObjects first for clean slate
        if (deckController != null)
        {
            deckController.DestroyAllCards();
        }
        
        // Clear all tracking lists (cards are already destroyed)
        if (cardInteractionsScripts != null) cardInteractionsScripts.Clear();
        if (kapkacCardsToBeReset != null) kapkacCardsToBeReset.Clear();
        if (cardPowerEffects != null) cardPowerEffects.Clear();
        if (copiedCardMap != null) copiedCardMap.Clear();
        
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
        isSunuDegisTokusSelectingOpponent = false;
        isSunuDegisBunuTokusActive = false;
        kopyalaSourceCard = null;
        sunuDegisTokusFirstCard = null;
        activeSunuDegisTokusPowerName = "Şunu Değiş Tokuş";
        ResetSunuDegisBunuTokusState(true);
        isKapkacPending = false;
        isYandimAnamPending = false;
        isBuDahaIyiPending = false;
        
        
    }



    void Awake()

    {

        

        if (LocalInstance != null && LocalInstance != this)

        {

            

            Destroy(this.gameObject);

            return;

        }

        LocalInstance = this;

        DontDestroyOnLoad(this.gameObject);

        

    }



    void Start()

    {

        

        InitialGameManagerSetUp();//Identifies and assigns necessary variables and calls other functions

        UpdateMoveProcessorState();



        SuperPowerSpawner.LocalInstance.InitializeSuperPowers(); // Initialize super powers

        
        // Instantiate DebugChainPrinter if it doesn't exist
        if (DebugChainPrinter.LocalInstance == null)
        {
            GameObject debugPrinterObj = new GameObject("DebugChainPrinter");
            debugPrinterObj.AddComponent<DebugChainPrinter>();
            DontDestroyOnLoad(debugPrinterObj);
            
        }
    }



    private void UpdateMoveProcessorState()
    {
        bool isMultiplayerListening = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        bool isSinglePlayerRunning = SinglePlayerModeController.Instance != null && SinglePlayerModeController.IsGameRunning;

        // Determine target mode
        string desiredMode = "None";
        if (isMultiplayerListening) desiredMode = "Multiplayer";
        else if (isSinglePlayerRunning) desiredMode = "Singleplayer";

        // Current mode
        string currentMode = "None";
        if (moveProcessor != null) currentMode = moveProcessor.ModeName;

        // If mismatch, re-initialize
        if (desiredMode != currentMode)
        {
            
            InitializeMoveProcessor(desiredMode);
        }
    }

    private void InitializeMoveProcessor(string mode)
    {
        if (mode == "Singleplayer" && SinglePlayerModeController.Instance != null)
        {
            moveProcessor = new SingleplayerMoveProcessor(SinglePlayerModeController.Instance);
            powerProcessor = new SingleplayerPowerProcessor(SinglePlayerModeController.Instance);
            // Ensure adapter exists; add if not present
            if (GetComponent<SingleplayerModeAdapter>() == null)
                gameObject.AddComponent<SingleplayerModeAdapter>();
            modeAdapter = GetComponent<SingleplayerModeAdapter>();
        }
        else if (mode == "Multiplayer" && networkRelay != null)
        {
            moveProcessor = new MultiplayerMoveProcessor(networkRelay);
            powerProcessor = new MultiplayerPowerProcessor(networkRelay);
            // Ensure adapter exists; add if not present
            if (GetComponent<MultiplayerModeAdapter>() == null)
                gameObject.AddComponent<MultiplayerModeAdapter>();
            modeAdapter = GetComponent<MultiplayerModeAdapter>();
        }
        else
        {
            moveProcessor = null;
            powerProcessor = null;
            modeAdapter = null;
        }

        // Initialize PowerOrchestrator for both modes
        if (GetComponent<PowerOrchestrator>() == null)
            gameObject.AddComponent<PowerOrchestrator>();
        powerOrchestrator = GetComponent<PowerOrchestrator>();

        if (moveProcessor != null)
        {
            
            AddToDebugLog($"[IMoveProcessor] Initialized: mode={moveProcessor.ModeName}");
        }
        else
        {
            
            AddToDebugLog("[IMoveProcessor] Reset to None");
        }

        if (powerProcessor != null)
        {
            
            AddToDebugLog($"[IPowerProcessor] Initialized: type={powerProcessor.GetType().Name}");
        }
            
    }

    public void NotifyConnection()
    {
        
        // Manual notification path no longer needed
        return;
    }

    /// <summary>
    /// Called when a reconnected client has successfully applied game state and is ready to play
    /// </summary>
    public void OnReconnectionGameStateApplied()
    {
        
        
        // 1. Close any waiting screens that might still be open
        if (waitingScreen != null && waitingScreen.activeSelf)
        {
            waitingScreen.SetActive(false);
            
        }
        
        // 2. Ensure main screen (lobby) is hidden and game is visible
        if (mainScreen != null && mainScreen.activeSelf)
        {
            mainScreen.SetActive(false);
            
        }

        // 3. Specifically deactivate "MainUI" as requested by user
        GameObject mainUI = GameObject.Find("MainUI");
        if (mainUI != null && mainUI.activeSelf)
        {
            mainUI.SetActive(false);
            
        }
        
        // 4. Update button visibility in case MainUIScript is still active (unlikely if MainUI is deactivated)
        MainUIScript mainUIScript = FindObjectOfType<MainUIScript>();
        if (mainUIScript != null)
        {
            // We can't easily call private methods, but deactivating the object should be enough
            // The user wants MainUI deactivated, so we've done that.
        }
        
        
    }



    void OnDestroy()

    {

        if (LocalInstance == this)

        {

            

            LocalInstance = null;

        }

    }





    private CardInteraction tempCard;

    void Update()

    {
        // Keep move processor state in sync with the active game mode (singleplayer vs multiplayer)
        UpdateMoveProcessorState();

        if (Input.GetKeyDown(KeyCode.W)) PlayAutomatically(3);

        if (Input.GetKeyDown(KeyCode.A)) PlayAutomatically(4);

        if (Input.GetKeyDown(KeyCode.S)) PlayAutomatically(1);

        if (Input.GetKeyDown(KeyCode.D)) PlayAutomatically(2);



        // Handle mouse input (WebGL)
        Vector3 mousePosition = Input.mousePosition;
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);

        // Mouse button down
        if (Input.GetMouseButtonDown(0))
        {
            AddToDebugLog($"[GameManager] Mouse down detected - Position: {mousePosition}");

            if (IsGameplayActionInProgress())
            {
                AddToDebugLogWarning($"[GameManager] Blocking new card input while gameplay action is active: {activeGameplayActionTag}");
                return;
            }
            
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                CardInteraction card = hit.collider.GetComponent<CardInteraction>();
                AddToDebugLog($"[GameManager] Found CardInteraction: {card?.gameObject?.name}, parent: {card?.gameObject?.transform?.parent?.name}");

                if (card != null)
                {
                    AddToDebugLog($"[GameManager] Mouse OnCardTouched called for card: {card.gameObject.name}, parent: {card.gameObject.transform.parent?.name}");
                    card.OnCardTouched(mousePosition);
                    tempCard = card; // Store the card for later use
                }
                else
                {
                    AddToDebugLog($"[GameManager] Mouse - No card found, calling TryStopAllShowcases");
                    if (deckController != null)
                    {
                        deckController.TryStopAllShowcases();
                    }
                }
            }
            else
            {
                AddToDebugLog($"[GameManager] Mouse - Raycast missed, calling TryStopAllShowcases");
                if (deckController != null)
                {
                    deckController.TryStopAllShowcases();
                }
            }
        }
        
        // Mouse drag (while button held)
        else if (Input.GetMouseButton(0) && tempCard != null)
        {
            if (CardInteraction.currentlySelectedCard == tempCard)
            {
                AddToDebugLog($"[GameManager] Mouse OnTouchDrag called for card: {tempCard.gameObject.name}");
                tempCard.OnTouchDrag(mousePosition);
            }
        }
        
        // Mouse button up
        else if (Input.GetMouseButtonUp(0) && tempCard != null)
        {
            if (CardInteraction.currentlySelectedCard == tempCard)
            {
                AddToDebugLog($"[GameManager] Mouse OnTouchUp called for card: {tempCard.gameObject.name}");
                tempCard.OnTouchUp();
            }
            tempCard = null;
        }

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

        if (isReconnection)
        {
            isReconnecting = true;
            
            if (networkRelay != null)
            {
                networkRelay.RequestFullStateSyncServerRPC();
            }
            yield break; // Do not reset or redeal locally
        }

        
        ResetForNewRound();
        roundCount++;
        isReconnecting = false;

        
        if (waitingScreen.activeSelf) 
        {
            
            waitingScreen.SetActive(false);
        }

        
        yield return StartCoroutine(deckController.DeckStart());
        
        
        // RECONNECTION: Let the normal flow handle everything
        // The card IDs will be deterministic and match the server
        // The existing desync detection will handle synchronization

        if (winScreen.activeSelf) 
        {
            
            winScreen.SetActive(false);
        }

        if (mainScreen.activeSelf) 
        {
            
            mainScreen.SetActive(false);
        }

        Debug.Log($"[GameManager] ===== ÖNEMLİ: INITIALIZE CARD PREFABS COMPLETED =====\n" +
                 $"IsReconnection: {isReconnection}\n" +
                 $"IsReconnecting flag: {isReconnecting}");

    }



    public void DeckReady()

    {
        
        Debug.Log($"[GameManager] ===== ÖNEMLİ: DECK READY CALLED =====\n" +
                 $"IsReconnecting: {isReconnecting}\n" +
                 $"GameNetworkRelay: {(networkRelay != null ? "FOUND" : "NULL")}\n" +
                 $"IsConnectedClient: {NetworkManager.Singleton.IsConnectedClient}\n" +
                 $"IsHost: {NetworkManager.Singleton.IsHost}\n" +
                 $"IsServer: {NetworkManager.Singleton.IsServer}\n" +
                 $"LocalClientId: {NetworkManager.Singleton.LocalClientId}");

        if (isReconnecting)
        {
            
            return; // Do not call DeckReadyServerRPC
        }

        

        // For normal game flow, use the regular RPC
        
        networkRelay.DeckReadyServerRPC();
        
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
        
        //Converts serializablelist to a normal list
        List<string> centerCardIDs = serializableCard.ToDictionary().Keys.ToList();

        // RECONNECTION FIX: Skip dealing during reconnection - let ApplyGameState handle it
        if (isReconnecting)
        {
            
            return;
        }

        //Informs the deckController to deal the center cards
        if (deckController)
        {
            // Kick off deck movement immediately so DealCenter's spin-wait resolves fast
            deckController.MoveDeckToReachPoint();
            StartCoroutine(deckController.DealCenter(centerCardIDs));
        }
        else   

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
            AddToDebugLog($"[GameManager] ŞunuDeğişTokuş/DeğişTokuş is active");

            Transform selectedTransform = CardInteraction.cardLookup.ContainsKey(cardID)
                ? CardInteraction.cardLookup[cardID].transform
                : null;
            bool isHandCard = selectedTransform != null &&
                              selectedTransform.parent != null &&
                              selectedTransform.parent.name.StartsWith("PlayerHand");

            if (!isHandCard)
            {
                AddToDebugLogWarning("ŞunuDeğişTokuş: Yalnızca el kartları seçilebilir.");
                return;
            }

            // If first card not selected, set it
            if (string.IsNullOrEmpty(sunuDegisTokusFirstCard))
            {
                sunuDegisTokusFirstCard = cardID;
                isSunuDegisTokusSelectingOpponent = false;
                AddToDebugLog($"[GameManager] ŞunuDeğişTokuş/DeğişTokuş: First hand card selected: {cardID}");

                // Sync PowerOrchestrator state to Phase 2
                PowerOrchestrator?.ExecuteSunuDegisTokus(cardID, null);

                // Update InfoBox with explicit step progression
                SuperPowerSpawner.LocalInstance?.ShowDualSelectionStepText("Adım 2/2: Şimdi farklı bir el kartı seç");

                AddToDebugLog("[GameManager] Now select a different hand card.");
                return;
            }

            // If second card is same as first, ignore
            if (sunuDegisTokusFirstCard == cardID)
            {
                AddToDebugLogWarning("ŞunuDeğişTokuş: Cannot select the same card twice.");
                return;
            }

            bool isActuallyCenterCard = selectedTransform != null &&
                                      selectedTransform.parent != null &&
                                      selectedTransform.parent.name == "Center";
            if (isActuallyCenterCard)
            {
                AddToDebugLogWarning("DeğişTokuş: Second selection cannot be a center card.");
                return;
            }

            // 1. Call PowerActivated for local effects
            
            TriggerActiveSunuDegisTokusPowerActivated();

            // 2. Send swap request to server — ClientRPC will handle the swap for all clients (including this one)
            networkRelay.UseSunuDegisTokusServerRPC(sunuDegisTokusFirstCard, cardID);

            // 3. Clear PowerOrchestrator state
            PowerOrchestrator?.ClearAllSelectionFlags();

            // 4. Stop hand showcasing
            
            if (SuperPowerSpawner.LocalInstance != null)
            {
                SuperPowerSpawner.LocalInstance.StopHandShowcase();
                
            }
            else
            {
                
            }

            isSunuDegisTokusActive = false;
            isSunuDegisTokusSelectingOpponent = false;
            sunuDegisTokusFirstCard = null;
            activeSunuDegisTokusPowerName = "Şunu Değiş Tokuş";

            // Both cards selected: close the InfoBox
            if (SuperPowerSpawner.LocalInstance != null)
                StartCoroutine(SuperPowerSpawner.LocalInstance.CloseInfoBox());

            return;
        }



        // ŞunuDeğişBunuTokuş logic

        if (isSunuDegisBunuTokusActive)

        {

            AddToDebugLog($"[GameManager] ŞunuDeğişBunuTokuş is active, checking if card is from own hand");

            // Only allow selecting a card from another player's hand

            if (sunuDegisBunuTokusMyHandSnapshot != null && sunuDegisBunuTokusMyHandSnapshot.Contains(cardID))

            {

                AddToDebugLogWarning("You must select a card from another player's hand for ŞunuDeğişBunuTokuş.");

                return;

            }

            // Guard: no snapshot or all swaps already collected
            if (sunuDegisBunuTokusMyHandSnapshot == null || sunuDegisBunuTokusSwapIndex >= sunuDegisBunuTokusTotalSwaps)
            {
                AddToDebugLogWarning("No more selections needed for ŞunuDeğişBunuTokuş.");
                return;
            }

            // Prevent selecting the same opponent card twice
            if (sunuDegisBunuTokusSelectedOpponentCards.Contains(cardID))
            {
                AddToDebugLogWarning("That opponent card is already queued for a swap. Pick a different one.");
                return;
            }

            // Record selection and show indicators — opponent card (teal) + own hand card (blue)
            sunuDegisBunuTokusSelectedOpponentCards.Add(cardID);
            if (CardInteraction.cardLookup.TryGetValue(cardID, out var selectedCI))
                selectedCI.ShowDebugCandidateIndicator(new Color(0f, 1f, 0.75f, 0.65f));

            // Indicate the paired own-hand card so all selected pairs stay visible
            string pairedHandCard = sunuDegisBunuTokusMyHandSnapshot[sunuDegisBunuTokusSwapIndex];
            if (CardInteraction.cardLookup.TryGetValue(pairedHandCard, out var pairedCI))
                pairedCI.ShowDebugCandidateIndicator(new Color(0.4f, 0.8f, 1f, 0.65f));

            

            sunuDegisBunuTokusSwapIndex++;
            AddToDebugLog($"ŞunuDeğişBunuTokuş: Queued swap {sunuDegisBunuTokusSwapIndex}/{sunuDegisBunuTokusTotalSwaps}.");

            if (sunuDegisBunuTokusSwapIndex >= sunuDegisBunuTokusTotalSwaps)
            {
                // All selections collected — send to server and animate
                CompleteSunuDegisBunuTokusActivation();
            }
            else
            {
                // Prompt for next selection
                SuperPowerSpawner.LocalInstance?.ShowDualSelectionStepText(GetSunuDegisBunuTokusSelectionText(sunuDegisBunuTokusSwapIndex, sunuDegisBunuTokusTotalSwaps));
            }

            return;

        }



        AddToDebugLog($"[GameManager] CardSelected completed, calling SuperPowerSpawner.SetActiveActivateButtonTrue()");

        SuperPowerSpawner.LocalInstance.SetActiveActivateButtonTrue();

    }



    //Called when the player tries to play the selected card with one or two center cards

    private void CardsPlayed(string cardID, GameObject cardObject, int playerNumber)

    {
        AddToDebugLog($"[SingleplayerCardPlay] ===== CARD PLAY FLOW START =====");
        AddToDebugLog($"[SingleplayerCardPlay] CardsPlayed event listener triggered: cardID={cardID}, playerNumber={playerNumber}");

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

        if (!IsGameplayActionInProgress())
        {
            string actionCard = string.IsNullOrEmpty(currentSelectedHandCard) ? "unknown" : currentSelectedHandCard;
            if (!TryBeginGameplayAction($"CardPlay:{actionCard}"))
            {
                AddToDebugLogWarning("[GameManager] CheckIfLegal aborted because another gameplay action is active.");
                return;
            }
        }

        

        //if(currentSelectedHandCard[0] == 0 || currentSelectedHandCard[1]==0)return false;



        // serializableCard is created per-processor; kept here only for the debug log below
        SerializableCard serializableCard = new SerializableCard(centerCards);



        int sumValue = centerCards.Count > 0 ? centerCards.Last().Value[1] : 0;

        

        AddToDebugLog($"[GameManager] About to access CardInteraction.cardLookup[currentSelectedHandCard]");

        AddToDebugLog($"[GameManager] currentSelectedHandCard value: {currentSelectedHandCard}");

        AddToDebugLog($"[GameManager] CardInteraction.cardLookup contains key: {CardInteraction.cardLookup.ContainsKey(currentSelectedHandCard)}");

        

        if (currentSelectedHandCard == null)

        {

            AddToDebugLogError($"[GameManager] ERROR: currentSelectedHandCard is NULL!");

            EndGameplayActionIfTagStartsWith("CardPlay", "CardPlay validation failed: selected card null");

            return;

        }

        

        if (!CardInteraction.cardLookup.ContainsKey(currentSelectedHandCard))

        {

            AddToDebugLogError($"[GameManager] ERROR: CardInteraction.cardLookup does not contain key: {currentSelectedHandCard}");

            AddToDebugLogError($"[GameManager] Available keys in cardLookup: {string.Join(", ", CardInteraction.cardLookup.Keys)}");

            EndGameplayActionIfTagStartsWith("CardPlay", "CardPlay validation failed: card missing in lookup");

            return;

        }

        

        int cardValue = CardInteraction.cardLookup[currentSelectedHandCard].GetCardID()[1];

        AddToDebugLog($"CardValue: {cardValue}, SumValue: {sumValue}");



        if (oynayamazsinActive)

        {

            AddToDebugLog($"[GameManager] oynayamazsinActive is true; move will be processed by server as add-to-center");

        }

        else if (cardValue == sumValue || (cardValue == 11 && sumValue != 0))

        {

            AddToDebugLog($"[GameManager] Card can capture; waiting for server confirmation before visual update");

        }

        else

        {

            AddToDebugLog($"[GameManager] Card cannot capture; waiting for server confirmation before visual update");

        }



        AddToDebugLog($"[GameManager] center cards count: {serializableCard.ToDictionary().Count}, movePlayedLocally: {movePlayedLocally}");

        AddToDebugLog($"[IMoveProcessor] Mode={moveProcessor?.ModeName ?? "null"}, IsActive={moveProcessor?.IsActive}");

        if (moveProcessor != null && moveProcessor.IsActive)
        {
            AddToDebugLog($"[IMoveProcessor] Routing to {moveProcessor.ModeName} processor");
            hasAlreadySentRPC = true;
            moveProcessor.ProcessCardPlay(currentSelectedHandCard, centerCards, sumValue, cardValue, playerNumber);
            CardInteraction.currentlySelectedCard = null;
            SetCurrentSelectedHandCardNull();
            if (moveProcessor.RunsPostMoveBackgroundCheck)
            {
                SuperPowerSpawner.LocalInstance.CheckIfBackgroundPanelOpen();
            }
            EndGameplayActionIfTagStartsWith("CardPlay", $"{moveProcessor.ModeName} move processed");
            return;
        }

        AddToDebugLogError("[IMoveProcessor] No active move processor! Cannot process move.");
        EndGameplayActionIfTagStartsWith("CardPlay", "Move processor unavailable");



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
        if (moveProcessor == null || !moveProcessor.IsActive)
        {
            
            return;
        }

        AddToDebugLog("[GameManager] Routing add-to-center through moveProcessor");
        int[] cardKindValue = CardInteraction.cardLookup.TryGetValue(currentSelectedHandCard, out var ci) ? ci.GetCardID() : null;
        moveProcessor.ProcessAddToCenter(currentSelectedHandCard, cardKindValue);

        // Multiplayer: GameManager removes the card immediately (server confirms separately).
        // Singleplayer: ProcessPlayerAddToCenterCoroutine checks Contains() before removing at animation time, so this is safe for both.
        myCards.Remove(currentSelectedHandCard);
        currentSelectedHandCard = null;

    }



    public void GetCardThatCaptured(string playedCard, SerializableCard serializedCard, int playerNumber)

    {

        AddToDebugLog($"[GameManager] GetCardThatCaptured called with playedCard: {playedCard}, playerNumber: {playerNumber}");

        AddToDebugLog($"[GameManager] This is local player: {playerNumber == deckController.thisPlayerNumber}");

        

        

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

                int relativePoolIdx = (playerNumber - deckController.thisPlayerNumber + deckController.playerCount) % deckController.playerCount;

                int poolIndex = deckController.GetPoolIndex(relativePoolIdx);

                capturedCard.transform.SetParent(playerPoolTransforms[poolIndex]);

                capturedCard.transform.localPosition = Vector3.zero;

                capturedCard.transform.localScale = Vector3.one;

                

                // Add to center cards objects list

                centerCardsObjects.Add(capturedCard);

                

                // Add to center cards dictionary

                centerCards[playedCard] = cardID;

                

                AddToDebugLog($"[GameManager] Added capture card {playedCard} with value [{cardID[0]},{cardID[1]}] to center cards");

                

            }

            

            // Add all captured cards to center cards dictionary for gold calculation

            foreach (var kvp in cardDictionary)

            {

                if (!centerCards.ContainsKey(kvp.Key))

                {

                    centerCards[kvp.Key] = kvp.Value;

                    AddToDebugLog($"[GameManager] Added captured card {kvp.Key} with value [{kvp.Value[0]},{kvp.Value[1]}] to center cards");

                    

                }

            }

        }

        

        // Apply capture visuals for all players only after server confirmation.
        AddToDebugLog($"[GameManager] Calling DiscardCapturedCards for player {playerNumber}");
        DiscardCapturedCards(playedCard, serializedCard, playerNumber);

        

        // Handle gold gain for local player

        if (playerNumber == deckController.thisPlayerNumber && SuperPowerSpawner.LocalInstance != null)

        {

            // Ensure the capture card is included in the center cards before calculating gold

            // The capture card should already be in centerCards by now

            AddToDebugLog($"[GameManager] Center cards before gold calculation: {centerCards.Count} cards");

            

            foreach (var kvp in centerCards)

            {

                AddToDebugLog($"[GameManager] Center card: {kvp.Key} -> [{kvp.Value[0]}, {kvp.Value[1]}]");

                

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

            if (myCards.Contains(playedCard))
            {
                myCards.Remove(playedCard);
            }

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

        if (myCards.Contains(uniqueCardID))
        {
            myCards.Remove(uniqueCardID);
        }

        PlayCardToCenter(uniqueCardID, cardID);

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

        // Turn sync indicates the previous card-play action finished processing.
        EndGameplayActionIfTagStartsWith("CardPlay", "Turn update synced");

        AddToDebugLog($"[GameManager] Updated currentPlayerNo to: {currentPlayerNo}");

        turnCounter = turnC;
        
        // Reset card selection if it was my turn before but isn't now
        if (wasMyTurn && !IsLocalPlayerTurn())
        {
            
            CardInteraction.ResetCardSelection();
            CancelDualSelectionPower();
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
            
            return false;
        }
        
        bool isMyTurn = currentPlayerNo == deckController.thisPlayerNumber;
        AddToDebugLog($"[GameManager] IsLocalPlayerTurn: currentPlayerNo={currentPlayerNo}, thisPlayerNumber={deckController.thisPlayerNumber}, isMyTurn={isMyTurn}");
        return isMyTurn;
    }

    /// <summary>
    /// Returns true while a gameplay-affecting action is being processed.
    /// </summary>
    public bool IsGameplayActionInProgress()
    {
        return isGameplayActionInProgress;
    }

    /// <summary>
    /// Tries to acquire the client-side gameplay action lock.
    /// Returns false if another gameplay action is still running.
    /// </summary>
    public bool TryBeginGameplayAction(string actionTag)
    {
        if (isGameplayActionInProgress)
        {
            AddToDebugLogWarning($"[GameManager] TryBeginGameplayAction blocked. Active={activeGameplayActionTag}, Requested={actionTag}");
            return false;
        }

        isGameplayActionInProgress = true;
        activeGameplayActionTag = actionTag;
        gameplayActionStartTime = Time.time;
        AddToDebugLog($"[GameManager] Gameplay action started: {activeGameplayActionTag}");
        return true;
    }

    /// <summary>
    /// Releases the client-side gameplay action lock.
    /// </summary>
    public void EndGameplayAction(string reason = "")
    {
        

        if (!isGameplayActionInProgress)
        {
            
            return;
        }

        string endedTag = activeGameplayActionTag;
        float elapsed = gameplayActionStartTime >= 0f ? (Time.time - gameplayActionStartTime) : 0f;

        
        isGameplayActionInProgress = false;
        activeGameplayActionTag = string.Empty;
        gameplayActionStartTime = -1f;

        
        AddToDebugLog($"[GameManager] Gameplay action ended: {endedTag}, reason={reason}, elapsed={elapsed:0.00}s");
    }

    /// <summary>
    /// Releases the lock only if the active action tag starts with the given prefix.
    /// </summary>
    public void EndGameplayActionIfTagStartsWith(string tagPrefix, string reason = "")
    {
        
        

        if (!isGameplayActionInProgress)
        {
            
            return;
        }

        
        if (string.IsNullOrEmpty(tagPrefix) || activeGameplayActionTag.StartsWith(tagPrefix))
        {
            
            EndGameplayAction(reason);
        }
        else
        {
            
        }
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

            

            List<string> cardList = kvp.Value;



            foreach (var card in cardList)

            {

                string cardRepresentation = string.Join(", ", card);

                

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



    public void SyncVisualScores(int score0, int score1, int p1Self = 0, int p2Self = 0, int p1Opp = 0, int p2Opp = 0)
    {
        UpdatePointText(score0, score1, p1Self, p2Self, p1Opp, p2Opp);
    }

    private void UpdatePointText(int point0, int point1, int p1Self = 0, int p2Self = 0, int p1Opp = 0, int p2Opp = 0)
    {
        lastAuthoritativeScore0 = point0;
        lastAuthoritativeScore1 = point1;
        lastP1SelfPointReduction = p1Self;
        lastP2SelfPointReduction = p2Self;
        lastP1OppPointReduction = p1Opp;
        lastP2OppPointReduction = p2Opp;

        RenderScoreBoardVisuals();
    }

    public void RenderScoreBoardVisuals()
    {
        if (pointTexts == null)
        {
            pointTexts = new List<Text>();
        }

        if (pointTexts.Count < 2)
        {
            var pointText1Obj = GameObject.Find("PlayerPointText1");
            var pointText2Obj = GameObject.Find("PlayerPointText2");
            if (pointText1Obj != null && pointText2Obj != null)
            {
                pointTexts.Clear();
                pointTexts.Add(pointText1Obj.GetComponent<Text>());
                pointTexts.Add(pointText2Obj.GetComponent<Text>());
            }
            else
            {
                
                return;
            }
        }

        int displayedScore0 = lastAuthoritativeScore0;
        int displayedScore1 = lastAuthoritativeScore1;

        int playerSeat = deckController != null ? deckController.thisPlayerNumber : -1;

        // Apply side-specific fake point reductions based on team perspective
        if (playerSeat == 0 || playerSeat == 2) // Team 0 (p1side)
        {
            displayedScore0 -= lastP1SelfPointReduction;
            displayedScore1 -= lastP2OppPointReduction; // Hide Team 1's closed cards from Team 0
        }
        else if (playerSeat == 1 || playerSeat == 3) // Team 1 (p2side)
        {
            displayedScore0 -= lastP1OppPointReduction; // Hide Team 0's closed cards from Team 1
            displayedScore1 -= lastP2SelfPointReduction;
        }

        if (playerSeat == 0 || playerSeat == 2)
        {
            pointTexts[0].text = displayedScore0.ToString();
            pointTexts[1].text = displayedScore1.ToString();

            var tally0 = GameObject.Find("TallyContainer")?.GetComponent<TallyMarkDisplay>();
            var tally1 = GameObject.Find("TallyContainer1")?.GetComponent<TallyMarkDisplay>();
            tally0?.UpdateTallyDisplay(displayedScore0);
            tally1?.UpdateTallyDisplay(displayedScore1);
        }
        else if (playerSeat == 1 || playerSeat == 3)
        {
            pointTexts[1].text = displayedScore0.ToString();
            pointTexts[0].text = displayedScore1.ToString();

            var tally0 = GameObject.Find("TallyContainer")?.GetComponent<TallyMarkDisplay>();
            var tally1 = GameObject.Find("TallyContainer1")?.GetComponent<TallyMarkDisplay>();
            tally0?.UpdateTallyDisplay(displayedScore1);
            tally1?.UpdateTallyDisplay(displayedScore0);
        }
        else
        {
            // Default fallback if seat is not assigned
            pointTexts[0].text = displayedScore0.ToString();
            pointTexts[1].text = displayedScore1.ToString();
        }

        if (SideManager.Instance != null)
        {
            SideManager.Instance.UpdatePoints(displayedScore0, displayedScore1);
        }
    }

    /// <summary>
    /// Applies live score updates received from server during gameplay.
    /// </summary>
    public void ApplyLiveScoreUpdate(int point0, int point1, int p1Self = 0, int p2Self = 0, int p1Opp = 0, int p2Opp = 0)
    {
        UpdatePointText(point0, point1, p1Self, p2Self, p1Opp, p2Opp);
    }



    public void GetPlayerNumber(int playerNumber)

    {
        
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

        networkRelay = FindObjectOfType<GameNetworkRelay>();





        //Setting the localInstances for ClientRPC messages

        if (LocalInstance == null)

        {

            LocalInstance = this;

        }



        ProfileScript profileScript = GameObject.Find("MainUI").GetComponent<ProfileScript>();

        cardBackSprite = profileScript.cardBackSprites[profileScript.cardBackIndex];



        pointTexts.Add(GameObject.Find("PlayerPointText1").GetComponent<Text>());

        GameObject.Find("PlayerPointText1").GetComponent<Text>().text = "0";

        pointTexts.Add(GameObject.Find("PlayerPointText2").GetComponent<Text>());

        GameObject.Find("PlayerPointText2").GetComponent<Text>().text = "0";



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
            explosionObject = centerObj.transform.Find("Explosion")?.gameObject;

            if (explosionObject != null)

            {

                explosionAnimator = explosionObject.GetComponent<Animator>();

                explosionObject.SetActive(false);
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

        // Determine which hand the current player should be playing from
        string expectedHandName = "";
        if (deckController.playerCount == 4)
        {
            expectedHandName = "PlayerHand" + (currentPlayerNo + 1);
        }
        else if (deckController.playerCount == 2)
        {
            int handIndex = (currentPlayerNo == 0) ? 0 : 2;
            expectedHandName = "PlayerHand" + (handIndex + 1);
        }

        bool isInCurrentPlayerHand = cardObj != null && cardObj.transform.parent != null && cardObj.transform.parent.name == expectedHandName;
        bool isBotPlaying = botControlledPlayers.Contains(currentPlayerNo) && isInCurrentPlayerHand;

        AddToDebugLog($"[GameManager] Expected Hand: {expectedHandName}, Is in hand: {isInCurrentPlayerHand}, Is bot playing: {isBotPlaying}");

        // Player can play if:
        // 1. It's their turn AND the card is in their local hand (PlayerHand1), OR
        // 2. It's a bot-controlled turn AND the card is in the bot's corresponding hand
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

        if (isBuDahaIyiPending)

        {

            AddToDebugLog($"[GameManager] Bu Daha İyi pending - allowing out-of-turn interaction");

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

        networkRelay.TurnProcessedServerRPC(Unity.Netcode.NetworkManager.Singleton.LocalClientId);

        AddToDebugLog($"[GameManager] TurnProcessedServerRPC called");

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



        playerPiştiPoolTransforms.Add(GameObject.Find("PlayerPiştiPool1").GetComponent<Transform>());

        playerPiştiPoolTransforms.Add(GameObject.Find("PlayerPiştiPool2").GetComponent<Transform>());



        verZehriObject = GameObject.Find("VerZehriFog");

        kutsalDesteObject = GameObject.Find("KutsalDesteFog");

    }



    /// <summary>

    /// Activates the "peek at a random opponent card" super power locally and sends the move to the server.

    /// </summary>

    public void UsePeekOpponentCardPower()
    {
        
        powerOrchestrator?.ExecutePeekOpponentCard();
    }

    public void OnPeekOpponentCardSynced(int opponentPlayerNo, int cardIndex)
    {
        Debug.Log($"[UcundanGözAt] GameManager.OnPeekOpponentCardSynced() called: opponentPlayerNo={opponentPlayerNo}, cardIndex={cardIndex}");
        
        if (deckController == null)
        {
            Debug.LogError("[UcundanGözAt] GameManager: deckController is null");
            return;
        }
        
        Debug.Log($"[UcundanGözAt] GameManager: Delegating to deckController.PeekOpponentCard()");
        deckController.PeekOpponentCard(opponentPlayerNo, cardIndex);
        Debug.Log("[UcundanGözAt] GameManager.OnPeekOpponentCardSynced() completed");
    }

    public void UseBayaBayaBakPower()
    {
        
        DebugChainPrinter.LocalInstance?.TrackPowerUsage("BayaBayaBak", deckController.thisPlayerNumber, "Routing to orchestrator");
        DebugChainPrinter.LocalInstance?.TrackLocalAction("UseBayaBayaBakPower - routing to orchestrator");

        powerOrchestrator?.ExecuteBayaBayaBak();

        
    }

    public void OnBayaBayaBakSynced(int opponentPlayerNo)
    {
        
        
        // Track the power effect
        DebugChainPrinter.LocalInstance?.TrackLocalAction($"OnBayaBayaBakSynced received for opponent {opponentPlayerNo}");
        DebugChainPrinter.LocalInstance?.TrackPowerUsage("BayaBayaBak", deckController.thisPlayerNumber, $"Effect applied - revealed opponent {opponentPlayerNo} cards");
        
        // Track power completion
        MoveChainIntegrator.ReportPowerCompletion("BayaBayaBak", deckController.thisPlayerNumber, $"Revealed opponent {opponentPlayerNo} cards");
        
        // CRITICAL FIX: Force game state save after power completion to ensure reconnection sync
        powerProcessor?.OnGameStateMutated();

        deckController.PeekOpponentCardAll(opponentPlayerNo);

        
    }

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

    private IEnumerator DelayedSwap(int myPlayerNo, string myHandCardID, int opponentPlayerNo, string oppHandCardID)

    {

        yield return new WaitForSeconds(0.5f); // Adjust as needed

        networkRelay.UseSunuDegisTokusServerRPC(myHandCardID, oppHandCardID);

    }



    public void ActivateValeArarPower()
    {
        // INTENTIONALLY CLIENT-LOCAL: Vale Arar only reveals Jack highlights to the activating player.
        // This is private information — it does not change game state for any other client.
        // No server sync is needed or appropriate here.
        
        
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
    public bool isKopyalaSelectingSource = false;

    // Call this when the power is activated.
    // Pass null to start from phase 1 (source selection); pass a card to skip directly to phase 2.
    public void StartKopyalaYapistirDualSelection(CardInteraction sourceCard)
    {
        if (sourceCard == null)
        {
            // Phase 1: player must select the source card first
            
            isKopyalaSelectingSource = true;
            isKopyalaActive = false;
            kopyalaSourceCard = null;

            // Pause turn timer and start power duration timer
            if (networkRelay != null)
            {
                networkRelay.PauseTurnTimerForPowerServerRPC();
                networkRelay.StartPowerDurationTimerServerRPC();
            }

            // Keep other hands enlarged while selecting source/target cards.
            if (DeckController.LocalInstance != null)
            {
                DeckController.LocalInstance.ShowcaseAllOtherHands(false);
            }

            
        }
        else
        {
            // Phase 2: source card already known, wait for target card
            
            isKopyalaSelectingSource = false;
            isKopyalaActive = true;
            kopyalaSourceCard = sourceCard;

            if (DeckController.LocalInstance != null)
            {
                DeckController.LocalInstance.ShowcaseAllOtherHands(false);
            }

            
        }
    }

    /// <summary>Called from CardInteraction when the player taps a card during Kopyala phase 1.</summary>
    public void SelectKopyalaSource(CardInteraction sourceCard)
    {
        
        isKopyalaSelectingSource = false;
        isKopyalaActive = true;
        kopyalaSourceCard = sourceCard;

        if (DeckController.LocalInstance != null)
        {
            DeckController.LocalInstance.ShowcaseAllOtherHands(false);
        }

        // Update InfoBox to guide target selection
        SuperPowerSpawner.LocalInstance?.ShowDualSelectionStepText("Yapıştırmak için bir kart seç");

        
    }
    






    // Call this from CardInteraction when a card is clicked and isKopyalaActive is true

    public void TryKopyalaYapistir(CardInteraction targetCard)
    {
        

        if (!isKopyalaActive || kopyalaSourceCard == null || targetCard == null || targetCard == kopyalaSourceCard)
        {
            
            return;
        }

        

        // NOW call PowerActivated() since both cards are selected and we're executing the power
        if (DeckController.LocalInstance != null)
        {
            
            // Create a temporary power instance to call PowerActivated
            var tempPower = ScriptableObject.CreateInstance<KopyalaYapistir>();
            tempPower.PowerActivated();
            DestroyImmediate(tempPower);
        }

        // Network the change to server and all clients
        networkRelay.KopyalaYapistirServerRPC(targetCard.uniqueCardInstanceID, kopyalaSourceCard.uniqueCardInstanceID);

        // Stop hand showcasing
        
        if (SuperPowerSpawner.LocalInstance != null)
        {
            SuperPowerSpawner.LocalInstance.StopHandShowcase();
            
        }
        else
        {
            
        }

        // Close InfoBox now that the copy is complete
        if (SuperPowerSpawner.LocalInstance != null)
            StartCoroutine(SuperPowerSpawner.LocalInstance.CloseInfoBox());

        // Exit other-hand showcase
        if (DeckController.LocalInstance != null)
            DeckController.LocalInstance.ExitShowcaseAllOtherHands();
        
        // Reset state
        isKopyalaActive = false;
        isKopyalaSelectingSource = false;
        kopyalaSourceCard = null;
        CardInteraction.currentlySelectedCard = null;
        SetCurrentSelectedHandCardNull();
    }



    public void OnKopyalaYapistir(string targetUniqueID, string sourceUniqueID, bool instant = false)

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
            powerProcessor?.OnGameStateMutated();

            // Copy cardID and sprite

            int[] newCardID = sourceCard.GetCardID();

            Sprite newSprite = sourceCard.GetComponent<SpriteRenderer>().sprite;

            targetCard.activePowerEffect = sourceCard.activePowerEffect; // Copy active power effect
            copiedCardMap[targetUniqueID] = sourceUniqueID;
            

            // Set cardID and sprite with fade-in (or instantly)
            if (instant)
            {
                targetCard.SetCardID(newCardID);
                targetCard.GetComponent<SpriteRenderer>().sprite = newSprite;
                targetCard.GetComponent<SpriteRenderer>().color = Color.white;
            }
            else
            {
                StartCoroutine(SetCardIDAndSpriteWithFade(targetCard, newCardID, newSprite));
            }

            ResetVisuals(targetCard);

            if (targetCard.activePowerEffect != "none")

            {

                CopyEffectVisuals(targetCard, instant);

            }

            ShowAffectedCardsDebugVisual("Kopyala Yapıştır", sourceUniqueID, targetUniqueID);

        }
        else
        {
            
        }

    }



    private void CopyEffectVisuals(CardInteraction targetCard, bool instant = false)

    {

        string effectName = targetCard.activePowerEffect;

        if (effectName == "Kapkaç")

        {

            if (instant)
            {
                OnKapkacCardChanged(targetCard.uniqueCardInstanceID, true);
            }
            else
            {
                StartCoroutine(KapkacCourotine(targetCard.uniqueCardInstanceID));
            }

        }

        else if (effectName == "YandımAnam")

        {

            if (instant)
            {
                OnYandimAnamCardChanged(targetCard.uniqueCardInstanceID, true);
            }
            else
            {
                StartCoroutine(YandimAnamCoroutine(targetCard.uniqueCardInstanceID));
            }

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

            

            return;

        }



        var handTransform = playerHandTransforms[playerIndex - 1];

        if (handTransform.childCount == 0)

        {

            

            return;

        }



        // Get the first card in the hand (change index if you want a different card)

        var cardObj = handTransform.GetChild(2).gameObject;

        var cardScript = cardObj.GetComponent<CardInteraction>();

        if (cardScript == null)

        {

            

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

        // Keep explosion disabled until cards are moved to BombedStack.
        if (explosionObject != null) explosionObject.SetActive(false);



        GameObject bombedStack = GameObject.Find("BombedStack");

        if (bombedStack == null)

        {

            

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



        // 2. Activate and play Explosion animation

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

            

        }



        // 3. Clear all center-related lists/dictionaries (as before)

        centerCardsObjects.Clear();

        if (centerCardIDList != null) centerCardIDList.Clear();



        

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



    public void StartKapkacSelectionPower()
    {
        

        isKapkacPending = true;
        isYandimAnamPending = false;
        isBuDahaIyiPending = false;

        CardInteraction.currentlySelectedCard = null;
        SetCurrentSelectedHandCardNull();

        if (networkRelay != null)
        {
            networkRelay.PauseTurnTimerForPowerServerRPC();
            networkRelay.StartPowerDurationTimerServerRPC();
            
        }

        if (DeckController.LocalInstance != null)
        {
            // Keep own hand at normal size while other player hands are enlarged.
            DeckController.LocalInstance.ShowcaseAllOtherHands(false);
        }

        SuperPowerSpawner.LocalInstance?.ShowDualSelectionStepText("Değiştirmek için bir kart seç");
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

        

        // TODO: Add your visual effect here

        foreach (Transform child in verZehriObject.transform)

        {

            child.GetComponent<FogController>().StartFog();

        }



    }



    private void StopVerZehriEffect()

    {

        // Hide UI/animation for VerZehri

        

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

        

        // TODO: Add your visual effect here

        foreach (Transform child in kutsalDesteObject.transform)

        {

            child.GetComponent<FogController>().StartFog();

        }

    }



    private void StopKutsalDesteEffect()

    {

        

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

            

            return;

        }

        // Get the top card of the center pile (last added)

        string topCenterCardID = centerCards.Keys.Last();

        networkRelay.UseBuDahaIyiServerRPC(currentSelectedHandCard, topCenterCardID);

    }

    public void StartBuDahaIyiSelectionPower()

    {

        

        isBuDahaIyiPending = true;
        isKapkacPending = false;
        isYandimAnamPending = false;

        CardInteraction.currentlySelectedCard = null;
        SetCurrentSelectedHandCardNull();

        if (networkRelay != null)
        {
            networkRelay.PauseTurnTimerForPowerServerRPC();
            networkRelay.StartPowerDurationTimerServerRPC();
            
        }

        if (DeckController.LocalInstance != null)
        {
            // Keep own hand at normal size while other player hands are enlarged.
            DeckController.LocalInstance.ShowcaseAllOtherHands(false);
        }

        SuperPowerSpawner.LocalInstance?.ShowDualSelectionStepText("Orta ile değiş tokuş için bir el kartı seç");
    }



    public void OnBuDahaIyiSynced(int activatingPlayerNo, int handCardOwnerPlayerNo, string handCardID, string centerCardID)

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
        MoveChainIntegrator.ReportPowerCompletion("Bu Daha İyi", activatingPlayerNo, $"Swapped hand card {handCardID} (owner P{handCardOwnerPlayerNo}) with center card {centerCardID}");

        // CRITICAL FIX: Force game state save after power completion to ensure reconnection sync
        powerProcessor?.OnGameStateMutated();

        // Swap card objects visually
        ShowAffectedCardsDebugVisual("Bu Daha İyi", handCardID, centerCardID);
        
        StartCoroutine(AnimateBuDahaIyiSwap(handCardID, centerCardID, handCardOwnerPlayerNo));

    }

    private IEnumerator AnimateBuDahaIyiSwap(string handCardID, string centerCardID, int playerNo)
    {
    // Animate swap
    yield return StartCoroutine(deckController.SwapHandCardWithCenterCard(handCardID, centerCardID, playerNo));

    // Update showcase originals for both swapped cards
    deckController.UpdateShowcaseOriginalsAfterSwap(CardInteraction.cardLookup[handCardID].gameObject);
    deckController.UpdateShowcaseOriginalsAfterSwap(CardInteraction.cardLookup[centerCardID].gameObject);

    // Now exit showcase
    deckController.ExitShowcaseAllOtherHands();
    deckController.TryStopShowcaseCenterCards();
    }



    // Add at the top of GameManager.cs

    public bool isSunuDegisTokusActive = false;

    public string sunuDegisTokusFirstCard = null;

    public bool isSunuDegisTokusSelectingOpponent = false;
    private string activeSunuDegisTokusPowerName = "Şunu Değiş Tokuş";

    [Header("Debug Visuals")]
    public bool debugVisualsEnabled = false;
    public bool debugDegisTokusCandidatesEnabled = true;

    public bool IsDegisTokusDebugVisualEnabled()
    {
        return debugVisualsEnabled && debugDegisTokusCandidatesEnabled;
    }

    [ContextMenu("Debug/Toggle Visual Debug")]
    public void ToggleVisualDebug()
    {
        debugVisualsEnabled = !debugVisualsEnabled;
        
    }

    [ContextMenu("Debug/Toggle Değiş Tokuş Candidate Debug")]
    public void ToggleDegisTokusCandidateDebug()
    {
        debugDegisTokusCandidatesEnabled = !debugDegisTokusCandidatesEnabled;
        
    }

    private void ShowAffectedCardsDebugVisual(string powerName, params string[] cardIds)
    {
        if (!debugVisualsEnabled || cardIds == null || cardIds.Length == 0)
        {
            return;
        }

        Color highlightColor = GetDebugColorForPower(powerName);
        float holdDuration = 1.2f;

        for (int i = 0; i < cardIds.Length; i++)
        {
            string cardId = cardIds[i];
            if (string.IsNullOrEmpty(cardId))
            {
                continue;
            }

            if (!CardInteraction.cardLookup.TryGetValue(cardId, out var cardInteraction) || cardInteraction == null)
            {
                continue;
            }

            cardInteraction.ShowDebugCandidateIndicator(highlightColor);
            StartCoroutine(HideAffectedCardDebugVisualAfterDelay(cardInteraction, holdDuration));
        }
    }

    private IEnumerator HideAffectedCardDebugVisualAfterDelay(CardInteraction cardInteraction, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (cardInteraction == null)
        {
            yield break;
        }

        // Avoid overriding active manual selection feedback.
        if (CardInteraction.currentlySelectedCard == cardInteraction)
        {
            yield break;
        }

        cardInteraction.HideDebugCandidateIndicator();
    }

    private Color GetDebugColorForPower(string powerName)
    {
        switch (powerName)
        {
            case "Kapkaç":
                return new Color(0.2f, 1f, 0.25f, 0.85f);
            case "Yandım Anam":
                return new Color(1f, 0.35f, 0.15f, 0.85f);
            case "Kopyala Yapıştır":
                return new Color(0.25f, 0.8f, 1f, 0.85f);
            case "Şunu Değiş Tokuş":
            case "Değiş Tokuş":
            case "Şunu Değiş Bunu Tokuş":
                return new Color(1f, 0.95f, 0.2f, 0.85f);
            default:
                return new Color(1f, 1f, 1f, 0.85f);
        }
    }



    // Call this to activate the power

    // New dual selection method for Şunu Değiş Tokuş / Değiş Tokuş
    public void StartSunuDegisTokusDualSelection(string myHandCard, string powerName = "Şunu Değiş Tokuş")
    {
        
        
        isSunuDegisTokusActive = true;
        sunuDegisTokusFirstCard = null;
        isSunuDegisTokusSelectingOpponent = false;
        activeSunuDegisTokusPowerName = powerName;

        // Pause the normal turn timer and start the power-selection timeout
        if (networkRelay != null)
        {
            networkRelay.PauseTurnTimerForPowerServerRPC();
            networkRelay.StartPowerDurationTimerServerRPC();
        }
        
        // All hand cards are selectable in this mode, so showcase every hand consistently.
        if (DeckController.LocalInstance != null)
        {
            DeckController.LocalInstance.ShowcaseAllOtherHands();
        }
        else
        {
            
        }

        SuperPowerSpawner.LocalInstance?.ShowDualSelectionStepText("Adım 1/2: Değiş tokuş için ilk el kartını seç");
        
    }

    /// <summary>
    /// Called when the server-side power duration timer expires. Cancels the active dual-selection power
    /// and returns the player to the normal play state so they can still play a card this turn.
    /// The server automatically restarts the turn timer after calling this.
    /// </summary>
    public void CancelDualSelectionPower()
    {
        if (!isSunuDegisTokusActive && !isSunuDegisBunuTokusActive && !isKopyalaActive && !isKopyalaSelectingSource && !isKapkacPending && !isYandimAnamPending && !isBuDahaIyiPending) return;

        if (isBuDahaIyiPending)
        {
            

            isBuDahaIyiPending = false;
            CardInteraction.currentlySelectedCard = null;
            SetCurrentSelectedHandCardNull();

            if (DeckController.LocalInstance != null)
                DeckController.LocalInstance.ExitShowcaseAllOtherHands();

            if (SuperPowerSpawner.LocalInstance != null)
                StartCoroutine(SuperPowerSpawner.LocalInstance.CloseInfoBox());

            
            return;
        }

        if (isKapkacPending || isYandimAnamPending)
        {
            string activePower = isKapkacPending ? "Kapkaç" : "Yandım Anam";
            

            isKapkacPending = false;
            isYandimAnamPending = false;
            isBuDahaIyiPending = false;
            CardInteraction.currentlySelectedCard = null;
            SetCurrentSelectedHandCardNull();

            if (DeckController.LocalInstance != null)
                DeckController.LocalInstance.ExitShowcaseAllOtherHands();

            if (SuperPowerSpawner.LocalInstance != null)
                StartCoroutine(SuperPowerSpawner.LocalInstance.CloseInfoBox());

            
            return;
        }

        if (isKopyalaActive || isKopyalaSelectingSource)
        {
            
            isKopyalaActive = false;
            isKopyalaSelectingSource = false;
            kopyalaSourceCard = null;
            CardInteraction.currentlySelectedCard = null;

            if (DeckController.LocalInstance != null)
                DeckController.LocalInstance.ExitShowcaseAllOtherHands();

            if (SuperPowerSpawner.LocalInstance != null)
                StartCoroutine(SuperPowerSpawner.LocalInstance.CloseInfoBox());

            
            return;
        }

        if (isSunuDegisBunuTokusActive)
        {
            

            ClearSunuDegisBunuTokusIndicators();
            ResetSunuDegisBunuTokusState(true);

            if (DeckController.LocalInstance != null)
                DeckController.LocalInstance.ExitShowcaseAllOtherHands();

            if (SuperPowerSpawner.LocalInstance != null)
                StartCoroutine(SuperPowerSpawner.LocalInstance.CloseInfoBox());

            
            return;
        }

        

        // Reset dual-selection state
        isSunuDegisTokusActive = false;
        isSunuDegisTokusSelectingOpponent = false;
        sunuDegisTokusFirstCard = null;
        activeSunuDegisTokusPowerName = "Şunu Değiş Tokuş";

        // Exit any active showcase so cards return to normal layout
        if (DeckController.LocalInstance != null)
            DeckController.LocalInstance.ExitShowcaseAllOtherHands();

        
    }

    public bool IsSunuDegisTokusSelectingOpponent()
    {
        return isSunuDegisTokusSelectingOpponent;
    }

    private void TriggerActiveSunuDegisTokusPowerActivated()
    {
        if (DeckController.LocalInstance == null)
            return;

        if (activeSunuDegisTokusPowerName == "Değiş Tokuş")
        {
            var tempPower = ScriptableObject.CreateInstance<DeğişTokuş>();
            tempPower.PowerActivated();
            DestroyImmediate(tempPower);
            return;
        }

        var defaultPower = ScriptableObject.CreateInstance<SunuDegisTokus>();
        defaultPower.PowerActivated();
        DestroyImmediate(defaultPower);
    }
    
    public IEnumerator OnSunuDegisTokusSynced(int myPlayerNo, int otherPlayerNo, string myHandCardID, string otherHandCardID)
    {
        // Track the card swap in move chain
        MoveChainIntegrator.TrackCardSwap(myPlayerNo, otherPlayerNo, myHandCardID, otherHandCardID, "Şunu Değiş Tokuş power");
        
        // Track power completion
        MoveChainIntegrator.ReportPowerCompletion("Şunu Değiş Tokuş", myPlayerNo, $"Swapped {myHandCardID} with {otherHandCardID} from P{otherPlayerNo}");
        
        // CRITICAL FIX: Force game state save after power completion to ensure reconnection sync
        powerProcessor?.OnGameStateMutated();
        
        // Swap in myCards if relevant
        if (deckController.thisPlayerNumber == myPlayerNo && deckController.thisPlayerNumber == otherPlayerNo)
        {
            int idxA = myCards.IndexOf(myHandCardID);
            int idxB = myCards.IndexOf(otherHandCardID);
            if (idxA != -1 && idxB != -1)
            {
                myCards[idxA] = otherHandCardID;
                myCards[idxB] = myHandCardID;
            }
        }
        else if (deckController.thisPlayerNumber == myPlayerNo)
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
        ShowAffectedCardsDebugVisual("Şunu Değiş Tokuş", myHandCardID, otherHandCardID);
        DeckController.LocalInstance.ExitShowcaseAllOtherHands();
        DeckController.LocalInstance.TryStopShowcaseCenterCards();
    }



    // Add at the top of GameManager.cs

    public bool isSunuDegisBunuTokusActive = false;

    private int sunuDegisBunuTokusSwapIndex = 0;

    private List<string> sunuDegisBunuTokusMyHandSnapshot = null;

    private int sunuDegisBunuTokusTotalSwaps = 0;

    // Opponent cards selected so far (in order), collected before any RPCs are sent.
    private List<string> sunuDegisBunuTokusSelectedOpponentCards = new List<string>();

    // Sequential swap queue — prevents concurrent coroutines from causing showcase restoration to
    // fire while earlier swap animations are still running (which would snap cards back mid-swap).
    private Queue<IEnumerator> sunuDegisBunuTokusSwapQueue = new Queue<IEnumerator>();
    private bool sunuDegisBunuTokusSwapProcessing = false;
    private bool sunuDegisBunuTokusActivationPending = false;
    private int sunuDegisBunuTokusExpectedSyncs = 0;
    private int sunuDegisBunuTokusCompletedSyncs = 0;

    public void EnqueueSunuDegisBunuTokusSwap(int myPlayerNo, int otherPlayerNo, string myHandCardID, string otherHandCardID, int myHandIndex, bool readyToExit)
    {
        
        sunuDegisBunuTokusSwapQueue.Enqueue(OnSunuDegisBunuTokusSynced(myPlayerNo, otherPlayerNo, myHandCardID, otherHandCardID, myHandIndex, readyToExit));
        if (!sunuDegisBunuTokusSwapProcessing)
            StartCoroutine(ProcessSunuDegisBunuTokusSwapQueue());
    }

    private IEnumerator ProcessSunuDegisBunuTokusSwapQueue()
    {
        sunuDegisBunuTokusSwapProcessing = true;
        
        while (sunuDegisBunuTokusSwapQueue.Count > 0)
        {
            
            yield return StartCoroutine(sunuDegisBunuTokusSwapQueue.Dequeue());
        }
        sunuDegisBunuTokusSwapProcessing = false;
        
    }



    // Call this to activate the power

    public void ActivateSunuDegisBunuTokusPower()

    {

        List<string> localHandSnapshot = GetCurrentLocalHandCardIdsForSwap();
        if (localHandSnapshot.Count == 0 && myCards != null)
        {
            localHandSnapshot = new List<string>(myCards.Where(id => !string.IsNullOrEmpty(id)));
        }

        if (localHandSnapshot.Count == 0)

        {

            

            return;

        }

        // Count total available opponent cards. Cap totalSwaps so we never ask for
        // more selections than there are opponent cards to pick from.
        int availableOpponentCards = GetAvailableOpponentCardCount();
        if (availableOpponentCards == 0)
        {
            
            return;
        }

        isSunuDegisBunuTokusActive = true;

        sunuDegisBunuTokusSwapIndex = 0;

        sunuDegisBunuTokusMyHandSnapshot = localHandSnapshot;
        sunuDegisBunuTokusTotalSwaps = Mathf.Min(sunuDegisBunuTokusMyHandSnapshot.Count, availableOpponentCards);
        sunuDegisBunuTokusSelectedOpponentCards = new List<string>();
        sunuDegisBunuTokusActivationPending = false;
        sunuDegisBunuTokusExpectedSyncs = 0;
        sunuDegisBunuTokusCompletedSyncs = 0;

        
        

        // Pause turn timer and start power duration timer
        if (networkRelay != null)
        {
            networkRelay.PauseTurnTimerForPowerServerRPC();
            networkRelay.StartPowerDurationTimerServerRPC();
        }

        // Show other hands at centerScale, bring own hand down to normalScale
        if (DeckController.LocalInstance != null)
        {
            DeckController.LocalInstance.ShowcaseAllOtherHands(false);
            DeckController.LocalInstance.TemporarilySetOwnHandToNormalScale();
        }

        // Show first selection prompt in InfoBox
        SuperPowerSpawner.LocalInstance?.ShowDualSelectionStepText(GetSunuDegisBunuTokusSelectionText(0, sunuDegisBunuTokusTotalSwaps));

        // Start hand showcasing for multi-swap selection
        
        if (SuperPowerSpawner.LocalInstance != null)
        {
            SuperPowerSpawner.LocalInstance.StartHandShowcaseForDualSelection("Şunu Değiş Bunu Tokuş");
            
        }
        else
        {
            
        }

    }

    private List<string> GetCurrentLocalHandCardIdsForSwap()
    {
        var result = new List<string>();
        if (deckController == null || deckController.playerHandTransforms == null || deckController.playerHandTransforms.Count == 0)
        {
            return result;
        }

        Transform localHand = deckController.playerHandTransforms[0];
        if (localHand == null)
        {
            return result;
        }

        int childIndex = 0;
        foreach (Transform child in localHand)
        {
            // Player hand container keeps non-card helper children in the first two slots.
            if (childIndex > 1)
            {
                CardInteraction ci = child.GetComponent<CardInteraction>();
                if (ci != null && !string.IsNullOrEmpty(ci.uniqueCardInstanceID))
                {
                    result.Add(ci.uniqueCardInstanceID);
                }
            }

            childIndex++;
        }

        return result;
    }

    /// <summary>
    /// Returns the total number of cards in all opponent hand transforms (indices 1+).
    /// Used to cap how many swaps Şunu Değiş Bunu Tokuş can make.
    /// </summary>
    private int GetAvailableOpponentCardCount()
    {
        int count = 0;
        if (deckController == null || deckController.playerHandTransforms == null) return 0;
        // Index 0 is the local player. Indices 1+ are opponents (relative layout).
        for (int i = 1; i < deckController.playerHandTransforms.Count; i++)
        {
            Transform hand = deckController.playerHandTransforms[i];
            if (hand == null) continue;
            int childIndex = 0;
            foreach (Transform child in hand)
            {
                if (childIndex > 1)
                {
                    CardInteraction ci = child.GetComponent<CardInteraction>();
                    if (ci != null && !string.IsNullOrEmpty(ci.uniqueCardInstanceID))
                    {
                        count++;
                    }
                }
                childIndex++;
            }
        }
        
        return count;
    }

    private void ResetSunuDegisBunuTokusState(bool clearQueue)
    {
        isSunuDegisBunuTokusActive = false;
        sunuDegisBunuTokusActivationPending = false;
        sunuDegisBunuTokusMyHandSnapshot = null;
        sunuDegisBunuTokusSwapIndex = 0;
        sunuDegisBunuTokusTotalSwaps = 0;
        sunuDegisBunuTokusExpectedSyncs = 0;
        sunuDegisBunuTokusCompletedSyncs = 0;
        sunuDegisBunuTokusSelectedOpponentCards?.Clear();
        if (clearQueue)
        {
            sunuDegisBunuTokusSwapQueue?.Clear();
            sunuDegisBunuTokusSwapProcessing = false;
        }
    }

    private void FinalizeSunuDegisBunuTokusActivation()
    {
        

        deckController.ExitShowcaseAllOtherHands();
        deckController.TryStopShowcaseCenterCards();

        if (SuperPowerSpawner.LocalInstance != null)
        {
            SuperPowerSpawner.LocalInstance.StopHandShowcase();
            StartCoroutine(SuperPowerSpawner.LocalInstance.CloseInfoBox());
        }

        ResetSunuDegisBunuTokusState(false);

        // CRITICAL FIX: Force game state save after power completion to ensure reconnection sync
        powerProcessor?.OnGameStateMutated();
    }

    private string GetSunuDegisBunuTokusSelectionText(int index, int totalSwaps)
    {
        string[] ordinals = { "İlk", "İkinci", "Üçüncü", "Dördüncü" };
        string ordinal = index < ordinals.Length ? ordinals[index] : $"{index + 1}.";
        int total = Mathf.Max(totalSwaps, 1);
        return $"{ordinal} kartın ile değiştirmek için bir kart seç ({index + 1}/{total})";
    }

    private void ClearSunuDegisBunuTokusIndicators()
    {
        if (sunuDegisBunuTokusSelectedOpponentCards == null) return;
        foreach (string cardID in sunuDegisBunuTokusSelectedOpponentCards)
        {
            if (CardInteraction.cardLookup.TryGetValue(cardID, out var ci))
                ci.HideDebugCandidateIndicator();
        }
        // Clear own-hand card indicators for all recorded pairs
        if (sunuDegisBunuTokusMyHandSnapshot != null)
        {
            int pairCount = sunuDegisBunuTokusSelectedOpponentCards.Count;
            for (int i = 0; i < pairCount && i < sunuDegisBunuTokusMyHandSnapshot.Count; i++)
            {
                if (CardInteraction.cardLookup.TryGetValue(sunuDegisBunuTokusMyHandSnapshot[i], out var ci))
                    ci.HideDebugCandidateIndicator();
            }
        }
    }

    /// <summary>
    /// Called when the player has finished making all opponent-card selections for
    /// Şunu Değiş Bunu Tokuş.  Clears indicators, sends the swap RPCs to the server
    /// in order, then resets all local selection state.
    /// </summary>
    private void CompleteSunuDegisBunuTokusActivation()
    {
        int pairCount = sunuDegisBunuTokusSelectedOpponentCards.Count;
        

        if (pairCount == 0)
        {
            
            ResetSunuDegisBunuTokusState(false);
            return;
        }

        // Local tracking (no showcase — server fires that on the last swap)
        if (DeckController.LocalInstance != null)
        {
            var tempPower = ScriptableObject.CreateInstance<SunuDegisBunuTokus>();
            tempPower.PowerActivated();
            DestroyImmediate(tempPower);
        }

        // Clear selection indicators before animation starts
        ClearSunuDegisBunuTokusIndicators();

        isSunuDegisBunuTokusActive = false;
        sunuDegisBunuTokusActivationPending = true;
        sunuDegisBunuTokusExpectedSyncs = pairCount;
        sunuDegisBunuTokusCompletedSyncs = 0;

        SuperPowerSpawner.LocalInstance?.ShowDualSelectionStepText($"Kartlar değiştiriliyor... (0/{pairCount})");

        // Send one RPC per swap pair, marking the last one with readyToExit=true
        for (int i = 0; i < pairCount; i++)
        {
            bool isLast = (i == pairCount - 1);
            
            networkRelay.UseSunuDegisBunuTokusServerRPC(
                sunuDegisBunuTokusMyHandSnapshot[i],
                sunuDegisBunuTokusSelectedOpponentCards[i],
                i,
                isLast);
        }

        AddToDebugLog("ŞunuDeğişBunuTokuş: All selections sent to server.");
        
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
        ShowAffectedCardsDebugVisual("Şunu Değiş Bunu Tokuş", myHandCardID, otherHandCardID);

        if (sunuDegisBunuTokusActivationPending)
        {
            sunuDegisBunuTokusCompletedSyncs++;
            
            SuperPowerSpawner.LocalInstance?.ShowDualSelectionStepText($"Kartlar değiştiriliyor... ({sunuDegisBunuTokusCompletedSyncs}/{Mathf.Max(sunuDegisBunuTokusExpectedSyncs, 1)})");
        }

        if (readyToExit)
        {
            if (!sunuDegisBunuTokusActivationPending)
            {
                
            }
            else if (sunuDegisBunuTokusCompletedSyncs >= sunuDegisBunuTokusExpectedSyncs)
            {
                FinalizeSunuDegisBunuTokusActivation();
            }
            else
            {
                
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



    public void OnKapkacCardChanged(string cardUniqueID, bool instant = false)

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
            if (instant)
            {
                // Apply visual instantly without animation
                ResetVisuals(cardInteraction);
                if (kapkacEffectPrefab != null)
                {
                    GameObject effect = Instantiate(kapkacEffectPrefab, cardInteraction.transform);
                    effect.transform.localPosition = new Vector3(0, 0, -0.001f);
                    kapkacCardsToBeReset.Add(cardInteraction.gameObject);
                    SpriteRenderer sr = effect.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        Color c = sr.color;
                        c.a = 1f;
                        sr.color = c;
                    }
                }
                ShowAffectedCardsDebugVisual("Kapkaç", cardUniqueID);
            }
            else
            {
                StartCoroutine(KapkacCourotine(cardUniqueID));
                ShowAffectedCardsDebugVisual("Kapkaç", cardUniqueID);
            }

        }

    }

    /// <summary>
    /// Called when a player disconnects from the game
    /// </summary>
    public void OnPlayerDisconnected(ulong clientId, string reason)
    {
        
        
        // TODO: Implement bot placeholder system here
        // This is where you would:
        // 1. Create a bot to replace the disconnected player
        // 2. Update the UI to show the player is now a bot
        // 3. Handle the bot's turns automatically
        
        
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
        StartYandimAnamSelectionPower();
    }

    public void StartYandimAnamSelectionPower()
    {
        

        isYandimAnamPending = true;
        isKapkacPending = false;
        isBuDahaIyiPending = false;

        CardInteraction.currentlySelectedCard = null;
        SetCurrentSelectedHandCardNull();

        if (DeckController.LocalInstance != null)
        {
            // Keep own hand at normal size while other player hands are enlarged.
            DeckController.LocalInstance.ShowcaseAllOtherHands(false);
        }

        SuperPowerSpawner.LocalInstance?.ShowDualSelectionStepText("Değiştirmek için bir kart seç");
    }





    public void OnYandimAnamCardChanged(string cardUniqueID, bool instant = false)

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

            if (instant)
            {
                // Apply visual instantly without animation
                ResetVisuals(cardInteraction);
                GameObject effect = null;
                if (yandimAnamSpritePrefab != null)
                {
                    effect = Instantiate(yandimAnamSpritePrefab, cardInteraction.transform);
                    effect.transform.localPosition = new Vector3(0, 0, -0.002f);
                    SpriteRenderer sr = effect.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        Color c = sr.color;
                        c.a = 1f;
                        sr.color = c;
                    }
                }

                // Apply missing state changes in instant path
                cardInteraction.SetCardValue(0);
                cardInteraction.activePowerEffect = "YandımAnam";
                cardPowerEffects[cardUniqueID] = "YandımAnam";
                cardInteraction.yandimAnamSpriteInstance = effect;

                ShowAffectedCardsDebugVisual("Yandım Anam", cardUniqueID);
            }
            else
            {
                StartCoroutine(YandimAnamCoroutine(cardUniqueID));
                ShowAffectedCardsDebugVisual("Yandım Anam", cardUniqueID);
            }

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
            
            StartCoroutine(ApplyGameStateWithReset(snapshot));
        }
        else
        {
            
            SyncLogError("deckController is null; cannot apply game state");
        }
    }

    private IEnumerator ApplyGameStateWithReset(SerializableGameState snapshot)
    {
        

        // CRITICAL: Reset transient state BEFORE initializing deck or resetting cards
        // This ensures flags like alreadySubbed are cleared so new cards can subscribe to events.
        ResetTransientClientState();
        
        // Enable instant mode for all animations during reconnection
        if (deckController != null)
        {
            deckController.isInstantMode = true;
            
        }
        
        // Ensure cards are initialized if this client is reconnecting and bypassed standard initialization
        if (deckController != null && (deckController.cardInteractionList == null || deckController.cardInteractionList.Count == 0))
        {
            
            yield return StartCoroutine(deckController.DeckStart());
        }

        // CRITICAL RECONNECTION FIX: Rebuild static cardLookup dictionary with active references
        if (deckController != null)
        {
            deckController.RebuildCardLookup();
        }

        // Hard reset the scene cards first so we do not stack
        SyncLog("Starting ResetCards to clear scene");
        yield return StartCoroutine(deckController.ResetCards());
        SyncLog("ResetCards completed");
        
        
        // Then continue the usual coroutine
        yield return StartCoroutine(ApplyGameStateCoroutine(snapshot));
        

        // CRITICAL RECONNECTION FIX: Ensure isReconnecting is false once state is fully restored
        isReconnecting = false;
        
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
        
        

        // 5. Apply effects and flags
        SyncLog("Step 5: Applying effects and flags");
        ApplyEffectsAndFlags(snapshot);

        // 6. Update UI and layout
        SyncLog("Step 6: Updating UI and layout");
        yield return StartCoroutine(UpdateUIAndLayout());

        // 7. Unfreeze input (ensure this always runs)
        SyncLog("Step 7: Unfreezing client after resync");
        UnfreezeClientAfterResync();

        // 8. (Reconnection completion detection is deferred — dormant path removed.)

        // Single comprehensive log for game state application completion
        Debug.Log($"[GameManager] ===== ÖNEMLİ: APPLYING GAME STATE COMPLETED =====\n" +
                 $"Snapshot version: {snapshot.snapshotVersion}\n" +
                 $"Final state: turnCounter={turnCounter}, currentPlayerNo={currentPlayerNo}\n" +
                 $"Center cards: {centerCards.Count}, My cards: {myCards.Count}\n" +
                 $"Reconnecting: {isReconnecting}\n" +
                 $"Game state application process finished successfully");
        
        SyncLog($"ApplyGameStateCoroutine completed successfully for snapshot v{snapshot.snapshotVersion}");
        // Final detailed dump AFTER apply for comparison
        LogDetailedLocalState("CLIENT AFTER APPLY");
        
        // Disable instant mode now that reconstruction is complete
        if (deckController != null)
        {
            deckController.isInstantMode = false;
            
        }
    }

    /// <summary>
    /// Freezes client input and animations during resync
    /// </summary>
    private void FreezeClientForResync()
    {
        
        
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
        
        
        // Reset selection state
        currentSelectedHandCard = null;
        CardInteraction.currentlySelectedCard = null;
        CardInteraction.isOneCardSelected = false;

        // Reset movement flags
        movePlayedLocally = false;
        isProcessingCapture = false;

        // RECONNECTION FIX: Reset critical flags to allow re-subscription and new RPCs
        alreadySubbed = false;
        hasAlreadySentRPC = false;
        EndGameplayAction("Reconnection reset");

        // Reset special power states
        isKopyalaActive = false;
        kopyalaSourceCard = null;
        isSunuDegisTokusActive = false;
        isSunuDegisTokusSelectingOpponent = false;
        sunuDegisTokusFirstCard = null;
        activeSunuDegisTokusPowerName = "Şunu Değiş Tokuş";
        isSunuDegisBunuTokusActive = false;
        sunuDegisBunuTokusMyHandSnapshot = null;
        sunuDegisBunuTokusSwapIndex = 0;
        sunuDegisBunuTokusTotalSwaps = 0;
        sunuDegisBunuTokusSelectedOpponentCards?.Clear();
        
        // Deactivate Vale Arar power at the end of round
        if (valeArarActive)
        {
            DeactivateValeArarPower();
            
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
        copiedCardMap.Clear();
        
        // CRITICAL: Reset move chains to prevent infinite desync loops
        // When we apply a full game state, the chains should be reset to match the new state
        // BUT: For reconnection, let the desync detection handle the synchronization naturally
        if (!isReconnecting)
        {
            MoveChainIntegrator.ResetChains();
            
        }
        else
        {
            
        }
    }

    /// <summary>
    /// Applies core game state values
    /// </summary>
    private void ApplyCoreGameState(SerializableGameState snapshot)
    {
        

        // Restore player number from PlayerPrefs immediately during reconnection/migration
        // to ensure we have the correct local seat perspective before card container rebuild starts.
        RestorePlayerNumberFromPrefs();
        
        // Update turn and player info from server snapshot
        currentPlayerNo = snapshot.currentPlayer;
        turnCounter = snapshot.turnCounter;
        roundCount = snapshot.roundCount;
        
        // CRITICAL FIX: Sync startingPlayerNoCounter with roundCount for reconnection
        // Both represent "how many rounds have been completed" and should be equal
        if (deckController != null)
        {
            deckController.SetStartingPlayerNoCounter(snapshot.roundCount);
            
        }
        
        // Update bot-controlled players
        botControlledPlayers.Clear();
        if (snapshot.botControlledPlayers.items != null)
        {
            botControlledPlayers.AddRange(snapshot.botControlledPlayers.items);
            
        }
        
        
        
        // Update points display instantly from the snapshot during reconnection or host migration resync
        if (snapshot.points.items != null && snapshot.points.items.Length >= 2)
        {
            UpdatePointText(
                snapshot.points.items[0], 
                snapshot.points.items[1],
                snapshot.p1side_selfFakePointReduction,
                snapshot.p2side_selfFakePointReduction,
                snapshot.p1side_oppFakePointReduction,
                snapshot.p2side_oppFakePointReduction
            );
            
        }

        // The server will handle sending the updated current player via existing RPC
        // We just need to trust that the snapshot is correct
    }

    /// <summary>
    /// NEW: Restores visual states by parsing move chain for power effects
    /// This runs after card containers are rebuilt but before applying current effects
    /// </summary>
    private IEnumerator RestoreVisualStatesFromMoveChain()
    {
        yield break;
    }
    
    /// <summary>
    /// Checks if a power changes card visuals and needs restoration
    /// </summary>
    private bool IsVisualChangingPower(string powerName)
    {
        bool isVisual = powerName == "Kopyala Yapıştır" || 
                       powerName == "Kapkaç" || 
                       powerName == "Yandım Anam";
        
        
        return isVisual;
        // Add more powers here as needed
    }
    
    /// <summary>
    /// Restores visual effect from a specific move
    /// </summary>
    private IEnumerator RestoreVisualEffectFromMove(GameMove move)
    {
        
        SyncLog($"Restoring visual effect for power: {move.superPowerName}");
        
        switch (move.superPowerName)
        {
            case "Kopyala Yapıştır":
                
                yield return StartCoroutine(RestoreKopyalaYapistirVisual(move));
                break;
                
            case "Kapkaç":
                
                yield return StartCoroutine(RestoreKapkacVisual(move));
                break;
                
            case "Yandım Anam":
                
                yield return StartCoroutine(RestoreYandimAnamVisual(move));
                break;
                
            default:
                
                SyncLogWarning($"Unknown visual power: {move.superPowerName}");
                break;
        }
        
        
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
        
        if (!CardInteraction.cardLookup.TryGetValue(targetCardId, out var targetCard))
        {
            
            yield break;
        }
        
        SyncLog($"Restoring Kopyala Yapıştır: {sourceCardId} -> {targetCardId}");
        
        // Use existing OnKopyalaYapistir method to restore visual state instantly
        OnKopyalaYapistir(targetCardId, sourceCardId, true);
        
        yield return null; // Allow one frame for processing
    }
    
    /// <summary>
    /// Restores Kapkaç visual effect from move data
    /// </summary>
    private IEnumerator RestoreKapkacVisual(GameMove move)
    {
        
        
        if (move.affectedCardIds == null || move.affectedCardIds.Length == 0)
        {
            
            SyncLogWarning("Kapkaç move missing affected card data");
            yield break;
        }
        
        string cardId = move.affectedCardIds[0];
        
        SyncLog($"Restoring Kapkaç visual effect for card: {cardId}");
        
        // Check if card exists in lookup before calling
        if (!CardInteraction.cardLookup.TryGetValue(cardId, out var targetCard))
        {
            
            yield break;
        }
        
        
        
        // Use existing OnKapkacCardChanged method to restore visual state instantly
        OnKapkacCardChanged(cardId, true);
        
        
        
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
        
        if (!CardInteraction.cardLookup.TryGetValue(cardId, out var targetCard))
        {
            
            yield break;
        }
        
        // Use existing OnYandimAnamCardChanged method to restore visual state instantly
        OnYandimAnamCardChanged(cardId, true);
        
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
        
        if (myNo < 0)
        {
            SyncLogWarning($"thisPlayerNumber is {myNo} (seat not yet assigned); waiting up to 2 seconds for identity restoration...");
            float waitTimer = 0f;
            while (deckCtrl.thisPlayerNumber < 0 && waitTimer < 2.0f)
            {
                yield return new WaitForSeconds(0.05f);
                waitTimer += 0.05f;
            }
            myNo = deckCtrl.thisPlayerNumber;
            SyncLog($"Wait finished. thisPlayerNumber is now: {myNo}");
        }

        SyncLog($"Local info: playerCount={playerCountLocal}, myNo={myNo}");
        
        if (playerCountLocal <= 0)
        {
            SyncLogWarning("playerCount is not set; skipping rebuild");
            yield break;
        }

        if (myNo < 0)
        {
            SyncLogWarning($"thisPlayerNumber is {myNo} (seat still not assigned after waiting); skipping rebuild to prevent perspective corruption");
            yield break;
        }

        // 0) Clear current center tracking (we will rebuild it exactly)
        SyncLog("Clearing current center tracking");
        centerCards.Clear();
        centerCardsObjects.Clear();

        // Clear and restore traditional first three dealt cards flags
        foreach (var kvp in CardInteraction.cardLookup)
        {
            if (kvp.Value != null)
            {
                kvp.Value.isFirstThreeDealtCard = false;
            }
        }
        var snapshotFirstThree = snapshot.firstThreeDealtCardIds.ToList();
        if (snapshotFirstThree != null)
        {
            foreach (var uid in snapshotFirstThree)
            {
                if (CardInteraction.cardLookup.TryGetValue(uid, out var ci) && ci != null)
                {
                    ci.isFirstThreeDealtCard = true;
                    SyncLog($"Flagged card {uid} as traditional first-three dealt card");
                }
            }
        }

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
                
                bool isFaceUp = !cardInteraction.isFirstThreeDealtCard;
                if (isFaceUp) // Played center cards and the 4th card should be face up
                {
                    cardObject.transform.rotation = Quaternion.Euler(centerRotation.x + 180, centerRotation.y, UnityEngine.Random.Range(-12, 12));
                }
                else // First 3 dealt center cards should be face down
                {
                    cardObject.transform.rotation = Quaternion.Euler(centerRotation.x, centerRotation.y, UnityEngine.Random.Range(-12, 12));
                }
                
                // Stack cards with an increasing height offset to match normal gameplay stacking and prevent z-fighting
                cardObject.transform.position = new Vector3(centerPosition.x, centerPosition.y + (i * 10), centerPosition.z);
                
                cardObject.transform.localScale = new Vector3(deckCtrl.centerScale, deckCtrl.centerScale, deckCtrl.centerScale);
                
                SyncLog($"Center card rebuilt: {cardId} -> [{cardInteraction.GetCardID()[0]},{cardInteraction.GetCardID()[1]}] (face up: {isFaceUp})");
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

        // 4.1) Remap pişti pools (absolute -> relative) and assign
        var absPistiPools = snapshot.pistiPools.ToDictionary();
        var relPistiPools = new Dictionary<int, List<string>>();
        SyncLog("Remapping pişti pools from absolute to relative indices");
        foreach (var kvp in absPistiPools)
        {
            int absPlayerNo = kvp.Key;
            int relIndex = (absPlayerNo - myNo + playerCountLocal) % playerCountLocal;
            relPistiPools[relIndex] = new List<string>(kvp.Value);
            SyncLog($"Pişti Pool mapping: abs P{absPlayerNo} -> rel {relIndex} ({kvp.Value.Count} cards)");
        }

        SyncLog("Calling AssignCardsToPlayerPistiPools");
        deckCtrl.AssignCardsToPlayerPistiPools(new SerializableDictionary(relPistiPools));
        SyncLog("AssignCardsToPlayerPistiPools completed");

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
        isSunuDegisTokusSelectingOpponent = false;
        activeSunuDegisTokusPowerName = "Şunu Değiş Tokuş";
        isSunuDegisBunuTokusActive = snapshot.isSunuDegisBunuTokusActive;

        // --- UNIFIED VISUAL RECONSTRUCTION PIPELINE ---
        // Reconstruct all card copies and power overlays in a single, ordered step.
        copiedCardMap = snapshot.copiedCardMap.ToDictionary();
        cardPowerEffects = snapshot.cardPowerEffects.ToDictionary();
        
        ReconstructPowerVisuals(snapshot);
        // ----------------------------------------------

        // Restore local player gold and superpower tokens from state snapshot
        if (SuperPowerSpawner.LocalInstance != null && deckController != null)
        {
            int myPlayerNo = deckController.thisPlayerNumber;
            if (myPlayerNo != -1)
            {
                // 1. Restore Gold
                var goldDict = snapshot.playerGold.ToDictionary();
                if (goldDict != null && goldDict.TryGetValue(myPlayerNo, out int restoredGold))
                {
                    SuperPowerSpawner.LocalInstance.SetGold(restoredGold);
                    
                }
                
                // 2. Restore Superpowers
                var powersDict = snapshot.playerSuperPowers.ToDictionary();
                if (powersDict != null && powersDict.TryGetValue(myPlayerNo, out List<string> restoredPowers))
                {
                    SuperPowerSpawner.LocalInstance.RestorePowers(restoredPowers);
                    
                }
            }
        }
    }

    private int localSnapshotVersion = 0;

    private int GetTransformIndexForSeat(int absSeat, int mySeat, int playerCount, int transformCount)
    {
        int relSeat = (absSeat - mySeat + playerCount) % playerCount;
        if (playerCount == 2)
        {
            // 1vs1 Board layout: bottom = index 0, top/opponent = index 2
            return (relSeat == 0) ? 0 : 2;
        }
        // 2vs2 Mode (4 players)
        return UnityEngine.Mathf.Clamp(relSeat, 0, transformCount - 1);
    }

    public SerializableGameState CaptureVisualSnapshot()
    {
        SerializableGameState snapshot = BuildEmptySerializableGameState();
        
        if (deckController == null) return snapshot;

        int playerCount = deckController.playerCount;
        int mySeat = deckController.GetThisPlayerNumber();
        
        // 1. Hands, Pools, PistiPools
        Dictionary<int, List<string>> playerHands = new Dictionary<int, List<string>>();
        Dictionary<int, List<string>> playerPools = new Dictionary<int, List<string>>();
        Dictionary<int, List<string>> playerPistiPools = new Dictionary<int, List<string>>();
        for (int i = 0; i < playerCount; i++)
        {
            playerHands[i] = new List<string>();
            playerPools[i] = new List<string>();
            playerPistiPools[i] = new List<string>();
        }

        // Hands Audit (absolute-seat first mapping)
        for (int absSeat = 0; absSeat < playerCount; absSeat++)
        {
            int transformIdx = GetTransformIndexForSeat(absSeat, mySeat, playerCount, deckController.playerHandTransforms.Count);
            if (transformIdx < 0 || transformIdx >= deckController.playerHandTransforms.Count) continue;
            Transform handTransform = deckController.playerHandTransforms[transformIdx];
            if (handTransform == null) continue;

            foreach (Transform child in handTransform)
            {
                CardInteraction ci = child.GetComponent<CardInteraction>();
                if (ci != null && !string.IsNullOrEmpty(ci.uniqueCardInstanceID)) playerHands[absSeat].Add(ci.uniqueCardInstanceID);
            }
        }

        // Pools Audit
        int absSeatSide0 = mySeat;
        int absSeatSide1 = (mySeat + 1) % playerCount;

        if (deckController.playerPoolTransforms != null && deckController.playerPoolTransforms.Count > 0 && deckController.playerPoolTransforms[0] != null)
        {
            foreach (Transform child in deckController.playerPoolTransforms[0])
            {
                CardInteraction ci = child.GetComponent<CardInteraction>();
                if (ci != null && !string.IsNullOrEmpty(ci.uniqueCardInstanceID)) playerPools[absSeatSide0].Add(ci.uniqueCardInstanceID);
            }
        }

        if (deckController.playerPoolTransforms != null && deckController.playerPoolTransforms.Count > 1 && deckController.playerPoolTransforms[1] != null)
        {
            foreach (Transform child in deckController.playerPoolTransforms[1])
            {
                CardInteraction ci = child.GetComponent<CardInteraction>();
                if (ci != null && !string.IsNullOrEmpty(ci.uniqueCardInstanceID)) playerPools[absSeatSide1].Add(ci.uniqueCardInstanceID);
            }
        }

        // PistiPools Audit
        if (deckController.playerPiştiPoolTransforms != null && deckController.playerPiştiPoolTransforms.Count > 0 && deckController.playerPiştiPoolTransforms[0] != null)
        {
            foreach (Transform child in deckController.playerPiştiPoolTransforms[0])
            {
                CardInteraction ci = child.GetComponent<CardInteraction>();
                if (ci != null && !string.IsNullOrEmpty(ci.uniqueCardInstanceID)) playerPistiPools[absSeatSide0].Add(ci.uniqueCardInstanceID);
            }
        }

        if (deckController.playerPiştiPoolTransforms != null && deckController.playerPiştiPoolTransforms.Count > 1 && deckController.playerPiştiPoolTransforms[1] != null)
        {
            foreach (Transform child in deckController.playerPiştiPoolTransforms[1])
            {
                CardInteraction ci = child.GetComponent<CardInteraction>();
                if (ci != null && !string.IsNullOrEmpty(ci.uniqueCardInstanceID)) playerPistiPools[absSeatSide1].Add(ci.uniqueCardInstanceID);
            }
        }
        
        snapshot.hands = new SerializableDictionary(playerHands);
        snapshot.pools = new SerializableDictionary(playerPools);
        snapshot.pistiPools = new SerializableDictionary(playerPistiPools);

        // 2. Center Cards
        List<string> centerList = new List<string>();
        foreach (var obj in centerCardsObjects)
        {
            if (obj != null)
            {
                CardInteraction ci = obj.GetComponent<CardInteraction>();
                if (ci != null && !string.IsNullOrEmpty(ci.uniqueCardInstanceID))
                {
                    centerList.Add(ci.uniqueCardInstanceID);
                }
            }
        }
        snapshot.center = new SerializableStringList(centerList);

        // 3. Power Effects & Flags
        snapshot.cardPowerEffects = new SerializableStringDictionary(cardPowerEffects);
        snapshot.copiedCardMap = new SerializableStringDictionary(copiedCardMap);
        snapshot.oynayamazsinActive = this.oynayamazsinActive;
        snapshot.isYapamazsınActive = this.isYapamazsınActive;
        snapshot.verZehriActive = this.verZehriActive;
        snapshot.kutsalDesteActive = this.kutsalDesteActive;
        
        snapshot.isKapkacPending = this.isKapkacPending;
        snapshot.isYandimAnamPending = this.isYandimAnamPending;
        snapshot.isKopyalaActive = this.isKopyalaActive;
        snapshot.isSunuDegisTokusActive = this.isSunuDegisTokusActive;
        snapshot.isSunuDegisBunuTokusActive = this.isSunuDegisBunuTokusActive;

        snapshot.playerCount = playerCount;
        snapshot.currentPlayer = currentPlayerNo;
        snapshot.turnCounter = this.turnCounter;
        snapshot.timestamp = DateTime.UtcNow.Ticks;
        snapshot.snapshotVersion = ++localSnapshotVersion;

        // 4. Card Master Lookup (Required for server to know kind/value of IDs)
        if (CardInteraction.cardLookup != null)
        {
            var lookupEntries = new List<CardLookupEntry>();
            foreach (var kvp in CardInteraction.cardLookup)
            {
                if (kvp.Value != null)
                {
                    int[] cardData = kvp.Value.GetCardID();
                    if (cardData != null && cardData.Length >= 2)
                    {
                        lookupEntries.Add(new CardLookupEntry
                        {
                            cardId = kvp.Key,
                            kind = cardData[0],
                            value = cardData[1]
                        });
                    }
                    else
                    {
                        
                    }
                }
            }
            snapshot.cardLookup = lookupEntries.ToArray();
        }

        return snapshot;
    }

    [ContextMenu("Print Captured Snapshot")]
    public void PrintCapturedSnapshot()
    {
        SerializableGameState snapshot = CaptureVisualSnapshot();
        
        
        snapshot.hands.PrintAll();
    }

    /// <summary>
    /// Checks if a card is in a scoring pool to prevent over-rendering visual effects.
    /// </summary>
    private bool IsCardInScoringPool(CardInteraction targetCard)
    {
        if (targetCard == null) return false;

        Transform t = targetCard.transform;
        while (t != null)
        {
            if (playerPoolTransforms != null && playerPoolTransforms.Contains(t))
                return true;
            if (playerPiştiPoolTransforms != null && playerPiştiPoolTransforms.Contains(t))
                return true;

            if (deckController != null)
            {
                if (deckController.playerPoolTransforms != null && deckController.playerPoolTransforms.Contains(t))
                    return true;
                if (deckController.playerPiştiPoolTransforms != null && deckController.playerPiştiPoolTransforms.Contains(t))
                    return true;
            }
            t = t.parent;
        }
        return false;
    }

    /// <summary>
    /// Applies copied card map to restore Kopyala Yapıştır visual changes after reconnection
    /// </summary>
    private void ApplyCopiedCardMap(Dictionary<string, string> copiedCards)
    {
        if (copiedCards == null || copiedCards.Count == 0) return;
        
        
        
        foreach (var kvp in copiedCards)
        {
            string targetCardID = kvp.Key;
            
            // Resolve transitive copy chains (e.g. A copies B, B copies C -> target copies C)
            string ultimateSourceID = targetCardID;
            int depth = 0;
            while (copiedCards.TryGetValue(ultimateSourceID, out string nextSourceID) && depth < 10)
            {
                ultimateSourceID = nextSourceID;
                depth++;
            }
            
            
            
            // Find target and ultimate source cards
            if (CardInteraction.cardLookup.TryGetValue(targetCardID, out var targetCard) &&
                CardInteraction.cardLookup.TryGetValue(ultimateSourceID, out var sourceCard))
            {
                // Retrieve original identity backups, falling back to current attributes if unset
                int[] newCardID = (sourceCard.originalCardID != null && sourceCard.originalCardID.Length >= 2) ? (int[])sourceCard.originalCardID.Clone() : sourceCard.GetCardID();
                Sprite newSprite = (sourceCard.originalSprite != null) ? sourceCard.originalSprite : sourceCard.GetComponent<SpriteRenderer>().sprite;
                
                // Store original card data if target doesn't have it yet, before mutating target values
                targetCard.StoreOriginalCardData();
                
                // Restore visual and logical copy attributes
                targetCard.SetCardIDAndSprite(newCardID, newSprite);
                targetCard.activePowerEffect = sourceCard.activePowerEffect;
                
                // Restore power effect visuals on the copied target card if active, universally to all 52 cards
                if (targetCard.activePowerEffect != "none")
                {
                    CopyEffectVisuals(targetCard, true);
                }
                
                
            }
            else
            {
                
            }
        }
        
        
    }

    /// <summary>
    /// Applies card power effects (Kapkaç, Yandım Anam) to cards after loading game state
    /// </summary>
    private void ApplyCardPowerEffects()
    {
        if (cardPowerEffects == null) return;

        // Iterate on a copied list to prevent InvalidOperationException if nested methods modify cardPowerEffects
        foreach (var kvp in new List<KeyValuePair<string, string>>(cardPowerEffects))
        {
            string cardUniqueID = kvp.Key;
            string powerEffect = kvp.Value;

            if (CardInteraction.cardLookup.TryGetValue(cardUniqueID, out var cardInteraction))
            {
                switch (powerEffect)
                {
                    case "Kapkaç":
                        // Apply both logical and instant visual state universally to all 52 cards
                        OnKapkacCardChanged(cardUniqueID, true);
                        break;

                    case "YandımAnam":
                        // Apply both logical and instant visual state universally to all 52 cards
                        OnYandimAnamCardChanged(cardUniqueID, true);
                        break;

                    default:
                        cardInteraction.activePowerEffect = powerEffect;
                        
                        break;
                }
            }
            else
            {
                
            }
        }
    }

    /// <summary>
    /// Unified pipeline to reconstruct all card identities and power visuals deterministically.
    /// This is the SINGLE SOURCE OF TRUTH for visual reconstruction on reconnected/migrated clients.
    /// </summary>
    public void ReconstructPowerVisuals(SerializableGameState snapshot)
    {
        

        // STEP 1: Pristine Slate
        int resetCount = 0;
        foreach (var kvp in CardInteraction.cardLookup)
        {
            var card = kvp.Value;
            if (card != null)
            {
                card.ResetToOriginalPristineState();
                ResetVisuals(card);
                resetCount++;
            }
        }
        

        // STEP 2: Base Identity Resolution (Kopyala Yapıştır)
        var copiedCards = snapshot.copiedCardMap.ToDictionary();
        if (copiedCards != null && copiedCards.Count > 0)
        {
            
            foreach (var kvp in copiedCards)
            {
                string targetCardID = kvp.Key;
                string mappedSourceID = kvp.Value;
                
                // Resolve transitive copy chains
                string ultimateSourceID = targetCardID;
                int depth = 0;
                while (copiedCards.TryGetValue(ultimateSourceID, out string nextSourceID) && depth < 10)
                {
                    ultimateSourceID = nextSourceID;
                    depth++;
                }

                bool hasTarget = CardInteraction.cardLookup.TryGetValue(targetCardID, out var targetCard);
                bool hasSource = CardInteraction.cardLookup.TryGetValue(ultimateSourceID, out var sourceCard);

                if (hasTarget && hasSource)
                {
                    int[] baseCardID = (sourceCard.originalCardID != null && sourceCard.originalCardID.Length >= 2) ? (int[])sourceCard.originalCardID.Clone() : sourceCard.GetCardID();
                    Sprite baseSprite = (sourceCard.originalSprite != null) ? sourceCard.originalSprite : sourceCard.GetComponent<SpriteRenderer>().sprite;

                    targetCard.StoreOriginalCardData();
                    targetCard.SetCardIDAndSprite(baseCardID, baseSprite);
                    targetCard.activePowerEffect = "none";
                    
                    
                }
            }
        }
        else
        {
            
        }

        // STEP 3: Status Alterations (Kapkaç, YandımAnam)
        var powerEffects = snapshot.cardPowerEffects.ToDictionary();
        if (powerEffects != null && powerEffects.Count > 0)
        {
            
            foreach (var kvp in powerEffects)
            {
                string cardUniqueID = kvp.Key;
                string powerEffect = kvp.Value;

                if (CardInteraction.cardLookup.TryGetValue(cardUniqueID, out var cardInteraction))
                {
                    
                    switch (powerEffect)
                    {
                        case "Kapkaç":
                            OnKapkacCardChanged(cardUniqueID, true);
                            break;

                        case "YandımAnam":
                        case "Yandım Anam":
                            OnYandimAnamCardChanged(cardUniqueID, true);
                            break;

                        default:
                            cardInteraction.activePowerEffect = powerEffect;
                            
                            break;
                    }
                }
                else
                {
                    
                }
            }
        }
        else
        {
            
        }

        
    }

    public Dictionary<string, string> GetCardPowerEffectsSnapshot()
    {
        if (cardPowerEffects == null)
        {
            return new Dictionary<string, string>();
        }

        return new Dictionary<string, string>(cardPowerEffects);
    }

    /// <summary>
    /// Updates UI and layout after state application
    /// </summary>
    private IEnumerator UpdateUIAndLayout()
    {
        
        
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
            
            return;
        }

        string testCardId = myCards[0];
        
        
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
            
            
            
            
        }
        else
        {
            
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
            
            return;
        }

        string testCardId = myCards[0];
        
        
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
            
            
            
        }
        else
        {
            
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
            
            return;
        }

        if (MoveChainTracker.ServerInstance == null)
        {
            
            return;
        }

        var clientChain = MoveChainTracker.ClientInstance.GetCurrentChain();
        var serverChain = MoveChainTracker.ServerInstance.GetCurrentChain();
        
        
        
        
        
        var validationResult = clientChain.ValidateAgainst(serverChain, out int mismatchIndex);
        bool isValid = validationResult == MoveChain.ValidationResult.Valid;
        
        if (isValid)
        {
            
        }
        else
        {
            
            
            
            
        }
    }
    
    /// <summary>
    /// Test function: Manually reset move chains (useful for testing)
    /// </summary>
    [ContextMenu("Reset Move Chains")]
    public void ResetMoveChains()
    {
        MoveChainIntegrator.ResetChains();
        
    }

    /// <summary>
    /// Rebuilds cardLookup for reconnection by mapping newly created cards to server IDs
    /// </summary>
    private void RebuildCardLookupForReconnection()
    {
        
        
        
        // Clear the existing cardLookup (it has wrong IDs)
        CardInteraction.cardLookup.Clear();
        
        // Get all card interactions from DeckController
        if (deckController != null && deckController.cardInteractionList != null)
        {
            
            
            // For each card interaction, we need to map it to the correct server ID
            // The issue is that we don't know which server ID corresponds to which card
            // We need to wait for the game state to be applied to know the correct mapping
            
            // For now, just log that we're ready to rebuild
            
        }
        else
        {
            
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
            
            return;
        }
        
        // Check if MoveChainTracker is properly set up
        var moveChainTracker = GetComponent<MoveChainTracker>();
        if (moveChainTracker == null)
        {
            
        }
        else
        {
            
        }
        
        // Ensure move tracking is enabled
        
    }

    /// <summary>
    /// Triggers a desync check after reconnection to ensure move chains are synchronized
    /// </summary>
    public void TriggerDesyncCheckAfterReconnection()
    {
        
        
        
        // Start the coroutine on this GameManager instance
        StartCoroutine(TriggerDesyncCheckAfterReconnectionCoroutine());
    }
    
    /// <summary>
    /// Coroutine that handles the actual desync check after reconnection
    /// </summary>
    private IEnumerator TriggerDesyncCheckAfterReconnectionCoroutine()
    {
        
        
        
        // Wait for the game state to be fully applied
        yield return new WaitForSeconds(1.0f);
        
        // CRITICAL FIX: Reset isReconnecting flag after reconnection sync is complete
        // This allows normal dealing to work again
        isReconnecting = false;
        
        
        // Check if MoveChainIntegrator is available
        if (MoveChainIntegrator.LocalInstance != null)
        {
            
            
            MoveChainIntegrator.LocalInstance.ForceImmediateDesyncCheck();
            
            // NEW: After desync check, trigger visual restoration
            
            StartCoroutine(TriggerVisualRestorationAfterDesyncCheck());
        }
        else
        {
            
            
        }
    }
    
    /// <summary>
    /// NEW: Triggers visual restoration after desync check completes
    /// This ensures the move chain is properly synchronized before restoring visuals
    /// </summary>
    private IEnumerator TriggerVisualRestorationAfterDesyncCheck()
    {
        
        
        // Wait for desync check to complete and move chain to be synchronized
        yield return new WaitForSeconds(2.0f);
        
        
        
        // Now restore visual states from the properly synchronized move chain
        yield return StartCoroutine(RestoreVisualStatesFromMoveChain());

        // Deactivate MainUI and other lobby screens after reconnection is complete
        OnReconnectionGameStateApplied();
        
        
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
            
            MoveChainIntegrator.LocalInstance.ForceImmediateDesyncCheck();
        }
        else
        {
            
        }
    }
    
    /// <summary>
    /// Restores player number from PlayerPrefs during reconnection
    /// </summary>
    private void RestorePlayerNumberFromPrefs()
    {
        
        
        // If the deck controller already has a valid seat assigned in-memory (e.g., from prior active gameplay),
        // do not overwrite it with shared PlayerPrefs which could be corrupted by multiple local testing instances.
        if (deckController != null && deckController.thisPlayerNumber >= 0)
        {
            int savedNumber = deckController.thisPlayerNumber;
            
            
            // Announce player number to server so it rebinds playerClientIds with the new client ID
            if (networkRelay != null)
            {
                networkRelay.AnnounceReconnectedPlayerNumberServerRPC(savedNumber);
                
            }
            
            return;
        }

        
        
        if (PlayerPrefs.HasKey("SavedPlayerSeat"))
        {
            int savedPlayerNumber = PlayerPrefs.GetInt("SavedPlayerSeat");
            
            
            // Set the player number directly on DeckController
            if (deckController != null)
            {
                
                deckController.SetPlayerNumber(savedPlayerNumber);
                
                

                // Announce player number to server so it rebinds playerClientIds with the new client ID
                if (networkRelay != null)
                {
                    networkRelay.AnnounceReconnectedPlayerNumberServerRPC(savedPlayerNumber);
                    
                }
            }
            else
            {
                
            }
        }
        else
        {
            
            
        }
        
        
    }
    
    private string[] GetAllPlayerPrefsKeys()
    {
        // This is a helper method to debug what's in PlayerPrefs
        // Note: Unity doesn't provide a direct way to get all keys, so we'll check common ones
        var keys = new List<string>();
        if (PlayerPrefs.HasKey("SavedPlayerSeat")) keys.Add("SavedPlayerSeat");
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
        
        
        
        // CRITICAL FIX: Explicitly apply current player and turn counter after redo
        // This ensures the UI reflects the correct turn immediately
        currentPlayerNo = snapshot.currentPlayer;
        turnCounter = snapshot.turnCounter;
        
        
        
        // Show a message to the user about the revert
        ShowcaseSuperPower($"Reverted to {revertType}", 0.5f, 3f);
        
        // CRITICAL FIX: Start coroutine that waits for scene reconstruction to complete
        // This ensures the server waits for all clients before sending current player update
        StartCoroutine(WaitForSceneReconstructionAndConfirm(snapshot, revertType));
        
        
    }

    /// <summary>
    /// Waits for scene reconstruction to complete and then confirms to server
    /// </summary>
    private IEnumerator WaitForSceneReconstructionAndConfirm(SerializableGameState snapshot, string revertType)
    {
        
        
        // Use existing game state application system and wait for it to complete
        if (deckController != null)
        {
            
            yield return StartCoroutine(ApplyGameStateWithReset(snapshot));
            
        }
        else
        {
            
            ApplyGameState(snapshot);
            // Wait a bit as fallback
            yield return new WaitForSeconds(2.0f);
        }
        
        // Additional safety wait to ensure all visual updates are complete
        yield return new WaitForSeconds(1.0f);
        
        
        
        if (networkRelay != null)
        {
            networkRelay.ConfirmRedoSceneReconstructionFinishedServerRPC();
        }
        else
        {
            
        }
        
        
    }

    /// <summary>
    /// Called when redo state status is received from server
    /// </summary>
    public void OnRedoStateStatusReceived(string status)
    {
        
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
            
            networkRelay.RequestRedoToPreviousStateServerRPC();
        }
        else
        {
            
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
            
            networkRelay.RequestRedoToPrePreviousStateServerRPC();
        }
        else
        {
            
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
            
            networkRelay.RequestRedoStateStatusServerRPC();
        }
        else
        {
            
        }
    }

}

