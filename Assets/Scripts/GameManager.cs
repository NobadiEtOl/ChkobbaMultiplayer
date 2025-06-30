using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.UIElements;
using TMPro;

public class GameManager : MonoBehaviour
{
    //Scripts
    public static GameManager LocalInstance { get; private set; }
    [SerializeField] private DeckController deckController;
    public NetworkRelay networkRelay;
    //Card Variables
    [SerializeField] public GameObject cardBack;
    private string currentSelectedHandCard;//Represents the card current player chose to play with.
    public Dictionary<string, int[]> centerCards = new Dictionary<string, int[]>();//List of cards in the center
    public List<GameObject> centerCardsObjects = new List<GameObject>();//List of the card objects in the center
    private List<CardInteraction> cardInteractionsScripts = new List<CardInteraction>();//Reference to the scripts of every card.
    private List<GameObject> cardObjectsToBeDiscarted = new List<GameObject>();
    public static int currentPlayerNo = 0;
    private List<string> centerCardIDList;
    public List<string> myCards;
    private GameObject winScreen;
    Text roundOverText;
    private float turnTimer = 0;
    private List<Text> pointTexts = new List<Text>();
    private CanvasGroup superPowerTextGroup;
    private TextMeshProUGUI superPowerText;
    private CanvasGroup pistiTextGroup;
    private TextMeshProUGUI pistiText;
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
    private GameObject explosionObject;
    private Animator explosionAnimator;
    private GameObject bombObject;
    private Animator bombAnimator;

    public List<GameObject> kapkacCardsToBeReset = new List<GameObject>();
    [SerializeField] private GameObject kapkacEffectPrefab; // Prefab with your PNG as a SpriteRenderers
    [SerializeField] private GameObject oynayamazsinBlockPrefab;
    private GameObject oynayamazsinBlockInstance;


    public void ResetForNewRound()
    {
        foreach (var cardScript in cardInteractionsScripts)
            cardScript.ResetToOriginalCard();
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
        Debug.Log($"GameManager Awake called on {gameObject.GetInstanceID()}");
        if (LocalInstance != null && LocalInstance != this)
        {
            Debug.LogWarning($"Destroying duplicate GameManager {gameObject.GetInstanceID()}");
            Destroy(this.gameObject);
            return;
        }
        LocalInstance = this;
        DontDestroyOnLoad(this.gameObject);
        Debug.Log($"GameManager LocalInstance set to {gameObject.GetInstanceID()}");
    }

    void Start()
    {
        Debug.Log("GameManager started");
        InitialGameManagerSetUp();//Identifies and assigns necessary variables and calls other functions

        SuperPowerSpawner.LocalInstance.InitializeSuperPowers(); // Initialize super powers
    }

    public void NotifyConnection()
    {
        networkRelay.NotifyCientConnectedServerRPC(NetworkManager.Singleton.LocalClientId);// Tells the server that a client is started
    }

    void OnDestroy()
    {
        if (LocalInstance == this)
        {
            Debug.LogWarning($"GameManager {gameObject.GetInstanceID()} destroyed, clearing LocalInstance");
            LocalInstance = null;
        }
    }


