using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;

public class HesapMakinesiController : MonoBehaviour
{
    [Header("Idle Movement Animation")]
    [SerializeField] private float moveAmplitudeX = 0.5f;
    [SerializeField] private float moveAmplitudeZ = 0.3f;
    [SerializeField] private float idleFrameRate = 10f; // Frames per second
    
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 0.25f;
    [SerializeField] private Ease moveEase = Ease.OutQuad;
    
    [Header("Coin Interaction")]
    [SerializeField] private float coinDistanceThreshold = 1f; // Distance from start to trigger movement
    
    [Header("UI Displays")]
    [SerializeField] private TextMeshProUGUI tableCoinAmountDisplay; // Shows gold amount at table
    [SerializeField] private TextMeshProUGUI hesapMakinesiCoinAmountDisplay; // Shows gold amount in calculator
    [SerializeField] private Transform tableSelectedModeDisplay; // Container for mode stars at table
    [SerializeField] private Transform hesapMakinesiSelectedModeDisplay; // Container for mode stars in calculator
    [SerializeField] private Sprite filledStarSprite; // Filled star sprite for mode display
    [SerializeField] private Sprite emptyStarSprite; // Empty star sprite for mode display

    [Header("Cost Tier UI")]
    [SerializeField] private UnityEngine.UI.Button[] costTierButtons;
    [SerializeField] private Color pushedInColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Vector2 starSize = new Vector2(0.5f, 0.5f);
    
    // Star references - two groups of 3 stars each
    private List<UnityEngine.UI.Image> tableStars = new List<UnityEngine.UI.Image>();
    private List<UnityEngine.UI.Image> hesapMakinesiStars = new List<UnityEngine.UI.Image>();
    
    // Private variables
    private Vector3 startingPosition;
    private Vector3 startingPositionForAnimation;
    private bool isMoving = false;
    private bool isAtReachPoint = false;
    private Sequence moveSequence;
    private Transform reachPoint; // Retrieved from ScreenEdgePositionAdjuster
    private ScreenEdgePositionAdjuster screenEdgeAdjuster;
    
    // Coin tracking
    private GameObject currentCoin;
    private Vector3 coinStartPosition;
    private bool hasMovedToReachPoint = false;
    
    // Animation tracking
    private float animationTimer = 0f;
    private int currentAnimationFrame = 0;
    private int totalAnimationFrames = 0;
    
    // Cost mode for power draw tier selection (1 = Tier1 boosted, 2 = Tier2 boosted, 3 = Tier3 boosted)
    private int selectedCostMode = 1;
    
    // Token data structure - using KeseController's TokenData
    // [System.Serializable]
    // public class TokenData
    // {
    //     public int value;
    //     public int count;
    //     
    //     public TokenData(int value, int count)
    //     {
    //         this.value = value;
    //         this.count = count;
    //     }
    // }
    
    void Start()
    {
        
        
        // Get reach points from ScreenEdgePositionAdjuster (centralized source)
        screenEdgeAdjuster = FindObjectOfType<ScreenEdgePositionAdjuster>();
        if (screenEdgeAdjuster != null)
        {
            
            
            reachPoint = screenEdgeAdjuster.GetReachPointTransform("HesapMakinesi");
            if (reachPoint == null)
            {
                
            }
            else
            {
                
            }

            // Get the return/starting position from the outside reach point
            
            startingPosition = screenEdgeAdjuster.GetOutsideReachPointPosition("HesapMakinesi");
            
            
            
            if (startingPosition == Vector3.zero)
            {
                
                startingPosition = transform.position;
                
            }
        }
        else
        {
            
            startingPosition = transform.position;
            
        }
        
        
        // Store the starting position for animation purposes
        startingPositionForAnimation = startingPosition;
        
        
        
        // Find the coin in the scene
        FindCoinInScene();
        
        // Store coin start position
        if (currentCoin != null)
        {
            coinStartPosition = currentCoin.transform.position;
        }
        
        // Initialize UI with default cost mode 1
        UpdateModeDisplays();
        UpdateButtonVisuals();
        
        // Initialize coin displays with 0 gold at game start
        UpdateCoinAmountDisplays(0);
    }
    
