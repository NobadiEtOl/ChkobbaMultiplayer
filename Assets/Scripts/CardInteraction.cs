using System;
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using System.Collections;

public class CardInteraction : MonoBehaviour
{
    private int[] cardID = new int[2];
    public static bool isOneCardSelected = false;
    public static CardInteraction currentlySelectedCard = null; // Tracks the currently selected card
    public bool isPlayable = false;
    public Vector3 originalScreenPosition; // Original position in screen space
    private Vector3 offset; // Offset between touch position and card position in screen space
    private bool isDragging = false;
    private float snapBackThreshold = 175f;
    private GameObject selectedCardIndicator;
    public event Action<string> OnCardSelected;
    public event Action<string, GameObject, int> OnCardsPlayed;
    private static GameObject activeCardIndicator = null; // Tracks the currently active card indicator
    public static Dictionary<string, CardInteraction> cardLookup = new Dictionary<string, CardInteraction>();
    // In CardInteraction.cs
    public string uniqueCardInstanceID; // e.g., a GUID
    private static Color baseCardIndicatorColor;
    // --- Card selection restriction system ---
    private static bool restrictToOwnHand = true;
    private static HashSet<string> allowedParentNames = new HashSet<string>();
    private static bool allowOwnHand = true;
    private static int allowedSelections = 0; // 0 means no limit
    private static int currentSelections = 0;
    public string activePowerEffect = "none";

    // ADD THIS PROPERTY
    public bool isAutoRotating 
    { 
        get { return autoRotateActive; } 
    }

    public int[] GetCardID()
    {
        // Return a copy of the cardID to prevent external modification
        return (int[])cardID.Clone();
    }
    
    public void SetUniqueID(string uniqueId, int[] cardID)
    {
        uniqueCardInstanceID = uniqueId;
        CardInteraction.cardLookup[uniqueCardInstanceID] = this;
        this.cardID = (int[])cardID.Clone();
        Debug.Log($"Card {gameObject.name} set with unique ID: {uniqueCardInstanceID}");
        InitializeCard();
    }

    public void SetCardID(int[] id)
    {
        cardID = (int[])id.Clone();
        gameObject.tag = cardID[0] + "_" + cardID[1];
    }

    public void SetCardValue(int value)
    {
        cardID[1] = value;
        gameObject.tag = cardID[0] + "_" + cardID[1];
    }

    void Start()
    {
        //InitializeCard();
        StopAutoRotate(); // Ensure auto-rotation is stopped at the start
    }

    void Update()
    {

    }

    private bool isRotating = false; // Flag to control rotation
    private float rotationProgress = 0f; // Tracks the progress of the rotation
    private Quaternion startRotation; // Starting rotation
    private Quaternion endRotation; // Target rotation
    private bool rotateDirection = true; // Tracks the direction of rotation (true = clockwise, false = counterclockwise)
    public GameObject yandimAnamEffectInstance;
    public GameObject yandimAnamSpriteInstance;

    private void RotateCardWithLerp()
    {
        if (!isRotating)
        {
            // Initialize the rotation
            isRotating = true;
            rotationProgress = 0f;

            // Set the start and end rotations
            startRotation = transform.rotation;
            float randomAngle = UnityEngine.Random.Range(10f, 25f); // Slight random angle
            if (!rotateDirection) randomAngle = -randomAngle; // Reverse direction if needed
            endRotation = Quaternion.Euler(90 + randomAngle / 10, 0, 0 + randomAngle / 2);
        }

        // Increment the rotation progress
        if (startRotation.eulerAngles.x == 90) rotationProgress += Time.deltaTime;
        else rotationProgress += Time.deltaTime / 7;

        // Smoothly interpolate between the start and end rotations
        transform.rotation = Quaternion.Lerp(startRotation, endRotation, rotationProgress);

        // Check if the rotation is complete
        if (rotationProgress >= 1f)
        {
            // Toggle the rotation direction for the next cycle
            rotateDirection = !rotateDirection;

            // Reset the flag to stop rotation
            isRotating = false;
        }
    }

