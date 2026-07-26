using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Controls the coin drag, hand follow, and pouch animation logic.
/// The right hand always follows the coin's reach point while dragging or after a failed drop,
/// and only returns to its start position after a successful drop or reset.
/// </summary>
public class KeseController : MonoBehaviour
{
    [Header("Coin Settings")]
    [SerializeField] private int coinAmount = 5; // Set this in the Inspector

    [Header("Idle Animation")]
    public Sprite[] animationFrames;
    public float frameRate = 10f;

    [Header("Jitter Animation")]
    [SerializeField] private float moveAmplitudeX = 0.5f;
    [SerializeField] private float moveAmplitudeZ = 0.3f;
    [SerializeField] private float jitterFrameRate = 10f;
    private float jitterTimer = 0f;
    private int currentJitterFrame = 0;

    [Header("Page Change Animation")]
    public Sprite[] pageChangeFrames;
    public float pageChangeFrameRate = 15f;

    [Header("Coin Drag & Buy Superpower")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private Transform coinStartPoint;
    [SerializeField] private Transform coinReachPoint; // This should be a child of the coin!
    [SerializeField] private Transform coinDestinationPoint;
    [SerializeField] private Transform rightHandStartPoint;
    [SerializeField] private Transform rightHandReachPoint; // Not used now, but kept for future use
    [SerializeField] private GameObject rightHandObject;
    [SerializeField] private SpriteRenderer pouchImage;
    [SerializeField] private Sprite[] pouchIdleFrames;
    [SerializeField] private Sprite[] pouchAcceptFrames;
    [SerializeField] private float pouchIdleFrameRate = 10f;
    [SerializeField] private float pouchAcceptFrameRate = 15f;
    [SerializeField] private float coinMoveSpeed = 1.5f;
    [SerializeField] private float handMoveSpeed = 1.5f;
    [SerializeField] private float coinFadeInDuration = 0.3f;
    [SerializeField] private Sprite[] coinFlipFrames;
    [SerializeField] private float coinFlipFrameRate = 20f;
    [SerializeField] private float acceptRadius = 100f;
    [SerializeField] private Ease moveEase = Ease.OutQuad;
    
    [Header("Kese Movement")]
    [SerializeField] private float keseMoveSpeed = 0.25f;
    [SerializeField] private Ease keseMoveEase = Ease.OutQuad;
    private Transform keseReachPoint; // Retrieved from ScreenEdgePositionAdjuster

    [SerializeField] private GameObject currentCoin;
    private bool isDraggingCoin = false;
    private Vector3 coinDragOffset;
    private Sequence handMoveSequence;
    private Sequence coinMoveSequence;
    private Sequence coinFadeSequence;
    private Sequence coinFlipSequence;
    private Vector3 coinOriginalScale;
    private Quaternion coinOriginalRotation;
    private Vector3 handOriginalScale;
    private Quaternion handOriginalRotation;
    
    // Timer tracking for hesap makinesi
    private float coinTouchStartTime = 0f;
    private bool coinIsBeingHeld = false;

    // For pouch animation
    private int pouchCurrentFrame = 0;
    private float pouchTimer = 0f;
    private bool isPouchAccepting = false;
    private Coroutine pouchAnimCoroutine;
    
    // Kese movement tracking (like infobox and hesap makinesi)
    private Vector3 keseStartingPosition;
    private bool isKeseMoving = false;
    private bool isKeseAtReachPoint = false;
    private Sequence keseMoveSequence;

    // For idle/page change
    private Image img;
    private int currentFrame;
    private float timer;
    private bool isPlayingPageChange = false;
    private bool isIdleAnimationEnabled = true;

    // For drag-follow
    private Vector3 dragTargetPosition;

    // Controls if the hand should keep following the coin's reach point after a failed drop
    private bool shouldHandFollowCoin = false;

    // Hand approach logic
    private bool handApproachingReachPoint = false;
    private float handCatchThreshold = 0.01f; // How close is "caught"
    [SerializeField] private float handApproachSpeed = 8f; // Units per second (tweak as needed)

    [SerializeField] private Animator smokeAnimator;
    
    [Header("Hesap Makinesi Integration")]
    [SerializeField] private HesapMakinesiController hesapMakinesiController;
    [SerializeField] private float quickDropTimeThreshold = 0.5f; // Time limit for "quick drop"
    


    void Start()
    {
        // Get reach points from ScreenEdgePositionAdjuster (centralized source)
        ScreenEdgePositionAdjuster screenEdgeAdjuster = FindObjectOfType<ScreenEdgePositionAdjuster>();
        if (screenEdgeAdjuster != null)
        {
            keseReachPoint = screenEdgeAdjuster.GetReachPointTransform("Kese");
            if (keseReachPoint == null)
            {
                
            }

            // Get the return/starting position from the outside reach point
            keseStartingPosition = screenEdgeAdjuster.GetOutsideReachPointPosition("Kese");
            if (keseStartingPosition == Vector3.zero)
            {
                
                keseStartingPosition = transform.position;
            }
        }
        else
        {
            
            keseStartingPosition = transform.position;
        }
        
        img = GetComponent<Image>();
        currentFrame = 0;
        timer = 0f;

        if (rightHandObject != null)
        {
            handOriginalScale = rightHandObject.transform.localScale;
            handOriginalRotation = rightHandObject.transform.localRotation;
        }
        
        // Ensure kese starts at starting position
        transform.position = keseStartingPosition;
        isKeseAtReachPoint = false;

        // Spawn the first coin (only once)
        SpawnCoin();
    }

    void Update()
    {
        // Update jitter animation
        UpdateJitterAnimation();

        // Don't play idle animation if page change is playing or if it's disabled
        if (isPlayingPageChange || !isIdleAnimationEnabled) return;

        PlayIdleAnimation_Internal();

        // Use mouse input for WebGL
        HandleMouseInput();

        // Check for hesap makinesi and kese timer logic
        CheckHesapMakinesiAndKeseTimer();

        // --- DRAG FOLLOW LOGIC & HAND APPROACH ---
        if (handApproachingReachPoint && rightHandObject != null && coinReachPoint != null)
        {
            // Move hand towards the reach point at a fixed speed
            Vector3 handPos = rightHandObject.transform.position;
            Vector3 target = coinReachPoint.position;
            float step = handApproachSpeed * Time.deltaTime;
            if (Vector3.Distance(handPos, target) <= handCatchThreshold)
            {
                // Snap and start following
                rightHandObject.transform.position = target;
                handApproachingReachPoint = false;
                shouldHandFollowCoin = true;
                
            }
            else
            {
                rightHandObject.transform.position = Vector3.MoveTowards(handPos, target, step);
            }
        }
        else if ((isDraggingCoin || shouldHandFollowCoin) && currentCoin != null && rightHandObject != null && coinReachPoint != null)
        {
            // --- DEBUG: Log hand/coin positions ---
            

            // Check if hand is at its starting position (within a small threshold)
            float handToStartDist = Vector3.Distance(rightHandObject.transform.position, rightHandStartPoint.position);
            if (handToStartDist < 0.01f)
            {
                // If hand is at start, keep it at start instead of following the coin
                rightHandObject.transform.position = rightHandStartPoint.position;
                
            }
            else
            {
                // Snap to reach point (follow exactly)
                rightHandObject.transform.position = coinReachPoint.position;
            }
        }

        // Coin drag follow
        if (isDraggingCoin && currentCoin != null)
        {
            currentCoin.transform.position = dragTargetPosition;
        }
    }

    /// <summary>
    /// Updates the jitter movement animation around the reach point
    /// </summary>
    private void UpdateJitterAnimation()
    {
        // Only jitter when at reach point and not currently sliding/moving
        if (isKeseAtReachPoint && !isKeseMoving && jitterFrameRate > 0)
        {
            jitterTimer += Time.deltaTime;
            float frameInterval = 1f / jitterFrameRate;
            int newFrame = Mathf.FloorToInt(jitterTimer / frameInterval);
            
            if (newFrame != currentJitterFrame)
            {
                currentJitterFrame = newFrame;
                float randomOffsetX = Random.Range(-moveAmplitudeX, moveAmplitudeX);
                float randomOffsetZ = Random.Range(-moveAmplitudeZ, moveAmplitudeZ);
                
                // Jitter around the kese reach point position
                Vector3 basePos = keseReachPoint != null ? keseReachPoint.position : transform.position;
                transform.position = basePos + new Vector3(randomOffsetX, 0f, randomOffsetZ);
            }
        }
        else if (!isKeseAtReachPoint && !isKeseMoving)
        {
            // Stop jitter and stay exactly at starting position when closed
            if (transform.position != keseStartingPosition)
            {
                transform.position = keseStartingPosition;
            }
        }
    }

    /// <summary>
    /// Handles idle animation frame switching.
    /// </summary>
    private void PlayIdleAnimation_Internal()
    {
        if (animationFrames == null || animationFrames.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= 1f / frameRate)
        {
            currentFrame = (currentFrame + 1) % animationFrames.Length;
            if (img != null)
                img.sprite = animationFrames[currentFrame];
            timer = 0f;
        }
    }

    /// <summary>
    /// Starts the page change animation.
    /// </summary>
    private void PlayPageChangeAnimation()
    {
        if (pageChangeFrames == null || pageChangeFrames.Length == 0)
            return;

        StartCoroutine(PlayPageChangeCoroutine());
    }

    private IEnumerator PlayPageChangeCoroutine()
    {
        isPlayingPageChange = true;

        for (int i = 0; i < pageChangeFrames.Length; i++)
        {
            if (img != null)
                img.sprite = pageChangeFrames[i];
            yield return new WaitForSeconds(1f / pageChangeFrameRate);
        }

        isPlayingPageChange = false;
        currentFrame = 0;
        timer = 0f;
    }

    // --- INPUT HANDLING ---

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 screenPos = Input.mousePosition;
            if (IsPointerOverCoin(screenPos))
                OnCoinTouchDown(screenPos);
            else if (!IsPointerOverHesapMakinesi(screenPos) && !IsPointerOverInfoBox(screenPos))
                OnOtherClickDetected();
        }
        else if (Input.GetMouseButton(0))
        {
            if (isDraggingCoin)
                OnCoinDrag(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (isDraggingCoin)
                OnCoinTouchUp();
        }
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 0) return;
        Touch touch = Input.GetTouch(0);
        Vector3 screenPos = touch.position;