    void Update()
    {
        // Update idle movement animation
        UpdateIdleMovement();
    }
    
    /// <summary>
    /// Updates the idle movement animation (jitter) around the target position
    /// </summary>
    private void UpdateIdleMovement()
    {
        // Only jitter when at reach point and not currently sliding/moving
        if (isAtReachPoint && !isMoving && idleFrameRate > 0)
        {
            animationTimer += Time.deltaTime;
            float frameInterval = 1f / idleFrameRate;
            
            int newFrame = Mathf.FloorToInt(animationTimer / frameInterval);
            
            if (newFrame != currentAnimationFrame)
            {
                currentAnimationFrame = newFrame;
                
                float randomOffsetX = Random.Range(-moveAmplitudeX, moveAmplitudeX);
                float randomOffsetZ = Random.Range(-moveAmplitudeZ, moveAmplitudeZ);
                
                // Jitter around the reach point position
                Vector3 basePos = reachPoint != null ? reachPoint.position : transform.position;
                Vector3 newIdlePos = basePos + new Vector3(randomOffsetX, 0f, randomOffsetZ);
                transform.position = newIdlePos;
            }
        }
        else if (!isAtReachPoint && !isMoving)
        {
            // Stop jitter and stay at starting position when closed
            if (transform.position != startingPosition)
            {
                transform.position = startingPosition;
            }
        }
    }
    
