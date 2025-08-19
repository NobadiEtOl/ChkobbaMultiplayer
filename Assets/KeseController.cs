using UnityEngine;
using UnityEngine.UI;
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
    [SerializeField] private Transform keseReachPoint; // The position where kese appears when active
    [SerializeField] private float keseMoveSpeed = 2f;
    [SerializeField] private Ease keseMoveEase = Ease.OutQuad;

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
    
    // Token data structure (same as in HesapMakinesiController)
    [System.Serializable]
    public class TokenData
    {
        public int value;
        public int count;
        
        public TokenData(int value, int count)
        {
            this.value = value;
            this.count = count;
        }
    }
    
    // Coin token data
    private List<TokenData> coinTokenData = new List<TokenData>();

    void Start()
    {
        img = GetComponent<Image>();
        currentFrame = 0;
        timer = 0f;

        if (rightHandObject != null)
        {
            handOriginalScale = rightHandObject.transform.localScale;
            handOriginalRotation = rightHandObject.transform.localRotation;
        }

        // Store kese starting position (like infobox and hesap makinesi)
        keseStartingPosition = transform.position;
        
        // Ensure kese starts at starting position
        transform.position = keseStartingPosition;
        isKeseAtReachPoint = false;

        // Spawn the first coin (only once)
        SpawnCoin();
    }

    void Update()
    {
        // Don't play idle animation if page change is playing or if it's disabled
        if (isPlayingPageChange || !isIdleAnimationEnabled) return;

        PlayIdleAnimation_Internal();

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#elif UNITY_ANDROID || UNITY_IOS
        HandleTouchInput();
#endif

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
                Debug.Log("[KeseController] Hand reached coin reach point, now following coin.");
            }
            else
            {
                rightHandObject.transform.position = Vector3.MoveTowards(handPos, target, step);
            }
        }
        else if ((isDraggingCoin || shouldHandFollowCoin) && currentCoin != null && rightHandObject != null && coinReachPoint != null)
        {
            // --- DEBUG: Log hand/coin positions ---
            Debug.Log($"[KeseController] Coin at {currentCoin.transform.position}, Hand at {rightHandObject.transform.position}, Reach at {coinReachPoint.position}, HandStart at {rightHandStartPoint.position}");

            // Check if hand is at its starting position (within a small threshold)
            float handToStartDist = Vector3.Distance(rightHandObject.transform.position, rightHandStartPoint.position);
            if (handToStartDist < 0.01f)
            {
                // If hand is at start, keep it at start instead of following the coin
                rightHandObject.transform.position = rightHandStartPoint.position;
                Debug.Log("[KeseController] Hand is at starting position, not following coin.");
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
            else if (IsPointerOverCalculatorButton(screenPos))
            {
                // Let the button handle its own click event
                // Don't close the calculator
            }
            else if (!IsPointerOverHesapMakinesi(screenPos))
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
                else if (IsPointerOverCalculatorButton(screenPos))
                {
                    // Let the button handle its own click event
                    // Don't close the calculator
                }
                else if (!IsPointerOverHesapMakinesi(screenPos))
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
    /// Checks if the pointer is over the coin using a physics raycast.
    /// </summary>
    private bool IsPointerOverCoin(Vector3 screenPos)
    {
        if (currentCoin == null) return false;
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
                Debug.Log($"[KeseController] Coin held for {holdTime:F2} seconds, showing kese");
                MoveKeseToReachPoint();
            }
        }
    }
    
    /// <summary>
    /// Called when something other than the coin is clicked
    /// </summary>
    private void OnOtherClickDetected()
    {
        if (hesapMakinesiController != null)
        {
            Debug.Log("[KeseController] Something else was clicked, force closing hesap makinesi");
            hesapMakinesiController.ForceClose();
        }
        
        // Also close kese when something else is clicked
        if (isKeseAtReachPoint)
        {
            Debug.Log("[KeseController] Something else was clicked, closing kese");
            MoveKeseToStartingPosition();
        }
    }
    
    /// <summary>
    /// Checks if the pointer is over the hesap makinesi
    /// </summary>
    private bool IsPointerOverHesapMakinesi(Vector3 screenPos)
    {
        if (hesapMakinesiController == null) return false;
        
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            // Check if the hit object is the hesap makinesi
            if (hit.collider.gameObject == hesapMakinesiController.gameObject)
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// Checks if the pointer is over any calculator button
    /// </summary>
    private bool IsPointerOverCalculatorButton(Vector3 screenPos)
    {
        if (hesapMakinesiController == null) return false;
        
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        
        foreach (RaycastHit hit in hits)
        {
            // Check if the hit object is a calculator button
            if (hesapMakinesiController.IsPointerOverCalculatorButton(hit.collider.gameObject))
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
        Debug.Log("[KeseController] Coin touch started, timer begins for hesap makinesi and kese");
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
            
            Debug.Log($"[KeseController] Coin dropped after {holdTime:F2} seconds");
            
            // If coin was held for a short time, notify hesap makinesi
            if (holdTime <= quickDropTimeThreshold && hesapMakinesiController != null)
            {
                Debug.Log($"[KeseController] Quick drop detected! Notifying hesap makinesi");
                hesapMakinesiController.OnQuickDropDetected();
            }
        }

        // Calculate distance to the kese's current position (not the fixed destination point)
        Vector3 keseCurrentPosition = isKeseAtReachPoint ? keseReachPoint.position : coinDestinationPoint.position;
        float distToDest = Vector3.Distance(currentCoin.transform.position, keseCurrentPosition);
        
        Debug.Log($"[KeseController] Coin drop check - Distance: {distToDest:F2}, AcceptRadius: {acceptRadius}, KeseAtReach: {isKeseAtReachPoint}, TokenCount: {coinTokenData.Count}");

        if (distToDest <= acceptRadius && coinTokenData.Count > 0)
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
                Debug.Log("[KeseController] Coin dropped outside accept radius, closing kese");
                MoveKeseToStartingPosition();
            }
        }
    }

    /// <summary>
    /// Handles coin acceptance and hand return after a successful drop.
    /// </summary>
    private void AcceptCoin()
    {
        if (coinMoveSequence != null) coinMoveSequence.Kill();
        if (handMoveSequence != null) handMoveSequence.Kill();

        // --- Pass spawn origin and scale to SuperPowerSpawner ---
        if (SuperPowerSpawner.LocalInstance != null)
        {
            Vector3 spawnOrigin = isKeseAtReachPoint ? keseReachPoint.position : coinDestinationPoint.position;
            float spawnScale = 1.5f; // Or any "big" scale you want
            
            // Use token data if available, otherwise use default coinAmount
            int totalTokens = GetTotalTokenCount();
            Debug.Log($"[KeseController] AcceptCoin: Spawning tokens at {spawnOrigin} with scale {spawnScale}, totalTokens={totalTokens}");
            
            if (coinTokenData.Count > 0)
            {
                // Spawn tokens based on calculator data
                SpawnTokensFromCalculatorData(spawnOrigin, spawnScale);
            }
            else
            {
                // Use default behavior
                SuperPowerSpawner.LocalInstance.ReadyToSpawnSuperPowers(1, spawnOrigin, spawnScale, coinAmount);
            }
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

        Debug.Log("[KeseController] ReturnCoin: Coin returning to start, hand will approach reach point then follow.");

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
            Debug.Log("[KeseController] Acceptance animation finished, closing kese");
            MoveKeseToStartingPosition();
        }
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

    /// <summary>
    /// Receives token data from HesapMakinesiController
    /// </summary>
    public void SetCoinTokenData(List<TokenData> tokens)
    {
        coinTokenData.Clear();
        coinTokenData.AddRange(tokens);
        
        Debug.Log($"[KeseController] Received token data: {tokens.Count} token types");
        foreach (var token in tokens)
        {
            Debug.Log($"[KeseController] Token: {token.count}x {token.value} value");
        }
    }
    
    /// <summary>
    /// Gets the total number of tokens from calculator data
    /// </summary>
    private int GetTotalTokenCount()
    {
        int total = 0;
        foreach (var token in coinTokenData)
        {
            total += token.count;
        }
        return total;
    }
    
    /// <summary>
    /// Spawns tokens based on calculator data
    /// </summary>
    private void SpawnTokensFromCalculatorData(Vector3 spawnOrigin, float spawnScale)
    {
        if (SuperPowerSpawner.LocalInstance == null) return;
        
        Debug.Log($"[KeseController] Spawning tokens from calculator data");
        
        // Calculate total cost
        int totalCost = 0;
        foreach (var token in coinTokenData)
        {
            totalCost += token.value * token.count;
        }
        
        // Check if player has enough gold
        if (SuperPowerSpawner.LocalInstance.HasEnoughGold(totalCost))
        {
            // Spend the gold
            SuperPowerSpawner.LocalInstance.SpendGold(totalCost);
            
            // Spawn the tokens
            foreach (var token in coinTokenData)
            {
                for (int i = 0; i < token.count; i++)
                {
                    // Spawn each token with its specific value
                    SuperPowerSpawner.LocalInstance.ReadyToSpawnSuperPowers(1, spawnOrigin, spawnScale, token.value);
                }
            }
            
            Debug.Log($"[KeseController] Successfully spent {totalCost} gold and spawned tokens");
        }
        else
        {
            // Not enough gold - return coin to start
            Debug.Log($"[KeseController] Insufficient gold! Need {totalCost}, have {SuperPowerSpawner.LocalInstance.GetCurrentGold()}");
            ReturnCoin();
        }
        
        // Clear token data after spawning
        //coinTokenData.Clear();
    }
    
    /// <summary>
    /// Clears the coin token data (called when coin is reset)
    /// </summary>
    public void ClearCoinTokenData()
    {
        coinTokenData.Clear();
        Debug.Log("[KeseController] Cleared coin token data");
    }
    
    // ===== KESE MOVEMENT METHODS (like infobox and hesap makinesi) =====
    
    /// <summary>
    /// Moves the kese from starting position to reach point
    /// </summary>
    public void MoveKeseToReachPoint()
    {
        if (isKeseMoving || keseReachPoint == null) return;
        
        Debug.Log("[KeseController] Moving kese to reach point");
        
        // Kill any existing movement sequence
        if (keseMoveSequence != null)
            keseMoveSequence.Kill();
        
        isKeseMoving = true;
        isKeseAtReachPoint = true;
        
        // Create movement sequence
        keseMoveSequence = DOTween.Sequence();
        keseMoveSequence.Append(transform.DOMove(keseReachPoint.position, keseMoveSpeed).SetEase(keseMoveEase));
        keseMoveSequence.OnComplete(() => {
            isKeseMoving = false;
            Debug.Log("[KeseController] Kese reached target position");
            
            // Start pouch idle animation when kese appears
            if (pouchImage != null && pouchIdleFrames.Length > 0)
            {
                if (pouchAnimCoroutine != null) StopCoroutine(pouchAnimCoroutine);
                pouchAnimCoroutine = StartCoroutine(PlayPouchIdleAnimation());
            }
        });
    }
    
    /// <summary>
    /// Moves the kese from reach point back to starting position
    /// </summary>
    public void MoveKeseToStartingPosition()
    {
        if (isKeseMoving) return;
        
        Debug.Log("[KeseController] Moving kese to starting position");
        
        // Kill any existing movement sequence
        if (keseMoveSequence != null)
            keseMoveSequence.Kill();
        
        // Stop pouch animation when kese closes
        if (pouchAnimCoroutine != null)
        {
            StopCoroutine(pouchAnimCoroutine);
            pouchAnimCoroutine = null;
        }
        
        isKeseMoving = true;
        isKeseAtReachPoint = false;
        
        // Create movement sequence
        keseMoveSequence = DOTween.Sequence();
        keseMoveSequence.Append(transform.DOMove(keseStartingPosition, keseMoveSpeed).SetEase(keseMoveEase));
        keseMoveSequence.OnComplete(() => {
            isKeseMoving = false;
            Debug.Log("[KeseController] Kese returned to starting position");
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