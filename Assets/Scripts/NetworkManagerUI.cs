using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField]private MainUIScript mainUIScript;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button hostTwoPlayerButton;
    [SerializeField] private Button hostFourPlayerButton;
    [SerializeField] private Button quickPlayTwoPlayerButton;
    [SerializeField] private Button quickPlayFourPlayerButton;
    [SerializeField] private InputField inputField;
    [SerializeField] private Text joinCodeText;
    [SerializeField] private Button returnToMainMenuButton; // Return button for waiting screen
    private string joinCodeVar;

    private bool isInGame = false; // Whether we're currently in an active game

    // === SESSIONS API ===
    private ISession currentSession;
    public ISession CurrentSession => currentSession;

    // === UI REFERENCES ===
    private GameObject mainScreen;

    void Awake()
    {
        SetupButtonListeners();
    }

    private void InitializeMainScreenReference()
    {
        mainScreen = GameObject.Find("MainScreen");
        if (mainScreen != null) Debug.Log("[NetworkManagerUI] Main screen reference initialized successfully");
        else Debug.LogError("[NetworkManagerUI] Main screen GameObject not found!");
    }

    async void Start()
    {
        InitializeMainScreenReference();
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        await EnsureServicesReady();
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private async Task EnsureServicesReady()
    {
        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized) await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (Exception e) { Debug.LogError($"[NetworkManagerUI] EnsureServicesReady failed: {e.Message}"); }
    }

    public async Task<string> StartHostWithRelay(int playerCount, bool privateFlag, bool isQuickPlay = false)
    {
        if (Server.Singleton != null) 
        {
            Server.Singleton.ResetAllServerVariables();
            Server.Singleton.SetBotModeEnabled(isQuickPlay);
        }
        if (GameManager.LocalInstance != null) GameManager.LocalInstance.ResetForNewGame();
        await EnsureServicesReady();
        try
        {
            var options = new SessionOptions { MaxPlayers = playerCount, IsPrivate = privateFlag }.WithRelayNetwork();
            currentSession = await MultiplayerService.Instance.CreateSessionAsync(options);
            joinCodeText.text = currentSession.Code;
            if (mainUIScript != null) mainUIScript.OpenWaitingScreenUI(privateFlag ? "blue" : "red", playerCount.ToString(), currentSession.Code);
            if (Server.Singleton != null) Server.Singleton.SetPlayerCount(playerCount);
            isInGame = true;
            return currentSession.Code;
        }
        catch (Exception e) { Debug.LogError($"[NetworkManagerUI] Failed to start host: {e.Message}"); return null; }
    }

    public async Task<bool> StartClientWithRelay()
    {
        string inputJoinCode = inputField.text;
        if (string.IsNullOrEmpty(inputJoinCode)) return false;
        if (mainUIScript != null) mainUIScript.OpenWaitingScreenUI("yellow", "2", inputJoinCode);
        await EnsureServicesReady();
        try
        {
            currentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(inputJoinCode);
            isInGame = true;
            return true;
        }
        catch (Exception e) { Debug.LogError($"[NetworkManagerUI] Failed to join session: {e.Message}"); if (mainUIScript != null) mainUIScript.OnDisconnectDetected(); return false; }
    }

    public async Task FindLobbiesAndStartHostIfNoneExist(int playerCount)
    {
        if (mainUIScript != null) mainUIScript.OpenWaitingScreenUI("red", playerCount.ToString(), "Quick Play...");
        await EnsureServicesReady();
        try
        {
            var queryOptions = new QuerySessionsOptions 
            {
                FilterOptions = new List<FilterOption> 
                {
                    new FilterOption(FilterField.MaxPlayers, playerCount.ToString(), FilterOperation.Equal),
                    new FilterOption(FilterField.AvailableSlots, "0", FilterOperation.Greater)
                }
            };
            var queryResponse = await MultiplayerService.Instance.QuerySessionsAsync(queryOptions);
            if (queryResponse.Sessions.Count > 0)
            {
                currentSession = await MultiplayerService.Instance.JoinSessionByIdAsync(queryResponse.Sessions[0].Id);
                isInGame = true;
            }
            else await StartHostWithRelay(playerCount, false, true);
        }
        catch (Exception e) { Debug.LogError($"[NetworkManagerUI] QuickPlay failed: {e.Message}"); if (mainUIScript != null) mainUIScript.OnDisconnectDetected(); }
    }

    private void OnClientConnected(ulong clientId) { isInGame = true; }
    private void OnClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId || clientId == NetworkManager.ServerClientId) PerformDisconnect();
    }

    public void OnReturnToMainMenuButtonClicked() { PerformDisconnect(); }

    private async void PerformDisconnect()
    {
        isInGame = false;
        ResetPlayerGameStateBeforeDisconnection();
        EnsureMainScreenIsActive();
        if (DeckController.LocalInstance != null) DeckController.LocalInstance.DestroyAllCards();
        try { if (currentSession != null) { await currentSession.LeaveAsync(); currentSession = null; } }
        catch (Exception e) { Debug.LogWarning($"[NetworkManagerUI] Error leaving session: {e.Message}"); }
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) NetworkManager.Singleton.Shutdown();
        if (mainUIScript != null) mainUIScript.OnReturnFromWaitingScreen();
    }

    private void ResetPlayerGameStateBeforeDisconnection()
    {
        SuperPowerSpawner spawner = FindObjectOfType<SuperPowerSpawner>();
        if (spawner != null) { spawner.ResetGoldToStarting(); spawner.ClearAllSpawnedPowers(); }
    }

    private void EnsureMainScreenIsActive()
    {
        if (mainScreen != null) mainScreen.SetActive(true);
        if (mainUIScript != null)
        {
            var field = mainUIScript.GetType().GetField("startingScreenUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                GameObject ui = field.GetValue(mainUIScript) as GameObject;
                if (ui != null) ui.SetActive(true);
            }
        }
    }

    void SetupButtonListeners()
    {
        clientButton.onClick.AddListener(async () => await StartClientWithRelay());
        hostTwoPlayerButton.onClick.AddListener(async () => await StartHostWithRelay(2, true, false));
        hostFourPlayerButton.onClick.AddListener(async () => await StartHostWithRelay(4, true, false));
        quickPlayTwoPlayerButton.onClick.AddListener(async () => await FindLobbiesAndStartHostIfNoneExist(2));
        quickPlayFourPlayerButton.onClick.AddListener(async () => await FindLobbiesAndStartHostIfNoneExist(4));
        if (returnToMainMenuButton != null) returnToMainMenuButton.onClick.AddListener(OnReturnToMainMenuButtonClicked);
    }
}