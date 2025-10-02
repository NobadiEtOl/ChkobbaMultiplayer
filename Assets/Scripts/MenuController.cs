using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

[System.Serializable]
public class TokenMenuData
{
    public Sprite tokenSprite;
    public string tokenName;
    public int rarity;
    public SuperPower superPower;
    public GameObject tokenPrefab;
    
    public TokenMenuData(Sprite sprite, string name, int rarity, SuperPower power, GameObject prefab)
    {
        this.tokenSprite = sprite;
        this.tokenName = name;
        this.rarity = rarity;
        this.superPower = power;
        this.tokenPrefab = prefab;
    }
}

public class MenuController : MonoBehaviour
{
    [Header("Menu Visual Settings")]
    [SerializeField] private Vector3 tokenScale = new Vector3(0.03f, 0.03f, 1f);
    [SerializeField] private float tokenSpacing = 2f;
    [SerializeField] private float rowHeight = 3f;
    [SerializeField] private int tokensPerRow = 1;
    [SerializeField] private Vector3 menuStartPosition = new Vector3(0f, 0f, -5f);
    [SerializeField] private float menuWidth = 12f;
    [SerializeField] private float menuHeight = 15f;
    
    [Header("Row Layout Settings")]
    [SerializeField] private float leftPadding = 1f; // Distance from left edge
    [SerializeField] private float tokenToNameSpacing = 2f; // Space between token and name
    [SerializeField] private float nameToRaritySpacing = 3f; // Space between name and rarity
    
    [Header("Text Appearance Settings")]
    [SerializeField] private float nameTextFontSize = 16f;
    [SerializeField] private float rarityTextFontSize = 16f;
    [SerializeField] private Vector2 nameTextSize = new Vector2(300f, 80f);
    [SerializeField] private Vector2 rarityTextSize = new Vector2(100f, 80f);
    [SerializeField] private Color nameTextColor = Color.white;
    [SerializeField] private Color rarityTextColor = Color.yellow;
    [SerializeField] private TextAlignmentOptions nameTextAlignment = TextAlignmentOptions.Left;
    [SerializeField] private TextAlignmentOptions rarityTextAlignment = TextAlignmentOptions.Center;
    [SerializeField] private float nameTextMinSize = 5f;
    [SerializeField] private float nameTextMaxSize = 8f;
    
    [Header("Scene References")]
    [SerializeField] private GameObject tokenDisplayArea;
    [SerializeField] private GameObject displayBackgroundPanel;
    [SerializeField] private GameObject scrollContainer;
    [SerializeField] private GameObject openButtonGameObject;
    
    // Public property to access openButtonGameObject
    public GameObject OpenButtonGameObject => openButtonGameObject;

    [Header("Scroll Settings")]
    [SerializeField] private float scrollSensitivity = 2f;
    [SerializeField] private float scrollDeceleration = 0.95f;
    [SerializeField] private float maxScrollDistance = 10f;
    [SerializeField] private float bounceBackForce = 5f;
    
    // Private variables
    private List<TokenMenuData> tokenDataList = new List<TokenMenuData>();
    private List<GameObject> instantiatedTokens = new List<GameObject>();
    private bool isMenuActive = false;
    private bool menuInitialized = false;
    
