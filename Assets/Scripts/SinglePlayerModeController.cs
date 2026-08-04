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
public class SinglePlayerModeController : MonoBehaviour, IGameModeInitState
{
    
    public static SinglePlayerModeController Instance { get; private set; }

    // ===== IGameModeInitState =====
    public bool IsInitialized => deckCardsDict != null && currentRunConfig != null;
    public string GetInitializationStatus() =>
        $"[Singleplayer Init] Deck:{deckCardsDict?.Count ?? 0} cards, " +
        $"OpponentHand:{opponentHandList?.Count ?? 0}, " +
        $"Center:{localCenterCards?.Count ?? 0}, " +
        $"Stage:{currentStage}, Running:{isGameRunning}";

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

    /// <summary>
    /// Stores pre-calculated opponent info for this stage.
    /// Calculated once at stage start, then reused for all 3 rounds in the stage.
    /// </summary>
    private class OpponentInfo
    {
        public int difficulty; // 0=Easy, 1=Normal, 2=Hard
        public int maxHealth; // Pre-calculated health for this opponent
    }

    // Run-wide state
    private SinglePlayerRunConfig currentRunConfig;
    private SinglePlayerRoundData currentRoundData;
    private int currentStage = 0;
    private int currentOpponentDifficulty = 0; // 0=Easy, 1=Normal, 2=Hard
    private float currentStageDifficultyMultiplier = 1.0f; // 1.3^stageIndex, cached per stage
    private int currentRoundInStage = 0; // Opponent fights completed within current stage

    // Opponent state
    private int opponentHealth = 0; // Damage accumulated against opponent this round
    private int opponentMaxHealth = 20; // Max health for current opponent (pre-calculated for this round)
    private const int OPPONENT_BASE_HEALTH = 2; // Base health before multipliers
    private List<OpponentInfo> stageOpponentsInfo; // Pre-calculated info for all 3 opponents in current stage

    // Game loop references
    private OpponentBehaviorManager.OpponentBehaviorConfig currentOpponentBehaviorConfig;
    private DamageSystem.CaptureTelemetry lastCaptureTelemetry;
    private bool isGameRunning = false;
    private int playerTurnIndex = 0; // Tracks turn sequence for opponent behavior

    // UI references
    [SerializeField] private TextMeshProUGUI stageRoundDisplayText;
    [SerializeField] private Transform jokerDisplayParent; // Assign joker overlay parent in Inspector
    
    // UI containers (for runtime creation)
    private List<JokerController.JokerDefinition> currentJokerOptions;
    private Canvas uiCanvas;
    private OpponentHealthDisplay opponentHealthDisplay;
    
    // References needed for dealing
    private DeckController deckController;
    private bool roundIsWaiting = true; // Flag to signal round end conditions

    // Deck management - mirroring multiplayer Server.deckCardsDict
    private Dictionary<string, int[]> deckCardsDict; // Full shuffled deck for current stage
    private bool isDeckInitializedForRound = false;

    // Turn management (offline, authoritative within SinglePlayer)
    public static bool IsPlayerTurn { get; private set; } = false;
    public static bool IsGameRunning => Instance != null && Instance.isGameRunning;
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
    private bool awaitingRoundContinue = false;
    [SerializeField] private GameObject roundEndScreenParent;
    [SerializeField] private Button roundEndContinueButton;

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

        if (roundEndScreenParent != null)
        {
            // Deactivate roundEndScreenParent initially
            roundEndScreenParent.gameObject.SetActive(false);
        }
        else
        {
            
        }
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
        /*if (stageRoundDisplayText == null)
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
        }*/

        // Try to find the pre-existing OpponentHealthDisplay on the table first
        if (opponentHealthDisplay == null)
        {
            opponentHealthDisplay = FindFirstObjectByType<OpponentHealthDisplay>(FindObjectsInactive.Include);
        }

