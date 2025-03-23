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
    public static int currentPlayerNo = 5;
    private NetworkRelay networkRelay;
    private List<int[]> centerCardIDList;
    private List<Text> poolTexts = new List<Text>();//Pool of the players in text for debugging
    private GameObject winScreen;
    Text roundOverText;
    public List<int[]> myCards;
    private float turnTimer=0;
    private Text currentPlayerText;
    private Text turnTimerText;
    public List<GameObject> activeCardIndicatorList=new List<GameObject>();
    private List<Text> pointTexts=new List<Text>();
    private List<Transform> timerTransforms = new List<Transform>();
    [SerializeField] public GameObject cardBack;
    [SerializeField] public GameObject cardIndicator;

    void Start()
    {
        print("GameManger Started");

        InitialSetUp();

        GetTurnTimeLocation();
        
        GetPoolTexts();

        networkRelay.NotifyCientConnectedServerRPC(NetworkManager.Singleton.LocalClientId);

        GameObject.Find("Holder").SetActive(false);
    }

    public bool printFlag=false;
    private bool playAfterTimeOutFlag = true;
    void FixedUpdate()
    {
        if(printFlag)
        {
            foreach(int[] cardID in myCards)
            {
                Debug.Log("cardID: " + cardID[0] + "_" + cardID[1]);
            }
            printFlag=false;
            if(currentPlayerNo == deckController.thisPlayerNumber)Invoke("PlayAfterTimeOut",1);
        }


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

    public void PrintFlagMakeTrue()
    {
        printFlag=true;
    }

    public void InitializeCardPrefabs()
    {
        deckController.DeckStart();
        playerChkobba = new List<int>{0,0,0,0};
        if(winScreen.activeSelf)winScreen.SetActive(false);
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
        turnTimerText.text = "";
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
            myCards.Remove(currentSelectedHandCard);
            currentSelectedHandCard = new int[]{0,0};
            currentSelectedCenterCards.Clear();
            DeactivateCardIndicators();
            GetTurnTimeLocation();
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
        //deckController.UpdateCenterCardsLayout();
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
        if(centerCards.Count!=0 && centerCards[centerCards.Count-1][1] == cardID[1])
        {
            CheckIfLegal(playerNumber);
        }
        else
        {
            CardAddedToCenter();
        }
    }

    //Checks if played move is legal before sending it to the server
    public bool CheckIfLegal(int playerNumber)
    {
        if(currentSelectedHandCard[0] == 0 || currentSelectedHandCard[1]==0)return false;
        bool sumFlag=false;
        int selectedCenterCardsSum=0;

        //Calculates the sum and compares it with the played card to check if the move is legal
        /*foreach(int[] cCard in currentSelectedCenterCards)
        {
            selectedCenterCardsSum+=cCard[1];
        }*/

        selectedCenterCardsSum += centerCards[centerCards.Count-1][1];

        
        /*bool CheckPriority()
        {
            foreach (int[] array in centerCardIDList)
            {
                if (array.Length > 1 && array[1] == currentSelectedHandCard[1]) // Check if int[1] exists and matches target
                {
                    return true;
                }
            }
            return false;
        }*/

        if(selectedCenterCardsSum == currentSelectedHandCard[1]) sumFlag=true;
        if(selectedCenterCardsSum > currentSelectedHandCard[1]) DeactivateCardIndicators();
        /*if(currentSelectedCenterCards.Count>1 && CheckPriority())
        {
            sumFlag=false;
            DeactivateCardIndicators();
        }*/

        //Final control to decide if cards can be played
        if(sumFlag)
        {
            List<int[]> cardsToRemove = new List<int[]>();

            // Iterate over the selected center cards and add them to the removal list
            foreach (int[] cardToBeRemoved in centerCards)
            {
                cardsToRemove.Add(cardToBeRemoved);
            }

            // Now remove the cards outside of the foreach loop
            SerializableList serializableList = new SerializableList(cardsToRemove);
            networkRelay.RemoveCenterCardsServerRPC(serializableList);

            //Update the game UI after the move is played
            serializableList = new SerializableList(centerCards);
            networkRelay.SendMoveToServerRPC(currentSelectedHandCard,serializableList,playerNumber,selectedCenterCardsSum);
            myCards.Remove(currentSelectedHandCard);

            //Clear the list even if its a correct move
            currentSelectedHandCard = new int[]{0,0};
            centerCards.Clear();

            DeactivateCardIndicators();
        }

        return sumFlag;
    }

    public bool CheckIfCanBeAddedToCenter()
    {
        /*if(currentSelectedHandCard[0] == 0 || currentSelectedHandCard[1]==0)
        {
            return false;
        }

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
        return !CheckCombinations(centerValues, cardValue);*/

        if(centerCards.Count==0)return true;
        //Plays the card if it can be played
        Debug.LogWarning(currentSelectedHandCard[1]);
        Debug.LogWarning(centerCards[centerCards.Count-1][1]);
        if(currentSelectedHandCard[1] == centerCards[centerCards.Count-1][1])
        {
            CardsPlayed(currentSelectedHandCard, GameObject.FindWithTag(currentSelectedHandCard[0] + "_" + currentSelectedHandCard[1]),currentPlayerNo);
            return false;
        }
        return true;
    }

    public bool CheckIfCanBeAddedToCenter(int[] cardID)
    {
        /*if(cardID[0] == 0 || cardID[1]==0)
        {
            return false;
        }

        int cardValue = cardID[1]; // Get cardID[1]

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
        return !CheckCombinations(centerValues, cardValue);*/
        if(centerCards.Count==0)return true;
        if(cardID[1] == centerCards[centerCards.Count-1][1])return false;
        return true;
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

        foreach (int[] selectedCardId in selectedCenterCards)
        {
            centerCards = centerCards.Where(card => !(card[0] == selectedCardId[0] && card[1] == selectedCardId[1])).ToList();
        }
        
        PrintCenterCards();
        Invoke("UpdateCenterCardsLayout",1f);
        CardInteraction.isOneCardSelected = false;
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
                Debug.LogWarning(childTransform.gameObject.name);
                Debug.LogWarning(childTransform.gameObject.tag);
                
                int.TryParse(tagStrings[0], out cardID[0]);
                int.TryParse(tagStrings[1], out cardID[1]);  
                myCards.Add(cardID);    
            }
            
        }

        bool addedToCenterFlag=false;

        foreach(int[] cardID in myCards)
        {
            if(CheckIfCanBeAddedToCenter(cardID))
            {
                networkRelay.AddCenterCardServerRPC(cardID);
                myCards.Remove(cardID);
                currentSelectedHandCard = new int[]{0,0};
                currentSelectedCenterCards.Clear();
                addedToCenterFlag=true;
                return;
            }
            else playableCardID=cardID;
        }

        if(!addedToCenterFlag)
        {
            currentSelectedCenterCards = GetMatchingCards(playableCardID);
            currentSelectedHandCard = playableCardID;
            CheckIfLegal(currentPlayerNo);
        }
    }

    public List<int[]> GetMatchingCards(int[] currentSelectedHandCard)
    {
        if(currentSelectedHandCard[0]==0 || currentSelectedHandCard[1]==0) return null;

        if (currentSelectedHandCard == null || currentSelectedHandCard.Length < 2)
        {
            Debug.LogError("Invalid card ID provided.");
            return null;
        }

        int targetValue = currentSelectedHandCard[1]; // Get cardID[1]

        // If the selected card's value is zero, return null
        if (targetValue == 0)
        {
            return null;
        }

        // Find combinations of centerCardID[1] values
        for (int i = 0; i < centerCardIDList.Count; i++)
        {
            List<int[]> potentialMatch = new List<int[]>();
            int sum = 0;

            for (int j = i; j < centerCardIDList.Count; j++)
            {
                sum += centerCardIDList[j][1];
                potentialMatch.Add(centerCardIDList[j]);

                if (sum == targetValue)
                {
                    return potentialMatch; // Return the first matching combination
                }

                if (sum > targetValue)
                {
                    break; // Stop this inner loop since the sum exceeded target
                }
            }
        }

        // No matching combination found
        return null;
    }

    public void DeactivateCardIndicators()
    {
        foreach(GameObject indicator in activeCardIndicatorList)
        {
            indicator.SetActive(false);
        }

        currentSelectedHandCard = new int[]{0,0};
        currentSelectedCenterCards.Clear();
        activeCardIndicatorList.Clear();
    }

    public void GetTurnTime(float turnTime)
    {
        turnTimer = turnTime;
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
            //Debug.Log(tempTag);
            poolTexts[i].gameObject.SetActive(false);
        }
    }

    private void InitialSetUp()
    {
        AudioManager.Instance.PlayAudio(4,0.04f,true);

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
        else 
        {
            Debug.LogWarning("Duplicate GameManager detected. Destroying extra instance.");
            //Destroy(gameObject);
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
    }

}
