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
    [SerializeField] private List<GameObject> superPowerTokens = new List<GameObject>();
    private Dictionary<SuperPower, GameObject> superPowerPrefabs = new Dictionary<SuperPower, GameObject>();
    private List<SuperPower> superPowerList = new List<SuperPower>();
    [SerializeField] private int maxSuperPowers = 5;
    private int numberOfSuperPowersToSpawn = 3;
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
    [SerializeField] private int maxGold = 100; // Maximum gold cap
    
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
    private bool isInfoBoxOpen = false; // Track if info box is currently open
    private Coroutine currentAnimationCoroutine; // Track current animation to prevent overlaps

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
    }

    void Update()
    {
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

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Ended)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    return;

                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    HandleTokenRaycast(hit);
                }
            }
        }
    }

    private void HandleTokenRaycast(RaycastHit hit)
    {
        Debug.LogWarning("Object touched: " + hit.collider.gameObject.tag);
        if (hit.collider.gameObject.tag == "Token")
        {
            SuperPowerToken superPowerToken = hit.collider.GetComponent<SuperPowerToken>();
            if (superPowerToken != null)
            {
                StartCoroutine(OpenInfoBox(superPowerToken));
                return;
            }
        }
        if (SuperPowerToken.ActiveInstance != null && hit.collider.gameObject.tag == "Respawn")
        {
            StartCoroutine(CloseInfoBox());
        }
    }

    private void GetUIElements()
    {
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

        // Debug what we found
        Debug.Log($"[SuperPowerSpawner] UI Elements found:");
        Debug.Log($"  - backgroundPanel: {(backgroundPanel != null ? "✓" : "✗")}");
        Debug.Log($"  - infoBoxCanvas: {(infoBoxCanvas != null ? "✓" : "✗")}");
        Debug.Log($"  - nameText: {(nameText != null ? "✓" : "✗")}");
        Debug.Log($"  - descriptionText: {(descriptionText != null ? "✓" : "✗")}");
        Debug.Log($"  - activateButton: {(activateButton != null ? "✓" : "✗")}");
        Debug.Log($"  - falseActivateButton: {(falseActivateButton != null ? "✓" : "✗")}");
        Debug.Log($"  - closeButton: {(closeButton != null ? "✓" : "✗")}");

        if (nameText == null || descriptionText == null || activateButton == null || closeButton == null)
        {
            Debug.LogError("One or more UI elements not found in InfoBoxCanvas for " + gameObject.name);
            if (nameText == null) Debug.LogError("NameText not found at path: InfoBoxCanvas/NamePanel/NameText");
            if (descriptionText == null) Debug.LogError("DescriptionText not found at path: InfoBoxCanvas/DescriptionPanel/DescriptionText");
            if (activateButton == null) Debug.LogError("ActivateButton not found in InfoBoxCanvas or as standalone GameObject");
            if (closeButton == null) Debug.LogError("CloseButton not found in InfoBoxCanvas or as standalone GameObject");
            return;
        }

        activateButton.onClick.AddListener(OnTokenClicked);
        falseActivateButton.onClick.AddListener(GetActiveButtonErrorMessage);
        closeButton.onClick.AddListener(() =>
        {
            RemoveSpawnedSuperPower(SuperPowerToken.ActiveInstance.gameObject);
            UpdateTokenPositions();
            StartCoroutine(SuperPowerToken.ActiveInstance.FadeOutSprite());
            StartCoroutine(CloseInfoBox());
        });

        StartCoroutine(CloseInfoBoxImmediate()); // Ensure the info box is closed initially without animation
    }

    private void OnTokenClicked()
    {
        Debug.Log("Activate button clicked for " + SuperPowerToken.ActiveInstance?.power.name);
        SuperPowerToken.ActiveInstance.OnTokenClicked();
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
            UpdateInfoBoxContent(superPowerToken);

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

            // Fade out the open button since InfoBox is now open
            StartCoroutine(FadeOpenButtonOut());

            // Don't play page change animation for initial opening
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
            UpdateInfoBoxContent(superPowerToken);
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
        UpdateInfoBoxContent(superPowerToken);

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

        // Update content
        bool canActivate = CheckIfCardShouldBeSelected(superPowerToken.power.name);
        activateButton.gameObject.SetActive(canActivate);
        falseActivateButton.gameObject.SetActive(!canActivate);
        closeButton.gameObject.SetActive(true);

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

        Debug.Log($"Info updated for: {superPowerToken.power.name}");
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
        
        // Fade in the open button since InfoBox is now closed
        StartCoroutine(FadeOpenButtonIn());
        
        SuperPowerToken.ActiveInstance = null;
        isInfoBoxOpen = false;
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
    private List<string> restirictedPowersName_CardNeedToBeSelected = new List<string> { "Bu Daha İyi", "Şunu Değiş Tokuş", "Kopyala Yapıştır"};
    private List<string> restirictedPowersName_CenterNotEmpty = new List<string> { "Bu Daha İyi", "Bomba" };
    
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

    public void SetActiveActivateButtonTrue()
    {
        if (activateButton.gameObject.activeSelf) return;
        else
        {
            activateButton.gameObject.SetActive(true);
            ResetInfoBoxText();
        }
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
                for (int i = 0; i < powerInstance.rarityMultiplier; i++)
                    superPowerList.Add(powerInstance);
            }
            else
            {
                Debug.LogWarning($"Super power {className} already exists in the dictionary.");
            }
        }
    }

    [ContextMenu("Ready to Spawn Super Powers")]
    // Add new parameters to ReadyToSpawnSuperPowers and SpawnSuperPower
    public void ReadyToSpawnSuperPowers(int numberOfSuperPowersToSpawn = 2, Vector3? spawnOrigin = null, float spawnScale = 150f, int coinAmount = -1)
    {
        StartCoroutine(ReadyToSpawnSuperPower(numberOfSuperPowersToSpawn, spawnOrigin, spawnScale, coinAmount));
    }

    private IEnumerator ReadyToSpawnSuperPower(int numberOfSuperPowersToSpawn, Vector3? spawnOrigin, float spawnScale, int coinAmount)
    {
        yield return new WaitForSeconds(1f);
        for (int i = 0; i < numberOfSuperPowersToSpawn; i++)
        {
            StartCoroutine(SpawnSuperPower(GetWeightedRandomSuperPower(coinAmount), spawnOrigin, spawnScale));
            yield return new WaitForSeconds(0.5f);
        }
    }

    private SuperPower GetWeightedRandomSuperPower(int coinAmount)
    {
        if (superPowerPrefabs.Count == 0)
            return null;

        // If coinAmount is not set, fallback to uniform random
        if (coinAmount < 0)
            return GetRandomSuperPower();

        List<SuperPower> powers = new List<SuperPower>(superPowerPrefabs.Keys);
        List<float> weights = new List<float>();
        float totalWeight = 0f;

        Debug.Log($"[SuperPowerSpawner] Calculating weights for coinAmount={coinAmount}:");
        for (int i = 0; i < powers.Count; i++)
        {
            var power = powers[i];
            int rarity = power.rarityMultiplier;
            float weight = 1f / (1f + Mathf.Abs(rarity - coinAmount));
            weights.Add(weight);
            totalWeight += weight;
            Debug.Log($"  Power: {power.name}, Rarity: {rarity}, Weight: {weight:F4}");
        }

        // Print normalized probabilities
        Debug.Log("[SuperPowerSpawner] Normalized probabilities:");
        for (int i = 0; i < powers.Count; i++)
        {
            float prob = weights[i] / totalWeight;
            Debug.Log($"  {powers[i].name}: {prob:P2}");
        }

        float rand = Random.value * totalWeight;
        Debug.Log($"[SuperPowerSpawner] Random value: {rand:F4} (totalWeight={totalWeight:F4})");
        float cumulative = 0f;
        for (int i = 0; i < powers.Count; i++)
        {
            cumulative += weights[i];
            if (rand <= cumulative)
            {
                Debug.Log($"[SuperPowerSpawner] Selected: {powers[i].name}");
                return powers[i];
            }
        }
        Debug.LogWarning("[SuperPowerSpawner] Fallback: selected last power.");
        return powers[powers.Count - 1]; // fallback
    }

    private SuperPower GetRandomSuperPower()
    {
        if (superPowerList.Count == 0)
        {
            Debug.LogWarning("No super powers available to spawn.");
            return null;
        }
        int randomIndex = Random.Range(0, superPowerList.Count);
        Debug.Log($"[SuperPowerSpawner] Uniform random selection: index={randomIndex}, power={superPowerList[randomIndex].name}");
        return superPowerList[randomIndex];
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
                StartCoroutine(OpenInfoBox(SuperPowerToken.ActiveInstance));
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
        NetworkRelay.Instance.ReportZaferPuaniServerRPC(playerNo, totalPoints);
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
    /// Note: Gold is now added individually per card in the new system.
    /// For 2v2 mode, teammate sharing is handled by sending the appropriate amount to teammate.
    /// </summary>
    public void OnLocalCapture(string playedCard)
    {
        // In 2v2 mode, we need to share gold with teammate
        if (Is2v2Mode())
        {
            int captureValue = CalculateCenterCardsValue();
            captureValue += CardInteraction.cardLookup[playedCard].GetCardID()[1];
            
            // Share gold with teammate (50/50 split)
            int sharedGold = captureValue / 2;
            
            // Send gold share to teammate via network
            int teammateNumber = GetTeammateNumber();
            GameManager.LocalInstance.networkRelay.ShareGoldWithTeammateServerRPC(teammateNumber, sharedGold);
            
            Debug.Log($"[SuperPowerSpawner] Local capture in 2v2! Total value: {captureValue}, Shared with teammate: {sharedGold}");
        }
        else
        {
            Debug.Log($"[SuperPowerSpawner] Local capture in 1v1! Gold will be added individually per card.");
        }
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
}