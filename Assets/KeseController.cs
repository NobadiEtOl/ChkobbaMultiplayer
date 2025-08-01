using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

/// <summary>
/// Controls the coin drag, hand follow, and pouch animation logic.
/// The right hand always follows the coin's reach point while dragging or after a failed drop,
/// and only returns to its start position after a successful drop or reset.
/// </summary>
public class KeseController : MonoBehaviour
{
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

    // For pouch animation
    private int pouchCurrentFrame = 0;
    private float pouchTimer = 0f;
    private bool isPouchAccepting = false;
    private Coroutine pouchAnimCoroutine;

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
            // Snap to reach point (follow exactly)
            rightHandObject.transform.position = coinReachPoint.position;
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

        float distToDest = Vector3.Distance(currentCoin.transform.position, coinDestinationPoint.position);

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
        }
    }

    /// <summary>
    /// Handles coin acceptance and hand return after a successful drop.
    /// </summary>
    private void AcceptCoin()
    {
        if (coinMoveSequence != null) coinMoveSequence.Kill();
        if (handMoveSequence != null) handMoveSequence.Kill();

        // Move coin and hand to destination together
        coinMoveSequence = DOTween.Sequence();
        coinMoveSequence.Join(currentCoin.transform.DOMove(coinDestinationPoint.position, coinMoveSpeed).SetEase(moveEase));
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

        yield return new WaitForSeconds(0.25f); // Optional delay before fading in

        if (coinImg != null)
        {
            coinImg.color = new Color(1, 1, 1, 0);
            Sequence coinFadeSequence = DOTween.Sequence();
            coinFadeSequence.Append(coinImg.DOFade(1f, coinFadeInDuration));
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

    private void OnDrawGizmosSelected()
    {
        if (coinDestinationPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(coinDestinationPoint.position, acceptRadius);
        }
    }
}