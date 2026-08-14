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
    
    // Single Player Mode UI References
    [SerializeField] private GameObject runSettingsPanel;
    [SerializeField] private TMP_InputField seedInputField;
    [SerializeField] private TMP_Dropdown deckClassDropdown;
    [SerializeField] private Button startSinglePlayerButton;
    [SerializeField] private Button continueSavedRunButton; // Continue button for saved runs
    
    private Coroutine joinCodeAnimationCoroutine;
    
    [SerializeField] private BreathingAnimation settingButtonBreathing;
    [SerializeField] private BreathingAnimation undoButtonBreathing;
    [SerializeField] private GameObject visualElementsHolder;

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

        // Wire up single player button if assigned
        if (startSinglePlayerButton != null)
        {
            startSinglePlayerButton.onClick.AddListener(OnStartSinglePlayerButtonClicked);
        }

        // Wire up continue button if assigned
        if (continueSavedRunButton != null)
        {
            continueSavedRunButton.onClick.AddListener(OnContinueSavedRunButtonClicked);
            // Update visibility based on saved run status
            UpdateContinueButtonVisibility();
        }

        // Ensure deck class dropdown has at least one option
        if (deckClassDropdown != null && deckClassDropdown.options.Count == 0)
        {
            deckClassDropdown.AddOptions(new List<string> { "Balanced" });
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
        
        // Update continue button visibility when quickplay UI is shown
        UpdateContinueButtonVisibility();
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

    // ===== SINGLE PLAYER MODE =====

    /// <summary>
    /// Move visualElementsHolder downward out of the screen.
    /// Called before the fade transition to create a smooth exit animation.
    /// Returns a coroutine that completes when the movement finishes.
    /// </summary>
    public IEnumerator MoveVisualElementsHolderDown(float duration = 0.8f, float moveDistance = 1500f)
    {
        if (visualElementsHolder == null)
        {
            Debug.LogWarning("[MainUIScript] visualElementsHolder is not assigned!");
            yield break;
        }

        RectTransform rectTransform = visualElementsHolder.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogWarning("[MainUIScript] visualElementsHolder does not have a RectTransform!");
            yield break;
        }

        Vector2 startPosition = rectTransform.anchoredPosition;
        Vector2 endPosition = startPosition + new Vector2(0, -moveDistance); // Move down

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / duration);
            
            // Use easing for smooth animation
            float easedTime = Mathf.Pow(normalizedTime, 2); // EaseIn
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, easedTime);

            yield return null;
        }

        // Ensure final position
        rectTransform.anchoredPosition = endPosition;
        Debug.Log($"[MainUIScript] visualElementsHolder movement complete");
    }

    /// <summary>
    /// Called when the Start Single Player button is pressed in the run settings panel.
    /// Reads the seed and deck class, then launches the single player run.
    /// </summary>
    public void OnStartSinglePlayerButtonClicked()
    {
        

        // Validate inputs
        if (seedInputField == null || deckClassDropdown == null)
        {
            
            return;
        }

        // Parse seed (if empty, use random)
        int seed = 0;
        if (!string.IsNullOrEmpty(seedInputField.text))
        {
            if (!int.TryParse(seedInputField.text, out seed))
            {
                
                seed = new System.Random().Next();
            }
        }
        else
        {
            seed = new System.Random().Next();
        }

        // Get deck class (default to first option if not set)
        string deckClassName = deckClassDropdown.options.Count > 0 
            ? deckClassDropdown.options[deckClassDropdown.value].text 
            : "Balanced";

        

        // Close quickplay UI
        if (quickPlayUI != null) quickPlayUI.SetActive(false);
        if (runSettingsPanel != null) runSettingsPanel.SetActive(false);

        // Launch the single player run
        if (SinglePlayerModeController.Instance != null)
        {
            SinglePlayerModeController.Instance.StartNewRun(seed, deckClassName);
        }
        else
        {
            
        }
    }

    /// <summary>
    /// Called when the Close button is pressed in the run settings panel.
    /// Returns to the quickplay selection screen.
    /// </summary>
    public void OnRunSettingsCloseButtonClicked()
    {
        
        if (runSettingsPanel != null) runSettingsPanel.SetActive(false);
        if (quickPlayUI != null) quickPlayUI.SetActive(true);
    }

    /// <summary>
    /// Called when the Continue Saved Run button is pressed.
    /// Resumes the previously saved single player run.
    /// </summary>
    public void OnContinueSavedRunButtonClicked()
    {
        

        // Check if there's actually a saved run
        if (!RunManager.HasSavedRunProgress())
        {
            
            return;
        }

        // Close quickplay UI
        if (quickPlayUI != null) quickPlayUI.SetActive(false);

        // Resume the saved run
        if (SinglePlayerModeController.Instance != null)
        {
            SinglePlayerModeController.Instance.ResumeSavedRun();
        }
        else
        {
            
        }
    }

    /// <summary>
    /// Update the visibility of the Continue button based on whether a saved run exists.
    /// Called when quickplay UI is shown.
    /// </summary>
    private void UpdateContinueButtonVisibility()
    {
        if (continueSavedRunButton == null) return;

        bool hasSavedRun = RunManager.HasSavedRunProgress();
        continueSavedRunButton.gameObject.SetActive(hasSavedRun);

        if (hasSavedRun)
        {
            
        }
        else
        {
            
        }
    }

    /// <summary>
    /// Opens the run settings panel for single player mode.
    /// Called by NetworkManagerUI when 1v1 quickplay button is clicked.
    /// </summary>
    public void ShowRunSettingsPanel()
    {
        
        if (runSettingsPanel != null)
        {
            runSettingsPanel.SetActive(true);
        }
        else
        {
            
        }
    }

    // ===== END SINGLE PLAYER MODE =====

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
        
        
        // STEP 1: Reset player's game state before disconnection
        ResetPlayerGameStateBeforeDisconnection();
        
        // STEP 2: Use the existing NetworkManagerUI disconnection system
        NetworkManagerUI networkManagerUI = FindObjectOfType<NetworkManagerUI>();
        
        if (networkManagerUI != null)
        {
            
            networkManagerUI.OnReturnToMainMenuButtonClicked();
        }
        else
        {
            
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
        
        
        // STEP 1: Reset Server singleton for fresh game start
        
        Server.ResetServerSingletonForMainMenu();
        
        // STEP 2: Close all UI popups/pages
        CloseAllUIPages();
        
        // STEP 3: Disconnect from network if connected
        DisconnectFromNetwork();
        
        // STEP 4: Reset UI state to main page
        ResetToMainPage();
        
        // STEP 5: Clear any pending network operations
        ClearPendingNetworkOperations();
        
        
    }

    /// <summary>
    /// Closes all open UI pages and popups
    /// </summary>
    private void CloseAllUIPages()
    {
        

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
        
        
    }

    /// <summary>
    /// Properly disconnects from any active network connections
    /// </summary>
    private void DisconnectFromNetwork()
    {
        
        
        // Shutdown NetworkManager if it's running
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsListening)
            {
                
                
                if (NetworkManager.Singleton.IsHost)
                {
                    
                    NetworkManager.Singleton.Shutdown();
                }
                else if (NetworkManager.Singleton.IsClient)
                {
                    
                    NetworkManager.Singleton.Shutdown();
                }
                else if (NetworkManager.Singleton.IsServer)
                {
                    
                    NetworkManager.Singleton.Shutdown();
                }
            }
            else
            {
                
            }
        }
        else
        {
            
        }
        
        // STEP 3: Also try to find and disconnect from any GameNetworkRelay
        var networkRelay = FindObjectOfType<GameNetworkRelay>();
        if (networkRelay != null)
        {
            
            // GameNetworkRelay will be cleaned up when NetworkManager shuts down
        }
        
        
    }

    /// <summary>
    /// Resets the UI to show only the main starting screen
    /// </summary>
    private void ResetToMainPage()
    {
        
        
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
        
        
    }

    /// <summary>
    /// Clears any pending network operations and resets network state
    /// </summary>
    private void ClearPendingNetworkOperations()
    {
        
        
        // CRITICAL FIX: Do NOT reset server state during disconnection
        // This was causing connectedPlayerCount to be reset to 0 before OnClientDisconnected() could run
        // The server needs to maintain its state for proper disconnection handling
        if (Server.Singleton != null)
        {
            
            // Server.Singleton.ResetAllServerVariables(); // COMMENTED OUT - causes double counting bug
        }
        
        // Reset any game manager state if it exists
        if (GameManager.LocalInstance != null)
        {
            
            GameManager.LocalInstance.ResetForNewRound();
        }
        
        // Reset move chains if they exist
        try
        {
            MoveChainIntegrator.ResetChains();
            
        }
        catch (System.Exception e)
        {
            
        }
        
        // Also reset the connection count manually to ensure clean state
        if (Server.Singleton != null)
        {
            
            Server.Singleton.ResetConnectionCount();
        }
        
        
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
        
        
        // Reset Server singleton for fresh game start
        
        Server.ResetServerSingletonForMainMenu();
        
        // Close waiting screen UI
        CloseAllUIPages();
        
        // Reset to main page
        ResetToMainPage();
        
        
    }

    /// <summary>
    /// Called when a disconnect is detected by NetworkManagerUI
    /// </summary>
    public void OnDisconnectDetected()
    {
        // NetworkManagerUI.OnClientDisconnected already performs the network teardown
        // (LeaveAsync + Shutdown) before calling this. Here we only do UI cleanup.
        
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
            
        }
        else
        {
            
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
            
        }
        else
        {
            
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
        
        
    }

    /// <summary>
    /// Applies the music volume to the music system
    /// </summary>
    /// <param name="volume">Volume value (0.0 to 1.0)</param>
    private void ApplyMusicVolume(float volume)
    {
        // TODO: Implement music volume control when music system is added
        // For now, this is a placeholder for future music implementation
        
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
            
            
            // Print current volume settings for debugging
            if (SoundMaster.Instance != null)
            {
                SoundMaster.Instance.PrintVolumeSettings();
            }
            else
            {
                
            }
            
            StartCoroutine(PlayMultipleCardDealSounds(soundController, 10));
        }
        else
        {
            
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
            
            
            // Wait 0.3 seconds between each sound
            yield return new WaitForSeconds(0.3f);
        }
        
        
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
            
            
        }
        else
        {
            
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
            
            
        }
        else
        {
            
        }
    }

    /// <summary>
    /// Handles the undo button click for both host and non-host players
    /// </summary>
    public void OnUndoButtonClicked()
    {
        if (DeckController.LocalInstance == null)
        {
            
            return;
        }

        bool isHost = NetworkManager.Singleton.IsHost;

        if (isHost)
        {
            
            // Host behavior: Execute the actual undo
            PerformActualUndo();

            // Stop breathing animations when host acts on the request
            if (settingButtonBreathing != null) settingButtonBreathing.StopAnimation();
            if (undoButtonBreathing != null) undoButtonBreathing.StopAnimation();
        }
        else
        {
            
            // Non-host behavior: Send correction request to host
            SendCorrectionRequestToHost();
        }
    }

    /// <summary>
    /// Performs the actual undo action (host only)
    /// </summary>
    private void PerformActualUndo()
    {
        
        
        if (GameManager.LocalInstance != null)
        {
            GameManager.LocalInstance.RequestRedoToPreviousState();
        }
        else
        {
            
        }
    }

    /// <summary>
    /// Sends a correction request to the host when a non-host player presses the undo button
    /// </summary>
    private void SendCorrectionRequestToHost()
    {
        
        
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            // Send RPC to host via GameNetworkRelay
            GameNetworkRelay relay = FindObjectOfType<GameNetworkRelay>();
            if (relay != null)
            {
                relay.SendCorrectionRequestToHostServerRPC();
                
                
                // Show local widget indicating request was sent
                ShowLocalCorrectionRequestSentWidget();
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
    /// Shows widget on non-host player's device indicating correction request was sent
    /// </summary>
    private void ShowLocalCorrectionRequestSentWidget()
    {
        
        
        string message = "Düzeltme talebi masa sahibine bildirildi";
        
        
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
        
        
        string message = "Başka bir oyuncu düzeltme talep ediyor";
        
        
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
        
        
        SuperPowerSpawner spawner = FindObjectOfType<SuperPowerSpawner>();
        if (spawner != null)
        {
            spawner.isDisconnectingCleanUp = true;
            
        }
        
        // STEP 1: Reset gold to starting amount
        ResetPlayerGoldToStarting();
        
        // STEP 2: Remove and destroy all superpower tokens
        RemoveAllSuperpowerTokens();
        
        
    }

    /// <summary>
    /// Resets the player's gold to the starting amount
    /// </summary>
    private void ResetPlayerGoldToStarting()
    {
        SuperPowerSpawner spawner = FindObjectOfType<SuperPowerSpawner>();
        if (spawner != null)
        {
            
            spawner.ResetGoldToStarting();
        }
        else
        {
            
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
            
            spawner.ClearAllSpawnedPowers();
        }
        else
        {
            
        }
    }

    #endregion
}
