using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    public static GameManager LocalInstance { get; private set; }
    [SerializeField]private DeckController deckController;
    private int[] currentSelectedHandCard;//Represents the card current player chose to play with.
    private GameObject currentlySelectedHandCardObject;//Represents the card object current player chose to play with
    private List<int[]> currentSelectedCenterCards = new List<int[]>();//Represents the card(s) played from the center
    public List<int[]> centerCards = new List<int[]>();//List of cards in the center
    public List<GameObject> centerCardsObjects = new List<GameObject>();//List of the card objects in the center
    private List<CardInteraction> cardInteractionsScripts;//Reference to the scripts of every card.
    private List<GameObject> cardObjectsToBeDiscarted = new List<GameObject>();
    public static int currentPlayerNo;
    private NetworkRelay networkRelay;
    private List<int[]> centerCardIDList;

    void Start()
    {
        print("GameManger Started");
        //Setting the localInstances for ClientRPC messages
        if(LocalInstance == null)
        {
            LocalInstance = this;
        }
        else 
        {
            Debug.LogWarning("Duplicate GameManager detected. Destroying extra instance.");
            Destroy(gameObject);
        }

        //Setting the networkRealy script to sen ServerRPCs
        networkRelay = FindObjectOfType<NetworkRelay>();
        if (networkRelay != null)
        {   
            print("in here");
            networkRelay.PrintMessageServerRPC("message sent");
        }
        else
        {
            print("NetworkRelay is null.");
        }
        
        networkRelay.AddGameManager(this); 
    }

    public void GetLocalInstance()
    {
        LocalInstance = this;
    }
    void Update()
    {

    }
    public void InitializeCardPrefabs()
    {
        deckController.DeckStart();
    }

    //Puts the proper card objects in front of correct players
    
    public void CardPrefabsToPlayers(int playerCount,SerializableDictionary serializableDictionary)
    {
        Debug.Log("Inside DealCardPrefabsToPlayers GameManager");
        Dictionary<int, List<int[]>> playerHands = serializableDictionary.ToDictionary();
        //Server.PrintDictionary(playerHands);
        if(deckController)deckController.DealPlayers(playerCount,playerHands);
        else Debug.Log("burda hata");
    }

    //Puts the proper card objects to the center
    public void CardPrefabsToCenter(SerializableList serializableList)
    {   
        Debug.Log("Inside DealCardPrefabsToCenter GameManager");
        List<int[]> centerCardIDs = serializableList.ToList();
        //Server.PrintList(centerCardIDs);
        if(deckController)deckController.DealCenter(centerCardIDs);
        else Debug.Log("burda başka hata");
    }

    //To remove the played card from the hand when it played to the center
    public void DiscardHandCards(int[] cardID)
    {
        GameObject tempCardObject = GameObject.FindWithTag(TurnCardIdToString(cardID));
        tempCardObject.transform.position = new Vector3(-10000,-10000,0);
        tempCardObject.transform.parent = null;
        Destroy(tempCardObject);
    }

    //Gets all the scripts of the cards from the deckController
    public void GetCardInteractionScripts(List<CardInteraction> cardScripts)
    {
        cardInteractionsScripts = cardScripts;
        SubscribeToEvents();
    }

    //Subscribes to each of the cards events
    private void SubscribeToEvents()
    {
        // Iterate through the list of CardInteraction scripts
        foreach (var cardInteraction in cardInteractionsScripts)
        {
            cardInteraction.OnCardSelected += CardSelected;
            cardInteraction.OnCardAddedToCenter += CardAddedToCenter;
            cardInteraction.OnCardsPlayed += CardsPlayed;
        }
    }

    //Called when a card is selected in the player's hand
    private void CardSelected(int[] cardID, GameObject cardObject)
    {
        //print("cardSelected");
        //Reset all the lists for preparation
        cardObjectsToBeDiscarted.Clear();
        cardObjectsToBeDiscarted.Add(cardObject);
        currentSelectedCenterCards.Clear();

        //Debug.Log("Card selected: " + cardObject.name);
        //Set both cardID and its cardObject
        currentSelectedHandCard=cardID;
        currentlySelectedHandCardObject=cardObject;
    }

    //Called when the player decides to put the selected hand card to the center
    private void CardAddedToCenter()
    {
        if(CheckIfCanBeAddedToCenter())
        {
            networkRelay.AddCenterCardServerRPC(currentSelectedHandCard);
            currentSelectedHandCard = new int[]{0,0};
            currentSelectedCenterCards.Clear();
        }
    }

    public void GetCardAddedToCenter(int[] cardID)
    {
        AddCardToCenter(cardID);
        DiscardHandCards(cardID);
    }
    //To add the proper cardObject to the center when a card from the player hand gets placed
    public void AddCardToCenter(int[] cardID)
    {
        List<Vector3> centerCardLocations = new List<Vector3>();

        if(centerCardsObjects.Count == 0)Debug.LogError("cetnerCardsObjects empty");
        foreach(GameObject centerCard in centerCardsObjects)
        {
            //Gets all of the center card objects position
            centerCardLocations.Add(centerCard.transform.position);
        }
        
        bool locationFlag=false;
        int locationCounter=0;
        while(!locationFlag)
        {
            //Send the position of where the card object will be placed
            if(!centerCardLocations.Contains(new Vector3(locationCounter * 700, 0, 0)))
            {
                deckController.PlaceCardToCenter(new Vector3(locationCounter * 700, 0, 0),cardID);
                locationFlag=true;
            }
            locationCounter++;
        } 
    }

    //Called when the player tries to play the selected card with one or two center cards
    private void CardsPlayed(int[] cardID, GameObject cardObject,int playerNumber)
    {
        //UI
        //Logic
        cardObjectsToBeDiscarted.Add(cardObject);
        currentSelectedCenterCards.Add(cardID);
        print("Move:");
        print(currentSelectedHandCard[0]+"_"+currentSelectedHandCard[1]);
        print(cardID[0] + "_" + cardID[1]);
        CheckIflegal(playerNumber);
    }

    //Checks if played move is legal before sending it to the server
    public bool CheckIflegal(int playerNumber)
    {
        if(currentSelectedHandCard[0] == 0 || currentSelectedHandCard[1]==0)Debug.LogError("Something went horribly wrong");
        bool sumFlag=false;
        int selectedCenterCardsSum=0;

        //Calculates the sum and compares it with the played card to check if the move is legal
        foreach(int[] cCard in currentSelectedCenterCards)
        {
            selectedCenterCardsSum+=cCard[1];
            if(cCard[1] == currentSelectedHandCard[1])
            {
                sumFlag=true;
            }
            else sumFlag=false;
        }

        if(selectedCenterCardsSum == currentSelectedHandCard[1]) sumFlag=true;

        //Final control to decide if cards can be played
        if(sumFlag || currentSelectedCenterCards.Count==2)
        {
            if(sumFlag)
            {
                List<int[]> cardsToRemove = new List<int[]>();

                // Iterate over the selected center cards and add them to the removal list
                foreach (int[] cardToBeRemoved in currentSelectedCenterCards)
                {
                    cardsToRemove.Add(cardToBeRemoved);
                }

                // Now remove the cards outside of the foreach loop
                SerializableList serializableList = new SerializableList(cardsToRemove);
                networkRelay.RemoveCenterCardsServerRPC(serializableList);
                serializableList = new SerializableList(currentSelectedCenterCards);
                //Update the game UI after the move is played
                networkRelay.SendMoveToServerRPC(currentSelectedHandCard,serializableList,playerNumber);
                //DiscardPlayedCards(currentSelectedHandCard, currentSelectedCenterCards, playerNumber);//Discard played cards only if sums match up
                
            }
            //Clear the list even if its a correct move
            currentSelectedHandCard = new int[]{0,0};
            currentSelectedCenterCards.Clear();
        }

        //print("Move is legal:");
        //print(sumFlag);

        //print("CurrentSelectedCenterCards:");
        //print(currentSelectedCenterCards.Count);
        return sumFlag;
    }

    public bool CheckIfCanBeAddedToCenter()
    {
        int cardValue = currentSelectedHandCard[1]; // Get cardID[1]

        // Check against singular centerCardID[1] values
        foreach (int[] centerCardID in centerCardIDList)
        {
            if (cardValue == centerCardID[1])
            {
                return false;
            }
        }

        // Check against the sum of two different centerCardID[1] values
        for (int i = 0; i < centerCardIDList.Count; i++)
        {
            for (int j = i + 1; j < centerCardIDList.Count; j++)
            {
                if (i != j) // Ensure i and j are different indices
                {
                    int sum = centerCardIDList[i][1] + centerCardIDList[j][1];
                    if (cardValue == sum)
                    {
                        return false;
                    }
                }
            }
        }

        // If no match found, return true
        return true;
    }

    public void DiscardPlayedCards(int[] playedCard, SerializableList serializedList, int playerNumber)
    {
        List<int[]> selectedCenterCards = serializedList.ToList();
        List<int[]> selectedCards = new List<int[]>(selectedCenterCards);
        selectedCards.Add(playedCard);
        cardObjectsToBeDiscarted.Clear();
        foreach(int[] cardID in selectedCards)
        {
            GameObject tempCardObject = GameObject.FindWithTag(cardID[0] + "_" + cardID[1]);
            if(tempCardObject != null)
            {
                Debug.Log("Found card with tag:" + cardID[0] + "_" + cardID[1] + "and name: " + tempCardObject.name);
                cardObjectsToBeDiscarted.Add(tempCardObject);
            }
            else
            {
                Debug.LogWarning("No card found with the tag: " + cardID[0] + "_" + cardID[1]);
            }
        }
        //Removes the card objects from the center and player hand
        if(cardObjectsToBeDiscarted.Count==0 ||cardObjectsToBeDiscarted == null)
        {
            print("cardObjectsToBeDiscarted is empty");
        }
        else
        {
            print("cardObjectsToBeDiscarted.Count(): ");
            print(cardObjectsToBeDiscarted.Count());
        } 
        foreach(GameObject cardObject in cardObjectsToBeDiscarted)
        {
            cardObject.transform.position = new Vector3(-10000,-10000,0);
            if(centerCardsObjects.Remove(cardObject))print("yes");
            else print("no");
            cardObject.transform.parent = null;
        }
        
        //print("SelectedCenterCards.Count: " + selectedCenterCards.Count);
        //Removes the selectedCenterCards from the centerCards list
        print("centerCards.Count: " + centerCards.Count);
        foreach (int[] selectedCardId in selectedCenterCards)
        {
            centerCards = centerCards.Where(card => !(card[0] == selectedCardId[0] && card[1] == selectedCardId[1])).ToList();
        }
        print("centerCards.Count: " + centerCards.Count);

        if(centerCards.Count == 0)
        {
            networkRelay.PlayerChkobbaServerRPC(playerNumber);
        }
        PrintCenterCards();
        //Combine the selected hand cards with the selected center card(s)
        selectedCenterCards.Add(playedCard);
        SerializableList serializableList = new SerializableList(selectedCenterCards);
        //networkRelay.EndTurnAfterPlayServerRPC(playerNumber,serializableList);
        CardInteraction.isOneCardSelected = false;
    }
    
    public void UpdateCurrentPlayer(int playerNumber)
    {
        Debug.Log("Inside UpdateCurrentPlayer");
        currentPlayerNo=playerNumber;
        Debug.Log("currentPlayerNo: " + currentPlayerNo);
    }
    public void UpdateCenterCardIDList(SerializableList serializableList)
    {
        Debug.Log("Inside UpdateCenterCardIDList");
        List<int[]> tempCenterList = serializableList.ToList();
        centerCardIDList = tempCenterList;
        Server.PrintList(centerCardIDList);
        Debug.Log("Ending UpdateCenterCardIDList");
    }

    public static string TurnCardIdToString(int[] tempCardID)
    {
        return tempCardID[0] + "_" + tempCardID[1];
    }

    public void PrintCenterCards()
    {
        foreach(int[] centerCardID in centerCards)
        {
            print(centerCardID[0]);
            print(centerCardID[1]);
        }
    }
}