    private CardInteraction tempCard;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W)) PlayAutomatically(3);
        if (Input.GetKeyDown(KeyCode.A)) PlayAutomatically(4);
        if (Input.GetKeyDown(KeyCode.S)) PlayAutomatically(1);
        if (Input.GetKeyDown(KeyCode.D)) PlayAutomatically(2);

        // ...existing code...
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
            Vector3 mousePosition = Input.mousePosition;
            Ray ray = Camera.main.ScreenPointToRay(mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                CardInteraction card = hit.collider.GetComponent<CardInteraction>();
                if (Input.GetMouseButtonDown(0) && card != null)
                {
                    card.OnCardTouched(mousePosition);
                    tempCard = card; // Store the card for later use
                }
                else if (card == null && Input.GetMouseButtonDown(0))
                {
                    //Debug.LogError("CardInteraction is null, trying to stop showcase player pool cards");
                    deckController.TryStopShowcasePlayerPoolCards();
                }
                else if (Input.GetMouseButton(0) && tempCard != null)
                {
                    if (CardInteraction.currentlySelectedCard == tempCard)
                    {
                        tempCard.OnTouchDrag(mousePosition); // If you have this method/event
                    }
                }
            }
        }

        else if (Input.GetMouseButtonUp(0))
        {
            if (CardInteraction.currentlySelectedCard != null)
            {
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
        yield return StartCoroutine(deckController.DeckStart());
        if (winScreen.activeSelf) winScreen.SetActive(false);
        if (mainScreen.activeSelf) mainScreen.SetActive(false);
    }

    public void DeckReady()
    {
        Debug.Log("Deck is ready, notifying server.");
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

        yield return new WaitForSeconds(0f);

        SuperPowerSpawner.LocalInstance.ReadyToSpawnSuperPowers();
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
        if (isSunuDegisTokusActive)
        {
            // Only allow selecting a card outside your own hand for the second selection
            if (myCards.Contains(cardID))
            {
                Debug.LogWarning("You must select a card from another player's hand for ŞunuDeğişTokuş.");
                return;
            }
            // Send swap request to server
            networkRelay.UseSunuDegisTokusServerRPC(deckController.thisPlayerNumber, sunuDegisTokusFirstCard, cardID);
            isSunuDegisTokusActive = false;
            sunuDegisTokusFirstCard = null;
            DeckController.LocalInstance.ExitShowcaseAllOtherHands();
            return;
        }

        // ŞunuDeğişBunuTokuş logic
        if (isSunuDegisBunuTokusActive)
        {
            // Only allow selecting a card from another player's hand
            if (myCards.Contains(cardID))
            {
                Debug.LogWarning("You must select a card from another player's hand for ŞunuDeğişBunuTokuş.");
                return;
            }
            // Get my hand snapshot and current swap index
            if (sunuDegisBunuTokusMyHandSnapshot == null || sunuDegisBunuTokusSwapIndex >= sunuDegisBunuTokusMyHandSnapshot.Count)
            {
                Debug.LogWarning("No more cards to swap for ŞunuDeğişBunuTokuş.");
                isSunuDegisBunuTokusActive = false;
                return;
            }
            string myHandCardID = sunuDegisBunuTokusMyHandSnapshot[sunuDegisBunuTokusSwapIndex];
            // Send swap request to server
            networkRelay.UseSunuDegisBunuTokusServerRPC(deckController.thisPlayerNumber, myHandCardID, cardID, sunuDegisBunuTokusSwapIndex);
            sunuDegisBunuTokusSwapIndex++;
            if (sunuDegisBunuTokusSwapIndex >= sunuDegisBunuTokusMyHandSnapshot.Count)
            {
                isSunuDegisBunuTokusActive = false;
                sunuDegisBunuTokusMyHandSnapshot = null;
                sunuDegisBunuTokusSwapIndex = 0;
                Debug.Log("ŞunuDeğişBunuTokuş: All swaps done.");
            }
            else
            {
                Debug.Log($"ŞunuDeğişBunuTokuş: Select card {sunuDegisBunuTokusSwapIndex + 1} to swap.");
            }
            return;
        }

        currentSelectedHandCard = cardID;
        SuperPowerSpawner.LocalInstance.SetActiveActivateButtonTrue();
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

        if (oynayamazsinActive)
        {
            DiscardHandCards(currentSelectedHandCard, CardInteraction.cardLookup[currentSelectedHandCard].GetCardID());
            movePlayedLocally = true;
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

        CardInteraction.currentlySelectedCard = null;
        SetCurrentSelectedHandCardNull();
        SuperPowerSpawner.LocalInstance.CheckIfBackgroundPanelOpen();

    }

    private IEnumerator KapkacCourotine(string playedCard)
    {
        // --- Kapkaç animation addition START ---
        GameObject cardObj = CardInteraction.cardLookup[playedCard].gameObject;
        if (kapkacEffectPrefab != null)
        {
            Debug.LogWarning("Instantiating Kapkaç effect for card: " + currentSelectedHandCard);
            GameObject effect = Instantiate(kapkacEffectPrefab, cardObj.transform);
            effect.transform.localPosition = new Vector3(0, 0, -0.01f); // Slightly above the card face
            kapkacCardsToBeReset.Add(cardObj);

            // Start fade-in
            SpriteRenderer sr = effect.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color c = sr.color;
                c.a = 0f;
                sr.color = c;
                yield return StartCoroutine(FadeInSprite(effect, 0.5f)); // 0.5 seconds fade-in
            }
        }
        // --- Kapkaç animation addition END ---
    }


    private IEnumerator FadeInSprite(GameObject effectObj, float duration = 0.5f)
    {
        var sr = effectObj.GetComponent<SpriteRenderer>();
        if (sr == null)
            yield break;

        Color color = sr.color;
        color.a = 0f;
        sr.color = color;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Clamp01(elapsed / duration);
            sr.color = color;
            yield return null;
        }
        color.a = 1f;
        sr.color = color;
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

    public void DiscardPlayedCards(string playedCard, SerializableCard serializedCard, int playerNumber)
    {
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
                if (CardInteraction.cardLookup[selectedCards[selectedCards.Count - 1]].GetCardID()[1] == CardInteraction.cardLookup[selectedCards[selectedCards.Count - 2]].GetCardID()[1])
                {
                    Debug.LogError("Pişti happened!");
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

    public void GetCardAddedToCenter(string uniqueCardID, int[] cardID)
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

    private int turnCounter;
    public void UpdateCurrentPlayer(int playerNumber, int turnC)
    {
        currentPlayerNo = playerNumber;
        turnCounter = turnC;
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

        winScreen.SetActive(true);
        roundOverText.text = message;

        UpdatePointText(point0, point1);

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
            GameObject.Find("TallyContainer").GetComponent<TallyMarkDisplay>().UpdateTallyDisplay(point0);
            GameObject.Find("TallyContainer1").GetComponent<TallyMarkDisplay>().UpdateTallyDisplay(point1);
        }

        else if (deckController.thisPlayerNumber == 1 || deckController.thisPlayerNumber == 3)
        {
            pointTexts[1].text = point0.ToString();
            pointTexts[0].text = point1.ToString();
            GameObject.Find("TallyContainer").GetComponent<TallyMarkDisplay>().UpdateTallyDisplay(point1);
            GameObject.Find("TallyContainer1").GetComponent<TallyMarkDisplay>().UpdateTallyDisplay(point0);
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
        waitingScreen.SetActive(false);
        mainScreen = GameObject.Find("MainScreen");

        winScreen = GameObject.Find("WinScreen");
        roundOverText = GameObject.Find("RoundOverText").GetComponent<Text>();
        
        superPowerTextGroup = GameObject.Find("SuperPowerTextContainer").GetComponent<CanvasGroup>();
        superPowerText = GameObject.Find("SuperPowerText").GetComponent<TextMeshProUGUI>();
        superPowerTextGroup.gameObject.SetActive(false);
        pistiTextGroup = GameObject.Find("PiştiTextContainer").GetComponent<CanvasGroup>();
        pistiText = GameObject.Find("PiştiText").GetComponent<TextMeshProUGUI>();
        pistiTextGroup.gameObject.SetActive(false);

        GetNecessaryTransforms();


        centerTransform = GameObject.Find("Center").GetComponent<Transform>();
        deckController.GetPlayerHandTransforms(playerHandTransforms, playerPoolTransforms, centerTransform, playerPiştiPoolTransforms);

        var centerObj = GameObject.Find("Center");
        if (centerObj != null)
        {
            bombObject = centerObj.transform.Find("Bomb")?.gameObject;
            if (bombObject != null)
            {
                bombAnimator = bombObject.GetComponent<Animator>();
                bombObject.SetActive(false);
            }

            explosionObject = centerObj.transform.Find("Explosion")?.gameObject;
            if (explosionObject != null)
            {
                explosionAnimator = explosionObject.GetComponent<Animator>();
                explosionObject.SetActive(false);

                // Find Bomb child and its animator

            }
        }


    }

    public void SkipTurn()
    {
        if (currentPlayerNo == deckController.thisPlayerNumber) Invoke("PlayAfterTimeOut", 1);
    }

    public void TellServerTurnEnded()
    {
        if (turnCounter == 47) NotifyZaferPuaniAtRoundEnd();
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

        verZehriObject = GameObject.Find("VerZehriFog");
        kutsalDesteObject = GameObject.Find("KutsalDesteFog");
    }

    /// <summary>
    /// Activates the "peek at a random opponent card" super power locally and sends the move to the server.
    /// </summary>
    public void UsePeekOpponentCardPower()
    {
        Debug.Log("Using Peek Opponent Card Power");
        int opponentPlayerNo = GetRandomOpponentPlayerNo();
        int cardIndex = deckController.GetRandomHandCardIndex(opponentPlayerNo);

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

    public void UseBayaBayaBakPower()
    {
        Debug.Log("Using BayaBayaBak Power");
        int opponentPlayerNo = GetRandomOpponentPlayerNo(); // You can reuse your existing logic
        networkRelay.UseBayaBayaBakServerRPC(opponentPlayerNo);
    }

    public void OnBayaBayaBakSynced(int opponentPlayerNo)
    {
        deckController.PeekOpponentCardAll(opponentPlayerNo);
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
        Debug.Log("Using Swap Card With Opponent Power (randomized, new logic)");
        int myPlayerNo = deckController.thisPlayerNumber;
        int opponentPlayerNo = GetRandomOpponentPlayerNo();

        if (myCards == null || myCards.Count == 0)
        {
            Debug.LogWarning("No cards in hand for DeğişTokuş!");
            return;
        }
        string myHandCardID = myCards[UnityEngine.Random.Range(0, myCards.Count)];

        List<string> oppHand = deckController.GetOpponentHandCardIDs(opponentPlayerNo);
        if (oppHand == null || oppHand.Count == 0)
        {
            Debug.LogWarning("No cards in opponent's hand for DeğişTokuş!");
            return;
        }
        string oppHandCardID = oppHand[UnityEngine.Random.Range(0, oppHand.Count)];

        // Add a short delay before swapping
        StartCoroutine(DelayedSwap(myPlayerNo, myHandCardID, opponentPlayerNo, oppHandCardID));
    }

    private IEnumerator DelayedSwap(int myPlayerNo, string myHandCardID, int opponentPlayerNo, string oppHandCardID)
    {
        yield return new WaitForSeconds(0.5f); // Adjust as needed
        networkRelay.UseSunuDegisTokusServerRPC(myPlayerNo, myHandCardID, oppHandCardID);
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
                    {
                        indicator.gameObject.SetActive(true);
                        indicator.GetComponent<SpriteRenderer>().color = Color.red; // Set the sprite to card back
                    }
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

        // Allow selection from all hands (including own), for just one card
        CardInteraction.AllowSelectionForParents(
            new[] { "PlayerHand1", "PlayerHand2", "PlayerHand3", "PlayerHand4" }, // Add/remove as needed for your player count
            allowOwnHandCards: true,
            maxSelections: 1
        );
    }


    // Call this from CardInteraction when a card is clicked and isKopyalaActive is true
    public void TryKopyalaYapistir(CardInteraction targetCard)
    {
        Debug.Log("Trying KopyalaYapıstır on: " + targetCard.gameObject.name);
        if (!isKopyalaActive || kopyalaSourceCard == null || targetCard == null || targetCard == kopyalaSourceCard)
            return;

        Debug.Log($"KopyalaYapıstır: {kopyalaSourceCard.gameObject.name} -> {targetCard.gameObject.name}");

        // Network the change to server and all clients
        networkRelay.KopyalaYapistirServerRPC(targetCard.uniqueCardInstanceID, kopyalaSourceCard.uniqueCardInstanceID);

        // Reset state
        isKopyalaActive = false;
        kopyalaSourceCard = null;
        CardInteraction.currentlySelectedCard = null;
        SetCurrentSelectedHandCardNull();
    }

    public void OnKopyalaYapistir(string targetUniqueID, string sourceUniqueID)
    {
        if (CardInteraction.cardLookup.TryGetValue(targetUniqueID, out var targetCard) &&
            CardInteraction.cardLookup.TryGetValue(sourceUniqueID, out var sourceCard))
        {
            // Copy cardID and sprite
            int[] newCardID = sourceCard.GetCardID();
            Sprite newSprite = sourceCard.GetComponent<SpriteRenderer>().sprite;

            // Set cardID and sprite with fade-in
            StartCoroutine(SetCardIDAndSpriteWithFade(targetCard, newCardID, newSprite));
        }
    }

    private IEnumerator SetCardIDAndSpriteWithFade(CardInteraction card, int[] newCardID, Sprite newSprite)
    {
        var sr = card.GetComponent<SpriteRenderer>();
        if (sr == null)
            yield break;

        // Fade out
        Color color = sr.color;
        float duration = 0.3f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Clamp01(1f - (elapsed / duration));
            sr.color = color;
            yield return null;
        }
        color.a = 0f;
        sr.color = color;

        // Change cardID and sprite
        card.SetCardIDAndSprite(newCardID, newSprite);

        // Fade in
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Clamp01(elapsed / duration);
            sr.color = color;
            yield return null;
        }
        color.a = 1f;
        sr.color = color;
    }

    // Call this at the end of the round to reset all cards
    public void ResetAllKopyalaCards()
    {
        /*foreach (var cardScript in cardInteractionsScripts)
            cardScript.ResetToOriginalCard();*/
    }

    public void PlayAutomatically(int playerIndex)
    {
        // Defensive: Check if playerHandTransforms is valid and has children
        if (playerHandTransforms == null || playerHandTransforms.Count <= playerIndex - 1)
        {
            Debug.LogError($"PlayAutomatically: playerHandTransforms missing for player {playerIndex}");
            return;
        }

        var handTransform = playerHandTransforms[playerIndex - 1];
        if (handTransform.childCount == 0)
        {
            Debug.LogWarning($"PlayAutomatically: No cards left in hand for player {playerIndex}");
            return;
        }

        // Get the first card in the hand (change index if you want a different card)
        var cardObj = handTransform.GetChild(2).gameObject;
        var cardScript = cardObj.GetComponent<CardInteraction>();
        if (cardScript == null)
        {
            Debug.LogError("PlayAutomatically: CardInteraction not found on hand card.");
            return;
        }

        // Set as if this card was selected
        currentSelectedHandCard = cardScript.uniqueCardInstanceID;
        CardInteraction.currentlySelectedCard = cardScript;

        // Optionally, invoke selection event if you want to mimic UI
        cardScript.TriggerOnCardSelected();

        // Play the card as if the player played it
        CheckIfLegal(playerIndex - 1);
    }

    public void ActivateBombaPower()
    {
        // Tell the server to bomb the center
        networkRelay.BombaServerRPC();
    }

    public void OnBombaCenter()
    {
        StartCoroutine(BombThenExplosionSequence());
    }

    private IEnumerator BombThenExplosionSequence()
    {
        centerCards.Clear();
        // 1. Ensure both are inactive at the start
        if (bombObject != null) bombObject.SetActive(false);
        if (explosionObject != null) explosionObject.SetActive(false);

        // 2. Activate and play Bomb animation
        if (bombObject != null && bombAnimator != null)
        {
            bombObject.SetActive(true);
            bombAnimator.Play("BombAnimationClip", 0, 0f);

            // Wait for bomb animation to finish
            float bombAnimLength = 1.0f;
            AnimatorStateInfo bombStateInfo = bombAnimator.GetCurrentAnimatorStateInfo(0);
            if (bombStateInfo.length > 0)
                bombAnimLength = bombStateInfo.length;
            else if (bombAnimator.runtimeAnimatorController != null && bombAnimator.runtimeAnimatorController.animationClips.Length > 0)
                bombAnimLength = bombAnimator.runtimeAnimatorController.animationClips[0].length;

            yield return new WaitForSeconds(bombAnimLength);

            // Optionally, hide the bomb sprite after animation
            var bombSpriteRenderer = bombObject.GetComponent<SpriteRenderer>();
            if (bombSpriteRenderer != null)
                bombSpriteRenderer.sprite = null;

            bombObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Bomb object or animator not found!");
        }

        GameObject bombedStack = GameObject.Find("BombedStack");
        if (bombedStack == null)
        {
            Debug.LogError("BombedStack GameObject not found in scene!");
            yield break;
        }

        foreach (var cardObj in centerCardsObjects)
        {
            if (cardObj != null)
            {
                cardObj.transform.SetParent(bombedStack.transform, true);
                cardObj.transform.localPosition = Vector3.zero;
            }
        }

        // 3. Activate and play Explosion animation
        if (explosionObject != null && explosionAnimator != null)
        {
            explosionObject.SetActive(true);
            explosionAnimator.Play("ExplosionAnimationClip", 0, 0f);

            // Wait for explosion animation to finish
            float explosionAnimLength = 1.0f;
            AnimatorStateInfo explosionStateInfo = explosionAnimator.GetCurrentAnimatorStateInfo(0);
            if (explosionStateInfo.length > 0)
                explosionAnimLength = explosionStateInfo.length;
            else if (explosionAnimator.runtimeAnimatorController != null && explosionAnimator.runtimeAnimatorController.animationClips.Length > 0)
                explosionAnimLength = explosionAnimator.runtimeAnimatorController.animationClips[0].length;

            yield return new WaitForSeconds(explosionAnimLength);

            explosionObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Explosion object or animator not found!");
        }

        // 5. Clear all center-related lists/dictionaries (as before)
        centerCardsObjects.Clear();
        if (centerCardIDList != null) centerCardIDList.Clear();

        Debug.Log("Bomba: Center cleared and cards moved to BombedStack.");
    }





    private IEnumerator DisableExplosionAfterAnimation()
    {
        // Wait for the animation to finish
        float animLength = 1.0f;
        if (explosionAnimator != null)
        {
            AnimatorStateInfo stateInfo = explosionAnimator.GetCurrentAnimatorStateInfo(0);
            animLength = stateInfo.length;
        }
        yield return new WaitForSeconds(animLength);

        if (explosionObject != null)
            explosionObject.SetActive(false);
    }



    private bool isYapamazsınActive = false;

    public void ActivateYapamazsınPower()
    {
        networkRelay.ActivateYapamazsınServerRPC();
    }

    public void SetYapamazsınActive(bool isActive)
    {
        isYapamazsınActive = isActive;
        // Optionally update UI here
    }

    private bool oynayamazsinActive = false;
    public void SetOynayamazsinActive(bool isActive)
    {
        oynayamazsinActive = isActive;

        if (isActive)
        {
            Debug.LogWarning("Oynayamazsin power activated! Showing block prefab above center.");
            // Show the block prefab above the center
            if (oynayamazsinBlockInstance == null && oynayamazsinBlockPrefab != null)
            {
                oynayamazsinBlockInstance = Instantiate(oynayamazsinBlockPrefab, centerTransform.GetChild(centerTransform.childCount - 1));
                oynayamazsinBlockInstance.transform.localPosition = new Vector3(0, 0, -0.05f); // Slightly above center

                StartCoroutine(FadeInSprite(oynayamazsinBlockInstance, 0.5f));
            }
        }
        else
        {
            // Hide and destroy the block prefab
            if (oynayamazsinBlockInstance != null)
            {
                Destroy(oynayamazsinBlockInstance);
                oynayamazsinBlockInstance = null;
            }
        }
    }

    public void ActivateKapkacPower()
    {
        // Only allow if a card is selected
        string selectedCardID = GetCurrentSelectedHandCard();
        if (string.IsNullOrEmpty(selectedCardID))
        {
            Debug.LogWarning("No card selected for Kapkaç!");
            return;
        }
        networkRelay.ActivateKapkacOnCardServerRPC(selectedCardID);
    }

    public void ActivateBlockNextPlayerPower()
    {
        networkRelay.ActivateOynayamazsinServerRPC();
    }

    private bool verZehriActive = false;
    private bool kutsalDesteActive = false;

    public void SetVerZehriActive(bool isActive)
    {
        verZehriActive = isActive;
        if (isActive)
        {
            StartVerZehriEffect();
        }

        else
        {
            StopVerZehriEffect();
        }

    }

    public void SetKutsalDesteActive(bool isActive)
    {
        kutsalDesteActive = isActive;
        if (isActive)
            StartKutsalDesteEffect();
        else
            StopKutsalDesteEffect();
    }

    [SerializeField] private GameObject verZehriObject;
    private void StartVerZehriEffect()
    {
        // Show UI/animation for VerZehri active
        Debug.Log("VerZehri effect started!");
        // TODO: Add your visual effect here
        foreach (Transform child in verZehriObject.transform)
        {
            child.GetComponent<FogController>().StartFog();
        }

    }

    private void StopVerZehriEffect()
    {
        // Hide UI/animation for VerZehri
        Debug.Log("VerZehri effect stopped!");
        // TODO: Remove your visual effect here
        foreach (Transform child in verZehriObject.transform)
        {
            child.GetComponent<FogController>().StopFog();
        }
    }

    [SerializeField] private GameObject kutsalDesteObject;
    private void StartKutsalDesteEffect()
    {
        Debug.Log("KutsalDeste effect started!");
        // TODO: Add your visual effect here
        foreach (Transform child in kutsalDesteObject.transform)
        {
            child.GetComponent<FogController>().StartFog();
        }
    }

    private void StopKutsalDesteEffect()
    {
        Debug.Log("KutsalDeste effect stopped!");
        // TODO: Remove your visual effect here
        foreach (Transform child in kutsalDesteObject.transform)
        {
            child.GetComponent<FogController>().StopFog();
        }
    }

    public void ShowVerZehriEffect(int playerNumber, int points)
    {
        // Show effect/animation/notification for VerZehri
    }
    public void ShowKutsalDesteEffect(int playerNumber, int points)
    {
        // Show effect/animation/notification for KutsalDeste
    }

    public void UseBuDahaIyiPower()
    {
        if (currentSelectedHandCard == null || centerCards.Count == 0)
        {
            Debug.LogWarning("No card selected or center is empty!");
            return;
        }
        // Get the top card of the center pile (last added)
        string topCenterCardID = centerCards.Keys.Last();
        networkRelay.UseBuDahaIyiServerRPC(deckController.thisPlayerNumber, currentSelectedHandCard, topCenterCardID);
    }

    public void OnBuDahaIyiSynced(int playerNo, string handCardID, string centerCardID)
    {
        // Swap in myCards
        if (myCards.Contains(handCardID))
        {
            myCards.Remove(handCardID);
            myCards.Add(centerCardID);
        }
        // Swap in centerCards
        if (centerCards.ContainsKey(centerCardID))
        {
            int[] temp = centerCards[centerCardID];
            centerCards.Remove(centerCardID);
            centerCards[handCardID] = CardInteraction.cardLookup[handCardID].GetCardID();
        }
        else if (centerCards.ContainsKey(handCardID))
        {
            int[] temp = centerCards[handCardID];
            centerCards.Remove(handCardID);
            centerCards[centerCardID] = CardInteraction.cardLookup[centerCardID].GetCardID();
        }
        else
        {
            // Fallback: just swap the last card
            var lastKey = centerCards.Keys.Last();
            int[] temp = centerCards[lastKey];
            centerCards.Remove(lastKey);
            centerCards[handCardID] = CardInteraction.cardLookup[handCardID].GetCardID();
        }

        // Swap card objects visually
        deckController.SwapHandCardWithCenterCard(handCardID, centerCardID, playerNo);
    }

    // Add at the top of GameManager.cs
    public bool isSunuDegisTokusActive = false;
    public string sunuDegisTokusFirstCard = null;

    // Call this to activate the power
    public void ActivateSunuDegisTokusPower()
    {
        if (currentSelectedHandCard == null)
        {
            Debug.LogWarning("No card selected in your hand for ŞunuDeğişTokuş!");
            return;
        }
        isSunuDegisTokusActive = true;
        sunuDegisTokusFirstCard = currentSelectedHandCard;
        Debug.Log("ŞunuDeğişTokuş: Select a card from another player's hand to swap with.");
        DeckController.LocalInstance.ShowcaseAllOtherHands();
    }

    public void OnSunuDegisTokusSynced(int myPlayerNo, int otherPlayerNo, string myHandCardID, string otherHandCardID)
    {
        // Swap in myCards if relevant
        if (deckController.thisPlayerNumber == myPlayerNo)
        {
            int idx = myCards.IndexOf(myHandCardID);
            if (idx != -1)
            {
                myCards[idx] = otherHandCardID;
            }
        }
        else if (deckController.thisPlayerNumber == otherPlayerNo)
        {
            int idx = myCards.IndexOf(otherHandCardID);
            if (idx != -1)
            {
                myCards[idx] = myHandCardID;
            }
        }

        // Visual swap
        deckController.SwapCardsBetweenPlayersByID(myPlayerNo, myHandCardID, otherPlayerNo, otherHandCardID);
    }

    // Add at the top of GameManager.cs
    public bool isSunuDegisBunuTokusActive = false;
    private int sunuDegisBunuTokusSwapIndex = 0;
    private List<string> sunuDegisBunuTokusMyHandSnapshot = null;

    // Call this to activate the power
    public void ActivateSunuDegisBunuTokusPower()
    {
        if (myCards == null || myCards.Count == 0)
        {
            Debug.LogWarning("No cards in hand for ŞunuDeğişBunuTokuş!");
            return;
        }
        isSunuDegisBunuTokusActive = true;
        sunuDegisBunuTokusSwapIndex = 0;
        sunuDegisBunuTokusMyHandSnapshot = new List<string>(myCards);
        Debug.Log("ŞunuDeğişBunuTokuş: Select a card from another player's hand to swap with your first card.");
        DeckController.LocalInstance.ShowcaseAllOtherHands();
    }

    // Add this method to handle the synced swap
    public void OnSunuDegisBunuTokusSynced(int myPlayerNo, int otherPlayerNo, string myHandCardID, string otherHandCardID, int myHandIndex)
    {
        // Update myCards if relevant
        if (deckController.thisPlayerNumber == myPlayerNo)
        {
            int idx = myCards.IndexOf(myHandCardID);
            if (idx != -1)
            {
                myCards[idx] = otherHandCardID;
            }
        }
        else if (deckController.thisPlayerNumber == otherPlayerNo)
        {
            int idx = myCards.IndexOf(otherHandCardID);
            if (idx != -1)
            {
                myCards[idx] = myHandCardID;
            }
        }
        // Visual swap at the correct index
        deckController.SwapCardsBetweenPlayersByIDAtIndex(myPlayerNo, myHandCardID, otherPlayerNo, otherHandCardID, myHandIndex);
    }

    // Add this public method to allow CardInteraction to check swap power activeness
    public bool IsAnySwapPowerActive()
    {
        return isSunuDegisTokusActive || isSunuDegisBunuTokusActive;
    }

    public void SetCurrentSelectedHandCardNull()
    {
        currentSelectedHandCard = null;
    }

    public string GetCurrentSelectedHandCard()
    {
        return currentSelectedHandCard;
    }

    public void NotifyZaferPuaniAtRoundEnd()
    {
        SuperPowerSpawner.LocalInstance.ReportZaferPuaniToServer();
    }

    public void OnKapkacCardChanged(string cardUniqueID)
    {
        // Set the card's value to 11 (Jack)
        if (CardInteraction.cardLookup.TryGetValue(cardUniqueID, out var cardInteraction))
        {
            int[] cardID = cardInteraction.GetCardID();
            cardID[1] = 11;
            cardInteraction.SetCardID(cardID);

            // Optionally, update the card's visual to indicate Kapkaç (e.g., highlight, effect)
            StartCoroutine(KapkacCourotine(cardUniqueID));
        }
    }

    public void ShowcaseSuperPower(string powerName, float fadeDuration = 0.5f, float displayDuration = 2f)
    {
        StartCoroutine(ShowcaseSuperPowerCoroutine(powerName, fadeDuration, displayDuration));
    }

    private IEnumerator ShowcaseSuperPowerCoroutine(string powerName, float fadeDuration, float displayDuration)
    {
        if (superPowerText == null || superPowerTextGroup == null)
            yield break;

        superPowerText.text = powerName;
        superPowerTextGroup.gameObject.SetActive(true);

        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            superPowerTextGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        superPowerTextGroup.alpha = 1f;

        // Wait
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            superPowerTextGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            yield return null;
        }
        superPowerTextGroup.alpha = 0f;
        superPowerTextGroup.gameObject.SetActive(false);
    }

    public void ShowPistiText(string message, float fadeDuration = 0.5f, float displayDuration = 2f)
    {
        StartCoroutine(ShowPistiTextCoroutine(message, fadeDuration, displayDuration));
    }

    private IEnumerator ShowPistiTextCoroutine(string message, float fadeDuration, float displayDuration)
    {
        if (pistiText == null || pistiTextGroup == null)
            yield break;

        pistiText.text = message;
        pistiTextGroup.gameObject.SetActive(true);

        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            pistiTextGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        pistiTextGroup.alpha = 1f;

        // Wait
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            pistiTextGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            yield return null;
        }
        pistiTextGroup.alpha = 0f;
        pistiTextGroup.gameObject.SetActive(false);
    }


}