    // Scroll variables
    private bool isDragging = false;
    private Vector3 lastMousePosition;
    private float scrollVelocity = 0f;
    private float currentScrollOffset = 0f;
    private Camera mainCamera;
    
    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null) mainCamera = FindObjectOfType<Camera>();
        
        Debug.Log("[MenuController] Start called - checking SuperPowerSpawner.LocalInstance");
        
        if (SuperPowerSpawner.LocalInstance == null)
        {
            Debug.LogWarning("[MenuController] SuperPowerSpawner.LocalInstance is null at Start, will try to initialize later");
            StartCoroutine(WaitForSuperPowerSpawnerAndInitialize());
        }
        else
        {
            Debug.Log("[MenuController] SuperPowerSpawner.LocalInstance found, initializing immediately");
            InitializeMenu();
        }
        
        // Setup open button click handler
        SetupOpenButtonClickHandler();
    }
    
    private IEnumerator WaitForSuperPowerSpawnerAndInitialize()
    {
        Debug.Log("[MenuController] Waiting for SuperPowerSpawner.LocalInstance...");
        
        while (SuperPowerSpawner.LocalInstance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        Debug.Log("[MenuController] SuperPowerSpawner.LocalInstance found, initializing menu");
        InitializeMenu();
    }
    
    private void InitializeMenu()
    {
        // Validate scene references
        if (!ValidateSceneReferences())
        {
            Debug.LogError("[MenuController] Failed to initialize menu - missing scene references!");
            return;
        }
        
        LoadTokenData();
        CreateTokensInSceneContainer();
        SetMenuActive(false);
        Debug.Log("[MenuController] Scene-based menu initialized successfully");
    }
    
    private bool ValidateSceneReferences()
    {
        bool valid = true;
        
        if (tokenDisplayArea == null)
        {
            Debug.LogError("[MenuController] TokenDisplayArea is not assigned!");
            valid = false;
        }
        
        if (displayBackgroundPanel == null)
        {
            Debug.LogError("[MenuController] DisplayBackgroundPanel is not assigned!");
            valid = false;
        }
        
        if (scrollContainer == null)
        {
            Debug.LogError("[MenuController] ScrollContainer is not assigned!");
            valid = false;
        }
        
        if (valid)
        {
            // Add colliders if missing (needed for scroll detection)
            EnsureScrollColliders();
            Debug.Log("[MenuController] All scene references validated successfully");
        }
        
        return valid;
    }
    
    private void EnsureScrollColliders()
    {
        // Add collider to TokenDisplayArea for scroll detection if missing
        if (tokenDisplayArea != null)
        {
            Collider displayCollider = tokenDisplayArea.GetComponent<Collider>();
            if (displayCollider == null)
            {
                BoxCollider boxCollider = tokenDisplayArea.AddComponent<BoxCollider>();
                // Set size based on display area - you may need to adjust these values
                boxCollider.size = new Vector3(menuWidth, menuHeight, 1f);
                boxCollider.isTrigger = true; // For better scroll detection
                Debug.Log("[MenuController] Added BoxCollider to TokenDisplayArea for scroll detection");
            }
        }
        
        // Add collider to DisplayBackgroundPanel for additional scroll area if missing
        if (displayBackgroundPanel != null)
        {
            Collider bgCollider = displayBackgroundPanel.GetComponent<Collider>();
            if (bgCollider == null)
            {
                // Get the sprite renderer to match collider size to sprite
                SpriteRenderer spriteRenderer = displayBackgroundPanel.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    BoxCollider boxCollider = displayBackgroundPanel.AddComponent<BoxCollider>();
                    // Match collider size to sprite bounds
                    Vector3 spriteSize = spriteRenderer.sprite.bounds.size;
                    boxCollider.size = new Vector3(spriteSize.x, spriteSize.y, 0.1f);
                    boxCollider.isTrigger = true;
                    Debug.Log("[MenuController] Added BoxCollider to DisplayBackgroundPanel for scroll detection");
                }
            }
        }
    }
    
    void Update()
    {
        HandleScrollInput();
        ApplyScrollPhysics();
    }
    
    [ContextMenu("Test Token Menu")]
    public void ShowTokenMenu()
    {
        Debug.Log("[MenuController] ShowTokenMenu called - toggle functionality");
        SuperPowerSpawner.LocalInstance.SetNameText("Menu");
        
        if (SuperPowerSpawner.LocalInstance == null)
        {
            Debug.LogError("[MenuController] SuperPowerSpawner.LocalInstance is null, cannot toggle InfoBox");
            return;
        }
        
        // Check if InfoBox is currently open (either menu or power information)
        if (SuperPowerSpawner.LocalInstance.isInfoBoxOpen)
        {
            Debug.Log("[MenuController] InfoBox is open, closing it");
            StartCoroutine(SuperPowerSpawner.LocalInstance.CloseInfoBox());
        }
        else
        {
            Debug.Log("[MenuController] InfoBox is closed, opening menu");
            
            if (!menuInitialized)
            {
                Debug.LogWarning("[MenuController] Menu not initialized yet, trying to initialize now...");
                InitializeMenu();
            }
            
            // Open InfoBox with menu page
            StartCoroutine(OpenMenuInInfoBox());
        }
    }
    
    
    [ContextMenu("Force Initialize Menu")]
    public void ForceInitializeMenu()
    {
        Debug.Log("[MenuController] Force initializing menu...");
        if (SuperPowerSpawner.LocalInstance == null)
        {
            Debug.LogError("[MenuController] Cannot force initialize - SuperPowerSpawner.LocalInstance is null!");
            return;
        }
        
        InitializeMenu();
        Debug.Log("[MenuController] Force initialization complete");
    }
    
    [ContextMenu("Test Toggle InfoBox")]
    public void TestToggleInfoBox()
    {
        Debug.Log("[MenuController] Testing ShowTokenMenu toggle functionality via context menu");
        ShowTokenMenu();
    }
    
    /// <summary>
    /// Check if the menu page is currently open in the InfoBox
    /// </summary>
    public bool IsMenuPageOpen()
    {
        if (SuperPowerSpawner.LocalInstance != null)
        {
            return SuperPowerSpawner.LocalInstance.isMenuPageOpen;
        }
        return false;
    }
    
    /// <summary>
    /// Check if the InfoBox is open (regardless of which page is shown)
    /// </summary>
    public bool IsInfoBoxOpen()
    {
        if (SuperPowerSpawner.LocalInstance != null)
        {
            return SuperPowerSpawner.LocalInstance.isInfoBoxOpen;
        }
        return false;
    }
    
    [ContextMenu("Debug Hierarchy State")]
    public void DebugHierarchyState()
    {
        Debug.Log("=== HIERARCHY DEBUG START ===");
        Debug.Log($"MenuController GameObject: {this.gameObject.name} (active: {this.gameObject.activeInHierarchy})");
        Debug.Log($"MenuController position: {this.transform.position}");
        Debug.Log($"MenuInitialized: {menuInitialized}");
        Debug.Log($"TokenDataList count: {tokenDataList.Count}");
        Debug.Log($"InstantiatedTokens count: {instantiatedTokens.Count}");
        
        if (tokenDisplayArea != null)
        {
            Debug.Log($"TokenDisplayArea: {tokenDisplayArea.name} (active: {tokenDisplayArea.activeInHierarchy})");
            Debug.Log($"TokenDisplayArea position: {tokenDisplayArea.transform.position}");
            Debug.Log($"TokenDisplayArea children: {tokenDisplayArea.transform.childCount}");
        }
        else
        {
            Debug.Log("TokenDisplayArea: NULL");
        }
        
        if (scrollContainer != null)
        {
            Debug.Log($"ScrollContainer: {scrollContainer.name} (active: {scrollContainer.activeInHierarchy})");
            Debug.Log($"ScrollContainer position: {scrollContainer.transform.position}");
            Debug.Log($"ScrollContainer children: {scrollContainer.transform.childCount}");
            
            for (int i = 0; i < scrollContainer.transform.childCount; i++)
            {
                Transform child = scrollContainer.transform.GetChild(i);
                Debug.Log($"  Child {i}: {child.name} (active: {child.gameObject.activeInHierarchy}) at {child.position}");
            }
        }
        else
        {
            Debug.Log("ScrollContainer: NULL");
        }
        
        Debug.Log("=== HIERARCHY DEBUG END ===");
    }
    
    private void CreateTokensInSceneContainer()
    {
        Debug.Log("[MenuController] Creating tokens in scene-based container");
        
        if (scrollContainer == null)
        {
            Debug.LogError("[MenuController] ScrollContainer is null! Cannot create tokens.");
            return;
        }
        
        // Clear any existing tokens first
        ClearAllTokens();
        
        // Setup masking system before creating tokens
        SetupMaskingSystem();
        
        // Create all tokens in the scene-provided scroll container
        CreateAllTokens();
        
        menuInitialized = true;
        Debug.Log("[MenuController] ✅ Scene-based menu created successfully");
        
        // Final hierarchy check
        Debug.Log($"[MenuController] Final hierarchy check:");
        Debug.Log($"  - MenuController: {this.name}");
        Debug.Log($"  - TokenDisplayArea: {tokenDisplayArea?.name} (active: {tokenDisplayArea?.activeInHierarchy})");
        Debug.Log($"  - DisplayBackgroundPanel: {displayBackgroundPanel?.name} (active: {displayBackgroundPanel?.activeInHierarchy})");
        Debug.Log($"  - ScrollContainer: {scrollContainer?.name} (active: {scrollContainer?.activeInHierarchy})");
        Debug.Log($"  - ScrollContainer children count: {scrollContainer?.transform.childCount}");
    }
    
    private void LoadTokenData()
    {
        tokenDataList.Clear();
        
        Debug.Log("[MenuController] LoadTokenData called");
        
        if (SuperPowerSpawner.LocalInstance == null) 
        {
            Debug.LogError("[MenuController] SuperPowerSpawner.LocalInstance is null in LoadTokenData!");
            return;
        }
        
        Debug.Log("[MenuController] SuperPowerSpawner.LocalInstance found, calling GetAllTokenData()");
        var allTokenData = SuperPowerSpawner.LocalInstance.GetAllTokenData();
        Debug.Log($"[MenuController] Got {allTokenData.Count} tokens from SuperPowerSpawner");
        
        if (allTokenData.Count == 0)
        {
            Debug.LogWarning("[MenuController] No tokens returned from SuperPowerSpawner!");
            return;
        }
        
        foreach (var (tokenPrefab, power) in allTokenData)
        {
            if (tokenPrefab == null)
            {
                Debug.LogWarning($"[MenuController] Token prefab is null for power: {power?.name}");
                continue;
            }
            
            if (power == null)
            {
                Debug.LogWarning("[MenuController] Power is null for a token");
                continue;
            }
            
            SpriteRenderer spriteRenderer = tokenPrefab.GetComponent<SpriteRenderer>();
            Image imageComponent = tokenPrefab.GetComponent<Image>();
            
            Sprite tokenSprite = null;
            if (spriteRenderer != null) tokenSprite = spriteRenderer.sprite;
            else if (imageComponent != null) tokenSprite = imageComponent.sprite;
            
            TokenMenuData tokenData = new TokenMenuData(tokenSprite, power.name, power.rarityMultiplier, power, tokenPrefab);
            tokenDataList.Add(tokenData);
            
            Debug.Log($"[MenuController] Added token: {power.name} (rarity: {power.rarityMultiplier})");
        }
        
        Debug.Log($"[MenuController] Total tokens loaded: {tokenDataList.Count}");
    }
    
    private void CreateAllTokens()
    {
        // Clear any existing tokens
        ClearAllTokens();
        
        if (tokenDataList.Count == 0)
        {
            Debug.LogWarning("[MenuController] No tokens to create!");
            return;
        }
        
        Debug.Log($"[MenuController] Creating {tokenDataList.Count} token GameObjects");
        
        for (int i = 0; i < tokenDataList.Count; i++)
        {
            CreateToken3D(tokenDataList[i], i);
        }
        
        Debug.Log($"[MenuController] Created {instantiatedTokens.Count} token GameObjects");
    }
    
    private void CreateToken3D(TokenMenuData tokenData, int index)
    {
        Debug.Log($"[MenuController] CreateToken3D called for index {index}, token: {tokenData.tokenName}");
        
        try
        {
            // Check if tokenData is valid
            if (tokenData == null)
            {
                Debug.LogError($"[MenuController] TokenData is null for index {index}!");
                return;
            }
            
            if (tokenData.tokenPrefab == null)
            {
                Debug.LogError($"[MenuController] TokenPrefab is null for {tokenData.tokenName} at index {index}!");
                return;
            }
            
            if (scrollContainer == null)
            {
                Debug.LogError($"[MenuController] ScrollContainer is null when trying to create token {tokenData.tokenName}!");
                return;
            }
            
            Debug.Log($"[MenuController] About to instantiate token prefab: {tokenData.tokenPrefab.name}");
            
            // Instantiate the actual token prefab
            GameObject tokenInstance = Instantiate(tokenData.tokenPrefab, scrollContainer.transform);
            
            if (tokenInstance == null)
            {
                Debug.LogError($"[MenuController] Failed to instantiate token {tokenData.tokenName}!");
                return;
            }
            
            Debug.Log($"[MenuController] Successfully instantiated {tokenInstance.name}");
            
            // CRITICAL: Assign the power to the token script to prevent "Power is not assigned" error
            SuperPowerToken tokenScript = tokenInstance.GetComponent<SuperPowerToken>();
            if (tokenScript != null && tokenData.superPower != null)
            {
                tokenScript.power = tokenData.superPower;
                Debug.Log($"[MenuController] Assigned power {tokenData.superPower.name} to token script");
            }
            else
            {
                Debug.LogWarning($"[MenuController] Could not assign power to token script - tokenScript: {tokenScript != null}, power: {tokenData.superPower != null}");
            }
            
            tokenInstance.name = $"Token_{tokenData.tokenName}_{index}";
            Debug.Log($"[MenuController] Renamed to: {tokenInstance.name}");
            
            // Scale the token
            tokenInstance.transform.localScale = tokenScale;
            
            // Align rotation with the display background (or identity if not assigned)
            if (displayBackgroundPanel != null)
            {
                tokenInstance.transform.localRotation = displayBackgroundPanel.transform.localRotation;
            }
            else
            {
                tokenInstance.transform.localRotation = Quaternion.identity;
            }
            Debug.Log($"[MenuController] Set scale to: {tokenScale} and rotation to match display background");
            
            // Position the token using background panel bounds
            Vector3 tokenPosition = CalculateTokenPosition(index);
            tokenInstance.transform.localPosition = tokenPosition;
            Debug.Log($"[MenuController] Set position to: {tokenPosition} (world: {tokenInstance.transform.position})");
            
            // Re-apply scale and rotation next frame to guard against external changes
            StartCoroutine(EnforceTransformNextFrame(tokenInstance));
            
            // Remove smoke effect child if it exists
            Transform smokeEffect = tokenInstance.transform.Find("SmokeEffectGameObject");
            if (smokeEffect != null)
            {
                DestroyImmediate(smokeEffect.gameObject);
                Debug.Log($"[MenuController] Removed SmokeEffectGameObject from {tokenData.tokenName}");
            }
            
            // FORCE THE TOKEN TO BE ACTIVE
            tokenInstance.SetActive(true);
            Debug.Log($"[MenuController] Forced token active state: {tokenInstance.activeInHierarchy}");
            
            // Configure SpriteRenderer for masking
            SpriteRenderer tokenRenderer = tokenInstance.GetComponent<SpriteRenderer>();
            if (tokenRenderer != null)
            {
                tokenRenderer.sortingOrder = 10; // High sorting order to appear on top
                tokenRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask; // Enable masking
                Debug.Log($"[MenuController] Set SpriteRenderer sorting order to 10 and enabled masking");
            }
            else
            {
                Debug.LogWarning($"[MenuController] No SpriteRenderer found on token {tokenData.tokenName}");
            }
            
            // Create text elements as children of the token
            CreateTokenText(tokenData, tokenInstance, index);
            
            Debug.Log($"[MenuController] Created text for: {tokenData.tokenName}");
            
            // Check for existing colliders (tokens should already have BoxCollider3D)
            Collider existingCollider3D = tokenInstance.GetComponent<Collider>();
            Collider2D existingCollider2D = tokenInstance.GetComponent<Collider2D>();
            
            if (existingCollider3D != null)
            {
                Debug.Log($"[MenuController] Found existing 3D collider: {existingCollider3D.GetType().Name}");
            }
            else if (existingCollider2D != null)
            {
                Debug.Log($"[MenuController] Found existing 2D collider: {existingCollider2D.GetType().Name}");
            }
            else
            {
                // Only add if no collider exists
                BoxCollider collider = tokenInstance.AddComponent<BoxCollider>();
                collider.size = Vector3.one;
                Debug.Log($"[MenuController] Added BoxCollider3D (no existing collider found)");
            }
            
            // Add click handler component
            TokenClickHandler clickHandler = tokenInstance.GetComponent<TokenClickHandler>();
            if (clickHandler == null)
            {
                clickHandler = tokenInstance.AddComponent<TokenClickHandler>();
            }
            clickHandler.Initialize(tokenData, this, tokenInstance); // Pass the instantiated token
            Debug.Log($"[MenuController] Added TokenClickHandler");
            
            instantiatedTokens.Add(tokenInstance);
            
            Debug.Log($"[MenuController] ✅ Successfully created token GameObject: {tokenData.tokenName} at position {tokenInstance.transform.position}");
            Debug.Log($"[MenuController] Token active in hierarchy: {tokenInstance.activeInHierarchy}");
            Debug.Log($"[MenuController] Token parent: {tokenInstance.transform.parent?.name}");
            Debug.Log($"[MenuController] Current instantiated tokens count: {instantiatedTokens.Count}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MenuController] Exception while creating token {tokenData?.tokenName}: {e.Message}\n{e.StackTrace}");
        }
    }
    
    public void SetMenuActive(bool active)
    {
        Debug.Log($"[MenuController] Setting menu active: {active}");
        if (tokenDisplayArea != null)
        {
            tokenDisplayArea.SetActive(active);
            Debug.Log($"[MenuController] Set TokenDisplayArea active: {active}");
        }
        else
        {
            Debug.LogError("[MenuController] TokenDisplayArea is null, cannot set active state!");
        }
        
        isMenuActive = active;
        
        // Reset scroll position when activating
        if (active)
        {
            currentScrollOffset = 0f;
            if (scrollContainer != null)
            {
                Vector3 resetPosition = scrollContainer.transform.localPosition;
                resetPosition.y = 0f;
                scrollContainer.transform.localPosition = resetPosition;
            }
        }
        else
        {
            currentScrollOffset = 0f;
            // Clean up menu token when closing
            CleanupMenuToken();
        }
    }
    
    /// <summary>
    /// Called by SuperPowerSpawner when a different token page is opened
    /// This ensures the menu is properly hidden when switching to other pages
    /// </summary>
    public void OnOtherPageOpened()
    {
        Debug.Log("[MenuController] Other page opened, deactivating menu");
        SetMenuActive(false);
    }
    
    private void CleanupMenuToken()
    {
        // Find and destroy the menu token if it exists
        GameObject menuToken = GameObject.Find("MenuPageToken");
        if (menuToken != null)
        {
            // Clear active instance if it's our menu token
            if (SuperPowerToken.ActiveInstance != null && SuperPowerToken.ActiveInstance.gameObject == menuToken)
            {
                SuperPowerToken.ActiveInstance = null;
            }
            Destroy(menuToken);
            Debug.Log("[MenuController] Cleaned up menu token");
        }
    }
    
    private void ClearAllTokens()
    {
        foreach (GameObject token in instantiatedTokens)
        {
            if (token != null) DestroyImmediate(token);
        }
        instantiatedTokens.Clear();
    }
    
    [ContextMenu("Close Token Menu")]
    public void CloseTokenMenu()
    {
        SetMenuActive(false);
        Debug.Log("[MenuController] Token menu closed");
    }
    
    // 3D Scroll handling methods
    private void HandleScrollInput()
    {
        if (!isMenuActive) return;
        
        // Mouse scroll wheel (invert direction to feel natural)
        if (Input.mouseScrollDelta.y != 0)
        {
            float scrollAmount = -Input.mouseScrollDelta.y; // Inverted (sensitivity applied in ScrollContent3D)
            ScrollContent3D(scrollAmount);
        }
        
        // Touch/mouse drag
        if (Input.GetMouseButtonDown(0))
        {
            OnBeginDrag3D();
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            OnDrag3D();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            OnEndDrag3D();
        }
    }
    
    private void OnBeginDrag3D()
    {
        if (!isMenuActive) return;
        
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Check if we hit the token display area, background panel, or any of their children
            bool hitScrollArea = false;
            
            if (tokenDisplayArea != null && (hit.collider.gameObject == tokenDisplayArea || hit.collider.transform.IsChildOf(tokenDisplayArea.transform)))
            {
                hitScrollArea = true;
            }
            else if (displayBackgroundPanel != null && (hit.collider.gameObject == displayBackgroundPanel || hit.collider.transform.IsChildOf(displayBackgroundPanel.transform)))
            {
                hitScrollArea = true;
            }
            else if (scrollContainer != null && hit.collider.transform.IsChildOf(scrollContainer.transform))
            {
                hitScrollArea = true;
            }
            
            if (hitScrollArea)
            {
                isDragging = true;
                lastMousePosition = Input.mousePosition;
                scrollVelocity = 0f;
                Debug.Log($"[MenuController] Started drag on: {hit.collider.gameObject.name}");
            }
        }
    }
    
    private void OnDrag3D()
    {
        if (!isDragging || !isMenuActive) return;
        
        Vector3 currentMousePosition = Input.mousePosition;
        Vector3 dragDelta = currentMousePosition - lastMousePosition;
        
        ScrollContent3D(-dragDelta.y); // Inverted for natural feel
        
        lastMousePosition = currentMousePosition;
        scrollVelocity = -dragDelta.y; // Inverted for natural feel
    }
    
    private void OnEndDrag3D()
    {
        isDragging = false;
    }
    
    private void ScrollContent3D(float deltaY)
    {
        if (scrollContainer == null) return;
        
        currentScrollOffset -= deltaY * 0.01f * scrollSensitivity; // Scale with sensitivity
        
        // Calculate dynamic scroll limits based on number of tokens and display area
        float minScroll, maxScroll;
        CalculateScrollLimits(out minScroll, out maxScroll);
        
        currentScrollOffset = Mathf.Clamp(currentScrollOffset, minScroll, maxScroll);
        
        // Position tokens within the scroll container relative to its starting position
        Vector3 newPosition = scrollContainer.transform.localPosition;
        newPosition.y = currentScrollOffset; // Scroll vertically
        scrollContainer.transform.localPosition = newPosition;
        
        Debug.Log($"[MenuController] Scrolled to offset: {currentScrollOffset}, limits: [{minScroll}, {maxScroll}]");
    }
    
    private void CalculateScrollLimits(out float minScroll, out float maxScroll)
    {
        // Get display area bounds
        float displayHeight = GetDisplayAreaHeight();
        
        // Calculate total content height based on number of tokens
        int tokenCount = instantiatedTokens.Count;
        float totalContentHeight = tokenCount * rowHeight;
        
        // Calculate how much we can scroll
        float scrollableDistance = Mathf.Max(0f, totalContentHeight - displayHeight);
        
        // Since we reversed scroll direction earlier, we need to adjust limits:
        // When content is larger than display area:
        // - minScroll = 0 (top boundary - can't scroll up past first token)
        // - maxScroll = +scrollableDistance (bottom boundary - can't scroll down past last token)
        
        if (totalContentHeight <= displayHeight)
        {
            // Content fits in display area - no scrolling needed
            minScroll = 0f;
            maxScroll = 0f;
        }
        else
        {
            // Content is larger - allow scrolling within bounds
            minScroll = 0f;  // Top boundary (first token)
            maxScroll = scrollableDistance;  // Bottom boundary (last token)
        }
        
        //Debug.Log($"[MenuController] Scroll limits calculated: tokenCount={tokenCount}, displayHeight={displayHeight}, totalContentHeight={totalContentHeight}, scrollableDistance={scrollableDistance}, limits=[{minScroll}, {maxScroll}]");
    }
    
    private float GetDisplayAreaHeight()
    {
        if (displayBackgroundPanel == null)
        {
            // Fallback to menu height setting
            return menuHeight;
        }
        
        // Get actual display area height from background panel
        SpriteRenderer bgRenderer = displayBackgroundPanel.GetComponent<SpriteRenderer>();
        if (bgRenderer != null && bgRenderer.sprite != null)
        {
            Bounds spriteBounds = bgRenderer.sprite.bounds;
            Vector3 bgScale = displayBackgroundPanel.transform.localScale;
            return spriteBounds.size.y * bgScale.y;
        }
        
        // Fallback to menu height setting
        return menuHeight;
    }
    
    private void ApplyScrollPhysics()
    {
        if (!isMenuActive || isDragging) return;
        
        if (Mathf.Abs(scrollVelocity) > 0.1f)
        {
            ScrollContent3D(scrollVelocity);
            scrollVelocity *= scrollDeceleration;
        }
        
        // Get dynamic scroll limits
        float minScroll, maxScroll;
        CalculateScrollLimits(out minScroll, out maxScroll);
        
        // Bounce back if scrolled past limits
        if (currentScrollOffset < minScroll)
        {
            // Scrolled up too far past first token
            currentScrollOffset = Mathf.Lerp(currentScrollOffset, minScroll, bounceBackForce * Time.deltaTime);
            Vector3 newPosition = scrollContainer.transform.localPosition;
            newPosition.y = currentScrollOffset;
            scrollContainer.transform.localPosition = newPosition;
        }
        else if (currentScrollOffset > maxScroll)
        {
            // Scrolled down too far past last token
            currentScrollOffset = Mathf.Lerp(currentScrollOffset, maxScroll, bounceBackForce * Time.deltaTime);
            Vector3 newPosition = scrollContainer.transform.localPosition;
            newPosition.y = currentScrollOffset;
            scrollContainer.transform.localPosition = newPosition;
        }
    }
    
    public void OnTokenClicked(TokenMenuData tokenData, GameObject instantiatedToken)
    {
        Debug.Log($"[MenuController] Token clicked: {tokenData.tokenName}");
        SetMenuActive(false);
        
        if (instantiatedToken != null && SuperPowerSpawner.LocalInstance != null)
        {
            SuperPowerToken tokenScript = instantiatedToken.GetComponent<SuperPowerToken>();
            if (tokenScript != null && tokenScript.power != null)
            {
                Debug.Log($"[MenuController] Opening InfoBox for instantiated token with power: {tokenScript.power.name}");
                StartCoroutine(SuperPowerSpawner.LocalInstance.OpenInfoBox(tokenScript));
            }
            else
            {
                Debug.LogError($"[MenuController] TokenScript or power is null - tokenScript: {tokenScript != null}, power: {tokenScript?.power != null}");
            }
        }
        else
        {
            Debug.LogError($"[MenuController] InstantiatedToken or SuperPowerSpawner.LocalInstance is null");
        }
    }

    private IEnumerator EnforceTransformNextFrame(GameObject tokenInstance)
    {
        yield return null; // wait 1 frame
        if (tokenInstance == null) yield break;
        tokenInstance.transform.localScale = tokenScale;
        if (displayBackgroundPanel != null)
            tokenInstance.transform.localRotation = displayBackgroundPanel.transform.localRotation;
        else
            tokenInstance.transform.localRotation = Quaternion.identity;
    }
    
    private void SetupMaskingSystem()
    {
        Debug.Log("[MenuController] Setting up masking system");
        
        // Create SpriteMask for token sprites
        SetupSpriteMask();
        
        // Create world-space Canvas with RectMask2D for text clipping
        SetupTextMaskCanvas();
    }
    
    private void SetupSpriteMask()
    {
        if (tokenDisplayArea == null || displayBackgroundPanel == null) return;
        
        // Create SpriteMask GameObject
        GameObject spriteMaskObj = new GameObject("TokenSpriteMask");
        spriteMaskObj.transform.SetParent(tokenDisplayArea.transform, false);
        
        // Position and scale to match displayBackgroundPanel
        spriteMaskObj.transform.localPosition = displayBackgroundPanel.transform.localPosition;
        spriteMaskObj.transform.localRotation = displayBackgroundPanel.transform.localRotation;
        spriteMaskObj.transform.localScale = displayBackgroundPanel.transform.localScale;
        
        // Add SpriteMask component
        SpriteMask spriteMask = spriteMaskObj.AddComponent<SpriteMask>();
        
        // Get sprite from displayBackgroundPanel
        SpriteRenderer bgSpriteRenderer = displayBackgroundPanel.GetComponent<SpriteRenderer>();
        if (bgSpriteRenderer != null && bgSpriteRenderer.sprite != null)
        {
            spriteMask.sprite = bgSpriteRenderer.sprite;
            spriteMask.alphaCutoff = 0.5f;
            Debug.Log($"[MenuController] Created SpriteMask with sprite: {bgSpriteRenderer.sprite.name}");
        }
        else
        {
            Debug.LogWarning("[MenuController] Could not get sprite from DisplayBackgroundPanel for SpriteMask");
        }
    }
    
    private Canvas textMaskCanvas;
    
    private void SetupTextMaskCanvas()
    {
        if (tokenDisplayArea == null || displayBackgroundPanel == null) return;
        
        // Create world-space Canvas GameObject
        GameObject canvasObj = new GameObject("TextMaskCanvas");
        canvasObj.transform.SetParent(tokenDisplayArea.transform, false);
        
        // Add Canvas component
        textMaskCanvas = canvasObj.AddComponent<Canvas>();
        textMaskCanvas.renderMode = RenderMode.WorldSpace;
        textMaskCanvas.worldCamera = Camera.main;
        textMaskCanvas.sortingOrder = 15; // Above tokens
        
        // Add CanvasScaler for consistent sizing
        CanvasScaler canvasScaler = canvasObj.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        canvasScaler.scaleFactor = 1f;
        
        // Add GraphicRaycaster for interactions
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Position and scale to match displayBackgroundPanel
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.localPosition = displayBackgroundPanel.transform.localPosition;
        canvasRect.localRotation = displayBackgroundPanel.transform.localRotation;
        
        // Size to match the background panel sprite
        SpriteRenderer bgSpriteRenderer = displayBackgroundPanel.GetComponent<SpriteRenderer>();
        if (bgSpriteRenderer != null && bgSpriteRenderer.sprite != null)
        {
            Vector2 spriteSize = bgSpriteRenderer.sprite.bounds.size;
            canvasRect.sizeDelta = spriteSize * 100f; // Convert to Canvas units
            canvasRect.localScale = displayBackgroundPanel.transform.localScale * 0.01f; // Adjust for Canvas scaling
        }
        
        // Create viewport object with RectMask2D
        GameObject viewportObj = new GameObject("TextViewport");
        viewportObj.transform.SetParent(canvasObj.transform, false);
        
        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.anchoredPosition = Vector2.zero;
        viewportRect.sizeDelta = Vector2.zero;
        
        // Add RectMask2D for clipping
        viewportObj.AddComponent<RectMask2D>();
        
        Debug.Log("[MenuController] Created world-space Canvas with RectMask2D for text clipping");
    }
    
    private Vector3 CalculateTokenPosition(int index)
    {
        if (displayBackgroundPanel == null)
        {
            // Fallback to simple positioning
            float yOffset = -index * rowHeight;
            return new Vector3(-2f, yOffset, 0f);
        }
        
        // Get background panel bounds
        SpriteRenderer bgRenderer = displayBackgroundPanel.GetComponent<SpriteRenderer>();
        if (bgRenderer == null || bgRenderer.sprite == null)
        {
            // Fallback to simple positioning
            float yOffset = -index * rowHeight;
            return new Vector3(-2f, yOffset, 0f);
        }
        
        // Calculate bounds in local space
        Bounds spriteBounds = bgRenderer.sprite.bounds;
        Vector3 bgScale = displayBackgroundPanel.transform.localScale;
        Vector3 bgPosition = displayBackgroundPanel.transform.localPosition;
        
        // Calculate actual bounds considering scale
        float actualWidth = spriteBounds.size.x * bgScale.x;
        float actualHeight = spriteBounds.size.y * bgScale.y;
        
        // Calculate left edge position
        float leftEdge = bgPosition.x - (actualWidth * 0.5f);
        
        // Calculate token position: left edge + padding
        float tokenX = leftEdge + leftPadding;
        float tokenY = bgPosition.y + (actualHeight * 0.5f) - (index * rowHeight) - (rowHeight * 0.5f);
        
        Debug.Log($"[MenuController] Background bounds: width={actualWidth}, height={actualHeight}, leftEdge={leftEdge}");
        Debug.Log($"[MenuController] Token {index} position: x={tokenX}, y={tokenY}");
        
        return new Vector3(tokenX, tokenY, 0f);
    }
    
    private void CreateMaskedText(TokenMenuData tokenData, GameObject tokenInstance, int index)
    {
        if (textMaskCanvas == null)
        {
            Debug.LogWarning("[MenuController] TextMaskCanvas is null, cannot create masked text");
            return;
        }
        
        // Find the viewport object with RectMask2D
        Transform viewport = textMaskCanvas.transform.Find("TextViewport");
        if (viewport == null)
        {
            Debug.LogWarning("[MenuController] TextViewport not found in TextMaskCanvas");
            return;
        }
        
        // Get the actual world position of the token
        Vector3 tokenWorldPos = tokenInstance.transform.position;
        
        // Convert to Canvas local coordinates
        Vector3 canvasLocalPos = textMaskCanvas.transform.InverseTransformPoint(tokenWorldPos);
        
        // Scale factor for world-to-canvas conversion
        float scaleFactor = 50f; // Adjust this to get proper text size/positioning
        
        // Use token's Y and Z exactly, just add X offset for name
        float nameX = canvasLocalPos.x * scaleFactor + tokenToNameSpacing * scaleFactor;
        float nameY = canvasLocalPos.y * scaleFactor;
        
        // Create name text (UI version for masking)
        GameObject nameTextObj = new GameObject($"NameText_{index}");
        nameTextObj.transform.SetParent(viewport, false);
        
        RectTransform nameRect = nameTextObj.AddComponent<RectTransform>();
        nameRect.anchoredPosition = new Vector2(nameX, nameY);
        nameRect.sizeDelta = nameTextSize;
        
        TextMeshProUGUI nameText = nameTextObj.AddComponent<TextMeshProUGUI>();
        nameText.text = tokenData.tokenName;
        nameText.fontSize = nameTextFontSize;
        nameText.color = nameTextColor;
        nameText.alignment = nameTextAlignment;
        nameText.fontStyle = FontStyles.Bold;
        nameText.enableAutoSizing = true;
        nameText.fontSizeMin = nameTextMinSize;
        nameText.fontSizeMax = nameTextMaxSize;
        
        Debug.Log($"[MenuController] Created masked name text: {tokenData.tokenName} at ({nameX}, {nameY})");
        
        // Calculate rarity position: token X + both spacings
        float rarityX = canvasLocalPos.x * scaleFactor + (tokenToNameSpacing + nameToRaritySpacing) * scaleFactor;
        float rarityY = canvasLocalPos.y * scaleFactor;
        
        // Create rarity text (UI version for masking)
        GameObject rarityTextObj = new GameObject($"RarityText_{index}");
        rarityTextObj.transform.SetParent(viewport, false);
        
        RectTransform rarityRect = rarityTextObj.AddComponent<RectTransform>();
        rarityRect.anchoredPosition = new Vector2(rarityX, rarityY);
        rarityRect.sizeDelta = rarityTextSize;
        
        TextMeshProUGUI rarityText = rarityTextObj.AddComponent<TextMeshProUGUI>();
        rarityText.text = tokenData.rarity.ToString();
        rarityText.fontSize = rarityTextFontSize;
        rarityText.color = rarityTextColor;
        rarityText.alignment = rarityTextAlignment;
        rarityText.fontStyle = FontStyles.Bold;
        
        Debug.Log($"[MenuController] Created masked rarity text: {tokenData.rarity}");
        
        // Add text follower component to keep text aligned with token
        TextFollower nameFollower = nameTextObj.AddComponent<TextFollower>();
        nameFollower.Initialize(tokenInstance, textMaskCanvas, tokenToNameSpacing, scaleFactor);
        
        TextFollower rarityFollower = rarityTextObj.AddComponent<TextFollower>();
        rarityFollower.Initialize(tokenInstance, textMaskCanvas, tokenToNameSpacing + nameToRaritySpacing, scaleFactor);
    }
    
    private void CreateTokenText(TokenMenuData tokenData, GameObject tokenInstance, int index)
    {
        if (textMaskCanvas == null)
        {
            Debug.LogWarning("[MenuController] TextMaskCanvas is null, cannot create text");
            return;
        }
        
        // Find the viewport object with RectMask2D
        Transform viewport = textMaskCanvas.transform.Find("TextViewport");
        if (viewport == null)
        {
            Debug.LogWarning("[MenuController] TextViewport not found in TextMaskCanvas");
            return;
        }
        
        // Create name text positioned relative to token using world position
        GameObject nameTextObj = new GameObject($"NameText_{index}");
        nameTextObj.transform.SetParent(viewport, false);
        
        RectTransform nameRect = nameTextObj.AddComponent<RectTransform>();
        nameRect.sizeDelta = nameTextSize;
        
        TextMeshProUGUI nameText = nameTextObj.AddComponent<TextMeshProUGUI>();
        nameText.text = tokenData.tokenName;
        nameText.fontSize = nameTextFontSize;
        nameText.color = nameTextColor;
        nameText.alignment = nameTextAlignment;
        nameText.fontStyle = FontStyles.Bold;
        nameText.enableAutoSizing = true;
        nameText.fontSizeMin = nameTextMinSize;
        nameText.fontSizeMax = nameTextMaxSize;
        
        // Create rarity text positioned relative to token using world position
        GameObject rarityTextObj = new GameObject($"RarityText_{index}");
        rarityTextObj.transform.SetParent(viewport, false);
        
        RectTransform rarityRect = rarityTextObj.AddComponent<RectTransform>();
        rarityRect.sizeDelta = rarityTextSize;
        
        TextMeshProUGUI rarityText = rarityTextObj.AddComponent<TextMeshProUGUI>();
        rarityText.text = tokenData.rarity.ToString();
        rarityText.fontSize = rarityTextFontSize;
        rarityText.color = rarityTextColor;
        rarityText.alignment = rarityTextAlignment;
        rarityText.fontStyle = FontStyles.Bold;
        
        // Add world position followers that use token's world position directly
        WorldTextFollower nameFollower = nameTextObj.AddComponent<WorldTextFollower>();
        nameFollower.Initialize(tokenInstance, textMaskCanvas, tokenToNameSpacing, 0f);
        
        WorldTextFollower rarityFollower = rarityTextObj.AddComponent<WorldTextFollower>();
        rarityFollower.Initialize(tokenInstance, textMaskCanvas, tokenToNameSpacing + nameToRaritySpacing, 0f);
        
        Debug.Log($"[MenuController] Created world-positioned text for: {tokenData.tokenName}");
    }
    
    private IEnumerator OpenMenuInInfoBox()
    {
        if (SuperPowerSpawner.LocalInstance == null)
        {
            Debug.LogError("[MenuController] SuperPowerSpawner.LocalInstance is null, cannot open menu in InfoBox");
            yield break;
        }
        
        // Check if we need to close any existing InfoBox content first
        if (SuperPowerToken.ActiveInstance != null)
        {
            yield return StartCoroutine(SuperPowerSpawner.LocalInstance.CloseInfoBoxImmediate());
        }
        
        // Create a menu token that behaves like a regular SuperPowerToken
        GameObject menuTokenObj = new GameObject("MenuPageToken");
        SuperPowerToken menuToken = menuTokenObj.AddComponent<SuperPowerToken>();
        
        // Create a menu power
        MenuPagePower menuPower = ScriptableObject.CreateInstance<MenuPagePower>();
        menuPower.name = "Menu";
        menuPower.description = "";
        
        menuToken.power = menuPower;
        
        // Set this as the active instance (important for proper behavior)
        SuperPowerToken.ActiveInstance = menuToken;
        
        // Open InfoBox with proper animation (same as token behavior)
        yield return StartCoroutine(SuperPowerSpawner.LocalInstance.OpenInfoBox(menuToken));
        
        // Now show our menu
        SetMenuActive(true);
        
        Debug.Log("[MenuController] Menu opened in InfoBox with proper token behavior");
    }
    
    /// <summary>
    /// Setup click handler for the open button to make it work as a toggle
    /// </summary>
    private void SetupOpenButtonClickHandler()
    {
        if (openButtonGameObject == null)
        {
            Debug.LogWarning("[MenuController] OpenButtonGameObject is null, cannot setup click handler");
            return;
        }
        
        // Get or add Button component
        Button button = openButtonGameObject.GetComponent<Button>();
        if (button == null)
        {
            button = openButtonGameObject.AddComponent<Button>();
            Debug.Log("[MenuController] Added Button component to open button");
        }
        
        // Ensure the button is properly configured
        button.interactable = true;
        
        // Clear existing listeners to avoid duplicates
        button.onClick.RemoveAllListeners();
        
        // Add the toggle listener - now calls ShowTokenMenu which has toggle logic
        button.onClick.AddListener(ShowTokenMenu);
        
        // Ensure the button has a collider for 3D interaction if needed
        Collider buttonCollider = openButtonGameObject.GetComponent<Collider>();
        if (buttonCollider == null)
        {
            BoxCollider boxCollider = openButtonGameObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            Debug.Log("[MenuController] Added BoxCollider to open button for 3D interaction");
        }
        
        Debug.Log("[MenuController] Open button listener setup complete - using Unity Button onClick");
    }
}

