using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main orchestrator for single player mode game loop.
/// Manages stage progression, round flow, opponent behavior, damage evaluation, and joker effects.
/// 
/// PLAYABLE MODE: Includes runtime UI creation and debug capture methods for testing.
/// </summary>
public class SinglePlayerModeController : MonoBehaviour
{
    public static SinglePlayerModeController Instance { get; private set; }

    [Serializable]
    public class SinglePlayerRunConfig
    {
        public int seed;
        public string deckClassName; // "Balanced", "Aggressive", etc.
    }

    [Serializable]
    public class SinglePlayerRoundData
    {
        public int currentRound; // Within stage
        public int totalDamageDealt; // To opponent this round
        public int opponentDamageDealt; // To player this round
        public int activeJokerId; // Which joker chosen for this stage
    }

    // Run-wide state
    private SinglePlayerRunConfig currentRunConfig;
    private SinglePlayerRoundData currentRoundData;
    private int currentStage = 0;
    private int currentOpponentDifficulty = 0; // 0=Easy, 1=Normal, 2=Hard
    private float currentStageDifficultyMultiplier = 1.0f; // 1.3^stageIndex, cached per stage
    private int currentRoundInStage = 0; // Opponent fights completed within current stage

    // Opponent state
    private int opponentHealth = 0; // Damage needed to defeat opponent
    private const int OPPONENT_HEALTH_PER_ROUND = 20; // Opponent defeated when damage >= 20

    // Game loop references
    private OpponentBehaviorManager.OpponentBehaviorConfig currentOpponentBehaviorConfig;
    private DamageSystem.CaptureTelemetry lastCaptureTelemetry;
    private bool isGameRunning = false;
    private int playerTurnIndex = 0; // Tracks turn sequence for opponent behavior

    // UI references
    [SerializeField] private TextMeshProUGUI stageRoundDisplayText;
    [SerializeField] private Image opponentHealthBarImage;
    [SerializeField] private TextMeshProUGUI opponentHealthText;
    [SerializeField] private Transform jokerDisplayParent; // Assign joker overlay parent in Inspector
    
    // UI containers (for runtime creation)
    private List<JokerController.JokerDefinition> currentJokerOptions;
    private Canvas uiCanvas;
    
    // References needed for dealing
    private DeckController deckController;
    private bool roundIsWaiting = true; // Flag to signal round end conditions

    // Deck management - mirroring multiplayer Server.deckCardsDict
    private Dictionary<string, int[]> deckCardsDict; // Full shuffled deck for current stage
    private bool isDeckInitializedForRound = false;

    // Turn management (offline, authoritative within SinglePlayer)
    public static bool IsPlayerTurn { get; private set; } = false;
    public static bool IsGameRunning { get; private set; } = false;
    private bool playerMoveSignal = false; // Set true when player completes a move; unblocks WaitForPlayerInput
    private string pendingPlayerCardId;
    private SerializableCard pendingPlayerCapturedCards;
    private bool pendingPlayerIsPisti;
    private bool pendingPlayerIsJackPisti;
    private bool pendingPlayerAddedToCenter; // True if the player's move was add-to-center (no capture)

    // Local game state (SinglePlayer owns this; GameManager is driven for visuals only)
    private List<string> opponentHandList = new List<string>();
    private Dictionary<string, int[]> localCenterCards = new Dictionary<string, int[]>();
    private int cardsRemainingInDeck = 0;
    private int handsDealtCount = 0; // Track number of hands dealt (max 6)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Get references
        deckController = FindFirstObjectByType<DeckController>();
        
