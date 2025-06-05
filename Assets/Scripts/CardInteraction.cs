using System;
using UnityEngine;
using System.Collections.Generic;

public class CardInteraction : MonoBehaviour
{
    private int[] cardID = new int[2];
    public static bool isOneCardSelected = false;
    public static CardInteraction currentlySelectedCard = null; // Tracks the currently selected card
    public bool isPlayable = false;

    private Vector3 originalScreenPosition; // Original position in screen space
    private Vector3 offset; // Offset between touch position and card position in screen space
    private bool isDragging = false;
    private float snapBackThreshold = 250f;
    private GameObject selectedCardIndicator;
    public event Action<string> OnCardSelected;
    public event Action<string, GameObject, int> OnCardsPlayed;
    private static GameObject activeCardIndicator = null; // Tracks the currently active card indicator

    // New variable to control auto-rotation
    public bool autoRotateFlag = false;
    public static Dictionary<string, CardInteraction> cardLookup = new Dictionary<string, CardInteraction>();

    // In CardInteraction.cs
    public string uniqueCardInstanceID; // e.g., a GUID

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

    void Start()
    {
        //InitializeCard();
    }

    void Update()
    {
        if (autoRotateFlag)
        {
            // Call the rotation function if the card is not already rotating
            RotateCardWithLerp();
        }

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

    public void OnCardTouched(Vector3 touchPosition)
    {
        //Debug.Log("OnCardTouched called for card: " + gameObject.name);
        // Ensure only one card is selected at a time
        //if (currentlySelectedCard == this)
            //return;

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

    public void OnTouchDrag(Vector3 touchPosition)
    {
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
                autoRotateFlag = false; // Stop auto-rotation when the card is played
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
        activeCardIndicator = selectedCardIndicator;

        // Mark this card as selected
        isOneCardSelected = true;
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
        cardIndTransform.localScale = new Vector3(1.7f, 2.3f, 1);
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

}