// Click handler component for 3D tokens
public class TokenClickHandler : MonoBehaviour
{
    private TokenMenuData tokenData;
    private MenuController menuController;
    private GameObject instantiatedToken;
    
    public void Initialize(TokenMenuData data, MenuController controller, GameObject token)
    {
        tokenData = data;
        menuController = controller;
        instantiatedToken = token;
    }
    
    void OnMouseDown()
    {
        if (menuController != null && tokenData != null && instantiatedToken != null)
        {
            menuController.OnTokenClicked(tokenData, instantiatedToken);
        }
        else
        {
            Debug.LogError($"[TokenClickHandler] Missing references - menuController: {menuController != null}, tokenData: {tokenData != null}, instantiatedToken: {instantiatedToken != null}");
        }
    }
}

// Simple SuperPower class for menu page
public class MenuPagePower : SuperPower
{
    public override void ActivatePower()
    {
        // Menu doesn't activate like a regular power
        Debug.Log("[MenuPagePower] Menu page power - no activation needed");
    }
}

// Component to make text follow tokens during scroll
public class TextFollower : MonoBehaviour
{
    private GameObject targetToken;
    private Canvas canvas;
    private RectTransform rectTransform;
    private float xOffset;
    private float scaleFactor;
    
    public void Initialize(GameObject token, Canvas maskCanvas, float xOffsetValue, float scale)
    {
        targetToken = token;
        canvas = maskCanvas;
        rectTransform = GetComponent<RectTransform>();
        xOffset = xOffsetValue;
        scaleFactor = scale;
    }
    
