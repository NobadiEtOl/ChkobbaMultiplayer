using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SuperPowerSpawner : MonoBehaviour
{
    [SerializeField] private Transform infoBoxOriginalPosition; // Original position stored at Start
    [SerializeField] private GameObject infoBoxReachPoint;
    public static SuperPowerSpawner LocalInstance { get; private set; }
    public bool isDisconnectingCleanUp = false;
    [SerializeField] private List<GameObject> superPowerTokens = new List<GameObject>();
    private Dictionary<SuperPower, GameObject> superPowerPrefabs = new Dictionary<SuperPower, GameObject>();
    private List<SuperPower> superPowerList = new List<SuperPower>(); // Now contains unique powers only (no pre-pooling)
    [SerializeField] private int maxSuperPowers = 3;
    private int numberOfSuperPowersToSpawn = 2;
    private List<GameObject> spawnedSuperPowers = new List<GameObject>();
    private Transform playerPowerPoolTransform;
    [SerializeField] private List<Transform> spawnPositions = new List<Transform>();
    [SerializeField] private GameObject centerGameObject;
    private Vector3 centerPosition;
    private GameObject backgroundPanel;
    private GameObject infoBoxCanvas;
    private TextMeshPro nameText;
    private TextMeshPro descriptionText;
    private Button activateButton;
    private Button falseActivateButton;
    private Button closeButton;

    [Header("Gold System")]
    [SerializeField] private TextMeshProUGUI goldDisplayText; // Gold display text
    [SerializeField] private int startingGold = 20; // Starting gold amount
    [SerializeField] private int maxGold = 15; // Maximum gold cap
    
    [Header("Gold Popup Animation")]
    [SerializeField] private Transform goldPopupLocation;
    [SerializeField] private GameObject goldPopupPrefab; // Prefab for gold popup text (will be created if null)
    [SerializeField] private float popupFadeDuration = 0.3f; // Duration for fade in/out
    [SerializeField] private float popupScaleDuration = 0.2f; // Duration for scale animation
    [SerializeField] private float popupMoveDuration = 1.0f; // Duration for upward movement
    [SerializeField] private float popupMoveDistance = 100f; // How far upward the popup moves
    [SerializeField] private float popupStartScale = 0.5f; // Starting scale of popup
    [SerializeField] private float popupEndScale = 1.2f; // Maximum scale before shrinking
    [SerializeField] private Color popupTextColor = Color.yellow; // Color of popup text
    [SerializeField] private int popupFontSize = 24; // Font size of popup text
    [SerializeField] private float cardProcessingDelay = 0.1f; // Delay between processing each card
    [SerializeField] private float popupDisplayDelay = 0.3f; // Delay between showing each popup in queue
    
    private int currentGold; // Current gold amount
    private int playerNumber; // Local player number
    
    // Gold popup queue system
    private Queue<int> goldPopupQueue = new Queue<int>();
    private bool isProcessingPopupQueue = false;

    
    [Header("InfoBox Animation Settings")]
    [SerializeField] private float animationDuration = 0.5f; // Duration of the movement animation
    [SerializeField] private float infoChangeDelay = 0.25f; // Delay before info changes (to match page change animation transition)
    [SerializeField] private float fadeDuration = 0.15f; // Duration of fade in/out animations
    [SerializeField] private float buttonFadeDuration = 0.3f; // Duration of button fade animations
    public bool isInfoBoxOpen = false; // Track if info box is currently open
    public bool isMenuPageOpen = false; // Track if menu page is currently open in InfoBox
    private Coroutine currentAnimationCoroutine; // Track current animation to prevent overlaps

    private Queue<IEnumerator> infoBoxAnimationQueue = new Queue<IEnumerator>();
    private bool isInfoBoxAnimationRunning = false;

    void Awake()
    {
        if (LocalInstance != null && LocalInstance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        LocalInstance = this;
        DontDestroyOnLoad(this.gameObject);

        centerPosition = centerGameObject.transform.position;
        GetUIElements();
    }

    public void NotifyServerOfGoldAndPowers()
    {
        if (isDisconnectingCleanUp)
        {
            Debug.Log("[SuperPowerSpawner] NotifyServerOfGoldAndPowers skipped - disconnection cleanup active");
            return;
        }

        if (GameNetworkRelay.Instance != null && DeckController.LocalInstance != null)
        {
            int myPlayerNo = DeckController.LocalInstance.thisPlayerNumber;
            if (myPlayerNo != -1)
            {
                List<string> powers = new List<string>();
                foreach (GameObject obj in spawnedSuperPowers)
                {
                    if (obj != null)
                    {
                        // Clean check: if it is a placeholder, skip or if it has a token script, extract name
                        SuperPowerToken token = obj.GetComponent<SuperPowerToken>();
                        if (token != null && token.power != null)
                        {
                            powers.Add(token.power.name);
                        }
                    }
                }
                GameNetworkRelay.Instance.SyncGoldAndPowersServerRPC(myPlayerNo, currentGold, powers.ToArray());
                Debug.Log($"[SuperPowerSpawner] Synced with server - Gold: {currentGold}, Powers Count: {powers.Count}");
            }
        }
    }

    public void SetGold(int amount)
    {
        currentGold = Mathf.Min(amount, maxGold);
        UpdateGoldDisplay();
        NotifyServerOfGoldAndPowers();
    }

    public void RestorePowers(List<string> powerNames)
    {
        // CRITICAL FIX: If the dictionary hasn't been initialized yet, initialize it now
        if (superPowerPrefabs == null || superPowerPrefabs.Count == 0)
        {
            Debug.Log("[SuperPowerSpawner] superPowerPrefabs is empty during RestorePowers. Initializing now...");
            InitializeSuperPowers();
        }

        // Temporarily set flag to avoid sending sync messages during clearing
        bool prevFlag = isDisconnectingCleanUp;
        isDisconnectingCleanUp = true;
        try
        {
            foreach (var power in spawnedSuperPowers.ToArray())
            {
                if (power != null)
                {
                    Destroy(power);
                }
            }
            spawnedSuperPowers.Clear();
        }
        finally
        {
            isDisconnectingCleanUp = prevFlag;
        }

        // Spawn each restored power
        foreach (string powerName in powerNames)
        {
            SuperPower targetPower = null;
            foreach (var power in superPowerPrefabs.Keys)
            {
                if (power != null && (power.name == powerName || power.GetType().Name == powerName))
                {
                    targetPower = power;
                    break;
                }
            }

            if (targetPower != null)
            {
                StartCoroutine(SpawnSuperPower(targetPower, null, 150f));
            }
            else
            {
                Debug.LogWarning($"[SuperPowerSpawner] RestorePowers: Could not find key for power: {powerName}");
            }
        }
    }

    void Start()
    {
        // Validate UI elements are properly initialized
        if (backgroundPanel == null || infoBoxCanvas == null || nameText == null || 
            descriptionText == null || activateButton == null || falseActivateButton == null || closeButton == null)
        {
            Debug.LogError("[SuperPowerSpawner] One or more UI elements are null after initialization. Attempting to re-initialize...");
            GetUIElements();
            
            // Check again after re-initialization
            if (backgroundPanel == null || infoBoxCanvas == null || nameText == null || 
                descriptionText == null || activateButton == null || falseActivateButton == null || closeButton == null)
            {
                Debug.LogError("[SuperPowerSpawner] UI elements still null after re-initialization. InfoBox functionality will be disabled.");
                return;
            }
        }
        
        // Validate reach point
        if (infoBoxReachPoint == null)
        {
            Debug.LogWarning("[SuperPowerSpawner] infoBoxReachPoint is not assigned! InfoBox will not animate to reach point.");
        }
        else
        {
            Debug.Log($"[SuperPowerSpawner] Reach point position: {infoBoxReachPoint.transform.position}");
        }
        
        // Initialize InfoBox state (closed but active)
        if (backgroundPanel != null)
        {
            // Ensure InfoBox is active but positioned at original (off-screen) position
            backgroundPanel.SetActive(true);
            backgroundPanel.transform.position = infoBoxOriginalPosition.position;
            
            // Start with idle animation disabled (InfoBox is "closed")
            UIFrameAnimator frameAnimator = backgroundPanel.GetComponent<UIFrameAnimator>();
            if (frameAnimator != null)
            {
                try
                {
                    frameAnimator.SetIdleAnimationEnabled(false);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[SuperPowerSpawner] Failed to disable idle animation in Start: {e.Message}");
                }
            }
        }
        
        // Initialize open button state (should be visible when InfoBox is closed)
        MenuController menuController = FindObjectOfType<MenuController>();
        if (menuController != null && menuController.OpenButtonGameObject != null)
        {
            menuController.OpenButtonGameObject.SetActive(true);
            CanvasGroup buttonCanvasGroup = GetOrAddCanvasGroup(menuController.OpenButtonGameObject);
            if (buttonCanvasGroup != null)
            {
                buttonCanvasGroup.alpha = 1f; // Ensure it's fully visible
            }
        }
        
        // Validate gold display text and popup location
        if (goldDisplayText == null)
        {
            Debug.LogWarning("[SuperPowerSpawner] goldDisplayText is not assigned! Gold display will not work.");
        }
        
        if (goldPopupLocation == null)
        {
            Debug.LogWarning("[SuperPowerSpawner] goldPopupLocation is not assigned! Gold popups will use fallback positioning.");
        }
        
        // Initialize gold system
        InitializeGoldSystem();
        
        // Get DeckController reference
        deckController = FindObjectOfType<DeckController>();
        if (deckController == null)
        {
            Debug.LogError("[SuperPowerSpawner] DeckController not found! Hand showcasing will not work.");
        }
    }

    void Update()
    {
        // Handle mouse input for WebGL
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Vector3 mousePosition = Input.mousePosition;
            Ray ray = Camera.main.ScreenPointToRay(mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                HandleTokenRaycast(hit);
            }
        }
    }

    private void HandleTokenRaycast(RaycastHit hit)
    {
        Debug.LogWarning("Object touched: " + hit.collider.gameObject.tag);
        Debug.LogWarning("Object name: " + hit.collider.gameObject.name);
        
        // Only handle token clicks - buttons now use proper Unity Button onClick events
        if (hit.collider.gameObject.tag == "Token")
        {
            SuperPowerToken superPowerToken = hit.collider.GetComponent<SuperPowerToken>();
            if (superPowerToken != null)
            {
                //StartCoroutine(OpenInfoBox(superPowerToken));
                return;
            }
        }
        
        // Note: Button clicks (including close button) are now handled by Unity Button onClick events
        // No need for raycast-based button handling anymore
        // InfoBox closing is now handled by the toggle system via the open button
    }

    private void GetUIElements()
    {
        Debug.LogError("🔧 [SuperPowerSpawner] GetUIElements() called!");
        backgroundPanel = GameObject.Find("InfoBoxBackGroundPanel")?.gameObject;
        
        if (backgroundPanel == null)
        {
            Debug.LogError("InfoBoxBackGroundPanel not found!");
            return;
        }
        
        infoBoxCanvas = backgroundPanel.transform.Find("InfoBoxCanvas")?.gameObject;
        
        if (infoBoxCanvas == null)
        {
            Debug.LogError("InfoBoxCanvas not found as child of InfoBoxBackGroundPanel!");
            return;
        }
        
        // Find UI elements within the InfoBoxCanvas
        nameText = infoBoxCanvas.transform.Find("NamePanel/NameText")?.GetComponent<TextMeshPro>();
        descriptionText = infoBoxCanvas.transform.Find("DescriptionPanel/DescriptionText")?.GetComponent<TextMeshPro>();
        
        // These buttons might be in the canvas or as separate GameObjects
        activateButton = infoBoxCanvas.transform.Find("ActivateButton")?.GetComponent<Button>();
        if (activateButton == null)
            activateButton = GameObject.Find("ActivateButton")?.GetComponent<Button>();
            
        falseActivateButton = infoBoxCanvas.transform.Find("FalseActivateButton")?.GetComponent<Button>();
        if (falseActivateButton == null)
            falseActivateButton = GameObject.Find("FalseActivateButton")?.GetComponent<Button>();
            
        closeButton = infoBoxCanvas.transform.Find("CloseButton")?.GetComponent<Button>();
        if (closeButton == null)
            closeButton = GameObject.Find("CloseButton")?.GetComponent<Button>();
            
        Debug.LogError($"[SuperPowerSpawner] Close button search result: {closeButton != null}");
        if (closeButton != null)
        {
            Debug.LogError($"[SuperPowerSpawner] Close button found: {closeButton.gameObject.name}");
        }

        // Debug what we found
        Debug.LogError($"[SuperPowerSpawner] UI Elements found:");
        Debug.LogError($"  - backgroundPanel: {(backgroundPanel != null ? "✓" : "✗")}");
        Debug.LogError($"  - infoBoxCanvas: {(infoBoxCanvas != null ? "✓" : "✗")}");
        Debug.LogError($"  - nameText: {(nameText != null ? "✓" : "✗")}");
        Debug.LogError($"  - descriptionText: {(descriptionText != null ? "✓" : "✗")}");
        Debug.LogError($"  - activateButton: {(activateButton != null ? "✓" : "✗")}");
        Debug.LogError($"  - falseActivateButton: {(falseActivateButton != null ? "✓" : "✗")}");
        Debug.LogError($"  - closeButton: {(closeButton != null ? "✓" : "✗")}");
        
        // CRITICAL: Detailed close button debugging
        if (closeButton != null)
        {
            Debug.LogError($"🔍 [SuperPowerSpawner] CLOSE BUTTON DETAILS:");
            Debug.LogError($"  - GameObject Name: {closeButton.gameObject.name}");
            Debug.LogError($"  - GameObject Active: {closeButton.gameObject.activeInHierarchy}");
            Debug.LogError($"  - Button Component: {closeButton != null}");
            Debug.LogError($"  - Button Interactable: {closeButton.interactable}");
            Debug.LogError($"  - Button Enabled: {closeButton.enabled}");
            Debug.LogError($"  - Button GameObject Active: {closeButton.gameObject.activeSelf}");
            Debug.LogError($"  - Button Parent: {(closeButton.transform.parent != null ? closeButton.transform.parent.name : "NULL")}");
            Debug.LogError($"  - Button Position: {closeButton.transform.position}");
            Debug.LogError($"  - Button Scale: {closeButton.transform.localScale}");
            
            // Check if button has a collider
            var collider = closeButton.GetComponent<Collider>();
            Debug.LogError($"  - Has Collider: {collider != null}");
            if (collider != null)
            {
                Debug.LogError($"  - Collider Enabled: {collider.enabled}");
                Debug.LogError($"  - Collider IsTrigger: {collider.isTrigger}");
            }
            
            // Check if button has a CanvasGroup
            var canvasGroup = closeButton.GetComponent<CanvasGroup>();
            Debug.LogError($"  - Has CanvasGroup: {canvasGroup != null}");
            if (canvasGroup != null)
            {
                Debug.LogError($"  - CanvasGroup Alpha: {canvasGroup.alpha}");
                Debug.LogError($"  - CanvasGroup Interactable: {canvasGroup.interactable}");
                Debug.LogError($"  - CanvasGroup BlocksRaycasts: {canvasGroup.blocksRaycasts}");
            }
        }
        else
        {
            Debug.LogError("❌ [SuperPowerSpawner] CLOSE BUTTON IS NULL!");
        }

        if (nameText == null || descriptionText == null || activateButton == null || closeButton == null)
        {
            Debug.LogError("One or more UI elements not found in InfoBoxCanvas for " + gameObject.name);
            if (nameText == null) Debug.LogError("NameText not found at path: InfoBoxCanvas/NamePanel/NameText");
            if (descriptionText == null) Debug.LogError("DescriptionText not found at path: InfoBoxCanvas/DescriptionPanel/DescriptionText");
            if (activateButton == null) Debug.LogError("ActivateButton not found in InfoBoxCanvas or as standalone GameObject");
            if (closeButton == null) Debug.LogError("CloseButton not found in InfoBoxCanvas or as standalone GameObject");
            return;
        }

        // CRITICAL FIX: Remove existing listeners before adding new ones to prevent multiple listeners
        Debug.LogError("🔧 [SuperPowerSpawner] Setting up button listeners...");
        activateButton.onClick.RemoveAllListeners();
        falseActivateButton.onClick.RemoveAllListeners();
        closeButton.onClick.RemoveAllListeners();
        
        Debug.LogError($"[SuperPowerSpawner] Close button found: {closeButton != null}");
        Debug.LogError($"[SuperPowerSpawner] Close button GameObject: {(closeButton != null ? closeButton.gameObject.name : "NULL")}");
        
        activateButton.onClick.AddListener(OnTokenClicked);
        falseActivateButton.onClick.AddListener(GetActiveButtonErrorMessage);
        
        Debug.LogError("🔧 [SuperPowerSpawner] Adding close button listener...");
        closeButton.onClick.AddListener(() =>
        {
            Debug.LogError("🔴 [SuperPowerSpawner] CLOSE BUTTON CLICKED! 🔴");
            Debug.LogError($"[SuperPowerSpawner] Close button GameObject: {closeButton.gameObject.name}");
            Debug.LogError($"[SuperPowerSpawner] Close button active: {closeButton.gameObject.activeInHierarchy}");
            Debug.LogError($"[SuperPowerSpawner] Close button interactable: {closeButton.interactable}");
            
            if (SuperPowerToken.ActiveInstance != null)
            {
                Debug.LogError($"[SuperPowerSpawner] Destroying power: {SuperPowerToken.ActiveInstance.power?.name}");
                RemoveSpawnedSuperPower(SuperPowerToken.ActiveInstance.gameObject);
                UpdateTokenPositions();
                StartCoroutine(SuperPowerToken.ActiveInstance.FadeOutSprite());
                StartCoroutine(CloseInfoBox());
            }
            else
            {
                Debug.LogError("[SuperPowerSpawner] Close button clicked but SuperPowerToken.ActiveInstance is null!");
                StartCoroutine(CloseInfoBox());
            }
        });
        Debug.LogError("✅ [SuperPowerSpawner] Close button listener added successfully!");

        StartCoroutine(CloseInfoBoxImmediate()); // Ensure the info box is closed initially without animation
    }

    private void OnTokenClicked()
    {
        Debug.Log("Activate button clicked for " + SuperPowerToken.ActiveInstance?.power.name);
        
        // Check if it's the player's turn before activating
        if (GameManager.LocalInstance != null && !GameManager.LocalInstance.IsLocalPlayerTurn())
        {
            Debug.LogWarning($"Cannot activate {SuperPowerToken.ActiveInstance?.power.name} - it's not your turn!");
            ShowOutOfTurnErrorMessage();
            return;
        }
        
        SuperPowerToken.ActiveInstance.OnTokenClicked();
    }
    
    /// <summary>
    /// Show error message when player tries to activate power out of turn
    /// </summary>
    public void ShowOutOfTurnErrorMessage()
    {
        if (nameText != null && descriptionText != null)
        {
            // Store original text
            string originalName = nameText.text;
            string originalDescription = descriptionText.text;
            
            // Show error message
            nameText.text = "Not Your Turn!";
            descriptionText.text = "You can only activate superpowers during your turn.";
            
            // Start coroutine to restore original text after delay
            StartCoroutine(RestoreOriginalTextAfterDelay(originalName, originalDescription, 2f));
        }
    }

    /// <summary>
    /// Shows a floating "wait for your turn" message after tapping a token repeatedly out of turn.
    /// </summary>
    public void ShowWaitForTurnMessage()
    {
        if (nameText != null && descriptionText != null)
        {
            string originalName = nameText.text;
            string originalDescription = descriptionText.text;

            nameText.text = "Sıranı Bekle!";
            descriptionText.text = "Tokeni kullanmak için sıranı bekle.";

            StartCoroutine(RestoreOriginalTextAfterDelay(originalName, originalDescription, 2.5f));
        }
    }
    
    /// <summary>
    /// Restore original text after showing error message
    /// </summary>
    /// <summary>
    /// Plays the page-change animation and updates the description text after a short delay.
    /// Used to guide players through dual-selection power steps (e.g. Şunu Değiş Tokuş).
    /// </summary>
    public void ShowDualSelectionStepText(string description)
    {
        if (!isInfoBoxOpen || backgroundPanel == null) return;
        StartCoroutine(ShowDualSelectionStepTextCoroutine(description));
    }

    private IEnumerator ShowDualSelectionStepTextCoroutine(string description)
    {
        UIFrameAnimator frameAnimator = backgroundPanel.GetComponent<UIFrameAnimator>();
        if (frameAnimator != null)
            frameAnimator.PlayPageChangeAnimation();

        yield return new WaitForSeconds(infoChangeDelay);

        if (descriptionText != null)
            descriptionText.text = description;
    }

    private IEnumerator RestoreOriginalTextAfterDelay(string originalName, string originalDescription, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (nameText != null && descriptionText != null)
        {
            nameText.text = originalName;
            descriptionText.text = originalDescription;
        }
    }

    /// <summary>
    /// Case 1: InfoBox was closed and gets opened (moves from original position to reach point)
    /// Case 2: InfoBox is already open (no movement, just update content)
    /// </summary>
    public IEnumerator OpenInfoBox(SuperPowerToken superPowerToken)
    {
        Debug.Log("Opening InfoBox for " + superPowerToken.power.name);

        // Check for null references
        if (backgroundPanel == null)
        {
            Debug.LogError("[SuperPowerSpawner] backgroundPanel is null in OpenInfoBox. Cannot open info box.");
            yield break;
        }

        if (superPowerToken == null || superPowerToken.power == null)
        {
            Debug.LogError("[SuperPowerSpawner] superPowerToken or its power is null in OpenInfoBox.");
            yield break;
        }

        // Stop any current animation
        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            currentAnimationCoroutine = null;
        }

        // Check if we're switching between different tokens while box is already open
        bool isSwitchingTokens = SuperPowerToken.ActiveInstance != null &&
                                SuperPowerToken.ActiveInstance != superPowerToken &&
                                isInfoBoxOpen;

        // Close currently active instance if different (but remember if box was open)
        bool wasBoxOpenBeforeSwitch = isInfoBoxOpen;
        if (SuperPowerToken.ActiveInstance != null && SuperPowerToken.ActiveInstance != superPowerToken)
        {
            yield return StartCoroutine(CloseInfoBoxImmediate()); // This sets isInfoBoxOpen to false
        }

        // Set the active instance early (before info update)
        SuperPowerToken.ActiveInstance = superPowerToken;
        
        // Notify MenuController if it exists that another page is being opened
        MenuController menuController = FindObjectOfType<MenuController>();
        if (menuController != null)
        {
            menuController.OnOtherPageOpened();
        }

        // FIXED: Check if we were switching tokens (Case 2) or opening fresh (Case 1)
        if (isSwitchingTokens && wasBoxOpenBeforeSwitch)
        {
            // Case 2: InfoBox was already open, just switching tokens - no movement animation
            Debug.Log("Case 2: Switching between tokens - no movement, with info delay and fade");
            backgroundPanel.SetActive(true);
            // Keep it at the reach point position (don't move it back to original)
            if (infoBoxReachPoint != null)
            {
                backgroundPanel.transform.position = infoBoxReachPoint.transform.position;
            }
            isInfoBoxOpen = true; // Restore the open state

            // NEW: Play page change animation first
            UIFrameAnimator frameAnimator = backgroundPanel.GetComponent<UIFrameAnimator>();
            if (frameAnimator != null)
            {
                frameAnimator.PlayPageChangeAnimation();
            }

            // NEW: Fade out current info, wait for delay, then fade in new info
            yield return StartCoroutine(FadeInfoWithDelay(superPowerToken));
            
            // Start idle animation after page change completes
            if (frameAnimator != null)
            {
                try
                {
                    frameAnimator.SetIdleAnimationEnabled(true);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[SuperPowerSpawner] Failed to enable idle animation after page change: {e.Message}");
                }
            }
        }
        else if (!isInfoBoxOpen)
        {
            // Case 1: InfoBox was truly closed - animate from original position to reach point
            Debug.Log("Case 1: InfoBox was closed - animating to reach point");
            backgroundPanel.SetActive(true);

            // Set to original position (off-screen)
            backgroundPanel.transform.position = infoBoxOriginalPosition.position;
            Debug.Log($"[SuperPowerSpawner] Set InfoBox to original position: {infoBoxOriginalPosition}");

            // Update content immediately for Case 1 (no delay needed for initial opening)
            // Check if this is a menu token
            if (superPowerToken.power is MenuPagePower)
            {
                // For menu tokens, set menu flag to true and don't update content
                isMenuPageOpen = true;
                Debug.Log("[SuperPowerSpawner] Opening menu page - setting isMenuPageOpen to true");
            }
            else
            {
                // For regular power tokens, update content (which sets isMenuPageOpen to false)
                UpdateInfoBoxContent(superPowerToken);
            }

            // Start button rotation simultaneously with InfoBox animation
            StartCoroutine(RotateOpenButtonOut());

            // Animate to reach point
            if (infoBoxReachPoint != null)
            {
                Debug.Log($"[SuperPowerSpawner] Starting animation to reach point: {infoBoxReachPoint.transform.position}");
                currentAnimationCoroutine = StartCoroutine(AnimateInfoBoxPosition(infoBoxReachPoint.transform.position));
                yield return currentAnimationCoroutine;
                currentAnimationCoroutine = null;
                Debug.Log("[SuperPowerSpawner] Animation to reach point completed");
            }
            else
            {
                Debug.LogWarning("[SuperPowerSpawner] infoBoxReachPoint is null, InfoBox will not animate to reach point");
            }

            isInfoBoxOpen = true;

            // Start idle animation after opening
            UIFrameAnimator frameAnimator = backgroundPanel.GetComponent<UIFrameAnimator>();
            if (frameAnimator != null)
            {
                try
                {
                    frameAnimator.SetIdleAnimationEnabled(true);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[SuperPowerSpawner] Failed to enable idle animation after opening: {e.Message}");
                }
            }

            // Don't play page change animation for initial opening
            // Note: isMenuPageOpen is already set to false by UpdateInfoBoxContent() above
            Debug.Log("Case 1: No page change animation - initial opening");
        }
        else
        {
            // Fallback: ensure it's active and positioned correctly
            backgroundPanel.SetActive(true);
            // Position at reach point if available, otherwise at original position
            if (infoBoxReachPoint != null)
            {
                backgroundPanel.transform.position = infoBoxReachPoint.transform.position;
            }
            else
            {
                backgroundPanel.transform.position = infoBoxOriginalPosition.position;
            }

            // NEW: Play page change animation for fallback case too
            UIFrameAnimator frameAnimator = backgroundPanel.GetComponent<UIFrameAnimator>();
            if (frameAnimator != null)
            {
                frameAnimator.PlayPageChangeAnimation();
            }

            // NEW: Fade out current info, wait for delay, then fade in new info
            yield return StartCoroutine(FadeInfoWithDelay(superPowerToken));
            
            // Start idle animation after fallback page change completes
            if (frameAnimator != null)
            {
                try
                {
                    frameAnimator.SetIdleAnimationEnabled(true);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[SuperPowerSpawner] Failed to enable idle animation after fallback page change: {e.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Handles the fade out, delay, and fade in sequence for info changes
    /// UPDATED: Works with the new InfoBoxCanvas structure
    /// </summary>
    private IEnumerator FadeInfoWithDelay(SuperPowerToken superPowerToken)
    {
        // Fade the entire InfoBoxCanvas instead of individual elements
        CanvasGroup canvasGroup = null;
        
        if (infoBoxCanvas != null)
        {
            canvasGroup = GetOrAddCanvasGroup(infoBoxCanvas);
        }
        
        if (canvasGroup == null)
        {
            Debug.LogWarning("InfoBoxCanvas or its CanvasGroup not found, updating content without fade");
            // Check if this is a menu token
            if (superPowerToken.power is MenuPagePower)
            {
                // For menu tokens, set menu flag to true and don't update content
                isMenuPageOpen = true;
                Debug.Log("[SuperPowerSpawner] Fallback: Opening menu page - setting isMenuPageOpen to true");
            }
            else
            {
                // For regular power tokens, update content (which sets isMenuPageOpen to false)
                UpdateInfoBoxContent(superPowerToken);
            }
            yield break;
        }

        // Phase 1: Fade out current info (entire canvas)
        yield return StartCoroutine(FadeCanvasGroups(new CanvasGroup[] { canvasGroup }, 0f, fadeDuration));

        // Phase 2: Wait for the remaining delay (accounting for fade out time)
        float remainingDelay = Mathf.Max(0f, infoChangeDelay - fadeDuration);
        if (remainingDelay > 0f)
        {
            yield return new WaitForSeconds(remainingDelay);
        }

        // Phase 3: Update content while faded out
        // Check if this is a menu token
        if (superPowerToken.power is MenuPagePower)
        {
            // For menu tokens, set menu flag to true and don't update content
            isMenuPageOpen = true;
            Debug.Log("[SuperPowerSpawner] Switching to menu page - setting isMenuPageOpen to true");
        }
        else
        {
            // For regular power tokens, update content (which sets isMenuPageOpen to false)
            UpdateInfoBoxContent(superPowerToken);
        }

        // Phase 4: Fade in new info (entire canvas)
        yield return StartCoroutine(FadeCanvasGroups(new CanvasGroup[] { canvasGroup }, 1f, fadeDuration));
    }

    /// <summary>
    /// Gets or adds a CanvasGroup component to a GameObject
    /// </summary>
    private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        if (obj == null) return null;
        
        CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = obj.AddComponent<CanvasGroup>();
        }
        return canvasGroup;
    }

    /// <summary>
    /// Fades multiple CanvasGroups and also fades TextMeshProUGUI components (nameText, descriptionText) to a target alpha value
    /// </summary>
    private IEnumerator FadeCanvasGroups(CanvasGroup[] canvasGroups, float targetAlpha, float duration)
    {
        // Prepare canvas group alphas
        float[] startAlphas = canvasGroups != null ? new float[canvasGroups.Length] : null;
        if (canvasGroups != null)
        {
            for (int i = 0; i < canvasGroups.Length; i++)
            {
                if (canvasGroups[i] != null)
                    startAlphas[i] = canvasGroups[i].alpha;
            }
        }

        // Prepare TextMeshPro alphas
        float nameStartAlpha = 1f, descStartAlpha = 1f;
        Color nameOrigColor = Color.white, descOrigColor = Color.white;
        bool hasNameText = nameText != null;
        bool hasDescText = descriptionText != null;
        if (hasNameText)
        {
            nameOrigColor = nameText.color;
            nameStartAlpha = nameOrigColor.a;
        }
        if (hasDescText)
        {
            descOrigColor = descriptionText.color;
            descStartAlpha = descOrigColor.a;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);

            // Apply smooth easing
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            // Fade canvas groups
            if (canvasGroups != null)
            {
                for (int i = 0; i < canvasGroups.Length; i++)
                {
                    if (canvasGroups[i] != null)
                        canvasGroups[i].alpha = Mathf.Lerp(startAlphas[i], targetAlpha, easedProgress);
                }
            }

            // Fade TextMeshProUGUI components
            if (hasNameText)
            {
                Color c = nameOrigColor;
                c.a = Mathf.Lerp(nameStartAlpha, targetAlpha, easedProgress);
                nameText.color = c;
            }
            if (hasDescText)
            {
                Color c = descOrigColor;
                c.a = Mathf.Lerp(descStartAlpha, targetAlpha, easedProgress);
                descriptionText.color = c;
            }

            yield return null;
        }

        // Ensure final alpha values are exact
        if (canvasGroups != null)
        {
            for (int i = 0; i < canvasGroups.Length; i++)
            {
                if (canvasGroups[i] != null)
                    canvasGroups[i].alpha = targetAlpha;
            }
        }
        if (hasNameText)
        {
            Color c = nameText.color;
            c.a = targetAlpha;
            nameText.color = c;
        }
        if (hasDescText)
        {
            Color c = descriptionText.color;
            c.a = targetAlpha;
            descriptionText.color = c;
        }
    }

    /// <summary>
    /// Separate method to update info box content - extracted for cleaner code
    /// </summary>
    private void UpdateInfoBoxContent(SuperPowerToken superPowerToken)
    {
        // Check for null references before updating content
        if (activateButton == null || falseActivateButton == null || closeButton == null || 
            nameText == null || descriptionText == null)
        {
            Debug.LogError("[SuperPowerSpawner] One or more UI elements are null in UpdateInfoBoxContent. Cannot update content.");
            return;
        }

        // Set menu page flag to false since we're showing power information
        isMenuPageOpen = false;

        // Update content
        bool canActivate = CheckIfCardShouldBeSelected(superPowerToken.power.name);
        activateButton.gameObject.SetActive(canActivate);
        falseActivateButton.gameObject.SetActive(!canActivate);
        closeButton.gameObject.SetActive(true);
        
        // Handle hand showcasing based on power type
        if (deckController != null)
        {
            if (PowerRequiresHandShowcase(superPowerToken.power.name))
            {
                Debug.Log($"[Showcase] InfoBox: Starting hand showcase for {superPowerToken.power.name}");
                //deckController.ShowcaseAllOtherHands();
                Debug.Log($"[Showcase] InfoBox: ShowcaseAllOtherHands() called for {superPowerToken.power.name}");
            }
            else
            {
                // Stop showcasing if switching to a power that doesn't require card selection
                Debug.Log($"[Showcase] InfoBox: Stopping hand showcase - switching to {superPowerToken.power.name}");
                //deckController.ExitShowcaseAllOtherHands();
                Debug.Log($"[Showcase] InfoBox: ExitShowcaseAllOtherHands() called for {superPowerToken.power.name}");
            }
        }
        else
        {
            Debug.LogError($"[Showcase] ERROR: DeckController is null in OpenInfoBox for {superPowerToken.power.name}");
        }

        currentNameText = superPowerToken.power.name;
        currentDescriptionText = superPowerToken.power.description;
        nameText.text = currentNameText;
        descriptionText.text = currentDescriptionText;

        // Stop error message if showing
        if (errorMessageCoroutine != null)
        {
            StopCoroutine(errorMessageCoroutine);
            errorMessageCoroutine = null;
        }
        
        // Update button state based on turn
        UpdateActivateButtonForTurn();

        Debug.Log($"Info updated for: {superPowerToken.power.name}");
    }
    
    /// <summary>
    /// Clear all InfoBox content to ensure clean state when closed
    /// </summary>
    private void ClearInfoBoxContent()
    {
        // Clear text content
        if (nameText != null)
        {
            nameText.text = "";
        }
        
        if (descriptionText != null)
        {
            descriptionText.text = "";
        }
        
        // Clear current text variables
        currentNameText = "";
        currentDescriptionText = "";
        
        // Stop any error message if showing
        if (errorMessageCoroutine != null)
        {
            StopCoroutine(errorMessageCoroutine);
            errorMessageCoroutine = null;
        }
        
        // Notify MenuController to hide menu if it's showing
        MenuController menuController = FindObjectOfType<MenuController>();
        if (menuController != null)
        {
            menuController.SetMenuActive(false);
        }
        
        Debug.Log("[SuperPowerSpawner] InfoBox content cleared");
    }

    /// <summary>
    /// Ensures all UI elements are fully visible (used for initialization and Case 1)
    /// UPDATED: Works with the new InfoBoxCanvas structure
    /// </summary>
    private void EnsureUIElementsVisible()
    {
        // Ensure the InfoBoxCanvas is fully visible
        if (infoBoxCanvas != null)
        {
            CanvasGroup canvasGroup = infoBoxCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }
    }

    /// <summary>
    /// Case 3: InfoBox is open and gets closed (moves from reach point back to original position)
    /// </summary>
    public IEnumerator CloseInfoBox()
    {
        Debug.Log("Closing InfoBox for " + SuperPowerToken.ActiveInstance?.power.name);

        // Check for null references
        if (backgroundPanel == null)
        {
            Debug.LogError("[SuperPowerSpawner] backgroundPanel is null in CloseInfoBox. Cannot close info box.");
            yield break;
        }

        if (!isInfoBoxOpen)
        {
            yield break; // Already closed
        }

        // Stop any current animation
        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            currentAnimationCoroutine = null;
        }

        // Ensure UI elements are visible before closing
        EnsureUIElementsVisible();

        // Start button rotation simultaneously with InfoBox closing animation
        StartCoroutine(RotateOpenButtonIn());

        // Case 3: Animate from reach point back to original position
        Debug.Log($"[SuperPowerSpawner] Starting close animation to original position: {infoBoxOriginalPosition}");
        currentAnimationCoroutine = StartCoroutine(AnimateInfoBoxPosition(infoBoxOriginalPosition.position));
        yield return currentAnimationCoroutine;
        currentAnimationCoroutine = null;
        Debug.Log("[SuperPowerSpawner] Close animation completed");

        // Hide UI elements and stop idle animation, but keep InfoBox active at original position
        if (activateButton != null) activateButton.gameObject.SetActive(false);
        if (falseActivateButton != null) falseActivateButton.gameObject.SetActive(false);
        if (closeButton != null) closeButton.gameObject.SetActive(false);
        
        // Clear InfoBox content to ensure clean state when closed
        ClearInfoBoxContent();
        
        // Stop hand showcasing when InfoBox is closed (but only if no dual selection is active)
        if (deckController != null)
        {
            // Check if any dual selection powers are active
            bool isDualSelectionActive = GameManager.LocalInstance != null && 
                (GameManager.LocalInstance.isKopyalaActive || GameManager.LocalInstance.isSunuDegisTokusActive || GameManager.LocalInstance.isSunuDegisBunuTokusActive);
            
            // Check if peek powers are active (these should not stop showcase immediately)
            bool isPeekPowerActive = SuperPowerToken.ActiveInstance != null && 
                (SuperPowerToken.ActiveInstance.superPowerClassName == "UcundanGözAt" || 
                 SuperPowerToken.ActiveInstance.superPowerClassName == "BayaBayaBak");
            
            if (isDualSelectionActive)
            {
                Debug.Log("[Showcase] InfoBox: Dual selection is active, NOT stopping showcase - InfoBox closed");
            }
            else if (isPeekPowerActive)
            {
                Debug.Log("[Showcase] InfoBox: Peek power is active, NOT stopping showcase immediately - InfoBox closed");
                // Don't stop showcase immediately for peek powers - let the animation handle it
            }
            else
            {
                Debug.Log("[Showcase] InfoBox: No dual selection or peek power active, force stopping hand showcase - InfoBox closed");
                ForceStopHandShowcase();
            }
        }
        else
        {
            Debug.LogError("[Showcase] ERROR: DeckController is null in CloseInfoBox");
        }
        
        // Stop idle animation
        UIFrameAnimator frameAnimator = backgroundPanel.GetComponent<UIFrameAnimator>();
        if (frameAnimator != null)
        {
            try
            {
                frameAnimator.SetIdleAnimationEnabled(false);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SuperPowerSpawner] Failed to disable idle animation: {e.Message}");
            }
        }
        
        SuperPowerToken.ActiveInstance = null;
        isInfoBoxOpen = false;
        isMenuPageOpen = false; // Reset menu page flag when InfoBox is closed
    }

    /// <summary>
    /// Immediately close the info box without animation (used for initialization or quick switches)
    /// </summary>
    public IEnumerator CloseInfoBoxImmediate()
    {
        // Ensure UI elements are visible before closing
        EnsureUIElementsVisible();

        // Move InfoBox to original position immediately (no animation)
        if (backgroundPanel != null)
        {
            backgroundPanel.transform.position = infoBoxOriginalPosition.position;
        }

        // Hide UI elements and stop idle animation, but keep InfoBox active
        activateButton.gameObject.SetActive(false);
        falseActivateButton.gameObject.SetActive(false);
        closeButton.gameObject.SetActive(false);
        
        // Clear InfoBox content to ensure clean state when closed
        ClearInfoBoxContent();
        
        // Stop hand showcasing when InfoBox is closed (but only if no dual selection is active)
        if (deckController != null)
        {
            // Check if any dual selection powers are active
            bool isDualSelectionActive = GameManager.LocalInstance != null && 
                (GameManager.LocalInstance.isKopyalaActive || GameManager.LocalInstance.isSunuDegisTokusActive || GameManager.LocalInstance.isSunuDegisBunuTokusActive);
            
            // Check if peek powers are active (these should not stop showcase immediately)
            bool isPeekPowerActive = SuperPowerToken.ActiveInstance != null && 
                (SuperPowerToken.ActiveInstance.superPowerClassName == "UcundanGözAt" || 
                 SuperPowerToken.ActiveInstance.superPowerClassName == "BayaBayaBak");
            
            if (isDualSelectionActive)
            {
                Debug.Log("[Showcase] InfoBox: Dual selection is active, NOT stopping showcase - InfoBox closed immediately");
            }
            else if (isPeekPowerActive)
            {
                Debug.Log("[Showcase] InfoBox: Peek power is active, NOT stopping showcase immediately - InfoBox closed immediately");
                // Don't stop showcase immediately for peek powers - let the animation handle it
            }
            else
            {
                Debug.Log("[Showcase] InfoBox: No dual selection or peek power active, force stopping hand showcase - InfoBox closed immediately");
                ForceStopHandShowcase();
            }
        }
        else
        {
            Debug.LogError("[Showcase] ERROR: DeckController is null in CloseInfoBoxImmediate");
        }
        
        // Stop idle animation
        UIFrameAnimator frameAnimator = backgroundPanel.GetComponent<UIFrameAnimator>();
        if (frameAnimator != null)
        {
            try
            {
                frameAnimator.SetIdleAnimationEnabled(false);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SuperPowerSpawner] Failed to disable idle animation: {e.Message}");
            }
        }
        
        SuperPowerToken.ActiveInstance = null;
        isInfoBoxOpen = false;
        isMenuPageOpen = false; // Reset menu page flag when InfoBox is closed immediately
        yield return null;
    }

    /// <summary>
    /// Animates the info box from its current position to the target position
    /// </summary>
    private IEnumerator AnimateInfoBoxPosition(Vector3 targetPosition)
    {
        if (backgroundPanel == null)
        {
            Debug.LogError("BackgroundPanel is null, cannot animate position");
            yield break;
        }

        Vector3 startPosition = backgroundPanel.transform.position;
        float elapsedTime = 0f;
        
        Debug.Log($"[SuperPowerSpawner] Animating InfoBox from {startPosition} to {targetPosition} over {animationDuration} seconds");

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            
            // Use smooth easing (you can change this to other easing functions)
            progress = Mathf.SmoothStep(0f, 1f, progress);
            
            Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, progress);
            backgroundPanel.transform.position = currentPosition;
            
            yield return null;
        }

        // Ensure final position is exact
        backgroundPanel.transform.position = targetPosition;
        Debug.Log($"[SuperPowerSpawner] Animation completed. Final position: {backgroundPanel.transform.position}");
    }

    // Rest of your existing code remains the same...
    public List<string> restirictedPowersName_CardNeedToBeSelected = new List<string> { "Şunu Değiş Tokuş"};
    private List<string> restirictedPowersName_CenterNotEmpty = new List<string> { "Bu Daha İyi", "Bomba" };
    public List<string> restirictedPowersName_WaitForSwap = new List<string> { "Bu Daha İyi", "Şunu Değiş Tokuş", "Şunu Değiş Bunu Tokuş", "Değiş Tokuş", "Kopyala Yapıştır", "Kapkaç", "Yandım Anam"};
    // Powers that require automatic hand showcasing when opened
    private List<string> powersRequiringHandShowcase = new List<string> { "Kapkaç", "Yandım Anam", "Bu Daha İyi", "Kopyala Yapıştır" };
    
    // Powers that require hand showcasing after activation (for dual selection)
    private List<string> powersRequiringHandShowcaseAfterActivation = new List<string> { "Şunu Değiş Tokuş", "Kopyala Yapıştır", "Şunu Değiş Bunu Tokuş" };
    
    // Reference to DeckController for showcasing hands
    [SerializeField] private DeckController deckController;
    
    private bool CheckIfCardShouldBeSelected(string superPowerTokenName)
    {
        bool flag = CardNeedToBeSelected(superPowerTokenName);
        flag = flag && CenterNotEmpty(superPowerTokenName);
        return flag;
    }

    private bool CardNeedToBeSelected(string superPowerTokenName)
    {
        if (restirictedPowersName_CardNeedToBeSelected.Contains(superPowerTokenName))
        {
            if (CardInteraction.currentlySelectedCard != null && GameManager.LocalInstance.GetCurrentSelectedHandCard() != null)
                return true;
            else
                return false;
        }
        return true;
    }

    private bool CenterNotEmpty(string superPowerTokenName)
    {
        if (restirictedPowersName_CenterNotEmpty.Contains(superPowerTokenName))
        {
            if (GameManager.LocalInstance.centerCards.Count != 0)
                return true;
            else
                return false;
        }
        return true;
    }
    
    /// <summary>
    /// Check if a power requires hand showcasing (only specific powers that need to select from any player's hand)
    /// </summary>
    private bool PowerRequiresHandShowcase(string superPowerTokenName)
    {
        return powersRequiringHandShowcase.Contains(superPowerTokenName);
    }
    
    /// <summary>
    /// Check if a power requires hand showcasing after activation (for dual selection powers)
    /// </summary>
    private bool PowerRequiresHandShowcaseAfterActivation(string superPowerTokenName)
    {
        return powersRequiringHandShowcaseAfterActivation.Contains(superPowerTokenName);
    }
    
    /// <summary>
    /// Start hand showcasing for dual selection powers (called from GameManager)
    /// </summary>
    public void StartHandShowcaseForDualSelection(string powerName)
    {
        Debug.Log($"[Showcase] StartHandShowcaseForDualSelection called for: {powerName}");
        
        if (deckController == null)
        {
            Debug.LogError($"[Showcase] ERROR: DeckController is null! Cannot start showcase for {powerName}");
            return;
        }
        
        if (!PowerRequiresHandShowcaseAfterActivation(powerName))
        {
            Debug.Log($"[Showcase] Power {powerName} does not require hand showcase after activation");
            return;
        }
        
        Debug.Log($"[Showcase] Starting hand showcase for dual selection: {powerName}");
        //deckController.ShowcaseAllOtherHands();
        Debug.Log($"[Showcase] ShowcaseAllOtherHands() called successfully for {powerName}");
    }
    
    /// <summary>
    /// Stop hand showcasing (called from GameManager when dual selection is complete)
    /// </summary>
    public void StopHandShowcase()
    {
        Debug.Log("[Showcase] StopHandShowcase called");
        
        if (deckController == null)
        {
            Debug.LogError("[Showcase] ERROR: DeckController is null! Cannot stop showcase");
            return;
        }
        
        Debug.Log("[Showcase] Stopping hand showcase - dual selection complete");
        //deckController.ExitShowcaseAllOtherHands();
        Debug.Log("[Showcase] ExitShowcaseAllOtherHands() called successfully");
    }
    
     /// <summary>
     /// Force stop hand showcasing (used when InfoBox closes and no dual selection is active)
     /// </summary>
     public void ForceStopHandShowcase()
     {
         Debug.Log("[Showcase] ForceStopHandShowcase called");
         
         if (deckController == null)
         {
             Debug.LogError("[Showcase] ERROR: DeckController is null! Cannot force stop showcase");
             return;
         }
         
         Debug.Log("[Showcase] Force stopping hand showcase");
         //deckController.ExitShowcaseAllOtherHands();
         Debug.Log("[Showcase] Force stop - ExitShowcaseAllOtherHands() called successfully");
     }
     
     /// <summary>
     /// Called when peek animation completes - ensures InfoBox closes properly
     /// </summary>
     public void OnPeekAnimationComplete()
     {
         Debug.Log("[SuperPowerSpawner] OnPeekAnimationComplete called");
         
         // Check if we have a peek power active and InfoBox is still open
         if (SuperPowerToken.ActiveInstance != null && 
             (SuperPowerToken.ActiveInstance.superPowerClassName == "UcundanGözAt" || 
              SuperPowerToken.ActiveInstance.superPowerClassName == "BayaBayaBak") &&
             isInfoBoxOpen)
         {
             Debug.Log("[SuperPowerSpawner] Peek animation complete - closing InfoBox now");
             
             // Clear the active instance
             SuperPowerToken.ActiveInstance = null;
             
             // Close InfoBox immediately
             StartCoroutine(CloseInfoBox());
         }
     }
     
     /// <summary>
     /// Check if a card selection power is currently shown in the InfoBox
     /// </summary>
     public bool IsCardSelectionPowerInInfoBox()
     {
         if (!isInfoBoxOpen || nameText == null)
         {
             return false;
         }
         
         string currentPowerName = nameText.text;
         bool isCardSelectionPower = powersRequiringHandShowcase.Contains(currentPowerName);
         
         Debug.Log($"[CardSelection] IsCardSelectionPowerInInfoBox: {currentPowerName} -> {isCardSelectionPower}");
         return isCardSelectionPower;
     }

    public void SetActiveActivateButtonTrue()
    {
        if (activateButton.gameObject.activeSelf) return;
        else
        {
            activateButton.gameObject.SetActive(true);
            ResetInfoBoxText();
        }
        
        // Update button state based on turn
        UpdateActivateButtonForTurn();
    }
    
    /// <summary>
    /// Update the activate button state based on whether it's the player's turn
    /// </summary>
    public void UpdateActivateButtonForTurn()
    {
        if (activateButton == null) return;
        
        bool isMyTurn = GameManager.LocalInstance != null && GameManager.LocalInstance.IsLocalPlayerTurn();
        
        // Enable/disable button based on turn
        activateButton.interactable = isMyTurn;
        
        // Visual feedback - change button color
        if (activateButton.targetGraphic != null)
        {
            if (isMyTurn)
            {
                activateButton.targetGraphic.color = Color.white;
            }
            else
            {
                activateButton.targetGraphic.color = Color.gray;
            }
        }
        
        Debug.Log($"[SuperPowerSpawner] Activate button updated - isMyTurn: {isMyTurn}, interactable: {activateButton.interactable}");
    }

    public void InitializeSuperPowers()
    {
        DictionaryCreation();
        playerPowerPoolTransform = GameObject.Find("PlayerPowerPool")?.transform;
        if (playerPowerPoolTransform == null)
            Debug.LogWarning("PlayerPowerPool transform not found!");
    }

    private void DictionaryCreation()
    {
        superPowerPrefabs.Clear();
        superPowerList.Clear();

        foreach (var token in superPowerTokens)
        {
            var tokenScript = token.GetComponent<SuperPowerToken>();
            if (tokenScript == null)
            {
                Debug.LogWarning($"Token prefab {token.name} does not have a SuperPowerToken component.");
                continue;
            }

            string className = tokenScript.superPowerClassName;
            if (string.IsNullOrEmpty(className))
            {
                Debug.LogWarning($"Token prefab {token.name} does not have a valid superPowerClassName.");
                continue;
            }

            var type = System.Type.GetType(className);
            if (type == null || !typeof(SuperPower).IsAssignableFrom(type))
            {
                Debug.LogWarning($"Could not find SuperPower type for {className}");
                continue;
            }

            SuperPower powerInstance = ScriptableObject.CreateInstance(type) as SuperPower;
            if (powerInstance == null)
            {
                Debug.LogWarning($"Failed to create SuperPower instance for {className}");
                continue;
            }

            if (!superPowerPrefabs.ContainsKey(powerInstance))
            {
                superPowerPrefabs.Add(powerInstance, token);
                // REMOVED PRE-POOLING: No longer add multiple copies to superPowerList
                // Each power is now added only once, eliminating pool depletion issues
                superPowerList.Add(powerInstance);
            }
            else
            {
                Debug.LogWarning($"Super power {className} already exists in the dictionary.");
            }
        }
        
        Debug.Log($"[SuperPowerSpawner] Dictionary created with {superPowerPrefabs.Count} unique powers (no pre-pooling)");
    }

    [ContextMenu("Ready to Spawn Super Powers")]
    /// <summary>
    /// Spawns multiple powers with inverse-weighted random selection.
    /// Higher rarityMultiplier = lower probability of being selected.
    /// Each power costs 1 gold.
    /// </summary>
    public void ReadyToSpawnSuperPowers(int numberOfSuperPowersToSpawn = 2, Vector3? spawnOrigin = null, float spawnScale = 150f, int costMode = 1)
    {
        StartCoroutine(ReadyToSpawnSuperPower(numberOfSuperPowersToSpawn, spawnOrigin, spawnScale, costMode));
    }

    private IEnumerator ReadyToSpawnSuperPower(int numberOfSuperPowersToSpawn, Vector3? spawnOrigin, float spawnScale, int costMode)
    {
        yield return new WaitForSeconds(1f);
        for (int i = 0; i < numberOfSuperPowersToSpawn; i++)
        {
            StartCoroutine(SpawnSuperPower(GetInverseWeightedRandomSuperPower(costMode), spawnOrigin, spawnScale));
            yield return new WaitForSeconds(0.5f);
        }
    }

    /// <summary>
    /// Selects a random power using inverse-weighted probability based on rarityMultiplier,
    /// boosted by costMode. Powers in the matching tier get a 5x weight boost.
    /// Tier 4 (ZaferPuani) is never boosted. Disabled powers are excluded.
    /// </summary>
    private SuperPower GetInverseWeightedRandomSuperPower(int costMode = 1)
    {
        if (superPowerPrefabs.Count == 0)
        {
            Debug.LogWarning("[SuperPowerSpawner] No super powers available to spawn.");
            return null;
        }

        const float boostMultiplier = 5f;
        List<SuperPower> allPowers = new List<SuperPower>(superPowerPrefabs.Keys);

        // Exclude disabled powers
        List<SuperPower> activePowers = new List<SuperPower>();
        foreach (var p in allPowers)
            if (p.isPowerEnabled) activePowers.Add(p);

        if (activePowers.Count == 0)
        {
            Debug.LogWarning("[SuperPowerSpawner] No active powers available.");
            return null;
        }

        // Calculate weights: base = 1/rarity, boosted if tier matches mode (tiers 1-3 only)
        List<float> weights = new List<float>();
        float totalWeight = 0f;

        for (int i = 0; i < activePowers.Count; i++)
        {
            float weight = 1f / activePowers[i].rarityMultiplier;
            if (activePowers[i].powerCostTier == costMode && costMode >= 1 && costMode <= 3)
                weight *= boostMultiplier;
            weights.Add(weight);
            totalWeight += weight;
        }

        // Pick a random value in [0, totalWeight)
        float randomValue = Random.Range(0f, totalWeight);
        float cumulativeWeight = 0f;

        for (int i = 0; i < activePowers.Count; i++)
        {
            cumulativeWeight += weights[i];
            if (randomValue < cumulativeWeight)
            {
                SuperPower selectedPower = activePowers[i];
                Debug.Log($"[SuperPowerSpawner] Mode {costMode} selection: {selectedPower.name} (tier: {selectedPower.powerCostTier}, weight: {weights[i]:F4})");
                return selectedPower;
            }
        }

        // Fallback (should rarely happen due to floating point precision)
        SuperPower fallbackPower = activePowers[activePowers.Count - 1];
        Debug.LogWarning($"[SuperPowerSpawner] Weighted selection fallback: {fallbackPower.name}");
        return fallbackPower;
    }

    // Update SpawnSuperPower to accept origin and scale
    private IEnumerator SpawnSuperPower(SuperPower superPower, Vector3? spawnOrigin, float spawnScale)
    {
        if (superPower == null)
        {
            Debug.LogError("SpawnSuperPower called with null SuperPower!");
            yield break;
        }

        if (spawnedSuperPowers.Count >= maxSuperPowers)
        {
            Debug.LogWarning("Max super powers reached, cannot spawn more.");
            yield break;
        }

        GameObject placeholder = new GameObject("TokenPlaceholder");
        spawnedSuperPowers.Add(placeholder);
        UpdateTokenPositions();

        yield return new WaitForSeconds(1f);

        if (superPowerPrefabs.TryGetValue(superPower, out GameObject prefab))
        {
            // Use spawnOrigin if provided, otherwise use placeholder position
            Vector3 startPos = spawnOrigin ?? placeholder.transform.position;
            Vector3 targetPos = placeholder.transform.position;

            GameObject instance = Instantiate(prefab, startPos, transform.rotation);

            // Set initial scale
            float bigScale = spawnScale;
            float normalScale = 100f;
            instance.transform.localScale = Vector3.one * bigScale;

            var tokenScript = instance.GetComponent<SuperPowerToken>();
            if (tokenScript != null)
            {
                tokenScript.power = superPower;
            }

            int placeholderIndex = spawnedSuperPowers.IndexOf(placeholder);
            if (placeholderIndex != -1)
            {
                spawnedSuperPowers[placeholderIndex] = instance;
            }
            Destroy(placeholder);

            Debug.Log($"{superPower.name} spawned.");
            UpdateTokenPositions();

            // Animate to target position and scale
            float moveDuration = 0.5f;
            float scaleDuration = 0.5f;
            float elapsed = 0f;
            Vector3 initialScale = instance.transform.localScale;
            Vector3 finalScale = Vector3.one * normalScale;
            Vector3 initialPos = startPos;
            Vector3 finalPos = targetPos;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);
                instance.transform.position = Vector3.Lerp(initialPos, finalPos, t);
                instance.transform.localScale = Vector3.Lerp(initialScale, finalScale, t);
                yield return null;
            }
            instance.transform.position = finalPos;
            instance.transform.localScale = finalScale;

            UpdateTokenPositions();
            NotifyServerOfGoldAndPowers();
        }
        else
        {
            Debug.LogError($"Super power {superPower?.name} not found in the dictionary.");
            spawnedSuperPowers.Remove(placeholder);
            Destroy(placeholder);
        }
    }


    public void UpdateTokenPositions(float moveDuration = 0.25f)
    {
        spawnedSuperPowers.RemoveAll(token => token == null);

        int[] indices = GetSpawnIndices(spawnedSuperPowers.Count);

        for (int i = 0; i < spawnedSuperPowers.Count && i < indices.Length; i++)
        {
            if (spawnedSuperPowers[i] != null)
            {
                StartCoroutine(MoveTokenToPosition(spawnedSuperPowers[i], spawnPositions[indices[i]].position, moveDuration));
            }
        }
    }

    private IEnumerator MoveTokenToPosition(GameObject token, Vector3 targetPosition, float duration)
    {
        if (token == null) yield break; // Early exit if token is already destroyed

        Vector3 startPos = token.transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (token == null) yield break; // Stop if token was destroyed during the animation

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            token.transform.position = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }
        if (token != null)
            token.transform.position = targetPosition;
    }


    public void RemoveSpawnedSuperPower(GameObject token)
    {
        if (spawnedSuperPowers.Contains(token))
        {
            spawnedSuperPowers.Remove(token);
            Debug.Log($"Removed {token.name} from spawned super powers.");
        }
        else
        {
            Debug.LogWarning($"{token.name} not found in spawned super powers.");
        }
        NotifyServerOfGoldAndPowers();
    }

    private static readonly int[][] spawnIndexPatterns = new int[][]
    {
        new int[] { 4 },
        new int[] { 3, 5 },
        new int[] { 2, 4, 6 },
        new int[] { 1, 3, 5, 7 },
        new int[] { 0, 2, 4, 6, 8 },
    };

    private int[] GetSpawnIndices(int count)
    {
        if (count <= 0) return new int[0];
        if (count <= spawnIndexPatterns.Length)
            return spawnIndexPatterns[count - 1];
        int[] indices = new int[count];
        for (int i = 0; i < count && i < spawnPositions.Count; i++)
            indices[i] = i;
        return indices;
    }

    private Coroutine errorMessageCoroutine;
    private string currentNameText = "";
    private string currentDescriptionText = "";

    public void GetActiveButtonErrorMessage()
    {
        if (errorMessageCoroutine != null)
        {
            StopCoroutine(errorMessageCoroutine);
        }
        errorMessageCoroutine = StartCoroutine(ShowErrorMessageCoroutine());
    }

    private IEnumerator ShowErrorMessageCoroutine()
    {
        if (nameText == null || descriptionText == null || activateButton == null || falseActivateButton == null)
        {
            Debug.LogError("[SuperPowerSpawner] UI elements are null in ShowErrorMessageCoroutine.");
            yield break;
        }
        
        nameText.text = "Hatan var";
        descriptionText.text = "Bu gücü kullanabilmek için önce bir kart seçmelisin";
        activateButton.gameObject.SetActive(false);
        falseActivateButton.gameObject.SetActive(true);

        float timer = 0f;
        while (timer < 4f)
        {
            if (nameText.text != "Hatan var" || descriptionText.text != "Bu gücü kullanabilmek için önce bir kart seçmelisin")
                yield break;
            timer += Time.deltaTime;
            yield return null;
        }
        ResetInfoBoxText();
    }

    private void ResetInfoBoxText()
    {
        if (nameText == null || descriptionText == null || activateButton == null || falseActivateButton == null)
        {
            Debug.LogError("[SuperPowerSpawner] UI elements are null in ResetInfoBoxText.");
            return;
        }
        
        nameText.text = currentNameText;
        descriptionText.text = currentDescriptionText;

        bool canActivate = false;
        if (SuperPowerToken.ActiveInstance != null && SuperPowerToken.ActiveInstance.power != null)
            canActivate = CheckIfCardShouldBeSelected(SuperPowerToken.ActiveInstance.power.name);

        activateButton.gameObject.SetActive(canActivate);
        falseActivateButton.gameObject.SetActive(!canActivate);

        if (errorMessageCoroutine != null)
        {
            StopCoroutine(errorMessageCoroutine);
            errorMessageCoroutine = null;
        }
    }

    public void CheckIfBackgroundPanelOpen()
    {
        if (backgroundPanel == null)
        {
            Debug.LogWarning("[SuperPowerSpawner] backgroundPanel is null in CheckIfBackgroundPanelOpen.");
            return;
        }
        
        if (!backgroundPanel.activeSelf) return;
        else
        {
            if (SuperPowerToken.ActiveInstance != null)
            {
                //StartCoroutine(OpenInfoBox(SuperPowerToken.ActiveInstance));
            }
            else
            {
                Debug.LogWarning("[SuperPowerSpawner] SuperPowerToken.ActiveInstance is null in CheckIfBackgroundPanelOpen.");
            }
        }
    }

    public void ReportZaferPuaniToServer()
    {
        int playerNo = DeckController.LocalInstance.thisPlayerNumber;
        int totalPoints = 0;
        foreach (var tokenObj in spawnedSuperPowers)
        {
            if (tokenObj == null) continue;
            var token = tokenObj.GetComponent<SuperPowerToken>();
            if (token != null && token.power is ZaferPuani zaferPower)
            {
                totalPoints += zaferPower.points;
            }
        }
        GameNetworkRelay.Instance.ReportZaferPuaniServerRPC(playerNo, totalPoints);
    }
    
    // ===== GOLD SYSTEM METHODS =====
    
    /// <summary>
    /// Initializes the gold system
    /// </summary>
    private void InitializeGoldSystem()
    {
        currentGold = startingGold;
        UpdateGoldDisplay();
        Debug.Log($"[SuperPowerSpawner] Gold system initialized with {currentGold} gold");
    }
    
    /// <summary>
    /// Resets gold to starting amount (called when new game starts)
    /// </summary>
    public void ResetGoldToStarting()
    {
        currentGold = startingGold;
        UpdateGoldDisplay();
        ClearGoldPopupQueue(); // Clear any pending popups
        Debug.Log($"[SuperPowerSpawner] Gold reset to {currentGold} (new game started)");
    }
    
    /// <summary>
    /// Updates the gold display text
    /// </summary>
    private void UpdateGoldDisplay()
    {
        if (goldDisplayText != null)
        {
            goldDisplayText.text = $"{currentGold}";
        }
    }
    
    /// <summary>
    /// Adds gold to the player's balance
    /// </summary>
    public void AddGold(int amount)
    {
        currentGold = Mathf.Min(currentGold + amount, maxGold);
        UpdateGoldDisplay();
        Debug.Log($"[SuperPowerSpawner] Added {amount} gold. New balance: {currentGold}");
        NotifyServerOfGoldAndPowers();
    }
    
    /// <summary>
    /// Spends gold if player has enough
    /// </summary>
    public bool SpendGold(int amount)
    {
        if (currentGold >= amount)
        {
            currentGold -= amount;
            UpdateGoldDisplay();
            Debug.Log($"[SuperPowerSpawner] Spent {amount} gold. New balance: {currentGold}");
            NotifyServerOfGoldAndPowers();
            return true;
        }
        else
        {
            Debug.Log($"[SuperPowerSpawner] Insufficient gold! Need {amount}, have {currentGold}");
            OnInsufficientGoldFeedback();
            return false;
        }
    }
    
    /// <summary>
    /// Calculates the total value of cards in the center
    /// </summary>
    public int CalculateCenterCardsValue()
    {
        int totalValue = 0;
        
        // Add value of all center cards
        foreach (var centerCard in GameManager.LocalInstance.centerCardsObjects)
        {
            CardInteraction cardInteraction = centerCard.GetComponent<CardInteraction>();
            if (cardInteraction != null)
            {
                Debug.Log($"[SuperPowerSpawner] Center card: {cardInteraction.GetCardID()}");
                totalValue += GetCardValue(cardInteraction.GetCardID());
            }
        }
        
        Debug.Log($"[SuperPowerSpawner] Center cards total value: {totalValue}");
        return totalValue;
    }
    
    /// <summary>
    /// Calculates gold using the new formula: [capturing card value] + [n(n+1)/2] where n = number of center cards
    /// </summary>
    public int CalculateNewGoldValue(int capturingCardValue, int centerCardCount)
    {
        // Formula: Gold equals total number of cards in capture pile (center cards + capturing card)
        int totalGold = centerCardCount + 1;
        
        Debug.Log($"[SuperPowerSpawner] New gold calculation: {centerCardCount} center cards + 1 capturing card = {totalGold} gold");
        return totalGold;
    }
    
    /// <summary>
    /// Calculates the total cards bonus (center cards + capturing card)
    /// </summary>
    public int CalculateCenterCardsBonus(int centerCardCount)
    {
        return centerCardCount + 1;
    }
    
    /// <summary>
    /// Gets the value of a card based on its ID
    /// </summary>
    private int GetCardValue(int[] cardID)
    {
        if (cardID == null || cardID.Length < 2) return 0;
        
        int suit = cardID[0];
        int rank = cardID[1];
        
        return rank; // 2-10
    }
    
    /// <summary>
    /// Called when a capture happens locally
    /// Uses the new gold calculation formula: [capturing card value] + [n(n+1)/2] where n = center card count
    /// </summary>
    public void OnLocalCapture(string playedCard)
    {
        if (CardInteraction.cardLookup == null || !CardInteraction.cardLookup.ContainsKey(playedCard))
        {
            Debug.LogError($"[SuperPowerSpawner] Card {playedCard} not found in cardLookup!");
            return;
        }
        
        // Get the capturing card value
        int capturingCardValue = CardInteraction.cardLookup[playedCard].GetCardID()[1];
        
        // Get the number of center cards (including the capturing card)
        int centerCardCount = GameManager.LocalInstance.centerCards.Count;
        
        // Calculate gold using new formula
        int totalGold = CalculateNewGoldValue(capturingCardValue, centerCardCount);
        int totalCardsInCapture = centerCardCount + 1;  // Center cards + capturing card
        
        Debug.Log($"[SuperPowerSpawner] Capture: Center count {centerCardCount}, Total cards captured {totalCardsInCapture}, Total gold {totalGold}");
        
        // In 2v2 mode, we need to share gold with teammate
        if (Is2v2Mode())
        {
            // Share gold with teammate (50/50 split)
            int sharedGold = totalGold / 2;
            
            // Send gold share to teammate via network
            int teammateNumber = GetTeammateNumber();
            GameManager.LocalInstance.networkRelay.ShareGoldWithTeammateServerRPC(teammateNumber, sharedGold);
            
            Debug.Log($"[SuperPowerSpawner] Local capture in 2v2! Total value: {totalGold}, Shared with teammate: {sharedGold}");
        }
        
        // Add gold to local player
        AddGold(totalGold);
        
        // Show two-stage popup animation
        ShowTwoStageGoldPopup(capturingCardValue, centerCardCount);
        
        Debug.Log($"[SuperPowerSpawner] Added {totalGold} gold to local player using new formula");
    }
    
    /// <summary>
    /// Called when receiving gold from teammate in 2v2 mode
    /// </summary>
    public void ReceiveGoldFromTeammate(int amount)
    {
        AddGold(amount);
        Debug.Log($"[SuperPowerSpawner] Received {amount} gold from teammate");
    }
    
    /// <summary>
    /// Checks if current game mode is 2v2
    /// </summary>
    public bool Is2v2Mode()
    {
        // You may need to adjust this based on how you determine game mode
        // For now, assuming 2v2 if there are 4 players
        Debug.Log($"[SuperPowerSpawner] Is2v2Mode: {DeckController.LocalInstance.playerCount}");    
        return DeckController.LocalInstance != null && GameManager.LocalInstance.networkRelay != null && DeckController.LocalInstance.playerCount == 4;
    }
    
    /// <summary>
    /// Gets teammate number in 2v2 mode
    /// </summary>
    private int GetTeammateNumber()
    {
        // 0-2 and 1-3 are teammates
        if (playerNumber == 0) return 2;
        if (playerNumber == 1) return 3;
        if (playerNumber == 2) return 0;
        if (playerNumber == 3) return 1;
        return -1;
    }
    
    /// <summary>
    /// Sets the local player number
    /// </summary>
    public void SetPlayerNumber(int number)
    {
        playerNumber = number;
        Debug.Log($"[SuperPowerSpawner] Player number set to {number}");
    }
    
    /// <summary>
    /// Get all token data for the token menu system
    /// </summary>
    public List<(GameObject tokenPrefab, SuperPower power)> GetAllTokenData()
    {
        List<(GameObject, SuperPower)> tokenData = new List<(GameObject, SuperPower)>();
        
        Debug.Log($"[SuperPowerSpawner] GetAllTokenData called - checking {superPowerTokens.Count} tokens");
        
        foreach (GameObject tokenPrefab in superPowerTokens)
        {
            if (tokenPrefab == null)
            {
                Debug.LogWarning("[SuperPowerSpawner] Null token prefab found in superPowerTokens list!");
                continue;
            }
            
            SuperPowerToken tokenScript = tokenPrefab.GetComponent<SuperPowerToken>();
            if (tokenScript == null)
            {
                Debug.LogWarning($"[SuperPowerSpawner] Token prefab {tokenPrefab.name} does not have SuperPowerToken component!");
                continue;
            }
            
            SuperPower power = null;
            
            // Try to use the assigned power first
            if (tokenScript.power != null)
            {
                power = tokenScript.power;
                Debug.Log($"[SuperPowerSpawner] Using assigned power for {tokenPrefab.name}: {power.name}");
            }
            // If no assigned power, try to create from className
            else if (!string.IsNullOrEmpty(tokenScript.superPowerClassName))
            {
                Debug.Log($"[SuperPowerSpawner] No assigned power for {tokenPrefab.name}, trying to create from className: {tokenScript.superPowerClassName}");
                
                var type = System.Type.GetType(tokenScript.superPowerClassName);
                if (type != null && typeof(SuperPower).IsAssignableFrom(type))
                {
                    power = ScriptableObject.CreateInstance(type) as SuperPower;
                    if (power != null)
                    {
                        Debug.Log($"[SuperPowerSpawner] Successfully created power from className: {power.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"[SuperPowerSpawner] Failed to create SuperPower instance for {tokenScript.superPowerClassName}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[SuperPowerSpawner] Could not find SuperPower type for {tokenScript.superPowerClassName}");
                }
            }
            else
            {
                Debug.LogWarning($"[SuperPowerSpawner] Token {tokenPrefab.name} has no power assigned and no superPowerClassName!");
                continue;
            }
            
            if (power != null)
            {
                tokenData.Add((tokenPrefab, power));
                Debug.Log($"[SuperPowerSpawner] Added token data: {tokenPrefab.name} -> {power.name} (rarity: {power.rarityMultiplier})");
            }
        }
        
        Debug.Log($"[SuperPowerSpawner] GetAllTokenData returning {tokenData.Count} tokens");
        return tokenData;
    }
    
    /// <summary>
    /// Gets current gold amount
    /// </summary>
    public int GetCurrentGold()
    {
        return currentGold;
    }
    
    /// <summary>
    /// Checks if player has enough gold for a purchase
    /// </summary>
    public bool HasEnoughGold(int requiredAmount)
    {
        return currentGold >= requiredAmount;
    }
    
    /// <summary>
    /// Called when there's insufficient gold for a purchase
    /// </summary>
    private void OnInsufficientGoldFeedback()
    {
        // Empty function for feedback - you can fill this later
        Debug.Log("[SuperPowerSpawner] Insufficient gold feedback triggered");
    }
    
    /// <summary>
    /// Fades the open button out when InfoBox opens
    /// </summary>
    private IEnumerator FadeOpenButtonOut()
    {
        MenuController menuController = FindObjectOfType<MenuController>();
        if (menuController == null || menuController.OpenButtonGameObject == null)
        {
            yield break;
        }
        
        GameObject openButton = menuController.OpenButtonGameObject;
        
        // Get or add CanvasGroup for fading
        CanvasGroup buttonCanvasGroup = GetOrAddCanvasGroup(openButton);
        if (buttonCanvasGroup == null)
        {
            Debug.LogWarning("[SuperPowerSpawner] Could not get/add CanvasGroup to open button");
            openButton.SetActive(false);
            yield break;
        }
        
        // Fade out
        float startAlpha = buttonCanvasGroup.alpha;
        float elapsedTime = 0f;
        
        while (elapsedTime < buttonFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / buttonFadeDuration;
            buttonCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);
            yield return null;
        }
        
        buttonCanvasGroup.alpha = 0f;
        openButton.SetActive(false);
        Debug.Log("[SuperPowerSpawner] Open button faded out and deactivated");
    }
    
    /// <summary>
    /// Fades the open button in when InfoBox closes
    /// </summary>
    private IEnumerator FadeOpenButtonIn()
    {
        MenuController menuController = FindObjectOfType<MenuController>();
        if (menuController == null || menuController.OpenButtonGameObject == null)
        {
            yield break;
        }
        
        GameObject openButton = menuController.OpenButtonGameObject;
        
        // Get or add CanvasGroup for fading
        CanvasGroup buttonCanvasGroup = GetOrAddCanvasGroup(openButton);
        if (buttonCanvasGroup == null)
        {
            Debug.LogWarning("[SuperPowerSpawner] Could not get/add CanvasGroup to open button");
            openButton.SetActive(true);
            yield break;
        }
        
        // Ensure button is active but invisible
        openButton.SetActive(true);
        buttonCanvasGroup.alpha = 0f;
        
        // Fade in
        float elapsedTime = 0f;
        
        while (elapsedTime < buttonFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / buttonFadeDuration;
            buttonCanvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);
            yield return null;
        }
        
        buttonCanvasGroup.alpha = 1f;
        Debug.Log("[SuperPowerSpawner] Open button faded in and activated");
    }
    
    /// <summary>
    /// Rotates the open button 180 degrees on Z axis and moves to X=-1 when InfoBox opens
    /// </summary>
    private IEnumerator RotateOpenButtonOut()
    {
        MenuController menuController = FindObjectOfType<MenuController>();
        if (menuController == null || menuController.OpenButtonGameObject == null)
        {
            yield break;
        }
        
        GameObject openButton = menuController.OpenButtonGameObject;
        
        // Ensure button is active and visible
        openButton.SetActive(true);
        
        // Get current rotation and position
        Vector3 startRotation = openButton.transform.localEulerAngles;
        Vector3 endRotation = startRotation + new Vector3(0f, 0f, 180f);
        
        Vector3 startPosition = openButton.transform.localPosition;
        Vector3 endPosition = new Vector3(-1f, startPosition.y, startPosition.z);
        
        float elapsedTime = 0f;
        
        while (elapsedTime < buttonFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / buttonFadeDuration;
            
            // Use smooth easing for both rotation and position
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            
            // Interpolate rotation and position simultaneously
            Vector3 currentRotation = Vector3.Lerp(startRotation, endRotation, easedProgress);
            Vector3 currentPosition = Vector3.Lerp(startPosition, endPosition, easedProgress);
            
            openButton.transform.localEulerAngles = currentRotation;
            openButton.transform.localPosition = currentPosition;
            
            yield return null;
        }
        
        // Ensure final rotation and position are exact
        openButton.transform.localEulerAngles = endRotation;
        openButton.transform.localPosition = endPosition;
        Debug.Log("[SuperPowerSpawner] Open button rotated 180 degrees on Z-axis and moved to X=-1");
    }
    
    /// <summary>
    /// Rotates the open button back 180 degrees on Z axis and moves to X=-1.5 when InfoBox closes
    /// </summary>
    private IEnumerator RotateOpenButtonIn()
    {
        MenuController menuController = FindObjectOfType<MenuController>();
        if (menuController == null || menuController.OpenButtonGameObject == null)
        {
            yield break;
        }
        
        GameObject openButton = menuController.OpenButtonGameObject;
        
        // Ensure button is active
        openButton.SetActive(true);
        
        // Get current rotation and position
        Vector3 startRotation = openButton.transform.localEulerAngles;
        Vector3 endRotation = startRotation + new Vector3(0f, 0f, 180f);
        
        Vector3 startPosition = openButton.transform.localPosition;
        Vector3 endPosition = new Vector3(-1.5f, startPosition.y, startPosition.z);
        
        float elapsedTime = 0f;
        
        while (elapsedTime < buttonFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / buttonFadeDuration;
            
            // Use smooth easing for both rotation and position
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            
            // Interpolate rotation and position simultaneously
            Vector3 currentRotation = Vector3.Lerp(startRotation, endRotation, easedProgress);
            Vector3 currentPosition = Vector3.Lerp(startPosition, endPosition, easedProgress);
            
            openButton.transform.localEulerAngles = currentRotation;
            openButton.transform.localPosition = currentPosition;
            
            yield return null;
        }
        
        // Ensure final rotation and position are exact
        openButton.transform.localEulerAngles = endRotation;
        openButton.transform.localPosition = endPosition;
        Debug.Log("[SuperPowerSpawner] Open button rotated 180 degrees on Z-axis and moved to X=-1.5");
    }
    
    /// <summary>
    /// Creates and animates a gold popup text at the specified location
    /// </summary>
    public void ShowGoldPopup(int goldAmount)
    {
        if (goldPopupLocation == null && goldDisplayText == null)
        {
            Debug.LogWarning("[SuperPowerSpawner] No goldPopupLocation or goldDisplayText assigned. Cannot show gold popup.");
            return;
        }
        
        StartCoroutine(ShowGoldPopupCoroutine(goldAmount));
    }
    
    /// <summary>
    /// Shows a two-stage gold popup: first the capturing card value, then the center cards bonus
    /// </summary>
    public void ShowTwoStageGoldPopup(int capturingCardValue, int centerCardCount)
    {
        if (goldPopupLocation == null && goldDisplayText == null)
        {
            Debug.LogWarning("[SuperPowerSpawner] No goldPopupLocation or goldDisplayText assigned. Cannot show gold popup.");
            return;
        }
        
        StartCoroutine(ShowTwoStageGoldPopupCoroutine(capturingCardValue, centerCardCount));
    }
    
    /// <summary>
    /// Coroutine that handles the gold popup animation
    /// </summary>
    /// <summary>
    /// Coroutine that handles the gold popup animation: fades in, scales from start to end, then fades out (no scale shrink)
    /// </summary>
    private IEnumerator ShowGoldPopupCoroutine(int goldAmount)
    {
        // Create popup text object
        GameObject popupObject = CreateGoldPopupObject(goldAmount);
        if (popupObject == null) yield break;

        TextMeshProUGUI popupText = popupObject.GetComponent<TextMeshProUGUI>();
        CanvasGroup popupCanvasGroup = GetOrAddCanvasGroup(popupObject);

        // Set initial state
        popupCanvasGroup.alpha = 0f;
        popupObject.transform.localScale = Vector3.one * popupStartScale;

        Vector3 startPosition = popupObject.transform.position;
        Vector3 endPosition = startPosition + new Vector3(0, popupMoveDistance, 0);

        // Phase 1: Fade in and scale up (from start to end scale)
        float elapsed = 0f;
        while (elapsed < popupFadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / popupFadeDuration);

            popupCanvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);
            popupObject.transform.localScale = Vector3.one * Mathf.Lerp(popupStartScale, popupEndScale, progress);

            yield return null;
        }
        popupCanvasGroup.alpha = 1f;
        popupObject.transform.localScale = Vector3.one * popupEndScale;

        // Phase 2: Move upward while visible (no scale shrink)
        elapsed = 0f;
        while (elapsed < popupMoveDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / popupMoveDuration);

            popupObject.transform.position = Vector3.Lerp(startPosition, endPosition, progress);

            yield return null;
        }
        popupObject.transform.position = endPosition;

        // Phase 3: Fade out (keep at end scale)
        elapsed = 0f;
        while (elapsed < popupFadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / popupFadeDuration);

            popupCanvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);

            yield return null;
        }
        popupCanvasGroup.alpha = 0f;

        Destroy(popupObject);
    }
    
    /// <summary>
    /// Coroutine that handles the two-stage gold popup animation
    /// </summary>
    private IEnumerator ShowTwoStageGoldPopupCoroutine(int capturingCardValue, int centerCardCount)
    {
        int centerBonus = CalculateCenterCardsBonus(centerCardCount);
        
        // Stage 1: Show capturing card value
        GameObject popupObject1 = CreateGoldPopupObject(capturingCardValue);
        if (popupObject1 != null)
        {
            yield return StartCoroutine(AnimateSingleGoldPopup(popupObject1));
        }
        
        // Small delay between stages
        yield return new WaitForSeconds(0.3f);
        
        // Stage 2: Show center cards bonus
        GameObject popupObject2 = CreateGoldPopupObject(centerBonus);
        if (popupObject2 != null)
        {
            yield return StartCoroutine(AnimateSingleGoldPopup(popupObject2));
        }
    }
    
    /// <summary>
    /// Animates a single gold popup object
    /// </summary>
    private IEnumerator AnimateSingleGoldPopup(GameObject popupObject)
    {
        if (popupObject == null) yield break;

        TextMeshProUGUI popupText = popupObject.GetComponent<TextMeshProUGUI>();
        CanvasGroup popupCanvasGroup = GetOrAddCanvasGroup(popupObject);

        // Set initial state
        popupCanvasGroup.alpha = 0f;
        popupObject.transform.localScale = Vector3.one * popupStartScale;

        Vector3 startPosition = popupObject.transform.position;
        Vector3 endPosition = startPosition + new Vector3(0, popupMoveDistance, 0);

        // Phase 1: Fade in and scale up (from start to end scale)
        float elapsed = 0f;
        while (elapsed < popupFadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / popupFadeDuration);

            popupCanvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);
            popupObject.transform.localScale = Vector3.one * Mathf.Lerp(popupStartScale, popupEndScale, progress);

            yield return null;
        }
        popupCanvasGroup.alpha = 1f;
        popupObject.transform.localScale = Vector3.one * popupEndScale;

        // Phase 2: Move upward while visible (no scale shrink)
        elapsed = 0f;
        while (elapsed < popupMoveDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / popupMoveDuration);

            popupObject.transform.position = Vector3.Lerp(startPosition, endPosition, progress);

            yield return null;
        }
        popupObject.transform.position = endPosition;

        // Phase 3: Fade out (keep at end scale)
        elapsed = 0f;
        while (elapsed < popupFadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / popupFadeDuration);

            popupCanvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);

            yield return null;
        }
        popupCanvasGroup.alpha = 0f;

        Destroy(popupObject);
    }

    /// <summary>
    /// Creates a gold popup text object
    /// </summary>
    private GameObject CreateGoldPopupObject(int goldAmount)
    {
        GameObject popupObject;
        
        // Use prefab if available, otherwise create from scratch
        if (goldPopupPrefab != null)
        {
            // Use goldPopupLocation as parent if available, otherwise use goldDisplayText parent
            Transform parentTransform = goldPopupLocation != null ? goldPopupLocation : 
                                      (goldDisplayText != null ? goldDisplayText.transform.parent : null);
            
            if (parentTransform == null)
            {
                Debug.LogError("[SuperPowerSpawner] No valid parent transform found for gold popup!");
                return null;
            }
            
            popupObject = Instantiate(goldPopupPrefab, parentTransform);
        }
        else
        {
            // Create popup object from scratch
            popupObject = new GameObject("GoldPopup");
            
            // Use goldPopupLocation as parent if available, otherwise use goldDisplayText parent
            Transform parentTransform = goldPopupLocation != null ? goldPopupLocation : 
                                      (goldDisplayText != null ? goldDisplayText.transform.parent : null);
            
            if (parentTransform == null)
            {
                Debug.LogError("[SuperPowerSpawner] No valid parent transform found for gold popup!");
                return null;
            }
            
            popupObject.transform.SetParent(parentTransform, false);
            
            // Add TextMeshProUGUI component
            TextMeshProUGUI popupText = popupObject.AddComponent<TextMeshProUGUI>();
            popupText.text = $"+{goldAmount}";
            popupText.fontSize = popupFontSize;
            popupText.color = popupTextColor;
            popupText.alignment = TextAlignmentOptions.Center;
            popupText.fontStyle = FontStyles.Bold;
            
            // Add RectTransform setup
            RectTransform rectTransform = popupObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(100, 50);
        }
        
        // Position at the goldPopupLocation if available, otherwise fallback to gold display position
        if (goldPopupLocation != null)
        {
            popupObject.transform.position = goldPopupLocation.position;
        }
        else if (goldDisplayText != null)
        {
            // Fallback to original behavior
            Vector3 goldDisplayPosition = goldDisplayText.transform.position;
            popupObject.transform.position = goldDisplayPosition + Vector3.up * 50f;
        }
        else
        {
            Debug.LogWarning("[SuperPowerSpawner] No goldPopupLocation or goldDisplayText found. Popup will appear at origin.");
            popupObject.transform.position = Vector3.zero;
        }
        
        return popupObject;
    }
    
    /// <summary>
    /// Adds gold for a single card and queues popup animation (new queue-based system)
    /// </summary>
    public void AddGoldWithPopupQueued(int cardValue)
    {
        // Add gold immediately
        AddGold(cardValue);
        
        // Queue the popup for display
        QueueGoldPopup(cardValue);
    }
    
    /// <summary>
    /// Adds a gold value to the popup queue and starts processing if not already running
    /// </summary>
    public void QueueGoldPopup(int goldAmount)
    {
        goldPopupQueue.Enqueue(goldAmount);
        Debug.Log($"[SuperPowerSpawner] Queued gold popup: +{goldAmount} (Queue size: {goldPopupQueue.Count})");
        
        // Start processing the queue if not already running
        if (!isProcessingPopupQueue)
        {
            StartCoroutine(ProcessGoldPopupQueue());
        }
    }
    
    /// <summary>
    /// Processes the gold popup queue, showing each popup with a delay
    /// </summary>
    private IEnumerator ProcessGoldPopupQueue()
    {
        isProcessingPopupQueue = true;
        
        while (goldPopupQueue.Count > 0)
        {
            int goldAmount = goldPopupQueue.Dequeue();
            
            // Show the popup for this gold amount
            ShowGoldPopup(goldAmount);
            
            // Wait before showing the next popup
            yield return new WaitForSeconds(popupDisplayDelay);
        }
        
        isProcessingPopupQueue = false;
    }
    
    /// <summary>
    /// Legacy method for backward compatibility (old system)
    /// </summary>
    public IEnumerator AddGoldWithPopup(int cardValue)
    {
        // Show popup animation
        ShowGoldPopup(cardValue);
        
        // Wait for a short delay to let popup start
        yield return new WaitForSeconds(popupFadeDuration + popupScaleDuration);
        
        // Add the gold
        AddGold(cardValue);
    }
    
    /// <summary>
    /// Public getter for card processing delay
    /// </summary>
    public float CardProcessingDelay => cardProcessingDelay;
    
    /// <summary>
    /// Clears the gold popup queue (useful when starting new rounds)
    /// </summary>
    public void ClearGoldPopupQueue()
    {
        goldPopupQueue.Clear();
        Debug.Log("[SuperPowerSpawner] Gold popup queue cleared.");
    }

    // ========================================
    // CONTEXT MENU FUNCTIONS FOR TESTING
    // ========================================
    
    /// <summary>
    /// Spawn a specific superpower for testing purposes
    /// </summary>
    [ContextMenu("Spawn Ucundan Göz At")]
    public void SpawnUcundanGozAt()
    {
        SpawnSpecificPower("Ucundan Göz At");
    }
    
    [ContextMenu("Spawn Oynayamazsın")]
    public void SpawnOynayamazsin()
    {
        SpawnSpecificPower("Oynayamazsın");
    }
    
    [ContextMenu("Spawn Değiş Tokuş")]
    public void SpawnDegisTokuş()
    {
        SpawnSpecificPower("Değiş Tokuş");
    }
    
    [ContextMenu("Spawn Kapkaç")]
    public void SpawnKapkac()
    {
        SpawnSpecificPower("Kapkaç");
    }
    
    [ContextMenu("Spawn Yandım Anam")]
    public void SpawnYandimAnam()
    {
        SpawnSpecificPower("Yandım Anam");
    }
    
    [ContextMenu("Spawn Baya Baya Bak")]
    public void SpawnBayaBayaBak()
    {
        SpawnSpecificPower("Baya Baya Bak");
    }
    
    [ContextMenu("Spawn Bomba")]
    public void SpawnBomba()
    {
        SpawnSpecificPower("Bomba");
    }
    
    [ContextMenu("Spawn Yapamazsın")]
    public void SpawnYapamazsin()
    {
        SpawnSpecificPower("Yapamazsın");
    }
    
    [ContextMenu("Spawn Bu Daha İyi")]
    public void SpawnBuDahaIyi()
    {
        SpawnSpecificPower("Bu Daha İyi");
    }
    
    [ContextMenu("Spawn Vale Arar")]
    public void SpawnValeArar()
    {
        SpawnSpecificPower("Vale Arar");
    }
    
    [ContextMenu("Spawn Kopyala Yapıştır")]
    public void SpawnKopyalaYapistir()
    {
        SpawnSpecificPower("Kopyala Yapıştır");
    }
    
    [ContextMenu("Spawn Şunu Değiş Tokuş")]
    public void SpawnSunuDegisTokuş()
    {
        SpawnSpecificPower("Şunu Değiş Tokuş");
    }
    
    [ContextMenu("Spawn Şunu Değiş Bunu Tokuş")]
    public void SpawnSunuDegisBunuTokuş()
    {
        SpawnSpecificPower("Şunu Değiş Bunu Tokuş");
    }
    
    [ContextMenu("Spawn Ver Zehri")]
    public void SpawnVerZehri()
    {
        SpawnSpecificPower("Ver Zehri");
    }
    
    [ContextMenu("Spawn Kutsal Deste")]
    public void SpawnKutsalDeste()
    {
        SpawnSpecificPower("Kutsal Deste");
    }
    
    [ContextMenu("Spawn Zafer Puanı")]
    public void SpawnZaferPuani()
    {
        SpawnSpecificPower("Zafer Puanı");
    }
    
    /// <summary>
    /// Spawn a random superpower for testing
    /// </summary>
    [ContextMenu("Spawn Random Power")]
    public void SpawnRandomPower()
    {
        if (superPowerPrefabs.Count > 0)
        {
            List<SuperPower> uniquePowers = new List<SuperPower>(superPowerPrefabs.Keys);
            int randomIndex = Random.Range(0, uniquePowers.Count);
            SuperPower randomPower = uniquePowers[randomIndex];
            SpawnSpecificPower(randomPower.name);
        }
        else
        {
            Debug.LogWarning("[SuperPowerSpawner] No superpowers available to spawn randomly.");
        }
    }
    
    /// <summary>
    /// Spawn multiple random powers for testing
    /// </summary>
    [ContextMenu("Spawn 3 Random Powers")]
    public void SpawnThreeRandomPowers()
    {
        ReadyToSpawnSuperPowers(3, null, 150f);
    }
    
    /// <summary>
    /// Spawn a specific superpower by name using the proper spawning system
    /// </summary>
    private void SpawnSpecificPower(string powerName)
    {
        // Find the power in the unique powers from superPowerPrefabs
        SuperPower targetPower = null;
        foreach (var power in superPowerPrefabs.Keys)
        {
            if (power.name == powerName)
            {
                targetPower = power;
                break;
            }
        }
        
        if (targetPower == null)
        {
            Debug.LogError($"[SuperPowerSpawner] Power '{powerName}' not found in available powers!");
            return;
        }
        
        // Check if we have a prefab for this power (should always be true since we found it in Keys)
        if (!superPowerPrefabs.ContainsKey(targetPower))
        {
            Debug.LogError($"[SuperPowerSpawner] No prefab found for power '{powerName}'!");
            return;
        }
        
        // Use the proper spawning system like the kese does
        Debug.Log($"[SuperPowerSpawner] Spawning {powerName} using proper spawning system");
        StartCoroutine(SpawnSuperPower(targetPower, null, 150f));
    }
    
    /// <summary>
    /// Clear all spawned superpowers for testing
    /// </summary>
    [ContextMenu("Clear All Spawned Powers")]
    public void ClearAllSpawnedPowers()
    {
        foreach (var power in spawnedSuperPowers.ToArray())
        {
            if (power != null)
            {
                DestroyImmediate(power);
            }
        }
        spawnedSuperPowers.Clear();
        Debug.Log("[SuperPowerSpawner] All spawned superpowers cleared.");
        NotifyServerOfGoldAndPowers();
    }
    
    /// <summary>
    /// Spawn all powers for comprehensive testing
    /// </summary>
    [ContextMenu("Spawn All Powers")]
    public void SpawnAllPowers()
    {
        foreach (var power in superPowerPrefabs.Keys)
        {
            SpawnSpecificPower(power.name);
        }
        Debug.Log($"[SuperPowerSpawner] Attempted to spawn all {superPowerPrefabs.Count} unique powers.");
    }
    
    /// <summary>
    /// Print current power list for debugging
    /// </summary>
    [ContextMenu("Print Available Powers")]
    public void PrintAvailablePowers()
    {
        List<SuperPower> uniquePowers = new List<SuperPower>(superPowerPrefabs.Keys);
        Debug.Log($"[SuperPowerSpawner] Available unique powers ({uniquePowers.Count}):");
        for (int i = 0; i < uniquePowers.Count; i++)
        {
            var power = uniquePowers[i];
            Debug.Log($"  {i}: {power.name} (rarity: {power.rarityMultiplier}) - {power.description}");
        }
        
        Debug.Log($"[SuperPowerSpawner] Available prefabs ({superPowerPrefabs.Count}):");
        foreach (var kvp in superPowerPrefabs)
        {
            Debug.Log($"  {kvp.Key.name}: {kvp.Value.name}");
        }
    }
    
    /// <summary>
    /// Comprehensive power validation - checks all expected powers against available ones
    /// </summary>
    [ContextMenu("Validate All Powers")]
    public void ValidateAllPowers()
    {
        // List of all expected power names from SuperPowerController
        string[] expectedPowerNames = {
            "Ucundan Göz At",
            "Oynayamazsın", 
            "Değiş Tokuş",
            "Kapkaç",
            "Vale Arar",
            "Kopyala Yapıştır",
            "Baya Baya Bak",
            "Bomba",
            "Yapamazsın",
            "Ver Zehri",
            "Kutsal Deste",
            "Bu Daha İyi",
            "Şunu Değiş Tokuş",
            "Şunu Değiş Bunu Tokuş",
            "Zafer Puanı",
            "Yandım Anam"
        };
        
        // List of corresponding class names
        string[] expectedClassNames = {
            "UcundanGözAt",
            "Oynayamazsın",
            "DeğişTokuş", 
            "Kapkaç",
            "ValeArar",
            "KopyalaYapistir",
            "BayaBayaBak",
            "Bomba",
            "Yapamazsın",
            "VerZehri",
            "KutsalDeste",
            "BuDahaİyi",
            "SunuDegisTokus",
            "SunuDegisBunuTokus",
            "ZaferPuani",
            "YandımAnam"
        };
        
        Debug.Log("=== POWER VALIDATION REPORT ===");
        Debug.Log($"Expected powers: {expectedPowerNames.Length}");
        Debug.Log($"Available powers: {superPowerPrefabs.Count}");
        Debug.Log($"Token prefabs: {superPowerTokens.Count}");
        
        // Check each expected power
        for (int i = 0; i < expectedPowerNames.Length; i++)
        {
            string powerName = expectedPowerNames[i];
            string className = expectedClassNames[i];
            
            // Check if power exists in superPowerPrefabs
            bool powerExists = false;
            SuperPower foundPower = null;
            foreach (var power in superPowerPrefabs.Keys)
            {
                if (power.name == powerName)
                {
                    powerExists = true;
                    foundPower = power;
                    break;
                }
            }
            
            if (powerExists)
            {
                Debug.Log($"✅ {powerName} - FOUND (rarity: {foundPower.rarityMultiplier})");
            }
            else
            {
                Debug.LogError($"❌ {powerName} - MISSING from superPowerPrefabs!");
                
                // Check if token prefab exists
                bool tokenExists = false;
                string tokenClassName = "";
                foreach (var token in superPowerTokens)
                {
                    if (token == null) continue;
                    
                    var tokenScript = token.GetComponent<SuperPowerToken>();
                    if (tokenScript != null && !string.IsNullOrEmpty(tokenScript.superPowerClassName))
                    {
                        tokenClassName = tokenScript.superPowerClassName;
                        if (tokenClassName == className)
                        {
                            tokenExists = true;
                            Debug.LogWarning($"   Token prefab exists: {token.name} with className: {tokenClassName}");
                            break;
                        }
                    }
                }
                
                if (!tokenExists)
                {
                    Debug.LogError($"   No token prefab found with className: {className}");
                }
                
                // Test type resolution
                var type = System.Type.GetType(className);
                if (type == null)
                {
                    Debug.LogError($"   Type resolution failed for: {className}");
                }
                else
                {
                    Debug.Log($"   Type resolution successful for: {className}");
                }
            }
        }
        
        Debug.Log("=== END VALIDATION REPORT ===");
    }
    
    /// <summary>
    /// Debug token prefab setup
    /// </summary>
    [ContextMenu("Debug Token Prefabs")]
    public void DebugTokenPrefabs()
    {
        Debug.Log("=== TOKEN PREFAB DEBUG ===");
        Debug.Log($"Total token prefabs: {superPowerTokens.Count}");
        
        for (int i = 0; i < superPowerTokens.Count; i++)
        {
            var token = superPowerTokens[i];
            if (token == null)
            {
                Debug.LogError($"Token {i}: NULL");
                continue;
            }
            
            var tokenScript = token.GetComponent<SuperPowerToken>();
            if (tokenScript == null)
            {
                Debug.LogError($"Token {i} ({token.name}): No SuperPowerToken component");
                continue;
            }
            
            string className = tokenScript.superPowerClassName;
            if (string.IsNullOrEmpty(className))
            {
                Debug.LogWarning($"Token {i} ({token.name}): No superPowerClassName set");
                continue;
            }
            
            // Test type resolution
            var type = System.Type.GetType(className);
            if (type == null)
            {
                Debug.LogError($"Token {i} ({token.name}): Type resolution failed for className '{className}'");
            }
            else if (!typeof(SuperPower).IsAssignableFrom(type))
            {
                Debug.LogError($"Token {i} ({token.name}): Type '{className}' is not a SuperPower");
            }
            else
            {
                Debug.Log($"Token {i} ({token.name}): className='{className}' ✅");
            }
        }
        
        Debug.Log("=== END TOKEN PREFAB DEBUG ===");
    }
    
    /// <summary>
    /// Force rebuild the power dictionary - useful when token prefabs are updated
    /// </summary>
    [ContextMenu("Rebuild Power Dictionary")]
    public void RebuildPowerDictionary()
    {
        Debug.Log("[SuperPowerSpawner] Rebuilding power dictionary...");
        DictionaryCreation();
        Debug.Log($"[SuperPowerSpawner] Power dictionary rebuilt. Found {superPowerPrefabs.Count} powers.");
    }
    
    /// <summary>
    /// Test spawning each power individually to identify which ones fail
    /// </summary>
    [ContextMenu("Test Spawn All Powers")]
    public void TestSpawnAllPowers()
    {
        string[] expectedPowerNames = {
            "Ucundan Göz At",
            "Oynayamazsın", 
            "Değiş Tokuş",
            "Kapkaç",
            "Vale Arar",
            "Kopyala Yapıştır",
            "Baya Baya Bak",
            "Bomba",
            "Yapamazsın",
            "Ver Zehri",
            "Kutsal Deste",
            "Bu Daha İyi",
            "Şunu Değiş Tokuş",
            "Şunu Değiş Bunu Tokuş",
            "Zafer Puanı",
            "Yandım Anam"
        };
        
        Debug.Log("=== TESTING POWER SPAWNING ===");
        
        for (int i = 0; i < expectedPowerNames.Length; i++)
        {
            string powerName = expectedPowerNames[i];
            Debug.Log($"Testing spawn: {powerName}");
            
            try
            {
                SpawnSpecificPower(powerName);
                Debug.Log($"✅ {powerName} - Spawn test successful");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ {powerName} - Spawn test failed: {e.Message}");
            }
            
            // Small delay between spawns
            if (i < expectedPowerNames.Length - 1)
            {
                System.Threading.Thread.Sleep(100);
            }
        }
        
        Debug.Log("=== END SPAWN TESTING ===");
    }
    
    /// <summary>
    /// Add testing gold
    /// </summary>
    [ContextMenu("Add 50 Gold")]
    public void AddTestingGold()
    {
        AddGold(50);
        Debug.Log($"[SuperPowerSpawner] Added 50 gold. Current total: {currentGold}");
    }
    
    [ContextMenu("Test New Gold Calculation")]
    public void TestNewGoldCalculation()
    {
        Debug.Log("=== Testing New Gold Calculation Formula ===");
        
        // Test cases: [capturing card value, center card count, expected total]
        int[][] testCases = {
            new int[] {5, 1, 6},    // 5 + 1(2)/2 = 5 + 1 = 6
            new int[] {3, 2, 6},    // 3 + 2(3)/2 = 3 + 3 = 6  
            new int[] {7, 3, 13},   // 7 + 3(4)/2 = 7 + 6 = 13
            new int[] {2, 4, 12},   // 2 + 4(5)/2 = 2 + 10 = 12
            new int[] {10, 5, 25}   // 10 + 5(6)/2 = 10 + 15 = 25
        };
        
        foreach (var testCase in testCases)
        {
            int cardValue = testCase[0];
            int centerCount = testCase[1];
            int expected = testCase[2];
            
            int calculated = CalculateNewGoldValue(cardValue, centerCount);
            
            Debug.Log($"Card Value: {cardValue}, Center Count: {centerCount}");
            Debug.Log($"Total: {calculated}, Expected: {expected}");
            Debug.Log($"Result: {(calculated == expected ? "PASS" : "FAIL")}");
            Debug.Log("---");
        }
    }
    
    [ContextMenu("Test Two-Stage Popup")]
    public void TestTwoStagePopup()
    {
        Debug.Log("[SuperPowerSpawner] Testing two-stage popup animation");
        ShowTwoStageGoldPopup(5, 3); // Card value 5, 3 center cards
    }
    
    /// <summary>
    /// Test open button rotation out (for debugging)
    /// </summary>
    [ContextMenu("Test Rotate Open Button Out")]
    public void TestRotateOpenButtonOut()
    {
        StartCoroutine(RotateOpenButtonOut());
    }
    
    /// <summary>
    /// Test open button rotation in (for debugging)
    /// </summary>
    [ContextMenu("Test Rotate Open Button In")]
    public void TestRotateOpenButtonIn()
    {
        StartCoroutine(RotateOpenButtonIn());
    }
    
    /// <summary>
    /// Reset open button rotation to 0 degrees (for debugging)
    /// </summary>
    [ContextMenu("Reset Open Button Rotation")]
    public void ResetOpenButtonRotation()
    {
        MenuController menuController = FindObjectOfType<MenuController>();
        if (menuController != null && menuController.OpenButtonGameObject != null)
        {
            Vector3 resetRotation = menuController.OpenButtonGameObject.transform.localEulerAngles;
            resetRotation.y = 0f;
            menuController.OpenButtonGameObject.transform.localEulerAngles = resetRotation;
            Debug.Log("[SuperPowerSpawner] Open button rotation reset to 0 degrees");
        }
        else
        {
            Debug.LogWarning("[SuperPowerSpawner] Could not reset open button rotation - MenuController or OpenButtonGameObject is null");
        }
    }
    
    /// <summary>
    /// Debug method to check current InfoBox and menu page states
    /// </summary>
    [ContextMenu("Debug InfoBox States")]
    public void DebugInfoBoxStates()
    {
        Debug.Log($"[SuperPowerSpawner] InfoBox States:");
        Debug.Log($"  - isInfoBoxOpen: {isInfoBoxOpen}");
        Debug.Log($"  - isMenuPageOpen: {isMenuPageOpen}");
        Debug.Log($"  - Active Token: {(SuperPowerToken.ActiveInstance != null ? SuperPowerToken.ActiveInstance.power.name : "None")}");
        Debug.Log($"  - Name Text: '{(nameText != null ? nameText.text : "null")}'");
        Debug.Log($"  - Description Text: '{(descriptionText != null ? descriptionText.text : "null")}'");
        
        if (isInfoBoxOpen && !isMenuPageOpen)
        {
            Debug.Log("  - Status: InfoBox is open showing power information");
        }
        else if (isInfoBoxOpen && isMenuPageOpen)
        {
            Debug.Log("  - Status: InfoBox is open showing menu page");
        }
        else if (!isInfoBoxOpen)
        {
            Debug.Log("  - Status: InfoBox is closed");
        }
    }
    
    /// <summary>
    /// Test method to manually clear InfoBox content (for debugging)
    /// </summary>
    [ContextMenu("Test Clear InfoBox Content")]
    public void TestClearInfoBoxContent()
    {
        Debug.Log("[SuperPowerSpawner] Testing InfoBox content clearing...");
        ClearInfoBoxContent();
    }
    
    /// <summary>
    /// Spawn a specific power at a specific position (like kese does)
    /// </summary>
    [ContextMenu("Spawn Kapkaç at Center")]
    public void SpawnKapkacAtCenter()
    {
        SuperPower kapkacPower = null;
        foreach (var power in superPowerPrefabs.Keys)
        {
            if (power.name == "Kapkaç")
            {
                kapkacPower = power;
                break;
            }
        }
        
        if (kapkacPower != null)
        {
            Vector3 centerPos = centerPosition;
            Debug.Log($"[SuperPowerSpawner] Spawning Kapkaç at center position {centerPos}");
            StartCoroutine(SpawnSuperPower(kapkacPower, centerPos, 150f));
        }
        else
        {
            Debug.LogError("[SuperPowerSpawner] Kapkaç power not found!");
        }
    }
    
    /// <summary>
    /// Spawn a specific power at mouse position for testing
    /// </summary>
    [ContextMenu("Spawn Random Power at Mouse")]
    public void SpawnRandomPowerAtMouse()
    {
        if (superPowerPrefabs.Count > 0)
        {
            List<SuperPower> uniquePowers = new List<SuperPower>(superPowerPrefabs.Keys);
            int randomIndex = Random.Range(0, uniquePowers.Count);
            SuperPower randomPower = uniquePowers[randomIndex];
            
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 10f; // Distance from camera
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            
            Debug.Log($"[SuperPowerSpawner] Spawning {randomPower.name} at mouse position {worldPos}");
            StartCoroutine(SpawnSuperPower(randomPower, worldPos, 150f));
        }
        else
        {
            Debug.LogWarning("[SuperPowerSpawner] No superpowers available to spawn randomly.");
        }
    }

    public void SetNameText(string name)
    {
        nameText.text = name;
    }

    public void SetDescriptionText(string description)
    {
        descriptionText.text = description;
    }

    public void EnqueueOpenInfoBox(SuperPowerToken token)
    {
        infoBoxAnimationQueue.Enqueue(OpenInfoBox(token));
        TryRunNextInfoBoxAnimation();
    }

    public void EnqueueCloseInfoBox()
    {
        infoBoxAnimationQueue.Enqueue(CloseInfoBox());
        TryRunNextInfoBoxAnimation();
    }

    private void TryRunNextInfoBoxAnimation()
    {
        if (!isInfoBoxAnimationRunning && infoBoxAnimationQueue.Count > 0)
        {
            StartCoroutine(RunNextInfoBoxAnimation());
        }
    }

    private IEnumerator RunNextInfoBoxAnimation()
    {
        isInfoBoxAnimationRunning = true;
        while (infoBoxAnimationQueue.Count > 0)
        {
            IEnumerator anim = infoBoxAnimationQueue.Dequeue();
            yield return StartCoroutine(anim);
            yield return null; // Wait a frame before next animation
        }
        isInfoBoxAnimationRunning = false;
    }
}