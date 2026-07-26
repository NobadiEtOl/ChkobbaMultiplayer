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

    [Header("Filter Settings")]
    [SerializeField] private Button tier1FilterButton;
    [SerializeField] private Button tier2FilterButton;
    [SerializeField] private Button tier3FilterButton;
    [SerializeField] private GameObject filterButtonContainer;
    [SerializeField] private Color normalFilterColor = Color.white;
    [SerializeField] private Color selectedFilterColor = new Color(0.7f, 0.7f, 0.7f);

    // Private variables
    private List<TokenMenuData> tokenDataList = new List<TokenMenuData>();
    private List<GameObject> instantiatedTokens = new List<GameObject>();
    private List<GameObject> instantiatedTextElements = new List<GameObject>();
    private bool isMenuActive = false;
    private bool menuInitialized = false;
    private int maxDisplayIndex = 0; // Track the highest display index (includes tier separators)
    private int currentFilterTier = -1; // -1 means Show All
    
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
        
        
        
        if (SuperPowerSpawner.LocalInstance == null)
        {
            
            StartCoroutine(WaitForSuperPowerSpawnerAndInitialize());
        }
        else
        {
            
            InitializeMenu();
        }
        
        // Setup open button click handler
        SetupOpenButtonClickHandler();
        
        // Setup filter button click handlers
        SetupFilterButtonClickHandler();
    }
    
    private void SetupFilterButtonClickHandler()
    {
        if (tier1FilterButton != null)
        {
            tier1FilterButton.onClick.RemoveAllListeners();
            tier1FilterButton.onClick.AddListener(() => OnTierButtonClicked(1));
        }
        if (tier2FilterButton != null)
        {
            tier2FilterButton.onClick.RemoveAllListeners();
            tier2FilterButton.onClick.AddListener(() => OnTierButtonClicked(2));
        }
        if (tier3FilterButton != null)
        {
            tier3FilterButton.onClick.RemoveAllListeners();
            tier3FilterButton.onClick.AddListener(() => OnTierButtonClicked(3));
        }
    }

    private IEnumerator WaitForSuperPowerSpawnerAndInitialize()
    {
        
        
        while (SuperPowerSpawner.LocalInstance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        
        InitializeMenu();
    }
    
    private void InitializeMenu()
    {
        // Validate scene references
        if (!ValidateSceneReferences())
        {
            
            return;
        }
        
        LoadTokenData();
        SortTokensByTier();
        CreateTokensInSceneContainer();
        SetMenuActive(false);
        
    }
    
    private bool ValidateSceneReferences()
    {
        bool valid = true;
        
        if (tokenDisplayArea == null)
        {
            
            valid = false;
        }
        
        if (displayBackgroundPanel == null)
        {
            
            valid = false;
        }
        
        if (scrollContainer == null)
        {
            
            valid = false;
        }
        
        if (valid)
        {
            // Add colliders if missing (needed for scroll detection)
            EnsureScrollColliders();
            
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
        if (SuperPowerSpawner.LocalInstance == null)
        {
            
            return;
        }

        
        SuperPowerSpawner.LocalInstance.SetNameText("Menu");

        // If menu is already the active page, toggle it closed.
        if (SuperPowerSpawner.LocalInstance.isInfoBoxOpen && SuperPowerSpawner.LocalInstance.isMenuPageOpen)
        {
            
            StartCoroutine(SuperPowerSpawner.LocalInstance.CloseInfoBox());
            return;
        }
        
        
        
        if (!menuInitialized)
        {
            
            InitializeMenu();
        }
        
        // Always open/switch to menu page instead of toggling close.
        StartCoroutine(OpenMenuInInfoBox());
    }
    
    
    [ContextMenu("Force Initialize Menu")]
    public void ForceInitializeMenu()
    {
        
        if (SuperPowerSpawner.LocalInstance == null)
        {
            
            return;
        }
        
        InitializeMenu();
        
    }
    
    [ContextMenu("Test Toggle InfoBox")]
    public void TestToggleInfoBox()
    {
        
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
        
        
        
        
        
        
        
        if (tokenDisplayArea != null)
        {
            
            
            
        }
        else
        {
            
        }
        
        if (scrollContainer != null)
        {
            
            
            
            
            for (int i = 0; i < scrollContainer.transform.childCount; i++)
            {
                Transform child = scrollContainer.transform.GetChild(i);
                
            }
        }
        else
        {
            
        }
        
        
    }
    
    private void CreateTokensInSceneContainer()
    {
        
        
        if (scrollContainer == null)
        {
            
            return;
        }
        
        // Clear any existing tokens first
        ClearAllTokens();
        
        // Setup masking system before creating tokens
        SetupMaskingSystem();
        
        // Create all tokens in the scene-provided scroll container
        CreateAllTokens();
        
        menuInitialized = true;
        
        
        // Final hierarchy check
        
        
        
        
        
        
    }
    
    private void LoadTokenData()
    {
        tokenDataList.Clear();
        
        
        
        if (SuperPowerSpawner.LocalInstance == null) 
        {
            
            return;
        }
        
        
        var allTokenData = SuperPowerSpawner.LocalInstance.GetAllTokenData();
        
        
        if (allTokenData.Count == 0)
        {
            
            return;
        }
        
        foreach (var (tokenPrefab, power) in allTokenData)
        {
            if (tokenPrefab == null)
            {
                
                continue;
            }
            
            if (power == null)
            {
                
                continue;
            }
            
            SpriteRenderer spriteRenderer = tokenPrefab.GetComponent<SpriteRenderer>();
            Image imageComponent = tokenPrefab.GetComponent<Image>();
            
            Sprite tokenSprite = null;
            if (spriteRenderer != null) tokenSprite = spriteRenderer.sprite;
            else if (imageComponent != null) tokenSprite = imageComponent.sprite;
            
            TokenMenuData tokenData = new TokenMenuData(tokenSprite, power.name, power.powerCostTier, power, tokenPrefab);
            tokenDataList.Add(tokenData);
            
            
        }
        
        
    }
    
    private void SortTokensByTier()
    {
        // Sort tokens by powerCostTier (stored in rarity field) in ascending order
        tokenDataList.Sort((a, b) => a.rarity.CompareTo(b.rarity));
        
        // Log the sorted order with tier separators for clarity
        
        int currentTier = -1;
        for (int i = 0; i < tokenDataList.Count; i++)
        {
            if (tokenDataList[i].rarity != currentTier)
            {
                currentTier = tokenDataList[i].rarity;
                
            }
            
        }
        
    }
    
    private void CreateAllTokens()
    {
        // Clear any existing tokens
        ClearAllTokens();
        
        // Filter the list based on currentFilterTier
        List<TokenMenuData> filteredList;
        if (currentFilterTier == -1)
        {
            filteredList = tokenDataList;
        }
        else
        {
            filteredList = tokenDataList.FindAll(t => t.rarity == currentFilterTier);
        }

        if (filteredList.Count == 0)
        {
            
            return;
        }
        
        
        
        int currentTier = -1;
        int displayIndex = 0; // Track display index including separators
        maxDisplayIndex = 0; // Reset max display index
        
        for (int i = 0; i < filteredList.Count; i++)
        {
            // Check if tier changed - add separator
            if (filteredList[i].rarity != currentTier)
            {
                currentTier = filteredList[i].rarity;
                
                // Create tier header separator
                if (i > 0) // Don't add separator before first tier
                {
                    displayIndex++; // Add spacing for separator
                    
                }
            }
            
            CreateToken3D(filteredList[i], displayIndex);
            displayIndex++;
        }
        
        // Store the maximum display index for scroll calculations
        maxDisplayIndex = displayIndex > 0 ? displayIndex - 1 : 0;
        
    }

    public void OnTierButtonClicked(int tier)
    {
        if (tier == currentFilterTier) return;
        
        if (SuperPowerSpawner.LocalInstance != null)
        {
            StartCoroutine(SuperPowerSpawner.LocalInstance.SwitchMenuTierWithAnimation(() => SetFilterAndRefresh(tier)));
        }
        else
        {
            SetFilterAndRefresh(tier);
        }
    }

    public void SetFilterAndRefresh(int tier)
    {
        currentFilterTier = tier;
        ClearAllTokens();
        CreateAllTokens();
        UpdateFilterButtonVisuals();
        
        // Reset scroll position when switching tiers
        currentScrollOffset = 0f;
        if (scrollContainer != null)
        {
            Vector3 resetPosition = scrollContainer.transform.localPosition;
            resetPosition.y = 0f;
            scrollContainer.transform.localPosition = resetPosition;
        }
    }

    private void UpdateFilterButtonVisuals()
    {
        if (tier1FilterButton != null && tier1FilterButton.image != null)
            tier1FilterButton.image.color = (currentFilterTier == 1) ? selectedFilterColor : normalFilterColor;
            
        if (tier2FilterButton != null && tier2FilterButton.image != null)
            tier2FilterButton.image.color = (currentFilterTier == 2) ? selectedFilterColor : normalFilterColor;
            
        if (tier3FilterButton != null && tier3FilterButton.image != null)
            tier3FilterButton.image.color = (currentFilterTier == 3) ? selectedFilterColor : normalFilterColor;
    }
    
    private void CreateToken3D(TokenMenuData tokenData, int index)
    {
        
        
        try
        {
            // Check if tokenData is valid
            if (tokenData == null)
            {
                
                return;
            }
            
            if (tokenData.tokenPrefab == null)
            {
                
                return;
            }
            
            if (scrollContainer == null)
            {
                
                return;
            }
            
            
            
            // Instantiate the actual token prefab
            GameObject tokenInstance = Instantiate(tokenData.tokenPrefab, scrollContainer.transform);
            
            if (tokenInstance == null)
            {
                
                return;
            }
            
            
            
            // CRITICAL: Assign the power to the token script to prevent "Power is not assigned" error
            SuperPowerToken tokenScript = tokenInstance.GetComponent<SuperPowerToken>();
            if (tokenScript != null && tokenData.superPower != null)
            {
                tokenScript.power = tokenData.superPower;
                
            }
            else
            {
                
            }
            
            tokenInstance.name = $"Token_{tokenData.tokenName}_{index}";
            
            
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
            
            
            // Position the token using background panel bounds
            Vector3 tokenPosition = CalculateTokenPosition(index);
            tokenInstance.transform.localPosition = tokenPosition;
            
            
            // Re-apply scale and rotation next frame to guard against external changes
            StartCoroutine(EnforceTransformNextFrame(tokenInstance));
            
            // Remove smoke effect child if it exists
            Transform smokeEffect = tokenInstance.transform.Find("SmokeEffectGameObject");
            if (smokeEffect != null)
            {
                DestroyImmediate(smokeEffect.gameObject);
                
            }
            
            // FORCE THE TOKEN TO BE ACTIVE
            tokenInstance.SetActive(true);
            
            
            // Configure SpriteRenderer for masking
            SpriteRenderer tokenRenderer = tokenInstance.GetComponent<SpriteRenderer>();
            if (tokenRenderer != null)
            {
                tokenRenderer.sortingOrder = 10; // High sorting order to appear on top
                tokenRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask; // Enable masking
                
            }
            else
            {
                
            }
            
            // Create text elements as children of the token
            CreateTokenText(tokenData, tokenInstance, index);
            
            
            
            // Check for existing colliders (tokens should already have BoxCollider3D)
            Collider existingCollider3D = tokenInstance.GetComponent<Collider>();
            Collider2D existingCollider2D = tokenInstance.GetComponent<Collider2D>();
            
            if (existingCollider3D != null)
            {
                
            }
            else if (existingCollider2D != null)
            {
                
            }
            else
            {
                // Only add if no collider exists
                BoxCollider collider = tokenInstance.AddComponent<BoxCollider>();
                collider.size = Vector3.one;
                
            }
            
            // Add click handler component
            TokenClickHandler clickHandler = tokenInstance.GetComponent<TokenClickHandler>();
            if (clickHandler == null)
            {
                clickHandler = tokenInstance.AddComponent<TokenClickHandler>();
            }
            clickHandler.Initialize(tokenData, this, tokenInstance); // Pass the instantiated token
            
            
            instantiatedTokens.Add(tokenInstance);
            
            
            
            
            
        }
        catch (System.Exception e)
        {
            
        }
    }
    
    public void SetMenuActive(bool active)
    {
        
        if (tokenDisplayArea != null)
        {
            tokenDisplayArea.SetActive(active);
            
        }
        else
        {
            
        }
        
        if (filterButtonContainer != null)
        {
            filterButtonContainer.SetActive(active);
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
            
        }
    }
    
    private void ClearAllTokens()
    {
        // Clear token GameObjects
        foreach (GameObject token in instantiatedTokens)
        {
            if (token != null) DestroyImmediate(token);
        }
        instantiatedTokens.Clear();
        
        // Clear text elements (name and rarity texts)
        foreach (GameObject textElement in instantiatedTextElements)
        {
            if (textElement != null) DestroyImmediate(textElement);
        }
        instantiatedTextElements.Clear();
    }
    
    [ContextMenu("Close Token Menu")]
    public void CloseTokenMenu()
    {
        SetMenuActive(false);
        
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
        
        
    }
    
    private void CalculateScrollLimits(out float minScroll, out float maxScroll)
    {
        // Get display area bounds
        float displayHeight = GetDisplayAreaHeight();
        
        // Calculate total content height based on maximum display index (includes tier separators)
        // We use maxDisplayIndex + 1 to account for all positioned rows
        float totalContentHeight = (maxDisplayIndex + 1) * rowHeight;
        
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
        
        SetMenuActive(false);
        // InfoBox opening removed from here; now handled by SuperPowerToken drag logic only
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
            
        }
        else
        {
            
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
        
        
        
        
        return new Vector3(tokenX, tokenY, 0f);
    }
    
    private void CreateMaskedText(TokenMenuData tokenData, GameObject tokenInstance, int index)
    {
        if (textMaskCanvas == null)
        {
            
            return;
        }
        
        // Find the viewport object with RectMask2D
        Transform viewport = textMaskCanvas.transform.Find("TextViewport");
        if (viewport == null)
        {
            
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
        
        
        
        // Add text follower component to keep text aligned with token
        TextFollower nameFollower = nameTextObj.AddComponent<TextFollower>();
        nameFollower.Initialize(tokenInstance, textMaskCanvas, tokenToNameSpacing, scaleFactor);
        
        TextFollower rarityFollower = rarityTextObj.AddComponent<TextFollower>();
        rarityFollower.Initialize(tokenInstance, textMaskCanvas, tokenToNameSpacing + nameToRaritySpacing, scaleFactor);
        
        // Track text elements for cleanup when switching tiers
        instantiatedTextElements.Add(nameTextObj);
        instantiatedTextElements.Add(rarityTextObj);
    }
    
    private void CreateTokenText(TokenMenuData tokenData, GameObject tokenInstance, int index)
    {
        if (textMaskCanvas == null)
        {
            
            return;
        }
        
        // Find the viewport object with RectMask2D
        Transform viewport = textMaskCanvas.transform.Find("TextViewport");
        if (viewport == null)
        {
            
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
        
        // Track text elements for cleanup when switching tiers
        instantiatedTextElements.Add(nameTextObj);
        instantiatedTextElements.Add(rarityTextObj);
        
        
    }
    
    private IEnumerator OpenMenuInInfoBox()
    {
        if (SuperPowerSpawner.LocalInstance == null)
        {
            
            yield break;
        }

        // Reset filter to -1 (Show All) when opening menu
        currentFilterTier = -1;
        UpdateFilterButtonVisuals();

        // If menu page is already open, keep it open and ensure menu visuals stay active.
        if (SuperPowerSpawner.LocalInstance.isInfoBoxOpen && SuperPowerSpawner.LocalInstance.isMenuPageOpen)
        {
            // Re-create tokens to reflect the reset filter if needed
            // However, usually it's better to clear and recreate if we want to ensure "Show All"
            CreateAllTokens();
            
            SetMenuActive(true);
            
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
        
        // Ensure tokens are created with the reset filter
        CreateAllTokens();

        // Now show our menu
        SetMenuActive(true);
        
        
    }
    
    /// <summary>
    /// Setup click handler for the open button to open/switch to the menu page
    /// </summary>
    private void SetupOpenButtonClickHandler()
    {
        if (openButtonGameObject == null)
        {
            
            return;
        }
        
        // Get or add Button component
        Button button = openButtonGameObject.GetComponent<Button>();
        if (button == null)
        {
            button = openButtonGameObject.AddComponent<Button>();
            
        }
        
        // Ensure the button is properly configured
        button.interactable = true;
        
        // Clear existing listeners to avoid duplicates
        button.onClick.RemoveAllListeners();
        
        // Add listener that always opens/switches to the menu page.
        button.onClick.AddListener(ShowTokenMenu);
        
        // Ensure the button has a collider for 3D interaction if needed
        Collider buttonCollider = openButtonGameObject.GetComponent<Collider>();
        if (buttonCollider == null)
        {
            BoxCollider boxCollider = openButtonGameObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            
        }
        
        
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
            
        }
    }
}

// Simple SuperPower class for menu page
public class MenuPagePower : SuperPower
{
    public override void ActivatePower()
    {
        // Menu doesn't activate like a regular power
        
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