        switch (touch.phase)
        {
            case TouchPhase.Began:
                if (IsPointerOverCoin(screenPos))
                    OnCoinTouchDown(screenPos);
                else if (!IsPointerOverHesapMakinesi(screenPos) && !IsPointerOverInfoBox(screenPos))
                    OnOtherClickDetected();
                break;
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                if (isDraggingCoin)
                    OnCoinDrag(screenPos);
                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (isDraggingCoin)
                    OnCoinTouchUp();
                break;
        }
    }

    /// <summary>
    /// Checks if the pointer is over the InfoBox or any of its menu/token elements.
    /// Only returns true if we're over InfoBox/Menu UI specifically, NOT other UI elements.
    /// </summary>
    private bool IsPointerOverInfoBox(Vector3 screenPos)
    {
        // First, use 3D raycasts to check for physical elements
        if (Camera.main != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            RaycastHit[] hits = Physics.RaycastAll(ray);
            
            GameObject infoBoxBg = GameObject.Find("InfoBoxBackGroundPanel");
            GameObject menuBg = GameObject.Find("DisplayBackgroundPanel");

            foreach (RaycastHit hit in hits)
            {
                GameObject hitObj = hit.collider.gameObject;
                
                // Check if we hit a Token (Super Power tokens)
                if (hitObj.CompareTag("Token") || hitObj.name.Contains("Token"))
                {
                    
                    return true;
                }

                // Check if we hit the top-level InfoBoxBackGroundPanel or any of its descendants
                if (infoBoxBg != null && (hitObj == infoBoxBg || hitObj.transform.IsChildOf(infoBoxBg.transform)))
                {
                    
                    return true;
                }

                // Check if we hit the menu background panel or any of its descendants
                if (menuBg != null && (hitObj == menuBg || hitObj.transform.IsChildOf(menuBg.transform)))
                {
                    
                    return true;
                }

                // Check for other potential scroll display elements by name
                if (hitObj.name == "DisplayBackgroundPanel" || hitObj.name == "TokenDisplayArea" || hitObj.name == "ScrollContainer")
                {
                    
                    return true;
                }
            }
        }

        // Second, check if we're over InfoBox/Menu UI canvas elements using EventSystem
        // But ONLY check canvases that are part of the InfoBox or Menu
        if (EventSystem.current != null)
        {
            GameObject infoBoxBg = GameObject.Find("InfoBoxBackGroundPanel");
            
            // If we found the InfoBox background, check if click is over its canvas UI
            if (infoBoxBg != null)
            {
                Canvas infoBoxCanvas = infoBoxBg.GetComponentInChildren<Canvas>();
                if (infoBoxCanvas != null)
                {
                    GraphicRaycaster raycaster = infoBoxCanvas.GetComponent<GraphicRaycaster>();
                    if (raycaster != null)
                    {
                        var pointerData = new PointerEventData(EventSystem.current);
                        pointerData.position = screenPos;
                        var results = new List<RaycastResult>();
                        raycaster.Raycast(pointerData, results);
                        
                        if (results.Count > 0)
                        {
                            
                            return true;
                        }
                    }
                }
            }
        }
        
        return false;
    }

    /// <summary>
    /// Checks if the pointer is over the coin using a physics raycast.
    /// </summary>
    private bool IsPointerOverCoin(Vector3 screenPos)
    {
        if (currentCoin == null) return false;
        if (Camera.main == null) return false;
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider != null && hit.collider.gameObject == currentCoin)
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// Checks for hesap makinesi and kese timer logic
    /// </summary>
    private void CheckHesapMakinesiAndKeseTimer()
    {
        if (coinIsBeingHeld)
        {
            float holdTime = Time.time - coinTouchStartTime;
            
            // Show kese when coin is held for more than threshold
            if (holdTime > quickDropTimeThreshold && !isKeseAtReachPoint && !isKeseMoving)
            {
                
                
                // Close hesap makinesi first if it's open
                if (hesapMakinesiController != null && hesapMakinesiController.IsAtReachPoint())
                {
                    
                    hesapMakinesiController.ForceClose();
                    
                    // Wait a short moment for hesap makinesi to start closing, then show kese
                    StartCoroutine(ShowKeseAfterHesapMakinesiCloses());
                }
                else
                {
                    // Hesap makinesi is not open, show kese immediately
                    MoveKeseToReachPoint();
                }
            }
        }
    }
    
    /// <summary>
    /// Called when something other than the coin, hesap makinesi, or infobox is clicked
    /// Closes both hesap makinesi and infobox while keeping them open if they were clicked
    /// </summary>
    private void OnOtherClickDetected()
    {
        
        
        // Close hesap makinesi if it's open
        if (hesapMakinesiController != null && hesapMakinesiController.IsAtReachPoint())
        {
            
            hesapMakinesiController.ForceClose();
        }
        
        // Close kese when something else is clicked
        if (isKeseAtReachPoint)
        {
            
            MoveKeseToStartingPosition();
        }

        // Close InfoBox (menu) if it is currently open
        if (SuperPowerSpawner.LocalInstance != null && SuperPowerSpawner.LocalInstance.isInfoBoxOpen)
        {
            
            SuperPowerSpawner.LocalInstance.StartCoroutine(SuperPowerSpawner.LocalInstance.CloseInfoBox());
        }
    }
    
    /// <summary>
    /// Checks if the pointer is over the hesap makinesi (improved to check all colliders)
    /// </summary>
    private bool IsPointerOverHesapMakinesi(Vector3 screenPos)
    {
        if (hesapMakinesiController == null) return false;
        if (Camera.main == null) return false;
        
        // Raycast from camera to check if we hit the hesap makinesi or any of its child colliders
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        
        GameObject hesapMakinesiGameObject = hesapMakinesiController.gameObject;
        
        // Check all hits to see if any belong to the hesap makinesi or its children
        foreach (RaycastHit hit in hits)
        {
            GameObject hitObj = hit.collider.gameObject;
            
            // Direct hit on the hesap makinesi itself
            if (hitObj == hesapMakinesiGameObject)
            {
                
                return true;
            }
            
            // Hit on a child of the hesap makinesi
            if (hitObj.transform.IsChildOf(hesapMakinesiGameObject.transform))
            {
                
                return true;
            }
        }
        
        return false;
    }


    // --- COIN DRAG & BUY SUPERPOWER SYSTEM ---

    /// <summary>
    /// Spawns a new coin at the start point, or resets the existing one.
    /// </summary>
    public void SpawnCoin()
    {
        // Only instantiate if there is no coin in the scene
        if (currentCoin == null)
        {
            currentCoin = Instantiate(coinPrefab, coinStartPoint.position, coinStartPoint.rotation, coinStartPoint.parent);
            currentCoin.transform.localScale = Vector3.one * 1000; // or your intended scale
        }
        else
        {
            // Reset position and scale if coin already exists
            currentCoin.transform.position = coinStartPoint.position;
            currentCoin.transform.rotation = coinStartPoint.rotation;
            currentCoin.transform.localScale = Vector3.one * 1000; // or your intended scale
        }
        coinOriginalScale = currentCoin.transform.localScale;
        coinOriginalRotation = currentCoin.transform.localRotation;
        // Fade in if you want, or skip for instant appearance
        var coinImg = currentCoin.GetComponent<Image>();
        if (coinImg != null)
        {
            coinImg.color = new Color(1, 1, 1, 0);
            Sequence coinFadeSequence = DOTween.Sequence();
            coinFadeSequence.Append(coinImg.DOFade(1f, coinFadeInDuration));
        }
    }

    /// <summary>
    /// Called when the coin is touched/clicked.
    /// </summary>
    public void OnCoinTouchDown(Vector3 screenPos)
    {
        if (currentCoin == null) return;
        isDraggingCoin = true;
        shouldHandFollowCoin = false;
        handApproachingReachPoint = true; // Start approach
        dragTargetPosition = ScreenToWorld(screenPos);
        
        // Start timer for hesap makinesi and kese
        coinTouchStartTime = Time.time;
        coinIsBeingHeld = true;
        
    }

    /// <summary>
    /// Called while dragging the coin.
    /// </summary>
    public void OnCoinDrag(Vector3 screenPos)
    {
        if (!isDraggingCoin || currentCoin == null) return;
        dragTargetPosition = ScreenToWorld(screenPos);
    }

    /// <summary>
    /// Called when the coin is released.
    /// </summary>
    public void OnCoinTouchUp()
    {
        if (!isDraggingCoin || currentCoin == null) return;
        isDraggingCoin = false;

        // Check for quick drop for hesap makinesi
        if (coinIsBeingHeld)
        {
            float holdTime = Time.time - coinTouchStartTime;
            coinIsBeingHeld = false;
            
            
            
            // If coin was held for a short time, notify hesap makinesi
            if (holdTime <= quickDropTimeThreshold && hesapMakinesiController != null)
            {
                
                hesapMakinesiController.OnQuickDropDetected();
            }
        }

        // Calculate distance to the kese's current position (not the fixed destination point)
        Vector3 keseCurrentPosition = isKeseAtReachPoint ? keseReachPoint.position : coinDestinationPoint.position;
        float distToDest = Vector3.Distance(currentCoin.transform.position, keseCurrentPosition);
        
        

        if (distToDest <= acceptRadius)
        {
            // Accept: move coin and hand to destination, play pouch accept, reset coin
            shouldHandFollowCoin = false;
            AcceptCoin();
        }
        else
        {
            // Not close enough: animate coin and hand back to start, then stop following
            shouldHandFollowCoin = false;
            ReturnCoin();
            
            // Close kese when coin is dropped outside accept radius
            if (isKeseAtReachPoint)
            {
                
                MoveKeseToStartingPosition();
            }
        }
    }

    /// <summary>
    /// Handles coin acceptance and hand return after a successful drop.
    /// Spawns 1 power with the selected cost mode tier boost.
    /// </summary>
    private void AcceptCoin()
    {
        if (coinMoveSequence != null) coinMoveSequence.Kill();
        if (handMoveSequence != null) handMoveSequence.Kill();

        // Spawn 1 power with the selected cost mode boost
        if (SuperPowerSpawner.LocalInstance != null)
        {
            Vector3 spawnOrigin = isKeseAtReachPoint ? keseReachPoint.position : coinDestinationPoint.position;
            float spawnScale = 1.5f;
            
            
            SuperPowerSpawner.LocalInstance.ReadyToSpawnSuperPowers(1, spawnOrigin, spawnScale, selectedCostMode);
        }
        
        // Move coin and hand to destination together
        coinMoveSequence = DOTween.Sequence();
        Vector3 targetPosition = isKeseAtReachPoint ? keseReachPoint.position : coinDestinationPoint.position;
        coinMoveSequence.Join(currentCoin.transform.DOMove(targetPosition, coinMoveSpeed).SetEase(moveEase));
        coinMoveSequence.OnComplete(() =>
        {
            // Make coin invisible
            currentCoin.SetActive(false);

            // Play pouch accept animation (stop previous if running)
            if (pouchAnimCoroutine != null) StopCoroutine(pouchAnimCoroutine);
            if (pouchImage != null && pouchAcceptFrames.Length > 0)
                pouchAnimCoroutine = StartCoroutine(PlayPouchAcceptAnimation());
            

            // Move hand back to start and stop following
            if (rightHandObject != null && rightHandStartPoint != null)
                rightHandObject.transform.DOMove(rightHandStartPoint.position, handMoveSpeed).SetEase(moveEase)
                    .OnComplete(() => { shouldHandFollowCoin = false; });

            // Reset coin position and fade in
            StartCoroutine(ResetAndFadeInCoin());
            
            // Close kese after acceptance animation finishes
            StartCoroutine(CloseKeseAfterAcceptance());
        });
    }

    /// <summary>
    /// Handles coin and hand return after a failed drop.
    /// </summary>
    private void ReturnCoin()
    {
        if (coinMoveSequence != null) coinMoveSequence.Kill();
        if (handMoveSequence != null) handMoveSequence.Kill();

        // While the coin is returning, the hand should approach and then follow the coin's reach point
        handApproachingReachPoint = true;
        shouldHandFollowCoin = false;

        

        // Move coin back to start
        coinMoveSequence = DOTween.Sequence();
        coinMoveSequence.Append(currentCoin.transform.DOMove(coinStartPoint.position, coinMoveSpeed).SetEase(moveEase));
        coinMoveSequence.OnComplete(() =>
        {
            // After coin is back, animate hand to its start position and stop following
            shouldHandFollowCoin = false;
            handApproachingReachPoint = false;
            if (rightHandObject != null && rightHandStartPoint != null)
            {
                rightHandObject.transform.DOMove(rightHandStartPoint.position, handMoveSpeed).SetEase(moveEase);
            }
        });
    }

    /// <summary>
    /// Resets the coin to the start position, makes it invisible, and fades it in.
    /// </summary>
    private IEnumerator ResetAndFadeInCoin()
    {
        yield return new WaitForSeconds(0.5f); // Optional delay before resetting

        currentCoin.SetActive(true);

        var coinImg = currentCoin.GetComponent<SpriteRenderer>();
        coinImg.color = new Color(1, 1, 1, 0);

        if (smokeAnimator != null)
        {
            smokeAnimator.gameObject.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
            smokeAnimator.Play("New_SmokeEffectAnimationClip");
        }

        currentCoin.transform.position = coinStartPoint.position;
        currentCoin.transform.rotation = coinStartPoint.rotation;
        currentCoin.transform.localScale = Vector3.one * 1000; // or your intended scale

        // Don't clear token data when coin is reset - keep it until calculator changes it
        // ClearCoinTokenData();

        yield return new WaitForSeconds(0.25f); // Optional delay before fading in

        if (coinImg != null)
        {
            coinImg.color = new Color(1, 1, 1, 0);
            Sequence coinFadeSequence = DOTween.Sequence();
            coinFadeSequence.Append(coinImg.DOFade(1f, coinFadeInDuration));
        }
    }
    
    /// <summary>
    /// Closes the kese after the acceptance animation finishes
    /// </summary>
    private IEnumerator CloseKeseAfterAcceptance()
    {
        // Wait for the pouch accept animation to finish
        float pouchAcceptDuration = pouchAcceptFrames.Length / pouchAcceptFrameRate;
        yield return new WaitForSeconds(pouchAcceptDuration + 0.5f); // Add extra delay for safety
        
        // Close the kese
        if (isKeseAtReachPoint)
        {
            
            MoveKeseToStartingPosition();
        }
    }
    
    /// <summary>
    /// Shows the kese after hesap makinesi has started closing
    /// </summary>
    private IEnumerator ShowKeseAfterHesapMakinesiCloses()
    {
        // Wait a short moment for hesap makinesi to start its closing animation
        yield return new WaitForSeconds(0.1f);
        
        // Now show the kese
        MoveKeseToReachPoint();
    }

    // --- POUCH ANIMATION ---

    private IEnumerator PlayPouchAcceptAnimation()
    {
        isPouchAccepting = true;
        for (int i = 0; i < pouchAcceptFrames.Length; i++)
        {
            pouchImage.sprite = pouchAcceptFrames[i];
            yield return new WaitForSeconds(1f / pouchAcceptFrameRate);
        }
        isPouchAccepting = false;
        // Resume idle animation (stop previous if running)
        if (pouchAnimCoroutine != null) StopCoroutine(pouchAnimCoroutine);
        pouchAnimCoroutine = StartCoroutine(PlayPouchIdleAnimation());
    }

    private IEnumerator PlayPouchIdleAnimation()
    {
        while (!isPouchAccepting && pouchIdleFrames.Length > 0)
        {
            for (int i = 0; i < pouchIdleFrames.Length; i++)
            {
                pouchImage.sprite = pouchIdleFrames[i];
                yield return new WaitForSeconds(1f / pouchIdleFrameRate);
            }
        }
    }

    // --- COIN FLIP ANIMATION ---

    public void PlayCoinFlipAnimation()
    {
        if (currentCoin == null || coinFlipFrames.Length == 0) return;
        if (coinFlipSequence != null) coinFlipSequence.Kill();
        StartCoroutine(CoinFlipCoroutine());
    }

    private IEnumerator CoinFlipCoroutine()
    {
        var coinImg = currentCoin.GetComponent<Image>();
        for (int i = 0; i < coinFlipFrames.Length; i++)
        {
            coinImg.sprite = coinFlipFrames[i];
            yield return new WaitForSeconds(1f / coinFlipFrameRate);
        }
    }

    // --- UTILS ---

    /// <summary>
    /// Converts screen position to world position at the coin's Y (table) level.
    /// </summary>
    private Vector3 ScreenToWorld(Vector3 screenPos)
    {
        if (Camera.main == null) return screenPos;
        // Use the coin's current Y as the "depth" (since scene is rotated 90 deg on X)
        float y = currentCoin != null ? currentCoin.transform.position.y : 0f;
        // Get the screen Z that matches this Y in world space
        float screenZ = Camera.main.WorldToScreenPoint(new Vector3(0, y, 0)).z;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, screenZ));
        // Keep the original Y (height) so the coin stays on the table
        if (currentCoin != null)
        {
            worldPos.y = currentCoin.transform.position.y;
        }
        return worldPos;
    }

    // --- Original idle/page change controls (unchanged) ---

    public void SetIdleAnimationEnabled(bool enabled)
    {
        isIdleAnimationEnabled = enabled;
        if (!enabled && !isPlayingPageChange)
        {
            if (animationFrames.Length > 0)
            {
                img.sprite = animationFrames[0];
            }
        }
    }

    public bool IsPlayingPageChange()
    {
        return isPlayingPageChange;
    }

    public void StopAllAnimations()
    {
        StopAllCoroutines();
        isPlayingPageChange = false;
        isIdleAnimationEnabled = false;
    }

    public void ResumeIdleAnimation()
    {
        if (!isPlayingPageChange)
        {
            isIdleAnimationEnabled = true;
            currentFrame = 0;
            timer = 0f;
        }
    }

    public void TriggerPageChange()
    {
        PlayPageChangeAnimation();
    }

    private int selectedCostMode = 1;

    /// <summary>
    /// Sets the cost mode tier for the next coin drop.
    /// costMode 1/2/3 boosts draws toward that tier; Tier 4 (ZaferPuani) is never boosted.
    /// </summary>
    public void SetCostMode(int costMode)
    {
        selectedCostMode = Mathf.Clamp(costMode, 1, 3);
        
    }
    
    // ===== KESE MOVEMENT METHODS (like infobox and hesap makinesi) =====
    
    /// <summary>
    /// Moves the kese from starting position to reach point
    /// </summary>
    public void MoveKeseToReachPoint()
    {
        if (isKeseMoving || keseReachPoint == null) return;
        
        
        
        // Kill any existing movement sequence
        if (keseMoveSequence != null)
            keseMoveSequence.Kill();
        
        // Start pouch idle animation BEFORE moving
        if (pouchImage != null && pouchIdleFrames.Length > 0)
        {
            if (pouchAnimCoroutine != null) StopCoroutine(pouchAnimCoroutine);
            pouchAnimCoroutine = StartCoroutine(PlayPouchIdleAnimation());
        }
        
        isKeseMoving = true;
        isKeseAtReachPoint = true;
        
        // Create movement sequence
        keseMoveSequence = DOTween.Sequence();
        keseMoveSequence.Append(transform.DOMove(keseReachPoint.position, keseMoveSpeed).SetEase(keseMoveEase));
        keseMoveSequence.OnComplete(() => {
            isKeseMoving = false;
            
        });
    }
    
    /// <summary>
    /// Moves the kese from reach point back to starting position
    /// </summary>
    public void MoveKeseToStartingPosition()
    {
        if (isKeseMoving) return;
        
        
        
        // Kill any existing movement sequence
        if (keseMoveSequence != null)
            keseMoveSequence.Kill();
        
        isKeseMoving = true;
        isKeseAtReachPoint = false;
        
        // Create movement sequence
        keseMoveSequence = DOTween.Sequence();
        keseMoveSequence.Append(transform.DOMove(keseStartingPosition, keseMoveSpeed).SetEase(keseMoveEase));
        keseMoveSequence.OnComplete(() => {
            isKeseMoving = false;
            
            
            // Stop pouch animation when kese reaches starting position
            if (pouchAnimCoroutine != null)
            {
                StopCoroutine(pouchAnimCoroutine);
                pouchAnimCoroutine = null;
            }
        });
    }
    
    /// <summary>
    /// Public method to check if kese is at reach point
    /// </summary>
    public bool IsKeseAtReachPoint()
    {
        return isKeseAtReachPoint;
    }
    
    /// <summary>
    /// Public method to check if kese is moving
    /// </summary>
    public bool IsKeseMoving()
    {
        return isKeseMoving;
    }
    
    /// <summary>
    /// Public method to manually trigger movement to reach point (for testing)
    /// </summary>
    [ContextMenu("Move Kese to Reach Point")]
    public void TriggerMoveKeseToReachPoint()
    {
        MoveKeseToReachPoint();
    }
    
    /// <summary>
    /// Public method to manually trigger movement to starting position (for testing)
    /// </summary>
    [ContextMenu("Move Kese to Starting Position")]
    public void TriggerMoveKeseToStartingPosition()
    {
        MoveKeseToStartingPosition();
    }
    
    void OnDestroy()
    {
        // Clean up DOTween sequences
        if (keseMoveSequence != null)
            keseMoveSequence.Kill();
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw acceptance radius at the current kese position
        Vector3 currentAcceptancePosition = isKeseAtReachPoint ? keseReachPoint.position : coinDestinationPoint.position;
        if (coinDestinationPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(currentAcceptancePosition, acceptRadius);
        }
        
        // Draw the kese reach point in the scene view
        if (keseReachPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(keseReachPoint.position, 0.5f);
            
            // Draw line from current position to reach point
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, keseReachPoint.position);
        }
    }
}