using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Unity.Netcode;
using UnityEngine.Pool;
using UnityEngine.Tilemaps;
using Unity.Services.Multiplayer;

public class Server : NetworkBehaviour, IGameModeInitState
{
    private const string SeatMapPropertyKey = "SEAT_MAP";

    // ===== IGameModeInitState =====
    public bool IsInitialized => deckCardsDict != null && playersHandCardsIDs != null;
    public string GetInitializationStatus() =>
        $"[Multiplayer Init] Deck:{deckCardsDict?.Count ?? 0} cards, " +
        $"PlayerHands:{playersHandCardsIDs?.Count ?? 0}, " +
        $"Center:{centerCardsDict?.Count ?? 0}, " +
        $"Players:{playerCount}, TurnCounter:{turnCounter}";

    [Serializable]
    private class SeatMapPayload
    {
        public int version = 1;
        public List<SeatBinding> seats = new List<SeatBinding>();
    }

    [Serializable]
    private class SeatBinding
    {
        public int seat;
        public string ownerPlayerId;
        public string clientId;
    }

    private struct SeatOwnerInfo
    {
        public string OwnerPlayerId;
        public ulong ClientId;

        public SeatOwnerInfo(string ownerPlayerId, ulong clientId)
        {
            OwnerPlayerId = ownerPlayerId;
            ClientId = clientId;
        }
    }

    public static Server Singleton { get; private set; } // Singleton instance
    [SerializeField] private GameNetworkRelay networkRelay; // Reference to the GameNetworkRelay script
    private Dictionary<int, List<string>> playersHandCardsIDs;//Dictionary containing all the players' hands
    private Dictionary<int, List<string>> playersPooledCardsIDs;//Dictionary containing all the players' pools
    private Dictionary<int, List<string>> playersPiştiPoolCardsIDs;//Dictionary containing all the players' pişti pools
    private Dictionary<string, int[]> deckCardsDict; // replaces deckCardsIDs
    public Dictionary<string, int[]> centerCardsDict; // replaces centerCardsIDs
    public Dictionary<string, int[]> allCardLookup = new Dictionary<string, int[]>();
    private int playerCount; // Number of players in the game for the game mode
    private int connectedPlayerCount = 0; // Start with 0, host will make it 1
    private HashSet<ulong> countedClients = new HashSet<ulong>(); // Tracks clients that contributed to connectedPlayerCount
    [SerializeField] private int seed;//Seed for the deck suffle
    public int turnCounter = 0;
    public int currentPlayer;//The player that is currently playing
    int[] points;// To store points for each player
    private int[] piştiCounts;
    public int lastPlayerToCapture = -1;
    private int startingPlayerNo = 0;
    private float timer = 0;
    private float turnTime = 15f; // the time player has before turn skips
    private int roundCount = 0;
    private int readyToEndTurnCounter = 0; //Counter to make sure every connected player is ready to end the turn
    private bool singleDebuggingMode;
    public bool winnerPrintFlag = false;
    
    // Bot system for 1v1 games
    [SerializeField] private bool isBotModeEnabled = false; // Bot mode setting controlled by button presses
    [SerializeField] private float botMoveDelay = 2.0f; // Delay before bot makes a move (inspector editable)
    private bool botPlayerActive = false; // Tracks if bot is playing as player 1
    private int botPlayerNumber = 1; // Bot plays as player 1 (opponent)
    private Coroutine botMoveCoroutine;
    
    // Reference to BotPlayer script
    private BotPlayer botPlayer;
    
    // BOT PLACEHOLDER SYSTEM: Track which players are currently controlled by bots
    private HashSet<int> botControlledPlayers = new HashSet<int>();
    
    // Server.cs
    private Dictionary<string, string> copiedCardMap = new Dictionary<string, string>();
    public List<string> firstThreeDealtCardIds = new List<string>();
    public int p1side_selfFakePointReduction = 0;
    public int p2side_selfFakePointReduction = 0;
    public int p1side_oppFakePointReduction = 0;
    public int p2side_oppFakePointReduction = 0;
    private bool oynayamazsinPending = false;
    private int oynayamazsinActivatedBy = -1;
    private HashSet<ulong> dealCenterFinishedClients = new HashSet<ulong>();

    // CONFIRMATION SYSTEM: Wait for all clients to acknowledge before advancing
    private HashSet<ulong> pendingTurnConfirmations = new HashSet<ulong>();
    private HashSet<ulong> dealHandsFinishedClients = new HashSet<ulong>();

    // PLAYER-CLIENT MAPPING: Map player numbers to client IDs for move validation
    public Dictionary<int, ulong> playerClientIds = new Dictionary<int, ulong>();

    private Dictionary<int, int> playerGolds = new Dictionary<int, int>();
    private Dictionary<int, List<string>> playerSuperPowers = new Dictionary<int, List<string>>();

    [Serializable]
    public class PlayerGoldEntry
    {
        public int playerNo;
        public int gold;
    }

    [Serializable]
    public class PlayerPowersEntry
    {
        public int playerNo;
        public List<string> powers;
    }

    public void UpdatePlayerGold(int playerNo, int gold)
    {
        playerGolds[playerNo] = gold;
        
        PersistServerTruthsToSession();
    }

    public void UpdatePlayerPowers(int playerNo, List<string> powers)
    {
        playerSuperPowers[playerNo] = new List<string>(powers);
        
        PersistServerTruthsToSession();
    }

    // TURN TIMER: Coroutine-based timer to auto-skip turns
    private Coroutine activeTurnTimerCoroutine;
    private bool isProcessingMove = false;

    public bool IsProcessingMove => isProcessingMove;

    /// <summary>Returns the player number for a given clientId, or -1 if not found.</summary>
    public int GetPlayerNoForClient(ulong clientId)
    {
        foreach (var kvp in playerClientIds)
        {
            if (kvp.Value == clientId)
                return kvp.Key;
        }
        return -1;
    }

    /// <summary>
    /// Checks if the current player is controlled by a bot.
    /// </summary>
    private bool IsBotTurn()
    {
        return botControlledPlayers.Contains(currentPlayer);
    }

    /// <summary>
    /// Checks if a specific player seat is controlled by a bot.
    /// </summary>
    public bool IsBotControlled(int seat)
    {
        return botControlledPlayers != null && botControlledPlayers.Contains(seat);
    }

    /// <summary>
    /// Rebinds a player slot to a new client ID after reconnection.
    /// Called when a reconnecting client announces their player number via ServerRPC.
    /// </summary>
    public void RebindPlayerClientId(int playerNo, ulong newClientId)
    {
        if (playerNo < 0 || playerNo >= playerCount)
        {
            
            return;
        }
        // Reject if this clientId already owns a *different* seat to prevent a single client
        // from claiming two seats simultaneously (e.g. a stale reconnect race).
        foreach (var kvp in playerClientIds)
        {
            if (kvp.Value == newClientId && kvp.Key != playerNo)
            {
                
                return;
            }
        }
        playerClientIds[playerNo] = newClientId;
        

        if (newClientId == NetworkManager.Singleton.LocalClientId)
        {
            PlayerPrefs.SetInt("SavedPlayerSeat", playerNo);
            PlayerPrefs.Save();
            
        }
    }

    private Dictionary<int, SeatOwnerInfo> LoadSeatMapFromSession()
    {
        var seatMap = new Dictionary<int, SeatOwnerInfo>();

        if (networkManagerUI == null || networkManagerUI.CurrentSession == null)
            return seatMap;

        var session = networkManagerUI.CurrentSession;
        if (!session.Properties.ContainsKey(SeatMapPropertyKey))
            return seatMap;

        string serialized = session.Properties[SeatMapPropertyKey].Value;
        if (string.IsNullOrEmpty(serialized))
            return seatMap;

        try
        {
            SeatMapPayload payload = JsonUtility.FromJson<SeatMapPayload>(serialized);
            if (payload == null || payload.seats == null)
                return seatMap;

            foreach (SeatBinding binding in payload.seats)
            {
                if (binding == null || string.IsNullOrEmpty(binding.ownerPlayerId))
                    continue;

                ulong parsedClientId = 0UL;
                if (!string.IsNullOrEmpty(binding.clientId))
                {
                    ulong.TryParse(binding.clientId, out parsedClientId);
                }

                seatMap[binding.seat] = new SeatOwnerInfo(binding.ownerPlayerId, parsedClientId);
            }
        }
        catch (Exception e)
        {
            
        }

        return seatMap;
    }

    private static string SerializeSeatMap(Dictionary<int, SeatOwnerInfo> seatMap)
    {
        var payload = new SeatMapPayload();
        if (seatMap != null)
        {
            foreach (var seat in seatMap.OrderBy(kvp => kvp.Key))
            {
                payload.seats.Add(new SeatBinding
                {
                    seat = seat.Key,
                    ownerPlayerId = seat.Value.OwnerPlayerId,
                    clientId = seat.Value.ClientId.ToString()
                });
            }
        }

        return JsonUtility.ToJson(payload);
    }

    private async void PersistSeatMapToSessionAsync(Dictionary<int, SeatOwnerInfo> seatMap)
    {
        if (!IsServer || networkManagerUI == null || networkManagerUI.CurrentSession == null)
            return;

        var session = networkManagerUI.CurrentSession;
        if (!session.IsHost)
            return;

        try
        {
            string serialized = SerializeSeatMap(seatMap);
            var hostSession = session.AsHost();
            hostSession.SetProperty(SeatMapPropertyKey, new SessionProperty(serialized, VisibilityPropertyOptions.Public));
            await hostSession.SavePropertiesAsync();
            
        }
        catch (Exception e)
        {
            
        }
    }

    [Serializable]
    private class ServerTruthsPayload
    {
        public int seed;
        public int[] points;
        public int[] pistiCounts;
        public int turnCounter;
        public int roundCount;
        public int lastPlayerToCapture;
        public int currentPlayer;
        public int startingPlayerNo;
        public List<PlayerGoldEntry> playerGolds;
        public List<PlayerPowersEntry> playerSuperPowers;
    }

    private string SerializeServerTruths()
    {
        var goldsList = new List<PlayerGoldEntry>();
        if (this.playerGolds != null)
        {
            foreach (var kvp in this.playerGolds)
            {
                goldsList.Add(new PlayerGoldEntry { playerNo = kvp.Key, gold = kvp.Value });
            }
        }

        var powersList = new List<PlayerPowersEntry>();
        if (this.playerSuperPowers != null)
        {
            foreach (var kvp in this.playerSuperPowers)
            {
                powersList.Add(new PlayerPowersEntry { playerNo = kvp.Key, powers = new List<string>(kvp.Value) });
            }
        }

        var payload = new ServerTruthsPayload
        {
            seed = this.seed,
            points = this.points ?? new int[0],
            pistiCounts = this.piştiCounts ?? new int[0],
            turnCounter = this.turnCounter,
            roundCount = this.roundCount,
            lastPlayerToCapture = this.lastPlayerToCapture,
            currentPlayer = this.currentPlayer,
            startingPlayerNo = this.startingPlayerNo,
            playerGolds = goldsList,
            playerSuperPowers = powersList
        };
        return JsonUtility.ToJson(payload);
    }

    private bool isTruthsDirty = false;
    private bool isPersistenceTaskActive = false;
    private float lastPersistenceTime = 0f;
    private const float PersistenceCooldown = 2.0f;

    public void PersistServerTruthsToSession()
    {
        isTruthsDirty = true;
        
        // If already waiting for cooldown or task is running, do nothing; 
        // the dirty flag ensures it will run again soon.
        if (isPersistenceTaskActive) return;

        float timeSinceLast = Time.time - lastPersistenceTime;
        if (timeSinceLast >= PersistenceCooldown)
        {
            StartCoroutine(PersistTruthsRoutine());
        }
        else
        {
            // Schedule it to run after the remaining cooldown
            float delay = PersistenceCooldown - timeSinceLast;
            Invoke("TriggerDelayedPersistence", delay);
        }
    }

    private void TriggerDelayedPersistence()
    {
        if (isTruthsDirty && !isPersistenceTaskActive)
        {
            StartCoroutine(PersistTruthsRoutine());
        }
    }

    private IEnumerator PersistTruthsRoutine()
    {
        if (!isTruthsDirty || isPersistenceTaskActive) yield break;

        isPersistenceTaskActive = true;
        isTruthsDirty = false;
        lastPersistenceTime = Time.time;

        if (networkManagerUI != null && networkManagerUI.CurrentSession != null && networkManagerUI.CurrentSession.IsHost)
        {
            string json = SerializeServerTruths();
            var hostSession = networkManagerUI.CurrentSession.AsHost();
            hostSession.SetProperty("GAME_META", new SessionProperty(json, VisibilityPropertyOptions.Public));
            
            var task = hostSession.SavePropertiesAsync();
            // Wrap the Task in a WaitUntil for the Coroutine
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
            {
                
                // On failure, mark as dirty to retry next cycle
                isTruthsDirty = true;
            }
            else
            {
                
            }
        }

        isPersistenceTaskActive = false;
        
        // Final check: if it became dirty while we were saving, trigger again after cooldown
        if (isTruthsDirty)
        {
            Invoke("TriggerDelayedPersistence", PersistenceCooldown);
        }
    }

    public bool TryHandleSeatReclaim(int playerNo, ulong clientId, string claimantPlayerId)
    {
        Dictionary<int, SeatOwnerInfo> seatMap = LoadSeatMapFromSession();

        // Server-authoritative seat resolution: if the seat number is invalid or not yet known by client,
        // search the session's seatMap to resolve the seat that matches the claimant's persistent Player ID.
        if (playerNo < 0 || playerNo >= playerCount)
        {
            int foundSeat = -1;
            foreach (var kvp in seatMap)
            {
                if (kvp.Value.OwnerPlayerId == claimantPlayerId)
                {
                    foundSeat = kvp.Key;
                    break;
                }
            }

            if (foundSeat != -1)
            {
                playerNo = foundSeat;
                
            }
            else
            {
                
                return false;
            }
        }

        if (string.IsNullOrEmpty(claimantPlayerId))
        {
            
            return false;
        }

        if (seatMap.TryGetValue(playerNo, out SeatOwnerInfo ownerInfo))
        {
            if (!string.IsNullOrEmpty(ownerInfo.OwnerPlayerId) && ownerInfo.OwnerPlayerId != claimantPlayerId)
            {
                
                return false;
            }
        }

        
        RebindPlayerClientId(playerNo, clientId);
        RemoveBotFromSeat(playerNo);

        seatMap[playerNo] = new SeatOwnerInfo(claimantPlayerId, clientId);
        PersistSeatMapToSessionAsync(seatMap);

        // Inform the reconnected client of their authorized/reclaimed seat number
        if (networkRelay != null)
        {
            networkRelay.GetPlayerNumberClientRPC(clientId, playerNo);
            
        }

        HandleReconnectJoin(clientId);
        return true;
    }

    public void AssignBotToSeat(int seat)
    {
        if (seat < 0 || seat >= playerCount)
        {
            
            return;
        }

        if (botControlledPlayers.Contains(seat))
        {
            
            return;
        }

        botControlledPlayers.Add(seat);
        

        if (GetBotPlayer() != null)
        {
            GetBotPlayer().ActivateBot();
        }

        if (currentPlayer == seat && isActiveHost && GetBotPlayer() != null)
        {
            
            GetBotPlayer().OnBotTurn(seat);
        }
    }

    public void RemoveBotFromSeat(int seat)
    {
        if (seat < 0 || seat >= playerCount)
        {
            
            return;
        }

        if (!botControlledPlayers.Remove(seat))
        {
            return;
        }

        

        if (botControlledPlayers.Count == 0 && !isBotModeEnabled && GetBotPlayer() != null)
        {
            GetBotPlayer().DeactivateBot();
            
        }
    }


    // POWER DURATION TIMER: Tracks how long a player can take during interactive power selection
    private Coroutine activePowerDurationCoroutine;
    private float powerDurationTime = 15f;

    // Track cards outside normal game flow (e.g., bombed cards)
    private List<string> bombedCards = new List<string>();

    // Game state management
    private SerializableGameState manualSavedState;
    private bool hasManualSavedState = false;
    private int snapshotVersionCounter = 0;
    public bool isActiveHost { get; private set; } = false;

    // REDO SYSTEM: Automatic game state tracking
    private SerializableGameState currentGameState;
    private SerializableGameState previousGameState;
    private SerializableGameState prePreviousGameState;
    private bool hasCurrentState = false;
    private bool hasPreviousState = false;
    private bool hasPrePreviousState = false;
    
    // REDO SYSTEM: Client confirmation tracking
    private HashSet<ulong> redoSceneReconstructionClients = new HashSet<ulong>();
    private bool isWaitingForRedoConfirmations = false;
    
    // RECONNECTION TRACKING: Track which clients are reconnecting
    private HashSet<ulong> reconnectingClients = new HashSet<ulong>();
    
    public enum ReconnectPhase { None, InitSent, ReadyReceived, SnapshotSent, Completed }
    public Dictionary<ulong, ReconnectPhase> clientReconnectPhases = new Dictionary<ulong, ReconnectPhase>();
    // Tracks when each client's reconnect handshake started, for stale-reconnect cleanup.
    private Dictionary<ulong, float> reconnectStartTimes = new Dictionary<ulong, float>();
private const float RECONNECT_TIMEOUT = 15f;

    private NetworkManagerUI networkManagerUI;

