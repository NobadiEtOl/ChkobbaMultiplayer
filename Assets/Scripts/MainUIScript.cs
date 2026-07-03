using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class MainUIScript : MonoBehaviour
{
    [SerializeField]private ProfileScript profileScript;
    [SerializeField] private GameObject startingScreenUI;
    [SerializeField] private GameObject waitingScreenUI;
    [SerializeField] private GameObject quickPlayUI;
    [SerializeField] private GameObject createRoomUI;
    [SerializeField] private GameObject findRoomUI;
    [SerializeField] private GameObject profileUI;
    [SerializeField] private GameObject settingsPopup;
    [SerializeField] private GameObject leaveGameButton;
    [SerializeField] private GameObject undoButton;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider soundEffectsVolumeSlider;
    [SerializeField] private GameObject[] currentMode1v1;
    [SerializeField] private GameObject[] currentMode2v2;
    [SerializeField] private GameObject currentModeYellow;
    [SerializeField] private GameObject mainScreen;
    
    private Coroutine joinCodeAnimationCoroutine;
    
    [SerializeField] private BreathingAnimation settingButtonBreathing;
    [SerializeField] private BreathingAnimation undoButtonBreathing;

    // Start is called before the first frame update
    void Start()
    {
        InitializeVolumeSliders();
        
        // Auto-assign if not set (fallback)
        if (settingButtonBreathing == null)
        {
            GameObject sb = GameObject.Find("UICanvas/SettingButton");
            if (sb != null) settingButtonBreathing = sb.GetComponent<BreathingAnimation>();
        }
        if (undoButtonBreathing == null)
        {
            if (undoButton != null) undoButtonBreathing = undoButton.GetComponent<BreathingAnimation>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnQuickPlayButtonClicked()
    {
        quickPlayUI.SetActive(true);
        startingScreenUI.SetActive(false);
    }
    public void OnCreateRoomButtonClicked()
    {
        createRoomUI.SetActive(true);
        startingScreenUI.SetActive(false);
    }
    public void OnFindRoomButtonClicked()
    {
        findRoomUI.SetActive(true);
        startingScreenUI.SetActive(false);
    }

    public void OnProfileButtonClicked()
    {
        profileUI.SetActive(true);
        startingScreenUI.SetActive(false);
        gameObject.GetComponent<ProfileScript>().UpdateMatchCountText();
    }

    public void OnSettingsButtonClicked()
    {
        settingsPopup.SetActive(true);
        startingScreenUI.SetActive(false);
        UpdateLeaveGameButtonVisibility();
        UpdateUndoButtonVisibility();
    }

    public void OnQuickPlayCloseButtonClicked()
    {
        Debug.Log("Quick Play Close Button Clicked");
        quickPlayUI.SetActive(false);
        startingScreenUI.SetActive(true);
    }
    public void OnCreateRoomCloseButtonClicked()
    {
        createRoomUI.SetActive(false);
        startingScreenUI.SetActive(true);
    }
    public void OnFindRoomCloseButtonClicked()
    {
        findRoomUI.SetActive(false);
        startingScreenUI.SetActive(true);
    }

    public void OnProfileCloseButtonClicked()
    {
        profileUI.SetActive(false);
        startingScreenUI.SetActive(true);
        profileScript.ResetCardBackShowcase();
        profileScript.ResetProfilePicShowcase();
    }

    public void OnSettingsCloseButtonClicked()
    {
        settingsPopup.SetActive(false);
        startingScreenUI.SetActive(true);
        //UpdateUndoButtonVisibility(); // Deactivate undo button when settings popup is closed
    }

    /// <summary>
    /// Public method to be connected to the Undo/BirHamleGeriAl button's onClick event in Inspector
    /// This is the entry point for the undo button
    /// </summary>
    public void OnUndoButtonPressedFromUI()
    {
        OnUndoButtonClicked();
    }

    public void OnLeaveGameButtonClicked()
    {
        Debug.Log("[MainUIScript] Leave Game button clicked - resetting player state before disconnection");
        
        // STEP 1: Reset player's game state before disconnection
        ResetPlayerGameStateBeforeDisconnection();
        
        // STEP 2: Use the existing NetworkManagerUI disconnection system
        NetworkManagerUI networkManagerUI = FindObjectOfType<NetworkManagerUI>();
        
        if (networkManagerUI != null)
        {
            Debug.Log("[MainUIScript] Using NetworkManagerUI.OnReturnToMainMenuButtonClicked() for proper disconnection");
            networkManagerUI.OnReturnToMainMenuButtonClicked();
        }
        else
        {
            Debug.LogError("[MainUIScript] NetworkManagerUI not found - falling back to basic return to main page");
            ReturnToMainPage();
        }
    }

    public void OpenWaitingScreenUI(string color, string playerCount, string joinCode)
    {
        if (joinCodeAnimationCoroutine != null)
        {
            StopCoroutine(joinCodeAnimationCoroutine);
            joinCodeAnimationCoroutine = null;
        }

        if(startingScreenUI.activeSelf)startingScreenUI.SetActive(false);
        waitingScreenUI.SetActive(true);
        
        // Deactivate quick play, create room, and find room UIs when opening waiting screen
        quickPlayUI.SetActive(false);
        createRoomUI.SetActive(false);
        findRoomUI.SetActive(false);
        
        // Update leave game button visibility since we're entering a game
        UpdateLeaveGameButtonVisibility();

        if(color == "red")
        {
            if(playerCount == "2")
            {
                currentMode1v1[0].SetActive(true);
            }
            else if(playerCount == "4")
            {
                currentMode2v2[0].SetActive(true);
            }
        }
        else if(color == "blue")
        {
            if (playerCount == "2")
            {
                currentMode1v1[1].SetActive(true);
                Text txt = currentMode1v1[1].transform.GetChild(0).gameObject.GetComponent<Text>();
                if (txt != null)
                {
                    if (joinCode == "YÜKLENİYOR")
                    {
                        joinCodeAnimationCoroutine = StartCoroutine(AnimateJoinCode(txt));
                    }
                    else
                    {
                        txt.text = joinCode;
                    }
                }
            }
            else if (playerCount == "4")
            {
                currentMode2v2[1].SetActive(true);
                Text txt = currentMode2v2[1].transform.GetChild(0).gameObject.GetComponent<Text>();
                if (txt != null)
                {
                    if (joinCode == "YÜKLENİYOR")
                    {
                        joinCodeAnimationCoroutine = StartCoroutine(AnimateJoinCode(txt));
                    }
                    else
                    {
                        txt.text = joinCode;
                    }
                }
            }
        }
        else if(color == "yellow")
        {
            currentModeYellow.SetActive(true);
            currentModeYellow.transform.GetChild(0).gameObject.GetComponent<Text>().text = joinCode;
        }
    }

    private IEnumerator AnimateJoinCode(Text textComponent)
    {
        if (textComponent == null) yield break;
        
        while (true)
        {
            textComponent.text = ".";
            yield return new WaitForSeconds(0.5f);
            textComponent.text = "..";
            yield return new WaitForSeconds(0.5f);
            textComponent.text = "...";
            yield return new WaitForSeconds(0.5f);
        }
    }

    /// <summary>
    /// Context menu method to return to main page while properly closing all network connections
    /// </summary>
    [ContextMenu("Return to Main Page (Close All Connections)")]
    public void ReturnToMainPage()
    {
        Debug.Log("[MainUIScript] Returning to main page and closing all network connections...");
        
        // STEP 1: Reset Server singleton for fresh game start
        Debug.Log("[MainUIScript] Resetting Server singleton for fresh game start");
        Server.ResetServerSingletonForMainMenu();
        
        // STEP 2: Close all UI popups/pages
        CloseAllUIPages();
        
        // STEP 3: Disconnect from network if connected
        DisconnectFromNetwork();
        
        // STEP 4: Reset UI state to main page
        ResetToMainPage();
        
        // STEP 5: Clear any pending network operations
        ClearPendingNetworkOperations();
        
        Debug.Log("[MainUIScript] Successfully returned to main page with clean network state");
    }

    /// <summary>
    /// Closes all open UI pages and popups
    /// </summary>
    private void CloseAllUIPages()
    {
        Debug.Log("[MainUIScript] Closing all UI pages...");

        if (joinCodeAnimationCoroutine != null)
        {
            StopCoroutine(joinCodeAnimationCoroutine);
            joinCodeAnimationCoroutine = null;
        }
        
        // Close all popup UIs
        quickPlayUI.SetActive(false);
        createRoomUI.SetActive(false);
        findRoomUI.SetActive(false);
        profileUI.SetActive(false);
        settingsPopup.SetActive(false);
        waitingScreenUI.SetActive(false);
        
        // Close all mode-specific UIs
        foreach (var mode in currentMode1v1)
        {
            if (mode != null) mode.SetActive(false);
        }
        
        foreach (var mode in currentMode2v2)
        {
            if (mode != null) mode.SetActive(false);
        }
        
        if (currentModeYellow != null) currentModeYellow.SetActive(false);
        
        // Update button visibility after closing all pages
        UpdateLeaveGameButtonVisibility();
        
        Debug.Log("[MainUIScript] All UI pages closed");
    }

    /// <summary>
    /// Properly disconnects from any active network connections
    /// </summary>
    private void DisconnectFromNetwork()
    {
        Debug.Log("[MainUIScript] Disconnecting from network...");
        
        // Shutdown NetworkManager if it's running
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsListening)
            {
                Debug.Log("[MainUIScript] NetworkManager is listening, shutting down...");
                
                if (NetworkManager.Singleton.IsHost)
                {
                    Debug.Log("[MainUIScript] Shutting down as Host...");
                    NetworkManager.Singleton.Shutdown();
                }
                else if (NetworkManager.Singleton.IsClient)
                {
                    Debug.Log("[MainUIScript] Shutting down as Client...");
                    NetworkManager.Singleton.Shutdown();
                }
                else if (NetworkManager.Singleton.IsServer)
                {
                    Debug.Log("[MainUIScript] Shutting down as Server...");
                    NetworkManager.Singleton.Shutdown();
                }
            }
            else
            {
                Debug.Log("[MainUIScript] NetworkManager is not listening, no need to shutdown");
            }
        }
        else
        {
            Debug.Log("[MainUIScript] NetworkManager.Singleton is null");
        }
        
        // STEP 3: Also try to find and disconnect from any GameNetworkRelay
        var networkRelay = FindObjectOfType<GameNetworkRelay>();
        if (networkRelay != null)
        {
            Debug.Log("[MainUIScript] Found GameNetworkRelay, ensuring clean state");
            // GameNetworkRelay will be cleaned up when NetworkManager shuts down
        }
        
        Debug.Log("[MainUIScript] Network disconnection complete");
    }

    /// <summary>
    /// Resets the UI to show only the main starting screen
    /// </summary>
    private void ResetToMainPage()
    {
        Debug.Log("[MainUIScript] Resetting to main page...");
        
        // Show the main starting screen
        startingScreenUI.SetActive(true);
        
        // Ensure all other screens are hidden
        waitingScreenUI.SetActive(false);
        quickPlayUI.SetActive(false);
        createRoomUI.SetActive(false);
        findRoomUI.SetActive(false);
        profileUI.SetActive(false);
        settingsPopup.SetActive(false);
        
        // Update button visibility since we're back to main screen
        UpdateLeaveGameButtonVisibility();
        
        Debug.Log("[MainUIScript] UI reset to main page complete");
    }

    /// <summary>
    /// Clears any pending network operations and resets network state
    /// </summary>
    private void ClearPendingNetworkOperations()
    {
        Debug.Log("[MainUIScript] Clearing pending network operations...");
        
        // CRITICAL FIX: Do NOT reset server state during disconnection
        // This was causing connectedPlayerCount to be reset to 0 before OnClientDisconnected() could run
        // The server needs to maintain its state for proper disconnection handling
        if (Server.Singleton != null)
        {
            Debug.Log("[MainUIScript] NOT resetting Server state during disconnection - server handles its own cleanup");
            // Server.Singleton.ResetAllServerVariables(); // COMMENTED OUT - causes double counting bug
        }
        
        // Reset any game manager state if it exists
        if (GameManager.LocalInstance != null)
        {
            Debug.Log("[MainUIScript] Resetting GameManager state...");
            GameManager.LocalInstance.ResetForNewRound();
        }
        
        // Reset move chains if they exist
        try
        {
            MoveChainIntegrator.ResetChains();
            Debug.Log("[MainUIScript] Move chains reset");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[MainUIScript] Could not reset move chains: {e.Message}");
        }
        
        // Also reset the connection count manually to ensure clean state
        if (Server.Singleton != null)
        {
            Debug.Log("[MainUIScript] Manually resetting Server connection count...");
            Server.Singleton.ResetConnectionCount();
        }
        
        Debug.Log("[MainUIScript] Pending network operations cleared");
    }

    /// <summary>
    /// Public method that can be called from UI buttons to return to main page
    /// </summary>
    public void OnReturnToMainPageButtonClicked()
    {
        ReturnToMainPage();
    }

    /// <summary>
    /// Called when returning from waiting screen - handles proper disconnection
    /// </summary>
    public void OnReturnFromWaitingScreen()
    {
        Debug.Log("[MainUIScript] Returning from waiting screen - cleaning up network state...");
        
        // Reset Server singleton for fresh game start
        Debug.Log("[MainUIScript] Resetting Server singleton for fresh game start");
        Server.ResetServerSingletonForMainMenu();
        
        // Close waiting screen UI
        CloseAllUIPages();
        
        // Reset to main page
        ResetToMainPage();
        
        Debug.Log("[MainUIScript] Successfully returned from waiting screen to main page");
    }

    /// <summary>
    /// Called when a disconnect is detected by NetworkManagerUI
    /// </summary>
    public void OnDisconnectDetected()
    {
        // NetworkManagerUI.OnClientDisconnected already performs the network teardown
        // (LeaveAsync + Shutdown) before calling this. Here we only do UI cleanup.
        Debug.Log("[MainUIScript] Disconnect detected - returning to main page");
        CloseAllUIPages();
        ResetToMainPage();
    }

    #region Volume Control Methods

    /// <summary>
    /// Initializes the volume sliders with current values and sets up event listeners
    /// </summary>
    private void InitializeVolumeSliders()
    {
        // Initialize music volume slider
        if (musicVolumeSlider != null)
        {
            // Set initial value from PlayerPrefs or default to 0.5
            float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
            musicVolumeSlider.value = musicVolume;
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            Debug.Log($"[MainUIScript] Music volume slider initialized with value: {musicVolume}");
        }
        else
        {
            Debug.LogWarning("[MainUIScript] Music volume slider is not assigned!");
        }

        // Initialize sound effects volume slider
        if (soundEffectsVolumeSlider != null)
        {
            // Set initial value from PlayerPrefs or default to 0.5
            float soundEffectsVolume = PlayerPrefs.GetFloat("SoundEffectsVolume", 0.5f);
            soundEffectsVolumeSlider.value = soundEffectsVolume;
            soundEffectsVolumeSlider.onValueChanged.AddListener(OnSoundEffectsVolumeChanged);
            
            // Apply the volume to the sound effects controller
            ApplySoundEffectsVolume(soundEffectsVolume);
            Debug.Log($"[MainUIScript] Sound effects volume slider initialized with value: {soundEffectsVolume}");
        }
        else
        {
            Debug.LogWarning("[MainUIScript] Sound effects volume slider is not assigned!");
        }
    }

    /// <summary>
    /// Called when music volume slider value changes
    /// </summary>
    /// <param name="volume">New volume value (0.0 to 1.0)</param>
    public void OnMusicVolumeChanged(float volume)
    {
        // Save the volume setting
        PlayerPrefs.SetFloat("MusicVolume", volume);
        PlayerPrefs.Save();
        
        // Apply volume to music system (when implemented)
        ApplyMusicVolume(volume);
        
        Debug.Log($"[MainUIScript] Music volume changed to: {volume}");
    }

    /// <summary>
    /// Called when sound effects volume slider value changes
    /// </summary>
    /// <param name="volume">New volume value (0.0 to 1.0)</param>
    public void OnSoundEffectsVolumeChanged(float volume)
    {
        // Save the volume setting
        PlayerPrefs.SetFloat("SoundEffectsVolume", volume);
        PlayerPrefs.Save();
        
        // Apply volume to sound effects system
        ApplySoundEffectsVolume(volume);
        
        Debug.Log($"[MainUIScript] Sound effects volume changed to: {volume}");
    }

    /// <summary>
    /// Applies the music volume to the music system
    /// </summary>
    /// <param name="volume">Volume value (0.0 to 1.0)</param>
    private void ApplyMusicVolume(float volume)
    {
        // TODO: Implement music volume control when music system is added
        // For now, this is a placeholder for future music implementation
        Debug.Log($"[MainUIScript] Music volume applied: {volume} (music system not yet implemented)");
    }

    /// <summary>
    /// Applies the sound effects volume to the sound effects system
    /// </summary>
    /// <param name="volume">Volume value (0.0 to 1.0)</param>
    private void ApplySoundEffectsVolume(float volume)
    {
        // Use SoundMaster if available, otherwise fall back to direct control
        if (SoundMaster.Instance != null)
        {
            SoundMaster.Instance.SetSoundEffectsVolume(volume);
            Debug.Log($"[MainUIScript] Sound effects volume set via SoundMaster: {volume}");
        }
        else
        {
            // Fallback: Find and update all SoundEffectsController instances directly
            SoundEffectsController[] soundControllers = FindObjectsOfType<SoundEffectsController>();
            foreach (var controller in soundControllers)
            {
                if (controller != null)
                {
                    controller.SetVolume(volume);
                }
            }
            Debug.Log($"[MainUIScript] Sound effects volume set directly to {soundControllers.Length} controllers: {volume}");
        }
    }

    /// <summary>
    /// Gets the current music volume from PlayerPrefs
    /// </summary>
    /// <returns>Current music volume (0.0 to 1.0)</returns>
    public float GetMusicVolume()
    {
        return PlayerPrefs.GetFloat("MusicVolume", 0.5f);
    }

    /// <summary>
    /// Gets the current sound effects volume from PlayerPrefs
    /// </summary>
    /// <returns>Current sound effects volume (0.0 to 1.0)</returns>
    public float GetSoundEffectsVolume()
    {
        return PlayerPrefs.GetFloat("SoundEffectsVolume", 0.5f);
    }

    /// <summary>
    /// Context menu to test card deal sound with current volume settings
    /// </summary>
    [ContextMenu("Test Card Deal Sound")]
    public void TestCardDealSound()
    {
        // Find a SoundEffectsController in the scene
        SoundEffectsController soundController = FindObjectOfType<SoundEffectsController>();
        
        if (soundController != null)
        {
            Debug.Log("[MainUIScript] Playing 10 card deal sounds for volume testing...");
            
            // Print current volume settings for debugging
            if (SoundMaster.Instance != null)
            {
                SoundMaster.Instance.PrintVolumeSettings();
            }
            else
            {
                Debug.Log($"[MainUIScript] No SoundMaster found. Using direct volume control.");
            }
            
            StartCoroutine(PlayMultipleCardDealSounds(soundController, 10));
        }
        else
        {
            Debug.LogWarning("[MainUIScript] No SoundEffectsController found in the scene! Make sure you have one in your game.");
        }
    }

    /// <summary>
    /// Coroutine to play multiple card deal sounds with delays
    /// </summary>
    /// <param name="soundController">The SoundEffectsController to use</param>
    /// <param name="count">Number of sounds to play</param>
    private System.Collections.IEnumerator PlayMultipleCardDealSounds(SoundEffectsController soundController, int count)
    {
        for (int i = 0; i < count; i++)
        {
            soundController.PlayCardDealSound();
            Debug.Log($"[MainUIScript] Playing card deal sound {i + 1}/{count}");
            
            // Wait 0.3 seconds between each sound
            yield return new WaitForSeconds(0.3f);
        }
        
        Debug.Log("[MainUIScript] Finished playing all test sounds");
    }

    /// <summary>
    /// Updates the leave game button visibility based on whether main screen is active
    /// </summary>
    private void UpdateLeaveGameButtonVisibility()
    {
        if (leaveGameButton != null)
        {
            // Leave game button is active when main screen is NOT active (i.e., when in game)
            bool shouldShowLeaveGameButton = !mainScreen.activeSelf;
            leaveGameButton.SetActive(shouldShowLeaveGameButton);
            
            Debug.Log($"[MainUIScript] Leave game button {(shouldShowLeaveGameButton ? "shown" : "hidden")} - Main screen active: {startingScreenUI.activeSelf}");
        }
        else
        {
            Debug.LogWarning("[MainUIScript] Leave game button is not assigned!");
        }
    }

    /// <summary>
    /// Updates the undo button visibility based on main screen state
    /// Now shows for all players - behavior differs based on host status
    /// </summary>
    private void UpdateUndoButtonVisibility()
    {
        if (undoButton != null)
        {
            // Undo button is active when in game, for all players
            // For non-hosts, it will send a correction request to the host
            bool isInGame = !mainScreen.activeSelf;
            
            undoButton.SetActive(isInGame);
            
            Debug.Log($"[MainUIScript] Undo button {(isInGame ? "shown" : "hidden")} - In game: {isInGame}");
        }
        else
        {
            Debug.LogWarning("[MainUIScript] Undo button is not assigned!");
        }
    }

    /// <summary>
    /// Handles the undo button click for both host and non-host players
    /// </summary>
    public void OnUndoButtonClicked()
    {
        if (DeckController.LocalInstance == null)
        {
            Debug.LogWarning("[MainUIScript] DeckController.LocalInstance is null - cannot process undo");
            return;
        }

        bool isHost = NetworkManager.Singleton.IsHost;

        if (isHost)
        {
            Debug.Log("[MainUIScript] Host pressed undo button - performing undo action");
            // Host behavior: Execute the actual undo
            PerformActualUndo();

            // Stop breathing animations when host acts on the request
            if (settingButtonBreathing != null) settingButtonBreathing.StopAnimation();
            if (undoButtonBreathing != null) undoButtonBreathing.StopAnimation();
        }
        else
        {
            Debug.Log("[MainUIScript] Non-host player pressed undo button - sending correction request to host");
            // Non-host behavior: Send correction request to host
            SendCorrectionRequestToHost();
        }
    }

    /// <summary>
    /// Performs the actual undo action (host only)
    /// </summary>
    private void PerformActualUndo()
    {
        Debug.Log("[MainUIScript] Performing undo - delegating to GameManager");
        
        if (GameManager.LocalInstance != null)
        {
            GameManager.LocalInstance.RequestRedoToPreviousState();
        }
        else
        {
            Debug.LogError("[MainUIScript] GameManager.LocalInstance is null - cannot perform undo");
        }
    }

    /// <summary>
    /// Sends a correction request to the host when a non-host player presses the undo button
    /// </summary>
    private void SendCorrectionRequestToHost()
    {
        Debug.Log("[MainUIScript] Non-host player sending correction request to host...");
        
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            // Send RPC to host via GameNetworkRelay
            GameNetworkRelay relay = FindObjectOfType<GameNetworkRelay>();
            if (relay != null)
            {
                relay.SendCorrectionRequestToHostServerRPC();
                Debug.Log("[MainUIScript] Correction request RPC sent to host");
                
                // Show local widget indicating request was sent
                ShowLocalCorrectionRequestSentWidget();
            }
            else
            {
                Debug.LogError("[MainUIScript] GameNetworkRelay not found - cannot send correction request");
            }
        }
        else
        {
            Debug.LogWarning("[MainUIScript] Not connected to network - cannot send correction request");
        }
    }

    /// <summary>
    /// Shows widget on non-host player's device indicating correction request was sent
    /// </summary>
    private void ShowLocalCorrectionRequestSentWidget()
    {
        Debug.Log("[MainUIScript] Showing 'correction request sent' widget on non-host device...");
        
        string message = "Düzeltme talebi masa sahibine bildirildi";
        Debug.Log($"[MainUIScript] CORRECTION REQUEST SENT: {message}");
        
        if (UIFeedbackManager.Instance != null)
        {
            UIFeedbackManager.Instance.ShowFeedback(message);
        }
    }

    /// <summary>
    /// Shows the correction request widget on the host's device
    /// This is called via RPC when a non-host player requests a correction
    /// </summary>
    public void ShowCorrectionRequestWidgetOnHost()
    {
        Debug.Log("[MainUIScript] Showing correction request widget on host device...");
        
        string message = "Başka bir oyuncu düzeltme talep ediyor";
        Debug.Log($"[MainUIScript] CORRECTION REQUEST RECEIVED: {message}");
        
        if (UIFeedbackManager.Instance != null)
        {
            // Show for a slightly longer duration on the host device
            UIFeedbackManager.Instance.ShowFeedback(message, 3.0f);
        }

        // Start breathing animations on host buttons to draw attention
        if (settingButtonBreathing != null) settingButtonBreathing.StartAnimation();
        if (undoButtonBreathing != null) undoButtonBreathing.StartAnimation();
    }

    /// <summary>
    /// Resets the player's game state before disconnection (gold and superpower tokens)
    /// </summary>
    private void ResetPlayerGameStateBeforeDisconnection()
    {
        Debug.Log("[MainUIScript] Resetting player game state before disconnection...");
        
        SuperPowerSpawner spawner = FindObjectOfType<SuperPowerSpawner>();
        if (spawner != null)
        {
            spawner.isDisconnectingCleanUp = true;
            Debug.Log("[MainUIScript] Set isDisconnectingCleanUp = true on spawner to safeguard server state");
        }
        
        // STEP 1: Reset gold to starting amount
        ResetPlayerGoldToStarting();
        
        // STEP 2: Remove and destroy all superpower tokens
        RemoveAllSuperpowerTokens();
        
        Debug.Log("[MainUIScript] Player game state reset complete - ready for disconnection");
    }

    /// <summary>
    /// Resets the player's gold to the starting amount
    /// </summary>
    private void ResetPlayerGoldToStarting()
    {
        SuperPowerSpawner spawner = FindObjectOfType<SuperPowerSpawner>();
        if (spawner != null)
        {
            Debug.Log("[MainUIScript] Resetting gold to starting amount before disconnection");
            spawner.ResetGoldToStarting();
        }
        else
        {
            Debug.LogWarning("[MainUIScript] SuperPowerSpawner not found - cannot reset gold");
        }
    }

    /// <summary>
    /// Removes and destroys all superpower tokens
    /// </summary>
    private void RemoveAllSuperpowerTokens()
    {
        SuperPowerSpawner spawner = FindObjectOfType<SuperPowerSpawner>();
        if (spawner != null)
        {
            Debug.Log("[MainUIScript] Removing all superpower tokens before disconnection");
            spawner.ClearAllSpawnedPowers();
        }
        else
        {
            Debug.LogWarning("[MainUIScript] SuperPowerSpawner not found - cannot clear superpower tokens");
        }
    }

    #endregion
}