    public void DetectTouchedCard(Vector3 touchPosition)
    {
        // Convert touch position to a ray
        Ray ray = Camera.main.ScreenPointToRay(touchPosition);

        // Perform a raycast to detect the card
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider != null && hit.collider.gameObject == this.gameObject)
            {
                OnCardTouched(touchPosition);
            }
        }
    }

    private bool stopPower = false;
    public void OnCardTouched(Vector3 touchPosition)
    {
        stopPower = AreSwapPowersActive();
        //Debug.Log("OnCardTouched called for card: " + gameObject.name);
        // Ensure only one card is selected at a time
        //if (currentlySelectedCard == this)
        //return;
        string parentName = gameObject.transform.parent != null ? gameObject.transform.parent.name : "";
        Transform localPlayerHand = GetLocalPlayerHandTransform();
        bool isOwnHand = transform.parent == localPlayerHand;

        /*if (restrictToOwnHand)
        {
            if (!isOwnHand)
                return; // Only allow own hand cards
        }
        else
        {
            // If not allowed to select own hand, block
            if (!allowOwnHand && isOwnHand)
                return;
            // If allowed parent names are set, only allow those
            if (allowedParentNames.Count > 0 && !allowedParentNames.Contains(parentName))
                return;
            // If selection count is limited, block after limit
            if (allowedSelections > 0 && currentSelections >= allowedSelections)
                return;
            currentSelections++;
            // If we've reached the limit, restore default restriction
            if (allowedSelections > 0 && currentSelections >= allowedSelections)
                RestrictSelectionToOwnHand();
        }*/

        // Kapkaç and Yandım Anam pending checks
        if (GameManager.LocalInstance != null)
        {
            // Kapkaç pending
            if (GameManager.LocalInstance.isKapkacPending)
            {
                Debug.Log($"[CardInteraction] KAPKAÇ POWER ACTIVATED on card: {this.uniqueCardInstanceID}");
                GameManager.LocalInstance.isKapkacPending = false;
                CardInteraction.RestrictSelectionToOwnHand();
                DeckController.LocalInstance.ExitShowcaseAllOtherHands();
                
                // IMPORTANT: Clear any existing selection state before applying Kapkaç
                if (CardInteraction.currentlySelectedCard != null)
                {
                    Debug.Log($"[CardInteraction] Clearing currently selected card before Kapkaç: {CardInteraction.currentlySelectedCard.uniqueCardInstanceID}");
                    CardInteraction.currentlySelectedCard = null;
                    CardInteraction.isOneCardSelected = false;
                }
                
                GameManager.LocalInstance.networkRelay.ActivateKapkacOnCardServerRPC(this.uniqueCardInstanceID);
                Debug.Log($"[CardInteraction] KAPKAÇ POWER - Returning early, should NOT continue to normal selection");
                return;
            }
            // Yandım Anam pending
            if (GameManager.LocalInstance.isYandimAnamPending)
            {
                Debug.Log($"[CardInteraction] YANDIM ANAM POWER ACTIVATED on card: {this.uniqueCardInstanceID}");
                GameManager.LocalInstance.isYandimAnamPending = false;
                CardInteraction.RestrictSelectionToOwnHand();
                DeckController.LocalInstance.ExitShowcaseAllOtherHands();
                
                // IMPORTANT: Clear any existing selection state before applying Yandım Anam
                if (CardInteraction.currentlySelectedCard != null)
                {
                    Debug.Log($"[CardInteraction] Clearing currently selected card before Yandım Anam: {CardInteraction.currentlySelectedCard.uniqueCardInstanceID}");
                    CardInteraction.currentlySelectedCard = null;
                    CardInteraction.isOneCardSelected = false;
                }
                
                GameManager.LocalInstance.networkRelay.ActivateYandimAnamOnCardServerRPC(this.uniqueCardInstanceID);
                Debug.Log($"[CardInteraction] YANDIM ANAM POWER - Returning early, should NOT continue to normal selection");
                return;
            }
        }

        // DEBUG: Log all card touches to see what's happening
        Debug.Log($"[CardInteraction] OnCardTouched called for card: {gameObject.name}, parent: {gameObject.transform.parent?.name}");

        // Check if this is a center card
        if (gameObject.transform.parent != null && gameObject.transform.parent.name == "Center")
        {
            Debug.Log($"[CardInteraction] CENTER CARD DETECTED: {gameObject.name}");
            
            // Kill any existing animations on this card
            KillAllTweens();
            
            // Handle center card showcase
            if (DeckController.LocalInstance != null)
            {
                // OPTION 1: Prevent clicks during animation (safer approach)
                if (DeckController.LocalInstance.IsCenterShowcaseAnimating())
                {
                    Debug.Log("[CardInteraction] Center showcase animation in progress - ignoring click");
                    return;
                }
                
                if (DeckController.LocalInstance.IsCenterShowcasing())
                {
                    Debug.Log("[CardInteraction] Stopping center showcase");
                    DeckController.LocalInstance.StopShowcaseCenterCards();
                }
                else
                {
                    Debug.Log("[CardInteraction] Starting center showcase");
                    DeckController.LocalInstance.StopShowcasePlayerPoolCards();
                    DeckController.LocalInstance.ExitShowcaseAllOtherHands();
                    DeckController.LocalInstance.ShowcaseCenterCards();
                }
            }
            else
            {
                Debug.LogError("[CardInteraction] DeckController.LocalInstance is null!");
            }
            
            // Return early to prevent normal card selection animations for center cards
            return;
        }


        // Check if this is a player pool card
        if (gameObject.transform.parent.name == "PlayerPool1" || gameObject.transform.parent.name == "PlayerPiştiPool1")
        {
            Debug.Log($"[CardInteraction] Player pool card touched: {gameObject.name}");
            // Stop center showcase if active
            DeckController.LocalInstance.StopShowcaseCenterCards();
            DeckController.LocalInstance.ShowcasePlayerPoolCards();
        }
        // Handle center cards and hand cards for selection
        else if (gameObject.transform.parent.name == "Center" || gameObject.transform.parent.name.StartsWith("PlayerHand"))
        {
            Debug.Log($"[CardInteraction] Card selected for interaction: {gameObject.name} (parent: {gameObject.transform.parent.name})");
            
            // Stop center showcase when touching hand cards (but not center cards themselves)
            if (gameObject.transform.parent.name != "Center")
            {
                DeckController.LocalInstance.StopShowcaseCenterCards();
            }
            
            // Store the original screen position for snap back
            originalScreenPosition = Camera.main.WorldToScreenPoint(transform.position);

            // Activate the card's indicator and deactivate others
            SelectCard();

            // Calculate the offset between the touch position and the card's screen position
            offset = originalScreenPosition - new Vector3(touchPosition.x, touchPosition.y, 0);

            // Set this card as the currently selected card
            currentlySelectedCard = this;

            // Invoke OnCardSelected
            OnCardSelected?.Invoke(this.uniqueCardInstanceID);
        }

        if (GameManager.LocalInstance != null && GameManager.LocalInstance.isKopyalaActive)
        {
            Debug.Log("Kopyala active, trying to copy card: " + gameObject.name);
            GameManager.LocalInstance.TryKopyalaYapistir(this);
            return;
        }

    }

    private static Transform GetLocalPlayerHandTransform()
    {
        if (DeckController.LocalInstance == null) return null;
        int myNo = DeckController.LocalInstance.thisPlayerNumber;
        if (DeckController.LocalInstance.playerHandTransforms == null || DeckController.LocalInstance.playerHandTransforms.Count == 0)
            return null;
        // In your setup, playerHandTransforms[0] is always the local player's hand
        return DeckController.LocalInstance.playerHandTransforms[myNo]; // Get the local player's hand transform
    }



    // Add this helper in CardInteraction.cs
    private int GetHandIndexFromParentName(string parentName)
    {
        // Assumes hand names like "PlayerHand1", "PlayerHand2", etc.
        if (parentName.StartsWith("PlayerHand"))
        {
            string num = parentName.Substring("PlayerHand".Length);
            if (int.TryParse(num, out int idx))
                return idx - 1; // zero-based
        }
        return -1;
    }

    public void OnTouchDrag(Vector3 touchPosition)
    {
        // Prevent drag while swap powers are active
        if (stopPower)
            return;
        Debug.Log("OnTouchDrag called for card: " + gameObject.name);
        if (isDragging)
        {
            // Move the card using the screen position and offset
            Vector3 newScreenPosition = new Vector3(touchPosition.x + offset.x, touchPosition.y + offset.y, originalScreenPosition.z);
            transform.position = Camera.main.ScreenToWorldPoint(newScreenPosition); // Convert back to world space for rendering
        }
    }

    public void OnTouchUp()
    {
        StartCoroutine(OnTouchUpCoroutine());
    }
    
    public IEnumerator OnTouchUpCoroutine()
    {
        // Kill all active DOTween animations on this card before proceeding
        //yield return StartCoroutine(WaitForAllTweens());
        // Prevent touch up while swap powers are active
        if (stopPower)
            yield return null;

        Debug.Log($"[CardInteraction] OnTouchUp called for card: {gameObject.name}, parent: {gameObject.transform.parent?.name}, isDragging: {isDragging}");
        if (!isDragging) 
        {
            Debug.Log("[CardInteraction] Not dragging, returning early");
            yield return null;
        }

        isDragging = false;

        // Calculate the distance the card has moved in screen space
        float distanceMoved = Vector3.Distance(Camera.main.WorldToScreenPoint(transform.position), originalScreenPosition);


        // Try to add the card to the center if moved enough distance
        if (distanceMoved > snapBackThreshold)
        {
            Debug.Log($"[CardInteraction] Card moved enough distance. Parent: {transform.parent.name}, isOneCardSelected: {isOneCardSelected}");

            if (transform.parent.name.Contains("PlayerHand") && isOneCardSelected)
            {
                // Invoke OnCardsPlayed
                Debug.Log("[CardInteraction] Invoking OnCardsPlayed for hand card");
                StopAutoRotate(); // Stop auto-rotation when the card is played

                //yield return new WaitForSeconds(1f);
                
                OnCardsPlayed?.Invoke(this.uniqueCardInstanceID, this.gameObject, GameManager.currentPlayerNo);

                if (activeCardIndicator != null)
                {
                    activeCardIndicator.SetActive(false); // Deactivate the previous card indicator
                }
            }
            else if (transform.parent.name == "Center" && isOneCardSelected)
            {
                Debug.Log("[CardInteraction] Center card was selected but cannot be played - showcasing only");
                // Center cards cannot be played, just reset selection
                isOneCardSelected = false;
                if (activeCardIndicator != null)
                {
                    activeCardIndicator.SetActive(false);
                }
            }
            else
            {
                Debug.Log("[CardInteraction] Card not from hand or not selected, resetting selection");
                isOneCardSelected = false;
            }
        }
        else
        {
            // Snap back to the original position
            transform.position = Camera.main.ScreenToWorldPoint(originalScreenPosition);
        }

        // Deactivate the card's indicator
        //selectedCardIndicator.SetActive(false);
    }

    Sequence popSequence;
    Sequence jiggleSequence;
    public void SelectCard()
    {
        Debug.Log($"[CardInteraction] SelectCard called for: {gameObject.name}, parent: {gameObject.transform.parent?.name}");
        
        if (activeCardIndicator != null)
        {
            activeCardIndicator.SetActive(false); // Deactivate the previous card indicator
        }
        isDragging = true;

        // Activate this card's indicator
        selectedCardIndicator.SetActive(true);
        selectedCardIndicator.GetComponent<SpriteRenderer>().color = baseCardIndicatorColor; // Bring the card to the front
        activeCardIndicator = selectedCardIndicator;

        // Mark this card as selected
        isOneCardSelected = true;

        //transform.DOKill(); // Stop any previous tweens
        popSequence = DOTween.Sequence();
        // Scale up to 1.25x
        popSequence.Append(transform.DOScale(transform.localScale * 1.25f, 0.1f));
        // Scale back to original
        popSequence.Append(transform.DOScale(transform.localScale, 0.1f));
        // Scale up to 1.1x
        popSequence.Append(transform.DOScale(transform.localScale * 1.1f, 0.1f));
        // Scale back to original
        popSequence.Append(transform.DOScale(transform.localScale, 0.1f));

        // Or for a jiggle:
        jiggleSequence = DOTween.Sequence();
        var startEuler = transform.rotation.eulerAngles;
        // +15, back
        jiggleSequence.Append(transform.DORotate(startEuler + new Vector3(0, 0, 15), 0.025f));
        jiggleSequence.Append(transform.DORotate(startEuler, 0.025f));
        // -12, back
        jiggleSequence.Append(transform.DORotate(startEuler + new Vector3(0, 0, -12), 0.025f));
        jiggleSequence.Append(transform.DORotate(startEuler, 0.025f));
        // +13, back
        jiggleSequence.Append(transform.DORotate(startEuler + new Vector3(0, 0, 13), 0.025f));
        jiggleSequence.Append(transform.DORotate(startEuler, 0.025f));
        // -16, back
        jiggleSequence.Append(transform.DORotate(startEuler + new Vector3(0, 0, -16), 0.025f));
        jiggleSequence.Append(transform.DORotate(startEuler, 0.025f));
        // +17, back
        jiggleSequence.Append(transform.DORotate(startEuler + new Vector3(0, 0, 17), 0.025f));
        jiggleSequence.Append(transform.DORotate(startEuler, 0.025f));
        // -17, back
        jiggleSequence.Append(transform.DORotate(startEuler + new Vector3(0, 0, -17), 0.025f));
        jiggleSequence.Append(transform.DORotate(startEuler, 0.025f));
    }


    public void InitializeCard()
    {
        //cardID = GetCardID();
        gameObject.tag = cardID[0] + "_" + cardID[1];

        transform.localScale = new Vector3(1000, 1000, 1000);
        transform.localRotation = Quaternion.Euler(0, 0, 0);
        InitializeCardInd();
        InitializeCardBack();
        
        // Store original data after initialization
        StoreOriginalCardData();
    }

    private void InitializeCardInd()
    {
        GameObject cardInd = Instantiate(GameManager.LocalInstance.cardIndicator, transform.position, Quaternion.identity);
        cardInd.transform.parent = transform;
        Transform cardIndTransform = cardInd.transform;
        cardIndTransform.localPosition = new Vector3(0, 0, 0.04f);
        cardIndTransform.localRotation = cardInd.transform.rotation;
        cardIndTransform.localScale = new Vector3(1.1f, 1.25f, 1);
        baseCardIndicatorColor = cardInd.GetComponent<SpriteRenderer>().color;
        cardInd.SetActive(false);
        selectedCardIndicator = cardInd;
    }

    private void InitializeCardBack()
    {
        GameManager.LocalInstance.cardBack.GetComponent<SpriteRenderer>().sprite = GameManager.LocalInstance.cardBackSprite;
        GameObject cardBack = Instantiate(GameManager.LocalInstance.cardBack, transform.position, Quaternion.identity);
        cardBack.transform.parent = transform;
        Transform cardBackTransform = cardBack.transform;
        cardBackTransform.localPosition = new Vector3(0, 0, 0.002f);
        cardBackTransform.localRotation = cardBack.transform.rotation;
        cardBackTransform.localScale = new Vector3(1, 1, 1);
    }

    // Add at the top of the class:
    private int[] originalCardID = null;
    private Sprite originalSprite = null;

    // Call this in InitializeCard() after setting cardID and sprite:
    public void StoreOriginalCardData()
    {
        if (originalCardID == null)
            originalCardID = (int[])cardID.Clone();
        if (originalSprite == null)
            originalSprite = GetComponent<SpriteRenderer>().sprite;
    }

    // Call this to reset the card to its original state:
    public void ResetToOriginalCard()
    {
        if (originalCardID != null)
        {
            cardID = (int[])originalCardID.Clone();
            gameObject.tag = cardID[0] + "_" + cardID[1];
        }
        if (originalSprite != null)
            GetComponent<SpriteRenderer>().sprite = originalSprite;
        
        // Reset power effect
        activePowerEffect = "none";
    }

    public void SetCardIDAndSprite(int[] newCardID, Sprite newSprite)
    {
        cardID = (int[])newCardID.Clone();
        gameObject.tag = cardID[0] + "_" + cardID[1];
        GetComponent<SpriteRenderer>().sprite = newSprite;
    }


    public void TriggerOnCardSelected()
    {
        OnCardSelected?.Invoke(uniqueCardInstanceID);
    }

    // Helper to check if swap powers are active
    private bool AreSwapPowersActive()
    {
        if (GameManager.LocalInstance == null)
            return false;
        return GameManager.LocalInstance.IsAnySwapPowerActive();
    }

    private Sequence autoRotateSequence;
    private bool autoRotateActive = false;

    public void StartAutoRotate(float minAngle = 10f, float maxAngle = 20f, float duration = 2.5f)
    {
        autoRotateActive = true;
        // Kill any previous sequence
        if (autoRotateSequence != null && autoRotateSequence.IsActive()) autoRotateSequence.Kill();
        transform.DOKill();

        // Always start from face up
        //transform.rotation = Quaternion.Euler(90, 0, 0);

        // Pick a random angle for this cycle
        float angle = UnityEngine.Random.Range(minAngle, maxAngle);
        float transformX = 90;
        float transformY = 0;

        autoRotateSequence = DOTween.Sequence();
        autoRotateSequence.Append(transform.DORotate(new Vector3(transformX, transformY, angle), duration).SetEase(Ease.OutSine));
        autoRotateSequence.Append(transform.DORotate(new Vector3(transformX, transformY, -angle), duration * 2).SetEase(Ease.InOutSine));
        autoRotateSequence.Append(transform.DORotate(new Vector3(transformX, transformY, 0), duration).SetEase(Ease.InSine));
        autoRotateSequence.SetLoops(1)
            .OnComplete(() =>
            {
                // If still active, start again with a new random angle
                if (autoRotateActive)
                    StartAutoRotate(minAngle, maxAngle, duration);
            });
    }

    public void StartAutoRotateFaceDown(float minAngle = 10f, float maxAngle = 20f, float duration = 2.5f)
    {
        autoRotateActive = true;
        // Kill any previous sequence
        if (autoRotateSequence != null && autoRotateSequence.IsActive()) autoRotateSequence.Kill();
        transform.DOKill();

        // Get the current rotation
        Vector3 currentEuler = transform.localRotation.eulerAngles;
        float currentZ = currentEuler.z;

        // Pick a random angle offset between minAngle and maxAngle
        float angleOffset = UnityEngine.Random.Range(minAngle, maxAngle);

        // Rotate around z axis: currentZ + angleOffset, then currentZ - angleOffset, then back to currentZ
        autoRotateSequence = DOTween.Sequence();
        autoRotateSequence.Append(transform.DORotate(new Vector3(-90, 0, currentZ + angleOffset), duration).SetEase(Ease.OutSine));
        autoRotateSequence.Append(transform.DORotate(new Vector3(-90, 0, currentZ - angleOffset), duration * 2).SetEase(Ease.InOutSine));
        autoRotateSequence.Append(transform.DORotate(new Vector3(-90, 0, currentZ), duration).SetEase(Ease.InSine));
        autoRotateSequence.SetLoops(1)
            .OnComplete(() =>
            {
                // If still active, start again with a new random angle
                if (autoRotateActive)
                    StartAutoRotateFaceDown(minAngle, maxAngle, duration);
            });
    }


    public void StopAutoRotate()
    {
        autoRotateActive = false;
        //yield return StartCoroutine(WaitForAllTweens());
        KillAllTweens();
    }

    /// <summary>
    /// Restrict selection to only own hand cards (default).
    /// </summary>
    public static void RestrictSelectionToOwnHand()
    {
        restrictToOwnHand = true;
        allowedParentNames = new HashSet<string> { "PlayerHand1", "PlayerHand2", "PlayerHand3", "PlayerHand4" };
        allowOwnHand = true;
        allowedSelections = 0;
        currentSelections = 0;
    }

    /// <summary>
    /// Allow selection of cards with specific parent names (e.g. other players' hands, center).
    /// Set allowOwnHand to false to prevent selecting own hand cards.
    /// Set maxSelections to limit how many times this is allowed (0 = unlimited).
    /// </summary>
    public static void AllowSelectionForParents(IEnumerable<string> parentNames, bool allowOwnHandCards = false, int maxSelections = 1)
    {
        restrictToOwnHand = false;
        allowedParentNames = new HashSet<string>(parentNames);
        allowOwnHand = allowOwnHandCards;
        allowedSelections = maxSelections;
        currentSelections = 0;
    }

    public void KillAllTweens()
    {
        // Kill DOTween tweens on this transform
        DOTween.Kill(transform);
        transform.DOKill();

        // Kill pop/jiggle sequences if active
        if (popSequence != null && popSequence.IsActive()) popSequence.Kill();
        if (jiggleSequence != null && jiggleSequence.IsActive()) jiggleSequence.Kill();

        // Kill auto-rotate sequence if active
        if (autoRotateSequence != null && autoRotateSequence.IsActive()) autoRotateSequence.Kill();
    }
    
    public void KillAllTweens(int a)
    {
        // Force complete any active sequences immediately
        if (popSequence != null && popSequence.IsActive())
        {
            popSequence.Complete(true); // true = with callbacks
            popSequence = null;
        }
        
        if (jiggleSequence != null && jiggleSequence.IsActive())
        {
            jiggleSequence.Complete(true);
            jiggleSequence = null;
        }
        
        // Also force complete any other tweens on this object
        DOTween.Complete(this);
    }

    public IEnumerator WaitForAllTweens()
    {
        // Wait until there are no active tweens on this transform
        while (DOTween.IsTweening(transform))
            yield return null;
    }

    /// <summary>
    /// Check if this card is currently animating (has active DOTween tweens)
    /// </summary>
    public bool IsAnimating()
    {
        return DOTween.IsTweening(transform);
    }

    public IEnumerator WaitForAllTweens(Transform target)
    {
        // Wait until there are no active tweens on this transform
        while (DOTween.IsTweening(target))
            yield return null;
    }

    /// <summary>
    /// Snaps the card back to its original position and resets selection state.
    /// </summary>
    public void SnapBackToOriginalPosition()
    {
        // Reset position to original screen position
        if (originalScreenPosition != Vector3.zero)
        {
            transform.position = Camera.main.ScreenToWorldPoint(originalScreenPosition);
        }
        
        // Reset drag state
        isDragging = false;
        
        // Deactivate card indicator if this card is selected
        if (selectedCardIndicator != null && selectedCardIndicator.activeSelf)
        {
            selectedCardIndicator.SetActive(false);
        }
        
        // Reset static selection state if this is the selected card
        if (currentlySelectedCard == this)
        {
            currentlySelectedCard = null;
            isOneCardSelected = false;
        }
    }
    
    /// <summary>
    /// Public method for bot to play a card - invokes OnCardsPlayed event
    /// </summary>
    public void PlayCardForBot(int playerNumber)
    {
        Debug.Log($"[Bot] ===== CARDINTERACTION.PLAYCARDFORBOT START =====");
        BotPlayer.AddBotLog($"[Bot] ===== CARDINTERACTION.PLAYCARDFORBOT START =====");
        Debug.Log($"[Bot] PlayCardForBot called for player {playerNumber}");
        BotPlayer.AddBotLog($"[Bot] PlayCardForBot called for player {playerNumber}");
        Debug.Log($"[Bot] Card: {this.uniqueCardInstanceID}");
        BotPlayer.AddBotLog($"[Bot] Card: {this.uniqueCardInstanceID}");
        Debug.Log($"[Bot] Card data: {this.GetCardID()[0]}_{this.GetCardID()[1]}");
        BotPlayer.AddBotLog($"[Bot] Card data: {this.GetCardID()[0]}_{this.GetCardID()[1]}");
        
        // Set the card as selected
        Debug.Log($"[Bot] Setting card selection state...");
        BotPlayer.AddBotLog($"[Bot] Setting card selection state...");
        isOneCardSelected = true;
        currentlySelectedCard = this;
        Debug.Log($"[Bot] ✓ Card selection state set");
        BotPlayer.AddBotLog($"[Bot] ✓ Card selection state set");
        
        // Set the card as selected in GameManager
        if (GameManager.LocalInstance != null)
        {
            Debug.Log($"[Bot] Setting GameManager.currentSelectedHandCard...");
            BotPlayer.AddBotLog($"[Bot] Setting GameManager.currentSelectedHandCard...");
            GameManager.LocalInstance.currentSelectedHandCard = this.uniqueCardInstanceID;
            Debug.Log($"[Bot] ✓ GameManager selection set");
            BotPlayer.AddBotLog($"[Bot] ✓ GameManager selection set");
        }
        else
        {
            Debug.LogError($"[Bot] ERROR: GameManager.LocalInstance is null!");
            BotPlayer.AddBotLog($"[Bot] ERROR: GameManager.LocalInstance is null!");
        }
        
        Debug.Log($"[Bot] About to invoke OnCardsPlayed event...");
        BotPlayer.AddBotLog($"[Bot] About to invoke OnCardsPlayed event...");
        
        // Invoke the OnCardsPlayed event
        OnCardsPlayed?.Invoke(this.uniqueCardInstanceID, this.gameObject, playerNumber);
        
        Debug.Log($"[Bot] ✓ OnCardsPlayed event invoked");
        BotPlayer.AddBotLog($"[Bot] ✓ OnCardsPlayed event invoked");
        Debug.Log($"[Bot] ===== CARDINTERACTION.PLAYCARDFORBOT COMPLETE =====");
        BotPlayer.AddBotLog($"[Bot] ===== CARDINTERACTION.PLAYCARDFORBOT COMPLETE =====");
    }
}