        // Only create a new one as a fallback if none exists in the scene
        if (opponentHealthDisplay == null)
        {
            GameObject healthDisplayGO = new GameObject("OpponentHealthDisplay");
            healthDisplayGO.transform.SetParent(uiCanvas.transform, false);
            RectTransform healthDisplayRect = healthDisplayGO.AddComponent<RectTransform>();
            healthDisplayRect.anchoredPosition = new Vector2(0, 200);
            healthDisplayRect.sizeDelta = new Vector2(300, 60);
            
            opponentHealthDisplay = healthDisplayGO.AddComponent<OpponentHealthDisplay>();
            
            // Start inactive - only show during singleplayer
            healthDisplayGO.SetActive(false);
        }
    }

    /// <summary>
    /// Entry point: Start a new single player run with the given configuration.
    /// </summary>
    public void StartNewRun(int seed, string deckClassName)
    {
        

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

        // Ensure side visibility is synchronized in singleplayer: side 0 on, side 1 off.
        if (SideManager.Instance != null)
        {
            SideManager.Instance.SetSidesVisible(true, false);
            SideManager.Instance.UpdateAllSides();
        }

        // Start the first stage
        StartStage(0);
    }

    /// <summary>
    /// Entry point: Resume a previously saved single player run.
    /// Loads the saved progress from PlayerPrefs and restarts the game loop from that point.
    /// Restores stage, difficulty, gold, joker, and all other run state.
    /// </summary>
    public void ResumeSavedRun()
    {
        

        // Load saved run progress from PlayerPrefs
        var savedData = RunManager.LoadRunProgress();
        if (savedData == null)
        {
            
            return;
        }

        Debug.Log($"[SinglePlayerModeController] Loaded saved run: Stage {savedData.currentStage}, " +
                  $"Opponent {savedData.currentOpponentDifficulty}, Gold {savedData.currentGold}");

        // Restore run configuration (seed, deck class)
        currentRunConfig = new SinglePlayerRunConfig
        {
            seed = savedData.seed,
            deckClassName = savedData.deckClassName
        };

        // Reset game state but keep progression state
        ResetForSingleplayerRun();

        // Restore progression state from saved data
        currentStage = savedData.currentStage;
        currentOpponentDifficulty = savedData.currentOpponentDifficulty;

        // Restore round data
        if (currentRoundData == null) currentRoundData = new SinglePlayerRoundData();
        currentRoundData.currentRound = savedData.currentRound;
        currentRoundData.totalDamageDealt = savedData.totalDamageDealt;
        currentRoundData.activeJokerId = savedData.activeJokerId;

        // Disable UI screens to show game (same as StartNewRun)
        DisableGameScreensForSinglePlayer();

        // Initialize game scene
        InitializeScene();

        // Register singleplayer participants in multiplayer systems
        RegisterSinglePlayerParticipants();

        // Restore gold amount via SuperPowerSpawner
        if (SuperPowerSpawner.LocalInstance != null)
        {
            SuperPowerSpawner.LocalInstance.SetGold(savedData.currentGold);
            
        }
        else
        {
            
        }

        // Ensure side visibility is synchronized in singleplayer: side 0 on, side 1 off.
        if (SideManager.Instance != null)
        {
            SideManager.Instance.SetSidesVisible(true, false);
            SideManager.Instance.UpdateAllSides();
        }

        // Restore the active joker for this run
        if (savedData.activeJokerId >= 0)
        {
            JokerController.SetActiveJoker(savedData.activeJokerId);
            
        }

        // Start at the saved stage and opponent
        // We'll skip joker selection since it's already been chosen and saved
        
        StartCoroutine(StartRoundLoop(currentOpponentDifficulty));
    }

    /// <summary>
    /// Disable UI screens when game starts (same as multiplayer).
    /// Hide MainUI, WinScreen, and other blocking screens so game is visible.
    /// </summary>
    private void DisableGameScreensForSinglePlayer()
    {
        

        // 1. Disable waiting screen
        GameObject waitingScreen = GameObject.Find("WaitingScreen");
        if (waitingScreen != null && waitingScreen.activeSelf)
        {
            waitingScreen.SetActive(false);
            
        }

        // 2. Disable main/lobby screen
        GameObject mainScreen = GameObject.Find("MainScreen");
        if (mainScreen != null && mainScreen.activeSelf)
        {
            mainScreen.SetActive(false);
            
        }

        // 3. Disable win screen
        GameObject winScreen = GameObject.Find("WinScreen");
        if (winScreen != null && winScreen.activeSelf)
        {
            winScreen.SetActive(false);
            
        }

        // 4. Disable MainUI
        GameObject mainUI = GameObject.Find("MainUI");
        if (mainUI != null && mainUI.activeSelf)
        {
            mainUI.SetActive(false);
            
        }
    }

    /// <summary>
    /// Initialize the game scene for single player (2-player setup).
    /// </summary>
    private void InitializeScene()
    {
        

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

        
    }

    /// <summary>
    /// Shuffle the deck using Fisher-Yates algorithm with the run's seed.
    /// Mirroring Server.SuffleCards().
    /// </summary>
    private void ShuffleDeck()
    {
        

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

        
    }

    /// <summary>
    /// Deal cards from the shuffled deck to center and player hands.
    /// Mirroring Server.DealCardsToCenter() and DealCardsToPlayerHands().
    /// Returns centerCardIDs and playerHands Dictionary ready for DealCenter/DealPlayers.
    /// </summary>
    private (List<string>, Dictionary<int, List<string>>) DealFromDeck(bool dealToCenter = true)
    {
        
        
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
        

        const int PLAYER_NUMBER = 0; // Human player is always player 0 (bottom/local)
        const int PLAYER_COUNT = 2;  // Singleplayer is a 2-player game (human + opponent AI)

        // 1. Register player number with DeckController
        if (deckController != null)
        {
            deckController.SetPlayerNumber(PLAYER_NUMBER);
            
        }
        else
        {
            
        }

        // 2. Register player count with DeckController and dependent systems
        // This calls ElHolderScript.SetHandMode(playerCount) internally
        if (deckController != null)
        {
            deckController.GetPlayerCount(PLAYER_COUNT, isReconnection: false);
            
        }

        // 3. Register player number with SuperPowerSpawner
        if (SuperPowerSpawner.LocalInstance != null)
        {
            SuperPowerSpawner.LocalInstance.SetPlayerNumber(PLAYER_NUMBER);
            
        }
        else
        {
            
        }

        // 4. Register hand mode with ElHolderScript (for token positions)
        if (ElHolderScript.LocalInstance != null)
        {
            ElHolderScript.LocalInstance.SetHandMode(PLAYER_COUNT);
            
        }
        else
        {
            
        }

        // 5. Opponent health display will be initialized when round starts with pre-calculated opponent max health
        if (opponentHealthDisplay == null)
        {
            
        }
        else
        {
            // Keep the component disabled until a round actually starts
            opponentHealthDisplay.SetActive(false);
            
        }

        
    }

    /// <summary>
    /// Calculate opponent's max health based on current stage and difficulty.
    /// Formula: BaseHealth × StageDifficultyMultiplier × DifficultyFactor
    /// Easy: 1.0, Normal: 1.15, Hard: 1.35
    /// </summary>
    private int CalculateOpponentMaxHealth(int difficulty)
    {
        float difficultyFactor = difficulty switch
        {
            0 => 1.0f,   // Easy
            1 => 1.15f,  // Normal
            2 => 1.35f,  // Hard
            _ => 1.0f
        };

        int calculatedHealth = Mathf.RoundToInt(OPPONENT_BASE_HEALTH * currentStageDifficultyMultiplier * difficultyFactor);
        
        return calculatedHealth;
    }

    /// <summary>
    /// Pre-calculate health for all 3 opponents in this stage.
    /// Called once at the start of StartRoundLoop before any rounds begin.
    /// This allows displaying opponent info to the player before rounds start.
    /// </summary>
    private void CalculateStageOpponentsInfo()
    {
        

        stageOpponentsInfo = new List<OpponentInfo>();
        for (int difficulty = 0; difficulty < 3; difficulty++)
        {
            int maxHealth = CalculateOpponentMaxHealth(difficulty);
            stageOpponentsInfo.Add(new OpponentInfo
            {
                difficulty = difficulty,
                maxHealth = maxHealth
            });
            
        }

        
    }

    // ===== LOOP RESET FUNCTIONS =====

    /// <summary>
    /// RunLoop reset: Full teardown for a fresh run.
    /// Destroys all card GameObjects, resets all power flags, and clears run-wide state.
    /// Called once at the start of StartNewRun() before anything else begins.
    /// </summary>
    private void ResetForSingleplayerRun()
    {
        

        // Full card teardown via GameManager (destroys GameObjects, clears power dicts, all flags)
        if (GameManager.LocalInstance != null)
            GameManager.LocalInstance.ResetForNewGame();

        // Deactivate health display during reset
        if (opponentHealthDisplay != null)
        {
            opponentHealthDisplay.SetActive(false);
        }

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
        awaitingRoundContinue = false;

        // Reset turn management
        IsPlayerTurn = false;
        playerMoveSignal = false;

        // Reset local state mirrors
        opponentHealth = 0;
        opponentHandList.Clear();
        localCenterCards.Clear();
        cardsRemainingInDeck = 0;

        // Reset gold for a fresh run
        if (SuperPowerSpawner.LocalInstance != null)
        {
            SuperPowerSpawner.LocalInstance.ResetGoldToStarting();
        }

        
    }

    /// <summary>
    /// StageLoop reset: Minimal setup for a new stage.
    /// Calculates difficulty multiplier and resets round counter.
    /// Cards and their power modifications are NOT touched — they persist across stages.
    /// Called at the start of StartStage() before joker selection.
    /// </summary>
    private void ResetForStage(int stageIndex)
    {
        

        // Calculate and cache difficulty multiplier (1.3^stageIndex)
        currentStageDifficultyMultiplier = Mathf.Pow(1.3f, stageIndex);

        // Reset round counter within this stage (ResetForRound will increment it)
        currentRoundInStage = 0;
        currentOpponentDifficulty = 0;

        // Reset deck flag so a fresh deck is built for this stage's first round
        isDeckInitializedForRound = false;

        

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
        

        // Increment round counter within this stage
        currentRoundInStage++;
        currentOpponentDifficulty = opponentDifficulty;

        // Get pre-calculated max health for this opponent from stage info
        if (stageOpponentsInfo != null && opponentDifficulty < stageOpponentsInfo.Count)
        {
            opponentMaxHealth = stageOpponentsInfo[opponentDifficulty].maxHealth;
            
        }
        else
        {
            // Fallback if info not pre-calculated (shouldn't happen if flow is correct)
            opponentMaxHealth = CalculateOpponentMaxHealth(opponentDifficulty);
            
        }

        // Reset opponent health to 0 (no damage dealt yet at round start)
        opponentHealth = 0;
        if (currentRoundData == null) currentRoundData = new SinglePlayerRoundData();
        currentRoundData.currentRound++;
        currentRoundData.totalDamageDealt = 0;
        currentRoundData.opponentDamageDealt = 0;

        // Initialize opponent health display with the pre-calculated max health
        // At round start, opponent health = 0 and display max health = pre-calculated value
        if (opponentHealthDisplay != null)
        {
            opponentHealthDisplay.SetActive(true);
            opponentHealthDisplay.InitializeHealthDisplay(opponentMaxHealth);
            
        }

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
        

        if (jokerDisplayParent == null)
        {
            
            return;
        }

        // ===== TESTING: Show ALL jokers in order for testing purposes =====
        // TODO: This is temporary for testing. Will be changed back to randomized selection of 3 options later.
        // Original logic (commented out):
        // currentJokerOptions = JokerController.GenerateRandomJokerOptions(3, UnityEngine.Random.state);
        
        // Testing: Get all available jokers in order
        var allJokers = JokerDefinitions.GetAllJokerDefinitions();
        currentJokerOptions = new List<JokerController.JokerDefinition>(allJokers);

        // Clear any existing jokers
        foreach (Transform child in jokerDisplayParent)
        {
            Destroy(child.gameObject);
        }

        // Create joker objects for all available jokers (testing: normally would be 3 random options)
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
            
            
        }

        
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
        

        if (currentRoundData == null)
        {
            currentRoundData = new SinglePlayerRoundData();
        }

        currentRoundData.activeJokerId = jokerID;
        JokerController.SetActiveJoker(jokerID);
        
        // NEW: Initialize PassiveManager with selected joker
        if (PassiveManager.Instance != null)
        {
            PassiveManager.Instance.SetActivePassive(jokerID);
            GameManager.AddToDebugLog($"[SinglePlayer] OnJokerSelected: Initialized PassiveManager with jokerID={jokerID}");
        }
        
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
    /// Each opponent battle automatically transitions to the next when defeated via CheckRoundEndConditions().
    /// When all 3 are defeated, calls OnStageCleared() to advance to next stage.
    /// </summary>
    private IEnumerator StartRoundLoop(int startingDifficulty)
    {
        
        

        // PRE-CALCULATE opponent info for all 3 opponents before any round starts
        // This allows the UI to display opponent stats at stage start
        CalculateStageOpponentsInfo();

        for (int difficulty = startingDifficulty; difficulty < 3; difficulty++)
        {
            currentOpponentDifficulty = difficulty;
            
            
            
            // ROUND LOOP: Run a single opponent battle
            yield return StartCoroutine(StartRound(difficulty));

            if (opponentHealth >= opponentMaxHealth)
            {
                
                yield return new WaitUntil(() => awaitingRoundContinue);
                awaitingRoundContinue = false;
                continue;
            }

            if (!isGameRunning && !roundIsWaiting)
            {
                
                yield break;
            }
        }

        
        OnStageCleared();
    }

    /// <summary>
    /// ROUND LOOP: Manages a single round with one opponent.
    /// Handles deck setup, initial deals, then drives the sequential turn loop.
    /// When opponent is defeated or player loses, CheckRoundEndConditions() triggers automatic transition.
    /// </summary>
    private IEnumerator StartRound(int opponentDifficulty)
    {
        
        

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

            // Keep a lookup before dealing because DealFromDeck removes dealt cards from deckCardsDict.
            Dictionary<string, int[]> dealtCardValueLookup = new Dictionary<string, int[]>(deckCardsDict);

            yield return StartCoroutine(deckController.DeckStart());
            

            var (centerCardIDs, playerHands) = DealFromDeck(true);
            handsDealtCount = 1;

            // Store opponent hand locally before DeckController animates it
            foreach (string id in playerHands[1])
                opponentHandList.Add(id);

            // Build local center mirror from dealt center IDs (including the initial 4 center cards).
            foreach (string id in centerCardIDs)
                if (dealtCardValueLookup.TryGetValue(id, out int[] val))
                    localCenterCards[id] = val;

            // cardsRemainingInDeck is whatever is left after dealing
            cardsRemainingInDeck = deckCardsDict.Count;

            yield return StartCoroutine(deckController.DealCenter(centerCardIDs));
            

            deckController.DealPlayers(2, playerHands);
            
        }

        isGameRunning = true;
        
        

        // Drive sequential Player → Opponent → Player turns until round ends
        // Round ends when CheckRoundEndConditions() detects opponent defeated or player defeated
        yield return StartCoroutine(GameTurnLoop());

        
        
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
            

            yield return StartCoroutine(WaitForPlayerInput());

            if (!roundIsWaiting || !isGameRunning) break;

            // --- OPPONENT TURN ---
            IsPlayerTurn = false;
            if (GameManager.LocalInstance != null)
                GameManager.LocalInstance.UpdateCurrentPlayer(1, playerTurnIndex);
            

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
            
            yield break;
        }

        // Brief delay so player can see what the opponent plays
        yield return new WaitForSeconds(1.0f);

        int cardIndex = OpponentBehaviorManager.SelectCardToPlay(opponentHandList, currentOpponentBehaviorConfig, playerTurnIndex);
        if (cardIndex < 0 || cardIndex >= opponentHandList.Count)
        {
            
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
            
        }

        // Animate opponent move and update GameManager state via DeckController
        if (deckController != null)
        {
            if (capturedSomething)
            {
                SerializableCard capturedCards = new SerializableCard(capturedCenterDict);
                
                yield return StartCoroutine(deckController.DiscardCapturedCards(playedCardId, capturedCards, 1));
            }
            else
            {
                
                yield return StartCoroutine(deckController.PlayHandCardToCenter(playedCardId, playedCardKindValue, isDiscarded: false));
            }
        }

        yield return new WaitForSeconds(0.5f); // Pause after animation for readability
    }

    /// <summary>
    /// DEBUG METHOD: Simulate a capture for testing without full game loop.
    /// Call this from console to test damage calculations.
    /// </summary>
    public void DebugSimulateCapture(List<int> capturedCardValues, int playedCardValue = 0, bool isPişti = false, bool isJackPişti = false)
    {
        if (!isGameRunning)
        {
            
            return;
        }

        

        lastCaptureTelemetry = new DamageSystem.CaptureTelemetry
        {
            capturedCardValues = capturedCardValues,
            capturedCardCount = capturedCardValues.Count,
            isPişti = isPişti,
            isJackPişti = isJackPişti,
            playerNumber = 0, // Player is always seat 0
            playedCardValue = playedCardValue
        };

        // NEW: PassiveManager applies joker effects to telemetry before damage calculation
        if (PassiveManager.Instance != null)
        {
            lastCaptureTelemetry = PassiveManager.Instance.OnCapture(lastCaptureTelemetry);
        }

        // Evaluate damage (now reads modifiers from telemetry, not from separate jokerModifiers)
        int damageDealt = DamageSystem.EvaluateTotalDamage(lastCaptureTelemetry);

        currentRoundData.totalDamageDealt += damageDealt;
        opponentHealth += damageDealt;

        

        UpdateOpponentHealthDisplay();

        // Check if opponent defeated
        CheckRoundEndConditions();
    }

    /// <summary>
    /// DEBUG METHOD: Simulate opponent turn (for testing).
    /// </summary>
    public void DebugSimulateOpponentTurn()
    {
        
        playerTurnIndex++;
        
    }

    /// <summary>
    /// DEBUG METHOD: Manually select a joker (useful for console testing).
    /// </summary>
    public void DebugSelectJoker(int jokerID)
    {
        
        OnJokerSelected(jokerID);
    }

    /// <summary>
    /// DEBUG METHOD: Manually defeat current opponent.
    /// </summary>
    public void DebugDefeatCurrentOpponent()
    {
        
        opponentHealth = opponentMaxHealth;
        CheckRoundEndConditions();
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
        

        // SERVER VALIDATION: Determine if this is a legal capture or add-to-center
        bool isValidCapture = false;

        if (GameManager.LocalInstance.oynayamazsinActive)
        {
            // Power active: cannot capture, must add to center
            GameManager.AddToDebugLog("[SingleplayerCardPlay] Validation: oynayamazsinActive=true → forcing ADD-TO-CENTER");
            
            isValidCapture = false;
        }
        else if (cardValue == sumValue || (cardValue == 11 && sumValue != 0))
        {
            // Capture rules: card matches sum, or jack (11) with non-empty center
            GameManager.AddToDebugLog($"[SingleplayerCardPlay] Validation: cardValue={cardValue} matches sumValue={sumValue} → CAPTURE VALID");
            
            isValidCapture = true;
        }
        else
        {
            // No match → add to center
            GameManager.AddToDebugLog($"[SingleplayerCardPlay] Validation: cardValue={cardValue} != sumValue={sumValue} → ADD-TO-CENTER");
            
            isValidCapture = false;
        }

        // SERVER DECISION: Route to appropriate handler based on validation result
        if (isValidCapture)
        {
            // Player captured center cards
            SerializableCard capturedCards = new SerializableCard(selectedCenterCards);
            GameManager.AddToDebugLog($"[SingleplayerCardPlay] DECISION: Routing to ProcessPlayerMove (CAPTURE) with {selectedCenterCards.Count} center cards");
            
            ProcessPlayerMove(cardId, capturedCards, isPisti: false, isJackPisti: false, cardValue);
        }
        else
        {
            // Player must add card to center (no capture)
            int[] cardKindValue = CardInteraction.cardLookup.TryGetValue(cardId, out var ci) ? ci.GetCardID() : null;
            GameManager.AddToDebugLog($"[SingleplayerCardPlay] DECISION: Routing to ProcessPlayerAddToCenter (ADD-TO-CENTER)");
            
            ProcessPlayerAddToCenter(cardId, cardKindValue);
        }
    }

    /// <summary>
    /// Called by GameManager when the player plays a card that potentially captures.
    /// Routes to coroutine that handles animation and game state updates.
    /// cardValue is the validated card value from server validation (required for damage calculation).
    /// </summary>
    public void ProcessPlayerMove(string cardId, SerializableCard capturedCards, bool isPisti, bool isJackPisti, int cardValue)
    {
        if (!isGameRunning || !IsPlayerTurn) return;
        StartCoroutine(ProcessPlayerMoveCoroutine(cardId, capturedCards, isPisti, isJackPisti, cardValue));
    }

    /// <summary>
    /// Coroutine: Process player card play with animation.
    /// Updates GameManager state, animates card, evaluates damage, then unblocks turn loop.
    /// cardValue: the validated card value from server validation (no lookup needed).
    /// </summary>
    private IEnumerator ProcessPlayerMoveCoroutine(string cardId, SerializableCard capturedCards, bool isPisti, bool isJackPisti, int cardValue)
    {
        if (!isGameRunning || !IsPlayerTurn) yield break;

        var capturedDict = capturedCards.ToDictionary();
        

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
                
            }

            // Trigger gold gain logic for capture (mirroring multiplayer flow)
            if (SuperPowerSpawner.LocalInstance != null)
            {
                SuperPowerSpawner.LocalInstance.OnLocalCapture(cardId);
            }
        }

        // Animate card play using DeckController (same as multiplayer)
        if (deckController != null)
        {
            yield return StartCoroutine(deckController.DiscardCapturedCards(cardId, capturedCards, 0));
        }

        // Use the validated cardValue passed from server validation (no lookup needed)
        int playedCardValue = cardValue;

        Debug.Log($"[SingleplayerCardPlay] ProcessPlayerMoveCoroutine: cardId={cardId}, playedCardValue={playedCardValue}, capturedCount={capturedValues.Count}, isPisti={isPisti}, isJackPisti={isJackPisti}");

        lastCaptureTelemetry = new DamageSystem.CaptureTelemetry
        {
            capturedCardValues = capturedValues,
            capturedCardCount = capturedValues.Count,
            isPişti = isPisti,
            isJackPişti = isJackPisti,
            playerNumber = 0,
            playedCardValue = playedCardValue
        };

        // NEW: PassiveManager applies joker effects to telemetry before damage calculation
        if (PassiveManager.Instance != null)
        {
            lastCaptureTelemetry = PassiveManager.Instance.OnCapture(lastCaptureTelemetry);
        }

        // EvaluateTotalDamage now reads modifiers from telemetry (no separate jokerModifiers parameter)
        int damageDealt = DamageSystem.EvaluateTotalDamage(lastCaptureTelemetry);

        currentRoundData.totalDamageDealt += damageDealt;
        opponentHealth += damageDealt;

        

        UpdateOpponentHealthDisplay();

        // Check if opponent defeated and handle round transitions
        CheckRoundEndConditions();

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
    /// cardValue is required for correct damage calculation.
    /// </summary>
    public void OnPlayerCardPlayed(string cardId, SerializableCard capturedCards, bool isPişti, bool isJackPişti, int cardValue)
    {
        // Delegate to the turn-loop-aware method
        ProcessPlayerMove(cardId, capturedCards, isPişti, isJackPişti, cardValue);
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
    /// Check if the current round should end based on game state (opponent defeated or player defeated).
    /// Called whenever opponent health changes to determine if round should end and trigger transitions.
    /// Automatically handles transition to next round or next stage based on stage progress.
    /// </summary>
    private void CheckRoundEndConditions()
    {
        // Check 1: Is opponent defeated?
        if (opponentHealth >= opponentMaxHealth)
        {
            
            
            
            

            isGameRunning = false;
            roundIsWaiting = false; // Signal GameTurnLoop to exit
            awaitingRoundContinue = false;

            RunManager.SaveRunProgress(BuildRunProgressData());

            ShowRoundEndScreen("Round Complete! Press Continue for the next opponent.");

            // NEW: Notify PassiveManager of round end
            if (PassiveManager.Instance != null)
            {
                bool playerWon = true;  // Player won this round (opponent defeated)
                PassiveManager.Instance.OnRoundEnd(currentOpponentDifficulty, playerWon);
            }

            // Determine next action based on stage progress
            if (currentRoundInStage < 3) // More opponents in this stage (0-indexed: 0, 1, 2)
            {
                int remainingOpponents = 3 - currentRoundInStage;
                
                // The StartRoundLoop coroutine will automatically call StartRound with next difficulty
                // which will call ResetForRound and begin the next opponent fight
            }
            else
            {
                // All 3 opponents in this stage defeated (currentRoundInStage == 3)
                
                
                
                
                // OnStageCleared will be called by StartRoundLoop when the difficulty loop ends
            }
            return;
        }

        // Check 2: Is player defeated? (checked in GameTurnLoop when both hands empty and no more deals)
        // Player defeat is handled in GameTurnLoop's hand re-deal logic, not here
    }

    /// <summary>
    /// Called when all 3 opponents in a stage are defeated.
    /// Advances to next stage (RUN LOOP continues).
    /// </summary>
    private void OnStageCleared()
    {
        
        
        
        

        // NEW: Notify PassiveManager of stage progression
        if (PassiveManager.Instance != null)
        {
            PassiveManager.Instance.OnStageCleared(currentStage);
        }

        currentStage++;
        isDeckInitializedForRound = false; // Reset deck flag for next stage
        RunManager.SaveRunProgress(BuildRunProgressData());

        const int ENDLESS_MODE_THRESHOLD = 5; // After 5 stages, offer endless mode

        if (currentStage >= ENDLESS_MODE_THRESHOLD)
        {
            
        }

        // Start next stage (RUN LOOP)
        StartStage(currentStage);
    }

    /// <summary>
    /// Show endless mode selection screen.
    /// </summary>
    private void ShowEndlessModePrompt()
    {
        
        // TODO: Show UI prompt for endless mode
    }

    /// <summary>
    /// Called when player loses (deck empty before defeating opponent).
    /// Signals round end and shows loss UI.
    /// </summary>
    private void OnPlayerLost()
    {
        
        
        
        
        

        isGameRunning = false;
        roundIsWaiting = false; // Signal round loop to exit
        awaitingRoundContinue = false;

        // Deactivate health display
        if (opponentHealthDisplay != null)
        {
            opponentHealthDisplay.SetActive(false);
        }

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
        // Update health display slider
        if (opponentHealthDisplay != null)
        {
            opponentHealthDisplay.UpdateHealth(opponentHealth);
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
    /// Public entry point for processors and external systems to trigger a run save.
    /// Safe to call at any time; no-ops if the run hasn't been configured yet.
    /// </summary>
    public void SaveRunState()
    {
        if (currentRunConfig == null) return;
        RunManager.SaveRunProgress(BuildRunProgressData());
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
            currentGold = SuperPowerSpawner.LocalInstance != null ? SuperPowerSpawner.LocalInstance.GetCurrentGold() : 0,
            seed = currentRunConfig.seed,
            deckClassName = currentRunConfig.deckClassName,
            timestamp = DateTime.Now.Ticks
        };
    }

    // ===== SUPERPOWER ACTIVATION LOGIC (IPowerProcessor) =====

    public void ExecutePeekOpponentCard()
    {
        Debug.Log("[UcundanGözAt] SinglePlayerModeController.ExecutePeekOpponentCard() called");
        
        if (opponentHandList.Count == 0)
        {
            Debug.LogError("[UcundanGözAt] SinglePlayerModeController: Opponent hand is empty - cannot peek");
            return;
        }
        
        Debug.Log($"[UcundanGözAt] SinglePlayerModeController: Opponent has {opponentHandList.Count} cards in hand");
        
        int cardIndex = UnityEngine.Random.Range(0, opponentHandList.Count);
        Debug.Log($"[UcundanGözAt] SinglePlayerModeController: Selected random card index {cardIndex}");
        
        if (GameManager.LocalInstance == null)
        {
            Debug.LogError("[UcundanGözAt] SinglePlayerModeController: GameManager.LocalInstance is null");
            return;
        }
        
        Debug.Log("[UcundanGözAt] SinglePlayerModeController: Calling GameManager.OnPeekOpponentCardSynced(opponentNo=1, cardIndex=" + cardIndex + ")");
        GameManager.LocalInstance.OnPeekOpponentCardSynced(1, cardIndex);
        Debug.Log("[UcundanGözAt] SinglePlayerModeController: GameManager.OnPeekOpponentCardSynced() returned");
        
        Debug.Log("[UcundanGözAt] SinglePlayerModeController: Calling ShowcaseSuperPower()");
        GameManager.LocalInstance.ShowcaseSuperPower("Ucundan Göz At");
        
        Debug.Log("[UcundanGözAt] SinglePlayerModeController: Calling SaveRunState()");
        SaveRunState();
        
        Debug.Log("[UcundanGözAt] SinglePlayerModeController.ExecutePeekOpponentCard() completed successfully");
    }

    public void ExecuteBayaBayaBak()
    {
        GameManager.LocalInstance.OnBayaBayaBakSynced(1);
        GameManager.LocalInstance.ShowcaseSuperPower("Baya Baya Bak");
        SaveRunState();
    }

    public void ExecuteSwapCardWithOpponent(string selectedCardId)
    {
        if (GameManager.LocalInstance.myCards == null || GameManager.LocalInstance.myCards.Count == 0 || opponentHandList.Count == 0) return;
        
        // Validate selected card is in player hand
        if (!GameManager.LocalInstance.myCards.Contains(selectedCardId)) return;
        
        // Pick random opponent card
        string oppCardId = opponentHandList[UnityEngine.Random.Range(0, opponentHandList.Count)];
        
        if (opponentHandList.Contains(oppCardId))
        {
            opponentHandList.Remove(oppCardId);
            opponentHandList.Add(selectedCardId);
        }

        GameManager.LocalInstance.StartCoroutine(GameManager.LocalInstance.OnSunuDegisTokusSynced(0, 1, selectedCardId, oppCardId));
        GameManager.LocalInstance.ShowcaseSuperPower("Değiş Tokuş");
        SaveRunState();
    }

    public void ExecuteValeArar()
    {
        GameManager.LocalInstance.ActivateValeArarPower();
        GameManager.LocalInstance.ShowcaseSuperPower("Vale Arar");
        SaveRunState();
    }

    public void ExecuteBomba()
    {
        localCenterCards.Clear();
        GameManager.LocalInstance.OnBombaCenter();
        GameManager.LocalInstance.ShowcaseSuperPower("Bomba");
        SaveRunState();
    }

    public void ExecuteYapamazsın()
    {
        GameManager.LocalInstance.SetYapamazsınActive(true);
        GameManager.LocalInstance.ShowcaseSuperPower("Yapamazsın");
        SaveRunState();
    }

    public void StartKapkacSelection()
    {
        GameManager.LocalInstance.StartKapkacSelectionPower();
    }

    public void ExecuteKapkacOnCard(string cardId)
    {
        GameManager.LocalInstance.OnKapkacCardChanged(cardId);
        GameManager.LocalInstance.ShowcaseSuperPower("Kapkaç");
        SaveRunState();
    }

    public void StartYandimAnamSelection()
    {
        GameManager.LocalInstance.StartYandimAnamSelectionPower();
    }

    public void ExecuteYandimAnamOnCard(string cardId)
    {
        GameManager.LocalInstance.OnYandimAnamCardChanged(cardId);
        GameManager.LocalInstance.ShowcaseSuperPower("Yandım Anam");
        SaveRunState();
    }

    public void StartDegisTokusSelection()
    {
        GameManager.LocalInstance.StartDegisTokusSelectionPower();
    }

    public void ExecuteBlockNextPlayer()
    {
        GameManager.LocalInstance.SetOynayamazsinActive(true);
        GameManager.LocalInstance.ShowcaseSuperPower("Oynayamazsın");
        SaveRunState();
    }

    public void StartKopyalaYapistirSelection()
    {
        GameManager.LocalInstance.StartKopyalaYapistirDualSelection(null);
    }

    public void ExecuteKopyalaYapistir(string targetId, string sourceId)
    {
        GameManager.LocalInstance.OnKopyalaYapistir(targetId, sourceId);
        GameManager.LocalInstance.ShowcaseSuperPower("Kopyala Yapıştır");
        SaveRunState();
    }

    public void ExecuteVerZehri()
    {
        GameManager.LocalInstance.SetVerZehriActive(true);
        GameManager.LocalInstance.ShowcaseSuperPower("Ver Zehri");
        SaveRunState();
    }

    public void ExecuteKutsalDeste()
    {
        GameManager.LocalInstance.SetKutsalDesteActive(true);
        GameManager.LocalInstance.ShowcaseSuperPower("Kutsal Deste");
        SaveRunState();
    }

    public void StartBuDahaIyiSelection()
    {
        GameManager.LocalInstance.StartBuDahaIyiSelectionPower();
    }

    public void ExecuteBuDahaIyi(string handCardId, string topCenterCardId)
    {
        if (localCenterCards.ContainsKey(topCenterCardId))
        {
            int[] handCardValue = CardInteraction.cardLookup.TryGetValue(handCardId, out var ci) ? ci.GetCardID() : new int[] { 0, 0 };
            localCenterCards.Remove(topCenterCardId);
            localCenterCards[handCardId] = handCardValue;
        }

        int playerNo = GameManager.LocalInstance.FindHandOwnerBySearching(handCardId);
        if (playerNo == -1)
        {
            // Fallback to local seat to avoid hard failure if owner resolution misses a transient card.
            playerNo = 0;
        }

        // Keep singleplayer hand model authoritative for AI turns.
        if (playerNo != 0 && opponentHandList.Contains(handCardId))
        {
            
            int index = opponentHandList.IndexOf(handCardId);
            if (index >= 0)
            {
                // Remove the old card
                opponentHandList.RemoveAt(index);

                // Insert the new card at the same index
                opponentHandList.Insert(index, topCenterCardId);
            }
            
        }

        GameManager.LocalInstance.OnBuDahaIyiSynced(0, playerNo, handCardId, topCenterCardId);
        GameManager.LocalInstance.ShowcaseSuperPower("Bu Daha İyi");
        SaveRunState();
    }

    public void StartSunuDegisTokusSelection()
    {
        GameManager.LocalInstance.StartSunuDegisTokusDualSelection(null, "Şunu Değiş Tokuş");
    }

    public void ExecuteSunuDegisTokus(string myCardId, string oppCardId)
    {
        if (opponentHandList.Contains(oppCardId))
        {
            opponentHandList.Remove(oppCardId);
            opponentHandList.Add(myCardId);
        }

        GameManager.LocalInstance.StartCoroutine(GameManager.LocalInstance.OnSunuDegisTokusSynced(0, 1, myCardId, oppCardId));
        GameManager.LocalInstance.ShowcaseSuperPower("Şunu Değiş Tokuş");
        SaveRunState();
    }

    public void StartSunuDegisBunuTokusSelection()
    {
        GameManager.LocalInstance.StartSunuDegisBunuTokusPower();
    }

    public void ExecuteZaferPuani(int points)
    {
        
        opponentHealth += points;
        UpdateOpponentHealthDisplay();
        GameManager.LocalInstance.ShowcaseSuperPower("Zafer Puanı");
        SaveRunState();

        CheckRoundEndConditions();
    }

    /// <summary>
    /// Show a simple transition screen at the end of a round.
    /// Blocks progression until player presses Continue.
    /// </summary>
    private void ShowRoundEndScreen(string message)
    {
        awaitingRoundContinue = false;

        if (roundEndScreenParent == null || roundEndContinueButton == null)
        {
            
            return;
        }

        roundEndScreenParent.SetActive(true);

        roundEndContinueButton.onClick.RemoveAllListeners();
        roundEndContinueButton.onClick.AddListener(ProceedToNextRound);
    }

    /// <summary>
    /// Called when player presses Continue on the round end screen.
    /// Decides whether to start next round or next stage.
    /// </summary>
    private void ProceedToNextRound()
    {
        if (roundEndScreenParent != null)
        {
            roundEndScreenParent.gameObject.SetActive(false);
        }
        else
        {
            
        }

        awaitingRoundContinue = true;
    }


}
