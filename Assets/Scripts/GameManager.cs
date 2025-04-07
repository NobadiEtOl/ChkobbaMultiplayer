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
    public List<int[]> centerCards = new List<int[]>();//List of cards in the center
    public List<GameObject> centerCardsObjects = new List<GameObject>();//List of the card objects in the center
    private List<CardInteraction> cardInteractionsScripts;//Reference to the scripts of every card.
    private List<GameObject> cardObjectsToBeDiscarted = new List<GameObject>();
    public static int currentPlayerNo = 0;
    private NetworkRelay networkRelay;
    private List<int[]> centerCardIDList;
    private List<Text> poolTexts = new List<Text>();//Pool of the players in text for debugging
    private GameObject winScreen;
    Text roundOverText;
    public List<int[]> myCards;
    private float turnTimer=0;
    private Text currentPlayerText;
    private Text turnTimerText;
    private List<Text> pointTexts=new List<Text>();
    private List<Transform> timerTransforms = new List<Transform>();
    [SerializeField] public GameObject cardBack;
    [SerializeField] public GameObject cardIndicator;
    [SerializeField] private List<Transform> playerHandTransforms;
    [SerializeField] private List<Transform> playerPoolTransforms;
    [SerializeField] private Transform centerTransform;

    void Start()
    {
        InitialGameManagerSetUp();//Identifies and assigns necessary variables and calls other functions

        networkRelay.NotifyCientConnectedServerRPC(NetworkManager.Singleton.LocalClientId);// Tells the server that a client is started
    }

    void FixedUpdate()
    {
        if(turnTimer>15)
        {
            turnTimer-=Time.deltaTime;
        }
        else if(turnTimer>0)
        {
            turnTimer-=Time.deltaTime;
            turnTimerText.text = Mathf.RoundToInt(turnTimer).ToString();
        }
    }

    //Gets message from the server to start the deck and the cards
    public void InitializeCardPrefabs()
    {
        deckController.DeckStart();
        if(winScreen.activeSelf)winScreen.SetActive(false);
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
            cardInteraction.OnCardsPlayed += CardsPlayed;
        }
    }

    //Gets message from the server to start dealing cards to players   
    public void CardPrefabsToPlayers(int playerCount,SerializableDictionary serializableDictionary)
    {
        //Converts serializablelist to a normal dictionary
        Dictionary<int, List<int[]>> playerHands = serializableDictionary.ToDictionary();

        //Informs the deckController to deal the players' cards
        if(deckController)deckController.DealPlayers(playerCount,playerHands);
    }

    //Gets message from the server to start dealing cards to center
    public void CardPrefabsToCenter(SerializableList serializableList)
    {   
        //Converts serializablelist to a normal list
        List<int[]> centerCardIDs = serializableList.ToList();

        //Informs the deckController to deal the center cards
        if(deckController)deckController.DealCenter(centerCardIDs);
        turnTimerText.text = "";
    }

    //Called when a card is selected in the player's hand
    private void CardSelected(int[] cardID)
    {
        currentSelectedHandCard = cardID;
    }

    public void GetCardAddedToCenter(int[] cardID)
    {
        DiscardHandCards(cardID);
    }

    public void UpdateCurrentPlayerHandLayoutCall()
    {
        //UI
        deckController.UpdateCurrentPlayerHandLayout();
    }

    //Called when the player tries to play the selected card with one or two center cards
    private void CardsPlayed(int[] cardID, GameObject cardObject,int playerNumber)
    {
        //UI
        //Logic
        if(centerCards.Count!=0 && (centerCards[centerCards.Count-1][1] == cardID[1] || 11 == cardID[1]))
        {
            CheckIfLegal(playerNumber);
        }
        else
        {
            CardAddedToCenter();
        }
    }

    //Checks if played move is legal before sending it to the server
    public void CheckIfLegal(int playerNumber)
    {
        //if(currentSelectedHandCard[0] == 0 || currentSelectedHandCard[1]==0)return false;

        //Final control to decide if cards can be played
        if(centerCards[centerCards.Count-1][1] == currentSelectedHandCard[1] || currentSelectedHandCard[1] == 11)
        {
            List<int[]> cardsToRemove = new List<int[]>();

            // Iterate over the selected center cards and add them to the removal list
            foreach (int[] cardToBeRemoved in centerCards)
            {
                cardsToRemove.Add(cardToBeRemoved);
            }

            SerializableList serializableList = new SerializableList(centerCards);

            //Update the game UI after the move is played
            networkRelay.SendMoveToServerRPC(currentSelectedHandCard,serializableList,playerNumber,centerCards[centerCards.Count-1][1]);
            myCards.Remove(currentSelectedHandCard);

            //Clear the list even if its a correct move
            currentSelectedHandCard = new int[]{0,0};
            centerCards.Clear();
        }
    }

    //Called when the player decides to put the selected hand card to the center
    private void CardAddedToCenter()
    {
        networkRelay.AddCenterCardServerRPC(currentSelectedHandCard);
        myCards.Remove(currentSelectedHandCard);
        currentSelectedHandCard = null;
        GetTurnTimeLocation();
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

        foreach (int[] selectedCardId in selectedCenterCards)
        {
            centerCards = centerCards.Where(card => !(card[0] == selectedCardId[0] && card[1] == selectedCardId[1])).ToList();
        }
        
        PrintCenterCards();
        CardInteraction.isOneCardSelected = false;
    }
    
    //To remove the played card from the hand when it played to the center
    public void DiscardHandCards(int[] cardID)
    {
        //UI
        deckController.DiscardHandCardToCenter(cardID);
    }
    
    public void UpdateCurrentPlayer(int playerNumber)
    {
        currentPlayerNo=playerNumber;
        currentPlayerText.text = "Current Player: " + (currentPlayerNo+1);
        GetTurnTimeLocation();
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
    //!!!! Maybe can be deleted later or deactivated.
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

    public void ShowWinScreen(string message, int winnerSide, int point0, int point1)
    {
        
        turnTimerText.text = "";

        UpdatePointText(point0,point1);

        winScreen.SetActive(true);
        roundOverText.text = message;

        if(winnerSide != -1)
        {
            if(winnerSide==3)
            {   
                //Draw
                AudioManager.Instance.PlayAudio(2,0.5f,false);
            }
            else if(deckController.thisPlayerNumber==0 || deckController.thisPlayerNumber==2)
            {
                //Side 0 win or lose
                if(winnerSide==0)AudioManager.Instance.PlayAudio(2,0.5f,false);
                else AudioManager.Instance.PlayAudio(1,0.5f,false);
            }
            else if(deckController.thisPlayerNumber==1 || deckController.thisPlayerNumber==3)
            {
                //Side 1 win or lose
                if(winnerSide==1)AudioManager.Instance.PlayAudio(2,0.5f,false);
                else AudioManager.Instance.PlayAudio(1,0.5f,false);
            }
        }
    
    }

    private void UpdatePointText(int point0, int point1)
    {
        if(deckController.thisPlayerNumber==0 ||deckController.thisPlayerNumber==2)
        {
            pointTexts[0].text = point0.ToString();
            pointTexts[1].text = point1.ToString();
        }
        
        else if(deckController.thisPlayerNumber==1 ||deckController.thisPlayerNumber==3)
        {
            pointTexts[1].text = point0.ToString();
            pointTexts[0].text = point1.ToString();     
        }
    }
    
    public void GetPlayerNumber(int playerNumber)
    {
        deckController.SetPlayerNumber(playerNumber);
    }

    public void AskPlayerNumber()
    {
        networkRelay.AskPlayerNumberServerRPC();
    }

    private void PlayAfterTimeOut()
    {
        int[] playableCardID={0,0};

        List<int[]> myCards = new List<int[]>();

        foreach(Transform childTransform in GameObject.Find("PlayerHand"+(deckController.thisPlayerNumber+1)).transform)
        {   
            string[] tagStrings = childTransform.gameObject.tag.Split('_');
            if(tagStrings.Length == 2)
            {
                int[] cardID = {0,0};
                
                int.TryParse(tagStrings[0], out cardID[0]);
                int.TryParse(tagStrings[1], out cardID[1]);  
                myCards.Add(cardID);    
            }
            
        }

        bool addedToCenterFlag=false;

        foreach(int[] cardID in myCards)
        {
            if(cardID[1] != centerCards[centerCards.Count-1][1])
            {
                currentSelectedHandCard = cardID;
                CardAddedToCenter();
                addedToCenterFlag=true;
                return;
            }
            else playableCardID=cardID;
        }

        if(!addedToCenterFlag)
        {
            currentSelectedHandCard = playableCardID;
            CheckIfLegal(currentPlayerNo);
        }
    }

    public void GetTurnTime(float turnTime)
    {
        turnTimer = turnTime;
    }

    private void InitialGameManagerSetUp()
    {
        AudioManager.Instance.PlayAudio(4,0.04f,true);

        playerChkobba = new List<int>{0,0,0,0};

        //Setting the networkRealy script to sen ServerRPCs
        networkRelay = FindObjectOfType<NetworkRelay>();

        pointTexts.Add(GameObject.Find("Point0").GetComponent<Text>());
        pointTexts.Add(GameObject.Find("Point1").GetComponent<Text>());

        if(pointTexts == null || pointTexts.Count == 0)Debug.LogError("point text empty");

        //Setting the localInstances for ClientRPC messages
        if(LocalInstance == null)
        {
            LocalInstance = this;
        }

        winScreen = GameObject.FindGameObjectWithTag("WinScreen");

        roundOverText = GameObject.FindGameObjectWithTag("RoundOverText").GetComponent<Text>();
        roundOverText.text = "Connected \n\n\n Waiting For Game To Start";
        currentPlayerText = GameObject.Find("CurrentPlayerText").GetComponent<Text>();

        turnTimerText = GameObject.Find("TurnTimer").GetComponent<Text>();

        timerTransforms.Add(GameObject.Find("Timer0").GetComponent<Transform>());
        timerTransforms.Add(GameObject.Find("Timer1").GetComponent<Transform>());
        timerTransforms.Add(GameObject.Find("Timer2").GetComponent<Transform>());
        timerTransforms.Add(GameObject.Find("Timer3").GetComponent<Transform>());
        playerHandTransforms.Add(GameObject.Find("PlayerHand1").GetComponent<Transform>());
        playerHandTransforms.Add(GameObject.Find("PlayerHand2").GetComponent<Transform>());
        playerHandTransforms.Add(GameObject.Find("PlayerHand3").GetComponent<Transform>());
        playerHandTransforms.Add(GameObject.Find("PlayerHand4").GetComponent<Transform>());
        playerPoolTransforms.Add(GameObject.Find("PlayerPool1").GetComponent<Transform>());
        playerPoolTransforms.Add(GameObject.Find("PlayerPool2").GetComponent<Transform>());
        playerPoolTransforms.Add(GameObject.Find("PlayerPool3").GetComponent<Transform>());
        playerPoolTransforms.Add(GameObject.Find("PlayerPool4").GetComponent<Transform>());
        centerTransform = GameObject.Find("Center").GetComponent<Transform>();
        deckController.getPlayerHandTransforms(playerHandTransforms, playerPoolTransforms, centerTransform);

        GetTurnTimeLocation();

        //GetPoolTexts();

        GameObject.Find("StartScreen").SetActive(false);
    }

    private void GetTurnTimeLocation()
    {
        //Decide placerment according to the number of players
        if(deckController.playerCount == 2)
        {
            int relativeIndex = (currentPlayerNo - deckController.thisPlayerNumber + 2) % 2;
            if(relativeIndex == 0)
            {
                turnTimerText.transform.position = timerTransforms[0].position;
            }
            else if(relativeIndex == 1)
            {
                turnTimerText.transform.position = timerTransforms[2].position;
            }
        }

        else if(deckController.playerCount == 4)
        {
            int relativeIndex = (currentPlayerNo - deckController.thisPlayerNumber + 4) % 4;
            turnTimerText.transform.position = timerTransforms[relativeIndex].position;
        }
    }

    private void GetPoolTexts()
    {
        for(int i = 0; i<4; i++)
        {
            string tempTag = "PoolText" + (i+1);
            poolTexts.Add(GameObject.FindGameObjectWithTag(tempTag).GetComponent<Text>());
            poolTexts[i].gameObject.SetActive(false);
        }
    }

    public void SkipTurn()
    {
        if(currentPlayerNo == deckController.thisPlayerNumber)Invoke("PlayAfterTimeOut",1);
    }

}
