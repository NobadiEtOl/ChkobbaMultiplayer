using System;
using UnityEngine;
using System.Collections.Generic;

public class CardInteraction : MonoBehaviour
{
    private int[] cardID = new int[2];
    public static bool isOneCardSelected = false;
    private static CardInteraction currentlySelectedCard = null; // Tracks the currently selected card
    public bool isPlayable = false;

    private Vector3 originalScreenPosition; // Original position in screen space
    private Vector3 offset; // Offset between touch position and card position in screen space
    private bool isDragging = false;
    private float snapBackThreshold = 500f;
    private GameObject selectedCardIndicator;
    public event Action<int[]> OnCardSelected;
    public event Action<int[], GameObject, int> OnCardsPlayed;
    private static GameObject activeCardIndicator = null; // Tracks the currently active card indicator

    void Start()
    {
        InitializeCard();
    }

    void Update()
    {
        if (Input.touchCount > 0) // Check if there is at least one touch
        {
            Touch touch = Input.GetTouch(0); // Get the first touch
            Vector3 touchPosition = touch.position; // Use screen position directly

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    DetectTouchedCard(touchPosition);
                    break;

                case TouchPhase.Moved:
                    if (currentlySelectedCard == this) // Only move the selected card
                    {
                        OnTouchDrag(touchPosition);
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (currentlySelectedCard == this) // Only release the selected card
                    {
                        OnTouchUp();
                        currentlySelectedCard = null; // Clear the selected card
                    }
                    break;
            }
        }
    }

    private void DetectTouchedCard(Vector3 touchPosition)
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

    private void OnCardTouched(Vector3 touchPosition)
    {
        // Ensure only one card is selected at a time
        if (currentlySelectedCard != null && currentlySelectedCard != this)
            return;

        // This method is called when the card is touched or clicked
        Debug.Log($"Touched Object: {gameObject.name}");

        // Store the original screen position for snap back
        originalScreenPosition = Camera.main.WorldToScreenPoint(transform.position);

        // Activate the card's indicator and deactivate others
        SelectCard();

        // Calculate the offset between the touch position and the card's screen position
        offset = originalScreenPosition - new Vector3(touchPosition.x, touchPosition.y, 0);

        // Set this card as the currently selected card
        currentlySelectedCard = this;

        // Invoke OnCardSelected
        OnCardSelected?.Invoke(this.cardID);
    }

    private void OnTouchDrag(Vector3 touchPosition)
    {
        if (isDragging)
        {
            // Move the card using the screen position and offset
            Vector3 newScreenPosition = new Vector3(touchPosition.x + offset.x, touchPosition.y + offset.y, originalScreenPosition.z);
            transform.position = Camera.main.ScreenToWorldPoint(newScreenPosition); // Convert back to world space for rendering
        }
    }

    private void OnTouchUp()
    {
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
                OnCardsPlayed?.Invoke(this.cardID, this.gameObject, GameManager.currentPlayerNo);
            }
            else
            {
                Debug.Log("else");
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
        if(activeCardIndicator != null)
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

    private int[] GetCardID()
    {
        string[] tagStrings = gameObject.tag.Split('_');
        int[] cardID = { 0, 0 };

        if (tagStrings.Length != 2)
        {
            Debug.LogError("Invalid tag format! Expected 'Kind_Value'.");
        }

        if (!int.TryParse(tagStrings[0], out cardID[0]) || cardID[0] <= 0)
        {
            Debug.LogError($"Invalid card kind: {tagStrings[0]}");
        }

        if (!int.TryParse(tagStrings[1], out cardID[1]) || cardID[1] <= 0 || cardID[1] > 13)
        {
            Debug.LogError($"Invalid card value: {tagStrings[1]}");
        }

        return cardID;
    }

    private void InitializeCard()
    {
        cardID = GetCardID();
        gameObject.tag = cardID[0] + "_" + cardID[1];

        InitializeCardInd();
        InitializeCardBack();

        transform.localScale = new Vector3(380, 400, 2);
    }

    private void InitializeCardInd()
    {
        GameObject cardInd = Instantiate(GameManager.LocalInstance.cardIndicator, transform.position, Quaternion.identity);
        cardInd.transform.parent = transform;
        Transform cardIndTransform = cardInd.transform;
        cardIndTransform.localPosition = new Vector3(0, 0, 0.04f);
        cardInd.SetActive(false);
        selectedCardIndicator = cardInd;
    }

    private void InitializeCardBack()
    {
        GameObject cardBack = Instantiate(GameManager.LocalInstance.cardBack, transform.position, Quaternion.identity);
        cardBack.transform.parent = transform;
        Transform cardBackTransform = cardBack.transform;
        cardBackTransform.localPosition = new Vector3(0, 0, 0.02f);
        cardBackTransform.localRotation = cardBack.transform.rotation;
    }
}