    public void ResetAllServerVariables(bool isMigration = false)
{
        if (isMigration)
        {
            
        }
        else
        {
            
            deckCardsDict = null;
            centerCardsDict = null;
            playersHandCardsIDs = null;
            playersPooledCardsIDs = null;
            playersPiştiPoolCardsIDs = null;

            playerGolds.Clear();
            playerSuperPowers.Clear();

            // CRITICAL: Reset allCardLookup for NEW GAME - this clears all power effects!
            if (allCardLookup != null)
            {
                
                allCardLookup.Clear();
                allCardLookup = new Dictionary<string, int[]>();
            }
        }

        seed = 0;
        turnCounter = 0;
        currentPlayer = 0;
        points = new int[2];
        points[0] = 0; points[1] = 0;
        piştiCounts = new int[2];
        piştiCounts[0] = 0; piştiCounts[1] = 0;
        lastPlayerToCapture = -1;
        startingPlayerNo = 0;
        timer = 0f;
        turnTime = 15f;
        connectedPlayerCount = 0; // FIXED: Reset connection count (host will make it 1)
        countedClients.Clear();
        roundCount = 0; // CRITICAL: Reset round count so SaveAllCards creates fresh cards
        singleDebuggingMode = false;
        winnerPrintFlag = false;
        copiedCardMap.Clear();
        firstThreeDealtCardIds.Clear();
        zaferPuaniPoints.Clear();
        bombedCards.Clear();
        
        // REDO SYSTEM: Reset redo states for new game
        ResetRedoStates();
        
        // Reset bot system
        botPlayerActive = false;
        if (botMoveCoroutine != null)
        {
            StopCoroutine(botMoveCoroutine);
            botMoveCoroutine = null;
        }
        
        // Reset connection tracking
        dealCenterFinishedClients.Clear();
        pendingTurnConfirmations.Clear();
        dealHandsFinishedClients.Clear();
        playerClientIds.Clear();
        if (activeTurnTimerCoroutine != null) { StopCoroutine(activeTurnTimerCoroutine); activeTurnTimerCoroutine = null; }
        if (activePowerDurationCoroutine != null) { StopCoroutine(activePowerDurationCoroutine); activePowerDurationCoroutine = null; }
        initialDealCoroutineCheckCounter = 0;
        
        // Note: No longer tracking client sync state - using desync detection instead
        
        // CRITICAL FIX: Do NOT stop relay keep-alive system during reset
        // This allows disconnected clients to reconnect to the same relay allocation
        // StopRelayKeepAlive(); // COMMENTED OUT - keep relay alive for reconnection
        // hostAllocationId = null; // COMMENTED OUT - keep allocation ID for reconnection
        // clientAllocationIds.Clear(); // COMMENTED OUT - keep client tracking for reconnection

        BroadcastLiveScoreUpdate();
        
        if (isMigration)
        {
            
        }
        else
        {
            PlayerPrefs.DeleteKey("HostMigrated");
            PlayerPrefs.Save();
            
        }
    }

    public void ResetForNewRound()
    {
        //deckCardsDict = null;
        centerCardsDict = null;
        playersHandCardsIDs = null;
        playersPooledCardsIDs = null;
        playersPiştiPoolCardsIDs = null;
        seed = 0; // Optionally keep or randomize for each round
        turnCounter = 0;
        currentPlayer = 0;
        lastPlayerToCapture = -1;
        timer = 0f;
        turnTime = 15f;
        //connectedPlayerCount = 0; // FIXED: Reset connection count (host will make it 1) for new round
        singleDebuggingMode = false;
        winnerPrintFlag = false;
        copiedCardMap.Clear();
        zaferPuaniPoints.Clear();
        bombedCards.Clear(); // Clear bombed cards for new round
        
        // Reset bot system for new round but keep botPlayerActive status
        if (botMoveCoroutine != null)
        {
            StopCoroutine(botMoveCoroutine);
            botMoveCoroutine = null;
        }
        
        // Reset BotPlayer for new round
        if (GetBotPlayer() != null && botPlayerActive)
        {
            GetBotPlayer().ResetForNewRound();
        }
        
        // Reset connection tracking
        dealCenterFinishedClients.Clear();
        pendingTurnConfirmations.Clear();
        dealHandsFinishedClients.Clear();
        if (activeTurnTimerCoroutine != null) { StopCoroutine(activeTurnTimerCoroutine); activeTurnTimerCoroutine = null; }
        if (activePowerDurationCoroutine != null) { StopCoroutine(activePowerDurationCoroutine); activePowerDurationCoroutine = null; }
        initialDealCoroutineCheckCounter = 0;
        
        // Reset move chains for new round
        MoveChainIntegrator.ResetChains();
        
        // DO NOT reset: points, piştiCounts, roundCount, startingPlayerNo
    }

    private void OnEnable()
    {
        Singleton = this;
        if (playerCount == 0) playerCount = 2;
        
        // NOTE: Network event subscription moved to SubscribeToNetworkEvents() to avoid double subscription
    }
    // Start is called before the first frame update
    /// <summary>
    /// Lazy-loads the BotPlayer reference on demand.
    /// Ensures BotPlayer.Awake() has run before we try to access the singleton.
    /// </summary>
    private BotPlayer GetBotPlayer()
    {
        if (botPlayer == null)
        {
            botPlayer = BotPlayer.Instance;
        }
        return botPlayer;
    }

    void Start()
    {
        print("server.cs start");
        ResetAllServerVariables();
        
        // Subscribe to network events if NetworkManager is available
        SubscribeToNetworkEvents();
        
        // Initialize NetworkManagerUI reference
        networkManagerUI = FindObjectOfType<NetworkManagerUI>();
        
        // AUTHENTIC HOST CHECK: If we are starting as server, we are the active host
        if (IsServer) isActiveHost = true;
        
        StartCoroutine(CleanupStaleReconnections());

        // Note: BotPlayer manages its own delays, don't override them

        //StartCoroutine(ServerSubsciribe());
    }

    /// <summary>
    /// Subscribes to NetworkManager events for disconnect handling
    /// </summary>
    private void SubscribeToNetworkEvents()
    {
        if (NetworkManager.Singleton != null && !hasSubscribedToNetworkEvents)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            NetworkManager.Singleton.OnClientConnectedCallback += AnotherPlayerConnected;
            hasSubscribedToNetworkEvents = true;
            
        }
        else if (hasSubscribedToNetworkEvents)
        {
            
        }
        else
        {
            
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (winnerPrintFlag)
        {
            DecideWinner();
            winnerPrintFlag = false;
        }
        
        // Try to subscribe to network events if not already subscribed
        if (NetworkManager.Singleton != null && !hasSubscribedToNetworkEvents)
        {
            SubscribeToNetworkEvents();
        }
    }

    public void StartGame(int tempPlayerCount)
    {
        timer = 0;
        if (roundCount > 0) ResetForNewRound();

        turnCounter = 0;
        playerCount = tempPlayerCount;
        
        // CRITICAL FIX: Do NOT manually set connectedPlayerCount here
        // Let AnotherPlayerConnected() handle all connection counting
        // The host will be counted when AnotherPlayerConnected() is called for them
        
        
        if (connectedPlayerCount == 1) singleDebuggingMode = true;
        else singleDebuggingMode = false;

        // Bot system: Activate bot for 1v1 or 2v2 games if bot mode is enabled
        // Bot activates when we have a 1v1 game (bot plays as player 1) or 2v2 game (bot plays as players 1, 2, 3)
        botControlledPlayers.Clear();
        if (isBotModeEnabled && (playerCount == 2 || playerCount == 4))
        {
            botPlayerActive = true;
            if (playerCount == 2)
            {
                botControlledPlayers.Add(1);
                
                
            }
            else if (playerCount == 4)
            {
                botControlledPlayers.Add(1);
                botControlledPlayers.Add(2);
                botControlledPlayers.Add(3);
                
                
            }
            
            // Activate the BotPlayer
            if (GetBotPlayer() != null)
            {
                GetBotPlayer().ActivateBot();
            }
        }
        else
        {
            botPlayerActive = false;
            
            // Deactivate the BotPlayer
            if (GetBotPlayer() != null)
            {
                GetBotPlayer().DeactivateBot();
            }
        }

        
        

        if (!IsServer)
        {
            
            return;
        }
        if (networkRelay == null)
        {
            
        }
        ServerStart();

        // Define current player and update Client
        currentPlayer = startingPlayerNo % playerCount;
        startingPlayerNo++;
        // 
        // 

        // Initialize the deck and shuffle it
        SaveAllCards();
        SuffleCards(seed);

        // Debug before ClientRpc calls
        

        GivePlayerCount();

        // Deal the cards
        InitializePlayerPools();
        if (networkRelay != null)
        {
            Invoke("CallUpdateCurrentPlayer", 1);
            networkRelay.InitializeCardPrefabsClientRPC(false); // false = not reconnection
        }
        
        // REDO SYSTEM: Save initial game state after cards are dealt
        Invoke("SaveInitialGameStateForRedo", 4f); // After cards are dealt
        PersistServerTruthsToSession();
    }

    public void CallUpdateCurrentPlayer()
    {
        networkRelay.UpdateCurrentPlayerClientRPC(currentPlayer, turnCounter);
        
        // Bot system: Check if it's the bot's turn at game start
        bool isBotTurn = IsBotTurn();
        if (isBotTurn && isActiveHost)
        {
            
            if (GetBotPlayer() != null)
            {
                GetBotPlayer().OnBotTurn(currentPlayer);
            }
        }
}

    private int initialDealCoroutineCheckCounter = 0;
    private bool hasSubscribedToNetworkEvents = false;
    
    // === DISCONNECT TRACKING ===
    private Dictionary<ulong, bool> disconnectedClients = new Dictionary<ulong, bool>();
    
    public void InitialDealCoroutineCheck()
    {
        
        
        // RECONNECTION: Skip initial deals if any clients are reconnecting
        if (reconnectingClients.Count > 0)
        {
            
            return;
        }
        
        initialDealCoroutineCheckCounter++;
        
        // Bot mode: Start deal when we have 1 human player + bot mode enabled
        // 1v1 bot mode: 1 human player + 1 bot
        // 2v2 bot mode: 1 human player + 3 bots
        // Normal mode: Start deal when all players are connected
        bool shouldStartDeal = false;
        if (isBotModeEnabled && botPlayerActive && ((playerCount == 2 && initialDealCoroutineCheckCounter == 1) || (playerCount == 4 && initialDealCoroutineCheckCounter == 1)))
        {
            shouldStartDeal = true;
            if (playerCount == 2)
            {
                
            }
            else if (playerCount == 4)
            {
                
            }
        }
        else if (initialDealCoroutineCheckCounter == connectedPlayerCount)
        {
            shouldStartDeal = true;
            
        }
        
        if (shouldStartDeal)
        {
            StartCoroutine(InitialDealCoroutine());
            initialDealCoroutineCheckCounter = 0;
        }
    }
    private IEnumerator InitialDealCoroutine()
    {
        

        yield return new WaitForSeconds(3.5f);

        // DealPhase handles: deal center → wait for all clients → deal hands → wait for all clients
        yield return StartCoroutine(DealPhase(dealCenter: true));

        

        // Start turn timer for the first player if human
        bool isBotTurn = IsBotTurn();
        if (!isBotTurn && isActiveHost)
        {
            if (activeTurnTimerCoroutine != null) StopCoroutine(activeTurnTimerCoroutine);
            activeTurnTimerCoroutine = StartCoroutine(TurnTimerCoroutine(currentPlayer));
        }
    }
private void ServerStart()
    {
        if (!IsServer)
        {
            print("Server no open");
        }
        else
        {
            System.Random random = new System.Random(DateTime.Now.Millisecond);
            if (seed == 0) seed = random.Next();

            
            //Invoke("StartGame",0f);
        }

        if (networkRelay != null)
        {
            print("networkRelay is not null");
            DelayedMessageSend();
        }
        else
        {
            print("NetworkRelay is null.");
        }
    }

    private void DelayedMessageSend()
    {
        print("DelayedMessageSend");
        networkRelay.PrintMessageServerRPC("message sent");
    }

    //Add all cards to the deckCardIDs by creating all necessary IDs.
    private void SaveAllCards()
    {
        // CRITICAL: Only create fresh cards on the FIRST round (roundCount == 0)
        // After that, preserve power-modified values from allCardLookup
        if (roundCount == 0)
        {
            
            deckCardsDict = new Dictionary<string, int[]>();
            int cardIndex = 0;
            // Club cards (kind=1, value=1 to 13)
            for (int value = 1; value <= 13; value++)
            {
                string uniqueID = "card_" + cardIndex++;
                deckCardsDict.Add(uniqueID, new int[] { 1, value });
            }
            // Diamond cards (kind=2, value=1 to 13)
            for (int value = 1; value <= 13; value++)
            {
                string uniqueID = "card_" + cardIndex++;
                deckCardsDict.Add(uniqueID, new int[] { 2, value });
            }
            // Heart cards (kind=3, value=1 to 13)
            for (int value = 1; value <= 13; value++)
            {
                string uniqueID = "card_" + cardIndex++;
                deckCardsDict.Add(uniqueID, new int[] { 3, value });
            }
            // Spade cards (kind=4, value=1 to 13)
            for (int value = 1; value <= 13; value++)
            {
                string uniqueID = "card_" + cardIndex++;
                deckCardsDict.Add(uniqueID, new int[] { 4, value });
            }

            // Initialize allCardLookup with fresh values (ONLY on first round)
            allCardLookup = new Dictionary<string, int[]>(deckCardsDict);
            
        }
        else
        {
            
            // Copy allCardLookup (which has power modifications) back to deckCardsDict
            // This preserves Kapkaç, Yandım Anam, Kopyala Yapıştır changes
            deckCardsDict = new Dictionary<string, int[]>();
            foreach (var kvp in allCardLookup)
            {
                // Clone the array to prevent reference issues
                deckCardsDict[kvp.Key] = new int[] { kvp.Value[0], kvp.Value[1] };
            }
            
        }

    }

    //Suffle the deck according to the seed
    private void SuffleCards(int seed)
    {
        System.Random rng = new System.Random(seed);
        var deckList = new List<KeyValuePair<string, int[]>>(deckCardsDict);
        int count = deckList.Count;
        for (int i = 0; i < count - 1; i++)
        {
            int r = rng.Next(i, count);
            var temp = deckList[i];
            deckList[i] = deckList[r];
            deckList[r] = temp;
        }
        // Rebuild the dictionary in shuffled order
        deckCardsDict = new Dictionary<string, int[]>();
        foreach (var kvp in deckList)
        {
            deckCardsDict[kvp.Key] = kvp.Value;
        }
    }

    //Add a new List<int[]> to the dictionary for each player representing the player pools.
    private void InitializePlayerPools()
    {
        playersPooledCardsIDs = new Dictionary<int, List<string>>();
        playersPiştiPoolCardsIDs = new Dictionary<int, List<string>>();

        for (int i = 0; i < playerCount; i++)
        {
            playersPooledCardsIDs[i] = new List<string>();
            playersPiştiPoolCardsIDs[i] = new List<string>();
        }
    }

    //Add a new List<int[]> to the dictionary for each player representing the player hands.
    private void InitializePlayersHands()
    {
        
        playersHandCardsIDs = new Dictionary<int, List<string>>();

        for (int i = 0; i < playerCount; i++)
        {
            playersHandCardsIDs[i] = new List<string>();
        }
        
    }

