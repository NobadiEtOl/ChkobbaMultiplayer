using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardInteraction : MonoBehaviour
{  
    //(kind)cardID[0]=>1:Clubs/2:Diamonds/3:Hearts/4:Spades , (value)cardID[1]=>1/2/3/4/5/6/7/8/9/10/11/12/13
    private int[] cardID = new int[2];
    //A static variable to keep track of if the player selected a card or not 
    public static bool isOneCardSelected;
    public bool isPlayable = false; // Condition to check if the card can be played when the server is iplemented
    private Vector3 originalPosition; // Original position of the card
    private bool isDragging = false; // Is the card currently being dragged
    private float snapBackThreshold = 500f; // Minimum distance to call functions, adjust as needed

    private GameObject selectedCardIndicator;
    void Start()
    {
        isOneCardSelected=false;

        InitializeCard();
    }

    private void InitializeCard()
    {
        //Create IDs for every card except for the add button
        cardID = GetCardID();
        gameObject.tag = cardID[0] + "_" + cardID[1];

        InitializeCardInd();

        InitializeCardBack();

        transform.localScale = new Vector3(380,400, 2);
    }

    //Check clicks done to the cards
    private void OnMouseDown()
    {
        //Setting original position for snap back
        originalPosition = transform.position;
        int thisPlayerNumber = DeckController.LocalInstance.thisPlayerNumber;

        if (transform.parent.name == "PlayerHand"+(thisPlayerNumber+1) /*&& GameManager.currentPlayerNo == thisPlayerNumber*/ || transform.parent.name.Contains("PlayerHand"))
        {
            SelectCard();
        }
    }

    void OnMouseDrag()
    {
        if (isDragging)
        {
            // Convert mouse position to world position and move the card
            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = Camera.main.WorldToScreenPoint(transform.position).z; // Keep Z-axis
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);

            transform.position = new Vector3(worldPosition.x, worldPosition.y, originalPosition.z); // Move card
        }
    }

    void OnMouseUp()
    {
        isDragging = false;

        // Calculate the distance the card has moved
        float distanceMoved = Vector3.Distance(transform.position, originalPosition);

        //Try to add the card to the center if moved enough distance
        if (distanceMoved > snapBackThreshold)
        {
            //Try to play a move if a card is already selected and a ceter card is pressed 
            if(transform.parent.name.Contains("PlayerHand") && isOneCardSelected)
            {   
                TryToPlayMove();
            }
            else
            {
                Debug.Log("else");
                isOneCardSelected=false;        
            }
            
        }
        else transform.position = originalPosition;
    }

    public event Action<int[]> OnCardSelected;
    private void SelectCard()//Event when a card is selected
    {
        isDragging = true;

        GameManager.LocalInstance.DeactivateCardIndicators();

        OnCardSelected?.Invoke(this.cardID);
        selectedCardIndicator.SetActive(true);
        GameManager.LocalInstance.activeCardIndicatorList.Add(selectedCardIndicator);
        isOneCardSelected=true;
    }

    public event Action<int[], GameObject, int> OnCardsPlayed;
    private void TryToPlayMove()//Event when selected cards is to be played
    {
        OnCardsPlayed?.Invoke(this.cardID, this.gameObject, GameManager.currentPlayerNo);
    }

    //Generates the cardId from its name
    private int[] GetCardID()
    {
        string[] tagStrings = gameObject.tag.Split('_');
        int[] cardID = { 0, 0 };

        if (tagStrings.Length != 2)
        {
            Debug.LogError("Invalid tag format! Expected 'Kind_Value'.");
        }

        // Parse the first part of the tag (Kind)
        if (!int.TryParse(tagStrings[0], out cardID[0]) || cardID[0] <= 0)
        {
            Debug.LogError($"Invalid card kind: {tagStrings[0]}");
        }

        // Parse the second part of the tag (Value)
        if (!int.TryParse(tagStrings[1], out cardID[1]) || cardID[1] <= 0 || cardID[1] > 13)
        {
            Debug.LogError($"Invalid card value: {tagStrings[1]}");
        }

        return cardID;
    }

    private void InitializeCardInd()
    {
        //Identifing the card indicator element
        GameObject cardInd = Instantiate(GameManager.LocalInstance.cardIndicator, transform.position, Quaternion.identity);
        cardInd.transform.parent = transform;
        Transform cardIndTransform = cardInd.transform;
        cardIndTransform.localPosition = new Vector3(0,0,0.04f);
        cardInd.SetActive(false);
        selectedCardIndicator = cardInd;
    }

    private void InitializeCardBack()
    {
        //Identifing and placing the back of the cards
        GameObject cardBack = Instantiate(GameManager.LocalInstance.cardBack, transform.position, Quaternion.identity);
        cardBack.transform.parent = transform;
        Transform cardBackTransform = cardBack.transform;
        cardBackTransform.localPosition = new Vector3(0,0,0.02f);
    }

}
