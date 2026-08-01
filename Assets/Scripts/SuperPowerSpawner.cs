using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SuperPowerSpawner : MonoBehaviour
{
    [SerializeField] private Transform infoBoxOriginalPosition; // Original position stored at Start
    public static SuperPowerSpawner LocalInstance { get; private set; }
    public bool isDisconnectingCleanUp = false;
    private Transform infoBoxReachPoint; // Retrieved from ScreenEdgePositionAdjuster
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
    [SerializeField] private float animationDuration = 0.25f; // Duration of the movement animation
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
                
            }
        }
    }

    void Start()
    {
        // Get reach points from ScreenEdgePositionAdjuster (centralized source)
        ScreenEdgePositionAdjuster screenEdgeAdjuster = FindObjectOfType<ScreenEdgePositionAdjuster>();
        if (screenEdgeAdjuster != null)
        {
            infoBoxReachPoint = screenEdgeAdjuster.GetReachPointTransform("InfoBox");
            if (infoBoxReachPoint == null)
            {
                
            }

            // Get the original position from ScreenEdgePositionAdjuster
            infoBoxOriginalPosition = screenEdgeAdjuster.GetOutsideReachPointTransform("InfoBox");
            if (infoBoxOriginalPosition == null)
            {
                
            }
        }
        else
        {
            
        }

        // Validate UI elements are properly initialized
        if (backgroundPanel == null || infoBoxCanvas == null || nameText == null || 
            descriptionText == null || activateButton == null || falseActivateButton == null || closeButton == null)
        {
            
            GetUIElements();
        }

        // Validate reach point
        if (infoBoxReachPoint == null)
        {
            
        }
        
        // Initialize InfoBox state (closed but active)
        if (backgroundPanel != null && infoBoxOriginalPosition != null)
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
        
        // Validate gold popup location
        if (goldPopupLocation == null)
        {
            
        }
        
        // Initialize gold system
        InitializeGoldSystem();
        
        // Get DeckController reference
        deckController = FindObjectOfType<DeckController>();
        if (deckController == null)
        {
            
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
        
        backgroundPanel = GameObject.Find("InfoBoxBackGroundPanel")?.gameObject;
        
        if (backgroundPanel == null)
        {
            
            return;
        }
        
        infoBoxCanvas = backgroundPanel.transform.Find("InfoBoxCanvas")?.gameObject;
        
        if (infoBoxCanvas == null)
        {
            
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
            
        
        if (closeButton != null)
        {
            
        }

        // Debug what we found
        
        
        
        
        
        
        
        
        
        // CRITICAL: Detailed close button debugging
        if (closeButton != null)
        {
            
            
            
            
            
            
            
            
            
            
            
            // Check if button has a collider
            var collider = closeButton.GetComponent<Collider>();
            
            if (collider != null)
            {
                
                
            }
            
            // Check if button has a CanvasGroup
            var canvasGroup = closeButton.GetComponent<CanvasGroup>();
            
            if (canvasGroup != null)
            {
                
                
                
            }
        }
        else
        {
            
        }

        if (nameText == null || descriptionText == null || activateButton == null || closeButton == null)
        {
            
            if (nameText == null) 
            if (descriptionText == null) 
            if (activateButton == null) 
            if (closeButton == null) 
            return;
        }

        // CRITICAL FIX: Remove existing listeners before adding new ones to prevent multiple listeners
        
        activateButton.onClick.RemoveAllListeners();
        falseActivateButton.onClick.RemoveAllListeners();
        closeButton.onClick.RemoveAllListeners();
        
        
        
        
        activateButton.onClick.AddListener(OnTokenDropped);
        falseActivateButton.onClick.AddListener(GetActiveButtonErrorMessage);
        
        
        closeButton.onClick.AddListener(() =>
        {
            
            
            
            
            
            if (SuperPowerToken.ActiveInstance != null)
            {
                
                RemoveSpawnedSuperPower(SuperPowerToken.ActiveInstance.gameObject);
                UpdateTokenPositions();
                StartCoroutine(SuperPowerToken.ActiveInstance.FadeOutSprite());
                StartCoroutine(CloseInfoBox());
            }
            else
            {
                
                StartCoroutine(CloseInfoBox());
            }
        });
        

        StartCoroutine(CloseInfoBoxImmediate()); // Ensure the info box is closed initially without animation
    }

    private void OnTokenDropped()
    {
        
        
        // Check if it's the player's turn before activating
        if (GameManager.LocalInstance != null && !GameManager.LocalInstance.IsLocalPlayerTurn())
        {
            
            ShowOutOfTurnErrorMessage();
            return;
        }
        
        SuperPowerToken.ActiveInstance.OnTokenDropped();
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
        

        // Check for null references
        if (backgroundPanel == null)
        {
            
            yield break;
        }

        if (superPowerToken == null || superPowerToken.power == null)
        {
            
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
                    
                }
            }
        }
        else if (!isInfoBoxOpen)
        {
            // Case 1: InfoBox was truly closed - animate from original position to reach point
            
            backgroundPanel.SetActive(true);

            // Set to original position (off-screen)
            backgroundPanel.transform.position = infoBoxOriginalPosition.position;
            

            // Update content immediately for Case 1 (no delay needed for initial opening)
            // Check if this is a menu token
            if (superPowerToken.power is MenuPagePower)
            {
                // For menu tokens, set menu flag to true and don't update content
                isMenuPageOpen = true;
                
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
                
                currentAnimationCoroutine = StartCoroutine(AnimateInfoBoxPosition(infoBoxReachPoint.position));
                yield return currentAnimationCoroutine;
                currentAnimationCoroutine = null;
                
            }
            else
            {
                
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
                    
                }
            }

            // Don't play page change animation for initial opening
            // Note: isMenuPageOpen is already set to false by UpdateInfoBoxContent() above
            
        }
        else
        {
            // Fallback: ensure it's active and positioned correctly
            backgroundPanel.SetActive(true);
            // Position at reach point if available, otherwise at original position
            if (infoBoxReachPoint != null)
            {
                backgroundPanel.transform.position = infoBoxReachPoint.position;
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
            
            // Check if this is a menu token
            if (superPowerToken.power is MenuPagePower)
            {
                // For menu tokens, set menu flag to true and don't update content
                isMenuPageOpen = true;
                
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
                
                //deckController.ShowcaseAllOtherHands();
                
            }
            else
            {
                // Stop showcasing if switching to a power that doesn't require card selection
                
                //deckController.ExitShowcaseAllOtherHands();
                
            }
        }
        else
        {
            
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
        

        // Check for null references
        if (backgroundPanel == null)
        {
            
            yield break;
        }

        if (!isInfoBoxOpen)
        {
            yield break; // Already closed
        }

        // This keeps the InfoBox open for the entire multi-step selection process
        if (GameManager.LocalInstance != null && 
            (GameManager.LocalInstance.isSunuDegisTokusActive || 
            GameManager.LocalInstance.isKopyalaActive || 
            GameManager.LocalInstance.isSunuDegisBunuTokusActive))
        {
            yield break; // Don't close during selection
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
        
        currentAnimationCoroutine = StartCoroutine(AnimateInfoBoxPosition(infoBoxOriginalPosition.position));
        yield return currentAnimationCoroutine;
        currentAnimationCoroutine = null;
        

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
                
            }
            else if (isPeekPowerActive)
            {
                
                // Don't stop showcase immediately for peek powers - let the animation handle it
            }
            else
            {
                
                ForceStopHandShowcase();
            }
        }
        else
        {
            
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
                
            }
        }
        
        SuperPowerToken.ActiveInstance = null;
        isInfoBoxOpen = false;
        isMenuPageOpen = false; // Reset menu page flag when InfoBox is closed
    }

    public void TriggerPageChange()
    {
        if (backgroundPanel != null)
        {
            UIFrameAnimator frameAnimator = backgroundPanel.GetComponent<UIFrameAnimator>();
            if (frameAnimator != null)
            {
                frameAnimator.TriggerPageChange();
            }
        }
    }

    public IEnumerator SwitchMenuTierWithAnimation(System.Action updateAction)
    {
        TriggerPageChange();
        
        CanvasGroup canvasGroup = GetOrAddCanvasGroup(infoBoxCanvas);
        if (canvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroups(new CanvasGroup[] { canvasGroup }, 0f, fadeDuration));
        }

        updateAction?.Invoke();

        if (canvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroups(new CanvasGroup[] { canvasGroup }, 1f, fadeDuration));
        }
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
                
            }
            else if (isPeekPowerActive)
            {
                
                // Don't stop showcase immediately for peek powers - let the animation handle it
            }
            else
            {
                
                ForceStopHandShowcase();
            }
        }
        else
        {
            
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
            
            yield break;
        }

        Vector3 startPosition = backgroundPanel.transform.position;
        float elapsedTime = 0f;
        
        

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
        
    }

    // Rest of your existing code remains the same...
    public List<string> restirictedPowersName_CardNeedToBeSelected = new List<string> { "Şunu Değiş Tokuş"};
    private List<string> restirictedPowersName_CenterNotEmpty = new List<string> { "Bu Daha İyi", "Bomba" };
    public List<string> restirictedPowersName_WaitForSwap = new List<string> { "Bu Daha İyi", "Şunu Değiş Tokuş", "Şunu Değiş Bunu Tokuş", "Değiş Tokuş", "Kopyala Yapıştır", "Kapkaç", "Yandım Anam"};
    // Powers that require automatic hand showcasing when opened
    private List<string> powersRequiringHandShowcase = new List<string> { "Kapkaç", "Yandım Anam", "Bu Daha İyi", "Kopyala Yapıştır" };
    
    // Powers that require hand showcasing after activation (for dual selection)
    private List<string> powersRequiringHandShowcaseAfterActivation = new List<string> { "Değiş Tokuş", "Şunu Değiş Tokuş", "Kopyala Yapıştır", "Şunu Değiş Bunu Tokuş" };
    
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
        
        
        if (deckController == null)
        {
            
            return;
        }
        
        if (!PowerRequiresHandShowcaseAfterActivation(powerName))
        {
            
            return;
        }
        
        
        //deckController.ShowcaseAllOtherHands();
        
    }
    
    /// <summary>
    /// Stop hand showcasing (called from GameManager when dual selection is complete)
    /// </summary>
    public void StopHandShowcase()
    {
        
        
        if (deckController == null)
        {
            
            return;
        }
        
        
        //deckController.ExitShowcaseAllOtherHands();
        
    }
    
     /// <summary>
     /// Force stop hand showcasing (used when InfoBox closes and no dual selection is active)
     /// </summary>
     public void ForceStopHandShowcase()
     {
         
         
         if (deckController == null)
         {
             
             return;
         }
         
         
         //deckController.ExitShowcaseAllOtherHands();
         
     }
     
     /// <summary>
     /// Called when peek animation completes - ensures InfoBox closes properly
     /// </summary>
     public void OnPeekAnimationComplete()
     {
         
         
         // Check if we have a peek power active and InfoBox is still open
         if (SuperPowerToken.ActiveInstance != null && 
             (SuperPowerToken.ActiveInstance.superPowerClassName == "UcundanGözAt" || 
              SuperPowerToken.ActiveInstance.superPowerClassName == "BayaBayaBak") &&
             isInfoBoxOpen)
         {
             
             
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
        
        
    }

    public void InitializeSuperPowers()
    {
        DictionaryCreation();
        playerPowerPoolTransform = GameObject.Find("PlayerPowerPool")?.transform;
            
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
                
                continue;
            }

            string className = tokenScript.superPowerClassName;
            if (string.IsNullOrEmpty(className))
            {
                
                continue;
            }

            var type = System.Type.GetType(className);
            if (type == null || !typeof(SuperPower).IsAssignableFrom(type))
            {
                
                continue;
            }

            SuperPower powerInstance = ScriptableObject.CreateInstance(type) as SuperPower;
            if (powerInstance == null)
            {
                
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
                
            }
        }
        
        
    }

    [ContextMenu("Ready to Spawn Super Powers")]
    /// <summary>
    /// Spawns multiple powers with cost-tier-weighted random selection.
    /// Lower powerCostTier = higher probability of being selected.
    /// Each power costs 1 gold.
    /// </summary>
    public void ReadyToSpawnSuperPowers(int numberOfSuperPowersToSpawn = 2, Vector3? spawnOrigin = null, float spawnScale = 150f, int costMode = 1, bool skipInitialDelay = false)
    {
        StartCoroutine(ReadyToSpawnSuperPower(numberOfSuperPowersToSpawn, spawnOrigin, spawnScale, costMode, skipInitialDelay));
    }

    private IEnumerator ReadyToSpawnSuperPower(int numberOfSuperPowersToSpawn, Vector3? spawnOrigin, float spawnScale, int costMode, bool skipInitialDelay)
    {
        if (!skipInitialDelay)
            yield return new WaitForSeconds(1f);

        for (int i = 0; i < numberOfSuperPowersToSpawn; i++)
        {
            StartCoroutine(SpawnSuperPower(GetInverseWeightedRandomSuperPower(costMode), spawnOrigin, spawnScale, skipInitialDelay));
            if (!skipInitialDelay)
                yield return new WaitForSeconds(0.5f);
            else
                yield return null; // Small gap if multiple are spawned fast
        }
    }

    /// <summary>
    /// Selects a random power using cost-tier-weighted probability,
    /// boosted by costMode. Powers in the matching tier get a 5x weight boost.
    /// Tier 4 (ZaferPuani) is never boosted. Disabled powers are excluded.
    /// </summary>
    private SuperPower GetInverseWeightedRandomSuperPower(int costMode = 1)
    {
        if (superPowerPrefabs.Count == 0)
        {
            
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
            
            return null;
        }

        // Calculate weights: base = 1/powerCostTier, boosted if tier matches mode (tiers 1-3 only)
        List<float> weights = new List<float>();
        float totalWeight = 0f;

        for (int i = 0; i < activePowers.Count; i++)
        {
            float weight = 1f / activePowers[i].powerCostTier;
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
                
                return selectedPower;
            }
        }

        // Fallback (should rarely happen due to floating point precision)
        SuperPower fallbackPower = activePowers[activePowers.Count - 1];
        
        return fallbackPower;
    }

    // Update SpawnSuperPower to accept origin and scale
    private IEnumerator SpawnSuperPower(SuperPower superPower, Vector3? spawnOrigin, float spawnScale, bool skipInitialDelay = false)
    {
        if (superPower == null)
        {
            
            yield break;
        }

        if (spawnedSuperPowers.Count >= maxSuperPowers)
        {
            
            yield break;
        }

        GameObject placeholder = new GameObject("TokenPlaceholder");
        spawnedSuperPowers.Add(placeholder);
        UpdateTokenPositions();

        if (!skipInitialDelay)
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
            
        }
        else
        {
            
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
        
    }
    
    /// <summary>
    /// Resets gold to starting amount (called when new game starts)
    /// </summary>
    public void ResetGoldToStarting()
    {
        currentGold = startingGold;
        UpdateGoldDisplay();
        ClearGoldPopupQueue(); // Clear any pending popups
        
    }
    
    /// <summary>
    /// Updates the gold display text via HesapMakinesiController
    /// </summary>
    private void UpdateGoldDisplay()
    {
        HesapMakinesiController hesapController = FindFirstObjectByType<HesapMakinesiController>();
        if (hesapController != null)
        {
            hesapController.UpdateCoinAmountDisplays(currentGold);
            
        }
    }
    
    /// <summary>
    /// Adds gold to the player's balance
    /// </summary>
    public void AddGold(int amount)
    {
        currentGold = Mathf.Min(currentGold + amount, maxGold);
        UpdateGoldDisplay();
        
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
            
            NotifyServerOfGoldAndPowers();
            return true;
        }
        else
        {
            
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
                
                totalValue += GetCardValue(cardInteraction.GetCardID());
            }
        }
        
        
        return totalValue;
    }
    
    /// <summary>
    /// Calculates gold using the new formula: [capturing card value] + [n(n+1)/2] where n = number of center cards
    /// </summary>
    public int CalculateNewGoldValue(int capturingCardValue, int centerCardCount)
    {
        // Formula: Gold equals total number of cards in capture pile (center cards + capturing card)
        int totalGold = centerCardCount + 1;
        
        
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
            
            return;
        }
        
        // Get the capturing card value
        int capturingCardValue = CardInteraction.cardLookup[playedCard].GetCardID()[1];
        
        // Get the number of center cards (including the capturing card)
        int centerCardCount = GameManager.LocalInstance.centerCards.Count;
        
        // Calculate gold using new formula
        int totalGold = CalculateNewGoldValue(capturingCardValue, centerCardCount);
        int totalCardsInCapture = centerCardCount + 1;  // Center cards + capturing card
        
        
        
        // In 2v2 mode, we need to share gold with teammate
        if (Is2v2Mode())
        {
            // Share gold with teammate (50/50 split)
            int sharedGold = totalGold / 2;
            
            // Send gold share to teammate via network
            int teammateNumber = GetTeammateNumber();
            GameManager.LocalInstance.networkRelay.ShareGoldWithTeammateServerRPC(teammateNumber, sharedGold);
            
            
        }
        
        // Add gold to local player
        AddGold(totalGold);
        
        // Show two-stage popup animation
        ShowTwoStageGoldPopup(capturingCardValue, centerCardCount);
        
        
    }
    
    /// <summary>
    /// Called when receiving gold from teammate in 2v2 mode
    /// </summary>
    public void ReceiveGoldFromTeammate(int amount)
    {
        AddGold(amount);
        
    }
    
    /// <summary>
    /// Checks if current game mode is 2v2
    /// </summary>
    public bool Is2v2Mode()
    {
        // You may need to adjust this based on how you determine game mode
        // For now, assuming 2v2 if there are 4 players
            
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
        
    }
    
    /// <summary>
    /// Get all token data for the token menu system
    /// </summary>
    public List<(GameObject tokenPrefab, SuperPower power)> GetAllTokenData()
    {
        List<(GameObject, SuperPower)> tokenData = new List<(GameObject, SuperPower)>();
        
        
        
        foreach (GameObject tokenPrefab in superPowerTokens)
        {
            if (tokenPrefab == null)
            {
                
                continue;
            }
            
            SuperPowerToken tokenScript = tokenPrefab.GetComponent<SuperPowerToken>();
            if (tokenScript == null)
            {
                
                continue;
            }
            
            SuperPower power = null;
            
            // Try to use the assigned power first
            if (tokenScript.power != null)
            {
                power = tokenScript.power;
                
            }
            // If no assigned power, try to create from className
            else if (!string.IsNullOrEmpty(tokenScript.superPowerClassName))
            {
                
                
                var type = System.Type.GetType(tokenScript.superPowerClassName);
                if (type != null && typeof(SuperPower).IsAssignableFrom(type))
                {
                    power = ScriptableObject.CreateInstance(type) as SuperPower;
                    if (power != null)
                    {
                        
                    }
                    else
                    {
                        
                    }
                }
                else
                {
                    
                }
            }
            else
            {
                
                continue;
            }
            
            if (power != null)
            {
                tokenData.Add((tokenPrefab, power));
                
            }
        }
        
        
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
        
    }
    
    /// <summary>
    /// Creates and animates a gold popup text at the specified location
    /// </summary>
    public void ShowGoldPopup(int goldAmount)
    {
        if (goldPopupLocation == null)
        {
            
            return;
        }
        
        StartCoroutine(ShowGoldPopupCoroutine(goldAmount));
    }
    
    /// <summary>
    /// Shows a two-stage gold popup: first the capturing card value, then the center cards bonus
    /// </summary>
    public void ShowTwoStageGoldPopup(int capturingCardValue, int centerCardCount)
    {
        if (goldPopupLocation == null)
        {
            
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
            Transform parentTransform = goldPopupLocation;
            
            if (parentTransform == null)
            {
                
                return null;
            }
            
            popupObject = Instantiate(goldPopupPrefab, parentTransform);
        }
        else
        {
            // Create popup object from scratch
            popupObject = new GameObject("GoldPopup");
            
            Transform parentTransform = goldPopupLocation;
            
            if (parentTransform == null)
            {
                
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
        
        // Position at the goldPopupLocation
        if (goldPopupLocation != null)
        {
            popupObject.transform.position = goldPopupLocation.position;
        }
        else
        {
            
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
            
            return;
        }
        
        // Check if we have a prefab for this power (should always be true since we found it in Keys)
        if (!superPowerPrefabs.ContainsKey(targetPower))
        {
            
            return;
        }
        
        // Use the proper spawning system like the kese does
        
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
        
    }
    
    /// <summary>
    /// Print current power list for debugging
    /// </summary>
    [ContextMenu("Print Available Powers")]
    public void PrintAvailablePowers()
    {
        List<SuperPower> uniquePowers = new List<SuperPower>(superPowerPrefabs.Keys);
        
        for (int i = 0; i < uniquePowers.Count; i++)
        {
            var power = uniquePowers[i];
            
        }
        
        
        foreach (var kvp in superPowerPrefabs)
        {
            
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
                
            }
            else
            {
                
                
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
                            
                            break;
                        }
                    }
                }
                
                if (!tokenExists)
                {
                    
                }
                
                // Test type resolution
                var type = System.Type.GetType(className);
                if (type == null)
                {
                    
                }
                else
                {
                    
                }
            }
        }
        
        
    }
    
    /// <summary>
    /// Debug token prefab setup
    /// </summary>
    [ContextMenu("Debug Token Prefabs")]
    public void DebugTokenPrefabs()
    {
        
        
        
        for (int i = 0; i < superPowerTokens.Count; i++)
        {
            var token = superPowerTokens[i];
            if (token == null)
            {
                
                continue;
            }
            
            var tokenScript = token.GetComponent<SuperPowerToken>();
            if (tokenScript == null)
            {
                
                continue;
            }
            
            string className = tokenScript.superPowerClassName;
            if (string.IsNullOrEmpty(className))
            {
                
                continue;
            }
            
            // Test type resolution
            var type = System.Type.GetType(className);
            if (type == null)
            {
                
            }
            else if (!typeof(SuperPower).IsAssignableFrom(type))
            {
                
            }
            else
            {
                
            }
        }
        
        
    }
    
    /// <summary>
    /// Force rebuild the power dictionary - useful when token prefabs are updated
    /// </summary>
    [ContextMenu("Rebuild Power Dictionary")]
    public void RebuildPowerDictionary()
    {
        
        DictionaryCreation();
        
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
        
        
        
        for (int i = 0; i < expectedPowerNames.Length; i++)
        {
            string powerName = expectedPowerNames[i];
            
            
            try
            {
                SpawnSpecificPower(powerName);
                
            }
            catch (System.Exception e)
            {
                
            }
            
            // Small delay between spawns
            if (i < expectedPowerNames.Length - 1)
            {
                System.Threading.Thread.Sleep(100);
            }
        }
        
        
    }
    
    /// <summary>
    /// Add testing gold
    /// </summary>
    [ContextMenu("Add 50 Gold")]
    public void AddTestingGold()
    {
        AddGold(50);
        
    }
    
    [ContextMenu("Test New Gold Calculation")]
    public void TestNewGoldCalculation()
    {
        
        
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
            
            
            
            
            
        }
    }
    
    [ContextMenu("Test Two-Stage Popup")]
    public void TestTwoStagePopup()
    {
        
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
            
        }
        else
        {
            
        }
    }
    
    /// <summary>
    /// Debug method to check current InfoBox and menu page states
    /// </summary>
    [ContextMenu("Debug InfoBox States")]
    public void DebugInfoBoxStates()
    {
        
        
        
        
        
        
        
        if (isInfoBoxOpen && !isMenuPageOpen)
        {
            
        }
        else if (isInfoBoxOpen && isMenuPageOpen)
        {
            
        }
        else if (!isInfoBoxOpen)
        {
            
        }
    }
    
    /// <summary>
    /// Test method to manually clear InfoBox content (for debugging)
    /// </summary>
    [ContextMenu("Test Clear InfoBox Content")]
    public void TestClearInfoBoxContent()
    {
        
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
            
            StartCoroutine(SpawnSuperPower(kapkacPower, centerPos, 150f));
        }
        else
        {
            
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
            
            
            StartCoroutine(SpawnSuperPower(randomPower, worldPos, 150f));
        }
        else
        {
            
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
            // Start the animation coroutine and wait for it
            Coroutine animCoroutine = StartCoroutine(anim);
            yield return animCoroutine;
            yield return null; // Wait a frame before next animation
        }
        isInfoBoxAnimationRunning = false;
    }
}