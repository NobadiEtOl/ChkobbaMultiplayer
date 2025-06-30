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
    private Vector3 originalScreenPosition; // Original position in screen space
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
        bool isOwnHand = parentName.Contains("PlayerHand") && DeckController.LocalInstance != null &&
                        DeckController.LocalInstance.thisPlayerNumber == GetHandIndexFromParentName(parentName);

        if (restrictToOwnHand)
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
        }

        if (gameObject.transform.parent.name == "PlayerPool1" || gameObject.transform.parent.name == "PlayerPiştiPool1")
        {
            DeckController.LocalInstance.ShowcasePlayerPoolCards();
        }

        else
        {
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
        // Prevent touch up while swap powers are active
        if (stopPower)
            return;

        Debug.Log("OnTouchUp called for card: " + gameObject.name);
        if (!isDragging) return;

        isDragging = false;

        // Calculate the distance the card has moved in screen space
        float distanceMoved = Vector3.Distance(Camera.main.WorldToScreenPoint(transform.position), originalScreenPosition);

        // Try to add the card to the center if moved enough distance
        if (distanceMoved > snapBackThreshold)
        {
            if (transform.parent.name.Contains("PlayerHand") && isOneCardSelected)
            {
                // Invoke OnCardsPlayed
                //Debug.Log("OnCardsPlayed invoked!");
                StopAutoRotate(); // Stop auto-rotation when the card is played
                OnCardsPlayed?.Invoke(this.uniqueCardInstanceID, this.gameObject, GameManager.currentPlayerNo);

                if (activeCardIndicator != null)
                {
                    activeCardIndicator.SetActive(false); // Deactivate the previous card indicator
                }
            }
            else
            {
                //Debug.Log("else");
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

    private void SelectCard()
    {
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
        Sequence popSequence = DOTween.Sequence();
        popSequence.Append(transform.DOScale(transform.localScale * 1.25f, 0.1f).SetLoops(2, LoopType.Yoyo));
        popSequence.Append(transform.DOScale(transform.localScale * 1.1f, 0.1f).SetLoops(2, LoopType.Yoyo));
        // Or for a jiggle:
        Sequence jiggleSequence = DOTween.Sequence();
        jiggleSequence.Append(transform.DORotate(transform.rotation.eulerAngles + new Vector3(0, 0, 15), 0.025f).SetLoops(2, LoopType.Yoyo));
        jiggleSequence.Append(transform.DORotate(transform.rotation.eulerAngles + new Vector3(0, 0, -12), 0.025f).SetLoops(2, LoopType.Yoyo));
        jiggleSequence.Append(transform.DORotate(transform.rotation.eulerAngles + new Vector3(0, 0, 13), 0.025f).SetLoops(2, LoopType.Yoyo));
        jiggleSequence.Append(transform.DORotate(transform.rotation.eulerAngles + new Vector3(0, 0, -16), 0.025f).SetLoops(2, LoopType.Yoyo));
        jiggleSequence.Append(transform.DORotate(transform.rotation.eulerAngles + new Vector3(0, 0, 17), 0.025f).SetLoops(2, LoopType.Yoyo));
        jiggleSequence.Append(transform.DORotate(transform.rotation.eulerAngles + new Vector3(0, 0, -17), 0.025f).SetLoops(2, LoopType.Yoyo));
    }

    public void InitializeCard()
    {
        //cardID = GetCardID();
        gameObject.tag = cardID[0] + "_" + cardID[1];

        transform.localScale = new Vector3(1000, 1000, 1000);
        transform.localRotation = Quaternion.Euler(0, 0, 0);
        InitializeCardInd();
        InitializeCardBack();
    }

    private void InitializeCardInd()
    {
        GameObject cardInd = Instantiate(GameManager.LocalInstance.cardIndicator, transform.position, Quaternion.identity);
        cardInd.transform.parent = transform;
        Transform cardIndTransform = cardInd.transform;
        cardIndTransform.localPosition = new Vector3(0, 0, 0.04f);
        cardIndTransform.localRotation = cardInd.transform.rotation;
        cardIndTransform.localScale = new Vector3(1f, 1f, 1);
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

        autoRotateSequence = DOTween.Sequence();
        autoRotateSequence.Append(transform.DORotate(new Vector3(90, 0, angle), duration).SetEase(Ease.OutSine));
        autoRotateSequence.Append(transform.DORotate(new Vector3(90, 0, -angle), duration * 2).SetEase(Ease.InOutSine));
        autoRotateSequence.Append(transform.DORotate(new Vector3(90, 0, 0), duration).SetEase(Ease.InSine));
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

        // Always start from face up
        //transform.rotation = Quaternion.Euler(90, 0, 0);

        // Pick a random angle for this cycle
        float angle = UnityEngine.Random.Range(minAngle, maxAngle);

        autoRotateSequence = DOTween.Sequence();
        autoRotateSequence.Append(transform.DORotate(new Vector3(-90, 0, angle), duration).SetEase(Ease.OutSine));
        autoRotateSequence.Append(transform.DORotate(new Vector3(-90, 0, -angle), duration * 2).SetEase(Ease.InOutSine));
        autoRotateSequence.Append(transform.DORotate(new Vector3(-90, 0, 0), duration).SetEase(Ease.InSine));
        autoRotateSequence.SetLoops(1)
            .OnComplete(() =>
            {
                // If still active, start again with a new random angle
                if (autoRotateActive)
                    StartAutoRotate(minAngle, maxAngle, duration);
            });
    }


    public void StopAutoRotate()
    {
        autoRotateActive = false;
        if (autoRotateSequence != null && autoRotateSequence.IsActive()) autoRotateSequence.Kill();
        transform.DOKill();
        //transform.localRotation = Quaternion.Euler(90, 0, 0); // Reset to face up
    }

    /// <summary>
    /// Restrict selection to only own hand cards (default).
    /// </summary>
    public static void RestrictSelectionToOwnHand()
    {
        restrictToOwnHand = true;
        allowedParentNames.Clear();
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


}