    void Update()
    {
        if (targetToken == null || canvas == null || rectTransform == null)
            return;
            
        // Get token's current world position
        Vector3 tokenWorldPos = targetToken.transform.position;
        
        // Convert to Canvas local coordinates
        Vector3 canvasLocalPos = canvas.transform.InverseTransformPoint(tokenWorldPos);
        
        // Use token's exact Y and Z, just add X offset
        float textX = canvasLocalPos.x * scaleFactor + xOffset * scaleFactor;
        float textY = canvasLocalPos.y * scaleFactor;
        
        // Update text position
        rectTransform.anchoredPosition = new Vector2(textX, textY);
    }
}

// Simpler component that uses world positions directly
public class WorldTextFollower : MonoBehaviour
{
    private GameObject targetToken;
    private Canvas canvas;
    private RectTransform rectTransform;
    private Camera cam;
    private float worldOffsetX;
    private float worldOffsetY;
    
    public void Initialize(GameObject token, Canvas maskCanvas, float xOffset, float yOffset)
    {
        targetToken = token;
        canvas = maskCanvas;
        rectTransform = GetComponent<RectTransform>();
        cam = Camera.main;
        worldOffsetX = xOffset;
        worldOffsetY = yOffset;
    }
    
    void Update()
    {
        if (targetToken == null || canvas == null || rectTransform == null || cam == null)
            return;
            
        // Get token's world position and add offset in world space
        Vector3 tokenWorldPos = targetToken.transform.position;
        Vector3 textWorldPos = new Vector3(
            tokenWorldPos.x + worldOffsetX,
            tokenWorldPos.y + worldOffsetY,
            tokenWorldPos.z
        );
        
        // Convert world position to screen position
        Vector3 screenPos = cam.WorldToScreenPoint(textWorldPos);
        
        // Convert screen position to Canvas local position
        Vector2 canvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            screenPos,
            cam,
            out canvasPos
        );
        
        // Update text position
        rectTransform.anchoredPosition = canvasPos;
    }
}