    //Chooses the cards to be dealth to the players
    private void DealCardsToPlayerHands()
    {
        
        
        InitializePlayersHands();//With each new deal players has to start with a fresh hand
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < playerCount; j++)
            {
                // Remove from the deck and add to player's hand
                var lastCard = deckCardsDict.Last();
                string uniqueID = lastCard.Key;
                int[] cardID = lastCard.Value;

                playersHandCardsIDs[j].Add(uniqueID);
                deckCardsDict.Remove(uniqueID);
            }
        }

        

        //Sends players hand to the gameManger so that card objects be given to the players
        SerializableDictionary playersHandCardsIDsSerialized = new SerializableDictionary(playersHandCardsIDs);
        Delayed_DealCardPrefabsToPlayers(playersHandCardsIDsSerialized);
    }

    private void Delayed_DealCardPrefabsToPlayers(SerializableDictionary playersHandCardsIDsSerialized)
    {
        if (IsServer) networkRelay.DealCardPrefabsToPlayersClientRPC(playerCount, playersHandCardsIDsSerialized);
    }

    //Chooses the cards to be dealth to the center
    private void DealCardsToCenter()
    {
        centerCardsDict = new Dictionary<string, int[]>();
        var deckEnum = deckCardsDict.GetEnumerator();
        for (int i = 0; i < 4; i++)
        {
            if (!deckEnum.MoveNext()) break;
            var kvp = deckEnum.Current;
            centerCardsDict[kvp.Key] = kvp.Value;
        }

        firstThreeDealtCardIds.Clear();
        int firstThreeCount = 0;
        foreach (var key in centerCardsDict.Keys)
        {
            if (firstThreeCount < 3)
            {
                firstThreeDealtCardIds.Add(key);
            }
            firstThreeCount++;
        }

        // Remove from deck
        foreach (var key in centerCardsDict.Keys)
        {
            deckCardsDict.Remove(key);
        }
        // Send to clients
        SerializableCard serializableCard = new SerializableCard(centerCardsDict);
        networkRelay.UpdateCenterCardIDListClientRPC(serializableCard);
        Delayed_DealCardPrefabsToCenter(serializableCard);
    }

    //******Check if the centerCardIDList in the game manager
    //is updated correctly, if so you dont need to send tempSerializableList
    //to the game mananger and you can use centerCardIDList instead
    private void Delayed_DealCardPrefabsToCenter(SerializableCard tempSerializableCard)
    {
        if (IsServer) networkRelay.DealCardPrefabsToCenterClientRPC(tempSerializableCard);
    }

    //Add played cards to the current players pool.
    public void AddDiscardedCardsToPlayerPool(SerializableCard serializableCard, int playerNumber)
    {
        var discardedDict = serializableCard.ToDictionary();
        foreach (var kvp in discardedDict)
        {
            // kvp.Key is uniqueID, kvp.Value is int[] cardID
            playersPooledCardsIDs[playerNumber].Add(kvp.Key);
            
        }

        int piştiPlayer = 5;
        bool jPistiFlag = false;

        if (discardedDict.Count == 2)
        {
            
            var values = new List<int[]>(discardedDict.Values);
            // If the last two cards have the same value, it's a pişti
            if (values[values.Count - 1][1] == values[values.Count - 2][1])
            {
                
                if (values[values.Count - 1][1] == 11)
                {
                    jPistiFlag = true;
                }
                PlayerPişti(currentPlayer, jPistiFlag);
                piştiPlayer = currentPlayer;

                // RECONSTRUCTION FIX: Track pişti cards in playersPiştiPoolCardsIDs
                if (playersPiştiPoolCardsIDs == null)
                {
                    playersPiştiPoolCardsIDs = new Dictionary<int, List<string>>();
                }
                if (!playersPiştiPoolCardsIDs.ContainsKey(playerNumber))
                {
                    playersPiştiPoolCardsIDs[playerNumber] = new List<string>();
                }
                foreach (var uniqueId in discardedDict.Keys)
                {
                    if (!playersPiştiPoolCardsIDs[playerNumber].Contains(uniqueId))
                    {
                        playersPiştiPoolCardsIDs[playerNumber].Add(uniqueId);
                    }
                }
            }
        }

        // Trigger immediate live score recalculation and update UI instantly on card capture
        BroadcastLiveScoreUpdate();

        //networkRelay.PrintPlayerPoolsClientRPC(new SerializableDictionary(playersPooledCardsIDs), piştiPlayer);
    }

    // ===== CONFIRMATION SYSTEM =====

    private int ConnectedNonBotPlayerCount()
    {
        if (NetworkManager.Singleton == null) return connectedPlayerCount;
        return NetworkManager.Singleton.ConnectedClients.Count;
    }

    private IEnumerator WaitForConfirmations(HashSet<ulong> tracker, string label, float timeoutSeconds)
    {
        float elapsed = 0f;
        while (elapsed < timeoutSeconds)
        {
            int required = ConnectedNonBotPlayerCount();
            if (tracker.Count >= required) break;
            elapsed += Time.deltaTime;
            yield return null;
        }
        int finalRequired = ConnectedNonBotPlayerCount();
            
    }

    public void OnClientTurnProcessed(ulong clientId)
    {
        pendingTurnConfirmations.Add(clientId);
        
    }

    public void OnClientDealHandsFinished(ulong clientId)
    {
        dealHandsFinishedClients.Add(clientId);
        
    }

    // ===== POST-MOVE SEQUENCE =====

    private IEnumerator PostMoveSequence()
    {
        pendingTurnConfirmations.Clear();
        yield return StartCoroutine(WaitForConfirmations(pendingTurnConfirmations, "move", 8f));

        if (activeTurnTimerCoroutine != null)
        {
            StopCoroutine(activeTurnTimerCoroutine);
            activeTurnTimerCoroutine = null;
        }

        int modulo = turnCounter % (playerCount * 4);
        int target = (playerCount * 4) - 1;
        

        if (modulo == target)
        {
            if (deckCardsDict != null && deckCardsDict.Count >= (playerCount * 4))
            {
                
                yield return StartCoroutine(DealPhase(dealCenter: false));
            }
            else
            {
                
                isProcessingMove = false;
                DecideWinner();
                yield break;
            }
        }

        // PHASE 2A: Persist checkpoint after every processed turn for host-loss recovery
        // (Implementation removed)

        isProcessingMove = false;
        NextTurn();
    }

    private IEnumerator DealPhase(bool dealCenter = false)
    {
        if (dealCenter)
        {
            dealCenterFinishedClients.Clear();
            DealCardsToCenter();
            yield return StartCoroutine(WaitForConfirmations(dealCenterFinishedClients, "centerDeal", 12f));
        }

        dealHandsFinishedClients.Clear();
        DealCardsToPlayerHands();
        yield return StartCoroutine(WaitForConfirmations(dealHandsFinishedClients, "handsDeal", 12f));
    }

    // --- Power Duration Timer Methods ---

    /// <summary>Stops the current turn timer so it doesn't expire while a player is making a power selection.</summary>
    public void PauseTurnTimerForPower()
    {
        if (activeTurnTimerCoroutine != null)
        {
            StopCoroutine(activeTurnTimerCoroutine);
            activeTurnTimerCoroutine = null;
            
        }
    }

    /// <summary>Starts a separate timeout coroutine for interactive power selection. If it expires the power is cancelled and the turn timer resumes.</summary>
    public void StartPowerDurationTimer()
    {
        if (activePowerDurationCoroutine != null)
        {
            StopCoroutine(activePowerDurationCoroutine);
            activePowerDurationCoroutine = null;
        }
        activePowerDurationCoroutine = StartCoroutine(PowerDurationTimerCoroutine(currentPlayer));
        
    }

    /// <summary>Stops the power duration timer (called when the power is completed successfully) and restarts the turn timer after a short delay for animation.</summary>
    public void StopPowerDurationTimerAndResumeTurn()
    {
        if (activePowerDurationCoroutine != null)
        {
            StopCoroutine(activePowerDurationCoroutine);
            activePowerDurationCoroutine = null;
            
        }
        // Resume turn timer after a short delay to let the swap animation finish on clients
        StartCoroutine(ResumeTurnTimerAfterDelay(currentPlayer, 1.5f));
    }

    private IEnumerator PowerDurationTimerCoroutine(int forPlayer)
    {
        yield return new WaitForSeconds(powerDurationTime);
        if (currentPlayer != forPlayer) { activePowerDurationCoroutine = null; yield break; }

        
        activePowerDurationCoroutine = null;

        // Tell all clients to cancel the power
        if (networkRelay != null)
            networkRelay.CancelPowerSelectionClientRPC();

        // Resume the turn timer immediately so the player can still play a card
        if (activeTurnTimerCoroutine != null) StopCoroutine(activeTurnTimerCoroutine);
        activeTurnTimerCoroutine = StartCoroutine(TurnTimerCoroutine(currentPlayer));
    }

    private IEnumerator ResumeTurnTimerAfterDelay(int forPlayer, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (currentPlayer != forPlayer) yield break;
        if (isActiveHost)
        {
            if (activeTurnTimerCoroutine != null) StopCoroutine(activeTurnTimerCoroutine);
            activeTurnTimerCoroutine = StartCoroutine(TurnTimerCoroutine(currentPlayer));
            
        }
    }

    // --- End Power Duration Timer Methods ---

    private IEnumerator TurnTimerCoroutine(int forPlayer)
    {
        yield return new WaitForSeconds(turnTime);
        if (currentPlayer != forPlayer) yield break;

        if (playersHandCardsIDs != null && playersHandCardsIDs.ContainsKey(forPlayer) && playersHandCardsIDs[forPlayer].Count > 0)
        {
            string autoCard = playersHandCardsIDs[forPlayer][0];
            
            GetMove(autoCard, new SerializableCard(new Dictionary<string, int[]>()), forPlayer, 0);
        }
        else
        {
            
            StartCoroutine(PostMoveSequence());
        }
    }

    //Called at the end of each turn — now only used for external callers; normal flow uses PostMoveSequence
    public void EndTurn()
    {
        StartCoroutine(PostMoveSequence());
    }

    private void NextTurn()
    {
        // REDO SYSTEM: Save current state before advancing turn
        SaveCurrentGameStateForRedo();
        
        int oldTurnCounter = turnCounter;
        turnCounter++;

        

        currentPlayer = (currentPlayer + 1) % playerCount;

        networkRelay.UpdateCurrentPlayerClientRPC(currentPlayer, turnCounter);
        PersistServerTruthsToSession();

        // Bot system: Check if it's the bot's turn
        bool isBotTurn = IsBotTurn();
        if (isBotTurn)
        {
            if (isActiveHost)
            {
                
                if (GetBotPlayer() != null)
                {
                    GetBotPlayer().OnBotTurn(currentPlayer);
                }
            }
        }
        else
        {
            // Start turn timer for human players
            if (isActiveHost)
            {
                if (activeTurnTimerCoroutine != null) StopCoroutine(activeTurnTimerCoroutine);
                activeTurnTimerCoroutine = StartCoroutine(TurnTimerCoroutine(currentPlayer));
            }
        }
}

    private void GivePlayerCount()
    {
        networkRelay.GivePlayerCountClientRPC(playerCount);
    }

    public void SkipTurn()
    {
        networkRelay.SkipTurnClientRPC();
    }

    [ContextMenu("DecideWinner")]
    private void DecideWinner()
    {
        AddRemainingCardsToPlayerPool();
        networkRelay.AddRemainingCardsToPoolClientRPC(lastPlayerToCapture);
        roundCount++;
        string roundOverText = "";

        // Variables to track rule comparisons
        int maxCardCount = 0;
        List<int> playerWithMostCards = new List<int>();

        Dictionary<int, List<string>> pooledCards;

        if (playerCount == 4)
        {
            // Combine card pools for teams
            pooledCards = new Dictionary<int, List<string>>
            {
                { 0, playersPooledCardsIDs[0].Concat(playersPooledCardsIDs[2]).ToList() },
                { 1, playersPooledCardsIDs[1].Concat(playersPooledCardsIDs[3]).ToList() }
            };
        }
        else
        {
            pooledCards = playersPooledCardsIDs;
        }

        foreach (var kvp in pooledCards)
        {
            int playerID = kvp.Key;
            List<string> cardList = kvp.Value;
            
            foreach (var card in cardList)
            {
                int[] cardID = allCardLookup[card];
                
            }
        }



        // Iterate through each player's or team's pooled cards
        foreach (var kvp in pooledCards)
        {
            int playerID = kvp.Key;
            List<string> cardList = kvp.Value;

            // Rule 1: Count cards
            int cardCount = cardList.Count;
            if (cardCount >= maxCardCount)
            {
                if (cardCount == maxCardCount)
                {
                    playerWithMostCards.Add(playerID);
                }
                else
                {
                    playerWithMostCards.Clear();
                    playerWithMostCards.Add(playerID);
                    maxCardCount = cardCount;
                }
            }

            List<string> controlCardList = new List<string>();
            // Calculate points based on card values
            foreach (var card in cardList)
            {
                if (controlCardList.Contains(card))
                {
                    
                    continue; // Skip duplicate cards
                }
                else
                {
                    controlCardList.Add(card);
                }

                int[] cardID;
                if (allCardLookup.ContainsKey(card) && (allCardLookup[card][1] == 11 || allCardLookup[card][1] == 0))
                {
                    // Directly use modified card value (Kapkaç or Yandım Anam)
                    cardID = allCardLookup[card];
                }
                else if (copiedCardMap.ContainsKey(card))
                {
                    cardID = allCardLookup[copiedCardMap[card]];
                }
                else
                {
                    cardID = allCardLookup[card];
                }
                int kind = cardID[0];
                int value = cardID[1];

                if (value == 1) // Ace
                {
                    
                    points[playerID]++;
                }
                else if (value == 11) // Jack
                {
                    
                    points[playerID]++;
                }
                else if (kind == 1 && value == 2) // 2 of Clubs
                {
                    
                    points[playerID] += 2;
                }
                else if (kind == 2 && value == 10) // 10 of Diamonds
                {
                    
                    points[playerID] += 3;
                }
            }
        }

        // Add 3-point bonus for most cards
        if (playerWithMostCards.Count == 1)
        {
            
            points[playerWithMostCards[0]] += 3;
        }

        // Collect all pooled cards across all players
        var allPooledCards = pooledCards.SelectMany(kvp => kvp.Value).ToList();
        var duplicateCards = allPooledCards.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

        // Determine the winner (max points)
        int maxPoints = -1;
        List<int> winnerIDs = new List<int>();

        for (int i = 0; i < 2; i++)
        {
            if (points[i] >= maxPoints)
            {
                if (points[i] == maxPoints)
                {
                    winnerIDs.Add(i);
                }
                else
                {
                    winnerIDs.Clear();
                    winnerIDs.Add(i);
                    maxPoints = points[i];
                }
            }
            if (playerCount == 4)
            {
                roundOverText += $"Team {i + 1} has {points[i]} points!";
                roundOverText += "\n";
            }
            else
            {
                roundOverText += $"Player {i + 1} has {points[i]} points!";
                roundOverText += "\n";
            }
        }
        roundOverText += "\n";

        int winnerSide = -1;

        if (points[0] >= 100 || points[1] >= 100)
        {
            if (playerCount == 4)
            {
                if (points[0] == points[1])
                {
                    roundOverText += $"Both teams win!!";
                    winnerSide = 3;
                }
                else
                {
                    roundOverText += $"Team {winnerIDs[0] + 1} wins";
                    winnerSide = winnerIDs[0];
                }
            }
            else
            {
                if (points[0] == points[1])
                {
                    roundOverText += $"Both players win!!";
                    winnerSide = 3;
                }
                else
                {
                    roundOverText += $"Player {winnerIDs[0] + 1} wins";
                    winnerSide = winnerIDs[0];
                }
            }
        }
        else
        {
            roundOverText += $"\n\nWaiting for another round to start";
        }

        

        // Push final round-calculated scores (captured-card scoring) to always-visible HUD.
        BroadcastLiveScoreUpdate();

        networkRelay.PrintPlayerPoolsClientRPC(new SerializableDictionary(playersPooledCardsIDs), 5);

        SendWinScreen(roundOverText, winnerSide, points[0], points[1]);

        if (winnerSide == -1)
        {
            Invoke("StartGameAutomatic", 10f);
        }
        else
        {
            // Match fully ended
        }
    }

    private void StartGameAutomatic()
    {
        if (timer >= 11)
        {
            StartGame(playerCount);
            
        }

    }

    private void SendWinScreen(string message, int winnerSide, int point0, int point1)
    {
        networkRelay.ShowWinScreenClientRPC(message, winnerSide, point0, point1);
    }

    //Add remaining cards in the center to the pool of the player who last captured a card.
    public void AddRemainingCardsToPlayerPool()
    {
        if (endTestFlag) return;
        if (centerCardsDict == null) return;
        foreach (var kvp in centerCardsDict)
        {
            
            if (playersPooledCardsIDs[lastPlayerToCapture].Contains(kvp.Key))
                
            playersPooledCardsIDs[lastPlayerToCapture].Add(kvp.Key);
        }
    }

    public void PrintCenterCards()
    {
        print("CenterCards:");
        if (centerCardsDict != null)
        {
            foreach (var kvp in centerCardsDict)
            {
                print($"{kvp.Key}: {kvp.Value[0]}_{kvp.Value[1]}");
            }
        }
    }

    public void PlayerPişti(int playerID, bool jPiştiFlag)
    {
        
        if (playerCount == 2)
        {
            if (jPiştiFlag) points[playerID] += 20;
            else points[playerID] += 10;
            piştiCounts[playerID]++;
        }

        if (playerCount == 4)
        {
            if (playerID == 0 || playerID == 2)
            {
                if (jPiştiFlag) points[0] += 20;
                else points[0] += 10;
                piştiCounts[0]++;
            }
            else if (playerID == 1 || playerID == 3)
            {
                if (jPiştiFlag) points[1] += 20;
                else points[1] += 10;
                piştiCounts[1]++;
            }
        }

        networkRelay.ShowPistiTextClientRPC(playerID, jPiştiFlag);
        BroadcastLiveScoreUpdate();
    }

    public void RemoveCardsFromCenter(SerializableCard serializableCard)
    {
        var cardsToRemove = serializableCard.ToDictionary();
        foreach (var key in cardsToRemove.Keys)
        {
            centerCardsDict.Remove(key);
        }
        networkRelay.UpdateCenterCardIDListClientRPC(new SerializableCard(centerCardsDict));
    }

    public void PrintMessage(string message)
    {
        print(message);
    }

    public static void PrintDictionary(Dictionary<int, List<int[]>> dictionary)
    {
        if (dictionary == null || dictionary.Count == 0)
        {
            
            return;
        }

        foreach (var kvp in dictionary)
        {
            
            
            foreach (var array in kvp.Value)
            {
                string arrayContents = string.Join(", ", array);
                
            }
        }
    }

    public static void PrintList(List<int[]> list)
    {
        if (list == null || list.Count == 0)
        {
            //
            return;
        }
        int counter = 0;
        foreach (var array in list)
        {
            // 
            //
            counter++;
        }
    }

    public void GetMove(string selectedHandCardUniqueID, SerializableCard serializableCard, int playerNumber, int sumValue)
    {
        

        if (isProcessingMove)
        {
            
            return;
        }

        // VALIDATION: Only accept moves from the current player
        if (playerNumber != currentPlayer)
        {
            
            return;
        }
        
        
        
        
        
        if (selectedHandCardUniqueID == null)
        {
            
            return;
        }
        
        if (!allCardLookup.ContainsKey(selectedHandCardUniqueID))
        {
            
            
            return;
        }
        
        // REDO SYSTEM: Save state before processing move
        SaveCurrentGameStateForRedo();
        isProcessingMove = true;
        
        // Track the initial pending states before any processing clears them
        bool wasOynayamazsinPending = oynayamazsinPending;
        bool wasVerZehriPending = verZehriPending;
        bool wasKutsalDestePending = kutsalDestePending;
        
        // === MOVE CHAIN TRACKING ===
        // Initialize variables needed for both hybrid power activation and regular card play
        int[] selectedHandCard = allCardLookup[selectedHandCardUniqueID];
        
        
        string[] capturedCardIds = new string[0];
        var dict = serializableCard.ToDictionary();
        if (dict != null && dict.Count > 0)
        {
            capturedCardIds = dict.Keys.ToArray();
        }
        
        // ACTIVATE PENDING DURATION POWERS WHEN CARD IS PLAYED (New Logic)
        if (oynayamazsinPending)
        {
            blockCount = 1;
            oynayamazsinPending = false;
            
            // CRITICAL: Record BOTH the power activation AND the card play together as ONE move
            MoveChainIntegrator.TrackHybridPowerTrueActivation(oynayamazsinActivatedBy, "Oynayamazsın", selectedHandCardUniqueID, selectedHandCard, capturedCardIds, sumValue);
            
            networkRelay.SetOynayamazsinActiveClientRPC(true); // Show block on all clients
            oynayamazsinActivatedBy = -1; // Reset
            
        }
        
        if (verZehriPending)
        {
            verZehriPending = false;
            
            // CRITICAL: Record BOTH the power activation AND the card play together as ONE move
            MoveChainIntegrator.TrackHybridPowerTrueActivation(verZehriActivatedBy, "Ver Zehri", selectedHandCardUniqueID, selectedHandCard, capturedCardIds, sumValue);
            verZehriActivatedBy = -1; // Reset
            
        }
        
        if (kutsalDestePending)
        {
            kutsalDestePending = false;
            
            // CRITICAL: Record BOTH the power activation AND the card play together as ONE move
            MoveChainIntegrator.TrackHybridPowerTrueActivation(kutsalDesteActivatedBy, "Kutsal Deste", selectedHandCardUniqueID, selectedHandCard, capturedCardIds, sumValue);
            kutsalDesteActivatedBy = -1; // Reset
            
        }
        
        // Oynayamazsın: force this card to be blocked (add to center, no capture)
        if (blockCount > 0)
        {
            
            sumValue = 0;
            blockCount = 0;
            networkRelay.SetOynayamazsinActiveClientRPC(false);
        }

        // Get the cardID for rules
        
        
        
        if (selectedHandCard[1] == sumValue || (selectedHandCard[1] == 11 && sumValue != 0))
        {
            // This is a capture move
            // NOTE: Card play is already recorded by TrackHybridPowerTrueActivation if a hybrid power was pending
            // Only record here if NO hybrid power was pending
            if (!wasOynayamazsinPending && !wasVerZehriPending && !wasKutsalDestePending)
            {
                MoveChainIntegrator.TrackServerCardPlay(playerNumber, selectedHandCardUniqueID, selectedHandCard, capturedCardIds, sumValue);
            }
            
            int centerCardCount = centerCardsDict.Count + 1;
            RemoveCardsFromCenter(serializableCard);
            // Add the played card to the serializableCard for pool addition
            var updatedDict = serializableCard.ToDictionary();
            updatedDict[selectedHandCardUniqueID] = selectedHandCard;
            AddDiscardedCardsToPlayerPool(new SerializableCard(updatedDict), playerNumber);
            updatedDict.Remove(selectedHandCardUniqueID); // Remove again if needed
            networkRelay.SendMoveToClientRPC(selectedHandCardUniqueID, new SerializableCard(updatedDict), playerNumber);
            lastPlayerToCapture = playerNumber;

            // CRITICAL FIX: Remove the played card from the server's hand tracking
            if (playersHandCardsIDs != null && playersHandCardsIDs.ContainsKey(playerNumber))
            {
                bool removed = playersHandCardsIDs[playerNumber].Remove(selectedHandCardUniqueID);
                
            }

            if (verZehriActive)
            {
                int team = (playerNumber % 2);
                points[team] -= centerCardCount;
                
                networkRelay.ShowVerZehriEffectClientRPC(playerNumber, -5);
                verZehriActive = false;
                networkRelay.SetVerZehriActiveClientRPC(false); // Notify clients to stop effect
                BroadcastLiveScoreUpdate();
            }
            if (kutsalDesteActive)
            {
                int team = (playerNumber % 2);
                points[team] += centerCardCount;
                
                networkRelay.ShowKutsalDesteEffectClientRPC(playerNumber, 5);
                kutsalDesteActive = false;
                networkRelay.SetKutsalDesteActiveClientRPC(false); // Notify clients to stop effect
                BroadcastLiveScoreUpdate();
            }
        }
        else
        {
            // This is a play to center move
            // NOTE: Card play is already recorded by TrackHybridPowerTrueActivation if a hybrid power was pending
            // Only record here if NO hybrid power was pending
            if (!wasOynayamazsinPending && !wasVerZehriPending && !wasKutsalDestePending)
            {
                MoveChainIntegrator.TrackServerCardPlay(playerNumber, selectedHandCardUniqueID, selectedHandCard, new string[0], sumValue);
            }
            
            AddCardIDToCenter(selectedHandCardUniqueID, selectedHandCard);
            
            // CRITICAL FIX: Remove the played card from the server's hand tracking
            if (playersHandCardsIDs != null && playersHandCardsIDs.ContainsKey(playerNumber))
            {
                bool removed = playersHandCardsIDs[playerNumber].Remove(selectedHandCardUniqueID);
                
            }
        }

        // Truly activate the newly triggered hybrid powers for the NEXT turn
        if (wasVerZehriPending)
        {
            verZehriActive = true;
            networkRelay.SetVerZehriActiveClientRPC(true); // Notify clients to start effect
            
        }
        
        if (wasKutsalDestePending)
        {
            kutsalDesteActive = true;
            networkRelay.SetKutsalDesteActiveClientRPC(true); // Notify clients to start effect
            
        }

        // Cancel any running turn timer (valid move received)
        if (activeTurnTimerCoroutine != null)
        {
            StopCoroutine(activeTurnTimerCoroutine);
            activeTurnTimerCoroutine = null;
        }
        // Begin waiting for all clients to confirm they processed this move
        StartCoroutine(PostMoveSequence());
    }

    public void AddCardIDToCenter(string uniqueID, int[] cardID)
    {
        
        centerCardsDict[uniqueID] = cardID;

        int owner = FindOwnerOfCard(uniqueID);
        if (owner != -1 && playersHandCardsIDs != null && playersHandCardsIDs.ContainsKey(owner))
        {
            playersHandCardsIDs[owner].Remove(uniqueID);
            
        }

        networkRelay.UpdateCenterCardIDListClientRPC(new SerializableCard(centerCardsDict));
        networkRelay.SendCardAddedToCenterClientRPC(uniqueID, cardID);
        //EndTurn();
    }

    public int SendPlayerNumber()
    {
        return NetworkManager.Singleton.ConnectedClients.Count - 1;
    }

    public void HandleFreshJoin(ulong clientId)
    {
        
        
        // CRITICAL FIX: Check if game has already started
        // If so, this is a reconnection, not a fresh join — route to reconnection logic instead
        bool gameHasStarted = (deckCardsDict != null || playersHandCardsIDs != null || centerCardsDict != null);
        if (gameHasStarted)
        {
            
            HandleReconnectJoin(clientId);
            return;
        }
        
        // Guard: Check if this client already has an assigned seat (e.g., bound during host migration self-bind)
        int existingSeat = GetPlayerNoForClient(clientId);
        if (existingSeat != -1)
        {
            
            
            // Still register the connection and count them so the server knows they are connected
            if (!countedClients.Contains(clientId))
            {
                countedClients.Add(clientId);
                connectedPlayerCount++;
            }
            return;
        }

        // Only assign player number to new players
        networkRelay.GetPlayerNumberClientRPC(clientId, connectedPlayerCount);
        playerClientIds[connectedPlayerCount] = clientId;
        
        
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            PlayerPrefs.SetInt("SavedPlayerSeat", connectedPlayerCount);
            PlayerPrefs.Save();
            
        }
        
        if (!countedClients.Contains(clientId))
        {
            countedClients.Add(clientId);
            connectedPlayerCount++;
        }
        
        // RECONNECTION FIX: Guard against starting fresh game if ANY clients are reconnecting
        if ((playerCount == connectedPlayerCount || (isBotModeEnabled && playerCount == 2 && connectedPlayerCount == 1) || (isBotModeEnabled && playerCount == 4 && connectedPlayerCount == 1)) && reconnectingClients.Count == 0)
        {
            if (playerCount == 2) StartGameAfterDelayTwoPlayer();
            else if (playerCount == 4) StartGameAfterDelayFourPlayer();
        }
    }

    public void HandleReconnectJoin(ulong clientId)
    {
        // IDEMPOTENCY GUARD: If this client is already in reconnection handshake, skip
        if (clientReconnectPhases.ContainsKey(clientId) && clientReconnectPhases[clientId] != ReconnectPhase.None)
        {
            
            return;
        }

        
        
        // RECONNECTION: Track this client as reconnecting
        reconnectingClients.Add(clientId);
        clientReconnectPhases[clientId] = ReconnectPhase.InitSent;
        reconnectStartTimes[clientId] = Time.time;
        
        // Clear pending confirmations to avoid blocking
        pendingTurnConfirmations.Remove(clientId);
        dealCenterFinishedClients.Remove(clientId);
        dealHandsFinishedClients.Remove(clientId);
        
        // BOT PLACEHOLDER SYSTEM: If a player reconnects, they are no longer bot-controlled
        int reconnectedPlayerNo = GetPlayerNoForClient(clientId);
        if (reconnectedPlayerNo != -1 && botControlledPlayers.Contains(reconnectedPlayerNo))
        {
            RemoveBotFromSeat(reconnectedPlayerNo);
            
        }

        // Count the client for this server instance
        if (!countedClients.Contains(clientId))
        {
            countedClients.Add(clientId);
            connectedPlayerCount++;
        }

        // Initialize reconnected client
        networkRelay.GivePlayerCountForReconnectedClientClientRPC(playerCount, clientId);
        networkRelay.InitializeCardPrefabsForReconnectedClientClientRPC(true, clientId);
        
        
    }

    public void HandleMigratedHostSelfBind(int playerNo, ulong clientId)
    {
        

        // 1. Sweep & Unbind: Remove stale ClientID mappings to avoid duplicate ID collisions
        List<int> seatsWithSameClientId = new List<int>();
        foreach (var kvp in playerClientIds)
        {
            if (kvp.Value == clientId && kvp.Key != playerNo)
            {
                seatsWithSameClientId.Add(kvp.Key);
            }
        }
        foreach (int seat in seatsWithSameClientId)
        {
            playerClientIds.Remove(seat);
            
        }

        // 2. Clear Target Seat: Remove old network bindings from our target slot
        if (playerClientIds.ContainsKey(playerNo))
        {
            playerClientIds.Remove(playerNo);
            
        }

        // 3. Bind: Rebind the new host client ID to their logical seat
        
        RebindPlayerClientId(playerNo, clientId);
        
        // Assert client mapping consistency
        if (playerClientIds.Values.Distinct().Count() != playerClientIds.Count)
        {
            
        }

        // Host is never a bot, remove bot control to be safe:
        RemoveBotFromSeat(playerNo);

        if (!countedClients.Contains(clientId))
        {
            countedClients.Add(clientId);
            connectedPlayerCount++;
        }
        
        // Persist updated seat map to session so clients can find the new host info
        Dictionary<int, SeatOwnerInfo> seatMap = LoadSeatMapFromSession();
        if (seatMap.ContainsKey(playerNo))
        {
            var oldInfo = seatMap[playerNo];
            seatMap[playerNo] = new SeatOwnerInfo(oldInfo.OwnerPlayerId, clientId);
            PersistSeatMapToSessionAsync(seatMap);
        }
    }

    /// <summary>
    /// Clears all session-scoped reconnect/seat tracking when the local user leaves a game
    /// (return to main menu, or after the match ends). This guarantees the NEXT session
    /// starts from the clean fresh-join path instead of being misdetected as a reconnect.
    /// </summary>
    public void ResetSessionStateForTeardown()
    {
        
        reconnectingClients.Clear();
        clientReconnectPhases.Clear();
        reconnectStartTimes.Clear();
        countedClients.Clear();
        disconnectedClients.Clear();
        playerClientIds.Clear();
        playerGolds.Clear();
        playerSuperPowers.Clear();
        connectedPlayerCount = 0;
        PlayerPrefs.DeleteKey("HostMigrated");
        PlayerPrefs.Save();
    }

    private IEnumerator CleanupStaleReconnections()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);
            
            List<ulong> staleClients = new List<ulong>();
            foreach (var kvp in clientReconnectPhases)
            {
                // A client stuck before completing the handshake past the timeout is stale
                // (e.g. it dropped again mid-reconnect). Clearing it prevents the InitialDeal
                // guard (reconnectingClients.Count > 0) from blocking the rest of the table.
                if (kvp.Value == ReconnectPhase.Completed) continue;

                if (reconnectStartTimes.TryGetValue(kvp.Key, out float startTime))
                {
                    if (Time.time - startTime > RECONNECT_TIMEOUT)
                        staleClients.Add(kvp.Key);
                }
                else
                {
                    // No start time tracked, treat as stale to avoid blocking future joins.
                    staleClients.Add(kvp.Key);
                }
            }
            
            foreach (ulong id in staleClients)
            {
                
                int staleSeat = GetPlayerNoForClient(id);
                if (staleSeat != -1)
                {
                    AssignBotToSeat(staleSeat);
                    
                }
                reconnectingClients.Remove(id);
                clientReconnectPhases.Remove(id);
                reconnectStartTimes.Remove(id);
            }
        }
    }

    public void AnotherPlayerConnected(ulong clientId)
{
        Debug.Log($"[Server] ===== ÖNEMLİ: ANOTHER PLAYER CONNECTED METHOD CALLED =====\n" +
                 $"ClientId: {clientId}\n" +
                 $"IsServer: {IsServer}\n" +
                 $"TurnCounter: {turnCounter}\n" +
                 $"ConnectedPlayerCount: {connectedPlayerCount}\n" +
                 $"PlayerCount: {playerCount}");
        
        // Build complete connection log as one string
        var connectionLog = new System.Text.StringBuilder();
        connectionLog.AppendLine("SERVER MESSAGE: ===== CLIENT CONNECTION DETECTED =====");
        connectionLog.AppendLine("SERVER MESSAGE: A CLIENT HAS CONNECTED!");
        connectionLog.AppendLine($"SERVER MESSAGE: Connected Client ID: {clientId}");
        connectionLog.AppendLine($"SERVER MESSAGE: Previous connected player count: {connectedPlayerCount}");
        connectionLog.AppendLine($"SERVER MESSAGE: NetworkManager connected clients: {NetworkManager.Singleton.ConnectedClients.Count}");
        connectionLog.AppendLine($"SERVER MESSAGE: Is Host: {NetworkManager.Singleton.IsHost}");
        connectionLog.AppendLine($"SERVER MESSAGE: Is Server: {NetworkManager.Singleton.IsServer}");
        
        // IDEMPOTENCY: If this client is already tracked (mid-reconnect), skip duplicate notification
        // Note: We don't skip if they are in playerClientIds because we need to count them for this session instance
        if (reconnectingClients.Contains(clientId))
        {
            
            return;
        }

        bool isMigrating = networkManagerUI != null && networkManagerUI.IsMigrating;

        // If this is the local host client spawning during a migration,
        // completely skip new-player setup and client-RPC handshakes!
        if (isMigrating && clientId == NetworkManager.Singleton.LocalClientId)
        {
            
            if (!countedClients.Contains(clientId))
            {
                countedClients.Add(clientId);
                connectedPlayerCount++;
            }
            return; // Exit early!
        }
        
        // RECONNECTION DETECTION: Game is "in progress" if cards have been set up.
        bool gameHasStarted = (deckCardsDict != null || playersHandCardsIDs != null || centerCardsDict != null);
        bool isReconnection = gameHasStarted; // FIXED: Host is also a reconnection during migration
        
        // Only assign player number to new players, not reconnecting ones
        // Reconnecting players will restore their player number from PlayerPrefs (handled by re-binding or sync)
        if (!isReconnection)
        {
            networkRelay.GetPlayerNumberClientRPC(clientId, connectedPlayerCount);
            playerClientIds[connectedPlayerCount] = clientId;
            
            
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                PlayerPrefs.SetInt("SavedPlayerSeat", connectedPlayerCount);
                PlayerPrefs.Save();
                
            }
        }
        else
        {
            
        }
        
        // SESSION COUNTING: Increment connectedPlayerCount only once per client for this server instance
        if (!countedClients.Contains(clientId))
        {
            countedClients.Add(clientId);
            connectedPlayerCount++;
            
        }
        
        // BOT PLACEHOLDER SYSTEM: If a player reconnects, they are no longer bot-controlled
        int reconnectedPlayerNo = GetPlayerNoForClient(clientId);
        if (reconnectedPlayerNo != -1)
        {
            if (botControlledPlayers.Contains(reconnectedPlayerNo))
            {
                botControlledPlayers.Remove(reconnectedPlayerNo);
                
            }
        }
        else
        {
            
        }
        
        connectionLog.AppendLine($"SERVER MESSAGE: New connected player count: {connectedPlayerCount}");
        connectionLog.AppendLine($"SERVER MESSAGE: Expected player count: {playerCount}");
        connectionLog.AppendLine($"SERVER MESSAGE: Turn counter: {turnCounter}");

        // Debug.Log($"[Server] ===== TURN COUNTER CHECK =====\n" +
        //          $"ClientId: {clientId}\n" +
        //          $"TurnCounter: {turnCounter}\n" +
        //          $"IsReconnection: {isReconnection}\n" +
        //          $"ConnectedPlayerCount: {connectedPlayerCount}\n" +
        //          $"PlayerCount: {playerCount}\n" +
        //          $"RoundCount: {roundCount}");
        
        if (isReconnection)
        {
            
            connectionLog.AppendLine($"SERVER MESSAGE: Client {clientId} reconnected to existing game (turn {turnCounter})");
            
            // UNIFIED FLOW: Delegate to master function
            
            HandleReconnectJoin(clientId);
            
            // Early return: skip fresh-join logic below
            connectionLog.AppendLine("SERVER MESSAGE: ===== END CLIENT CONNECTION LOG =====");
            
            return;
        }
        else if ((playerCount == connectedPlayerCount && !isReconnection) || (isBotModeEnabled && playerCount == 2 && connectedPlayerCount == 1) || (isBotModeEnabled && playerCount == 4 && connectedPlayerCount == 1))
        {
            
            // This is a fresh game start
            // Normal case: All players connected
            // Bot case 1v1: 1 human player + bot mode enabled
            // Bot case 2v2: 1 human player + bot mode enabled (bots play as players 1, 2, 3)
            
            if (isBotModeEnabled && playerCount == 2 && connectedPlayerCount == 1)
            {
                connectionLog.AppendLine($"SERVER MESSAGE: Bot mode enabled - starting 1v1 game with bot");
            }
            else if (isBotModeEnabled && playerCount == 4 && connectedPlayerCount == 1)
            {
                connectionLog.AppendLine($"SERVER MESSAGE: Bot mode enabled - starting 2v2 game with 3 bots");
            }
            else
            {
                connectionLog.AppendLine($"SERVER MESSAGE: All players connected - starting new game");
            }
            
            if (playerCount == 2)
            {
                StartGameAfterDelayTwoPlayer();
            }
            else if (playerCount == 4)
            {
                StartGameAfterDelayFourPlayer();
            }
        }
        
        connectionLog.AppendLine("SERVER MESSAGE: ===== END CLIENT CONNECTION LOG =====");
        
        // Print as one log entry
        
    }

    /// <summary>
    /// Handles when a client disconnects from the server
    /// </summary>
    public void OnClientDisconnected(ulong clientId)
    {
        // CRITICAL FIX: Prevent double-handling of the same disconnection
        if (disconnectedClients.ContainsKey(clientId) && disconnectedClients[clientId])
        {
            
            return;
        }
        
        // CRITICAL FIX: Check if this is a host disconnection - ALWAYS reset server when host disconnects
        if (clientId == NetworkManager.ServerClientId)
        {
            
            HandleHostDisconnection();
            return;
        }
        
        // CRITICAL FIX: Check if this is a pre-game disconnection (client only)
        bool isPreGameDisconnection = IsPreGameDisconnection();
        if (isPreGameDisconnection)
        {
            
            
            // Client disconnected - just remove them, don't reset server state
            // Host should continue waiting for more players
            
            HandleClientPreGameDisconnection(clientId);
            return;
        }
        
        // Build complete disconnection log as one string
        var disconnectionLog = new System.Text.StringBuilder();
        disconnectionLog.AppendLine("SERVER MESSAGE: ===== CLIENT DISCONNECTION DETECTED =====");
        disconnectionLog.AppendLine("SERVER MESSAGE: A CLIENT HAS DISCONNECTED!");
        disconnectionLog.AppendLine($"SERVER MESSAGE: Disconnected Client ID: {clientId}");
        disconnectionLog.AppendLine($"SERVER MESSAGE: BEFORE - Connected player count: {connectedPlayerCount}");
        
        // CRITICAL FIX: Only access ConnectedClients if we're actually the server
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            disconnectionLog.AppendLine($"SERVER MESSAGE: NetworkManager connected clients: {NetworkManager.Singleton.ConnectedClients.Count}");
            disconnectionLog.AppendLine($"SERVER MESSAGE: Is Host: {NetworkManager.Singleton.IsHost}");
            disconnectionLog.AppendLine($"SERVER MESSAGE: Is Server: {NetworkManager.Singleton.IsServer}");
        }
        else
        {
            disconnectionLog.AppendLine($"SERVER MESSAGE: NetworkManager not available or not server");
        }
        
        // Mark client as disconnected FIRST (to prevent double processing)
        disconnectedClients[clientId] = true;

        // CONFIRMATION SYSTEM: Remove disconnected client so waiting coroutines can proceed
        pendingTurnConfirmations.Remove(clientId);
        dealCenterFinishedClients.Remove(clientId);
        dealHandsFinishedClients.Remove(clientId);
        
        // REDO SYSTEM: Remove disconnected client from redo confirmation tracking
        if (isWaitingForRedoConfirmations)
        {
            redoSceneReconstructionClients.Remove(clientId);
            
            
            // Re-check if all remaining clients are ready
            CheckAllClientsReadyForRedoCurrentPlayerUpdate();
        }
        
        // Decrease the connected player count (only once per client)
        if (connectedPlayerCount > 0) // Always decrement, but never go below 0
        {
            connectedPlayerCount--;
            disconnectionLog.AppendLine($"SERVER MESSAGE: AFTER - Connected player count decreased to: {connectedPlayerCount}");
        }
        else
        {
            disconnectionLog.AppendLine($"SERVER MESSAGE: Connected player count was already 0 - no change");
        }
        
        // CRITICAL FIX: Do NOT reset game state when all clients disconnect
        // connectedPlayerCount == 1 means only host is left (no clients)
        if (connectedPlayerCount == 1)
        {
            disconnectionLog.AppendLine("SERVER MESSAGE: All players disconnected");
            disconnectionLog.AppendLine("SERVER MESSAGE: NOT resetting game state");
            // DO NOT call ResetAllServerVariables() here
        }
        
        // Reset only the essential connection tracking (don't reset game state)
        dealCenterFinishedClients.Remove(clientId); // Remove only this client
        // Don't reset initialDealCoroutineCheckCounter or readyToEndTurnCounter
        // These should maintain their state for the remaining players
        
        disconnectionLog.AppendLine("SERVER MESSAGE: ===== END CLIENT DISCONNECTION LOG =====");
        
        // TODO: Implement bot placeholder system here
        disconnectionLog.AppendLine($"SERVER MESSAGE: Client {clientId} disconnected - bot placeholder system should activate");
        
        int playerNo = GetPlayerNoForClient(clientId);
        if (playerNo != -1)
        {
            AssignBotToSeat(playerNo);
            disconnectionLog.AppendLine($"SERVER MESSAGE: Player {playerNo} is now bot-controlled");
        }
        
        // Print as one log entry
        
    }

    /// <summary>
    /// Context menu method to manually reset connection count (for testing)
    /// </summary>
    [ContextMenu("Reset Connection Count")]
    public void ResetConnectionCount()
    {
        
        connectedPlayerCount = 0; // Host will make it 1 when they start
        dealCenterFinishedClients.Clear();
        pendingTurnConfirmations.Clear();
        dealHandsFinishedClients.Clear();
        initialDealCoroutineCheckCounter = 0;
        
    }

    /// <summary>
    /// Completely resets the Server singleton for fresh game start
    /// This should be called when returning to main menu to ensure clean state
    /// </summary>
    public static void ResetServerSingletonForMainMenu()
    {
        if (Singleton != null)
        {
            
            
            
            
            
            // Perform complete reset
            Singleton.ResetAllServerVariables();
            
            // Clear all tracking dictionaries
            Singleton.disconnectedClients.Clear();
            Singleton.reconnectingClients.Clear();
            
            // Reset connection count to 0
            Singleton.connectedPlayerCount = 0;
            
            
            
        }
        else
        {
            
        }
    }

    /// <summary>
    /// Determines if this is a disconnection before the game has started
    /// </summary>
    private bool IsPreGameDisconnection()
    {
        // Game hasn't started if:
        // 1. No cards have been dealt (deckCardsDict is null)
        // 2. Turn counter is 0 (no turns have been taken)
        // 3. No center cards (centerCardsDict is null or empty)
        bool noCardsDealt = deckCardsDict == null;
        bool noTurnsTaken = turnCounter == 0;
        bool noCenterCards = centerCardsDict == null || centerCardsDict.Count == 0;
        
        bool isPreGame = noCardsDealt && noTurnsTaken && noCenterCards;
        
        
        
        return isPreGame;
    }

    /// <summary>
    /// Determines if the game is ready to start (all players connected)
    /// This is used to disable the return button
    /// </summary>
    public bool IsGameReadyToStart()
    {
        // Game is ready to start when:
        // 1. All required players are connected (or bot mode conditions are met)
        // 2. Game hasn't actually started yet (no cards dealt)
        bool allPlayersConnected = playerCount == connectedPlayerCount || 
                                  (isBotModeEnabled && playerCount == 2 && connectedPlayerCount == 1) ||
                                  (isBotModeEnabled && playerCount == 4 && connectedPlayerCount == 1);
        bool gameNotStarted = deckCardsDict == null && turnCounter == 0;
        
        bool isReady = allPlayersConnected && gameNotStarted;
        
        
        
        return isReady;
    }

    /// <summary>
    /// Handles disconnections that occur before the game starts
    /// Resets server state to allow fresh connections
    /// </summary>
    private void HandlePreGameDisconnection(ulong clientId)
    {
        
        
        
        
        // Mark client as disconnected
        disconnectedClients[clientId] = true;
        
        // Decrease connected player count
        if (connectedPlayerCount > 0)
        {
            connectedPlayerCount--;
            
        }
        
        // CRITICAL: Reset server state for fresh connections
        // This prevents ghost players and ensures clean state for next game
        
        
        // Reset connection tracking
        dealCenterFinishedClients.Clear();
        pendingTurnConfirmations.Clear();
        dealHandsFinishedClients.Clear();
        if (activeTurnTimerCoroutine != null) { StopCoroutine(activeTurnTimerCoroutine); activeTurnTimerCoroutine = null; }
        initialDealCoroutineCheckCounter = 0;

        // Reset game state variables
        turnCounter = 0;
        currentPlayer = 0;
        lastPlayerToCapture = -1;
        timer = 0f;
        winnerPrintFlag = false;
        
        // Clear any existing game data
        if (deckCardsDict != null) deckCardsDict.Clear();
        if (centerCardsDict != null) centerCardsDict.Clear();
        if (playersHandCardsIDs != null) playersHandCardsIDs.Clear();
        if (playersPooledCardsIDs != null) playersPooledCardsIDs.Clear();
        if (playersPiştiPoolCardsIDs != null) playersPiştiPoolCardsIDs.Clear();
        
        // Reset bot system if active
        if (botPlayerActive)
        {
            
            botPlayerActive = false;
            if (botMoveCoroutine != null)
            {
                StopCoroutine(botMoveCoroutine);
                botMoveCoroutine = null;
            }
        }
        
        // Reset move chains
        MoveChainIntegrator.ResetChains();
        
        // Clear reconnection tracking
        reconnectingClients.Clear();
        
        
        
        
    }

    /// <summary>
    /// Handles when a client disconnects before the game starts
    /// Only removes the client, keeps host waiting for more players
    /// </summary>
    private void HandleClientPreGameDisconnection(ulong clientId)
    {
        
        
        
        
        // Mark client as disconnected
        disconnectedClients[clientId] = true;
        
        // Decrease connected player count
        if (connectedPlayerCount > 0)
        {
            connectedPlayerCount--;
            
        }
        
        // Remove only this client from tracking (don't reset everything)
        dealCenterFinishedClients.Remove(clientId);
        
        // Remove from reconnection tracking if present
        reconnectingClients.Remove(clientId);
        
        
        
        
    }

    /// <summary>
    /// Handles when the host disconnects (any time - pre-game or during game)
    /// Performs complete server reset since host is gone
    /// </summary>
    private void HandleHostDisconnection()
    {
        
        // NOTE: Host migration / survivor re-host has been removed. When the host leaves,
        // each client detects the loss via NetworkManager's OnClientDisconnectCallback and
        // returns to the main menu itself (handled in NetworkManagerUI). Here we only clear
        // stale server-side connection tracking.

        // Clear stale connection tracking
        disconnectedClients.Clear();
        reconnectingClients.Clear();
        connectedPlayerCount = 0;

        
    }


    void OnDestroy()
    {
        if (Singleton == this)
        {
            Singleton = null;
        }
        
        // Unsubscribe from network events
        if (NetworkManager.Singleton != null && hasSubscribedToNetworkEvents)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnClientConnectedCallback -= AnotherPlayerConnected;
            hasSubscribedToNetworkEvents = false;
            
        }
        
    }

    /// <summary>
    /// Called when the application is paused (mobile) or loses focus
    /// This handles cases where the game is minimized or backgrounded
    /// </summary>
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && IsServer)
        {
            
            // The NetworkManager will automatically detect this as a client disconnection
            // and call OnClientDisconnected() for the appropriate client
        }
    }

    /// <summary>
    /// Called when the application is about to quit
    /// This handles cases where the game is closed completely
    /// </summary>
    void OnApplicationQuit()
    {
        if (IsServer)
        {
            
            // The NetworkManager will automatically detect this as a host disconnection
            // and call OnClientDisconnected() for the host
        }
    }
    
    public void StartGameAfterDelayFourPlayer()
    {
        Invoke("StartGameDelayedFourPlayer", 3f);
    }
    [ContextMenu("StartGameDelayedFourPlayer")]
    private void StartGameDelayedFourPlayer()
    {
        StartGame(4);
    }
    public void StartGameAfterDelayTwoPlayer()
    {
        Invoke("StartGameDelayedTwoPlayer", 3f);
    }
    [ContextMenu("StartGameDelayedTwoPlayer")]
    private void StartGameDelayedTwoPlayer()
    {
        StartGame(2);
    }

    public void SetPlayerCount(int playerCountVar)
    {
        playerCount = playerCountVar;
    }
    
    /// <summary>
    /// Gets the current player count for the game
    /// </summary>
    /// <returns>The number of players in the current game (2 for 1v1, 4 for 2v2)</returns>
    public int GetPlayerCount()
    {
        return playerCount;
    }

    [ContextMenu("PrintPlayerPools")]
    public void CallPrintPlayerPools()
    {
        networkRelay.PrintPlayerPoolsClientRPC(new SerializableDictionary(playersPooledCardsIDs), 5);
    }

    /// <summary>
    /// Swaps cards between two players in the server's hand data.
    /// </summary>
    public void SwapCardsBetweenPlayersOnServer(int playerANo, int cardAIndex, int playerBNo, int cardBIndex)
    {
        if (playersHandCardsIDs == null) return;
        if (!playersHandCardsIDs.ContainsKey(playerANo) || !playersHandCardsIDs.ContainsKey(playerBNo)) return;

        var handA = playersHandCardsIDs[playerANo];
        var handB = playersHandCardsIDs[playerBNo];

        if (handA.Count <= cardAIndex || handB.Count <= cardBIndex) return;

        // Swap the cards in the server's hand data
        string temp = handA[cardAIndex];
        handA[cardAIndex] = handB[cardBIndex];
        handB[cardBIndex] = temp;

        
        
        // Track the card swap in move chain for synchronization
        MoveChainIntegrator.TrackCardSwap(playerANo, playerBNo, temp, handA[cardAIndex], "Server-side card swap");
    }

    public void RegisterCopiedCard(string targetUniqueID, string sourceUniqueID)
    {
        copiedCardMap[targetUniqueID] = sourceUniqueID;
        
    }

    public List<string> GetPlayerHand(int playerNo)
    {
        if (playersHandCardsIDs != null && playersHandCardsIDs.ContainsKey(playerNo))
            return new List<string>(playersHandCardsIDs[playerNo]);
        return new List<string>();
    }

    public void BombaCenter()
    {
        // Move all center cards to bombed stack (outside normal game flow)
        if (centerCardsDict != null && centerCardsDict.Count > 0)
        {
            foreach (var cardId in centerCardsDict.Keys.ToList())
            {
                bombedCards.Add(cardId);
                
            }
            centerCardsDict.Clear();
        }

        // Notify all clients to update their view
        networkRelay.BombaClientRPC();
    }

    private bool isYapamazsınActive = false;
    public void ActivateYapamazsın()
    {
        isYapamazsınActive = true;
        networkRelay.SetYapamazsınActiveClientRPC(true);
    }
    public bool TryBlockPower()
    {
        if (isYapamazsınActive)
        {
            isYapamazsınActive = false;
            networkRelay.SetYapamazsınActiveClientRPC(false);
            return true; // Blocked
        }
        return false; // Not blocked
    }

    public int blockCount = 0;

    public void ActivateOynayamazsin()
    {
        oynayamazsinPending = true;
        oynayamazsinActivatedBy = currentPlayer; // Track who activated the power
    }

    private bool verZehriActive = false;
    private bool kutsalDesteActive = false;
    private bool verZehriPending = false;
    private bool kutsalDestePending = false;
    private int verZehriActivatedBy = -1;
    private int kutsalDesteActivatedBy = -1;

    public void ActivateVerZehri()
    {
        verZehriPending = true;
        verZehriActivatedBy = currentPlayer; // Track who activated the power
    }

    public void ActivateKutsalDeste()
    {
        kutsalDestePending = true;
        kutsalDesteActivatedBy = currentPlayer; // Track who activated the power
    }

    public void BuDahaIyiSwap(int playerNo, string handCardID, string centerCardID)
    {
        // Swap in player's hand
        if (playersHandCardsIDs[playerNo].Contains(handCardID))
        {
            playersHandCardsIDs[playerNo].Remove(handCardID);
            playersHandCardsIDs[playerNo].Add(centerCardID);
        }
        // Swap in center
        if (centerCardsDict.ContainsKey(centerCardID))
        {
            int[] temp = centerCardsDict[centerCardID];
            centerCardsDict.Remove(centerCardID);
            centerCardsDict[handCardID] = allCardLookup[handCardID];
        }
        else if (centerCardsDict.ContainsKey(handCardID))
        {
            int[] temp = centerCardsDict[handCardID];
            centerCardsDict.Remove(handCardID);
            centerCardsDict[centerCardID] = allCardLookup[centerCardID];
        }
        else
        {
            // Fallback: just swap the last card
            var lastKey = centerCardsDict.Keys.Last();
            int[] temp = centerCardsDict[lastKey];
            centerCardsDict.Remove(lastKey);
            centerCardsDict[handCardID] = allCardLookup[handCardID];
        }
    }

    public int FindOwnerOfCard(string cardID)
    {
        foreach (var kvp in playersHandCardsIDs)
        {
            if (kvp.Value.Contains(cardID))
                return kvp.Key;
        }
        return -1;
    }

    public bool IsCardInCenter(string cardID)
    {
        return centerCardsDict != null && centerCardsDict.ContainsKey(cardID);
    }

    public bool IsCardInPlayerHand(int playerNo, string cardID)
    {
        return playersHandCardsIDs != null &&
               playersHandCardsIDs.ContainsKey(playerNo) &&
               playersHandCardsIDs[playerNo] != null &&
               playersHandCardsIDs[playerNo].Contains(cardID);
    }

    public int GetHandCardCount(int playerNo)
    {
        if (playersHandCardsIDs == null || !playersHandCardsIDs.ContainsKey(playerNo)) return 0;
        return playersHandCardsIDs[playerNo].Count;
    }

    public List<int> GetOpponentPlayers(int myPlayerNo)
    {
        var result = new List<int>();
        if (playerCount == 2)
        {
            result.Add((myPlayerNo + 1) % 2);
            return result;
        }

        if (playerCount == 4)
        {
            if (myPlayerNo == 0 || myPlayerNo == 2) result.AddRange(new[] { 1, 3 });
            else result.AddRange(new[] { 0, 2 });
        }

        return result;
    }

    public void ExecuteRandomDegisTokus(int myPlayerNo)
    {
        if (playersHandCardsIDs == null || !playersHandCardsIDs.ContainsKey(myPlayerNo)) return;

        var myHand = playersHandCardsIDs[myPlayerNo];
        if (myHand == null || myHand.Count == 0) return;

        var validOpponents = new List<int>();
        foreach (var opp in GetOpponentPlayers(myPlayerNo))
        {
            if (playersHandCardsIDs.ContainsKey(opp) && playersHandCardsIDs[opp] != null && playersHandCardsIDs[opp].Count > 0)
                validOpponents.Add(opp);
        }

        if (validOpponents.Count == 0) return;

        int oppPlayer = validOpponents[UnityEngine.Random.Range(0, validOpponents.Count)];
        string myCard = myHand[UnityEngine.Random.Range(0, myHand.Count)];
        string oppCard = playersHandCardsIDs[oppPlayer][UnityEngine.Random.Range(0, playersHandCardsIDs[oppPlayer].Count)];

        if (!IsCardInPlayerHand(myPlayerNo, myCard)) return;
        if (!IsCardInPlayerHand(oppPlayer, oppCard)) return;
        if (IsCardInCenter(myCard) || IsCardInCenter(oppCard)) return;

        SunuDegisTokusSwap(myPlayerNo, oppPlayer, myCard, oppCard);
        networkRelay.UseSunuDegisTokusClientRPC(myPlayerNo, oppPlayer, myCard, oppCard);
    }

    public void ExecuteDegisTokusWithSelectedCard(int myPlayerNo, string selectedCardId)
    {
        if (playersHandCardsIDs == null || !playersHandCardsIDs.ContainsKey(myPlayerNo)) return;

        // Validate selected card is in player's hand
        if (!IsCardInPlayerHand(myPlayerNo, selectedCardId)) return;
        if (IsCardInCenter(selectedCardId)) return;

        // Find random opponent
        var validOpponents = new List<int>();
        foreach (var opp in GetOpponentPlayers(myPlayerNo))
        {
            if (playersHandCardsIDs.ContainsKey(opp) && playersHandCardsIDs[opp] != null && playersHandCardsIDs[opp].Count > 0)
                validOpponents.Add(opp);
        }

        if (validOpponents.Count == 0) return;

        int oppPlayer = validOpponents[UnityEngine.Random.Range(0, validOpponents.Count)];
        string oppCard = playersHandCardsIDs[oppPlayer][UnityEngine.Random.Range(0, playersHandCardsIDs[oppPlayer].Count)];

        if (!IsCardInPlayerHand(oppPlayer, oppCard)) return;
        if (IsCardInCenter(oppCard)) return;

        // Perform swap with selected player card and random opponent card
        SunuDegisTokusSwap(myPlayerNo, oppPlayer, selectedCardId, oppCard);
        networkRelay.UseSunuDegisTokusClientRPC(myPlayerNo, oppPlayer, selectedCardId, oppCard);
    }

    public void SunuDegisTokusSwap(int playerANo, int playerBNo, string cardAID, string cardBID)
    {
        // Swap in player hands
        var handA = playersHandCardsIDs[playerANo];
        var handB = playersHandCardsIDs[playerBNo];
        int idxA = handA.IndexOf(cardAID);
        int idxB = handB.IndexOf(cardBID);
        if (idxA == -1 || idxB == -1) return;

        handA[idxA] = cardBID;
        handB[idxB] = cardAID;
        
        // Track the card swap in move chain for synchronization
        MoveChainIntegrator.TrackCardSwap(playerANo, playerBNo, cardAID, cardBID, "Şunu Değiş Tokuş power");
    }

    /// <summary>
    /// Swaps a specific card in player A's hand (by index) with a specific card in player B's hand (by unique ID).
    /// </summary>
    public void SunuDegisBunuTokusSwap(int playerANo, int playerBNo, string cardAID, string cardBID, int handIndexA)
    {
        if (playersHandCardsIDs == null) return;
        if (!playersHandCardsIDs.ContainsKey(playerANo) || !playersHandCardsIDs.ContainsKey(playerBNo)) return;

        var handA = playersHandCardsIDs[playerANo];
        var handB = playersHandCardsIDs[playerBNo];

        int idxA = handA.IndexOf(cardAID);
        int idxB = handB.IndexOf(cardBID);

        // For safety, use the provided index for handA
        if (handIndexA >= 0 && handIndexA < handA.Count && idxB != -1)
        {
            // Remove cardA from handA at handIndexA
            handA.RemoveAt(handIndexA);
            // Insert cardB into handA at handIndexA
            handA.Insert(handIndexA, cardBID);

            // Remove cardB from handB at idxB
            handB.RemoveAt(idxB);
            // Insert cardA into handB at the same index (idxB)
            handB.Insert(idxB, cardAID);

            
        
        // Track the card swap in move chain for synchronization
        MoveChainIntegrator.TrackCardSwap(playerANo, playerBNo, cardAID, cardBID, "Şunu Değiş Bunu Tokuş power");
        }
    }

    [ContextMenu("NamedPlayerHands")]
    public void NamedPlayerHands()
    {
        if (playersHandCardsIDs == null || allCardLookup == null)
        {
            
            return;
        }

        
        foreach (var kvp in playersHandCardsIDs)
        {
            int playerNo = kvp.Key;
            List<string> hand = kvp.Value;
            
            foreach (var cardID in hand)
            {
                if (allCardLookup.TryGetValue(cardID, out int[] cardArr))
                {
                    
                }
                else
                {
                    
                }
            }
        }
        
    }

    [ContextMenu("Debug Current Game State for Save")]
    public void DebugCurrentGameStateForSave()
    {
        
        
        // Show what would be saved
        if (playersHandCardsIDs != null)
        {
            
            foreach (var kvp in playersHandCardsIDs)
            {
                
            }
        }
        
        if (centerCardsDict != null)
        {
            var centerList = centerCardsDict.Keys.ToList();
            
        }
        
        if (playersPooledCardsIDs != null)
        {
            
            foreach (var kvp in playersPooledCardsIDs)
            {
                
            }
        }
        
        if (bombedCards != null)
        {
            
        }
        
        
    }

    private Dictionary<int, int> zaferPuaniPoints = new Dictionary<int, int>();
    private int zaferPuaniReportsReceived = 0;
    public void AddZaferPuaniPoints(int playerNo, int points)
    {
        if (!zaferPuaniPoints.ContainsKey(playerNo))
            zaferPuaniPoints[playerNo] = 0;
        zaferPuaniPoints[playerNo] += points;
        zaferPuaniReportsReceived++;

        if (zaferPuaniReportsReceived >= connectedPlayerCount)
        {
            ApplyZaferPuaniPoints();
            DecideWinner();
        }
    }

    /// <summary>
    /// Simplified method to add Zafer Puanı points directly (for Kapkaç power)
    /// </summary>
    public void AddZaferPuaniPoint(int playerNo, int pointValue)
    {
        
        
        // Handle team-based scoring for 2v2 mode
        if (playerCount == 4)
        {
            // In 2v2, players 0,2 are team 0, players 1,3 are team 1
            int teamNo = playerNo % 2;
            points[teamNo] += pointValue;
            
        }
        else
        {
            // In 1v1, direct player scoring
            points[playerNo] += pointValue;
            
        }

        BroadcastLiveScoreUpdate();
    }

    private void ApplyZaferPuaniPoints()
    {
        foreach (var kvp in zaferPuaniPoints)
        {
            int playerNo = kvp.Key;
            int points = kvp.Value;
            this.points[playerNo] += points; // 'this.points' is your main points array
            
        }
        zaferPuaniPoints.Clear();
        zaferPuaniReportsReceived = 0;

        BroadcastLiveScoreUpdate();
    }

    public void RecalculateLivePoints(out int livePoints0, out int livePoints1)
    {
        livePoints0 = (points != null && points.Length >= 1) ? points[0] : 0;
        livePoints1 = (points != null && points.Length >= 2) ? points[1] : 0;

        if (playersPooledCardsIDs == null) return;

        Dictionary<int, List<string>> pooledCards;
        if (playerCount == 4)
        {
            pooledCards = new Dictionary<int, List<string>>();
            List<string> pool0 = new List<string>();
            List<string> pool1 = new List<string>();
            if (playersPooledCardsIDs.ContainsKey(0) && playersPooledCardsIDs[0] != null) pool0.AddRange(playersPooledCardsIDs[0]);
            if (playersPooledCardsIDs.ContainsKey(2) && playersPooledCardsIDs[2] != null) pool0.AddRange(playersPooledCardsIDs[2]);
            if (playersPooledCardsIDs.ContainsKey(1) && playersPooledCardsIDs[1] != null) pool1.AddRange(playersPooledCardsIDs[1]);
            if (playersPooledCardsIDs.ContainsKey(3) && playersPooledCardsIDs[3] != null) pool1.AddRange(playersPooledCardsIDs[3]);
            pooledCards[0] = pool0;
            pooledCards[1] = pool1;
        }
        else
        {
            pooledCards = playersPooledCardsIDs;
        }

        foreach (var kvp in pooledCards)
        {
            int playerID = kvp.Key;
            List<string> cardList = kvp.Value;
            if (cardList == null) continue;

            List<string> controlCardList = new List<string>();
            foreach (var card in cardList)
            {
                if (controlCardList.Contains(card)) continue;
                controlCardList.Add(card);

                int[] cardID = null;
                if (allCardLookup != null && allCardLookup.ContainsKey(card))
                {
                    if (allCardLookup[card][1] == 11 || allCardLookup[card][1] == 0)
                    {
                        cardID = allCardLookup[card];
                    }
                    else if (copiedCardMap != null && copiedCardMap.ContainsKey(card))
                    {
                        string copiedCard = copiedCardMap[card];
                        cardID = (allCardLookup.ContainsKey(copiedCard)) ? allCardLookup[copiedCard] : allCardLookup[card];
                    }
                    else
                    {
                        cardID = allCardLookup[card];
                    }
                }

                if (cardID == null) continue;

                int kind = cardID[0];
                int value = cardID[1];

                int gainedPoints = 0;
                if (value == 1) gainedPoints = 1;
                else if (value == 11) gainedPoints = 1;
                else if (kind == 1 && value == 2) gainedPoints = 2;
                else if (kind == 2 && value == 10) gainedPoints = 3;

                if (playerID == 0) livePoints0 += gainedPoints;
                else if (playerID == 1) livePoints1 += gainedPoints;
            }
        }
    }

    public int CalculatePoolClosedCardPoints(int teamNo)
    {
        if (playersPooledCardsIDs == null || firstThreeDealtCardIds == null || firstThreeDealtCardIds.Count == 0) return 0;

        List<string> poolCards = new List<string>();
        if (playerCount == 4)
        {
            if (teamNo == 0)
            {
                if (playersPooledCardsIDs.ContainsKey(0) && playersPooledCardsIDs[0] != null) poolCards.AddRange(playersPooledCardsIDs[0]);
                if (playersPooledCardsIDs.ContainsKey(2) && playersPooledCardsIDs[2] != null) poolCards.AddRange(playersPooledCardsIDs[2]);
            }
            else if (teamNo == 1)
            {
                if (playersPooledCardsIDs.ContainsKey(1) && playersPooledCardsIDs[1] != null) poolCards.AddRange(playersPooledCardsIDs[1]);
                if (playersPooledCardsIDs.ContainsKey(3) && playersPooledCardsIDs[3] != null) poolCards.AddRange(playersPooledCardsIDs[3]);
            }
        }
        else
        {
            if (playersPooledCardsIDs.ContainsKey(teamNo) && playersPooledCardsIDs[teamNo] != null)
            {
                poolCards.AddRange(playersPooledCardsIDs[teamNo]);
            }
        }

        int closedPoints = 0;
        List<string> controlList = new List<string>();
        foreach (var card in poolCards)
        {
            if (controlList.Contains(card)) continue;
            controlList.Add(card);

            if (firstThreeDealtCardIds.Contains(card))
            {
                int[] cardID = null;
                if (allCardLookup != null && allCardLookup.ContainsKey(card))
                {
                    if (allCardLookup[card][1] == 11 || allCardLookup[card][1] == 0)
                    {
                        cardID = allCardLookup[card];
                    }
                    else if (copiedCardMap != null && copiedCardMap.ContainsKey(card))
                    {
                        string copiedCard = copiedCardMap[card];
                        cardID = (allCardLookup.ContainsKey(copiedCard)) ? allCardLookup[copiedCard] : allCardLookup[card];
                    }
                    else
                    {
                        cardID = allCardLookup[card];
                    }
                }

                if (cardID != null)
                {
                    int kind = cardID[0];
                    int value = cardID[1];

                    int gainedPoints = 0;
                    if (value == 1) gainedPoints = 1;
                    else if (value == 11) gainedPoints = 1;
                    else if (kind == 1 && value == 2) gainedPoints = 2;
                    else if (kind == 2 && value == 10) gainedPoints = 3;

                    closedPoints += gainedPoints;
                }
            }
        }

        return closedPoints;
    }

    public void BroadcastLiveScoreUpdate()
    {
        if (networkRelay == null)
        {
            return;
        }

        if (points == null || points.Length < 2)
        {
            return;
        }

        RecalculateLivePoints(out int livePoints0, out int livePoints1);

        // Calculate visual fake score reductions for closed point cards currently in team pools
        p1side_selfFakePointReduction = 0; // Extensible
        p2side_selfFakePointReduction = 0; // Extensible
        p1side_oppFakePointReduction = CalculatePoolClosedCardPoints(0);
        p2side_oppFakePointReduction = CalculatePoolClosedCardPoints(1);

        networkRelay.UpdateScoreDisplayClientRPC(
            livePoints0, 
            livePoints1, 
            p1side_selfFakePointReduction, 
            p2side_selfFakePointReduction, 
            p1side_oppFakePointReduction, 
            p2side_oppFakePointReduction
        );
        PersistServerTruthsToSession();
    }


    bool endTestFlag = false;
    [ContextMenu("RandomlyDistributeCardsAndDecideWinner")]
    public void RandomlyDistributeCardsAndDecideWinner()
    {
        endTestFlag = true;
        // Ensure player pools are initialized
        playersPooledCardsIDs = new Dictionary<int, List<string>>();
        playersPiştiPoolCardsIDs = new Dictionary<int, List<string>>();
        for (int i = 0; i < playerCount; i++)
        {
            playersPooledCardsIDs[i] = new List<string>();
            playersPiştiPoolCardsIDs[i] = new List<string>();
        }

        // Get all card IDs
        List<string> allCardIDs = new List<string>(allCardLookup.Keys);

        // Shuffle the cards
        System.Random rng = new System.Random();
        int n = allCardIDs.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            string value = allCardIDs[k];
            allCardIDs[k] = allCardIDs[n];
            allCardIDs[n] = value;
        }

        // Distribute cards to player pools
        for (int i = 0; i < allCardIDs.Count; i++)
        {
            int playerIndex = i % playerCount;
            playersPooledCardsIDs[playerIndex].Add(allCardIDs[i]);
        }

        
        // Example: Assign pools for showcase

        networkRelay.AssignCardsToPlayerPoolsClientRPC(new SerializableDictionary(playersPooledCardsIDs));
        DecideWinner();
    }


    public void OnClientDealCenterFinished(ulong clientId)
    {
        dealCenterFinishedClients.Add(clientId);
        
        // DealCardsToPlayerHands is now triggered by DealPhase coroutine after all confirmations arrive
    }

    // ===== GAME STATE MANAGEMENT =====

    /// <summary>
    /// Builds a complete game state snapshot from current server state
    /// </summary>
    public SerializableGameState BuildGameStateSnapshot()
    {
        snapshotVersionCounter++;
        
        var snapshot = new SerializableGameState();
        
        // Core game info
        snapshot.snapshotVersion = snapshotVersionCounter;
        snapshot.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        snapshot.playerCount = this.playerCount;
        snapshot.currentPlayer = this.currentPlayer;
        snapshot.turnCounter = this.turnCounter;
        snapshot.roundCount = this.roundCount;
        snapshot.startingPlayerNo = this.startingPlayerNo;
        snapshot.seed = this.seed;
        snapshot.lastPlayerToCapture = this.lastPlayerToCapture;
        snapshot.readyToEndTurnCounter = this.readyToEndTurnCounter;
        snapshot.turnTimerElapsed = this.timer;
        snapshot.turnTimeLimit = this.turnTime;

        // Convert deck from dictionary to ordered list (if you maintain deck order)
        var deckList = new List<string>();
        if (deckCardsDict != null)
        {
            foreach (var kvp in deckCardsDict)
            {
                deckList.Add(kvp.Key);
            }
        }
        snapshot.deck = new SerializableStringList(deckList);

        // Center cards (maintain order)
        var centerList = new List<string>();
        if (centerCardsDict != null)
        {
            foreach (var kvp in centerCardsDict)
            {
                centerList.Add(kvp.Key);
            }
        }
        snapshot.center = new SerializableStringList(centerList);
        snapshot.firstThreeDealtCardIds = new SerializableStringList(firstThreeDealtCardIds ?? new List<string>());

        // Player hands
        snapshot.hands = new SerializableDictionary();
        if (playersHandCardsIDs != null)
        {
            var handsDict = new Dictionary<int, List<string>>();
            foreach (var kvp in playersHandCardsIDs)
            {
                handsDict[kvp.Key] = new List<string>(kvp.Value);
            }
            snapshot.hands = new SerializableDictionary(handsDict);
        }

        // Player pools
        snapshot.pools = new SerializableDictionary();
        if (playersPooledCardsIDs != null)
        {
            var poolsDict = new Dictionary<int, List<string>>();
            foreach (var kvp in playersPooledCardsIDs)
            {
                poolsDict[kvp.Key] = new List<string>(kvp.Value);
            }
            snapshot.pools = new SerializableDictionary(poolsDict);
        }

        // Pişti pools
        snapshot.pistiPools = new SerializableDictionary();
        if (playersPiştiPoolCardsIDs != null)
        {
            var pistiPoolsDict = new Dictionary<int, List<string>>();
            foreach (var kvp in playersPiştiPoolCardsIDs)
            {
                pistiPoolsDict[kvp.Key] = new List<string>(kvp.Value);
            }
            snapshot.pistiPools = new SerializableDictionary(pistiPoolsDict);
        }
        else
        {
            snapshot.pistiPools = new SerializableDictionary(new Dictionary<int, List<string>>());
        }

        // Bomb stack (cards outside normal game flow)
        snapshot.bombStack = new SerializableStringList(bombedCards ?? new List<string>());

        // Scores and counts
        snapshot.points = new SerializableIntArray(points ?? new int[playerCount]);
        snapshot.pistiCounts = new SerializableIntArray(piştiCounts ?? new int[playerCount]);

        // Perspective Point Reductions
        snapshot.p1side_selfFakePointReduction = p1side_selfFakePointReduction;
        snapshot.p2side_selfFakePointReduction = p2side_selfFakePointReduction;
        snapshot.p1side_oppFakePointReduction = p1side_oppFakePointReduction;
        snapshot.p2side_oppFakePointReduction = p2side_oppFakePointReduction;

        // Bot-controlled players
        var botList = new List<int>();
        if (botControlledPlayers != null)
        {
            botList.AddRange(botControlledPlayers);
        }
        snapshot.botControlledPlayers = new SerializableIntArray(botList.ToArray());

        // Active effects and flags
        snapshot.copiedCardMap = new SerializableStringDictionary(copiedCardMap ?? new Dictionary<string, string>());
        
        snapshot.oynayamazsinActive = oynayamazsinPending; // Use your actual flag
        snapshot.isYapamazsınActive = isYapamazsınActive;
        snapshot.verZehriActive = verZehriActive;
        snapshot.kutsalDesteActive = kutsalDesteActive;
        snapshot.verZehriPending = verZehriPending;
        snapshot.kutsalDestePending = kutsalDestePending;
        snapshot.oynayamazsinPending = oynayamazsinPending;
        snapshot.oynayamazsinActivatedBy = oynayamazsinActivatedBy;
        snapshot.blockCount = blockCount;

        // Pull host-side client power states when available so migration snapshots are complete.
        GameManager gameManager = GameManager.LocalInstance;
        if (gameManager != null)
        {
            snapshot.oynayamazsinActive = gameManager.oynayamazsinActive;
            snapshot.isKapkacPending = gameManager.isKapkacPending;
            snapshot.isYandimAnamPending = gameManager.isYandimAnamPending;
            snapshot.isKopyalaActive = gameManager.isKopyalaActive;
            snapshot.isSunuDegisTokusActive = gameManager.isSunuDegisTokusActive;
            snapshot.isSunuDegisBunuTokusActive = gameManager.isSunuDegisBunuTokusActive;
            snapshot.cardPowerEffects = new SerializableStringDictionary(gameManager.GetCardPowerEffectsSnapshot());
        }
        else
        {
            snapshot.oynayamazsinActive = oynayamazsinPending;
            snapshot.isKapkacPending = false;
            snapshot.isYandimAnamPending = false;
            snapshot.isKopyalaActive = false;
            snapshot.isSunuDegisTokusActive = false;
            snapshot.isSunuDegisBunuTokusActive = false;
            snapshot.cardPowerEffects = new SerializableStringDictionary(new Dictionary<string, string>());
        }

        // Optional: Player gold (leave empty for now since it's client-managed)
        snapshot.playerGold = new SerializableIntDictionary(playerGolds);

        snapshot.playerSuperPowers = new SerializableDictionary(playerSuperPowers);

        // Card master lookup - required for re-host so ApplyGameStateToServer can rebuild centerCardsDict
        if (allCardLookup != null && allCardLookup.Count > 0)
        {
            var entries = new CardLookupEntry[allCardLookup.Count];
            int i = 0;
            foreach (var kvp in allCardLookup)
            {
                entries[i++] = new CardLookupEntry { cardId = kvp.Key, kind = kvp.Value[0], value = kvp.Value[1] };
            }
            snapshot.cardLookup = entries;
        }

        
        
        return snapshot;
}

    /// <summary>
    /// Context menu to manually save current game state for testing
    /// </summary>
    [ContextMenu("Save Current Game State")]
    public void SaveCurrentGameState()
    {
        // Debug what's being saved BEFORE creating snapshot
        DebugCurrentGameStateForSave();
        
        manualSavedState = BuildGameStateSnapshot();
        hasManualSavedState = true;
        
        // Ask clients to log the saved snapshot for BEFORE/AFTER comparison
        networkRelay.LogSnapshotClientRPC(manualSavedState, $"SERVER SAVED SNAPSHOT v{manualSavedState.snapshotVersion}");
    }

    /// <summary>
    /// Context menu to load and apply the manually saved game state to all clients
    /// </summary>
    [ContextMenu("Load Saved Game State")]
    public void LoadSavedGameState()
    {
        if (!hasManualSavedState)
        {
            
            return;
        }

        
        // Tell clients to log their current local BEFORE state via ApplyGameState path
        networkRelay.LogSnapshotClientRPC(manualSavedState, $"SERVER LOADING SNAPSHOT v{manualSavedState.snapshotVersion}");
        ApplyGameStateToServer(manualSavedState);
        networkRelay.ApplyGameStateClientRPC(manualSavedState);
    }

    /// <summary>
    /// Context menu to dump current game state for debugging
    /// </summary>
    [ContextMenu("Dump Current Game State")]
    public void DumpCurrentGameState()
    {
        var snapshot = BuildGameStateSnapshot();
        Debug.Log($"[Server] Current Game State Dump:\n" +
                  $"Version: {snapshot.snapshotVersion}\n" +
                  $"Player Count: {snapshot.playerCount}\n" +
                  $"Current Player: {snapshot.currentPlayer}\n" +
                  $"Turn Counter: {snapshot.turnCounter}\n" +
                  $"Round Count: {snapshot.roundCount}\n" +
                  $"Center Cards: {snapshot.center.ToList().Count}\n" +
                  $"Player Hands: {(snapshot.hands.ToDictionary()?.Count ?? 0)}\n" +
                  $"Player Pools: {(snapshot.pools.ToDictionary()?.Count ?? 0)}");
    }

    /// <summary>
    /// Applies a game state snapshot to the server's internal state
    /// </summary>
    public void ApplyGameStateToServer(SerializableGameState snapshot, bool bypassStaleCheck = false)
    {
        // STALE SNAPSHOT GUARD: Reject snapshots that are older than current tracked state
        if (!bypassStaleCheck && hasCurrentState && snapshot.snapshotVersion > 0 && snapshot.snapshotVersion <= currentGameState.snapshotVersion)
        {
            
            return;
        }

        // SNAPSHOT VALIDATION: Ensure critical containers are present before wiping current state
        var handsToApply = snapshot.hands.ToDictionary();
        var poolsToApply = snapshot.pools.ToDictionary();
        if (handsToApply == null || poolsToApply == null)
        {
            
            return;
        }

        // Core game info
        this.playerCount = snapshot.playerCount; // FIX: Ensure playerCount is restored
        this.currentPlayer = snapshot.currentPlayer;
        this.turnCounter = snapshot.turnCounter;
        this.roundCount = snapshot.roundCount;
        this.startingPlayerNo = snapshot.startingPlayerNo;

        this.seed = snapshot.seed; // FIX: Restore seed for consistent deck across migration
        this.lastPlayerToCapture = snapshot.lastPlayerToCapture;
        this.readyToEndTurnCounter = snapshot.readyToEndTurnCounter;
        this.timer = snapshot.turnTimerElapsed;
        this.turnTime = snapshot.turnTimeLimit;

        if (firstThreeDealtCardIds == null) firstThreeDealtCardIds = new List<string>();
        firstThreeDealtCardIds.Clear();
        var snapshotFirstThree = snapshot.firstThreeDealtCardIds.ToList();
        if (snapshotFirstThree != null)
        {
            firstThreeDealtCardIds.AddRange(snapshotFirstThree);
        }

        // Restore allCardLookup first so other card rebuilds work
        if (snapshot.cardLookup != null && snapshot.cardLookup.Length > 0)
        {
            if (allCardLookup == null) allCardLookup = new Dictionary<string, int[]>();
            allCardLookup.Clear();
            foreach (var entry in snapshot.cardLookup)
            {
                allCardLookup[entry.cardId] = new int[] { entry.kind, entry.value };
            }
            
        }

        // Rebuild dictionaries from snapshot
        if (deckCardsDict == null) deckCardsDict = new Dictionary<string, int[]>();
        deckCardsDict.Clear();

        List<string> snapshotDeck = snapshot.deck.ToList();
        if (snapshotDeck.Count > 0)
        {
            foreach (string cardId in snapshotDeck)
            {
                if (allCardLookup != null && allCardLookup.ContainsKey(cardId))
                {
                    deckCardsDict[cardId] = allCardLookup[cardId];
                }
            }
        }
        else if (snapshot.seed != 0 && allCardLookup != null && allCardLookup.Count > 0)
        {
            // HYBRID RECONSTRUCTION: Rebuild deck from seed and subtract dealt cards
            
            
            // 1. Create a list of all cards in the master lookup, sorted by ID numeric suffix
            // This ensures the order is identical to the initial SaveAllCards() call.
            var fullDeckList = allCardLookup.Keys
                .OrderBy(id => {
                    if (id.StartsWith("card_") && int.TryParse(id.Substring(5), out int num)) return num;
                    return 999;
                })
                .ToList();
            
            // 2. Shuffle it with the same algorithm as SuffleCards(seed)
            System.Random rng = new System.Random(snapshot.seed);
            int count = fullDeckList.Count;
            for (int i = 0; i < count - 1; i++)
            {
                int r = rng.Next(i, count);
                string temp = fullDeckList[i];
                fullDeckList[i] = fullDeckList[r];
                fullDeckList[r] = temp;
            }

            // 3. Identify all cards currently "in play"
            var cardsInPlay = new HashSet<string>();
            foreach (var cardId in snapshot.center.ToList()) cardsInPlay.Add(cardId);
            foreach (var cardId in snapshot.bombStack.ToList()) cardsInPlay.Add(cardId);
            
            var handsDict = snapshot.hands.ToDictionary();
            foreach (var list in handsDict.Values) { foreach (var cardId in list) cardsInPlay.Add(cardId); }
            
            var poolsDict = snapshot.pools.ToDictionary();
            foreach (var list in poolsDict.Values) { foreach (var cardId in list) cardsInPlay.Add(cardId); }
            
            var pistiPoolsDict = snapshot.pistiPools.ToDictionary();
            foreach (var list in pistiPoolsDict.Values) { foreach (var cardId in list) cardsInPlay.Add(cardId); }

            // 4. Populate deckCardsDict with cards NOT in play, preserving shuffled order
            foreach (string cardId in fullDeckList)
            {
                if (!cardsInPlay.Contains(cardId))
                {
                    deckCardsDict[cardId] = allCardLookup[cardId];
                }
            }
            
        }

        if (centerCardsDict == null) centerCardsDict = new Dictionary<string, int[]>();
        centerCardsDict.Clear();
        bool lookupMissing = allCardLookup == null || allCardLookup.Count == 0;
        if (lookupMissing)
            
        foreach (string cardId in snapshot.center.ToList())
        {
            if (!lookupMissing && allCardLookup.ContainsKey(cardId))
            {
                centerCardsDict[cardId] = allCardLookup[cardId];
            }
        }

        if (playersHandCardsIDs == null) playersHandCardsIDs = new Dictionary<int, List<string>>();
        playersHandCardsIDs.Clear();
        foreach (var kvp in handsToApply)
        {
            playersHandCardsIDs[kvp.Key] = kvp.Value;
        }

        if (playersPooledCardsIDs == null) playersPooledCardsIDs = new Dictionary<int, List<string>>();
        playersPooledCardsIDs.Clear();
        foreach (var kvp in poolsToApply)
        {
            playersPooledCardsIDs[kvp.Key] = kvp.Value;
        }

        if (playersPiştiPoolCardsIDs == null) playersPiştiPoolCardsIDs = new Dictionary<int, List<string>>();
        playersPiştiPoolCardsIDs.Clear();
        var pistiPoolsToApply = snapshot.pistiPools.ToDictionary();
        foreach (var kvp in pistiPoolsToApply)
        {
            playersPiştiPoolCardsIDs[kvp.Key] = kvp.Value;
        }

        // Restore bot-controlled players
        botControlledPlayers.Clear();
        if (snapshot.botControlledPlayers.items != null)
        {
            foreach (int playerNo in snapshot.botControlledPlayers.items)
            {
                botControlledPlayers.Add(playerNo);
                
            }
        }

        // Apply bombed cards (cards outside normal game flow)
        bombedCards.Clear();
        var bombedList = snapshot.bombStack.ToList();
        foreach (string cardId in bombedList)
        {
            bombedCards.Add(cardId);
            
        }

        // Apply effects and flags
        copiedCardMap = snapshot.copiedCardMap.ToDictionary();
        
        isYapamazsınActive = snapshot.isYapamazsınActive;
        verZehriActive = snapshot.verZehriActive;
        kutsalDesteActive = snapshot.kutsalDesteActive;
        verZehriPending = snapshot.verZehriPending;
        kutsalDestePending = snapshot.kutsalDestePending;
        oynayamazsinPending = snapshot.oynayamazsinPending;
        oynayamazsinActivatedBy = snapshot.oynayamazsinActivatedBy;
        blockCount = snapshot.blockCount;

        // Apply scores
        points = snapshot.points.ToArray();
        piştiCounts = snapshot.pistiCounts.ToArray();

        // Restore perspective reductions
        p1side_selfFakePointReduction = snapshot.p1side_selfFakePointReduction;
        p2side_selfFakePointReduction = snapshot.p2side_selfFakePointReduction;
        p1side_oppFakePointReduction = snapshot.p1side_oppFakePointReduction;
        p2side_oppFakePointReduction = snapshot.p2side_oppFakePointReduction;

        playerGolds = snapshot.playerGold.ToDictionary();
        playerSuperPowers = snapshot.playerSuperPowers.ToDictionary();

        
    }

    public SerializableGameState CaptureMigrationSnapshot()
    {
        if (hasCurrentState)
        {
            return currentGameState;
        }

        return BuildGameStateSnapshot();
    }

    public bool RestoreFromSnapshot(SerializableGameState snapshot)
    {
        if (!IsSnapshotValidForRestore(snapshot, out string validationError))
        {
            
            return false;
        }

        ApplyGameStateToServer(snapshot);
        currentGameState = snapshot;
        hasCurrentState = true;

        // Seat binding: Restore playerClientIds from SEAT_MAP in session
        try
        {
            var seatMap = LoadSeatMapFromSession();
            foreach (var entry in seatMap)
            {
                if (entry.Value.ClientId != 0)
                {
                    playerClientIds[entry.Key] = entry.Value.ClientId;
                    
                }
            }
        }
        catch (Exception e)
        {
            
        }

        ValidateTotalCardCount();

        // MIGRATION TURN RESUMPTION: Broadcast turn state to clients and resume turn timer / bot logic
        CallUpdateCurrentPlayer();
        BroadcastLiveScoreUpdate();
        if (isActiveHost)
        {
            if (activeTurnTimerCoroutine != null) StopCoroutine(activeTurnTimerCoroutine);
            bool isBotTurn = IsBotTurn();
            if (!isBotTurn)
            {
                activeTurnTimerCoroutine = StartCoroutine(TurnTimerCoroutine(currentPlayer));
                
            }
        }

        return true;
    }

    private void ValidateTotalCardCount()
    {
        int total = 0;
        if (deckCardsDict != null) total += deckCardsDict.Count;
        if (centerCardsDict != null) total += centerCardsDict.Count;
        
        if (playersHandCardsIDs != null)
        {
            foreach (var list in playersHandCardsIDs.Values) total += list.Count;
        }
        
        if (playersPooledCardsIDs != null)
        {
            foreach (var list in playersPooledCardsIDs.Values) total += list.Count;
        }
        
        if (bombedCards != null) total += bombedCards.Count;

        
        if (total != 52)
        {
            
        }
    }

    private bool IsSnapshotValidForRestore(SerializableGameState snapshot, out string validationError)
    {
        validationError = string.Empty;

        var hands = snapshot.hands.ToDictionary();
        var pools = snapshot.pools.ToDictionary();
        if (hands == null || pools == null)
        {
            validationError = "hands or pools is null";
            return false;
        }

        if (snapshot.playerCount <= 0)
        {
            validationError = "playerCount is invalid";
            return false;
        }

        return true;
    }

    // Note: Automatic or request-based broadcasting removed per user request. Only manual Save/Load remains.

    // ===== REDO SYSTEM: AUTOMATIC GAME STATE TRACKING =====

    /// <summary>
    /// Automatically saves the current game state for redo functionality
    /// This should be called at strategic points (before turn changes, before major actions)
    /// </summary>
    public void SaveCurrentGameStateForRedo()
    {
        // Shift the states: current -> previous -> pre-previous
        if (hasCurrentState)
        {
            if (hasPreviousState)
            {
                prePreviousGameState = previousGameState;
                hasPrePreviousState = true;
            }
            
            previousGameState = currentGameState;
            hasPreviousState = true;
        }
        
        // Save new current state
        currentGameState = BuildGameStateSnapshot();
        hasCurrentState = true;
        
        
        
    }

    /// <summary>
    /// Reverts to the previous game state (1 turn back)
    /// </summary>
    [ContextMenu("Redo: Revert to Previous State")]
    public void RevertToPreviousState()
    {
        if (!hasPreviousState)
        {
            
            return;
        }

        
        
        
        // Apply the previous state to server
        ApplyGameStateToServer(previousGameState, true);
        
        // Initialize client confirmation tracking for redo
        redoSceneReconstructionClients.Clear();
        isWaitingForRedoConfirmations = true;
        
        // Broadcast to all clients and wait for their confirmation
        networkRelay.RedoRevertToStateClientRPC(previousGameState, "Previous State");
        
        // Safety timeout: if no clients respond within 5 seconds, proceed anyway
        Invoke("ForceRedoCurrentPlayerUpdate", 5.0f);
        
        
        
        // Shift states: previous becomes current, pre-previous becomes previous
        currentGameState = previousGameState;
        hasCurrentState = true;
        
        if (hasPrePreviousState)
        {
            previousGameState = prePreviousGameState;
            hasPreviousState = true;
            hasPrePreviousState = false;
        }
        else
        {
            hasPreviousState = false;
        }
        
        
    }

    /// <summary>
    /// Reverts to the pre-previous game state (2 turns back)
    /// </summary>
    [ContextMenu("Redo: Revert to Pre-Previous State")]
    public void RevertToPrePreviousState()
    {
        if (!hasPrePreviousState)
        {
            
            return;
        }

        
        
        
        // Apply the pre-previous state to server
        ApplyGameStateToServer(prePreviousGameState, true);
        
        // Initialize client confirmation tracking for redo
        redoSceneReconstructionClients.Clear();
        isWaitingForRedoConfirmations = true;
        
        // Broadcast to all clients and wait for their confirmation
        networkRelay.RedoRevertToStateClientRPC(prePreviousGameState, "Pre-Previous State");
        
        // Safety timeout: if no clients respond within 5 seconds, proceed anyway
        Invoke("ForceRedoCurrentPlayerUpdate", 5.0f);
        
        
        
        // Shift states: pre-previous becomes current, clear others
        currentGameState = prePreviousGameState;
        hasCurrentState = true;
        hasPreviousState = false;
        hasPrePreviousState = false;
        
        
    }

    /// <summary>
    /// Gets the status of available redo states
    /// </summary>
    public string GetRedoStateStatus()
    {
        return $"Redo States - Current: {hasCurrentState}, Previous: {hasPreviousState}, PrePrevious: {hasPrePreviousState}";
    }

    /// <summary>
    /// Context menu to show redo state status
    /// </summary>
    [ContextMenu("Show Redo State Status")]
    public void ShowRedoStateStatus()
    {
        string status = GetRedoStateStatus();
            
    }

    /// <summary>
    /// Resets all redo states (called when starting new game)
    /// </summary>
    public void ResetRedoStates()
    {
        hasCurrentState = false;
        hasPreviousState = false;
        hasPrePreviousState = false;
        
    }

    /// <summary>
    /// Saves the initial game state after cards are dealt (called via Invoke)
    /// </summary>
    public void SaveInitialGameStateForRedo()
    {
        // Save the initial state after game setup is complete
        SaveCurrentGameStateForRedo();
        
    }

    /// <summary>
    /// Sends current player update after redo (called via Invoke with delay)
    /// </summary>
    public void SendCurrentPlayerUpdateAfterRedo()
    {
        
        networkRelay.UpdateCurrentPlayerClientRPC(currentPlayer, turnCounter);
        
        // REDO BOT FIX: Check if it's bot's turn after redo and trigger bot move
        bool isBotTurn = IsBotTurn();
        if (isBotTurn)
        {
            
            if (botPlayer != null)
            {
                // Small delay to ensure client state is fully updated
                Invoke("TriggerBotMoveAfterRedo", 1.0f);
            }
            else
            {
                
            }
        }
        else
        {
            
        }
    }

    /// <summary>
    /// Triggers bot move after redo completion (called via Invoke with delay)
    /// </summary>
    public void TriggerBotMoveAfterRedo()
    {
        
        
        bool isBotTurn = IsBotTurn();
        if (isBotTurn && GetBotPlayer() != null && isActiveHost)
        {
            
            GetBotPlayer().OnBotTurn(currentPlayer);
        }
else
        {
            
        }
    }

    /// <summary>
    /// Context menu for testing bot trigger after redo
    /// </summary>
    [ContextMenu("Test Bot Trigger After Redo")]
    public void TestBotTriggerAfterRedo()
    {
        
        TriggerBotMoveAfterRedo();
    }

    /// <summary>
    /// Forces current player update after redo timeout (safety mechanism)
    /// </summary>
    public void ForceRedoCurrentPlayerUpdate()
    {
        if (isWaitingForRedoConfirmations)
        {
            
            
            // Reset confirmation tracking
            isWaitingForRedoConfirmations = false;
            redoSceneReconstructionClients.Clear();
            
            // Send current player update anyway (this will also trigger bot if needed)
            SendCurrentPlayerUpdateAfterRedo();
        }
    }

    /// <summary>
    /// Handles client confirmation that they've finished redo scene reconstruction
    /// </summary>
    public void OnClientRedoSceneReconstructionFinished(ulong clientId)
    {
        if (!isWaitingForRedoConfirmations)
        {
            
            return;
        }

        
        redoSceneReconstructionClients.Add(clientId);

        // Check if all clients have confirmed
        CheckAllClientsReadyForRedoCurrentPlayerUpdate();
    }

    /// <summary>
    /// Checks if all connected clients have confirmed redo scene reconstruction
    /// </summary>
    private void CheckAllClientsReadyForRedoCurrentPlayerUpdate()
    {
        if (!isWaitingForRedoConfirmations)
            return;

        // Count connected clients (excluding server/host)
        int expectedClients = 0;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            expectedClients = NetworkManager.Singleton.ConnectedClients.Count - 1; // -1 for host
        }

        

        if (redoSceneReconstructionClients.Count >= expectedClients)
        {
            
            
            // Reset confirmation tracking
            isWaitingForRedoConfirmations = false;
            redoSceneReconstructionClients.Clear();
            
            // Now send the current player update
            SendCurrentPlayerUpdateAfterRedo();
        }
        else if (expectedClients == 0)
        {
            // No clients connected (bot mode or single player)
            
            
            // Reset confirmation tracking
            isWaitingForRedoConfirmations = false;
            redoSceneReconstructionClients.Clear();
            
            // Send current player update immediately (this will also trigger bot if needed)
            SendCurrentPlayerUpdateAfterRedo();
        }
    }

    // Note: Reconnection now uses existing desync detection system - no special methods needed
    
    // Note: Sync timeout checking removed - using desync detection system

    /// <summary>
    /// Checks if a client is currently reconnecting
    /// </summary>
    public bool IsClientReconnecting(ulong clientId)
    {
        return reconnectingClients.Contains(clientId);
    }

    /// <summary>
    /// Removes a client from the reconnecting set
    /// </summary>
    public void RemoveReconnectingClient(ulong clientId)
    {
        if (reconnectingClients.Contains(clientId))
        {
            reconnectingClients.Remove(clientId);
            
        }
        reconnectStartTimes.Remove(clientId);
    }

    public void OnReconnectionReady(ulong clientId)
    {
        RemoveReconnectingClient(clientId);
        CallUpdateCurrentPlayer();

        // Resume turn timer if it is not currently running (e.g. after re-host recovery from a crash)
        if (activeTurnTimerCoroutine == null && reconnectingClients.Count == 0 && isActiveHost)
        {
            bool isBotTurn = botPlayerActive && ((playerCount == 2 && currentPlayer == 1) || (playerCount == 4 && currentPlayer != 0));
            if (!isBotTurn)
            {
                activeTurnTimerCoroutine = StartCoroutine(TurnTimerCoroutine(currentPlayer));
                
            }
        }

        
    }

    // ===== BOT SYSTEM =====

    /// <summary>
    /// Public method to enable/disable bot mode for 1v1 games
    /// </summary>
    [ContextMenu("Toggle Bot Mode")]
    public void ToggleBotMode()
    {
        isBotModeEnabled = !isBotModeEnabled;
        
        
        // If disabling bot mode during an active bot game, deactivate the bot
        if (!isBotModeEnabled && botPlayerActive)
        {
            botPlayerActive = false;
            if (botMoveCoroutine != null)
            {
                StopCoroutine(botMoveCoroutine);
                botMoveCoroutine = null;
            }
            
            // Deactivate the BotPlayer
            if (GetBotPlayer() != null)
            {
                GetBotPlayer().DeactivateBot();
            }
            
            
        }
    }
    
    /// <summary>
    /// Sets the bot mode enabled state (called by BotPlayer)
    /// </summary>
    /// <param name="enabled">True to enable bot mode, false to disable</param>
    public void SetBotModeEnabled(bool enabled)
    {
        isBotModeEnabled = enabled;
        
        
        // If disabling bot mode during an active bot game, deactivate the bot
        if (!isBotModeEnabled && botPlayerActive)
        {
            botPlayerActive = false;
            if (botMoveCoroutine != null)
            {
                StopCoroutine(botMoveCoroutine);
                botMoveCoroutine = null;
            }
            
            // Deactivate the BotPlayer
            if (GetBotPlayer() != null)
            {
                GetBotPlayer().DeactivateBot();
            }
            
            
        }
    }
    

    // OLD BOT METHODS - NO LONGER USED (BotPlayer handles this now)
    /*
    /// <summary>
    /// Schedules a bot move after a short delay
    /// </summary>
    private void ScheduleBotMove()
    {
        if (botMoveCoroutine != null)
        {
            StopCoroutine(botMoveCoroutine);
        }
        botMoveCoroutine = StartCoroutine(ExecuteBotMoveCoroutine());
    }
    */

    /*
    /// <summary>
    /// Coroutine that waits for a delay then executes a bot move
    /// </summary>
    private IEnumerator ExecuteBotMoveCoroutine()
    {
        yield return new WaitForSeconds(botMoveDelay);
        
        if (botPlayerActive && currentPlayer == botPlayerNumber)
        {
            ExecuteBotMove();
        }
        
        botMoveCoroutine = null;
    }
    */

    /*
    /// <summary>
    /// Executes a bot move by selecting a card and determining if it can capture
    /// </summary>
    private void ExecuteBotMove()
    {
        
        
        // Get bot's hand
        if (playersHandCardsIDs == null || !playersHandCardsIDs.ContainsKey(botPlayerNumber))
        {
            
            return;
        }

        List<string> botHand = playersHandCardsIDs[botPlayerNumber];
        if (botHand == null || botHand.Count == 0)
        {
            
            return;
        }

        // Select card to play using improved strategy
        string selectedCardId = SelectBotCard(botHand);
        int[] selectedCard = allCardLookup[selectedCardId];
        
        

        // Check for possible captures
        SerializableCard captureCards = FindBestCapture(selectedCard);
        int sumValue = 0;
        
        if (captureCards.ToDictionary().Count > 0)
        {
            // Bot can capture - calculate sum value
            var captureDict = captureCards.ToDictionary();
            sumValue = captureDict.Values.LastOrDefault()?[1] ?? 0;
            
        }
        else
        {
            // Bot is playing to center
            
        }

        // Execute the move using the same flow as human players
        // The turn will end automatically when the client-side animation completes
        GetMove(selectedCardId, captureCards, botPlayerNumber, sumValue);
    }

    /// <summary>
    /// Finds the best capture for the given card
    /// Returns a SerializableCard containing cards that can be captured
    /// </summary>
    private SerializableCard FindBestCapture(int[] selectedCard)
    {
        Dictionary<string, int[]> captures = new Dictionary<string, int[]>();
        
        if (centerCardsDict == null || centerCardsDict.Count == 0)
        {
            
            return new SerializableCard(captures); // No center cards to capture
        }

        int cardValue = selectedCard[1];
        
        
        // Debug: Print all center cards
        foreach (var kvp in centerCardsDict)
        {
            
        }
        
        // Simple bot logic: Try to capture cards that match the selected card's value
        // Or capture all cards if playing a Jack (value 11)
        if (cardValue == 11) // Jack captures all
        {
            captures = new Dictionary<string, int[]>(centerCardsDict);
            
        }
        else
        {
            // Look for exact value matches - only capture matching cards
            foreach (var kvp in centerCardsDict)
            {
                if (kvp.Value[1] == cardValue)
                {
                    captures[kvp.Key] = kvp.Value;
                    
                    // Only take one match for now (simple bot logic)
                    break;
                }
            }
            
            if (captures.Count == 0)
            {
                
            }
        }

        
        return new SerializableCard(captures);
    }

    /// <summary>
    /// Selects which card the bot should play using a simple strategy
    /// Priority: 1) Card that can capture 2) Jack (captures all) 3) Sequential order
    /// </summary>
    private string SelectBotCard(List<string> botHand)
    {
        // Strategy 1: Look for cards that can capture something
        foreach (string cardId in botHand)
        {
            int[] card = allCardLookup[cardId];
            SerializableCard testCapture = FindBestCapture(card);
            if (testCapture.ToDictionary().Count > 0)
            {
                
                return cardId;
            }
        }

        // Strategy 2: If no captures available, prefer Jacks (they're powerful)
        foreach (string cardId in botHand)
        {
            int[] card = allCardLookup[cardId];
            if (card[1] == 11) // Jack
            {
                
                return cardId;
            }
        }

        // Strategy 3: Default to first card (sequential order)
        string defaultCard = botHand[0];
        int[] defaultCardData = allCardLookup[defaultCard];
        
        return defaultCard;
    }


    /*
    /// <summary>
    /// Context menu method to force a bot move (for testing)
    /// </summary>
    [ContextMenu("Force Bot Move")]
    public void ForceBotMove()
    {
        if (botPlayerActive)
        {
            
            ExecuteBotMove();
        }
        else
        {
            
        }
    }
    */

    /// <summary>
    /// Context menu method to force a bot move (for testing) - uses BotPlayer
    /// </summary>
    [ContextMenu("Force Bot Move")]
    public void ForceBotMove()
    {
        if (botPlayerActive && GetBotPlayer() != null)
        {
            
            GetBotPlayer().ForceBotMove();
        }
        else
        {
            
        }
    }

    /// <summary>
    /// Context menu method to start a bot game manually (for testing)
    /// </summary>
    [ContextMenu("Start Bot Game")]
    public void StartBotGame()
    {
        if (!isBotModeEnabled)
        {
            
            return;
        }

        if (playerCount != 2)
        {
            
            return;
        }

        
        StartGame(2);
    }


}