    /// <summary>
    /// Finds the coin object in the scene
    /// </summary>
    private void FindCoinInScene()
    {
        // Look for objects with "Coin" in their name or tag
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("coin") || 
                (obj.tag != null && obj.tag.ToLower().Contains("coin")))
            {
                currentCoin = obj;
                //
                break;
            }
        }
        
        if (currentCoin == null)
        {
            //
        }
    }
    
    /// <summary>
    /// Called by KeseController when a quick drop is detected
    /// </summary>
    public void OnQuickDropDetected()
    {
        //
        hasMovedToReachPoint = true;
        MoveToReachPoint();

        // Also activate the menu
        ActivateMenuWithExistingAnimation();
    }
    

    
    /// <summary>
    /// Called by KeseController to force close the hesap makinesi
    /// </summary>
    public void ForceClose()
    {
        
        hasMovedToReachPoint = false;
        MoveToStartingPosition();
    }
    
    /// <summary>
    /// Public method to manually open the hesap makinesi (for testing)
    /// </summary>
    [ContextMenu("Open Hesap Makinesi")]
    public void ManualOpen()
    {
        
        hasMovedToReachPoint = true;
        MoveToReachPoint();

        // Also activate the menu
        ActivateMenuWithExistingAnimation();
    }
    
    /// <summary>
    /// Public method to manually close the hesap makinesi (for testing)
    /// </summary>
    [ContextMenu("Close Hesap Makinesi")]
    public void ManualClose()
    {
        
        hasMovedToReachPoint = false;
        MoveToStartingPosition();
    }

    /// <summary>
    /// Activates the menu in InfoBox alongside the calculator using existing animation
    /// </summary>
    private void ActivateMenuWithExistingAnimation()
    {
        MenuController menuController = FindObjectOfType<MenuController>();
        if (menuController != null)
        {
            if (SuperPowerSpawner.LocalInstance != null && (!SuperPowerSpawner.LocalInstance.isInfoBoxOpen || !SuperPowerSpawner.LocalInstance.isMenuPageOpen))
            {
                
                menuController.ShowTokenMenu();
            }
        }
    }
    
    // ===== COST MODE METHODS =====
    
    public void SetCostMode(int mode)
    {
        selectedCostMode = Mathf.Clamp(mode, 1, 3);
        
        UpdateModeDisplays();
        UpdateButtonVisuals();
        
        // Communicate cost mode to KeseController for next coin drop
        KeseController keseController = FindObjectOfType<KeseController>();
        if (keseController != null)
        {
            keseController.SetCostMode(selectedCostMode);
            
        }
    }
    
    /// <summary>
    /// Public method to select cost tier 1 (called from button onClick)
    /// </summary>
    public void SelectCostMode1() => SetCostMode(1);
    
    /// <summary>
    /// Public method to select cost tier 2 (called from button onClick)
    /// </summary>
    public void SelectCostMode2() => SetCostMode(2);
    
    /// <summary>
    /// Public method to select cost tier 3 (called from button onClick)
    /// </summary>
    public void SelectCostMode3() => SetCostMode(3);

    private void UpdateButtonVisuals()
    {
        if (costTierButtons == null || costTierButtons.Length == 0) return;

        for (int i = 0; i < costTierButtons.Length; i++)
        {
            if (costTierButtons[i] == null) continue;

            UnityEngine.UI.Image btnImage = costTierButtons[i].GetComponent<UnityEngine.UI.Image>();
            if (btnImage != null)
            {
                // Mode is 1-indexed (1, 2, 3), array is 0-indexed
                bool isSelected = (i + 1 == selectedCostMode);
                btnImage.color = isSelected ? pushedInColor : normalColor;
                
                // Scale for "pushed" look
                costTierButtons[i].transform.localScale = isSelected ? new Vector3(0.9f, 0.9f, 1f) : Vector3.one;
            }
        }
    }

    public int GetSelectedCostMode() => selectedCostMode;
    
    /// <summary>
    /// Gets a reference to a specific star in the table display (0-2)
    /// </summary>
    public UnityEngine.UI.Image GetTableStar(int index)
    {
        if (index < 0 || index >= tableStars.Count)
        {
            
            return null;
        }
        return tableStars[index];
    }
    
    /// <summary>
    /// Gets a reference to a specific star in the hesap makinesi display (0-2)
    /// </summary>
    public UnityEngine.UI.Image GetHesapMakinesiStar(int index)
    {
        if (index < 0 || index >= hesapMakinesiStars.Count)
        {
            
            return null;
        }
        return hesapMakinesiStars[index];
    }
    
    /// <summary>
    /// Gets all table stars as a list
    /// </summary>
    public List<UnityEngine.UI.Image> GetTableStars() => tableStars;
    
    /// <summary>
    /// Gets all hesap makinesi stars as a list
    /// </summary>
    public List<UnityEngine.UI.Image> GetHesapMakinesiStars() => hesapMakinesiStars;
    
    /// <summary>
    /// Updates both mode display UIs (table and calculator) to show 1, 2, or 3 stars
    /// </summary>
    private void UpdateModeDisplays()
    {
        UpdateModeDisplay(tableSelectedModeDisplay, starSize, tableStars);
        UpdateModeDisplay(hesapMakinesiSelectedModeDisplay, starSize * 2f, hesapMakinesiStars);
    }
    
    /// <summary>
    /// Updates a specific mode display container to show 3 stars with filled/empty based on cost mode
    /// </summary>
    private void UpdateModeDisplay(Transform displayContainer, Vector2 targetSize, List<UnityEngine.UI.Image> starList)
    {
        if (displayContainer == null) return;
        
        // Clear the list and rebuild references
        starList.Clear();
        
        // Always ensure exactly 3 stars exist
        if (displayContainer.childCount == 0)
        {
            if (filledStarSprite == null || emptyStarSprite == null)
            {
                
                return;
            }
            
            for (int i = 0; i < 3; i++)
            {
                GameObject starObj = new GameObject("Star_" + (i + 1), typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.LayoutElement));
                starObj.transform.SetParent(displayContainer, false);
                
                UnityEngine.UI.Image img = starObj.GetComponent<UnityEngine.UI.Image>();
                img.preserveAspect = true;
                img.raycastTarget = false;
                
                RectTransform rt = starObj.GetComponent<RectTransform>();
                rt.sizeDelta = targetSize;
                
                UnityEngine.UI.LayoutElement le = starObj.GetComponent<UnityEngine.UI.LayoutElement>();
                le.preferredWidth = targetSize.x;
                le.preferredHeight = targetSize.y;
                
                starList.Add(img);
            }
        }
        else
        {
            // Rebuild list from existing children
            for (int i = 0; i < displayContainer.childCount; i++)
            {
                Transform child = displayContainer.GetChild(i);
                UnityEngine.UI.Image img = child.GetComponent<UnityEngine.UI.Image>();
                if (img != null) 
                {
                    starList.Add(img);
                }
            }
        }
        
        if (starList.Count != 3)
        {
            
            return;
        }
        
        // Set first N stars to filled (based on selectedCostMode), rest to empty
        // Ensure ALL stars are always active and visible
        for (int i = 0; i < 3; i++)
        {
            // Always activate the star
            starList[i].gameObject.SetActive(true);
            
            // Set sprite based on cost mode
            if (i < selectedCostMode)
            {
                starList[i].sprite = filledStarSprite;
            }
            else
            {
                starList[i].sprite = emptyStarSprite;
            }
            
            // Ensure size is set
            RectTransform rt = starList[i].GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = targetSize;
            }
        }
        
        
    }
    
    /// <summary>
    /// Updates both coin amount displays with current player gold
    /// </summary>
    public void UpdateCoinAmountDisplays(int goldAmount)
    {
        if (tableCoinAmountDisplay != null)
        {
            tableCoinAmountDisplay.text = goldAmount.ToString();
        }
        
        if (hesapMakinesiCoinAmountDisplay != null)
        {
            hesapMakinesiCoinAmountDisplay.text = goldAmount.ToString();
        }
        
        
    }

    
    /// <summary>
    /// Moves the hesap makinesi from starting position to reach point
    /// </summary>
    public void MoveToReachPoint()
    {
        if (isMoving || reachPoint == null)
        {
            
            return;
        }
        
        
        
        // Kill any existing movement sequence
        if (moveSequence != null)
        {
            
            moveSequence.Kill();
        }
        
        isMoving = true;
        isAtReachPoint = true;
        animationTimer = 0f; // Reset animation timer
        currentAnimationFrame = 0;
        
        
        
        // Create movement sequence
        moveSequence = DOTween.Sequence();
        moveSequence.Append(transform.DOMove(reachPoint.position, moveSpeed).SetEase(moveEase));
        moveSequence.OnComplete(() => {
            isMoving = false;
            
        });
    }
    
    /// <summary>
    /// Moves the hesap makinesi from reach point back to starting position
    /// </summary>
    public void MoveToStartingPosition()
    {
        if (isMoving)
        {
            
            return;
        }
        
        
        
        // Kill any existing movement sequence
        if (moveSequence != null)
        {
            
            moveSequence.Kill();
        }
        
        isMoving = true;
        isAtReachPoint = false;
        animationTimer = 0f; // Reset animation timer
        currentAnimationFrame = 0;
        
        
        
        // Create movement sequence
        moveSequence = DOTween.Sequence();
        moveSequence.Append(transform.DOMove(startingPosition, moveSpeed).SetEase(moveEase));
        moveSequence.OnComplete(() => {
            isMoving = false;
            
        });
    }
    
    /// <summary>
    /// Public method to check if hesap makinesi is at reach point
    /// </summary>
    public bool IsAtReachPoint()
    {
        return isAtReachPoint;
    }
    
    /// <summary>
    /// Public method to check if hesap makinesi is moving
    /// </summary>
    public bool IsMoving()
    {
        return isMoving;
    }
    
    /// <summary>
    /// Public method to manually trigger movement to reach point
    /// </summary>
    public void TriggerMoveToReachPoint()
    {
        MoveToReachPoint();
    }
    
    /// <summary>
    /// Public method to manually trigger movement to starting position
    /// </summary>
    public void TriggerMoveToStartingPosition()
    {
        MoveToStartingPosition();
    }
    
    void OnDestroy()
    {
        // Clean up DOTween sequences
        if (moveSequence != null)
            moveSequence.Kill();
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw the reach point in the scene view
        if (reachPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(reachPoint.position, 0.5f);
            
            // Draw line from current position to reach point
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, reachPoint.position);
        }
    }
}

