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
    public bool isPlayable = true; // Condition to check if the card can be played when the server is iplemented

    void Start()
    {
        isOneCardSelected=false;
        //Create IDs for every card except for the add button
        if(gameObject.name!="AddCardButton(Clone)")
        {
            string[] temp = gameObject.name.Split("_");
            cardID = GetCardID(temp);
        }
        else cardID = new int[]{5,1};//Add button has a special ID.

        gameObject.tag = cardID[0] + "_" + cardID[1];
        
    }

    //Check clicks done to the cards
    private void OnMouseDown()
    {
        Debug.Log("OnMouseDown");
        // Check if the card's parent is PlayerHand1 and the user is Player 1
        if (transform.parent.name == "PlayerHand1" && GameManager.currentPlayerNo == 0) //&& Player.playerID == 1)
        {
            // The user is Player 1 and the card is in their hand
            if (isPlayable)
            {
                SelectCard();
            }
        }
        // Similarly, you can check for Player 2
        else if (transform.parent.name == "PlayerHand2" && GameManager.currentPlayerNo == 1) //&& Player.playerID == 1)
        {
            if (isPlayable)
            {
                SelectCard();
            }

        }
        //Player 3
        else if (transform.parent.name == "PlayerHand3" && GameManager.currentPlayerNo == 2) //&& Player.playerID == 1)
        {
            if (isPlayable)
            {
                SelectCard();
            }

        }
        //Player 4
        else if (transform.parent.name == "PlayerHand4" && GameManager.currentPlayerNo == 3) //&& Player.playerID == 1)
        {
            if (isPlayable)
            {
                SelectCard();
            }

        }
        else if(transform.parent.name == "Center" && isOneCardSelected)
        {   
            if(gameObject.name == "AddCardButton(Clone)")
            {
                print("clicked to the add.");
                AddToCenter();
            }
            else
            {
                TryToPlayMove();
            }
        }
        else
        {
            Debug.Log("This card is not in your hand or cannot be played.");
            print("Clicked card: " + gameObject.name + ", Parent: " + transform.parent.name + "OneCardSelected: " + isOneCardSelected);
            isOneCardSelected=false;        
        }

        
    }

    public event Action<int[], GameObject> OnCardSelected;
    private void SelectCard()//Event when a card is selected
    {
        //print("I am here");
        OnCardSelected?.Invoke(this.cardID, this.gameObject);
        isOneCardSelected=true;
    }

    public event Action OnCardAddedToCenter;
    private void AddToCenter()//Event when selected card is to be placed in the center
    {
        OnCardAddedToCenter?.Invoke();
    }

    public event Action<int[], GameObject, int> OnCardsPlayed;
    private void TryToPlayMove()//Event when selected cards is to be played
    {
        OnCardsPlayed?.Invoke(this.cardID, this.gameObject, GameManager.currentPlayerNo);
    }

    //Generates the cardId from its name
    private int[] GetCardID(string[] cardName)
    {
        if(cardName[0] == "Carreau") cardID[0]=1;
        else if(cardName[0] == "Coeur") cardID[0]=2;
        else if(cardName[0] == "Pique") cardID[0]=3;
        else if(cardName[0] == "Trefle") cardID[0]=4;
        else Debug.LogError("Card kind cant be identified");

        string cardValue = cardName[1].Split("(")[0];

        Int32.TryParse(cardValue, out cardID[1]);
        if(cardID[1] <= 0 || cardID[1] > 10) Debug.LogError("Card value cannot be identified");

        return new int[] { cardID[0], cardID[1]};
    }
}
