using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardInteraction : MonoBehaviour
{  
    //(kind)cardID[0]=>1:Carreau/2:Coeur/3:Pique/4:Trefle , (value)cardID[1]=>1/2/3/4/5/6/7/8/9/10
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

        //Create IDs for every card except for the add button

        cardID = GetCardID();

        selectedCardIndicator = gameObject.transform.GetChild(1).gameObject;

        Transform cardBackTransform = transform.GetChild(0);
        cardBackTransform.localPosition = new Vector3(0,0,0.02f);

        gameObject.tag = cardID[0] + "_" + cardID[1];
        
    }

    //Check clicks done to the cards
    private void OnMouseDown()
    {
        Debug.Log("OnMouseDown");
        originalPosition = transform.position;
        int thisPlayerNumber = DeckController.LocalInstance.thisPlayerNumber;

        // Store the original position when dragging starts
        // Check if the card's parent is PlayerHand1 and the user is Player 1
        if (transform.parent.name == "PlayerHand"+(thisPlayerNumber+1)  && GameManager.currentPlayerNo == thisPlayerNumber) //&& Player.playerID == 1)
        {
            SelectCard();
        }
        else if(transform.parent.name == "Center" && isOneCardSelected)
        {   
            TryToPlayMove();
        }
        else
        {
            Debug.Log("This card is not in your hand or cannot be played.");
            print("Clicked card: " + gameObject.name + ", Parent: " + transform.parent.name + "OneCardSelected: " + isOneCardSelected);
            isOneCardSelected=false;        
        }

        
    }

    /*private void OnMouseDown()
    {
        Debug.Log("OnMouseDown");
        originalPosition = transform.position;
        int thisPlayerNumber = DeckController.LocalInstance.thisPlayerNumber;

        // Store the original position when dragging starts
        // Check if the card's parent is PlayerHand1 and the user is Player 1
        if (transform.parent.name.Contains("PlayerHand"))
        {
            SelectCard();
        }
        else if(transform.parent.name == "Center" && isOneCardSelected)
        {   
            TryToPlayMove();
        }
        else
        {
            Debug.Log("This card is not in your hand or cannot be played.");
            print("Clicked card: " + gameObject.name + ", Parent: " + transform.parent.name + "OneCardSelected: " + isOneCardSelected);
            isOneCardSelected=false;        
        }

        
    }*/

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

        if (distanceMoved > snapBackThreshold)
        {
            if(transform.parent == null)
            {}
            else if(transform.parent.name != "Center" )
            {
                // Call functions when dropped far enough from the original position
                if(!OnCardMoved())
                {
                    // Snap back to original position
                    transform.position = originalPosition;
                    OnCardSnapBack();
                }
            }
            
        }
        else transform.position = originalPosition;
    }

    // Function to handle the card being moved far enough
    private bool OnCardMoved()
    {
        Debug.Log("Card moved far enough. Triggering relevant actions.");
        return AddToCenter();
        // Add your logic here for when the card is moved and dropped
    }

    // Function to handle the card snapping back
    private void OnCardSnapBack()
    {
        Debug.Log("Card snapped back to original position.");
        // Add your logic here for snapping the card back
    }

    public event Action<int[], GameObject> OnCardSelected;
    private void SelectCard()//Event when a card is selected
    {
        isDragging = true;
        GameManager.LocalInstance.DeactivateCardIndicators();
        //print("I am here");
        OnCardSelected?.Invoke(this.cardID, this.gameObject);
        selectedCardIndicator.SetActive(true);
        GameManager.LocalInstance.activeCardIndicatorList.Add(selectedCardIndicator);
        isOneCardSelected=true;
    }

    public delegate bool BoolDelegate();
    public event BoolDelegate OnCardAddedToCenter;
    private bool AddToCenter()//Event when selected card is to be placed in the center
    {
        return OnCardAddedToCenter?.Invoke() ?? false;
    }

    public event Action<int[], GameObject, int> OnCardsPlayed;
    private void TryToPlayMove()//Event when selected cards is to be played
    {
        selectedCardIndicator.SetActive(true);
        GameManager.LocalInstance.activeCardIndicatorList.Add(selectedCardIndicator);
        OnCardsPlayed?.Invoke(this.cardID, this.gameObject, GameManager.currentPlayerNo);
    }

    //Generates the cardId from its name
    private int[] GetCardID()
    {
        string[] tagStrings = gameObject.tag.Split('_');

        if (tagStrings.Length != 2)
        {
            Debug.LogError("Invalid tag format! Expected 'Kind_Value'.");
            return new int[] { 0, 0 };
        }

        int[] cardID = { 0, 0 };

        // Parse the first part of the tag (Kind)
        if (!int.TryParse(tagStrings[0], out cardID[0]) || cardID[0] <= 0)
        {
            Debug.LogError($"Invalid card kind: {tagStrings[0]}");
        }

        // Parse the second part of the tag (Value)
        if (!int.TryParse(tagStrings[1], out cardID[1]) || cardID[1] <= 0 || cardID[1] > 10)
        {
            Debug.LogError($"Invalid card value: {tagStrings[1]}");
        }

        return cardID;
    }

}
