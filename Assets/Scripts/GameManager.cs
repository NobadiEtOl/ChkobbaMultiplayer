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
    //Scripts
    public static GameManager LocalInstance { get; private set; }
    [SerializeField] private DeckController deckController;
    private NetworkRelay networkRelay;
    //Card Variables
    [SerializeField] public GameObject cardBack;
    private string currentSelectedHandCard;//Represents the card current player chose to play with.
    public Dictionary<string, int[]> centerCards = new Dictionary<string, int[]>();//List of cards in the center
    public List<GameObject> centerCardsObjects = new List<GameObject>();//List of the card objects in the center
    private List<CardInteraction> cardInteractionsScripts;//Reference to the scripts of every card.
    private List<GameObject> cardObjectsToBeDiscarted = new List<GameObject>();
    public static int currentPlayerNo = 0;
    private List<string> centerCardIDList;
    public List<string> myCards;
    private GameObject winScreen;
    Text roundOverText;
    private float turnTimer = 0;
    private List<Text> pointTexts = new List<Text>();
    public Sprite cardBackSprite;
    [SerializeField] public GameObject cardIndicator;
    [SerializeField] private List<Transform> playerHandTransforms;
    [SerializeField] private List<Transform> playerPoolTransforms;
    [SerializeField] private List<Transform> playerPiştiPoolTransforms;
    [SerializeField] private Transform centerTransform;
    bool alreadySubbed = false;
    private bool movePlayedLocally = false;
    private GameObject waitingScreen;
    private GameObject mainScreen;

    public void ResetForNewRound()
    {
        currentSelectedHandCard = null;
        centerCards.Clear();
        centerCardsObjects.Clear();
        //if (cardInteractionsScripts != null) cardInteractionsScripts.Clear();
        cardObjectsToBeDiscarted.Clear();
        if (centerCardIDList != null) centerCardIDList.Clear();
        if (myCards != null) myCards.Clear();
        turnTimer = 0f;
        movePlayedLocally = false;
        // Set currentPlayerNo to the

        if (roundCount != 0)
        {
            foreach (var cardScript in cardInteractionsScripts)
            {
                if (cardScript != null && cardScript.gameObject != null)
                {
                    string[] tagParts = cardScript.gameObject.tag.Split('_');
                    if (tagParts.Length == 2 && int.TryParse(tagParts[1], out int value) && value == 11)
                    {
                        var indicator = cardScript.transform.Find("CardIndicator(Clone)");
                        if (indicator != null)
                            indicator.gameObject.SetActive(false);
                    }
                }
            }
        }

    }

    void Awake()
    {
        if (LocalInstance != null && LocalInstance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        LocalInstance = this;
        DontDestroyOnLoad(this.gameObject); // Optional, if you want it to persist
    }

    void Start()
    {
        Debug.Log("GameManager started");
        InitialGameManagerSetUp();//Identifies and assigns necessary variables and calls other functions

        networkRelay.NotifyCientConnectedServerRPC(NetworkManager.Singleton.LocalClientId);// Tells the server that a client is started
    }

    private CardInteraction tempCard;
    void Update()
    {
        // Handle touch input (mobile)
        /*if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector3 touchPosition = touch.position;

            Ray ray = Camera.main.ScreenPointToRay(touchPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                CardInteraction card = hit.collider.GetComponent<CardInteraction>();
                if (card != null)
                {
                    switch (touch.phase)
                    {
                        case TouchPhase.Began:
                            card.OnCardTouched(touchPosition);
                            break;
                        case TouchPhase.Moved:
                            if (CardInteraction.currentlySelectedCard == card)
                            {
                                card.OnTouchDrag(touchPosition); // If you have this method/event
                            }
                            break;
                        case TouchPhase.Ended:
                        case TouchPhase.Canceled:
                            if (CardInteraction.currentlySelectedCard == card)
                            {
                                card.OnTouchUp(); // If you have this method/event
                                CardInteraction.currentlySelectedCard = null; // Reset the currently selected card
                            }
                            break;
                    }
                }
            }
        }*/
        // Handle mouse input (editor/desktop)
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
        {
            Debug.LogWarning("OnClicked");
            Vector3 mousePosition = Input.mousePosition;
            Ray ray = Camera.main.ScreenPointToRay(mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Debug.LogWarning("OnCardTouchedIN");
                CardInteraction card = hit.collider.GetComponent<CardInteraction>();
                if (Input.GetMouseButtonDown(0) && card != null)
                {
                    card.OnCardTouched(mousePosition);
                    tempCard = card; // Store the card for later use
                }
                else if (card == null && Input.GetMouseButtonDown(0))
                {
                    Debug.LogError("CardInteraction is null, trying to stop showcase player pool cards");
                    deckController.TryStopShowcasePlayerPoolCards();
                }
                else if (Input.GetMouseButton(0) && tempCard != null)
                {
                    Debug.LogWarning("OnTouchDrag");
                    if (CardInteraction.currentlySelectedCard == tempCard)
                    {
                        Debug.LogWarning("OnTouchDragIN");
                        tempCard.OnTouchDrag(mousePosition); // If you have this method/event
                    }
                }
            }
        }

        else if (Input.GetMouseButtonUp(0))
        {
            Debug.LogWarning("OnTouchUp");
            if (CardInteraction.currentlySelectedCard != null)
            {
                Debug.LogWarning("OnTouchUpIN");
                CardInteraction.currentlySelectedCard.OnTouchUp();
                //CardInteraction.currentlySelectedCard = null;
                tempCard = null; // Reset the stored card
            }
        }
    }

    void FixedUpdate()
    {
        if (turnTimer > 15)
        {
            turnTimer -= Time.deltaTime;
        }
        else if (turnTimer > 0)
        {
            turnTimer -= Time.deltaTime;
            //turnTimerText.text = Mathf.RoundToInt(turnTimer).ToString();
        }
    }

    private int roundCount = 0;
    //Gets message from the server to start the deck and the cards
    [ContextMenu("Initialize Card Prefabs")]
    public IEnumerator InitializeCardPrefabs()
    {
        ResetForNewRound();
        roundCount++;
        if (waitingScreen.activeSelf) waitingScreen.SetActive(false);
        if (mainScreen.activeSelf) mainScreen.SetActive(false);
        yield return StartCoroutine(deckController.DeckStart());
        if (winScreen.activeSelf) winScreen.SetActive(false);
    }

    public void DeckReady()
    {
        networkRelay.DeckReadyServerRPC();
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
        if (alreadySubbed) return;
        foreach (var cardInteraction in cardInteractionsScripts)
        {
            cardInteraction.OnCardSelected += CardSelected;
            cardInteraction.OnCardsPlayed += CardsPlayed;
            alreadySubbed = true;
        }
    }

    //Gets message from the server to start dealing cards to players   
    public void CardPrefabsToPlayers(int playerCount, SerializableDictionary serializableDictionary)
    {
        //Converts serializablelist to a normal dictionary
        Dictionary<int, List<string>> playerHands = serializableDictionary.ToDictionary();

        //Informs the deckController to deal the players' cards
        StartCoroutine(DelayedDealPlayers(playerCount, playerHands));
    }

    private IEnumerator DelayedDealPlayers(int playerCount, Dictionary<int, List<string>> playerHands)
    {
        //Needed so that hands dont get updated before the previous ordeals are done
        yield return new WaitForSeconds(1.5f);

        if (deckController) deckController.DealPlayers(playerCount, playerHands);
    }

    //Gets message from the server to start dealing cards to center
    public void CardPrefabsToCenter(SerializableCard serializableCard)
    {
        //Converts serializablelist to a normal list
        List<string> centerCardIDs = serializableCard.ToDictionary().Keys.ToList();

        //Informs the deckController to deal the center cards
        if (deckController) deckController.DealCenter(centerCardIDs);
        //turnTimerText.text = "";
    }

    //Called when a card is selected in the player's hand
    private void CardSelected(string cardID)
    {
        currentSelectedHandCard = cardID;
    }

    public void UpdateCurrentPlayerHandLayoutCall()
    {
        //UI
        deckController.UpdateCurrentPlayerHandLayout();
    }

    //Called when the player tries to play the selected card with one or two center cards
    private void CardsPlayed(string cardID, GameObject cardObject, int playerNumber)
    {
        CheckIfLegal(playerNumber);
    }

    //Checks if played move is legal before sending it to the server
    public void CheckIfLegal(int playerNumber)
    {
        //if(currentSelectedHandCard[0] == 0 || currentSelectedHandCard[1]==0)return false;

        Dictionary<string, int[]> cardsToRemove = new Dictionary<string, int[]>();

        // Iterate over the selected center cards and add them to the removal list
        foreach (var cardToBeRemoved in centerCards)
        {
            cardsToRemove.Add(cardToBeRemoved.Key, cardToBeRemoved.Value);
        }

        SerializableCard serializableCard = new SerializableCard(centerCards);

        int sumValue = centerCards.Count > 0 ? centerCards.Last().Value[1] : 0;
        int cardValue = CardInteraction.cardLookup[currentSelectedHandCard].GetCardID()[1];

        // Kapkaç logic: If this player has Kapkaç, force their card to act as value 11 and capture
        if (kapkacPlayerNo == playerNumber && kapkacCount > 0)
        {
            Debug.LogWarning($"Player {playerNumber}'s move is affected by Kapkaç!");
            // Force the played card to act as value 11 (Jack)
            string kapkacCard = (string)currentSelectedHandCard.Clone();
            //var kapkacCard = CardInteraction.cardLookup[kapkacCardID].GetCardID();
            // kapkacCard[1] = 11;

            SerializableCard tempSerializableList = new SerializableCard(centerCards);

            DiscardPlayedCards(kapkacCard, tempSerializableList, playerNumber, currentSelectedHandCard[1]); // Use the original value for Kapkaç
            movePlayedLocally = true;
            kapkacCount--;
            if (kapkacCount == 0) kapkacPlayerNo = -1;
            networkRelay.SendMoveToServerRPC(kapkacCard, serializableCard, playerNumber, 11); // Use 11 to indicate Jack
            myCards.Remove(currentSelectedHandCard);
            return;
        }
    
        if (blockedPlayerNo == playerNumber && blockCount > 0)
        {
            Debug.LogWarning($"Player {playerNumber}'s move is blocked by Oynayamazsın!");
            DiscardHandCards(currentSelectedHandCard, CardInteraction.cardLookup[currentSelectedHandCard].GetCardID()); // Always add to center
            movePlayedLocally = true;
            blockCount--;
            if (blockCount == 0) blockedPlayerNo = -1;
            networkRelay.SendMoveToServerRPC(currentSelectedHandCard, new SerializableCard(centerCards), playerNumber, -999); // Use -999 or another value to indicate block
            myCards.Remove(currentSelectedHandCard);
            return;
        }
        else if (cardValue == sumValue || (cardValue == 11 && sumValue != 0))
        {
            DiscardPlayedCards(currentSelectedHandCard, serializableCard, playerNumber);
            movePlayedLocally = true;
        }
        else
        {
            DiscardHandCards(currentSelectedHandCard, CardInteraction.cardLookup[currentSelectedHandCard].GetCardID());
            movePlayedLocally = true;
        }

        networkRelay.SendMoveToServerRPC(currentSelectedHandCard, serializableCard, playerNumber, sumValue);
        myCards.Remove(currentSelectedHandCard);


    }

    //Called when the player decides to put the selected hand card to the center
    private void CardAddedToCenter()
    {
        networkRelay.AddCenterCardServerRPC(currentSelectedHandCard, CardInteraction.cardLookup[currentSelectedHandCard].GetCardID());
        myCards.Remove(currentSelectedHandCard);
        currentSelectedHandCard = null;
    }

    public void GetCardThatCaptured(string playedCard, SerializableCard serializedCard, int playerNumber)
    {
        Debug.Log("Discarding hand cards: " + movePlayedLocally);
        if (!movePlayedLocally)
        {
            DiscardPlayedCards(playedCard, serializedCard, playerNumber);
        }
        else
        {
            movePlayedLocally = false;
        }
    }

    public void DiscardPlayedCards(string playedCard, SerializableCard serializedCard, int playerNumber, int KKvalue = 0)
    {
        if (KKvalue != 0)
        {
            CardInteraction.cardLookup[playedCard].GetCardID()[1] = KKvalue; // Set the value to 11 if Kapkaç is used
        }
        Debug.Log("Discarding played cards: " + movePlayedLocally);
        if (!movePlayedLocally)
        {
            List<string> selectedCenterCards = serializedCard.ToDictionary().Keys.ToList();
            List<string> selectedCards = new List<string>(selectedCenterCards);
            selectedCards.Add(playedCard);
            cardObjectsToBeDiscarted.Clear();

            foreach (string cardID in selectedCards)
            {
                GameObject tempCardObject = CardInteraction.cardLookup[cardID].gameObject;
                tempCardObject.transform.parent = null;

                if (cardID == playedCard)
                {
                    tempCardObject.transform.rotation = Quaternion.Euler(90, 0, 0);
                    tempCardObject.transform.position = (centerTransform.position + tempCardObject.transform.position) / 2;
                }

                if (tempCardObject != null)
                {
                    cardObjectsToBeDiscarted.Add(tempCardObject);
                }
                else
                {
                    Debug.LogWarning("No card found with the tag: " + cardID[0] + "_" + cardID[1]);
                }
            }

            bool piştiHappened = false;

            if (selectedCards.Count == 2)
            {
                if (selectedCards[selectedCards.Count - 1][1] != 11)
                {
                    piştiHappened = true;
                }

                else if (selectedCards[selectedCards.Count - 1][1] != 11 && selectedCards[selectedCards.Count - 2][1] != 11)
                {
                    piştiHappened = true;
                }
            }

            deckController.MoveCardsToPlayerPool(cardObjectsToBeDiscarted, playerNumber, piştiHappened);

            foreach (string uniqueCardId in selectedCenterCards)
            {
                var selectedCardId = CardInteraction.cardLookup[uniqueCardId].GetCardID();
                centerCards = centerCards
                    .Where(card => !(card.Value[0] == selectedCardId[0] && card.Value[1] == selectedCardId[1]))
                    .ToDictionary(card => card.Key, card => card.Value);
            }

            PrintCenterCards();
        }
        else
        {
            movePlayedLocally = false;
            CardInteraction.isOneCardSelected = false;
            currentSelectedHandCard = null;
            centerCards.Clear();
        }
    }

    public void GetCardAddedToCenter(string uniqueCardID,int[] cardID)
    {
        Debug.Log("Discarding hand cards: " + movePlayedLocally);
        if (!movePlayedLocally)
        {
            DiscardHandCards(uniqueCardID, cardID);
        }
        else
        {
            movePlayedLocally = false;
        }
    }

    //To remove the played card from the hand when it played to the center
    public void DiscardHandCards(string uniqueCardID, int[] cardID)
    {
        //UI
        deckController.DiscardHandCardToCenter(uniqueCardID, cardID);
    }

    public void UpdateCurrentPlayer(int playerNumber)
    {
        currentPlayerNo = playerNumber;
    }

    public void UpdateCenterCardIDList(SerializableCard serializableCard)
    {
        centerCardIDList = serializableCard.ToDictionary().Keys.ToList();
    }

    public static string TurnCardIdToString(int[] tempCardID)
    {
        return tempCardID[0] + "_" + tempCardID[1];
    }

    public void PrintCenterCards()
    {
        foreach (var centerCardID in centerCards)
        {
            print(centerCardID.Value[0]);
            print(centerCardID.Value[1]);
        }
    }

    //!!!! Maybe can be deleted later or deactivated.
    public void PrintPlayerPools(SerializableDictionary serializableDictionary, int chkobbaPlayer)
    {
        Dictionary<int, List<string>> playersPooledCardsIDs = serializableDictionary.ToDictionary();

        foreach (var kvp in playersPooledCardsIDs)
        {
            int playerKey = kvp.Key;
            Debug.Log("Player " + playerKey);
            List<string> cardList = kvp.Value;

            foreach (var card in cardList)
            {
                string cardRepresentation = string.Join(", ", card);
                Debug.Log("Card: " + cardRepresentation);
            }
        }
    }

    public void ShowWinScreen(string message, int winnerSide, int point0, int point1)
    {

        //turnTimerText.text = "";

        UpdatePointText(point0, point1);

        winScreen.SetActive(true);
        roundOverText.text = message;

        if (winnerSide != -1)
        {
            if (winnerSide == 3)
            {
                //Draw
                AudioManager.Instance.PlayAudio(2, 0.5f, false);
            }
            else if (deckController.thisPlayerNumber == 0 || deckController.thisPlayerNumber == 2)
            {
                //Side 0 win or lose
                if (winnerSide == 0) AudioManager.Instance.PlayAudio(2, 0.5f, false);
                else AudioManager.Instance.PlayAudio(1, 0.5f, false);
            }
            else if (deckController.thisPlayerNumber == 1 || deckController.thisPlayerNumber == 3)
            {
                //Side 1 win or lose
                if (winnerSide == 1) AudioManager.Instance.PlayAudio(2, 0.5f, false);
                else AudioManager.Instance.PlayAudio(1, 0.5f, false);
            }
        }
    }

    private void UpdatePointText(int point0, int point1)
    {
        if (deckController.thisPlayerNumber == 0 || deckController.thisPlayerNumber == 2)
        {
            pointTexts[0].text = point0.ToString();
            pointTexts[1].text = point1.ToString();
        }

        else if (deckController.thisPlayerNumber == 1 || deckController.thisPlayerNumber == 3)
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
        /*int[] playableCardID = { 0, 0 };

        List<int[]> myCards = new List<int[]>();

        foreach (Transform childTransform in GameObject.Find("PlayerHand" + (deckController.thisPlayerNumber + 1)).transform)
        {
            string[] tagStrings = childTransform.gameObject.tag.Split('_');
            if (tagStrings.Length == 2)
            {
                int[] cardID = { 0, 0 };

                int.TryParse(tagStrings[0], out cardID[0]);
                int.TryParse(tagStrings[1], out cardID[1]);
                myCards.Add(cardID);
            }

        }

        bool addedToCenterFlag = false;

        foreach (int[] cardID in myCards)
        {
            if (cardID[1] != centerCards[centerCards.Count - 1][1])
            {
                currentSelectedHandCard = cardID;
                CardAddedToCenter();
                addedToCenterFlag = true;
                return;
            }
            else playableCardID = cardID;
        }

        if (!addedToCenterFlag)
        {
            currentSelectedHandCard = playableCardID;
            CheckIfLegal(currentPlayerNo);
        }*/
    }

    public void GetTurnTime(float turnTime)
    {
        turnTimer = turnTime;
    }

    private void InitialGameManagerSetUp()
    {
        AudioManager.Instance.PlayAudio(4, 0.04f, true);

        //Setting the networkRealy script to sen ServerRPCs
        networkRelay = FindObjectOfType<NetworkRelay>();


        //Setting the localInstances for ClientRPC messages
        if (LocalInstance == null)
        {
            LocalInstance = this;
        }

        ProfileScript profileScript = GameObject.Find("MainUI").GetComponent<ProfileScript>();
        cardBackSprite = profileScript.cardBackSprites[profileScript.cardBackIndex];

        pointTexts.Add(GameObject.Find("PlayerPointText1").GetComponent<Text>());
        GameObject.Find("PlayerPointText1").GetComponent<Text>().text = "0 ";
        pointTexts.Add(GameObject.Find("PlayerPointText2").GetComponent<Text>());
        GameObject.Find("PlayerPointText2").GetComponent<Text>().text = "0 ";

        waitingScreen = GameObject.Find("WaitingScreen");
        mainScreen = GameObject.Find("MainScreen");

        winScreen = GameObject.Find("WinScreen");
        roundOverText = GameObject.Find("RoundOverText").GetComponent<Text>();

        GetNecessaryTransforms();


        centerTransform = GameObject.Find("Center").GetComponent<Transform>();
        deckController.GetPlayerHandTransforms(playerHandTransforms, playerPoolTransforms, centerTransform, playerPiştiPoolTransforms);
    }

    public void SkipTurn()
    {
        if (currentPlayerNo == deckController.thisPlayerNumber) Invoke("PlayAfterTimeOut", 1);
    }

    public void TellServerTurnEnded()
    {
        networkRelay.NotifyTurnIsReadyToEndServerRPC();
        //Debug.LogError("Turn ended");
    }

    private void GetNecessaryTransforms()
    {
        //All the necessary transforms for card placements

        playerHandTransforms.Add(GameObject.Find("PlayerHand1").GetComponent<Transform>());
        playerHandTransforms.Add(GameObject.Find("PlayerHand2").GetComponent<Transform>());
        playerHandTransforms.Add(GameObject.Find("PlayerHand3").GetComponent<Transform>());
        playerHandTransforms.Add(GameObject.Find("PlayerHand4").GetComponent<Transform>());

        playerPoolTransforms.Add(GameObject.Find("PlayerPool1").GetComponent<Transform>());
        playerPoolTransforms.Add(GameObject.Find("PlayerPool2").GetComponent<Transform>());
        playerPoolTransforms.Add(GameObject.Find("PlayerPool3").GetComponent<Transform>());
        playerPoolTransforms.Add(GameObject.Find("PlayerPool4").GetComponent<Transform>());

        playerPiştiPoolTransforms.Add(GameObject.Find("PlayerPiştiPool1").GetComponent<Transform>());
        playerPiştiPoolTransforms.Add(GameObject.Find("PlayerPiştiPool2").GetComponent<Transform>());
        playerPiştiPoolTransforms.Add(GameObject.Find("PlayerPiştiPool3").GetComponent<Transform>());
        playerPiştiPoolTransforms.Add(GameObject.Find("PlayerPiştiPool4").GetComponent<Transform>());
    }

    /// <summary>
    /// Activates the "peek at a random opponent card" super power locally and sends the move to the server.
    /// </summary>
    public void UsePeekOpponentCardPower()
    {
        Debug.Log("Using Peek Opponent Card Power");
        int opponentPlayerNo = GetRandomOpponentPlayerNo();
        int cardIndex = deckController.GetRandomHandCardIndex(opponentPlayerNo);

        // Locally show the effect
        deckController.PeekOpponentCard(opponentPlayerNo, cardIndex);

        // Send to server for sync
        networkRelay.UsePeekOpponentCardPowerServerRPC(opponentPlayerNo, cardIndex);
    }

    /// <summary>
    /// Called by the server to sync the peek effect to all clients.
    /// </summary>
    public void OnPeekOpponentCardSynced(int opponentPlayerNo, int cardIndex)
    {
        deckController.PeekOpponentCard(opponentPlayerNo, cardIndex);
    }

    /// <summary>
    /// Returns a random opponent player number (not self or teammate in 2v2).
    /// </summary>
    private int GetRandomOpponentPlayerNo()
    {
        List<int> possibleOpponents = new List<int>();
        int myNo = deckController.thisPlayerNumber;
        int playerCount = deckController.playerCount;

        if (playerCount == 2)
        {
            possibleOpponents.Add((myNo + 1) % 2);
        }
        else if (playerCount == 4)
        {
            // In 2v2, teammates are 0/2 and 1/3
            if (myNo == 0 || myNo == 2)
                possibleOpponents.AddRange(new int[] { 1, 3 });
            else
                possibleOpponents.AddRange(new int[] { 0, 2 });
        }
        return possibleOpponents[UnityEngine.Random.Range(0, possibleOpponents.Count)];
    }

    /// <summary>
    /// Activates the "swap a card with an opponent" super power locally and sends the move to the server.
    /// </summary>
    public void UseSwapCardWithOpponentPower()
    {
        Debug.Log("Using Swap Card With Opponent Power");
        int myPlayerNo = deckController.thisPlayerNumber;
        int opponentPlayerNo = GetRandomOpponentPlayerNo();
        int myCardIndex = deckController.GetRandomHandCardIndex(myPlayerNo);
        int oppCardIndex = deckController.GetRandomHandCardIndex(opponentPlayerNo);

        // Locally show the effect and swap
        StartCoroutine(deckController.SwapCardsBetweenPlayers(myPlayerNo, myCardIndex, opponentPlayerNo, oppCardIndex));

        // Send to server for sync
        //snetworkRelay.UseSwapCardWithOpponentPowerServerRPC(myPlayerNo, myCardIndex, opponentPlayerNo, oppCardIndex);
    }

    /// <summary>
    /// Called by the server to sync the swap effect to all clients.
    /// </summary>
    public void OnSwapCardWithOpponentSynced(int myPlayerNo, int myCardIndex, int opponentPlayerNo, int oppCardIndex)
    {
        deckController.SwapCardsBetweenPlayers(myPlayerNo, myCardIndex, opponentPlayerNo, oppCardIndex);
    }

    private int blockedPlayerNo = -1;
    private int blockCount = 0;

    public void ActivateBlockNextPlayerPower()
    {
        // Block the next player for one turn
        blockedPlayerNo = (deckController.thisPlayerNumber + 1) % deckController.playerCount;
        blockCount = 1;
        Debug.LogWarning($"Player {blockedPlayerNo} will be blocked on their next move!");
        // Optionally, sync this state to the server/other clients if needed
    }

    private int kapkacPlayerNo = -1;
    private int kapkacCount = 0;
    public void ActivateKapkacPower()
    {
        kapkacPlayerNo = deckController.thisPlayerNumber;
        kapkacCount = 1;
        Debug.LogWarning($"Player {kapkacPlayerNo} will have Kapkaç effect on their next move!");
        // Optionally, sync this state to the server/other clients if needed
    }

    public void ActivateValeArarPower()
    {
        Debug.Log("ValeArar power activated! Showing indicators for all Jacks.");
        foreach (var cardScript in cardInteractionsScripts)
        {
            // Assuming cardID[1] is the value, and 11 is Jack
            if (cardScript != null && cardScript.gameObject != null)
            {
                string[] tagParts = cardScript.gameObject.tag.Split('_');
                if (tagParts.Length == 2 && int.TryParse(tagParts[1], out int value) && value == 11)
                {
                    // Activate the indicator for this card
                    var indicator = cardScript.transform.Find("SelectedCardIndicator(Clone)");
                    if (indicator != null)
                        indicator.gameObject.SetActive(true);
                }
            }
        }
    }

    public void DeactivateValeArarPower()
    {
        Debug.Log("Deactivating ValeArar power: hiding indicators for all Jacks.");
        foreach (var cardScript in cardInteractionsScripts)
        {
            if (cardScript != null && cardScript.gameObject != null)
            {
                string[] tagParts = cardScript.gameObject.tag.Split('_');
                if (tagParts.Length == 2 && int.TryParse(tagParts[1], out int value) && value == 11)
                {
                    var indicator = cardScript.transform.Find("SelectedCardIndicator(Clone)");
                    if (indicator != null)
                        indicator.gameObject.SetActive(false);
                }
            }
        }
    }


    private CardInteraction kopyalaSourceCard = null;
    public bool isKopyalaActive = false;
    // Call this when the power is activated
    public void ActivateKopyalaYapistirPower()
    {
        Debug.Log("KopyalaYapıstır activated! Select a card to copy to.");
        isKopyalaActive = true;
        kopyalaSourceCard = CardInteraction.currentlySelectedCard;
        if (kopyalaSourceCard == null)
            Debug.LogWarning("KopyalaYapıstır: No source card selected when activating power!");
    }

    // Call this from CardInteraction when a card is clicked and isKopyalaActive is true
    public void TryKopyalaYapistir(CardInteraction targetCard)
    {
        Debug.Log("Trying KopyalaYapıstır on: " + targetCard.gameObject.name);
        if (!isKopyalaActive || kopyalaSourceCard == null || targetCard == null || targetCard == kopyalaSourceCard)
            return;

        Debug.Log($"KopyalaYapıstır: {kopyalaSourceCard.gameObject.name} -> {targetCard.gameObject.name}");
        // Store original data if not already stored
        targetCard.StoreOriginalCardData();

        // Copy suit, value, and sprite
        targetCard.SetCardIDAndSprite(kopyalaSourceCard.GetCardID(), kopyalaSourceCard.GetComponent<SpriteRenderer>().sprite);

        Debug.Log($"KopyalaYapıstır: {kopyalaSourceCard.gameObject.name} copied to {targetCard.gameObject.name}");

        // Reset state
        isKopyalaActive = false;
        kopyalaSourceCard = null;
    }

    // Call this at the end of the round to reset all cards
    public void ResetAllKopyalaCards()
    {
        foreach (var cardScript in cardInteractionsScripts)
            cardScript.ResetToOriginalCard();
    }

}
