using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SuperPowerSpawner : MonoBehaviour
{
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
    private Text nameText;
    private Text descriptionText;
    private Button activateButton;
    private Button falseActivateButton;
    private Button closeButton;

    [Header("InfoBox Animation Settings")]
    [SerializeField] private Transform infoBoxStartLocation; // Starting position for the info box
    [SerializeField] private float animationDuration = 0.5f; // Duration of the movement animation
    [SerializeField] private float infoChangeDelay = 0.25f; // Delay before info changes (to match page change animation transition)
    [SerializeField] private float fadeDuration = 0.15f; // Duration of fade in/out animations

    private Vector3 infoBoxOriginalPosition; // Original position stored at Start
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
        // Store the original position of the info box
        if (backgroundPanel != null)
        {
            infoBoxOriginalPosition = backgroundPanel.transform.position;
        }
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
        nameText = backgroundPanel.transform.Find("NamePanel/NameText")?.GetComponent<Text>();
        descriptionText = backgroundPanel.transform.Find("DescriptionPanel/DescriptionText")?.GetComponent<Text>();
        activateButton = GameObject.Find("ActivateButton")?.GetComponent<Button>();
        falseActivateButton = GameObject.Find("FalseActivateButton")?.GetComponent<Button>();
        closeButton = GameObject.Find("CloseButton")?.GetComponent<Button>();

        if (nameText == null || descriptionText == null || activateButton == null || closeButton == null)
        {
            Debug.LogError("One or more UI elements not found in InfoBoxCanvas for " + gameObject.name);
            if (nameText == null) Debug.LogError("NameText not found");
            if (descriptionText == null) Debug.LogError("DescriptionText not found");
            if (activateButton == null) Debug.LogError("ActivateButton not found");
            if (closeButton == null) Debug.LogError("CloseButton not found");
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
    /// Case 1: InfoBox was closed and gets opened (moves from start to original position)
    /// Case 2: InfoBox is already open (no movement, just update content)
    /// </summary>
    public IEnumerator OpenInfoBox(SuperPowerToken superPowerToken)
    {
        Debug.Log("Opening InfoBox for " + superPowerToken.power.name);

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

        // FIXED: Check if we were switching tokens (Case 2) or opening fresh (Case 1)
        if (isSwitchingTokens && wasBoxOpenBeforeSwitch)
        {
            // Case 2: InfoBox was already open, just switching tokens - no movement animation
            Debug.Log("Case 2: Switching between tokens - no movement, with info delay and fade");
            backgroundPanel.SetActive(true);
            backgroundPanel.transform.position = infoBoxOriginalPosition; // Ensure it's at the right position
            isInfoBoxOpen = true; // Restore the open state

            // NEW: Play page change animation first
            UIFrameAnimator frameAnimator = backgroundPanel.GetComponent<UIFrameAnimator>();
            if (frameAnimator != null)
            {
                frameAnimator.PlayPageChangeAnimation();
            }

            // NEW: Fade out current info, wait for delay, then fade in new info
            yield return StartCoroutine(FadeInfoWithDelay(superPowerToken));
        }
        else if (!isInfoBoxOpen)
        {
            // Case 1: InfoBox was truly closed - animate from start position to original position
            Debug.Log("Case 1: InfoBox was closed - animating to open");
            backgroundPanel.SetActive(true);

            // Set to start position
            if (infoBoxStartLocation != null)
            {
                backgroundPanel.transform.position = infoBoxStartLocation.position;
            }

            // Update content immediately for Case 1 (no delay needed for initial opening)
            UpdateInfoBoxContent(superPowerToken);

            // Animate to original position
            currentAnimationCoroutine = StartCoroutine(AnimateInfoBoxPosition(infoBoxOriginalPosition));
            yield return currentAnimationCoroutine;
            currentAnimationCoroutine = null;

            isInfoBoxOpen = true;

            // Don't play page change animation for initial opening
            Debug.Log("Case 1: No page change animation - initial opening");
        }
        else
        {
            // Fallback: ensure it's active and positioned correctly
            backgroundPanel.SetActive(true);
            backgroundPanel.transform.position = infoBoxOriginalPosition;

            // NEW: Play page change animation for fallback case too
            UIFrameAnimator frameAnimator = backgroundPanel.GetComponent<UIFrameAnimator>();
            if (frameAnimator != null)
            {
                frameAnimator.PlayPageChangeAnimation();
            }

            // NEW: Fade out current info, wait for delay, then fade in new info
            yield return StartCoroutine(FadeInfoWithDelay(superPowerToken));
        }
    }

    /// <summary>
    /// Handles the fade out, delay, and fade in sequence for info changes
    /// UPDATED: Only affects child elements, not the parent backgroundPanel
    /// </summary>
    private IEnumerator FadeInfoWithDelay(SuperPowerToken superPowerToken)
    {
        // Get only the child UI elements that need to fade (not the parent backgroundPanel)
        List<CanvasGroup> fadeGroups = new List<CanvasGroup>();
        
        // Add specific child elements to fade
        if (nameText != null)
            fadeGroups.Add(GetOrAddCanvasGroup(nameText.gameObject));
        
        if (descriptionText != null)
            fadeGroups.Add(GetOrAddCanvasGroup(descriptionText.gameObject));
        
        if (activateButton != null && activateButton.transform.parent != backgroundPanel.transform)
            fadeGroups.Add(GetOrAddCanvasGroup(activateButton.transform.parent.gameObject));
        
        // Alternative: Find all direct children of backgroundPanel and fade them
        // This ensures we fade child content panels but not the backgroundPanel itself
        foreach (Transform child in backgroundPanel.transform)
        {
            // Skip if it's one of the specific elements we already added
            bool alreadyAdded = false;
            foreach (var group in fadeGroups)
            {
                if (group.gameObject == child.gameObject)
                {
                    alreadyAdded = true;
                    break;
                }
            }
            
            if (!alreadyAdded)
            {
                // Add canvas group to direct children (like panels containing the UI elements)
                CanvasGroup childGroup = GetOrAddCanvasGroup(child.gameObject);
                if (childGroup != null)
                    fadeGroups.Add(childGroup);
            }
        }

        // Phase 1: Fade out current info (only child elements)
        yield return StartCoroutine(FadeCanvasGroups(fadeGroups.ToArray(), 0f, fadeDuration));

        // Phase 2: Wait for the remaining delay (accounting for fade out time)
        float remainingDelay = Mathf.Max(0f, infoChangeDelay - fadeDuration);
        if (remainingDelay > 0f)
        {
            yield return new WaitForSeconds(remainingDelay);
        }

        // Phase 3: Update content while faded out
        UpdateInfoBoxContent(superPowerToken);

        // Phase 4: Fade in new info (only child elements)
        yield return StartCoroutine(FadeCanvasGroups(fadeGroups.ToArray(), 1f, fadeDuration));
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
    /// Fades multiple CanvasGroups to a target alpha value
    /// </summary>
    private IEnumerator FadeCanvasGroups(CanvasGroup[] canvasGroups, float targetAlpha, float duration)
    {
        if (canvasGroups == null || canvasGroups.Length == 0) yield break;
        
        float[] startAlphas = new float[canvasGroups.Length];

        // Store starting alpha values
        for (int i = 0; i < canvasGroups.Length; i++)
        {
            if (canvasGroups[i] != null)
                startAlphas[i] = canvasGroups[i].alpha;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);

            // Apply smooth easing
            progress = Mathf.SmoothStep(0f, 1f, progress);

            // Update all canvas groups
            for (int i = 0; i < canvasGroups.Length; i++)
            {
                if (canvasGroups[i] != null)
                    canvasGroups[i].alpha = Mathf.Lerp(startAlphas[i], targetAlpha, progress);
            }

            yield return null;
        }

        // Ensure final alpha values are exact
        for (int i = 0; i < canvasGroups.Length; i++)
        {
            if (canvasGroups[i] != null)
                canvasGroups[i].alpha = targetAlpha;
        }
    }

    /// <summary>
    /// Separate method to update info box content - extracted for cleaner code
    /// </summary>
    private void UpdateInfoBoxContent(SuperPowerToken superPowerToken)
    {
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
    /// UPDATED: Only affects child elements, not the parent backgroundPanel
    /// </summary>
    private void EnsureUIElementsVisible()
    {
        // Only reset alpha for child elements, not the backgroundPanel itself
        foreach (Transform child in backgroundPanel.transform)
        {
            CanvasGroup childGroup = child.gameObject.GetComponent<CanvasGroup>();
            if (childGroup != null)
            {
                childGroup.alpha = 1f;
            }
        }

        // Also ensure specific UI elements are visible
        if (nameText != null)
        {
            CanvasGroup nameCanvasGroup = nameText.gameObject.GetComponent<CanvasGroup>();
            if (nameCanvasGroup != null) nameCanvasGroup.alpha = 1f;
        }
        
        if (descriptionText != null)
        {
            CanvasGroup descCanvasGroup = descriptionText.gameObject.GetComponent<CanvasGroup>();
            if (descCanvasGroup != null) descCanvasGroup.alpha = 1f;
        }
        
        if (activateButton != null && activateButton.transform.parent != backgroundPanel.transform)
        {
            CanvasGroup buttonCanvasGroup = activateButton.transform.parent.gameObject.GetComponent<CanvasGroup>();
            if (buttonCanvasGroup != null) buttonCanvasGroup.alpha = 1f;
        }
    }

    /// <summary>
    /// Case 3: InfoBox is open and gets closed (moves from original position to start position)
    /// </summary>
    public IEnumerator CloseInfoBox()
    {
        Debug.Log("Closing InfoBox for " + SuperPowerToken.ActiveInstance?.power.name);

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

        // Case 3: Animate from original position to start position
        if (infoBoxStartLocation != null)
        {
            currentAnimationCoroutine = StartCoroutine(AnimateInfoBoxPosition(infoBoxStartLocation.position));
            yield return currentAnimationCoroutine;
            currentAnimationCoroutine = null;
        }

        // Hide and reset
        backgroundPanel.SetActive(false);
        activateButton.gameObject.SetActive(false);
        falseActivateButton.gameObject.SetActive(false);
        closeButton.gameObject.SetActive(false);
        SuperPowerToken.ActiveInstance = null;
        isInfoBoxOpen = false;
    }

    /// <summary>
    /// Immediately close the info box without animation (used for initialization or quick switches)
    /// </summary>
    private IEnumerator CloseInfoBoxImmediate()
    {
        // Ensure UI elements are visible before closing
        EnsureUIElementsVisible();

        backgroundPanel.SetActive(false);
        activateButton.gameObject.SetActive(false);
        falseActivateButton.gameObject.SetActive(false);
        closeButton.gameObject.SetActive(false);
        SuperPowerToken.ActiveInstance = null;
        isInfoBoxOpen = false;
        yield return null;
    }

    /// <summary>
    /// Animates the info box from its current position to the target position
    /// </summary>
    private IEnumerator AnimateInfoBoxPosition(Vector3 targetPosition)
    {
        Vector3 startPosition = backgroundPanel.transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            
            // Use smooth easing (you can change this to other easing functions)
            progress = Mathf.SmoothStep(0f, 1f, progress);
            
            backgroundPanel.transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
            yield return null;
        }

        // Ensure final position is exact
        backgroundPanel.transform.position = targetPosition;
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
        if (!backgroundPanel.activeSelf) return;
        else
        {
            StartCoroutine(OpenInfoBox(SuperPowerToken.ActiveInstance));
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
}