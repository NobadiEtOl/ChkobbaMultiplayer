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
    
    // 3D Menu References (created at start)
    private GameObject menuContainer;
    private GameObject scrollContainer;
    private GameObject backgroundPanel;
    
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
        LoadTokenData();
        CreateMenu3D();
        SetMenuActive(false);
        
        Debug.Log("[MenuController] 3D Menu initialized successfully");
    }
    
    void Update()
    {
        HandleScrollInput();
        ApplyScrollPhysics();
    }
    
    [ContextMenu("Test Token Menu")]
    public void ShowTokenMenu()
    {
        if (!menuInitialized)
        {
            Debug.LogWarning("[MenuController] Menu not initialized yet, trying to initialize now...");
            if (SuperPowerSpawner.LocalInstance != null)
            {
                InitializeMenu();
            }
            else
            {
                Debug.LogError("[MenuController] Cannot initialize - SuperPowerSpawner.LocalInstance is still null!");
                return;
            }
        }
        
        SetMenuActive(true);
        Debug.Log("[MenuController] 3D Token menu opened");
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
    
    [ContextMenu("Debug Hierarchy State")]
    public void DebugHierarchyState()
    {
        Debug.Log("=== HIERARCHY DEBUG START ===");
        Debug.Log($"MenuController GameObject: {this.gameObject.name} (active: {this.gameObject.activeInHierarchy})");
        Debug.Log($"MenuController position: {this.transform.position}");
        Debug.Log($"MenuInitialized: {menuInitialized}");
        Debug.Log($"TokenDataList count: {tokenDataList.Count}");
        Debug.Log($"InstantiatedTokens count: {instantiatedTokens.Count}");
        
        if (menuContainer != null)
        {
            Debug.Log($"MenuContainer: {menuContainer.name} (active: {menuContainer.activeInHierarchy})");
            Debug.Log($"MenuContainer position: {menuContainer.transform.position}");
            Debug.Log($"MenuContainer children: {menuContainer.transform.childCount}");
        }
        else
        {
            Debug.Log("MenuContainer: NULL");
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
    
    private void CreateMenu3D()
    {
        Debug.Log("[MenuController] CreateMenu3D called");
        
        try
        {
            // Create main menu container as child of this GameObject
            menuContainer = new GameObject("TokenMenu3D");
            if (menuContainer == null)
            {
                Debug.LogError("[MenuController] Failed to create menuContainer!");
                return;
            }
            
            menuContainer.transform.SetParent(this.transform, false);
            menuContainer.transform.localPosition = menuStartPosition;
            Debug.Log($"[MenuController] Created menuContainer at position: {menuStartPosition}");
            
            // Create background panel (optional - can be a simple plane or quad)
            backgroundPanel = GameObject.CreatePrimitive(PrimitiveType.Quad);
            if (backgroundPanel == null)
            {
                Debug.LogError("[MenuController] Failed to create backgroundPanel!");
                return;
            }
            
            backgroundPanel.name = "MenuBackground";
            backgroundPanel.transform.SetParent(menuContainer.transform, false);
            backgroundPanel.transform.localPosition = Vector3.zero;
            backgroundPanel.transform.localScale = new Vector3(menuWidth, menuHeight, 1f);
            backgroundPanel.transform.localRotation = Quaternion.identity;
            Debug.Log($"[MenuController] Created backgroundPanel with scale: {menuWidth}x{menuHeight}");
            
            // Set background color
            Renderer bgRenderer = backgroundPanel.GetComponent<Renderer>();
            if (bgRenderer != null)
            {
                bgRenderer.material = new Material(Shader.Find("Standard"));
                bgRenderer.material.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
                bgRenderer.material.SetFloat("_Mode", 3); // Transparent mode
                bgRenderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                bgRenderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                bgRenderer.material.SetInt("_ZWrite", 0);
                bgRenderer.material.DisableKeyword("_ALPHATEST_ON");
                bgRenderer.material.EnableKeyword("_ALPHABLEND_ON");
                bgRenderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                bgRenderer.material.renderQueue = 3000;
                Debug.Log("[MenuController] Set background material properties");
            }
            
            // Create scroll container for tokens
            scrollContainer = new GameObject("ScrollContainer");
            if (scrollContainer == null)
            {
                Debug.LogError("[MenuController] Failed to create scrollContainer!");
                return;
            }
            
            scrollContainer.transform.SetParent(menuContainer.transform, false);
            Vector3 scrollPos = new Vector3(0f, menuHeight * 0.4f, 0.1f);
            scrollContainer.transform.localPosition = scrollPos;
            Debug.Log($"[MenuController] Created scrollContainer at position: {scrollPos}");
            
            // FORCE ALL CONTAINERS TO BE ACTIVE
            menuContainer.SetActive(true);
            backgroundPanel.SetActive(true);
            scrollContainer.SetActive(true);
            Debug.Log("[MenuController] Forced all containers to be active");
            
            // Create and position all tokens at start
            Debug.Log("[MenuController] About to call CreateAllTokens()");
            CreateAllTokens();
            
            menuInitialized = true;
            Debug.Log("[MenuController] ✅ 3D Menu created successfully");
            
            // Final hierarchy check
            Debug.Log($"[MenuController] Final hierarchy check:");
            Debug.Log($"  - MenuController: {this.name}");
            Debug.Log($"  - MenuContainer: {menuContainer?.name} (active: {menuContainer?.activeInHierarchy})");
            Debug.Log($"  - BackgroundPanel: {backgroundPanel?.name} (active: {backgroundPanel?.activeInHierarchy})");
            Debug.Log($"  - ScrollContainer: {scrollContainer?.name} (active: {scrollContainer?.activeInHierarchy})");
            Debug.Log($"  - ScrollContainer children count: {scrollContainer?.transform.childCount}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MenuController] Exception in CreateMenu3D: {e.Message}\n{e.StackTrace}");
        }
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
            Debug.Log($"[MenuController] Set scale to: {tokenScale}");
            
            // Position the token
            float yOffset = -index * rowHeight;
            Vector3 newPosition = new Vector3(0f, yOffset, 0f);
            tokenInstance.transform.localPosition = newPosition;
            Debug.Log($"[MenuController] Set position to: {newPosition} (world: {tokenInstance.transform.position})");
            
            // FORCE THE TOKEN TO BE ACTIVE
            tokenInstance.SetActive(true);
            Debug.Log($"[MenuController] Forced token active state: {tokenInstance.activeInHierarchy}");
            
            // Ensure it's visible by adjusting sorting layer or z-position
            SpriteRenderer tokenRenderer = tokenInstance.GetComponent<SpriteRenderer>();
            if (tokenRenderer != null)
            {
                tokenRenderer.sortingOrder = 10; // High sorting order to appear on top
                Debug.Log($"[MenuController] Set SpriteRenderer sorting order to 10");
            }
            else
            {
                Debug.LogWarning($"[MenuController] No SpriteRenderer found on token {tokenData.tokenName}");
            }
            
            // Create name text (TextMeshPro, not UI)
            GameObject nameTextObj = new GameObject($"NameText_{index}");
            nameTextObj.transform.SetParent(tokenInstance.transform, false);
            nameTextObj.transform.localPosition = new Vector3(2f, 0f, -0.1f); // To the right of token, slightly forward
            nameTextObj.transform.localRotation = Quaternion.Euler(0f, 0f, 180f); // Rotate 180 degrees to fix mirroring
            
            TextMeshPro nameText = nameTextObj.AddComponent<TextMeshPro>();
            nameText.text = tokenData.tokenName;
            nameText.fontSize = 4f;
            nameText.color = Color.white;
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.sortingOrder = 11;
            
            Debug.Log($"[MenuController] Created name text: {tokenData.tokenName}");
            
            // Create rarity text (TextMeshPro, not UI)
            GameObject rarityTextObj = new GameObject($"RarityText_{index}");
            rarityTextObj.transform.SetParent(tokenInstance.transform, false);
            rarityTextObj.transform.localPosition = new Vector3(6f, 0f, -0.1f); // Further to the right
            rarityTextObj.transform.localRotation = Quaternion.Euler(0f, 0f, 180f); // Rotate 180 degrees to fix mirroring
            
            TextMeshPro rarityText = rarityTextObj.AddComponent<TextMeshPro>();
            rarityText.text = tokenData.rarity.ToString();
            rarityText.fontSize = 4f;
            rarityText.color = Color.yellow;
            rarityText.alignment = TextAlignmentOptions.Center;
            rarityText.sortingOrder = 11;
            
            Debug.Log($"[MenuController] Created rarity text: {tokenData.rarity}");
            
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
    
    private void SetMenuActive(bool active)
    {
        if (menuContainer != null) menuContainer.SetActive(active);
        isMenuActive = active;
        if (!active) currentScrollOffset = 0f;
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
        
        // Mouse scroll wheel
        if (Input.mouseScrollDelta.y != 0)
        {
            float scrollAmount = Input.mouseScrollDelta.y * scrollSensitivity;
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
            // Check if we hit the scroll container or any token
            if (hit.collider.transform.IsChildOf(scrollContainer.transform))
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
        
        ScrollContent3D(dragDelta.y);
        
        lastMousePosition = currentMousePosition;
        scrollVelocity = dragDelta.y;
    }
    
    private void OnEndDrag3D()
    {
        isDragging = false;
    }
    
    private void ScrollContent3D(float deltaY)
    {
        if (scrollContainer == null) return;
        
        currentScrollOffset -= deltaY;
        currentScrollOffset = Mathf.Clamp(currentScrollOffset, -maxScrollDistance, 0f);
        
        Vector3 newPosition = scrollContainer.transform.localPosition;
        newPosition.y = menuHeight * 0.4f + currentScrollOffset;
        scrollContainer.transform.localPosition = newPosition;
    }
    
    private void ApplyScrollPhysics()
    {
        if (!isMenuActive || isDragging) return;
        
        if (Mathf.Abs(scrollVelocity) > 0.1f)
        {
            ScrollContent3D(scrollVelocity);
            scrollVelocity *= scrollDeceleration;
        }
        
        // Bounce back if scrolled too far
        if (currentScrollOffset > 0f)
        {
            currentScrollOffset = Mathf.Lerp(currentScrollOffset, 0f, bounceBackForce * Time.deltaTime);
            Vector3 newPosition = scrollContainer.transform.localPosition;
            newPosition.y = menuHeight * 0.4f + currentScrollOffset;
            scrollContainer.transform.localPosition = newPosition;
        }
        else if (currentScrollOffset < -maxScrollDistance)
        {
            currentScrollOffset = Mathf.Lerp(currentScrollOffset, -maxScrollDistance, bounceBackForce * Time.deltaTime);
            Vector3 newPosition = scrollContainer.transform.localPosition;
            newPosition.y = menuHeight * 0.4f + currentScrollOffset;
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