        // Create placeholder UI at runtime if it doesn't exist
        EnsureUIExists();
    }

    /// <summary>
    /// Create placeholder UI at runtime if UI elements aren't assigned.
    /// This ensures the game is playable even without pre-built UI.
    /// </summary>
    private void EnsureUIExists()
    {
        // Find or create Canvas
        uiCanvas = FindFirstObjectByType<Canvas>();
        if (uiCanvas == null)
        {
            GameObject canvasGO = new GameObject("SinglePlayerCanvas");
            uiCanvas = canvasGO.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<GraphicRaycaster>();
            
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        }

        // Create stage display if not assigned
        if (stageRoundDisplayText == null)
        {
            GameObject stageDisplayGO = new GameObject("StageDisplay");
            stageDisplayGO.transform.SetParent(uiCanvas.transform, false);
            RectTransform stageRect = stageDisplayGO.AddComponent<RectTransform>();
            stageRect.anchoredPosition = new Vector2(0, 300);
            stageRect.sizeDelta = new Vector2(600, 100);
            
            stageRoundDisplayText = stageDisplayGO.AddComponent<TextMeshProUGUI>();
            stageRoundDisplayText.text = "Stage 1 • Opponent 1/3 (Easy) • Round 0";
            stageRoundDisplayText.alignment = TextAlignmentOptions.Center;
            stageRoundDisplayText.fontSize = 36;
            
            Image bgImage = stageDisplayGO.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        }

        // Create opponent health bar if not assigned
        if (opponentHealthBarImage == null)
        {
            GameObject healthBarGO = new GameObject("OpponentHealthBar");
            healthBarGO.transform.SetParent(uiCanvas.transform, false);
            RectTransform healthRect = healthBarGO.AddComponent<RectTransform>();
            healthRect.anchoredPosition = new Vector2(0, 200);
            healthRect.sizeDelta = new Vector2(300, 40);
            
            Image bgImage = healthBarGO.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            // Health fill
            GameObject fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(healthBarGO.transform, false);
            RectTransform fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = Vector2.zero;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.zero;
            
            opponentHealthBarImage = fillGO.AddComponent<Image>();
            opponentHealthBarImage.color = new Color(1, 0, 0, 0.8f); // Red
        }

        // Create health text if not assigned
        if (opponentHealthText == null)
        {
            GameObject healthTextGO = new GameObject("HealthText");
            healthTextGO.transform.SetParent(uiCanvas.transform, false);
            RectTransform healthTextRect = healthTextGO.AddComponent<RectTransform>();
            healthTextRect.anchoredPosition = new Vector2(0, 200);
            healthTextRect.sizeDelta = new Vector2(300, 40);
            
            opponentHealthText = healthTextGO.AddComponent<TextMeshProUGUI>();
            opponentHealthText.text = "0/20";
            opponentHealthText.alignment = TextAlignmentOptions.Center;
            opponentHealthText.fontSize = 28;
        }
    }

    /// <summary>
    /// Entry point: Start a new single player run with the given configuration.
    /// </summary>
    public void StartNewRun(int seed, string deckClassName)
    {
        Debug.Log($"\n=== [SinglePlayerModeController] STARTING NEW RUN ===\nSeed: {seed} | DeckClass: {deckClassName}\n");

        currentRunConfig = new SinglePlayerRunConfig
        {
            seed = seed,
            deckClassName = deckClassName
        };

        ResetForSingleplayerRun();

        // Disable UI screens to show game (same as multiplayer)
        DisableGameScreensForSinglePlayer();

        // Initialize game scene
        InitializeScene();

        // Register singleplayer participants in multiplayer systems
        RegisterSinglePlayerParticipants();

        // Start the first stage
        StartStage(0);
    }

    /// <summary>
    /// Disable UI screens when game starts (same as multiplayer).
    /// Hide MainUI, WinScreen, and other blocking screens so game is visible.
    /// </summary>
    private void DisableGameScreensForSinglePlayer()
    {
        Debug.Log("[SinglePlayerModeController] Disabling UI screens for game start");

        // 1. Disable waiting screen
        GameObject waitingScreen = GameObject.Find("WaitingScreen");
        if (waitingScreen != null && waitingScreen.activeSelf)
        {
            waitingScreen.SetActive(false);
            Debug.Log("[SinglePlayerModeController] Deactivated WaitingScreen");
        }

        // 2. Disable main/lobby screen
        GameObject mainScreen = GameObject.Find("MainScreen");
        if (mainScreen != null && mainScreen.activeSelf)
        {
            mainScreen.SetActive(false);
            Debug.Log("[SinglePlayerModeController] Deactivated MainScreen");
        }

        // 3. Disable win screen
        GameObject winScreen = GameObject.Find("WinScreen");
        if (winScreen != null && winScreen.activeSelf)
        {
            winScreen.SetActive(false);
            Debug.Log("[SinglePlayerModeController] Deactivated WinScreen");
        }

        // 4. Disable MainUI
        GameObject mainUI = GameObject.Find("MainUI");
        if (mainUI != null && mainUI.activeSelf)
        {
            mainUI.SetActive(false);
            Debug.Log("[SinglePlayerModeController] Deactivated MainUI");
        }
    }

    /// <summary>
    /// Initialize the game scene for single player (2-player setup).
    /// </summary>
    private void InitializeScene()
    {
        Debug.Log("[SinglePlayerModeController] Initializing scene for single player mode");

        // Seed the random number generator for reproducibility
        UnityEngine.Random.InitState(currentRunConfig.seed);
    }

    /// <summary>
    /// Build a fresh deck with all 52 cards (mirroring Server.InitializeOrRestoreDeck).
    /// Creates deckCardsDict with proper unique IDs and [suit, value] pairs.
    /// Called once per stage (not per round - deck is reused across rounds in multiplayer).
    /// </summary>
    private void BuildFreshDeck()
    {
        Debug.Log("[SinglePlayerModeController] === BUILDING FRESH DECK ===");
        
        deckCardsDict = new Dictionary<string, int[]>();
        int cardIndex = 0;

        // Club cards (kind=1, value=1 to 13)
        for (int value = 1; value <= 13; value++)
        {
            string uniqueID = "card_" + cardIndex;
            deckCardsDict.Add(uniqueID, new int[] { 1, value });
            cardIndex++;
        }

        // Diamond cards (kind=2, value=1 to 13)
        for (int value = 1; value <= 13; value++)
        {
            string uniqueID = "card_" + cardIndex;
            deckCardsDict.Add(uniqueID, new int[] { 2, value });
            cardIndex++;
        }

        // Heart cards (kind=3, value=1 to 13)
        for (int value = 1; value <= 13; value++)
        {
            string uniqueID = "card_" + cardIndex;
            deckCardsDict.Add(uniqueID, new int[] { 3, value });
            cardIndex++;
        }

        // Spade cards (kind=4, value=1 to 13)
        for (int value = 1; value <= 13; value++)
        {
            string uniqueID = "card_" + cardIndex;
            deckCardsDict.Add(uniqueID, new int[] { 4, value });
            cardIndex++;
        }

        Debug.Log($"[SinglePlayerModeController] Fresh deck built with {deckCardsDict.Count} cards");
    }

    /// <summary>
    /// Shuffle the deck using Fisher-Yates algorithm with the run's seed.
    /// Mirroring Server.SuffleCards().
    /// </summary>
    private void ShuffleDeck()
    {
        Debug.Log($"[SinglePlayerModeController] === SHUFFLING DECK with seed {currentRunConfig.seed} ===");

        System.Random rng = new System.Random(currentRunConfig.seed);
        var deckList = new List<KeyValuePair<string, int[]>>(deckCardsDict);
        int count = deckList.Count;

        // Fisher-Yates shuffle
        for (int i = 0; i < count - 1; i++)
        {
            int randomIndex = rng.Next(i, count);
            var temp = deckList[i];
            deckList[i] = deckList[randomIndex];
            deckList[randomIndex] = temp;
        }

        // Rebuild dictionary in shuffled order
        deckCardsDict = new Dictionary<string, int[]>();
        foreach (var kvp in deckList)
        {
            deckCardsDict[kvp.Key] = kvp.Value;
        }

        Debug.Log($"[SinglePlayerModeController] Deck shuffled. First 4 cards (center): {string.Join(", ", deckList.Take(4).Select(kvp => kvp.Key))}");
    }

    /// <summary>
    /// Deal cards from the shuffled deck to center and player hands.
    /// Mirroring Server.DealCardsToCenter() and DealCardsToPlayerHands().
    /// Returns centerCardIDs and playerHands Dictionary ready for DealCenter/DealPlayers.
    /// </summary>
    private (List<string>, Dictionary<int, List<string>>) DealFromDeck(bool dealToCenter = true)
    {
        Debug.Log($"[SinglePlayerModeController] === DEALING FROM DECK (Center: {dealToCenter}) ===");
        
        // Deal center (first 4 cards) if requested
        List<string> centerCardIDs = new List<string>();
        var deckEnum = deckCardsDict.GetEnumerator();
        
        if (dealToCenter)
        {
            for (int i = 0; i < 4; i++)
            {
                if (deckEnum.MoveNext())
                {
                    centerCardIDs.Add(deckEnum.Current.Key);
                }
            }
        }

        // Deal player hands (next 4 cards per player for 2-player)
        Dictionary<int, List<string>> playerHands = new Dictionary<int, List<string>>();
        playerHands[0] = new List<string>(); // Player 0
        playerHands[1] = new List<string>(); // Player 1 (opponent)

        for (int i = 0; i < 4; i++) // 4 cards per player
        {
            for (int playerNum = 0; playerNum < 2; playerNum++) // For each player
            {
                if (deckEnum.MoveNext())
                {
                    playerHands[playerNum].Add(deckEnum.Current.Key);
                }
            }
        }

        // Remove dealt cards from deck (for potential future rounds)
        foreach (string cardID in centerCardIDs)
        {
            deckCardsDict.Remove(cardID);
        }
        foreach (var playerCards in playerHands.Values)
        {
            foreach (string cardID in playerCards)
            {
                deckCardsDict.Remove(cardID);
            }
        }

        Debug.Log($"[SinglePlayerModeController] Dealt to center: {string.Join(", ", centerCardIDs)}");
        Debug.Log($"[SinglePlayerModeController] Dealt to player 0: {string.Join(", ", playerHands[0])}");
        Debug.Log($"[SinglePlayerModeController] Dealt to player 1: {string.Join(", ", playerHands[1])}");
        Debug.Log($"[SinglePlayerModeController] Remaining deck: {deckCardsDict.Count} cards");

        return (centerCardIDs, playerHands);
    }

    /// <summary>
    /// Register singleplayer participants in multiplayer-dependent systems.
    /// This mimics the multiplayer player registration flow:
    /// - Sets DeckController player number (always 0 = human player)
    /// - Sets player count (always 2 = human vs opponent)
    /// - Registers player with SuperPowerSpawner and ElHolderScript
    /// Called once at the start of the run before any stages begin.
    /// </summary>
    private void RegisterSinglePlayerParticipants()
    {
        Debug.Log("[SinglePlayerModeController] === REGISTERING SINGLEPLAYER PARTICIPANTS ===");

        const int PLAYER_NUMBER = 0; // Human player is always player 0 (bottom/local)
        const int PLAYER_COUNT = 2;  // Singleplayer is a 2-player game (human + opponent AI)

        // 1. Register player number with DeckController
        if (deckController != null)
        {
            deckController.SetPlayerNumber(PLAYER_NUMBER);
            Debug.Log($"[SinglePlayerModeController] Registered with DeckController - playerNumber: {PLAYER_NUMBER}");
        }
        else
        {
            Debug.LogError("[SinglePlayerModeController] DeckController not found! Cannot register player.");
        }

        // 2. Register player count with DeckController and dependent systems
        // This calls ElHolderScript.SetHandMode(playerCount) internally
        if (deckController != null)
        {
            deckController.GetPlayerCount(PLAYER_COUNT, isReconnection: false);
            Debug.Log($"[SinglePlayerModeController] Registered with DeckController - playerCount: {PLAYER_COUNT}");
        }

        // 3. Register player number with SuperPowerSpawner
        if (SuperPowerSpawner.LocalInstance != null)
        {
            SuperPowerSpawner.LocalInstance.SetPlayerNumber(PLAYER_NUMBER);
            Debug.Log($"[SinglePlayerModeController] Registered with SuperPowerSpawner - playerNumber: {PLAYER_NUMBER}");
        }
        else
        {
            Debug.LogWarning("[SinglePlayerModeController] SuperPowerSpawner.LocalInstance not found");
        }

        // 4. Register hand mode with ElHolderScript (for token positions)
        if (ElHolderScript.LocalInstance != null)
        {
            ElHolderScript.LocalInstance.SetHandMode(PLAYER_COUNT);
            Debug.Log($"[SinglePlayerModeController] Registered with ElHolderScript - playerCount: {PLAYER_COUNT}");
        }
        else
        {
            Debug.LogWarning("[SinglePlayerModeController] ElHolderScript.LocalInstance not found");
        }

        Debug.Log("[SinglePlayerModeController] === PLAYER REGISTRATION COMPLETE ===\n");
    }

    // ===== LOOP RESET FUNCTIONS =====

    /// <summary>
    /// RunLoop reset: Full teardown for a fresh run.
    /// Destroys all card GameObjects, resets all power flags, and clears run-wide state.
    /// Called once at the start of StartNewRun() before anything else begins.
    /// </summary>
    private void ResetForSingleplayerRun()
    {
        Debug.Log("[SinglePlayerModeController] === RESET FOR SINGLEPLAYER RUN ===");

        // Full card teardown via GameManager (destroys GameObjects, clears power dicts, all flags)
        if (GameManager.LocalInstance != null)
            GameManager.LocalInstance.ResetForNewGame();

        // Reset singleplayer progression
        currentStage = 0;
        currentRoundInStage = 0;
        currentStageDifficultyMultiplier = 1.0f;
        currentRoundData = new SinglePlayerRoundData();

        // Reset game loop state
        isGameRunning = false;
        playerTurnIndex = 0;
        isDeckInitializedForRound = false;
        deckCardsDict = null;
        roundIsWaiting = true;

        // Reset turn management
        IsPlayerTurn = false;
        playerMoveSignal = false;

        // Reset local state mirrors
        opponentHealth = 0;
        opponentHandList.Clear();
        localCenterCards.Clear();
        cardsRemainingInDeck = 0;

        Debug.Log("[SinglePlayerModeController] Run reset complete");
    }

    /// <summary>
    /// StageLoop reset: Minimal setup for a new stage.
    /// Calculates difficulty multiplier and resets round counter.
    /// Cards and their power modifications are NOT touched — they persist across stages.
    /// Called at the start of StartStage() before joker selection.
    /// </summary>
    private void ResetForStage(int stageIndex)
    {
        Debug.Log($"[SinglePlayerModeController] === RESET FOR STAGE {stageIndex} ===");

        // Calculate and cache difficulty multiplier (1.3^stageIndex)
        currentStageDifficultyMultiplier = Mathf.Pow(1.3f, stageIndex);

        // Reset round counter within this stage (ResetForRound will increment it)
        currentRoundInStage = 0;
        currentOpponentDifficulty = 0;

        // Reset deck flag so a fresh deck is built for this stage's first round
        isDeckInitializedForRound = false;

        Debug.Log($"[SinglePlayerModeController] Stage {stageIndex} reset: difficulty multiplier = {currentStageDifficultyMultiplier:F2}");

        UpdateStageDisplay();
    }

    /// <summary>
    /// RoundLoop reset: Initializes a fresh 1v1 opponent fight.
    /// Resets opponent health, scores, deck tracking, and turn management.
    /// Card GameObjects and power modifications persist — only tracking lists are cleared.
    /// Called at the start of StartRound() before dealing.
    /// </summary>
    private void ResetForRound(int opponentDifficulty)
    {
        Debug.Log($"[SinglePlayerModeController] === RESET FOR ROUND (Opponent {opponentDifficulty + 1}/3, Stage {currentStage}) ===");

        // Increment round counter within this stage
        currentRoundInStage++;
        currentOpponentDifficulty = opponentDifficulty;

        // Reset opponent health and scores for a fresh fight
        opponentHealth = 0;
        if (currentRoundData == null) currentRoundData = new SinglePlayerRoundData();
        currentRoundData.currentRound++;
        currentRoundData.totalDamageDealt = 0;
        currentRoundData.opponentDamageDealt = 0;

        // Reset turn state — player always starts each round
        GameManager.currentPlayerNo = 0;
        playerTurnIndex = 0;
        IsPlayerTurn = false;
        playerMoveSignal = false;
        roundIsWaiting = true;
        handsDealtCount = 0;

        // Reset local state mirrors
        opponentHandList.Clear();
        localCenterCards.Clear();

        // Reset GameManager card tracking lists (DeckController rebuilds visuals when dealing)
        if (GameManager.LocalInstance != null)
        {
            GameManager.LocalInstance.centerCards.Clear();
            GameManager.LocalInstance.centerCardsObjects.Clear();
            GameManager.LocalInstance.cardObjectsToBeDiscarted.Clear();
            if (GameManager.LocalInstance.myCards != null) GameManager.LocalInstance.myCards.Clear();
            GameManager.LocalInstance.movePlayedLocally = false;
            GameManager.LocalInstance.isProcessingCapture = false;
            GameManager.LocalInstance.SetCurrentSelectedHandCardNull(); // also resets hasAlreadySentRPC
            GameManager.LocalInstance.EndGameplayAction("Round reset");
        }

        // Reset card selection state
        CardInteraction.currentlySelectedCard = null;
        CardInteraction.isOneCardSelected = false;

        Debug.Log($"[SinglePlayerModeController] Round reset complete: opponent {opponentDifficulty + 1}/3, multiplier {currentStageDifficultyMultiplier:F2}");

        UpdateOpponentHealthDisplay();
        UpdateStageDisplay();
    }

    // ===== END LOOP RESET FUNCTIONS =====

    /// <summary>
    /// Start a new stage. Display stage UI and show joker selection panel.
    /// A stage contains 3 opponents (Easy, Normal, Hard).
    /// </summary>
    private void StartStage(int stageNumber)
    {
        Debug.Log($"\n>>> STARTING STAGE {stageNumber} <<<");

        currentStage = stageNumber;
        ResetForStage(stageNumber);

        // Show joker selection panel for this stage
        ShowJokerSelectionPanel();
    }

    /// <summary>
    /// Display 3 random joker options under the assigned jokerDisplayParent.
    /// </summary>
    private void ShowJokerSelectionPanel()
    {
        Debug.Log("[SinglePlayerModeController] === JOKER SELECTION PHASE ===");

        if (jokerDisplayParent == null)
        {
            Debug.LogError("[SinglePlayerModeController] jokerDisplayParent not assigned! Cannot display jokers.");
            return;
        }

        // Generate 3 random joker options
        currentJokerOptions = JokerController.GenerateRandomJokerOptions(3, UnityEngine.Random.state);

        // Clear any existing jokers
        foreach (Transform child in jokerDisplayParent)
        {
            Destroy(child.gameObject);
        }

        // Create 3 joker objects under jokerDisplayParent
        for (int i = 0; i < currentJokerOptions.Count; i++)
        {
            JokerController.JokerDefinition joker = currentJokerOptions[i];
            
            GameObject jokerGO = new GameObject($"Joker_{i}_{joker.jokerName}");
            jokerGO.transform.SetParent(jokerDisplayParent, false);
            
            // Add image component and set sprite
            Image image = jokerGO.AddComponent<Image>();
            if (joker.jokerImage != null)
            {
                image.sprite = joker.jokerImage;
            }
            
            // Add button component for selection
            Button button = jokerGO.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => OnJokerButtonClicked(joker.jokerID));
            
            Debug.Log($"  Joker {i}: {joker.jokerName} (ID: {joker.jokerID})");
        }

        Debug.Log("Select a joker, or use: SinglePlayerModeController.Instance.DebugSelectJoker(jokerID)");
    }

    /// <summary>
    /// Called when player clicks a joker button.
    /// </summary>
    private void OnJokerButtonClicked(int jokerID)
    {
        OnJokerSelected(jokerID);
    }

    /// <summary>
    /// Callback when player selects a joker.
    /// Saves the joker selection and starts the stage's round loop.
    /// </summary>
    private void OnJokerSelected(int jokerID)
    {
        Debug.Log($"\n*** JOKER SELECTED: ID={jokerID} ***\n");

        if (currentRoundData == null)
        {
            currentRoundData = new SinglePlayerRoundData();
        }

        currentRoundData.activeJokerId = jokerID;
        JokerController.SetActiveJoker(jokerID);
        RunManager.SaveRunProgress(BuildRunProgressData());

        // Clear jokers from display
        if (jokerDisplayParent != null)
        {
            foreach (Transform child in jokerDisplayParent)
            {
                Destroy(child.gameObject);
            }
        }

        // STAGE LOOP: Start with first opponent (difficulty 0)
        StartCoroutine(StartRoundLoop(0));
    }

    /// <summary>
    /// STAGE LOOP: Manages the 3 opponents in this stage.
    /// Loops through difficulties 0 (Easy) → 1 (Normal) → 2 (Hard).
    /// </summary>
    private IEnumerator StartRoundLoop(int startingDifficulty)
    {
        Debug.Log($"[SinglePlayerModeController] === STAGE LOOP START ===");

        for (int difficulty = startingDifficulty; difficulty < 3; difficulty++)
        {
            currentOpponentDifficulty = difficulty;
            
            // ROUND LOOP: Run a single opponent battle
            yield return StartCoroutine(StartRound(difficulty));
            
            // After round ends, check if we won (OnOpponentDefeated already increments difficulty)
            // If all 3 opponents defeated, we exit this loop and stage clears
        }

        Debug.Log($"[SinglePlayerModeController] === STAGE LOOP COMPLETE - ALL OPPONENTS DEFEATED ===");
        OnStageCleared();
    }

    /// <summary>
    /// ROUND LOOP: Manages a single round with one opponent.
    /// Handles deck setup, initial deals, then drives the sequential turn loop.
    /// </summary>
    private IEnumerator StartRound(int opponentDifficulty)
    {
        Debug.Log($"\n>> ROUND START - Opponent #{opponentDifficulty + 1} ({GetDifficultyName(opponentDifficulty)})\n");

        ResetForRound(opponentDifficulty);

        currentOpponentBehaviorConfig = OpponentBehaviorManager.GetConfigForDifficulty(
            opponentDifficulty,
            currentRunConfig.seed
        );

        UpdateStageDisplay();

        if (deckController != null)
        {
            if (!isDeckInitializedForRound)
            {
                BuildFreshDeck();
                ShuffleDeck();
                isDeckInitializedForRound = true;
            }

            yield return StartCoroutine(deckController.DeckStart());
            Debug.Log("[SinglePlayerModeController] Deck ready");

            var (centerCardIDs, playerHands) = DealFromDeck(true);
            handsDealtCount = 1;

            // Store opponent hand locally before DeckController animates it
            foreach (string id in playerHands[1])
                opponentHandList.Add(id);

            // Build local center mirror from dealt center IDs
            foreach (string id in centerCardIDs)
                if (deckCardsDict.TryGetValue(id, out int[] val))
                    localCenterCards[id] = val;

            // cardsRemainingInDeck is whatever is left after dealing
            cardsRemainingInDeck = deckCardsDict.Count;

            yield return StartCoroutine(deckController.DealCenter(centerCardIDs));
            Debug.Log("[SinglePlayerModeController] Center cards dealt");

            deckController.DealPlayers(2, playerHands);
            Debug.Log("[SinglePlayerModeController] Player hands dealt");
        }

        isGameRunning = true;
        Debug.Log($"[SinglePlayerModeController] Round ready! Opponent has {OPPONENT_HEALTH_PER_ROUND} HP. Deck remaining: {cardsRemainingInDeck}");

        // Drive sequential Player → Opponent → Player turns until round ends
        yield return StartCoroutine(GameTurnLoop());

        Debug.Log("[SinglePlayerModeController] Round ended. Returning to stage loop.");
    }

    /// <summary>
    /// Sequential turn engine: Player turn → Opponent turn → repeat.
    /// Runs until roundIsWaiting becomes false (win/loss signal).
    /// </summary>
    private IEnumerator GameTurnLoop()
    {
        while (roundIsWaiting && isGameRunning)
        {
            // --- PLAYER TURN ---
            IsPlayerTurn = true;
            if (GameManager.LocalInstance != null)
                GameManager.LocalInstance.UpdateCurrentPlayer(0, playerTurnIndex);
            Debug.Log($"[SinglePlayerModeController] Player turn {playerTurnIndex}");

            yield return StartCoroutine(WaitForPlayerInput());

            if (!roundIsWaiting || !isGameRunning) break;

            // --- OPPONENT TURN ---
            IsPlayerTurn = false;
            if (GameManager.LocalInstance != null)
                GameManager.LocalInstance.UpdateCurrentPlayer(1, playerTurnIndex);
            Debug.Log($"[SinglePlayerModeController] Opponent turn {playerTurnIndex}");

            yield return StartCoroutine(ExecuteOpponentTurn());

            playerTurnIndex++;

            if (!roundIsWaiting || !isGameRunning) break;

            // Re-deal if both hands are empty
            bool playerHandEmpty = GameManager.LocalInstance != null && (GameManager.LocalInstance.myCards == null || GameManager.LocalInstance.myCards.Count == 0);
            bool opponentHandEmpty = opponentHandList.Count == 0;

            if (playerHandEmpty && opponentHandEmpty)
            {
                if (handsDealtCount < 6)
                {
                    Debug.Log($"[SinglePlayerModeController] Both hands empty - dealing hand {handsDealtCount + 1}");
                    var (_, newHands) = DealFromDeck(false);
                    handsDealtCount++;
                    cardsRemainingInDeck = deckCardsDict.Count;

                    foreach (string id in newHands[1])
                        opponentHandList.Add(id);

                    // DealPlayers internally updates GameManager.LocalInstance.myCards for player 0
                    deckController.DealPlayers(2, newHands);
                    
                    // Brief wait for dealing animation
                    yield return new WaitForSeconds(1.0f);
                }
                else
                {
                    // 6+1'th deal check: all 6 hands dealt and played, opponent still alive
                    Debug.Log("[SinglePlayerModeController] All 6 hands played - player did not defeat opponent in time");
                    OnPlayerLost();
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Blocks until the player completes their move (card played or added to center).
    /// GameManager calls ProcessPlayerMove/ProcessPlayerAddToCenter to unblock.
    /// </summary>
    private IEnumerator WaitForPlayerInput()
    {
        playerMoveSignal = false;
        while (!playerMoveSignal && roundIsWaiting && isGameRunning)
            yield return null;
    }

    /// <summary>
    /// Opponent AI turn: select card, check capture, update center state, animate card.
    /// </summary>
    private IEnumerator ExecuteOpponentTurn()
    {
        if (opponentHandList.Count == 0)
        {
            Debug.LogWarning("[SinglePlayerModeController] Opponent has no cards - skipping turn");
            yield break;
        }

        // Brief delay so player can see what the opponent plays
        yield return new WaitForSeconds(1.0f);

        int cardIndex = OpponentBehaviorManager.SelectCardToPlay(opponentHandList, currentOpponentBehaviorConfig, playerTurnIndex);
        if (cardIndex < 0 || cardIndex >= opponentHandList.Count)
        {
            Debug.LogWarning("[SinglePlayerModeController] OpponentBehaviorManager returned invalid index");
            yield break;
        }

        string playedCardId = opponentHandList[cardIndex];
        opponentHandList.RemoveAt(cardIndex);

        int[] playedCardKindValue = null;
        // Find played card value from deckCardsDict backup or CardInteraction lookup
        if (CardInteraction.cardLookup.TryGetValue(playedCardId, out CardInteraction ci))
            playedCardKindValue = ci.GetCardID();

        int playedValue = playedCardKindValue != null ? playedCardKindValue[1] : 0;
        bool isJack = playedValue == 11;

        Debug.Log($"[SinglePlayerModeController] Opponent plays card {playedCardId} (value {playedValue})");

        // Check if opponent captures any center cards
        bool capturedSomething = false;
        Dictionary<string, int[]> capturedCenterDict = new Dictionary<string, int[]>();

        // Capture conditions:
        // 1. Jack captures all center cards
        // 2. Non-Jack captures only if top card (most recent) matches played card value
        var topCard = localCenterCards.Values.LastOrDefault();
        if (localCenterCards.Count > 0 && (isJack || (topCard != null && topCard[1] == playedValue)))
        {
            capturedSomething = true;
            Debug.Log($"[SinglePlayerModeController] Opponent captures center cards");
            
            // Copy captured cards before clearing
            foreach (var kvp in localCenterCards)
            {
                capturedCenterDict[kvp.Key] = kvp.Value;
            }
            
            localCenterCards.Clear(); // Opponent sweeps matching/all cards
        }
        else
        {
            // No capture — opponent adds card to center
            if (playedCardKindValue != null)
                localCenterCards[playedCardId] = playedCardKindValue;
            Debug.Log($"[SinglePlayerModeController] Opponent adds card to center (no capture)");
        }

        // Animate opponent move and update GameManager state via DeckController
        if (deckController != null)
        {
            if (capturedSomething)
            {
                SerializableCard capturedCards = new SerializableCard(capturedCenterDict);
                Debug.Log($"[SinglePlayerModeController] Animating opponent capture with {capturedCenterDict.Count} cards");
                yield return StartCoroutine(deckController.DiscardCapturedCards(playedCardId, capturedCards, 1));
            }
            else
            {
                Debug.Log("[SinglePlayerModeController] Animating opponent add-to-center");
                yield return StartCoroutine(deckController.PlayHandCardToCenter(playedCardId, playedCardKindValue, isDiscarded: false));
            }
        }

        yield return new WaitForSeconds(0.5f); // Pause after animation for readability
    }

    /// <summary>
    /// DEBUG METHOD: Simulate a capture for testing without full game loop.
    /// Call this from console to test damage calculations.
    /// </summary>
    public void DebugSimulateCapture(List<int> capturedCardValues, bool isPişti = false, bool isJackPişti = false)
    {
        if (!isGameRunning)
        {
            Debug.LogError("[DEBUG] Game not running! Start a run first.");
            return;
        }

        Debug.Log($"\n[DEBUG CAPTURE] Captured: {string.Join(", ", capturedCardValues)} | Pişti: {isPişti} | JackPişti: {isJackPişti}");

        lastCaptureTelemetry = new DamageSystem.CaptureTelemetry
        {
            capturedCardValues = capturedCardValues,
            capturedCardCount = capturedCardValues.Count,
            isPişti = isPişti,
            isJackPişti = isJackPişti,
            playerNumber = 0 // Player is always seat 0
        };

        // Evaluate damage
        var jokerModifiers = JokerController.GetActiveJokerModifiers(currentRoundData.activeJokerId);
        int damageDealt = DamageSystem.EvaluateTotalDamage(lastCaptureTelemetry, jokerModifiers);

        currentRoundData.totalDamageDealt += damageDealt;
        opponentHealth += damageDealt;

        Debug.Log($"[DEBUG] Damage dealt: {damageDealt} | Total this round: {currentRoundData.totalDamageDealt} | Opponent HP: {opponentHealth}/{OPPONENT_HEALTH_PER_ROUND}\n");

        UpdateOpponentHealthDisplay();

        // Check if opponent defeated
        if (opponentHealth >= OPPONENT_HEALTH_PER_ROUND)
        {
            OnOpponentDefeated();
        }
        else
        {
            Debug.Log($"[DEBUG] Opponent still has {OPPONENT_HEALTH_PER_ROUND - opponentHealth} HP remaining.\n");
        }
    }

    /// <summary>
    /// DEBUG METHOD: Simulate opponent turn (for testing).
    /// </summary>
    public void DebugSimulateOpponentTurn()
    {
        Debug.Log("\n[DEBUG] Simulating opponent turn...");
        playerTurnIndex++;
        Debug.Log($"[DEBUG] Opponent turn {playerTurnIndex} executed. Waiting for next player capture.\n");
    }

    /// <summary>
    /// DEBUG METHOD: Manually select a joker (useful for console testing).
    /// </summary>
    public void DebugSelectJoker(int jokerID)
    {
        Debug.Log($"[DEBUG] Selecting joker ID {jokerID}");
        OnJokerSelected(jokerID);
    }

    /// <summary>
    /// DEBUG METHOD: Manually defeat current opponent.
    /// </summary>
    public void DebugDefeatCurrentOpponent()
    {
        Debug.Log($"[DEBUG] Forcing opponent defeat!");
        opponentHealth = OPPONENT_HEALTH_PER_ROUND;
        OnOpponentDefeated();
    }

    /// <summary>
    /// SERVER-SIDE VALIDATION: Acts like the multiplayer server.
    /// Validates the player's move and determines if it's a capture or add-to-center.
    /// Called by GameManager.CheckIfLegal when in singleplayer mode.
    /// 
    /// Validation rules:
    /// 1. If oynayamazsinActive is true → always add-to-center (power restricts captures)
    /// 2. If cardValue == sumValue OR (cardValue == 11 AND sumValue != 0) → capture
    /// 3. Otherwise → add-to-center (no valid capture)
    /// 
    /// Then routes to either ProcessPlayerMove (capture) or ProcessPlayerAddToCenter (no capture).
    /// </summary>
    public void ValidateAndProcessPlayerMove(string cardId, Dictionary<string, int[]> selectedCenterCards, int sumValue, int cardValue)
    {
        if (!isGameRunning || !IsPlayerTurn)
        {
            Debug.LogWarning("[SingleplayerCardPlay] ValidateAndProcessPlayerMove: Not player turn or game not running");
            return;
        }

        // --- NEW: KIND MATCH SUPERPOWER SPAWN ---
        // If the played card's suit matches the top center card's suit, spawn a superpower.
        if (localCenterCards != null && localCenterCards.Count > 0)
        {
            int playedKind = CardInteraction.cardLookup.TryGetValue(cardId, out var playedCi) ? playedCi.GetCardID()[0] : -1;
            int[] topCardData = localCenterCards.Values.LastOrDefault();
            int topCardKind = topCardData != null ? topCardData[0] : -2;

            if (playedKind != -1 && playedKind == topCardKind)
            {
                Debug.Log($"[SinglePlayerModeController] Kind match! Played suit {playedKind} matches top center card. Spawning superpower token.");
                
                Vector3 spawnPos = Vector3.zero;
                if (GameManager.LocalInstance != null && GameManager.LocalInstance.centerCardsObjects != null && GameManager.LocalInstance.centerCardsObjects.Count > 0)
                {
                    GameObject topCardObj = GameManager.LocalInstance.centerCardsObjects.LastOrDefault();
                    if (topCardObj != null)
                    {
                        spawnPos = topCardObj.transform.position;
                    }
                }

                if (SuperPowerSpawner.LocalInstance != null)
                {
                    SuperPowerSpawner.LocalInstance.ReadyToSpawnSuperPowers(1, spawnPos, 1.5f, 1, true);
                }
            }
        }
        // ---------------------------------------

        GameManager.AddToDebugLog($"[SingleplayerCardPlay] ValidateAndProcessPlayerMove entered: cardId={cardId}, cardValue={cardValue}, sumValue={sumValue}");
        Debug.Log($"[SinglePlayerModeController] ValidateAndProcessPlayerMove: cardId={cardId}, cardValue={cardValue}, sumValue={sumValue}, oynayamazsinActive={GameManager.LocalInstance.oynayamazsinActive}");

        // SERVER VALIDATION: Determine if this is a legal capture or add-to-center
        bool isValidCapture = false;

        if (GameManager.LocalInstance.oynayamazsinActive)
        {
            // Power active: cannot capture, must add to center
            GameManager.AddToDebugLog("[SingleplayerCardPlay] Validation: oynayamazsinActive=true → forcing ADD-TO-CENTER");
            Debug.Log("[SinglePlayerModeController] oynayamazsinActive is true → enforcing add-to-center");
            isValidCapture = false;
        }
        else if (cardValue == sumValue || (cardValue == 11 && sumValue != 0))
        {
            // Capture rules: card matches sum, or jack (11) with non-empty center
            GameManager.AddToDebugLog($"[SingleplayerCardPlay] Validation: cardValue={cardValue} matches sumValue={sumValue} → CAPTURE VALID");
            Debug.Log($"[SinglePlayerModeController] Capture validation: cardValue ({cardValue}) matches sumValue ({sumValue}) or Jack rule → VALID CAPTURE");
            isValidCapture = true;
        }
        else
        {
            // No match → add to center
            GameManager.AddToDebugLog($"[SingleplayerCardPlay] Validation: cardValue={cardValue} != sumValue={sumValue} → ADD-TO-CENTER");
            Debug.Log($"[SinglePlayerModeController] Capture validation: cardValue ({cardValue}) does not match sumValue ({sumValue}) → ADD-TO-CENTER");
            isValidCapture = false;
        }

        // SERVER DECISION: Route to appropriate handler based on validation result
        if (isValidCapture)
        {
            // Player captured center cards
            SerializableCard capturedCards = new SerializableCard(selectedCenterCards);
            GameManager.AddToDebugLog($"[SingleplayerCardPlay] DECISION: Routing to ProcessPlayerMove (CAPTURE) with {selectedCenterCards.Count} center cards");
            Debug.Log($"[SinglePlayerModeController] Server decision: CAPTURE {selectedCenterCards.Count} center cards");
            ProcessPlayerMove(cardId, capturedCards, isPisti: false, isJackPisti: false);
        }
        else
        {
            // Player must add card to center (no capture)
            int[] cardKindValue = CardInteraction.cardLookup.TryGetValue(cardId, out var ci) ? ci.GetCardID() : null;
            GameManager.AddToDebugLog($"[SingleplayerCardPlay] DECISION: Routing to ProcessPlayerAddToCenter (ADD-TO-CENTER)");
            Debug.Log($"[SinglePlayerModeController] Server decision: ADD-TO-CENTER");
            ProcessPlayerAddToCenter(cardId, cardKindValue);
        }
    }

    /// <summary>
    /// Called by GameManager when the player plays a card that potentially captures.
    /// Routes to coroutine that handles animation and game state updates.
    /// </summary>
    public void ProcessPlayerMove(string cardId, SerializableCard capturedCards, bool isPisti, bool isJackPisti)
    {
        if (!isGameRunning || !IsPlayerTurn) return;
        StartCoroutine(ProcessPlayerMoveCoroutine(cardId, capturedCards, isPisti, isJackPisti));
    }

    /// <summary>
    /// Coroutine: Process player card play with animation.
    /// Updates GameManager state, animates card, evaluates damage, then unblocks turn loop.
    /// </summary>
    private IEnumerator ProcessPlayerMoveCoroutine(string cardId, SerializableCard capturedCards, bool isPisti, bool isJackPisti)
    {
        if (!isGameRunning || !IsPlayerTurn) yield break;

        var capturedDict = capturedCards.ToDictionary();
        Debug.Log($"[SinglePlayerModeController] Player played card {cardId}, captured {capturedDict.Count} cards");

        var capturedValues = new List<int>();
        foreach (var kvp in capturedDict)
            capturedValues.Add(kvp.Value[1]);

        // Remove captured cards from local center
        foreach (string capturedId in capturedDict.Keys)
            localCenterCards.Remove(capturedId);

        // Update GameManager state to match multiplayer flow (BEFORE animation)
        if (GameManager.LocalInstance != null)
        {
            // Add all captured cards to GameManager tracking
            foreach (var kvp in capturedDict)
            {
                if (!GameManager.LocalInstance.centerCards.ContainsKey(kvp.Key))
                {
                    GameManager.LocalInstance.centerCards[kvp.Key] = kvp.Value;
                    if (CardInteraction.cardLookup.ContainsKey(kvp.Key))
                    {
                        GameObject cardObj = CardInteraction.cardLookup[kvp.Key].gameObject;
                        if (!GameManager.LocalInstance.centerCardsObjects.Contains(cardObj))
                        {
                            GameManager.LocalInstance.centerCardsObjects.Add(cardObj);
                        }
                    }
                }
            }
            
            // Remove played card from player's hand (mirroring multiplayer flow)
            if (GameManager.LocalInstance.myCards != null && GameManager.LocalInstance.myCards.Contains(cardId))
            {
                GameManager.LocalInstance.myCards.Remove(cardId);
                Debug.Log($"[SinglePlayerModeController] Removed {cardId} from player's hand after capture");
            }
        }

        // Animate card play using DeckController (same as multiplayer)
        if (deckController != null)
        {
            yield return StartCoroutine(deckController.DiscardCapturedCards(cardId, capturedCards, 0));
        }

        lastCaptureTelemetry = new DamageSystem.CaptureTelemetry
        {
            capturedCardValues = capturedValues,
            capturedCardCount = capturedValues.Count,
            isPişti = isPisti,
            isJackPişti = isJackPisti,
            playerNumber = 0
        };

        var jokerModifiers = JokerController.GetActiveJokerModifiers(currentRoundData.activeJokerId);
        int damageDealt = DamageSystem.EvaluateTotalDamage(lastCaptureTelemetry, jokerModifiers);

        currentRoundData.totalDamageDealt += damageDealt;
        opponentHealth += damageDealt;

        Debug.Log($"[SinglePlayerModeController] Damage: {damageDealt} | Total: {currentRoundData.totalDamageDealt} | Opponent HP: {opponentHealth}/{OPPONENT_HEALTH_PER_ROUND}");

        UpdateOpponentHealthDisplay();

        if (opponentHealth >= OPPONENT_HEALTH_PER_ROUND)
        {
            OnOpponentDefeated();
        }

        playerMoveSignal = true; // Unblock WaitForPlayerInput after animation completes
    }

    /// <summary>
    /// Called by GameManager when the player adds a card to center without capturing.
    /// Routes to coroutine that handles animation and state updates.
    /// </summary>
    public void ProcessPlayerAddToCenter(string cardId, int[] cardKindValue)
    {
        if (!isGameRunning || !IsPlayerTurn) return;
        StartCoroutine(ProcessPlayerAddToCenterCoroutine(cardId, cardKindValue));
    }

    /// <summary>
    /// Coroutine: Process player card add-to-center (no capture).
    /// Animates card to center, updates state, then unblocks turn loop.
    /// Mirroring multiplayer CardAddedToCenter flow.
    /// </summary>
    private IEnumerator ProcessPlayerAddToCenterCoroutine(string cardId, int[] cardKindValue)
    {
        if (!isGameRunning || !IsPlayerTurn) yield break;

        GameManager.AddToDebugLog($"[SingleplayerCardPlay] ProcessPlayerAddToCenterCoroutine: card {cardId} moving to center");
        Debug.Log($"[SinglePlayerModeController] Player added card {cardId} to center (no capture)");

        if (cardKindValue != null)
            localCenterCards[cardId] = cardKindValue;

        // Update GameManager state (BEFORE animation)
        if (GameManager.LocalInstance != null)
        {
            GameManager.AddToDebugLog($"[SingleplayerCardPlay] Hand cleanup for {cardId}");
            
            // Remove card from player's hand (mirroring multiplayer CardAddedToCenter)
            if (GameManager.LocalInstance.myCards != null && GameManager.LocalInstance.myCards.Contains(cardId))
            {
                GameManager.LocalInstance.myCards.Remove(cardId);
                GameManager.AddToDebugLog($"[SingleplayerCardPlay] Removed {cardId} from player's hand");
                Debug.Log($"[SinglePlayerModeController] Removed {cardId} from player's hand");
            }
        }

        // Animate card to center using DeckController (same as multiplayer)
        if (deckController != null)
        {
            GameManager.AddToDebugLog($"[SingleplayerCardPlay] Calling DeckController.PlayHandCardToCenter for animation");
            yield return StartCoroutine(deckController.PlayHandCardToCenter(cardId, cardKindValue, isDiscarded: false));
            GameManager.AddToDebugLog($"[SingleplayerCardPlay] Animation complete for card {cardId}");
        }

        playerMoveSignal = true; // Unblock WaitForPlayerInput after animation completes
    }

    /// <summary>
    /// Called when player plays a card and makes a capture.
    /// Evaluate damage based on capture type and joker modifiers.
    /// Check if opponent is defeated.
    /// </summary>
    public void OnPlayerCardPlayed(string cardId, SerializableCard capturedCards, bool isPişti, bool isJackPişti)
    {
        // Delegate to the turn-loop-aware method
        ProcessPlayerMove(cardId, capturedCards, isPişti, isJackPişti);
    }

    /// <summary>
    /// Delay before opponent takes their turn for UI readability.
    /// Kept for backwards compatibility; GameTurnLoop now manages sequencing.
    /// </summary>
    private IEnumerator OpponentTurnDelay()
    {
        yield return new WaitForSeconds(1.5f);
        // No-op: ExecuteOpponentTurn handles this via GameTurnLoop
    }

    /// <summary>
    /// Execute opponent's turn — legacy stub kept for compatibility.
    /// Actual logic is in ExecuteOpponentTurn() coroutine called by GameTurnLoop.
    /// </summary>
    private void OnOpponentTurn()
    {
        playerTurnIndex++;
    }

    /// <summary>
    /// Called when opponent is defeated (opponentHealth >= OPPONENT_HEALTH_PER_ROUND).
    /// Signals round end - the round loop will check stage completion.
    /// </summary>
    private void OnOpponentDefeated()
    {
        Debug.Log($"\n✓ OPPONENT DEFEATED! Stage {currentStage}, Opponent {currentOpponentDifficulty} ({GetDifficultyName(currentOpponentDifficulty)}) defeated.\n");

        isGameRunning = false;
        roundIsWaiting = false; // Signal round loop to exit

        RunManager.SaveRunProgress(BuildRunProgressData());
    }

    /// <summary>
    /// Called when all 3 opponents in a stage are defeated.
    /// Advances to next stage (RUN LOOP continues).
    /// </summary>
    private void OnStageCleared()
    {
        Debug.Log($"\n╔════════════════════════════════╗");
        Debug.Log($"║ STAGE {currentStage} CLEARED!          ║");
        Debug.Log($"╚════════════════════════════════╝\n");

        currentStage++;
        isDeckInitializedForRound = false; // Reset deck flag for next stage
        RunManager.SaveRunProgress(BuildRunProgressData());

        const int ENDLESS_MODE_THRESHOLD = 5; // After 5 stages, offer endless mode

        if (currentStage >= ENDLESS_MODE_THRESHOLD)
        {
            Debug.Log(">>> ENDLESS MODE UNLOCKED <<<\nContinuing to Stage " + currentStage + "...\n");
        }

        // Start next stage (RUN LOOP)
        StartStage(currentStage);
    }

    /// <summary>
    /// Show endless mode selection screen.
    /// </summary>
    private void ShowEndlessModePrompt()
    {
        Debug.Log("[SinglePlayerModeController] Offering endless mode");
        // TODO: Show UI prompt for endless mode
    }

    /// <summary>
    /// Called when player loses (deck empty before defeating opponent).
    /// Signals round end.
    /// </summary>
    private void OnPlayerLost()
    {
        Debug.Log($"[SinglePlayerModeController] Player lost at Stage {currentStage}, Opponent {currentOpponentDifficulty}");

        isGameRunning = false;
        roundIsWaiting = false; // Signal round loop to exit

        // Show loss screen with summary
        // TODO: Show UI with stage reached, opponent defeated count, etc.

        RunManager.ClearRunProgress();
    }

    /// <summary>
    /// Update the stage/round display UI.
    /// </summary>
    private void UpdateStageDisplay()
    {
        if (stageRoundDisplayText != null)
        {
            int opponentNumber = currentOpponentDifficulty + 1; // 1-indexed for display
            string difficultyName = GetDifficultyName(currentOpponentDifficulty);
            string displayText = $"Stage {currentStage + 1} • Opponent {opponentNumber}/3 ({difficultyName}) • Round {currentRoundData?.currentRound ?? 0}";
            stageRoundDisplayText.text = displayText;
        }
    }

    /// <summary>
    /// Update the opponent health bar display.
    /// </summary>
    private void UpdateOpponentHealthDisplay()
    {
        if (opponentHealthBarImage != null)
        {
            float healthPercent = Mathf.Clamp01((float)opponentHealth / OPPONENT_HEALTH_PER_ROUND);
            opponentHealthBarImage.fillAmount = healthPercent;
        }

        if (opponentHealthText != null)
        {
            opponentHealthText.text = $"{opponentHealth}/{OPPONENT_HEALTH_PER_ROUND}";
        }
    }

    /// <summary>
    /// Get difficulty name from level.
    /// </summary>
    private string GetDifficultyName(int difficulty)
    {
        return difficulty == 0 ? "Easy" : (difficulty == 1 ? "Normal" : "Hard");
    }

    /// <summary>
    /// Build run progress data for persistence.
    /// </summary>
    private RunManager.RunProgressData BuildRunProgressData()
    {
        return new RunManager.RunProgressData
        {
            currentStage = currentStage,
            currentOpponentDifficulty = currentOpponentDifficulty,
            currentRound = currentRoundData?.currentRound ?? 0,
            totalDamageDealt = currentRoundData?.totalDamageDealt ?? 0,
            activeJokerId = currentRoundData?.activeJokerId ?? -1,
            seed = currentRunConfig.seed,
            deckClassName = currentRunConfig.deckClassName,
            timestamp = DateTime.Now.Ticks
        };
    }
}
