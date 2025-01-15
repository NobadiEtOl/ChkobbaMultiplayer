using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class GameManager : NetworkBehaviour
{
    public static GameManager LocalInstance { get; private set; }
    [SerializeField]private DeckController deckController;
    private int[] currentSelectedHandCard;//Represents the card current player chose to play with.
    private List<int[]> currentSelectedCenterCards = new List<int[]>();//Represents the card(s) played from the center
    public List<int[]> centerCards = new List<int[]>();//List of cards in the center
    public List<GameObject> centerCardsObjects = new List<GameObject>();//List of the card objects in the center
    private List<CardInteraction> cardInteractionsScripts;//Reference to the scripts of every card.
    private List<GameObject> cardObjectsToBeDiscarted = new List<GameObject>();
    public static int currentPlayerNo;
    private NetworkRelay networkRelay;
    private List<int[]> centerCardIDList;
    private List<Text> poolTexts = new List<Text>();//Pool of the players in text for debugging
    private GameObject winScreen;

    

    void Start()
    {
        print("GameManger Started");

        //Setting the networkRealy script to sen ServerRPCs
        networkRelay = FindObjectOfType<NetworkRelay>();

        //Setting the localInstances for ClientRPC messages
        if(LocalInstance == null)
        {
            LocalInstance = this;
        }
        else 
        {
            Debug.LogWarning("Duplicate GameManager detected. Destroying extra instance.");
            //Destroy(gameObject);
        }

        winScreen = GameObject.FindGameObjectWithTag("WinScreen");
        if(winScreen.activeSelf)winScreen.SetActive(false);
        
        GetPoolTexts();
    }

    private void GetPoolTexts()
    {
        for(int i = 0; i<4; i++)
        {
            string tempTag = "PoolText" + (i+1);
            poolTexts.Add(GameObject.FindGameObjectWithTag(tempTag).GetComponent<Text>());
            //Debug.Log(tempTag);
        }
    }

    public void InitializeCardPrefabs()
    {
        deckController.DeckStart();
    }

    //Puts the proper card objects in front of correct players    
    public void CardPrefabsToPlayers(int playerCount,SerializableDictionary serializableDictionary)
    {
        Dictionary<int, List<int[]>> playerHands = serializableDictionary.ToDictionary();

        if(deckController)deckController.DealPlayers(playerCount,playerHands);
        else Debug.Log("burda hata");
    }

    //Puts the proper card objects to the center
    public void CardPrefabsToCenter(SerializableList serializableList)
    {   
        List<int[]> centerCardIDs = serializableList.ToList();

        if(deckController)deckController.DealCenter(centerCardIDs);
        else Debug.Log("burda başka hata");
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
        currentSelectedCenterCards.Clear();
        currentSelectedHandCard=cardID;
    }

    //Called when the player decides to put the selected hand card to the center
    private bool CardAddedToCenter()
    {
        if(CheckIfCanBeAddedToCenter())
        {
            networkRelay.AddCenterCardServerRPC(currentSelectedHandCard);
            currentSelectedHandCard = new int[]{0,0};
            currentSelectedCenterCards.Clear();
            return true;
        }
        else return false;
    }

    public void GetCardAddedToCenter(int[] cardID)
    {
        DiscardHandCards(cardID);
    }

    //To remove the played card from the hand when it played to the center
    public void DiscardHandCards(int[] cardID)
    {
        //UI
        deckController.DiscardHandCardToCenter(cardID);
    }

    public void UpdateCenterCardsLayout()
    {
        //UI
        deckController.UpdateCenterCardsLayout();
    }

    //Called when the player tries to play the selected card with one or two center cards
    private void CardsPlayed(int[] cardID, GameObject cardObject,int playerNumber)
    {
        //UI
        //Logic
        if (!currentSelectedCenterCards.Contains(cardID))
        {
            currentSelectedCenterCards.Add(cardID);
        }
        CheckIfLegal(playerNumber);
    }

    //Checks if played move is legal before sending it to the server
    public bool CheckIfLegal(int playerNumber)
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

            //Update the game UI after the move is played
            serializableList = new SerializableList(currentSelectedCenterCards);
            networkRelay.SendMoveToServerRPC(currentSelectedHandCard,serializableList,playerNumber);

            //Clear the list even if its a correct move
            currentSelectedHandCard = new int[]{0,0};
            currentSelectedCenterCards.Clear();
        }

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

        // Check against all combinations of centerCardID[1] values
        List<int> centerValues = new List<int>();
        foreach (int[] centerCardID in centerCardIDList)
        {
            centerValues.Add(centerCardID[1]);
        }

        // Use a recursive method to check all combinations
        return !CheckCombinations(centerValues, cardValue);
    }

    // Helper function to check all combinations
    private bool CheckCombinations(List<int> values, int target, int start = 0, int currentSum = 0, int depth = 0)
    {
        if (currentSum == target && depth > 0) // Ensure at least one value is used
        {
            return true;
        }

        for (int i = start; i < values.Count; i++)
        {
            if (CheckCombinations(values, target, i + 1, currentSum + values[i], depth + 1))
            {
                return true;
            }
        }

        return false;
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
                cardObjectsToBeDiscarted.Add(tempCardObject);
            }
            else
            {
                Debug.LogWarning("No card found with the tag: " + cardID[0] + "_" + cardID[1]);
            }
        }

        deckController.MoveCardsToPlayerPool(cardObjectsToBeDiscarted,playerNumber);
        //Removes the card objects from the center and player hand
        /*foreach(GameObject cardObject in cardObjectsToBeDiscarted)
        {
            cardObject.transform.position = new Vector3(-10000,-10000,0);
            centerCardsObjects.Remove(cardObject);
            cardObject.transform.parent = null;
        }*/
        
        //Removes the selectedCenterCards from the centerCards list
        foreach (int[] selectedCardId in selectedCenterCards)
        {
            centerCards = centerCards.Where(card => !(card[0] == selectedCardId[0] && card[1] == selectedCardId[1])).ToList();
        }
        
        PrintCenterCards();
        UpdateCenterCardsLayout();
        CardInteraction.isOneCardSelected = false;
    }
    
    public void UpdateCurrentPlayer(int playerNumber)
    {
        currentPlayerNo=playerNumber;
    }
    public void UpdateCenterCardIDList(SerializableList serializableList)
    {
        List<int[]> tempCenterList = serializableList.ToList();
        centerCardIDList = tempCenterList;
        Server.PrintList(centerCardIDList);
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

    List<int> playerChkobba = new List<int>{0,0,0,0};
    public void PrintPlayerPools(SerializableDictionary serializableDictionary, int chkobbaPlayer)
    {
        Dictionary<int, List<int[]>> playersPooledCardsIDs = serializableDictionary.ToDictionary();

        foreach (var kvp in playersPooledCardsIDs)
        {
            int playerKey = kvp.Key;
            List<int[]> cardList = kvp.Value;

            // Updating the poolTexts UI with player pools
            if (playerKey < poolTexts.Count)
            {
                if(playerKey==chkobbaPlayer)playerChkobba[playerKey]++;
                string cardRepresentation = string.Join(", ", cardList.Select(card => $"[{card[0]}_{card[1]}]"));
                poolTexts[playerKey].text = $"Player {playerKey}: {cardRepresentation}//ChkobbaCount: {playerChkobba[playerKey]}";
            }

            foreach (var card in cardList)
            {
                string cardRepresentation = string.Join(", ", card);
            }
        }
    }

    public void ShowWinScreen(string message)
    {
        winScreen.SetActive(true);
        Text roundOverText = GameObject.FindGameObjectWithTag("RoundOverText").GetComponent<Text>();
        roundOverText.text = message;
    }
    
    public void GetPlayerNumber(int playerNumber)
    {
        deckController.SetPlayerNumber(playerNumber);
    }

    public void AskPlayerNumber()
    {
        networkRelay.AskPlayerNumberServerRPC();
    }
}
