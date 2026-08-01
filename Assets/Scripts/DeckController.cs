using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.Netcode;
//using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;
//using Unity.VisualScripting;

public class DeckController : MonoBehaviour
{
    public static DeckController LocalInstance;
    public int thisPlayerNumber;

    public int GetThisPlayerNumber()
    {
        return thisPlayerNumber;
    }
    [SerializeField] private GameManager gameManager;
    [SerializeField] private SoundEffectsController soundEffectsController;
    [SerializeField] private List<GameObject> cardPrefabsList;//Prefabs of all the cards.
    private Dictionary<string, GameObject> cardPrefabs;//A dictionary to keep track of each card prefabs with its ID
    private Dictionary<string, GameObject> deckPool;//A dictionary of card ID and a list of all the instantiated cards
    private List<GameObject> activeCards = new List<GameObject>();
    public List<CardInteraction> cardInteractionList; // List to store CardInteraction references
    public List<Transform> playerHandTransforms = new List<Transform>();
    public List<Transform> playerPoolTransforms = new List<Transform>();
    public Transform centerTransform;
    public List<Transform> playerPiştiPoolTransforms = new List<Transform>();
    private int relativeIndex = 0;
    public int playerCount = 0;
    int offset = 150;
    int offsetCounter0 = -1;
    int offsetCounter1 = -1;
    int offsetCounter2 = -1;
    private int zOffsetCounter = 0;

    private int initialScale = 750; // Initial scale for the cards
    private int normalScale = 500; // Scale for the normal cards
    public int centerScale = 900; // Scale for the center cards
    private int myCardsScale = 1200; // Scale for the player's cards
    
    [Header("Deck Movement")]
    [SerializeField] private Transform deckReachPoint; // The position where deck appears when dealing
    [SerializeField] private float deckMoveSpeed = 2f;
    [SerializeField] private Ease deckMoveEase = Ease.OutQuad;
    
    // Deck movement tracking
    private Vector3 deckStartingPosition;
    private bool isDeckMoving = false;
    private bool isDeckAtReachPoint = false;
    private Sequence deckMoveSequence;
    
    //private GameObject cardPool;

    void Awake()
    {
        if (LocalInstance != null && LocalInstance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        LocalInstance = this;
        // Optionally: DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
        thisPlayerNumber = -1;
        InitialDeckSetUp();
        
        // Store deck starting position
        Transform deckTransform = GameObject.Find("DeckTransform").transform;
        if (deckTransform != null)
        {
            deckStartingPosition = deckTransform.position;
            
        }
        else
        {
            
        }
        
        // Validate deck movement settings
        if (deckReachPoint == null)
        {
            
        }
        else
        {
            
        }
    }

    //Called when the deck is ready to start
    public IEnumerator DeckStart()
    {
        // Debug.Log($"[DeckController] ===== ÖNEMLİ: DECK START BEGIN =====\n" +
        //          $"GameManager: {(gameManager != null ? "FOUND" : "NULL")}\n" +
        //          $"CardPrefabsList: {(cardPrefabsList != null ? "FOUND" : "NULL")}\n" +
        //          $"CardPrefabsList Count: {cardPrefabsList?.Count ?? 0}");
        
        // 
        yield return StartCoroutine(DefineCardPrefabs());
        // 
        
        // 
        yield return StartCoroutine(InitializeCardPool());
        // 
        
        // Move deck to reach point for initial dealing
        // 
        MoveDeckToReachPoint();
        
        // Wait for deck to reach position before notifying that deck is ready
        // 
        while (isDeckMoving || !isDeckAtReachPoint)
        {
            yield return null;
        }
        
        // 
        // Debug.Log($"[DeckController] ===== ÖNEMLİ: DECK START COMPLETED =====\n" +
        //          $"Calling gameManager.DeckReady()");
        gameManager.DeckReady();
    }

    public IEnumerator DefineCardPrefabs()
    {
        for (int i = 0; i < cardPrefabsList.Count; i++)
        {
            int[] cardID = new int[] { (i / 13) + 1, (i % 13) + 1 };
            string cardIDString = GameManager.TurnCardIdToString(cardID); // Convert cardID to string

            // Check if the card ID already exists in the dictionary
            if (!cardPrefabs.ContainsKey(cardIDString))
            {
                cardPrefabs.Add(cardIDString, cardPrefabsList[i]);
            }
            else
            {
                //
            }
        }
        yield return null;
    }


    //Initialize and instantiate all the card objects that can be used
    private List<GameObject> cardObjectList = new List<GameObject>();
    private int uniqueCardCounter = 0;
    public bool isInstantMode = false; // Flag to make all animations instant during reconnection
    private IEnumerator InitializeCardPool()
    {
        yield return new WaitForSeconds(0);
        if (deckPool.Count == 0)
        {
            cardInteractionList = new List<CardInteraction>();
            Transform deckTransform = GameObject.Find("DeckTransform").transform;

            int counter = 0;
            foreach (var cardPrefabEntry in cardPrefabs)
            {
                int[] cardID = new int[] { (counter / 13) + 1, (counter % 13) + 1 };
                var cardIDString = cardPrefabEntry.Key;
                var cardPrefab = cardPrefabEntry.Value;


                GameObject card = Instantiate(cardPrefab);
                card.SetActive(true); // Deactivate the card
                card.transform.rotation = Quaternion.Euler(deckTransform.rotation.x, deckTransform.rotation.y, deckTransform.rotation.z);


                // Get the CardInteraction component and add it to the list
                CardInteraction cardInteraction = card.GetComponent<CardInteraction>();
                if (cardInteraction != null)
                {
                    // CRITICAL FIX: Use deterministic ID based on suit and value, not counter
                    // This ensures server and client always have matching IDs
                    string uniqueID = "card_" + counter; // Use counter instead of uniqueCardCounter
                    cardInteraction.SetUniqueID(uniqueID, cardID);
                    cardInteractionList.Add(cardInteraction);
                }

                card.transform.position = new Vector3(deckTransform.transform.position.x, deckTransform.transform.position.y + (counter * 2), deckTransform.transform.position.z);
                card.transform.rotation = Quaternion.Euler(deckTransform.rotation.x - 90, deckTransform.rotation.y, deckTransform.rotation.z);
                card.transform.localScale = new Vector3(initialScale, initialScale, initialScale); // Set the scale of the card
                card.transform.parent = deckTransform.transform;
                deckPool[cardIDString] = card;
                cardObjectList.Add(card);

                counter++;
            }
            
            //deckTransform.rotation = Quaternion.Euler(90, 0, 0);
        }

        else 
        {
            yield return StartCoroutine(ResetCards());
            
            // CRITICAL: Repopulate cardInteractionList even when using existing cards
            cardInteractionList.Clear();
            foreach (var card in cardObjectList)
            {
                if (card != null)
                {
                    CardInteraction cardInteraction = card.GetComponent<CardInteraction>();
                    if (cardInteraction != null)
                    {
                        cardInteractionList.Add(cardInteraction);
                    }
                }
            }
            
        }

        // CRITICAL: Send card interactions to GameManager for reconnection
        // 
        SendCardInteractionsToGameManager();

    }

    [ContextMenu("Reset Cards")]
    public IEnumerator ResetCards()
    {
        Transform deckTransform = GameObject.Find("DeckTransform").transform;
        Transform centerTransform = GameObject.Find("Center").transform;

        // Store the original position and rotation of the deck
        Vector3 originalDeckPosition = deckTransform.position;
        Quaternion originalDeckRotation = deckTransform.rotation;

        // Move deckTransform to the center position
        deckTransform.position = centerTransform.position;
        deckTransform.rotation = centerTransform.rotation;

        List<GameObject> tempCardObjectList = new List<GameObject>();

        foreach (var card in cardObjectList)
        {
            if (orderedCardObjectList.Contains(card)) ;
            else tempCardObjectList.Add(card);
        }

        foreach (var card in orderedCardObjectList)
        {
            tempCardObjectList.Add(card);
        }

        foreach (var card in tempCardObjectList)
        {
            if (card != null)
            {
                SetAutoRotateFlagFalse(card);
                card.transform.parent = null; // Unparent the card
                card.transform.parent = deckTransform.transform;
                // Optionally reset rotation here if needed
            }
        }

        activeCards.Clear();
        gameManager.centerCards.Clear();
        gameManager.centerCardsObjects.Clear();
        offsetCounter0 = -1;
        offsetCounter1 = -1;
        offsetCounter2 = -1;
        zOffsetCounter = 0;
        poolCardOriginalTransforms.Clear();
        orderedCardObjectList.Clear();

        List<Vector3> positions = Enumerable.Repeat(deckTransform.position, 52).ToList();
        List<Quaternion> rotations = Enumerable.Repeat(Quaternion.Euler(deckTransform.rotation.x - 90, deckTransform.rotation.y, deckTransform.rotation.z), 52).ToList();
        List<Vector3> scales = Enumerable.Repeat(new Vector3(initialScale, initialScale, initialScale), 52).ToList();

        yield return StartCoroutine(ChainMoveCards(positions, cardObjectList, 30, rotations, scales));

        // Move deckTransform back to its original position and rotation
        yield return StartCoroutine(TweenMoveTransform(deckTransform, originalDeckPosition, originalDeckRotation, deckTransform.localScale, 0.5f));
    }

    //Deals to players according to the playerCount
    public void DealPlayers(int playerCount, Dictionary<int, List<string>> playerHands)
    {
        this.playerCount = playerCount;

        if (playerCount != 2 && playerCount != 4)
        {
            //
            return;
        }

        // Check if deck is already at reach point (initial dealing) or needs to be moved (subsequent dealing)
        if (isDeckAtReachPoint && !isDeckMoving)
        {
            // 
            StartCoroutine(DelayedDealPlayersCoroutine(playerHands));
        }
        else
        {
            // 
            MoveDeckToReachPoint();
            StartCoroutine(DelayedDealPlayersCoroutine(playerHands));
        }
    }
    
    private IEnumerator DelayedDealPlayersCoroutine(Dictionary<int, List<string>> playerHands)
    {
        // Deck should already be at reach point from DeckStart()
        // Wait for deck to be ready before starting to deal (safety check)
        while (isDeckMoving || !isDeckAtReachPoint)
        {
            yield return null;
        }
        
        if (playerCount == 2)
        {
            yield return StartCoroutine(DealTwoPlayers(playerHands));
        }
        else if (playerCount == 4)
        {
            yield return StartCoroutine(DealFourPlayers(playerHands));
        }

        // Signal server that this client finished receiving/displaying player hands
        GameNetworkRelay.Instance.NotifyDealHandsFinishedServerRPC(Unity.Netcode.NetworkManager.Singleton.LocalClientId);
    }

    private IEnumerator DealTwoPlayers(Dictionary<int, List<string>> playerHands)
    {
        
        
        // CRITICAL FIX: Wait for player number to be set (for reconnection cases)
        int retryCount = 0;
        while (thisPlayerNumber == -1 && retryCount < 50) // Wait up to 5 seconds
        {
            
            yield return new WaitForSeconds(0.1f);
            retryCount++;
        }
        
        if (thisPlayerNumber == -1)
        {
            
            yield break;
        }
        
        
        
        var cardObjects = new List<GameObject>();
        var positions = new List<Vector3>();
        var rotations = new List<Quaternion>();
        var scales = new List<Vector3>(); // List to store scales
        List<GameObject> myCardObjects = new List<GameObject>();

        for (int n = 0; n < 2; n++)
        {
            int i = (startingPlayerNoCounter + n) % 2;
            relativeIndex = (i - thisPlayerNumber + playerCount) % playerCount;
            

            if (relativeIndex == 0)
            {
                gameManager.myCards = new List<string>();
            }

            for (int j = 0; j < 4; j++)
            {
                string uniqueCardID = playerHands[i][j];
                if (relativeIndex == 0)
                {
                    gameManager.myCards.Add(uniqueCardID);
                    myCardObjects.Add(CardInteraction.cardLookup[uniqueCardID].gameObject);
                }

                //if (relativeIndex == 1) relativeIndex=2;

                GameObject tempCardObject = CardInteraction.cardLookup[uniqueCardID].gameObject;
                if (tempCardObject == null) continue;

                cardObjects.Add(tempCardObject);

                tempCardObject.transform.parent = playerHandTransforms[GetHandIndex(relativeIndex)].transform;

                var playerHandTransform = playerHandTransforms[GetHandIndex(relativeIndex)];
                Vector3 basePos = playerHandTransform.position;
                Vector3 centerRotation = playerHandTransform.rotation.eulerAngles;
                Vector3 offset = Vector3.zero;
                Quaternion rotation = Quaternion.identity;

                float randomOffset1 = UnityEngine.Random.Range(-5, 5);
                float randomOffset2 = UnityEngine.Random.Range(-100, 100);

                switch (relativeIndex)
                {
                    case 0: // Bottom (Player 0)
                        offset = new Vector3(randomOffset1 * randomOffset2, j * 5, randomOffset1 * randomOffset2);
                        rotation = Quaternion.Euler(centerRotation.x, 0 + (randomOffset1 * 3), centerRotation.z);
                        //scales.Add(new Vector3(800, 800, 800));
                        break;
                    case 1: // Top (Player 2)
                        offset = new Vector3(randomOffset1 * randomOffset2, j * 5, randomOffset1 * randomOffset2);
                        rotation = Quaternion.Euler(centerRotation.x, 180 + (randomOffset1 * 3), centerRotation.z);
                        //scales.Add(new Vector3(600, 600, 600));
                        break;
                }

                positions.Add(basePos + offset);
                rotations.Add(rotation);
                scales.Add(new Vector3(normalScale, normalScale, normalScale));
            }
        }

        yield return StartCoroutine(ChainMoveCards(positions, cardObjects, 10, rotations, scales));
        UpdateCurrentPlayerHandLayout();
        StartCoroutine(SetAutoRotateFlagTrue(myCardObjects));
        
        // Reset pitch after player dealing is complete
        if (soundEffectsController != null)
        {
            soundEffectsController.ResetPitch();
        }
        
        // After dealing to players is complete, close the deck
        
        MoveDeckToStartingPosition();

    }

    private IEnumerator DealFourPlayers(Dictionary<int, List<string>> playerHands)
    {
        
        
        // CRITICAL FIX: Wait for player number to be set (for reconnection cases)
        int retryCount = 0;
        while (thisPlayerNumber == -1 && retryCount < 50) // Wait up to 5 seconds
        {
            
            yield return new WaitForSeconds(0.1f);
            retryCount++;
        }
        
        if (thisPlayerNumber == -1)
        {
            
            yield break;
        }
        
        
        
        var cardObjects = new List<GameObject>();
        var positions = new List<Vector3>();
        var rotations = new List<Quaternion>();
        var scales = new List<Vector3>(); // List to store scales
        List<GameObject> myCardObjects = new List<GameObject>();

        for (int n = 0; n < 4; n++)
        {
            int i = (startingPlayerNoCounter + n) % 4;
            relativeIndex = (i - thisPlayerNumber + playerCount) % playerCount;
            

            if (relativeIndex == 0)
            {
                gameManager.myCards = new List<string>();
            }

            for (int j = 0; j < 4; j++)
            {
                string uniqueCardID = playerHands[i][j];
                if (relativeIndex == 0)
                {
                    gameManager.myCards.Add(uniqueCardID);
                    myCardObjects.Add(CardInteraction.cardLookup[uniqueCardID].gameObject);
                }

                GameObject tempCardObject = CardInteraction.cardLookup[uniqueCardID].gameObject;
                if (tempCardObject == null) continue;

                cardObjects.Add(tempCardObject);

                tempCardObject.transform.parent = playerHandTransforms[GetHandIndex(relativeIndex)].transform;

                var playerHandTransform = playerHandTransforms[GetHandIndex(relativeIndex)];
                Vector3 basePos = playerHandTransform.position;
                Vector3 centerRotation = playerHandTransform.rotation.eulerAngles;
                Vector3 offset = Vector3.zero;
                Quaternion rotation = Quaternion.identity;

                float randomOffset1 = UnityEngine.Random.Range(-5, 5);
                float randomOffset2 = UnityEngine.Random.Range(-100, 100);

                switch (relativeIndex)
                {
                    case 0: // Bottom (Player 0)
                        offset = new Vector3(randomOffset1 * randomOffset2, j * 5, randomOffset1 * randomOffset2);
                        rotation = Quaternion.Euler(centerRotation.x, 0 + (randomOffset1 * 3), centerRotation.z);
                        break;
                    case 1: // Right (Player 1)
                        offset = new Vector3(randomOffset1 * randomOffset2, j * 5, randomOffset1 * randomOffset2);
                        rotation = Quaternion.Euler(centerRotation.x, 90 + (randomOffset1 * 3), centerRotation.z);
                        break;
                    case 2: // Top (Player 2)
                        offset = new Vector3(randomOffset1 * randomOffset2, j * 5, randomOffset1 * randomOffset2);
                        rotation = Quaternion.Euler(centerRotation.x, 180 + (randomOffset1 * 3), centerRotation.z);
                        break;
                    case 3: // Left (Player 3)
                        offset = new Vector3(randomOffset1 * randomOffset2, j * 5, randomOffset1 * randomOffset2);
                        rotation = Quaternion.Euler(centerRotation.x, 270 + (randomOffset1 * 3), centerRotation.z);
                        break;
                }

                positions.Add(basePos + offset);
                rotations.Add(rotation);
                scales.Add(new Vector3(normalScale, normalScale, normalScale));
            }
        }

        yield return StartCoroutine(ChainMoveCards(positions, cardObjects, 10, rotations, scales));
        UpdateCurrentPlayerHandLayout();
        StartCoroutine(SetAutoRotateFlagTrue(myCardObjects));
        
        // Reset pitch after player dealing is complete
        if (soundEffectsController != null)
        {
            soundEffectsController.ResetPitch();
        }
        
        // After dealing to players is complete, close the deck
        
        MoveDeckToStartingPosition();
    }


    private int startingPlayerNoCounter = -1;
    //Deals to center according to the playerCount
    public IEnumerator DealCenter(List<string> centerCardIDs)
    {
        // Wait for deck to be at reach point before dealing
        while (isDeckMoving || !isDeckAtReachPoint)
        {
            yield return null;
        }
        
        
        
        //To make sure the center position is correct each round
        centerTransform.position = new Vector3(0, 50, 0);
        
        startingPlayerNoCounter++;
        SendCardInteractionsToGameManager();
        List<GameObject> cardObjects = new List<GameObject>();
        List<Vector3> positions = new List<Vector3>();
        List<Quaternion> rotations = new List<Quaternion>();
        List<Vector3> scales = new List<Vector3>();

        for (int i = 0; i < 4; i++)
        {
            string uniqueCardID = centerCardIDs[i];
            CardInteraction ci = CardInteraction.cardLookup[uniqueCardID];
            GameObject tempCenterCard = ci.gameObject;
            cardObjects.Add(tempCenterCard);
            if (tempCenterCard != null)
            {
                tempCenterCard.transform.parent = centerTransform;

                // Explicitly set/clear the isFirstThreeDealtCard flag on the client
                ci.isFirstThreeDealtCard = (i < 3);

                // Calculate position based on centerTransform
                Vector3 centerPosition = centerTransform.position;
                Vector3 centerRotation = centerTransform.rotation.eulerAngles;

                gameManager.centerCardsObjects.Add(tempCenterCard);
                gameManager.centerCards.Add(uniqueCardID, ci.GetCardID());

                if (i == 3)
                {
                    rotations.Add(Quaternion.Euler(centerRotation.x + 180, centerRotation.y, UnityEngine.Random.Range(-12, 12)));
                    positions.Add(new Vector3(centerPosition.x, centerPosition.y + 10, centerPosition.z));
                }
                else
                {
                    rotations.Add(Quaternion.Euler(centerRotation.x, centerRotation.y, UnityEngine.Random.Range(-12, 12)));
                    positions.Add(new Vector3(centerPosition.x, centerPosition.y, centerPosition.z));
                }
                scales.Add(new Vector3(centerScale, centerScale, centerScale));
            }
        }

        yield return StartCoroutine(ChainMoveCards(positions, cardObjects, 10, rotations, scales));

        // Move centerTransform up a little after dealing
        Vector3 originalPos = centerTransform.position;
        Vector3 targetPos = originalPos + new Vector3(0, 0, 500); // Move up by 100 units (adjust as needed)
        float duration = 0.3f;
        yield return StartCoroutine(TweenMoveTransform(centerTransform, targetPos, centerTransform.rotation, centerTransform.localScale, duration));
        
        // NOTE: Don't close the deck here - it needs to stay open for player dealing
        
        
        // After DealCenter animation/logic is done:
        GameNetworkRelay.Instance.NotifyDealCenterFinishedServerRPC(NetworkManager.Singleton.LocalClientId);

    }


    public IEnumerator DiscardCapturedCards(string playedCard, SerializableCard serializedCard, int playerNumber)
    {
        GameManager.AddToDebugLog($"[DeckController] DiscardCapturedCards called with playedCard: {playedCard}, playerNumber: {playerNumber}");
        GameManager.AddToDebugLog($"[DeckController] movePlayedLocally: {GameManager.LocalInstance.movePlayedLocally}");
        GameManager.AddToDebugLog($"[DeckController] isProcessingCapture: {GameManager.LocalInstance.isProcessingCapture}");
        GameManager.AddToDebugLog($"[DeckController] thisPlayerNumber: {thisPlayerNumber}, playerNumber: {playerNumber}");
        GameManager.AddToDebugLog($"[DeckController] Is local player: {playerNumber == thisPlayerNumber}");
        
        bool isLocalPlayer = (playerNumber == thisPlayerNumber);
        
        // Always do visual processing for captures
        GameManager.AddToDebugLog($"[DeckController] Processing capture visual updates");
        Vector3 centerPosition = new Vector3(centerTransform.position.x, centerTransform.position.y + 10 * GameManager.LocalInstance.centerCardsObjects.Count, centerTransform.position.z);
        Quaternion centerRotation = Quaternion.Euler( 90, centerTransform.rotation.y, centerTransform.rotation.z);
        yield return StartCoroutine(MoveCardCoroutine(centerPosition, CardInteraction.cardLookup[playedCard].gameObject, 10, centerRotation, new Vector3(centerScale, centerScale, centerScale)));

        List<string> selectedCenterCards = serializedCard.ToDictionary().Keys.ToList();
        List<string> selectedCards = new List<string>(selectedCenterCards);
        selectedCards.Add(playedCard);
        GameManager.LocalInstance.cardObjectsToBeDiscarted.Clear();

        //yield return StartCoroutine(PlayHandCardToCenter(playedCard, CardInteraction.cardLookup[playedCard].GetCardID(), true));

        foreach (string cardID in selectedCards)
        {
            GameObject tempCardObject = CardInteraction.cardLookup[cardID].gameObject;
            
            // CRITICAL FIX: Save world position BEFORE reparenting
            Vector3 cardWorldPosition = tempCardObject.transform.position;
            
            tempCardObject.transform.parent = null;
            
            // CRITICAL FIX: Restore world position after reparenting
            tempCardObject.transform.position = cardWorldPosition;

            if (cardID == playedCard)
            {
                //tempCardObject.transform.rotation = Quaternion.Euler(90, 0, 0);
                //tempCardObject.transform.position = (centerTransform.position + tempCardObject.transform.position) / 2;
            }

            if (tempCardObject != null)
            {
                GameManager.LocalInstance.cardObjectsToBeDiscarted.Add(tempCardObject);
            }
            else
            {
                
            }
        }

        bool piştiHappened = false;

        if (selectedCards.Count == 2)
        {
            if (CardInteraction.cardLookup[selectedCards[selectedCards.Count - 1]].GetCardID()[1] == CardInteraction.cardLookup[selectedCards[selectedCards.Count - 2]].GetCardID()[1])
            {
                
                piştiHappened = true;
            }
        }

        MoveCardsToPlayerPool(GameManager.LocalInstance.cardObjectsToBeDiscarted, playerNumber, piştiHappened);
        UpdateCurrentPlayerHandLayout();
        

        foreach (string uniqueCardId in selectedCenterCards)
        {
            var selectedCardId = CardInteraction.cardLookup[uniqueCardId].GetCardID();
            GameManager.LocalInstance.centerCards = GameManager.LocalInstance.centerCards
                .Where(card => !(card.Value[0] == selectedCardId[0] && card.Value[1] == selectedCardId[1]))
                .ToDictionary(card => card.Key, card => card.Value);
        }
        
        // Reset flags and call TellServerTurnEnded for local player captures
        if (isLocalPlayer)
        {
            GameManager.AddToDebugLog($"[DeckController] This is local player's capture, calling TellServerTurnEnded()");
            //gameManager.TellServerTurnEnded();
            GameManager.AddToDebugLog($"[DeckController] TellServerTurnEnded() called for local player capture");
            GameManager.AddToDebugLog($"[DeckController] Setting movePlayedLocally to false");
            GameManager.LocalInstance.movePlayedLocally = false;
            CardInteraction.isOneCardSelected = false;
            GameManager.LocalInstance.currentSelectedHandCard = null;
        }
        else
        {
            GameManager.AddToDebugLog($"[DeckController] This is non-local player's capture, no flag reset needed");
        }
        
        yield return null;
    }

    private void SendCardInteractionsToGameManager()
    {
        gameManager.GetCardInteractionScripts(cardInteractionList);
    }

    public IEnumerator PlayHandCardToCenter(string uniqueCardID, int[] cardID, bool isDiscarded = false)
    {
        GameManager.AddToDebugLog($"[SingleplayerCardPlay] PlayHandCardToCenter START: cardID={uniqueCardID}, isDiscarded={isDiscarded}");
        if (CardInteraction.cardLookup == null)
        {
            GameManager.AddToDebugLog("[SingleplayerCardPlay][Diag] cardLookup is NULL in PlayHandCardToCenter");
            yield break;
        }
        if (!CardInteraction.cardLookup.ContainsKey(uniqueCardID))
        {
            GameManager.AddToDebugLog($"[SingleplayerCardPlay][Diag] cardLookup MISSING key for {uniqueCardID}");
            yield break;
        }
        GameObject placedCard = CardInteraction.cardLookup[uniqueCardID].gameObject;
        GameManager.AddToDebugLog($"[SingleplayerCardPlay][Diag] placedCard resolved: {(placedCard != null ? placedCard.name : "NULL")}");
        List<Vector3> positions = new List<Vector3>();
        List<GameObject> cardObjects = new List<GameObject>();
        List<Quaternion> rotations = new List<Quaternion>();
        List<Vector3> scales = new List<Vector3>();

        if (placedCard != null)
        {
            // Normalize card state before sending it to center so no face-down idle rotation leaks.
            CardInteraction placedInteraction = placedCard.GetComponent<CardInteraction>();
            if (placedInteraction != null)
            {
                placedInteraction.StopAutoRotate();
                placedInteraction.KillAllTweens();
            }

            // CRITICAL FIX: Save world position BEFORE reparenting
            // This prevents the card from jumping when parent changes
            Vector3 currentWorldPosition = placedCard.transform.position;

            Vector3 centerPosition = centerTransform.position;
            Vector3 centerRotation = centerTransform.rotation.eulerAngles;

            rotations.Add(Quaternion.Euler(centerRotation.x + 180, centerRotation.y, UnityEngine.Random.Range(-12, 12)));
            
            // Change parent
            placedCard.transform.parent = centerTransform;
            
            // CRITICAL FIX: Restore world position after reparenting
            // This ensures the card stays where it was dragged instead of jumping
            placedCard.transform.position = currentWorldPosition;

            // Use centerTransform for positioning
            positions.Add(new Vector3(centerPosition.x, centerPosition.y + 10 * GameManager.LocalInstance.centerCardsObjects.Count, centerPosition.z));
            scales.Add(new Vector3(centerScale, centerScale, centerScale));

            gameManager.centerCards.Add(uniqueCardID, cardID);
            gameManager.centerCardsObjects.Add(placedCard);
            cardObjects.Add(placedCard);
            GameManager.AddToDebugLog($"[SingleplayerCardPlay][Diag] Added card to animation lists. currentWorldPos={currentWorldPosition}, targetPos={positions[positions.Count - 1]}, parentNow={placedCard.transform.parent?.name}");
        }
        else
        {
            GameManager.AddToDebugLog($"[SingleplayerCardPlay][Diag] placedCard is NULL for {uniqueCardID} before animation setup");
        }

        GameManager.AddToDebugLog($"[SingleplayerCardPlay][Diag] Pre-chain counts: cardObjects={cardObjects.Count}, positions={positions.Count}, rotations={rotations.Count}, scales={scales.Count}, centerCardsObjects={gameManager.centerCardsObjects.Count}");

        if (cardObjects.Count == 0 || positions.Count == 0 || rotations.Count == 0 || scales.Count == 0)
        {
            GameManager.AddToDebugLog("[SingleplayerCardPlay][Diag] Aborting animation chain due to empty/misaligned lists");
            UpdateCurrentPlayerHandLayout();
            yield break;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAudio(3, 1, false);
            GameManager.AddToDebugLog("[SingleplayerCardPlay][Diag] AudioManager.PlayAudio succeeded");
        }
        else
        {
            GameManager.AddToDebugLog("[SingleplayerCardPlay][Diag] AudioManager.Instance is NULL; skipping PlayAudio");
        }
        GameManager.AddToDebugLog($"[SingleplayerCardPlay] PlayHandCardToCenter: Starting animation chain with {cardObjects.Count} cards");
        if (isDiscarded) yield return StartCoroutine(ChainMoveCardsCoroutine(positions, cardObjects, 10, rotations, scales));
        else yield return StartCoroutine(ChainMoveCards(positions, cardObjects, 10, rotations, scales, true));
        GameManager.AddToDebugLog($"[SingleplayerCardPlay] PlayHandCardToCenter: Animation complete, updating hand layout");
        UpdateCurrentPlayerHandLayout();
    }

    public void UpdateCurrentPlayerHandLayout()
    {
        
        
        if (isShowcaseAllActive)
        {
            
            //ShowcaseAllOtherHandsLayout();
            return;
        }

        if (playerCount == 2)
        {
            for (int i = 0; i < 2; i++)
            {
                UpdateCurrentPlayerHandLayoutTwoPlayers(i);
            }
        }
        else if (playerCount == 4)
        {
            for (int i = 0; i < 4; i++)
            {
                UpdateCurrentPlayerHandLayoutFourPlayers(i);
            }
        }

        /*for (int handIdx = 0; handIdx < playerHandTransforms.Count; handIdx++)
        {
            foreach (Transform child in playerHandTransforms[handIdx])
            {
                var ci = child.GetComponent<CardInteraction>();
                if (ci == null) continue;
                if (handIdx == 0)
                {
                    //ci.StartAutoRotate();
                }
                else
                {
                    ci.StopAutoRotate();
                }
            }
        }*/
    }

    [ContextMenu("ShowcaseAllOtherHands")]
    private void ShowcaseAllOtherHandsLayout()
    {
        
        
        
        
        float spacing = 500f;
        int handCount = playerHandTransforms.Count;
        int startIdx = showcaseOnlyOwnHand ? 0 : (showcaseIncludeOwnHand ? 0 : 1);
        int endExclusive = showcaseOnlyOwnHand ? Mathf.Min(1, handCount) : handCount;

        // Layout for all hands (showcase)
        
        for (int handIdx = startIdx; handIdx < endExclusive; handIdx++)
        {
            Transform hand = playerHandTransforms[handIdx];
            if (hand == null)
            {
                
                continue;
            }
            
            
            var playerCards = new List<GameObject>();
            int counter = 0;
            foreach (Transform child in hand)
            {
                if (counter >= 0) playerCards.Add(child.gameObject);
                counter++;
            }
            int totalCards = playerCards.Count;
            
            if (totalCards == 0) continue;

            float offsetMult = (totalCards - 1) / 2f;
            Vector3 basePos = hand.position;
            Vector3 centerRotation = hand.rotation.eulerAngles;

            for (int i = 0; i < totalCards; i++)
            {
                GameObject card = playerCards[i];
                if (card == null)
                {
                    
                    continue;
                }
                
                var ci = card.GetComponent<CardInteraction>();
                if (ci == null)
                {
                    
                    continue;
                }

                // Store original transform and flags if not already stored
                if (!showcaseOriginalTransforms.ContainsKey(card))
                {
                    
                    showcaseOriginalTransforms[card] = (card.transform.position, card.transform.rotation, card.transform.localScale, ci.isAutoRotating);
                }
                else
                {
                    
                }

                Vector3 offset = Vector3.zero;
                Quaternion rotation = card.transform.rotation; // Default to current rotation
                Vector3 scale = new Vector3(centerScale, centerScale, centerScale); // All cards use center scale
                Vector3 targetPosition = Vector3.zero;

                switch (handIdx)
                {
                    case 0: // Bottom (Your hand - Player 0)
                        offset = new Vector3(spacing * 3f * (i - offsetMult), (i * 10)+1000, 0);
                        targetPosition = basePos + offset;
                        
                        // For your cards: Only change position and scale, keep current rotation
                        StartCoroutine(MoveCardPositionAndScaleOnly(card, targetPosition, scale));
                        break;
                        
                    case 1: // Right (PlayerHand2) - shift +1000 on x-axis
                        offset = new Vector3(1000, (i * 10) + 1000, spacing * 3f * (i - offsetMult));
                        rotation = Quaternion.Euler(centerRotation.x, centerRotation.y + 90, centerRotation.z);
                        targetPosition = basePos + offset;
                        
                        MoveCard(targetPosition, card, 10, rotation, scale);
                        break;
                        
                    case 3: // Left (PlayerHand4) - shift -1000 on x-axis
                        offset = new Vector3(-1000, (i * 10) + 1000, spacing * 3f * (i - offsetMult));
                        rotation = Quaternion.Euler(centerRotation.x, centerRotation.y + 90, centerRotation.z);
                        targetPosition = basePos + offset;
                        
                        MoveCard(targetPosition, card, 10, rotation, scale);
                        break;
                        
                    case 2: // Top
                        offset = new Vector3(spacing * 3f * (i - offsetMult), (i * 10) + 1000, 0);
                        rotation = Quaternion.Euler(centerRotation.x, centerRotation.y, centerRotation.z);
                        targetPosition = basePos + offset;
                        
                        MoveCard(targetPosition, card, 10, rotation, scale);
                        break;
                        
                    default:
                        offset = new Vector3(spacing * 3f * (i - offsetMult), i * 10, 0);
                        rotation = Quaternion.Euler(centerRotation.x, centerRotation.y, centerRotation.z);
                        targetPosition = basePos + offset;
                        
                        MoveCard(targetPosition, card, 10, rotation, scale);
                        break;
                }

                // Set auto-rotate behavior based on hand
                if (handIdx == 0) // Your hand (Player 0)
                {
                    // Keep auto-rotate as is for your cards (don't change it)
                    // Your cards continue auto-rotating during showcase
                }
                else // Other players' hands
                {
                    ci.StartAutoRotateFaceDown();
                }
            }
        }
    }

    /// <summary>
    /// Moves a card's position and scale only, preserving its current rotation
    /// </summary>
    private IEnumerator MoveCardPositionAndScaleOnly(GameObject cardObject, Vector3 targetPosition, Vector3 targetScale)
    {
        float duration = 0.1f; // Same as speed 10 in MoveCard (1f/10)

        // Create a DOTween sequence for position and scale only (no rotation)
        DG.Tweening.Sequence moveSeq = DOTween.Sequence();
        moveSeq.Join(cardObject.transform.DOMove(targetPosition, duration));
        moveSeq.Join(cardObject.transform.DOScale(targetScale, duration));

        yield return moveSeq.WaitForCompletion();
    }


    public bool isShowcaseAllActive = false;
    private bool showcaseIncludeOwnHand = true;
    private bool showcaseOnlyOwnHand = false;
    public Dictionary<GameObject, (Vector3 pos, Quaternion rot, Vector3 scale, bool autoRotateFlag)> showcaseOriginalTransforms = new Dictionary<GameObject, (Vector3, Quaternion, Vector3, bool)>();
    private readonly List<GameObject> degisTokusDebugIndicators = new List<GameObject>();

    public void ShowDegisTokusDebugCandidates()
    {
        ClearDegisTokusDebugCandidates();

        if (playerHandTransforms == null)
        {
            return;
        }

        for (int handIdx = 1; handIdx < playerHandTransforms.Count; handIdx++)
        {
            Transform hand = playerHandTransforms[handIdx];
            if (hand == null)
            {
                continue;
            }

            int childIndex = 0;
            foreach (Transform child in hand)
            {

                if (child == null)
                {
                    childIndex++;
                    continue;
                }

                CardInteraction ci = child.GetComponent<CardInteraction>();
                if (ci == null)
                {
                    childIndex++;
                    continue;
                }

                ci.ShowDebugCandidateIndicator(new Color(0.1f, 1f, 0.1f, 0.75f));
                degisTokusDebugIndicators.Add(child.gameObject);
                childIndex++;
            }
        }
    }

    public void ClearDegisTokusDebugCandidates()
    {
        for (int i = 0; i < degisTokusDebugIndicators.Count; i++)
        {
            GameObject go = degisTokusDebugIndicators[i];
            if (go == null)
            {
                continue;
            }

            CardInteraction ci = go.GetComponent<CardInteraction>();
            if (ci != null)
            {
                ci.HideDebugCandidateIndicator();
            }
        }

        degisTokusDebugIndicators.Clear();
    }

    [ContextMenu("ExitShowcaseAllOtherHands")]
    public void ExitShowcaseAllOtherHands()
    {
        
        
        
        
        isShowcaseAllActive = false;
        showcaseOnlyOwnHand = false;
        
        
        // CRITICAL FIX: Don't restore cards immediately if peek animations are active
        // This prevents the twitching conflict between showcase restoration and peek animations
        if (IsPeekAnimationActive())
        {
            
            StartCoroutine(DelayedShowcaseRestoration());
            return;
        }
        
        // Restore all cards to their stored transforms and flags
        
        RestoreShowcaseCards();
    }
    
    /// <summary>
    /// Check if any peek animations are currently active
    /// </summary>
    private bool IsPeekAnimationActive()
    {
        // Check if SuperPowerToken has a peek power active
        if (SuperPowerToken.ActiveInstance != null && 
            (SuperPowerToken.ActiveInstance.superPowerClassName == "UcundanGözAt" || 
             SuperPowerToken.ActiveInstance.superPowerClassName == "BayaBayaBak"))
        {
            return true;
        }
        
        // Additional check: Look for cards that are currently being peeked
        // (This is a backup check in case the SuperPowerToken check fails)
        foreach (var kvp in showcaseOriginalTransforms)
        {
            GameObject card = kvp.Key;
            if (card != null)
            {
                // Check if card is currently animating (being peeked)
                if (DOTween.IsTweening(card.transform))
                {
                    
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Delayed showcase restoration for when peek animations are active
    /// </summary>
    private IEnumerator DelayedShowcaseRestoration()
    {
        
        
        // Wait a bit longer than the peek animations to ensure they complete
        float waitTime = 5.0f; // Safe buffer time
        yield return new WaitForSeconds(waitTime);
        
        
        RestoreShowcaseCards();
    }
    
    /// <summary>
    /// Actually restore the showcase cards to their original positions
    /// </summary>
    private void RestoreShowcaseCards()
    {
        int restoredCount = 0;
        foreach (var kvp in showcaseOriginalTransforms)
        {
            GameObject card = kvp.Key;
            var (pos, rot, scale, autoRotateFlag) = kvp.Value;
            
            if (card == null)
            {
                
                continue;
            }
            
            
            MoveCard(pos, card, 10, rot, scale);
            restoredCount++;

            var ci = card.GetComponent<CardInteraction>();
            if (ci != null)
            {
                ci.StopAutoRotate();
                
            }
            else
            {
                
            }
        }
        
        
        showcaseOriginalTransforms.Clear();

        // After restoring, update layout to ensure flags are correct for your hand
        
        UpdateCurrentPlayerHandLayout();
    }

    public void UpdateShowcaseOriginalsAfterSwap(GameObject card)
    {
        if (isShowcaseAllActive && showcaseOriginalTransforms.ContainsKey(card))
        {
            var ci = card.GetComponent<CardInteraction>();
            bool autoRotate = ci != null ? false : false;
            showcaseOriginalTransforms[card] = (card.transform.position, card.transform.rotation, card.transform.localScale, autoRotate);
        }
    }

    public void ShowcaseAllOtherHands(bool includeOwnHand = true)
    {
        
        
        
        
        isShowcaseAllActive = true;
        showcaseIncludeOwnHand = includeOwnHand;
        showcaseOnlyOwnHand = false;
        
        
        ShowcaseAllOtherHandsLayout();
        
    }

    public void ShowcaseOnlyOwnHand()
    {
        

        isShowcaseAllActive = true;
        showcaseIncludeOwnHand = true;
        showcaseOnlyOwnHand = true;

        ShowcaseAllOtherHandsLayout();
        
    }

    /// <summary>
    /// Temporarily de-emphasize local hand cards by scaling them to normalScale.
    /// Used between dual-selection phases when own-card selection is completed.
    /// </summary>
    public void TemporarilySetOwnHandToNormalScale()
    {
        if (playerHandTransforms == null || playerHandTransforms.Count == 0)
        {
            return;
        }

        Transform ownHand = playerHandTransforms[0];
        if (ownHand == null)
        {
            return;
        }

        Vector3 targetScale = new Vector3(normalScale, normalScale, normalScale);
        foreach (Transform child in ownHand)
        {
            if (child != null && (child.name.Contains("Pool") || child.name.Contains("Pişti")))
            {
                continue;
            }

            if (child != null)
            {
                StartCoroutine(MoveCardPositionAndScaleOnly(child.gameObject, child.position, targetScale));
            }
        }
    }


    private void UpdateCurrentPlayerHandLayoutTwoPlayers(int playerNumber = -1)
    {
        if (playerNumber == 1) playerNumber = 2;

        // Define the spacing for the layout
        float spacing = 150;

        // Get the parent object of the current player's hand
        Transform currentPlayerHand;
        if (playerNumber != -1)
        {
            currentPlayerHand = playerHandTransforms[playerNumber];
        }
        else
        {
            currentPlayerHand = playerHandTransforms[GameManager.currentPlayerNo];
            playerNumber = GameManager.currentPlayerNo;
        }


        if (currentPlayerHand.childCount == 1)
        {
            return;
        }

        // Get all the cards that are children of the current player's hand
        var playerCards = new List<GameObject>();
        foreach (Transform child in currentPlayerHand)
        {
            if (child != null && !(child.name.Contains("Pool") || child.name.Contains("Pişti")))
            {
                playerCards.Add(child.gameObject);
            }
        }

        int totalCards = playerCards.Count;
        if (totalCards == 0) return;

        // Calculate the offset multiplier
        float offsetMult = (totalCards - 1) / 2f;

        // Arrange cards
        for (int i = 0; i < totalCards; i++)
        {
            Vector3 offset = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            Vector3 centerRotation = playerHandTransforms[relativeIndex].rotation.eulerAngles;

            Vector3 currentScale = playerCards[i].transform.localScale; // Use the current scale of the card

            float cardZ = playerCards[i].transform.rotation.eulerAngles.y;
            Quaternion cardRotation = playerCards[i].transform.rotation;
            switch (playerNumber)
            {
                case 0: // Bottom (Player 0)
                    offset = new Vector3(spacing * 15f * (i - offsetMult), 1000, 0);
                    rotation = cardRotation;//Quaternion.Euler(-centerRotation.x, cardZ, centerRotation.z);
                    currentScale = new Vector3(myCardsScale, myCardsScale, myCardsScale);
                    break;
                case 2: // Top (Player 2)
                    offset = new Vector3(spacing * 3f * (i - offsetMult), 1000 + (i * 10), 0);
                    rotation = Quaternion.Euler(centerRotation.x, cardZ , centerRotation.z);
                    currentScale = new Vector3(normalScale, normalScale, normalScale);
                    break;
            }

            Vector3 targetPosition = playerHandTransforms[playerNumber].position + offset;
            MoveCard(targetPosition, playerCards[i], 10, rotation, currentScale);
        }
        //if (playerNumber == 0) StartCoroutine(SetAutoRotateFlagTrue(playerCards));
        //
    }

    private void UpdateCurrentPlayerHandLayoutFourPlayers(int playerNumber = -1)
    {
        // Define the spacing for the layout
        float spacing = 150;

        // Get the parent object of the current player's hand
        Transform currentPlayerHand;
        if (playerNumber != -1)
        {
            currentPlayerHand = playerHandTransforms[playerNumber];
        }
        else
        {
            currentPlayerHand = playerHandTransforms[GameManager.currentPlayerNo];
            playerNumber = GameManager.currentPlayerNo;
        }

        if (currentPlayerHand.childCount == 1)
        {
            return;
        }

        // Get all the cards that are children of the current player's hand
        var playerCards = new List<GameObject>();
        foreach (Transform child in currentPlayerHand)
        {
            if (child != null && !(child.name.Contains("Pool") || child.name.Contains("Pişti")))
            {
                playerCards.Add(child.gameObject);
            }
        }

        int totalCards = playerCards.Count;
        if (totalCards == 0) return;

        // Calculate the offset multiplier
        float offsetMult = (totalCards - 1) / 2f;

        // Arrange cards
        for (int i = 0; i < totalCards; i++)
        {
            Vector3 offset = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            Vector3 centerRotation = playerHandTransforms[playerNumber].rotation.eulerAngles;

            Vector3 currentScale = playerCards[i].transform.localScale; // Use the current scale of the card

            float cardZ = playerCards[i].transform.rotation.eulerAngles.y;
            Quaternion cardRotation = playerCards[i].transform.rotation;

            switch (playerNumber)
            {
                case 0: // Bottom (Player 0)
                    offset = new Vector3(spacing * 15f * (i - offsetMult), 1000, 0);
                    rotation = cardRotation;//Quaternion.Euler(-centerRotation.x, cardZ, centerRotation.z);
                    currentScale = new Vector3(myCardsScale, myCardsScale, myCardsScale);
                    break;
                case 1: // Right (Player 1)
                    offset = new Vector3(0, i * 10, spacing * 3f * (i - offsetMult));
                    rotation = Quaternion.Euler(centerRotation.x, cardZ, centerRotation.z);
                    currentScale = new Vector3(normalScale, normalScale, normalScale);
                    break;
                case 2: // Top (Player 2)
                    offset = new Vector3(spacing * 3f * (i - offsetMult), 1000 + (i * 10), 0);
                    rotation = Quaternion.Euler(centerRotation.x, cardZ, centerRotation.z);
                    currentScale = new Vector3(normalScale, normalScale, normalScale);
                    break;
                case 3: // Left (Player 3)
                    offset = new Vector3(0, i * 10, spacing * 3f * (i - offsetMult));
                    rotation = Quaternion.Euler(centerRotation.x, cardZ, centerRotation.z);
                    currentScale = new Vector3(normalScale, normalScale, normalScale);
                    break;
            }

            Vector3 targetPosition = playerHandTransforms[playerNumber].position + offset;
            MoveCard(targetPosition, playerCards[i], 10, rotation, currentScale);
        }
    }


    public IEnumerator SetAutoRotateFlagTrue(List<GameObject> cardObjects)
    {
        //
        yield return new WaitForSeconds(0.2f);
        // Search the list for a CardInteraction with the matching GameObject
        foreach (var cardObject in cardObjects)
        {
            foreach (var cardInteraction in cardInteractionList)
            {
                if (cardInteraction.gameObject == cardObject)
                {
                    cardInteraction.StartAutoRotate(); // Return the matching CardInteraction
                }
            }
        }

    }

    public void SetAutoRotateFlagFalse(GameObject cardObject)
    {
        //

        foreach (var cardInteraction in cardInteractionList)
        {
            if (cardInteraction.gameObject == cardObject)
            {
                cardInteraction.StopAutoRotate();// Return the matching CardInteraction
                return;
            }
        }



        //

    }

    private Dictionary<GameObject, (Vector3 pos, Quaternion rot, Vector3 scale)> poolCardOriginalTransforms = new Dictionary<GameObject, (Vector3, Quaternion, Vector3)>();
    private bool isShowcasing = false;
    private int showcasedPoolIndex = -1;
    
    [Header("Center Card Showcase Settings")]
    [SerializeField] private float centerShowcaseSpacing = 800f; // Spacing between showcased center cards
    [SerializeField] private float centerShowcaseXOffset = 0f; // X offset for showcased center cards
    [SerializeField] private float centerShowcaseYOffset = 1500f; // Y offset for showcased center cards
    [SerializeField] private float centerShowcaseZOffset = -500f; // Z offset for showcased center cards
    [SerializeField] private float centerShowcaseScale = 1200f; // Scale for showcased center cards
    [SerializeField] private float centerShowcaseMoveDuration = 0.4f; // Movement duration for showcase animation
    
    private bool isCenterShowcasing = false;
    private bool isCenterShowcaseAnimating = false; // Flag to track if showcase animation is running
    private Dictionary<GameObject, (Vector3 pos, Quaternion rot, Vector3 scale)> centerShowcaseOriginalTransforms = new Dictionary<GameObject, (Vector3, Quaternion, Vector3)>();
    
    [ContextMenu("Showcase Player Pool Cards")]
    public void ShowcasePlayerPoolCards()
    {
        ShowcasePlayerPoolCards(0);
    }

    public void ShowcasePlayerPoolCards(int poolIndex)
    {
        if (poolIndex < 0 || poolIndex >= playerPoolTransforms.Count || poolIndex >= playerPiştiPoolTransforms.Count)
        {
            
            return;
        }

        isShowcasing = true;
        showcasedPoolIndex = poolIndex;
        poolCardOriginalTransforms.Clear();
        var poolTransform = playerPoolTransforms[poolIndex];
        var piştiPoolTransform = playerPiştiPoolTransforms[poolIndex];
        var centerRotation = poolTransform.rotation.eulerAngles;

        // --- Regular Pool Cards ---
        var poolCards = new List<GameObject>();
        foreach (Transform child in poolTransform)
        {
            var card = child.gameObject;
            poolCards.Add(card);
            if (!poolCardOriginalTransforms.ContainsKey(card))
                poolCardOriginalTransforms[card] = (card.transform.position, card.transform.rotation, card.transform.localScale);
        }

        // --- Pişti Pool Cards ---
        var piştiCards = new List<GameObject>();
        foreach (Transform child in piştiPoolTransform)
        {
            var card = child.gameObject;
            piştiCards.Add(card);
            if (!poolCardOriginalTransforms.ContainsKey(card))
                poolCardOriginalTransforms[card] = (card.transform.position, card.transform.rotation, card.transform.localScale);
        }

        // --- Layout Regular Pool Cards (sorted/unsorted face down depending on side) ---
        if (poolCards.Count > 0)
        {
            if (poolIndex == 0) // Owner: Sort all cards and show them face-up as normal
            {
                poolCards.Sort((a, b) =>
                {
                    int aKind = 0, aValue = 0, bKind = 0, bValue = 0;
                    var aParts = a.tag.Split('_');
                    var bParts = b.tag.Split('_');
                    if (aParts.Length == 2) { int.TryParse(aParts[0], out aKind); int.TryParse(aParts[1], out aValue); }
                    if (bParts.Length == 2) { int.TryParse(bParts[0], out bKind); int.TryParse(bParts[1], out bValue); }
                    int kindCompare = aKind.CompareTo(bKind);
                    return kindCompare != 0 ? kindCompare : aValue.CompareTo(bValue);
                });

                float spacing = 600f;
                float offsetMult = (poolCards.Count - 1) / 2f;
                float baseZ = centerTransform.position.z - 1200;
                for (int i = 0; i < poolCards.Count; i++)
                {
                    Quaternion rotation = Quaternion.Euler(90, 0, 0); // Face up
                    Vector3 targetPosition = new Vector3(spacing * (i - offsetMult), (i * 10) + centerTransform.position.y + 5000, baseZ);
                    Vector3 targetScale = new Vector3(myCardsScale, myCardsScale, myCardsScale);
                    MoveCard(targetPosition, poolCards[i], 10, rotation, targetScale);
                }
            }
            else // Opponent: First 3 dealt center cards stay face-down and unsorted at the beginning of the list
            {
                var firstThree = new List<GameObject>();
                var others = new List<GameObject>();

                foreach (var card in poolCards)
                {
                    var ci = card.GetComponent<CardInteraction>();
                    if (ci != null && ci.isFirstThreeDealtCard)
                    {
                        firstThree.Add(card);
                    }
                    else
                    {
                        others.Add(card);
                    }
                }

                // Sort only subsequent cards (others)
                others.Sort((a, b) =>
                {
                    int aKind = 0, aValue = 0, bKind = 0, bValue = 0;
                    var aParts = a.tag.Split('_');
                    var bParts = b.tag.Split('_');
                    if (aParts.Length == 2) { int.TryParse(aParts[0], out aKind); int.TryParse(aParts[1], out aValue); }
                    if (bParts.Length == 2) { int.TryParse(bParts[0], out bKind); int.TryParse(bParts[1], out bValue); }
                    int kindCompare = aKind.CompareTo(bKind);
                    return kindCompare != 0 ? kindCompare : aValue.CompareTo(bValue);
                });

                // Combine: unsorted first-three cards at the beginning, followed by sorted others
                var combinedPoolCards = new List<GameObject>();
                combinedPoolCards.AddRange(firstThree);
                combinedPoolCards.AddRange(others);

                float spacing = 600f;
                float offsetMult = (combinedPoolCards.Count - 1) / 2f;
                float baseZ = centerTransform.position.z - 1200;
                for (int i = 0; i < combinedPoolCards.Count; i++)
                {
                    var card = combinedPoolCards[i];
                    var ci = card.GetComponent<CardInteraction>();
                    bool isFaceDown = ci != null && ci.isFirstThreeDealtCard;
                    
                    Quaternion rotation = isFaceDown ? Quaternion.Euler(-90, 0, 0) : Quaternion.Euler(90, 0, 0);
                    Vector3 targetPosition = new Vector3(spacing * (i - offsetMult), (i * 10) + centerTransform.position.y + 5000, baseZ);
                    Vector3 targetScale = new Vector3(myCardsScale, myCardsScale, myCardsScale);
                    MoveCard(targetPosition, card, 10, rotation, targetScale);
                }
            }
        }

        // --- Layout Pişti Pool Cards (grouped in pairs, no sorting) ---
        if (piştiCards.Count > 0)
        {
            float pairSpacing = 3000f;
            float cardSpacingInPair = 100f;
            int pairCount = piştiCards.Count / 2;
            float offsetMult = (pairCount - 1) / 2f;
            float baseZ = centerTransform.position.z + 1200f; // Above regular pool cards in Z axis

            for (int pair = 0; pair < pairCount; pair++)
            {
                // Each pair: piştiCards[2*pair], piştiCards[2*pair+1]
                for (int j = 0; j < 2; j++)
                {
                    int cardIdx = pair * 2 + j;
                    Quaternion rotation = Quaternion.identity;
                    if (j == 1) rotation = Quaternion.Euler(90, 0, 0);
                    else if (j == 0) rotation = Quaternion.Euler(90, 90, 0);
                    Vector3 targetPosition = new Vector3(
                        pairSpacing * (pair - offsetMult) + (j - 10f) * cardSpacingInPair,
                        (j * 10) + centerTransform.position.y + 5000,
                        baseZ
                    );
                    Vector3 targetScale = new Vector3(myCardsScale, myCardsScale, myCardsScale);
                    MoveCard(targetPosition, piştiCards[cardIdx], 10, rotation, targetScale);
                }
            }
        }
    }

    [ContextMenu("Stop Showcase Player Pool Cards")]
    public void StopShowcasePlayerPoolCards()
    {
        isShowcasing = false;
        showcasedPoolIndex = -1;

        foreach (var kvp in poolCardOriginalTransforms)
        {
            var card = kvp.Key;
            var original = kvp.Value;
            if (card != null)
            {
                MoveCard(original.pos, card, 10, original.rot, original.scale);
            }
        }

        poolCardOriginalTransforms.Clear();
    }

    public bool IsPoolShowcasing()
    {
        return isShowcasing;
    }

    public int GetShowcasingPoolIndex()
    {
        return showcasedPoolIndex;
    }

    public int GetPoolIndexFromParentName(string parentName)
    {
        if (string.IsNullOrEmpty(parentName))
        {
            return -1;
        }

        if (!parentName.StartsWith("PlayerPool") && !parentName.StartsWith("PlayerPiştiPool"))
        {
            return -1;
        }

        int suffixStart = parentName.Length - 1;
        while (suffixStart >= 0 && char.IsDigit(parentName[suffixStart]))
        {
            suffixStart--;
        }

        string numberPart = parentName.Substring(suffixStart + 1);
        if (!int.TryParse(numberPart, out int oneBasedPoolNo))
        {
            return -1;
        }

        int zeroBasedPoolIndex = oneBasedPoolNo - 1;
        if (zeroBasedPoolIndex < 0 || zeroBasedPoolIndex >= playerPoolTransforms.Count)
        {
            return -1;
        }

        return zeroBasedPoolIndex;
    }

    [ContextMenu("Showcase Center Cards")]
    public void ShowcaseCenterCards()
    {
        if (isCenterShowcasing) return; // Already showcasing
        if (isCenterShowcaseAnimating) return; // Animation in progress, ignore
        
        // OPTION 2: Force-kill all center animations before starting
        KillAllCenterCardAnimations();
        
        isCenterShowcasing = true;
        isCenterShowcaseAnimating = true; // Set animation flag
        centerShowcaseOriginalTransforms.Clear();
        
        // Get all center cards
        List<GameObject> centerCards = new List<GameObject>();
        foreach (Transform child in centerTransform)
        {
            centerCards.Add(child.gameObject);
        }
        
        if (centerCards.Count == 0) 
        {
            isCenterShowcasing = false;
            return;
        }
        
        // Store original transforms
        foreach (var card in centerCards)
        {
            centerShowcaseOriginalTransforms[card] = (card.transform.position, card.transform.rotation, card.transform.localScale);
        }
        
        // Calculate positions based on distance from middle
        float spacing = centerShowcaseSpacing;
        float middle = centerCards.Count / 2f;
        Vector3 centerPosition = centerTransform.position;
        float baseZ = centerPosition.z + centerShowcaseZOffset;
        
        // Move cards to showcase positions using distance from middle logic
        for (int i = 0; i < centerCards.Count; i++)
        {
            GameObject card = centerCards[i];
            
            // Kill any existing animations on the card before moving
            var cardInteraction = card.GetComponent<CardInteraction>();
            if (cardInteraction != null)
            {
                cardInteraction.KillAllTweens();
            }
            
            // Calculate offset from middle: negative = left, positive = right
            float offsetFromMiddle = i - middle;
            
            Vector3 showcasePosition = new Vector3(
                centerPosition.x + centerShowcaseXOffset + (offsetFromMiddle * spacing),
                centerPosition.y + centerShowcaseYOffset + (i * 10),
                baseZ
            );
            
            // Keep original rotation, only change position and scale
            Vector3 showcaseScale = new Vector3(centerShowcaseScale, centerShowcaseScale, centerShowcaseScale);
            
            // Use the existing MoveCard method but preserve rotation
            StartCoroutine(MoveCardCoroutineShowcase(showcasePosition, card, 1f/centerShowcaseMoveDuration, card.transform.rotation, showcaseScale));
        }
    }
    
    [ContextMenu("Stop Showcase Center Cards")]
    public void StopShowcaseCenterCards()
    {
        if (!isCenterShowcasing) return;
        if (isCenterShowcaseAnimating) return; // Animation in progress, ignore
        
        isCenterShowcasing = false;
        isCenterShowcaseAnimating = true; // Set animation flag for closing
        
        // Restore all center cards to their original positions
        foreach (var kvp in centerShowcaseOriginalTransforms)
        {
            GameObject card = kvp.Key;
            var (pos, rot, scale) = kvp.Value;
            
            if (card != null)
            {
                // Kill any existing animations on the card before restoring
                var cardInteraction = card.GetComponent<CardInteraction>();
                if (cardInteraction != null)
                {
                    cardInteraction.KillAllTweens();
                }
                
                StartCoroutine(MoveCardCoroutineShowcase(pos, card, 1f/centerShowcaseMoveDuration, rot, scale));
            }
        }
        
        centerShowcaseOriginalTransforms.Clear();
    }
    
    /// <summary>
    /// Separate coroutine for showcase movement to avoid conflicts with regular MoveCard
    /// </summary>
    private IEnumerator MoveCardCoroutineShowcase(Vector3 endPos, GameObject cardObject, float speedMultiplier, Quaternion rotation, Vector3 scale)
    {
        // If instant mode (reconnection), teleport immediately
        if (isInstantMode)
        {
            cardObject.transform.position = endPos;
            cardObject.transform.rotation = rotation;
            cardObject.transform.localScale = scale;
            CheckAndClearShowcaseAnimationFlag();
            yield return null;
        }
        else
        {
            float duration = 1f/speedMultiplier;

            // Create a DOTween sequence for position, rotation, and scale
            DG.Tweening.Sequence moveSeq = DOTween.Sequence();
            moveSeq.Join(cardObject.transform.DOMove(endPos, duration));
            moveSeq.Join(cardObject.transform.DORotateQuaternion(rotation, duration));
            moveSeq.Join(cardObject.transform.DOScale(scale, duration));

            yield return moveSeq.WaitForCompletion();
            
            // Clear animation flag when this specific card's animation completes
            // Note: This will be called for each card, but we only need to clear it once
            // We'll use a counter or check if all animations are done
            CheckAndClearShowcaseAnimationFlag();
        }
    }
    

    
    /// <summary>
    /// Public method to check if center cards are being showcased
    /// </summary>
    public bool IsCenterShowcasing()
    {
        return isCenterShowcasing;
    }
    
    /// <summary>
    /// Public method to check if center showcase animation is currently running
    /// </summary>
    public bool IsCenterShowcaseAnimating()
    {
        return isCenterShowcaseAnimating;
    }
    
    /// <summary>
    /// Check if all showcase animations are complete and clear the flag
    /// </summary>
    private void CheckAndClearShowcaseAnimationFlag()
    {
        // Check if any center cards are still animating
        if (centerTransform != null)
        {
            foreach (Transform child in centerTransform)
            {
                var cardInteraction = child.GetComponent<CardInteraction>();
                if (cardInteraction != null && cardInteraction.IsAnimating())
                {
                    return; // Still animating, don't clear flag yet
                }
            }
        }
        
        // All animations complete, clear the flag
        isCenterShowcaseAnimating = false;
        
    }
    
    /// <summary>
    /// Force-kills all animations on center cards (Option 2 approach)
    /// </summary>
    public void KillAllCenterCardAnimations()
    {
        if (centerTransform != null)
        {
            foreach (Transform child in centerTransform)
            {
                var cardInteraction = child.GetComponent<CardInteraction>();
                if (cardInteraction != null)
                {
                    cardInteraction.KillAllTweens();
                }
            }
        }
        
        // Clear animation flag when force-killing animations
        isCenterShowcaseAnimating = false;
    }

    [ContextMenu("Showcase All Piştis and Point Cards")]
    public void AllCardsShowcase()
    {
        if (playerCount == 2)
        {
            ShowcasePiştisAndPoints1v1();
        }
        else if (playerCount == 4)
        {
            ShowcasePiştisAndPoints2v2();
        }
    }

    // Helper: Returns true if the card is a point card
    private bool IsPointCard(GameObject card)
    {
        // Card tag is expected to be "kind_value"
        var tagParts = card.tag.Split('_');
        if (tagParts.Length != 2) return false;
        int kind, value;
        if (!int.TryParse(tagParts[0], out kind) || !int.TryParse(tagParts[1], out value)) return false;

        // Aces
        if (value == 1) return true;
        // Jacks
        if (value == 11) return true;
        // 2 of clubs (kind==1, value==2)
        if (kind == 1 && value == 2) return true;
        // 10 of diamonds (kind==2, value==10)
        if (kind == 2 && value == 10) return true;

        return false;
    }

    private void ShowcasePiştisAndPoints1v1()
    {
        int cardsPerRow = 4;
        float cardSpacing = 500f;
        float rowSpacing = 1250f;
        float cardScale = myCardsScale;
        float piştiGap = 1.5f * rowSpacing;

        // Get screen positions for left (my side) and right (opponent)
        Vector3 leftScreen = new Vector3(Screen.width * 0.2f, Screen.height * 0.6f, 3000f);
        Vector3 rightScreen = new Vector3(Screen.width * 0.8f, Screen.height * 0.6f, 3000f);

        Vector3 leftWorld = Camera.main.ScreenToWorldPoint(leftScreen);
        Vector3 rightWorld = Camera.main.ScreenToWorldPoint(rightScreen);

        // Determine my and opponent indices
        int myPoolIndex = GetPoolIndex(0);
        int oppPoolIndex = GetPoolIndex(1);

        // --- My side ---
        List<GameObject> myPiştiCards = new List<GameObject>();
        foreach (Transform t in playerPiştiPoolTransforms[myPoolIndex]) myPiştiCards.Add(t.gameObject);

        List<GameObject> myPointCards = new List<GameObject>();
        foreach (Transform t in playerPoolTransforms[myPoolIndex])
            if (IsPointCard(t.gameObject)) myPointCards.Add(t.gameObject);

        // Layout my pişti cards (top)
        int myPiştiRows = Mathf.CeilToInt(myPiştiCards.Count / (float)cardsPerRow);
        if (myPiştiCards.Count > 0)
        {
            for (int i = 0; i < myPiştiCards.Count; i++)
            {
                int row = i / cardsPerRow;
                int col = i % cardsPerRow;
                Vector3 pos = leftWorld + new Vector3((col - (cardsPerRow - 1) / 2f) * cardSpacing, 100 + i, -row * rowSpacing);
                MoveCard(pos, myPiştiCards[i], 10, Quaternion.Euler(90, 0, 0), new Vector3(cardScale, cardScale, cardScale));
            }
        }

        // Layout my point cards (below pişti if present, else at top)
        float myPointZOffset = (myPiştiCards.Count > 0) ? (-myPiştiRows * rowSpacing - piştiGap) : 0f;
        for (int i = 0; i < myPointCards.Count; i++)
        {
            int row = i / cardsPerRow;
            int col = i % cardsPerRow;
            Vector3 pos = leftWorld + new Vector3(
                (col - (cardsPerRow - 1) / 2f) * cardSpacing,
                100 + i,
                myPointZOffset - row * rowSpacing
            );
            MoveCard(pos, myPointCards[i], 10, Quaternion.Euler(90, 0, 0), new Vector3(cardScale, cardScale, cardScale));
        }

        // --- Opponent side ---
        List<GameObject> oppPiştiCards = new List<GameObject>();
        foreach (Transform t in playerPiştiPoolTransforms[oppPoolIndex]) oppPiştiCards.Add(t.gameObject);

        List<GameObject> oppPointCards = new List<GameObject>();
        foreach (Transform t in playerPoolTransforms[oppPoolIndex])
            if (IsPointCard(t.gameObject)) oppPointCards.Add(t.gameObject);

        int oppPiştiRows = Mathf.CeilToInt(oppPiştiCards.Count / (float)cardsPerRow);
        if (oppPiştiCards.Count > 0)
        {
            for (int i = 0; i < oppPiştiCards.Count; i++)
            {
                int row = i / cardsPerRow;
                int col = i % cardsPerRow;
                Vector3 pos = rightWorld + new Vector3((col - (cardsPerRow - 1) / 2f) * cardSpacing, 100 + i, -row * rowSpacing);
                MoveCard(pos, oppPiştiCards[i], 10, Quaternion.Euler(90, 0, 0), new Vector3(cardScale, cardScale, cardScale));
            }
        }

        float oppPointZOffset = (oppPiştiCards.Count > 0) ? (-oppPiştiRows * rowSpacing - piştiGap) : 0f;
        for (int i = 0; i < oppPointCards.Count; i++)
        {
            int row = i / cardsPerRow;
            int col = i % cardsPerRow;
            Vector3 pos = rightWorld + new Vector3(
                (col - (cardsPerRow - 1) / 2f) * cardSpacing,
                100 + i,
                oppPointZOffset - row * rowSpacing
            );
            MoveCard(pos, oppPointCards[i], 10, Quaternion.Euler(90, 0, 0), new Vector3(cardScale, cardScale, cardScale));
        }
    }

    private void ShowcasePiştisAndPoints2v2()
    {
        int cardsPerRow = 5;
        float cardSpacing = 500f;
        float rowSpacing = 1250f;
        float cardScale = myCardsScale;
        float piştiGap = 1.5f * rowSpacing;

        // Get screen positions for left (my team) and right (opponent team)
        Vector3 leftScreen = new Vector3(Screen.width * 0.2f, Screen.height * 0.6f, 3000f);
        Vector3 rightScreen = new Vector3(Screen.width * 0.8f, Screen.height * 0.6f, 3000f);

        Vector3 leftWorld = Camera.main.ScreenToWorldPoint(leftScreen);
        Vector3 rightWorld = Camera.main.ScreenToWorldPoint(rightScreen);

        // My team: Side 0 (pool index 0)
        List<GameObject> myTeamPiştiCards = new List<GameObject>();
        List<GameObject> myTeamPointCards = new List<GameObject>();
        if (playerPiştiPoolTransforms.Count > 0 && playerPiştiPoolTransforms[0] != null)
        {
            foreach (Transform t in playerPiştiPoolTransforms[0]) myTeamPiştiCards.Add(t.gameObject);
        }
        if (playerPoolTransforms.Count > 0 && playerPoolTransforms[0] != null)
        {
            foreach (Transform t in playerPoolTransforms[0])
                if (IsPointCard(t.gameObject)) myTeamPointCards.Add(t.gameObject);
        }

        // Opponent team: Side 1 (pool index 1)
        List<GameObject> oppTeamPiştiCards = new List<GameObject>();
        List<GameObject> oppTeamPointCards = new List<GameObject>();
        if (playerPiştiPoolTransforms.Count > 1 && playerPiştiPoolTransforms[1] != null)
        {
            foreach (Transform t in playerPiştiPoolTransforms[1]) oppTeamPiştiCards.Add(t.gameObject);
        }
        if (playerPoolTransforms.Count > 1 && playerPoolTransforms[1] != null)
        {
            foreach (Transform t in playerPoolTransforms[1])
                if (IsPointCard(t.gameObject)) oppTeamPointCards.Add(t.gameObject);
        }

        // Layout my team pişti cards (top)
        int myTeamPiştiRows = Mathf.CeilToInt(myTeamPiştiCards.Count / (float)cardsPerRow);
        if (myTeamPiştiCards.Count > 0)
        {
            for (int i = 0; i < myTeamPiştiCards.Count; i++)
            {
                int row = i / cardsPerRow;
                int col = i % cardsPerRow;
                Vector3 pos = leftWorld + new Vector3((col - (cardsPerRow - 1) / 2f) * cardSpacing, 100 + i, -row * rowSpacing);
                MoveCard(pos, myTeamPiştiCards[i], 10, Quaternion.Euler(90, 0, 0), new Vector3(cardScale, cardScale, cardScale));
            }
        }

        // Layout my team point cards (below pişti if present, else at top)
        float myTeamPointZOffset = (myTeamPiştiCards.Count > 0) ? (-myTeamPiştiRows * rowSpacing - piştiGap) : 0f;
        for (int i = 0; i < myTeamPointCards.Count; i++)
        {
            int row = i / cardsPerRow;
            int col = i % cardsPerRow;
            Vector3 pos = leftWorld + new Vector3(
                (col - (cardsPerRow - 1) / 2f) * cardSpacing,
                100 + i,
                myTeamPointZOffset - row * rowSpacing
            );
            MoveCard(pos, myTeamPointCards[i], 10, Quaternion.Euler(90, 0, 0), new Vector3(cardScale, cardScale, cardScale));
        }

        // Layout opponent team pişti cards (top)
        int oppTeamPiştiRows = Mathf.CeilToInt(oppTeamPiştiCards.Count / (float)cardsPerRow);
        if (oppTeamPiştiCards.Count > 0)
        {
            for (int i = 0; i < oppTeamPiştiCards.Count; i++)
            {
                int row = i / cardsPerRow;
                int col = i % cardsPerRow;
                Vector3 pos = rightWorld + new Vector3((col - (cardsPerRow - 1) / 2f) * cardSpacing, 100 + i, -row * rowSpacing);
                MoveCard(pos, oppTeamPiştiCards[i], 10, Quaternion.Euler(90, 0, 0), new Vector3(cardScale, cardScale, cardScale));
            }
        }

        // Layout opponent team point cards (below pişti if present, else at top)
        float oppTeamPointZOffset = (oppTeamPiştiCards.Count > 0) ? (-oppTeamPiştiRows * rowSpacing - piştiGap) : 0f;
        for (int i = 0; i < oppTeamPointCards.Count; i++)
        {
            int row = i / cardsPerRow;
            int col = i % cardsPerRow;
            Vector3 pos = rightWorld + new Vector3(
                (col - (cardsPerRow - 1) / 2f) * cardSpacing,
                100 + i,
                oppTeamPointZOffset - row * rowSpacing
            );
            MoveCard(pos, oppTeamPointCards[i], 10, Quaternion.Euler(90, 0, 0), new Vector3(cardScale, cardScale, cardScale));
        }
    }


    public void TryStopShowcasePlayerPoolCards()
    {
        if (isShowcasing)
        {
            StopShowcasePlayerPoolCards();
        }
    }
    
    public void TryStopShowcaseCenterCards()
    {
        if (isCenterShowcasing)
        {
            StopShowcaseCenterCards();
        }
    }
    
    /// <summary>
    /// Stops all active showcases (center, player pools, other hands)
    /// </summary>
    public void TryStopAllShowcases()
    {
        TryStopShowcaseCenterCards();
        TryStopShowcasePlayerPoolCards();
        if (isShowcaseAllActive)
        {
            //ExitShowcaseAllOtherHands();
        }
    }

    public void MoveCard(Vector3 endPos, GameObject cardObject, float speed, Quaternion rotation, Vector3 scales)
    {
        // Start the coroutine to move the card
        StartCoroutine(MoveCardCoroutine(endPos, cardObject, speed, rotation, scales));
        //cardObject.GetComponent<CardInteraction>().KillAllTweens();
    }

    private IEnumerator MoveCardCoroutine(Vector3 endPos, GameObject cardObject, float speedMultiplier, Quaternion rotation, Vector3 scale)
    {
        if (cardObject == null)
        {
            GameManager.AddToDebugLog("[SingleplayerCardPlay][Diag] MoveCardCoroutine received NULL cardObject");
            yield break;
        }

        Vector3 startPos = cardObject.transform.position;
        Quaternion startRot = cardObject.transform.rotation;
        Vector3 startScale = cardObject.transform.localScale;
        GameManager.AddToDebugLog($"[SingleplayerCardPlay][Diag] MoveCardCoroutine start: card={cardObject.name}, active={cardObject.activeInHierarchy}, parent={cardObject.transform.parent?.name}, startPos={startPos}, endPos={endPos}, speedMultiplier={speedMultiplier}, isInstantMode={isInstantMode}");

        // If instant mode (reconnection), teleport immediately
        if (isInstantMode)
        {
            GameManager.AddToDebugLog($"[SingleplayerCardPlay] MoveCardCoroutine: Instant mode - teleporting {cardObject.name} to {endPos}");
            cardObject.transform.position = endPos;
            cardObject.transform.rotation = rotation;
            cardObject.transform.localScale = scale;
            GameManager.AddToDebugLog($"[SingleplayerCardPlay][Diag] Instant mode applied: finalPos={cardObject.transform.position}, finalRot={cardObject.transform.rotation.eulerAngles}, finalScale={cardObject.transform.localScale}");
            yield return null;
        }
        else
        {
            var cardInteraction = cardObject.GetComponent<CardInteraction>();
            /*if (cardInteraction != null)
            {
                cardInteraction.KillAllTweens();
                yield return cardInteraction.WaitForAllTweens();
            }*/

            float duration = 1f/speedMultiplier; // Adjust as needed
            GameManager.AddToDebugLog($"[SingleplayerCardPlay] MoveCardCoroutine: Animating {cardObject.name} with duration={duration}s to endPos={endPos}");

            // Create a DOTween sequence for position, rotation, and scale
            DG.Tweening.Sequence moveSeq = DOTween.Sequence();
            moveSeq.Join(cardObject.transform.DOMove(endPos, duration));
            moveSeq.Join(cardObject.transform.DORotateQuaternion(rotation, duration));
            moveSeq.Join(cardObject.transform.DOScale(scale, duration));

            if (moveSeq == null)
            {
                GameManager.AddToDebugLog($"[SingleplayerCardPlay][Diag] moveSeq is NULL for {cardObject.name}");
                yield break;
            }

            GameManager.AddToDebugLog($"[SingleplayerCardPlay][Diag] moveSeq created: active={moveSeq.active}, playing={moveSeq.IsPlaying()}, complete={moveSeq.IsComplete()}, duration={moveSeq.Duration()}");

            yield return moveSeq.WaitForCompletion();
            GameManager.AddToDebugLog($"[SingleplayerCardPlay] MoveCardCoroutine: Animation complete for {cardObject.name}");
            GameManager.AddToDebugLog($"[SingleplayerCardPlay][Diag] MoveCardCoroutine end: card={cardObject.name}, finalPos={cardObject.transform.position}, expectedEnd={endPos}, delta={(cardObject.transform.position - endPos)}");
        }
    }

    public IEnumerator ChainMoveCards(List<Vector3> positions, List<GameObject> cardObject, float speed, List<Quaternion> rotations, List<Vector3> scales, bool endTurnFlag = false, bool lastMove = false, bool updateFlag = true)
    {
        GameManager.AddToDebugLog($"[SingleplayerCardPlay] ChainMoveCards: Starting with {cardObject.Count} cards, speed={speed}");
        yield return StartCoroutine(ChainMoveCardsCoroutine(positions, cardObject, speed, rotations, scales));
        GameManager.AddToDebugLog($"[SingleplayerCardPlay] ChainMoveCards: Coroutine complete");
        if (lastMove) AllCardsShowcase();
        if (endTurnFlag) gameManager.TellServerTurnEnded();
    }

    public void BuildPoolMoveListsAndMoveCards(Vector3 position, List<GameObject> cardObjects, float speed, int poolIndex, bool piştiFlag = false, bool lastMove = false)
    {
        List<Vector3> positions = new List<Vector3>();
        List<Quaternion> rotations = new List<Quaternion>();
        List<Vector3> scales = new List<Vector3>();
        for (int i = 0; i < cardObjects.Count; i++)
        {
            Vector3 tempRotation = playerPoolTransforms[poolIndex].transform.rotation.eulerAngles;
            positions.Add(position);
            Quaternion rotation = Quaternion.Euler(tempRotation.x - 90, tempRotation.y, tempRotation.z + UnityEngine.Random.Range(170f, 190f));
            rotations.Add(rotation);
            // Always use smaller normal scale for all captured cards
            int targetScale = normalScale;
            scales.Add(new Vector3(targetScale, targetScale, targetScale));

            if (piştiFlag)
            {
                if (i == cardObjects.Count - 1)
                {
                    positions[i] = new Vector3(position.x + 100 + GetChkobbaZOffset(), position.y, position.z - GetChkobbaZOffset() + 10);
                    rotations[i] = Quaternion.Euler(tempRotation.x + 90, tempRotation.y, tempRotation.z + UnityEngine.Random.Range(170f, 190f) + 90);
                    //
                }
                StartCoroutine(ChkobbaCoroutine());
            }
        }

        StartCoroutine(ChainMoveCards(positions, cardObjects, speed, rotations, scales, true, lastMove));
    }

    private IEnumerator ChkobbaCoroutine()
    {
        //chkobbaText.SetActive(true);

        yield return new WaitForSeconds(2);

        //chkobbaText.SetActive(false);
    }

    private int GetChkobbaYOffset(int poolIndex)
    {
        if (poolIndex == 0)
        {
            offsetCounter0++;
            return (offset * offsetCounter0) - 200;

        }
        else if (poolIndex == 1)
        {
            offsetCounter1++;
            return -(offset * offsetCounter1) + 200;
        }
        else if (poolIndex == 2)
        {
            offsetCounter2++;
            return -(offset * offsetCounter2) + 200;
        }
        else return 0;
    }

    private int GetChkobbaZOffset()
    {
        zOffsetCounter++;
        return zOffsetCounter;
    }


    private IEnumerator ChainMoveCardsCoroutine(List<Vector3> positions, List<GameObject> cardObjects, float speed, List<Quaternion> rotations, List<Vector3> scales)
    {
        GameManager.AddToDebugLog($"[SingleplayerCardPlay] ChainMoveCardsCoroutine: Processing {cardObjects.Count} cards sequentially");
        if (positions == null || cardObjects == null || rotations == null || scales == null)
        {
            GameManager.AddToDebugLog("[SingleplayerCardPlay][Diag] ChainMoveCardsCoroutine received NULL list(s)");
            yield break;
        }
        if (positions.Count != cardObjects.Count || rotations.Count != cardObjects.Count || scales.Count != cardObjects.Count)
        {
            GameManager.AddToDebugLog($"[SingleplayerCardPlay][Diag] List count mismatch: positions={positions.Count}, cardObjects={cardObjects.Count}, rotations={rotations.Count}, scales={scales.Count}");
            yield break;
        }
        //if(cardObjects.Count<20)yield return new WaitForSeconds(2f - (cardObjects.Count*0.1f));
        //else yield return new WaitForSeconds(0.2f);
        for (int i = 0; i < cardObjects.Count; i++)
        {
            GameManager.AddToDebugLog($"[SingleplayerCardPlay] ChainMoveCardsCoroutine: Animating card {i+1}/{cardObjects.Count}");
            if (cardObjects[i] == null)
            {
                GameManager.AddToDebugLog($"[SingleplayerCardPlay][Diag] cardObjects[{i}] is NULL");
                continue;
            }
            GameManager.AddToDebugLog($"[SingleplayerCardPlay][Diag] Card[{i}]={cardObjects[i].name}, parent={cardObjects[i].transform.parent?.name}, target={positions[i]}");
            // Play card deal sound effect for each individual card movement
            if (soundEffectsController != null)
            {
                soundEffectsController.PlayCardDealSound();
            }
            
            yield return StartCoroutine(MoveCardCoroutine(positions[i], cardObjects[i], speed + (i * 0.25f), rotations[i], scales[i]));
        }
        GameManager.AddToDebugLog($"[SingleplayerCardPlay] ChainMoveCardsCoroutine: All {cardObjects.Count} cards animated");
    }
    public void SetPlayerNumber(int playerNumber)
    {
        thisPlayerNumber = playerNumber;
        
        
        // CRITICAL FIX: Save player number to PlayerPrefs for reconnection
        PlayerPrefs.SetInt("SavedPlayerSeat", playerNumber);
        PlayerPrefs.Save();
        

        if (SideManager.Instance != null)
        {
            SideManager.Instance.UpdateAllSides();
        }
    }

    /// <summary>
    /// Rebuilds CardInteraction.cardLookup deterministically from active GameObjects.
    /// Prevents KeyNotFoundExceptions on reconnection due to stale static references.
    /// </summary>
    public void RebuildCardLookup()
    {
        if (CardInteraction.cardLookup == null)
        {
            CardInteraction.cardLookup = new Dictionary<string, CardInteraction>();
        }
        CardInteraction.cardLookup.Clear();

        // 1. First rebuild from our tracked cardObjectList
        if (cardObjectList != null)
        {
            foreach (var cardObj in cardObjectList)
            {
                if (cardObj != null)
                {
                    CardInteraction cardInteraction = cardObj.GetComponent<CardInteraction>();
                    if (cardInteraction != null && !string.IsNullOrEmpty(cardInteraction.uniqueCardInstanceID))
                    {
                        CardInteraction.cardLookup[cardInteraction.uniqueCardInstanceID] = cardInteraction;
                    }
                }
            }
        }

        // 2. Fallback: Scan the scene to ensure we catch every card (e.g. if list was cleared but objects remain)
        var allCardInteractions = UnityEngine.Object.FindObjectsByType<CardInteraction>(UnityEngine.FindObjectsSortMode.None);
        foreach (var ci in allCardInteractions)
        {
            if (ci != null && !string.IsNullOrEmpty(ci.uniqueCardInstanceID))
            {
                CardInteraction.cardLookup[ci.uniqueCardInstanceID] = ci;
            }
        }

        
    }

    public int GetSavedPlayerSeat()
    {
        return PlayerPrefs.GetInt("SavedPlayerSeat", -1);
    }

    /// <summary>
    /// Sets the startingPlayerNoCounter from server game state during reconnection
    /// </summary>
    public void SetStartingPlayerNoCounter(int counter)
    {
        startingPlayerNoCounter = counter;
        
    }
 
    public int SendPlayerNumber()
    {
        return thisPlayerNumber;
    }

    [ContextMenu("Print ThisPlayerNumber")]
    private void PrintThisPlayerNumber()
    {
        //
    }

    private List<GameObject> orderedCardObjectList = new List<GameObject>();
    public void AddCardsToPlayerPool(int playerNumber)
    {
        if (playerNumber == -1) return;
        //
        List<GameObject> cardObjects = new List<GameObject>();
        int relativePoolIndex = (playerNumber - thisPlayerNumber + playerCount) % playerCount;

        var children = new List<Transform>();
        foreach (Transform child in GameObject.Find("Center").transform) { children.Add(child); }

        foreach (Transform child in children)
        {
            
            GameObject card = child.gameObject;
            cardObjects.Add(card);
            card.transform.parent = null; // Unparent the card
            card.transform.parent = playerPoolTransforms[GetPoolIndex(relativePoolIndex)];
            gameManager.centerCardsObjects.Remove(card);
            orderedCardObjectList.Add(card);
        }


        BuildPoolMoveListsAndMoveCards(playerPoolTransforms[GetPoolIndex(relativePoolIndex)].position, cardObjects, 10, GetPoolIndex(relativePoolIndex), false, true);
    }

    public void MoveCardsToPlayerPool(List<GameObject> cardObjects, int playerNumber, bool piştiFlag)
    {
        int relativePoolIndex = (playerNumber - thisPlayerNumber + playerCount) % playerCount;

        if (piştiFlag)
        {
            foreach (var card in cardObjects)
            {
                card.transform.parent = null; // Unparent the card
                card.transform.parent = playerPiştiPoolTransforms[GetPoolIndex(relativePoolIndex)];
                gameManager.centerCardsObjects.Remove(card);
                orderedCardObjectList.Add(card);
            }
            BuildPoolMoveListsAndMoveCards(playerPiştiPoolTransforms[GetPoolIndex(relativePoolIndex)].position, cardObjects, 10, GetPoolIndex(relativePoolIndex), piştiFlag);
        }

        else
        {
            // If this is the local player, process gold values first
            if (relativePoolIndex == 0) // Local player
            {
                StartCoroutine(MoveCardsToLocalPlayerPoolWithGold(cardObjects, relativePoolIndex));
            }
            
            // Move cards using existing system for ALL players (including local)
            foreach (var card in cardObjects)
            {
                card.transform.parent = null; // Unparent the card
                card.transform.parent = playerPoolTransforms[GetPoolIndex(relativePoolIndex)];
                gameManager.centerCardsObjects.Remove(card);
            }
            BuildPoolMoveListsAndMoveCards(playerPoolTransforms[GetPoolIndex(relativePoolIndex)].position, cardObjects, 10, GetPoolIndex(relativePoolIndex));
        }
    }

    /// <summary>
    /// Processes gold values for local player (NO card movement - only gold calculation and queuing)
    /// NOTE: Gold calculation is now handled in SuperPowerSpawner.OnLocalCapture() using the new formula
    /// </summary>
    private IEnumerator MoveCardsToLocalPlayerPoolWithGold(List<GameObject> cardObjects, int relativePoolIndex)
    {
        // Gold calculation is now handled in SuperPowerSpawner.OnLocalCapture() 
        // which uses the new formula: [capturing card value] + [n(n+1)/2]
        // No need to process individual cards here anymore
        
        
        
        // No card movement here - BuildPoolMoveListsAndMoveCards handles that
        yield return null;
    }

    public int GetPoolIndex(int relativePoolIndex)
    {
        if (relativePoolIndex == 0 || relativePoolIndex == 2)
        {
            return 0;
        }
        else
        {
            return 1;
        }
    }

    public int GetHandIndex(int relativeIndex)
    {
        int handIndex = 0;
        if (playerCount == 4)
        {
            handIndex = relativeIndex;
        }
        else if (playerCount == 2)
        {
            if (relativeIndex == 0) handIndex = 0;
            else if (relativeIndex == 1) handIndex = 2;
        }

        return handIndex;
    }

    public void GetPlayerCount(int playerC, bool isReconnection = false)
    {
        Debug.Log($"[DeckController] ===== ÖNEMLİ: GET PLAYER COUNT CALLED =====\n" +
                 $"PlayerCount: {playerC}\n" +
                 $"IsReconnection: {isReconnection}\n" +
                 $"ElHolderScript.LocalInstance: {(ElHolderScript.LocalInstance != null ? "FOUND" : "NULL")}");
        
        playerCount = playerC;
        ElHolderScript.LocalInstance.SetHandMode(playerCount);
        
        // Only do UI management during reconnection, not during normal game start
        if (isReconnection)
        {
            
            
            // Find and manage UI screens (same as InitialGameManagerSetUp and InitializeCardPrefabs do)
            var waitingScreen = GameObject.Find("WaitingScreen");
            if (waitingScreen != null && waitingScreen.activeSelf)
            {
                
                waitingScreen.SetActive(false);
            }
            
            var winScreen = GameObject.Find("WinScreen");
            if (winScreen != null && winScreen.activeSelf)
            {
                
                winScreen.SetActive(false);
            }
            
            var mainScreen = GameObject.Find("MainScreen");
            if (mainScreen != null && mainScreen.activeSelf)
            {
                
                mainScreen.SetActive(false);
            }
        }
        else
        {
            
        }
        
        Debug.Log($"[DeckController] ===== ÖNEMLİ: GET PLAYER COUNT COMPLETED =====\n" +
                 $"UI screens initialized for player count: {playerC}");
        
        // Note: Desync detection now happens in GameManager.DeckReady() after card initialization
    }

    private void InitialDeckSetUp()
    {
        cardPrefabs = new Dictionary<string, GameObject>();
        deckPool = new Dictionary<string, GameObject>();
    }

    public void GetPlayerHandTransforms(List<Transform> playerHTransforms, List<Transform> playerPTransforms, Transform cTransform, List<Transform> playerPiştiTransforms)
    {
        playerHandTransforms.Clear();
        playerPoolTransforms.Clear();
        playerPiştiPoolTransforms.Clear();

        foreach (var playerP in playerPiştiTransforms)
        {
            playerPiştiPoolTransforms.Add(playerP);
        }

        foreach (var playerH in playerHTransforms)
        {
            playerHandTransforms.Add(playerH);
        }

        foreach (var playerP in playerPTransforms)
        {
            playerPoolTransforms.Add(playerP);
        }

        centerTransform = cTransform;
    }

    /// <summary>
    /// Animates peeking at a specific card in an opponent's hand, using relative index for correct perspective.
    /// </summary>
    public void PeekOpponentCard(int opponentPlayerNo, int cardIndex)
    {
        Debug.Log($"[UcundanGözAt] DeckController.PeekOpponentCard() called: opponentPlayerNo={opponentPlayerNo}, cardIndex={cardIndex}");
        Debug.Log($"[UcundanGözAt] DeckController: thisPlayerNumber={thisPlayerNumber}, playerCount={playerCount}");
        
        try
        {
            int myNo = thisPlayerNumber;
            int playerCount = this.playerCount;
            int relativeIndex = (opponentPlayerNo - myNo + playerCount) % playerCount;
            bool isMine = thisPlayerNumber == opponentPlayerNo;
            
            Debug.Log($"[UcundanGözAt] DeckController: relativeIndex={relativeIndex}, isMine={isMine}");
            Debug.Log($"[UcundanGözAt] DeckController: playerHandTransforms.Count={playerHandTransforms?.Count ?? -1}");
            
            // Get hand index safely
            int handIndex = -1;
            try
            {
                handIndex = GetHandIndex(relativeIndex);
                Debug.Log($"[UcundanGözAt] DeckController: GetHandIndex({relativeIndex}) returned {handIndex}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[UcundanGözAt] DeckController: GetHandIndex({relativeIndex}) threw exception: {ex.Message}\n{ex.StackTrace}");
                return;
            }
            
            // Validate hand index
            if (playerHandTransforms == null)
            {
                Debug.LogError($"[UcundanGözAt] DeckController: playerHandTransforms is NULL");
                return;
            }
            
            if (handIndex < 0 || handIndex >= playerHandTransforms.Count)
            {
                Debug.LogError($"[UcundanGözAt] DeckController: handIndex {handIndex} is out of bounds (playerHandTransforms.Count={playerHandTransforms.Count})");
                return;
            }
            
            // Get the hand transform for the opponent in my perspective
            Transform handTransform = playerHandTransforms[handIndex];
            Debug.Log($"[UcundanGözAt] DeckController: handTransform retrieved, childCount={handTransform.childCount}");

            // Get the card GameObject at the specified index
            if (handTransform.childCount <= cardIndex)
            {
                Debug.LogWarning($"[UcundanGözAt] DeckController: Not enough cards. childCount={handTransform.childCount}, need cardIndex+2={cardIndex + 2}");
                return;
            }

            int actualIndex = 0;
            int found = 0;
            foreach (Transform child in handTransform)
            {
                if (actualIndex >= 0)
                {
                    if (found == cardIndex)
                    {
                        GameObject card = child.gameObject;
                        Debug.Log($"[UcundanGözAt] DeckController: Found card at actualIndex={actualIndex}, starting PeekCardAnimation");
                        StartCoroutine(PeekCardAnimation(card, isMine));
                        return;
                    }
                    found++;
                }
                actualIndex++;
            }
            
            Debug.LogError($"[UcundanGözAt] DeckController: Card at index {cardIndex} was not found in hand");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UcundanGözAt] DeckController.PeekOpponentCard() threw exception: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public void PeekOpponentCardAll(int opponentPlayerNo)
    {
        int myNo = thisPlayerNumber;
        int playerCount = this.playerCount;
        int relativeIndex = (opponentPlayerNo - myNo + playerCount) % playerCount;
        bool isMine = thisPlayerNumber == opponentPlayerNo;

        // Get the hand transform for the opponent in my perspective
        Transform handTransform = playerHandTransforms[GetHandIndex(relativeIndex)];


        // Skip pool/extra children if needed
        int actualIndex = 0;
        List<GameObject> cardsToPeek = new List<GameObject>();
        foreach (Transform child in handTransform)
        {
            if (actualIndex >= 0)
            {
                cardsToPeek.Add(child.gameObject);
            }
            actualIndex++;
        }

        StartCoroutine(PeekCardAllAnimation(cardsToPeek, isMine));
    }


    /// <summary>
    /// Coroutine to animate the peek effect on a card.
    /// </summary>
    private IEnumerator PeekCardAnimation(GameObject card, bool isMine)
    {
        Debug.Log($"[UcundanGözAt] DeckController.PeekCardAnimation() started: card={card.name}, isMine={isMine}");
        
        Vector3 originalPos = card.transform.position;
        Quaternion originalRot = card.transform.rotation;
        Vector3 originalScale = card.transform.localScale;
        
        Debug.Log($"[UcundanGözAt] DeckController: Original state - pos={originalPos}, rot={originalRot}, scale={originalScale}");

        if (isMine)
        {
            // Keep local auto-rotation from fighting the temporary peek rotation.
            CardInteraction interaction = card.GetComponent<CardInteraction>();
            if (interaction != null)
            {
                Debug.Log("[UcundanGözAt] DeckController: Stopping auto-rotate for my card");
                interaction.StopAutoRotate();
            }
        }

        Vector3 peekPos = originalPos + new Vector3(0, 1200, isMine ? 0 : (playerCount == 2 ? -500 : 0));
        Vector3 peekScale = isMine ? originalScale : originalScale * 2.0f;
        Quaternion peekRot = isMine ? Quaternion.Euler(-90, 0, 0) : Quaternion.Euler(90, 0, 0);
        
        Debug.Log($"[UcundanGözAt] DeckController: Peek state - peekPos={peekPos}, peekRot={peekRot}, peekScale={peekScale}");

        float moveDuration = 0.4f;
        float pauseDuration = 3f;

        // Move to peek
        Debug.Log($"[UcundanGözAt] DeckController: Starting move to peek position (duration={moveDuration}s)");
        yield return TweenMoveTransform(card.transform, peekPos, peekRot, peekScale, moveDuration);
        Debug.Log("[UcundanGözAt] DeckController: Move to peek position completed");

        Debug.Log($"[UcundanGözAt] DeckController: Pausing for {pauseDuration}s to show card");
        yield return new WaitForSeconds(pauseDuration);
        Debug.Log("[UcundanGözAt] DeckController: Pause completed");

        // Move back
        Debug.Log($"[UcundanGözAt] DeckController: Starting move back to original position (duration={moveDuration}s)");
        yield return TweenMoveTransform(card.transform, originalPos, originalRot, originalScale, moveDuration);
        Debug.Log("[UcundanGözAt] DeckController: Move back to original position completed");

        if (isMine)
        {
            Debug.Log("[UcundanGözAt] DeckController: Restarting auto-rotate for my card");
            CardInteraction interaction = card.GetComponent<CardInteraction>();
            if (interaction != null)
            {
                interaction.StartAutoRotate();
            }
        }

        if (SuperPowerSpawner.LocalInstance != null && SuperPowerSpawner.LocalInstance.isInfoBoxOpen)
        {
            Debug.Log("[UcundanGözAt] DeckController: Closing SuperPowerSpawner InfoBox");
            SuperPowerSpawner.LocalInstance.StartCoroutine(SuperPowerSpawner.LocalInstance.CloseInfoBox());
        }
        else
        {
            Debug.Log("[UcundanGözAt] DeckController: SuperPowerSpawner infobox not open or spawner null");
        }

        if (GameManager.LocalInstance != null)
        {
            Debug.Log("[UcundanGözAt] DeckController: Calling EndGameplayActionIfTagStartsWith");
            GameManager.LocalInstance.EndGameplayActionIfTagStartsWith("SuperPower", "Ucundan Goz At animation completed");
        }
        
        Debug.Log("[UcundanGözAt] DeckController.PeekCardAnimation() completed successfully");
    }



    private IEnumerator PeekCardAllAnimation(List<GameObject> cards, bool isMine)
    {
        List<Vector3> originalPoss = new List<Vector3>();
        List<Quaternion> originalRots = new List<Quaternion>();
        List<Vector3> originalScales = new List<Vector3>();

        for (int i = 0; i < cards.Count; i++)
        {
            originalPoss.Add(cards[i].transform.position);
            originalRots.Add(cards[i].transform.rotation);
            originalScales.Add(cards[i].transform.localScale);
        }

        float spread = 1000f;
        float yOffset = 1200f;
        Vector3 center = Vector3.zero;
        foreach (var pos in originalPoss) center += pos;
        center /= cards.Count;

        List<Vector3> peekPoss = new List<Vector3>();
        List<Quaternion> peekRots = new List<Quaternion>();
        List<Vector3> peekScales = new List<Vector3>();

        for (int i = 0; i < cards.Count; i++)
        {
            if (isMine) cards[i].GetComponent<CardInteraction>().StopAutoRotate();
            float offset = (i - (cards.Count - 1) / 2f) * spread;
            Vector3 peekPos = originalPoss[i] + new Vector3(offset, yOffset, isMine ? 0 : (playerCount == 2 ? -500 : 0));
            peekPoss.Add(peekPos);
            
            // CRITICAL FIX: Use the same rotation logic as Ucundan Göz At for consistency
            if (isMine)
            {
                // My cards: rotate to show back to me (same as Ucundan Göz At)
                peekRots.Add(Quaternion.Euler(-90, 0, 0));
            }
            else
            {
                // Opponent cards: rotate to show their faces (same as Ucundan Göz At)
                peekRots.Add(Quaternion.Euler(90, 0, 0));
            }
            
            peekScales.Add(isMine ? originalScales[i] : originalScales[i] * 2.0f);
        }

        float duration = 0.4f;
        float pause = 3f;

        // Animate to peek positions
        List<Sequence> seqs = new List<Sequence>();
        for (int i = 0; i < cards.Count; i++)
        {
            Sequence seq = DOTween.Sequence();
            seq.Join(cards[i].transform.DOMove(peekPoss[i], duration));
            seq.Join(cards[i].transform.DORotateQuaternion(peekRots[i], duration));
            seq.Join(cards[i].transform.DOScale(peekScales[i], duration));
            seqs.Add(seq);
        }
        foreach (var seq in seqs) yield return seq.WaitForCompletion();

        yield return new WaitForSeconds(pause);

        // Animate back to original
        seqs.Clear();
        for (int i = 0; i < cards.Count; i++)
        {
            Sequence seq = DOTween.Sequence();
            seq.Join(cards[i].transform.DOMove(originalPoss[i], duration));
            seq.Join(cards[i].transform.DORotateQuaternion(originalRots[i], duration));
            seq.Join(cards[i].transform.DOScale(originalScales[i], duration));
            seqs.Add(seq);
        }
        foreach (var seq in seqs) yield return seq.WaitForCompletion();

        for (int i = 0; i < cards.Count; i++)
            if (isMine) cards[i].GetComponent<CardInteraction>().StartAutoRotate();
            
        // CRITICAL FIX: Notify that Baya Baya Bak animation is complete
        if (!isMine) // Only for opponent cards (Baya Baya Bak)
        {
            
            if (SuperPowerSpawner.LocalInstance != null)
            {
                SuperPowerSpawner.LocalInstance.OnPeekAnimationComplete();
            }
        }

        // After the Baya Baya Bak animation is complete, close the InfoBox if needed
        if (SuperPowerSpawner.LocalInstance != null && SuperPowerSpawner.LocalInstance.isInfoBoxOpen)
        {
            SuperPowerSpawner.LocalInstance.StartCoroutine(SuperPowerSpawner.LocalInstance.CloseInfoBox());
        }

        if (GameManager.LocalInstance != null)
        {
            GameManager.LocalInstance.EndGameplayActionIfTagStartsWith("SuperPower", "Baya Baya Bak animation completed");
        }
    }



    public int GetRandomHandCardIndex(int absolutePlayerNo)
    {
        // Use relative index logic to get the correct hand transform for this player from my perspective
        int myNo = thisPlayerNumber;
        int playerCount = this.playerCount;
        int relativeIndex = (absolutePlayerNo - myNo + playerCount) % playerCount;

        // Get the hand transform for the opponent in my perspective
        Transform handTransform = playerHandTransforms[GetHandIndex(relativeIndex)];

        List<Transform> handCards = new List<Transform>();
        foreach (Transform child in handTransform)
        {
            // You may want to filter out non-hand cards here if needed
            if (!child.gameObject.name.Contains("Player")) handCards.Add(child);
        }

        if (handCards.Count == 0)
            return -1; // No cards to pick

        // Pick a random card index
        return UnityEngine.Random.Range(0, handCards.Count);
    }

    IEnumerator WaitForBoth(IEnumerator a, IEnumerator b)
    {
        bool aDone = false, bDone = false;
        IEnumerator Wrap(IEnumerator routine, Action onDone)
        {
            yield return StartCoroutine(routine);
            onDone();
        }
        yield return StartCoroutine(Wrap(a, () => aDone = true));
        yield return StartCoroutine(Wrap(b, () => bDone = true));
        while (!aDone || !bDone) yield return null;
    }

    // Optionally, if you want to update layout after the animation
    private IEnumerator DelayedLayoutUpdate()
    {
        yield return new WaitForSeconds(1f);
        UpdateCurrentPlayerHandLayout();
    }

    /// <summary>
    /// Gets the hand card GameObject at the given index
    /// </summary>
    private GameObject GetHandCardByIndex(Transform handTransform, int index)
    {
        int actualIndex = 0;
        foreach (Transform child in handTransform)
        {
            if (actualIndex >= 0) // skip pool/extra
            {
                if (actualIndex == index)
                    return child.gameObject;
            }
            actualIndex++;
        }
        return null;
    }

    public IEnumerator SwapHandCardWithCenterCard(string handCardID, string centerCardID, int playerNo)
    {
        // Track card movement in move chain
        MoveChainIntegrator.TrackCardMovement(playerNo, handCardID, "Hand", "Center", "Bu Daha İyi power");
        MoveChainIntegrator.TrackCardMovement(playerNo, centerCardID, "Center", "Hand", "Bu Daha İyi power");

        if (!CardInteraction.cardLookup.ContainsKey(handCardID) || !CardInteraction.cardLookup.ContainsKey(centerCardID))
        {
            yield break;
        }

        GameObject handCardObj = CardInteraction.cardLookup[handCardID].gameObject;
        GameObject centerCardObj = CardInteraction.cardLookup[centerCardID].gameObject;
        if (handCardObj == null || centerCardObj == null)
        {
            yield break;
        }

        int relativeIndex = (playerNo - thisPlayerNumber + playerCount) % playerCount;
        int handIndex = GetHandIndex(relativeIndex);
        if (handIndex < 0 || handIndex >= playerHandTransforms.Count)
        {
            yield break;
        }

        Transform handTransform = playerHandTransforms[handIndex];

        // Find hand card sibling index in destination hand
        int handCardIndex = -1;
        int idx = 0;
        foreach (Transform child in handTransform)
        {
            if (child.gameObject == handCardObj)
            {
                handCardIndex = idx;
                break;
            }
            idx++;
        }

        // Find center card sibling index in center
        int centerCardIndex = -1;
        idx = 0;
        foreach (Transform child in centerTransform)
        {
            if (child.gameObject == centerCardObj)
            {
                centerCardIndex = idx;
                break;
            }
            idx++;
        }

        // Capture world-space targets before any parenting changes to avoid visual jumps.
        Vector3 handCardOldPos = handCardObj.transform.position;
        Quaternion handCardOldRot = handCardObj.transform.rotation;

        Vector3 centerCardOldPos = centerCardObj.transform.position;
        Quaternion centerCardOldRot = centerCardObj.transform.rotation;
        Vector3 targetScaleInHand = (relativeIndex == 0)
            ? new Vector3(myCardsScale, myCardsScale, myCardsScale)
            : new Vector3(normalScale, normalScale, normalScale);

        // Animate both cards first, then reparent.
        yield return StartCoroutine(WaitForBoth(
            MoveCardCoroutine(centerCardOldPos, handCardObj, 1, centerCardOldRot, new Vector3(centerScale, centerScale, centerScale)),
            MoveCardCoroutine(handCardOldPos, centerCardObj, 1, handCardOldRot, targetScaleInHand)));

        // Now swap parents while preserving world transforms.
        handCardObj.transform.SetParent(centerTransform, true);
        centerCardObj.transform.SetParent(handTransform, true);

        // Keep list ordering deterministic for all peers.
        List<Transform> handChildren = new List<Transform>();
        foreach (Transform child in handTransform) handChildren.Add(child);
        handChildren.Remove(centerCardObj.transform);
        if (handCardIndex >= 0 && handCardIndex <= handChildren.Count)
            handChildren.Insert(handCardIndex, centerCardObj.transform);
        else
            handChildren.Add(centerCardObj.transform);
        for (int i = 0; i < handChildren.Count; i++)
            handChildren[i].SetSiblingIndex(i);

        List<Transform> centerChildren = new List<Transform>();
        foreach (Transform child in centerTransform) centerChildren.Add(child);
        centerChildren.Remove(handCardObj.transform);
        if (centerCardIndex >= 0 && centerCardIndex <= centerChildren.Count)
            centerChildren.Insert(centerCardIndex, handCardObj.transform);
        else
            centerChildren.Add(handCardObj.transform);
        for (int i = 0; i < centerChildren.Count; i++)
            centerChildren[i].SetSiblingIndex(i);

        // Normalize idle behavior by destination ownership.
        var handCardInteraction = handCardObj.GetComponent<CardInteraction>();
        var centerCardInteraction = centerCardObj.GetComponent<CardInteraction>();
        handCardInteraction?.StopAutoRotate(); // Card moved to center should not idle-rotate.

        // Card moved from center to hand should be face-up only in the local player's own hand.
        if (centerCardInteraction != null)
        {
            if (relativeIndex == 0)
            {
                centerCardInteraction.StartAutoRotate();
            }
            else
            {
                centerCardInteraction.StartAutoRotateFaceDown();
            }
        }

        CardInteraction.currentlySelectedCard = null;
        GameManager.LocalInstance.SetCurrentSelectedHandCardNull();
    }



    public IEnumerator SwapCardsBetweenPlayersByID(int playerANo, string cardAID, int playerBNo, string cardBID, bool sunuFlag = false)
    {
        
        int relA = (playerANo - thisPlayerNumber + playerCount) % playerCount;
        int relB = (playerBNo - thisPlayerNumber + playerCount) % playerCount;

        Transform handA = playerHandTransforms[GetHandIndex(relA)];
        Transform handB = playerHandTransforms[GetHandIndex(relB)];

        if (!CardInteraction.cardLookup.TryGetValue(cardAID, out var cardAInteraction) ||
            !CardInteraction.cardLookup.TryGetValue(cardBID, out var cardBInteraction))
        {
            
            yield break;
        }

        GameObject cardAObj = cardAInteraction.gameObject;
        GameObject cardBObj = cardBInteraction.gameObject;

        // --- Find original indexes ---
        int idxA = -1, idxB = -1, i = 0;
        foreach (Transform child in handA)
        {
            if (child.gameObject == cardAObj) { idxA = i; break; }
            i++;
        }
        i = 0;
        foreach (Transform child in handB)
        {
            if (child.gameObject == cardBObj) { idxB = i; break; }
            i++;
        }

        

        // Store world positions before changing parents
        Vector3 cardAOldPos = cardAObj.transform.position;
        Quaternion cardAOldRot = cardAObj.transform.rotation;
        Vector3 cardBOldPos = cardBObj.transform.position;
        Quaternion cardBOldRot = cardBObj.transform.rotation;

        // Target scale must be based on destination hand (local hand uses myCardsScale, others use normalScale).
        Vector3 targetScaleInHandA = (relA == 0)
            ? new Vector3(myCardsScale, myCardsScale, myCardsScale)
            : new Vector3(normalScale, normalScale, normalScale);
        Vector3 targetScaleInHandB = (relB == 0)
            ? new Vector3(myCardsScale, myCardsScale, myCardsScale)
            : new Vector3(normalScale, normalScale, normalScale);

        // Animate cards to each other's old positions
        StartCoroutine(MoveCardCoroutine(cardBOldPos, cardAObj, 1, cardBOldRot, targetScaleInHandB));
        yield return StartCoroutine(MoveCardCoroutine(cardAOldPos, cardBObj, 1, cardAOldRot, targetScaleInHandA));

        if (sunuFlag)
        {
            showcaseOriginalTransforms.Remove(cardBObj);
        }

        // Track parenting changes for move chain synchronization
        string oldParentA = cardAObj.transform.parent != null ? cardAObj.transform.parent.name : "None";
        string oldParentB = cardBObj.transform.parent != null ? cardBObj.transform.parent.name : "None";
        
        // Swap parents but keep world positions for animation
        cardAObj.transform.SetParent(handB, true);
        cardBObj.transform.SetParent(handA, true);
        
        // Track the parenting changes in move chain
        MoveChainIntegrator.TrackCardParentingChange(playerANo, cardAID, oldParentA, handB.name, "Card swap - Şunu Değiş Tokuş");
        MoveChainIntegrator.TrackCardParentingChange(playerBNo, cardBID, oldParentB, handA.name, "Card swap - Şunu Değiş Tokuş");
        MoveChainIntegrator.TrackCardSwap(playerANo, playerBNo, cardAID, cardBID, "Şunu Değiş Tokuş power");

        // --- Insert at correct indexes ---
        // For handA (insert cardBObj at idxA)
        List<Transform> handAChildren = new List<Transform>();
        foreach (Transform child in handA) handAChildren.Add(child);
        handAChildren.Remove(cardBObj.transform);
        if (idxA >= 0 && idxA <= handAChildren.Count)
            handAChildren.Insert(idxA, cardBObj.transform);
        else
            handAChildren.Add(cardBObj.transform);
        for (int j = 0; j < handAChildren.Count; j++)
            handAChildren[j].SetSiblingIndex(j);

        // For handB (insert cardAObj at idxB)
        List<Transform> handBChildren = new List<Transform>();
        foreach (Transform child in handB) handBChildren.Add(child);
        handBChildren.Remove(cardAObj.transform);
        if (idxB >= 0 && idxB <= handBChildren.Count)
            handBChildren.Insert(idxB, cardAObj.transform);
        else
            handBChildren.Add(cardAObj.transform);
        for (int j = 0; j < handBChildren.Count; j++)
            handBChildren[j].SetSiblingIndex(j);

        // Snap final scale to destination-hand scale in case any previous tween/state left residual size.
        cardAObj.transform.localScale = targetScaleInHandB;
        cardBObj.transform.localScale = targetScaleInHandA;

        // Set auto-rotate flags and input
        if (thisPlayerNumber == playerANo)
        {
            cardBObj.GetComponent<CardInteraction>().StartAutoRotate();
            //cardBObj.GetComponent<CardInteraction>().SelectCard();
            cardAObj.GetComponent<CardInteraction>().StopAutoRotate();
        }
        else if (thisPlayerNumber == playerBNo)
        {
            cardAObj.GetComponent<CardInteraction>().StartAutoRotate();
            //cardAObj.GetComponent<CardInteraction>().SelectCard();
            cardBObj.GetComponent<CardInteraction>().StopAutoRotate();
        }

        //UpdateCurrentPlayerHandLayout();
        CardInteraction.currentlySelectedCard = null;
        GameManager.LocalInstance.SetCurrentSelectedHandCardNull();
        
        //yield return new WaitForSeconds(0.5f);
    }



    public List<string> GetOpponentHandCardIDs(int absolutePlayerNo)
    {
        int myNo = thisPlayerNumber;
        int playerCount = this.playerCount;
        int relativeIndex = (absolutePlayerNo - myNo + playerCount) % playerCount;
        Transform handTransform = playerHandTransforms[GetHandIndex(relativeIndex)];
        List<string> handCardIDs = new List<string>();
        int actualIndex = 0;
        foreach (Transform child in handTransform)
        {
            if (actualIndex >= 0) // skip pool/extra
            {
                CardInteraction ci = child.GetComponent<CardInteraction>();
                if (ci != null) handCardIDs.Add(ci.uniqueCardInstanceID);
            }
            actualIndex++;
        }
        return handCardIDs;
    }

    /// <summary>
    /// Swaps a specific card in player A's hand (by index) with a specific card in player B's hand (by unique ID), preserving the index in A's hand.
    /// </summary>
    public void SwapCardsBetweenPlayersByIDAtIndex(int playerANo, string cardAID, int playerBNo, string cardBID, int handIndexA)
    {
        StartCoroutine(SwapCardsAndUpdateLayoutCoroutine(playerANo, cardAID, playerBNo, cardBID, handIndexA));
    }

    public IEnumerator SwapCardsAndUpdateLayoutCoroutine(int playerANo, string cardAID, int playerBNo, string cardBID, int handIndexA, bool sunuFlag = false)
    {
        int relA = (playerANo - thisPlayerNumber + playerCount) % playerCount;
        int relB = (playerBNo - thisPlayerNumber + playerCount) % playerCount;

        Transform handA = playerHandTransforms[GetHandIndex(relA)];
        Transform handB = playerHandTransforms[GetHandIndex(relB)];

        GameObject cardAObj = CardInteraction.cardLookup[cardAID].gameObject;
        GameObject cardBObj = CardInteraction.cardLookup[cardBID].gameObject;

        // --- Find original indexes ---
        int idxA = -1, idxB = -1, i = 0;
        foreach (Transform child in handA)
        {
            if (child.gameObject == cardAObj) { idxA = i; break; }
            i++;
        }
        i = 0;
        foreach (Transform child in handB)
        {
            if (child.gameObject == cardBObj) { idxB = i; break; }
            i++;
        }

        // Store world positions before changing parents
        Vector3 cardAOldPos = cardAObj.transform.position;
        Quaternion cardAOldRot = cardAObj.transform.rotation;
        Vector3 cardAOldScale = cardAObj.transform.localScale;

        Vector3 cardBOldPos = cardBObj.transform.position;
        Quaternion cardBOldRot = cardBObj.transform.rotation;
        Vector3 cardBOldScale = cardBObj.transform.localScale;

        // Track parenting changes for move chain synchronization
        string oldParentA = cardAObj.transform.parent != null ? cardAObj.transform.parent.name : "None";
        string oldParentB = cardBObj.transform.parent != null ? cardBObj.transform.parent.name : "None";
        
        // Set parents
        cardAObj.transform.SetParent(handB, true);
        cardBObj.transform.SetParent(handA, true);
        
        // Track the parenting changes in move chain
        MoveChainIntegrator.TrackCardParentingChange(thisPlayerNumber, cardAID, oldParentA, handB.name, "Card swap - Index-based swap");
        MoveChainIntegrator.TrackCardParentingChange(thisPlayerNumber, cardBID, oldParentB, handA.name, "Card swap - Index-based swap");
        MoveChainIntegrator.TrackCardSwap(thisPlayerNumber, (thisPlayerNumber + 1) % playerCount, cardAID, cardBID, "Index-based card swap");

        // Animate cards to each other's old positions and wait for both to finish
        var moveA = MoveCardCoroutine(cardBOldPos, cardAObj, 1, cardBOldRot, new Vector3(normalScale, normalScale, normalScale));
        var moveB = MoveCardCoroutine(cardAOldPos, cardBObj, 1, cardAOldRot, new Vector3(myCardsScale, myCardsScale, myCardsScale));
        yield return StartCoroutine(WaitForBoth(moveA, moveB));
        if (sunuFlag)
        {
            showcaseOriginalTransforms.Remove(cardBObj);
        }

        // --- Insert cardBObj at the correct index in handA ---
        List<Transform> handAChildren = new List<Transform>();
        foreach (Transform child in handA) handAChildren.Add(child);
        handAChildren.Remove(cardBObj.transform);
        if (idxA >= 0 && idxA <= handAChildren.Count)
            handAChildren.Insert(idxA, cardBObj.transform);
        else
            handAChildren.Add(cardBObj.transform);
        for (int j = 0; j < handAChildren.Count; j++)
            handAChildren[j].SetSiblingIndex(j);

        // --- Insert cardAObj at the correct index in handB ---
        List<Transform> handBChildren = new List<Transform>();
        foreach (Transform child in handB) handBChildren.Add(child);
        handBChildren.Remove(cardAObj.transform);
        if (idxB >= 0 && idxB <= handBChildren.Count)
            handBChildren.Insert(idxB, cardAObj.transform);
        else
            handBChildren.Add(cardAObj.transform);
        for (int j = 0; j < handBChildren.Count; j++)
            handBChildren[j].SetSiblingIndex(j);

        // Set auto-rotate flags and input
        if (thisPlayerNumber == playerANo)
        {
            cardBObj.GetComponent<CardInteraction>().StartAutoRotate();
            //cardBObj.GetComponent<CardInteraction>().OnCardTouched(Input.mousePosition);
            cardAObj.GetComponent<CardInteraction>().StopAutoRotate();
        }
        else if (thisPlayerNumber == playerBNo)
        {
            cardAObj.GetComponent<CardInteraction>().StartAutoRotate();
            //cardAObj.GetComponent<CardInteraction>().OnCardTouched(Input.mousePosition);
            cardBObj.GetComponent<CardInteraction>().StopAutoRotate();
        }

        //UpdateCurrentPlayerHandLayout();

        UpdateShowcaseOriginalsAfterSwap(cardAObj);
        UpdateShowcaseOriginalsAfterSwap(cardBObj);

        //if (!GameManager.LocalInstance.isSunuDegisBunuTokusActive) ExitShowcaseAllOtherHands();

        CardInteraction.currentlySelectedCard = null;
        GameManager.LocalInstance.SetCurrentSelectedHandCardNull();
    }



    /// <summary>
    /// Assigns and visually formats card GameObjects to player pools with proper layout offsets and rotation.
    /// </summary>
    public void AssignCardsToPlayerPools(SerializableDictionary playersPooledCardsIDsSerialized)
    {
        Dictionary<int, List<string>> playersPooledCardsIDs = playersPooledCardsIDsSerialized.ToDictionary();
        if (playerPoolTransforms == null || playerPoolTransforms.Count == 0)
        {
            
            return;
        }

        // Aggregate cards by target poolIndex
        Dictionary<int, List<string>> aggregatedPools = new Dictionary<int, List<string>>();
        foreach (var kvp in playersPooledCardsIDs)
        {
            int relSeat = kvp.Key;
            int poolIndex = GetPoolIndex(relSeat);
            if (!aggregatedPools.ContainsKey(poolIndex))
            {
                aggregatedPools[poolIndex] = new List<string>();
            }
            aggregatedPools[poolIndex].AddRange(kvp.Value);
        }

        foreach (var kvp in aggregatedPools)
        {
            int poolIndex = kvp.Key;
            List<string> cardIDs = kvp.Value;

            Transform poolTransform = playerPoolTransforms[poolIndex];

            // Re-sync the layout offset counters based on the count of rebuilt pool cards
            ResetOffsetCounterForPool(poolIndex, cardIDs.Count);

            for (int i = 0; i < cardIDs.Count; i++)
            {
                string cardID = cardIDs[i];
                if (CardInteraction.cardLookup.TryGetValue(cardID, out var cardInteraction))
                {
                    GameObject cardObj = cardInteraction.gameObject;
                    cardObj.transform.SetParent(poolTransform, false);

                    // Re-connection fix: Place exactly at pool world position (localPosition = zero)
                    cardObj.transform.localPosition = Vector3.zero;

                    // Re-connection fix: Set world rotation to avoid double-rotation under parent
                    Vector3 tempRotation = poolTransform.rotation.eulerAngles;
                    cardObj.transform.rotation = Quaternion.Euler(tempRotation.x - 90, tempRotation.y, tempRotation.z + UnityEngine.Random.Range(170f, 190f));

                    // Always use normal scale
                    int targetScale = normalScale;
                    cardObj.transform.localScale = new Vector3(targetScale, targetScale, targetScale);
                }
                else
                {
                    
                }
            }
        }
    }

    /// <summary>
    /// Assigns and visually formats card GameObjects to player Pişti pools.
    /// </summary>
    public void AssignCardsToPlayerPistiPools(SerializableDictionary playersPistiCardsIDsSerialized)
    {
        Dictionary<int, List<string>> playersPistiCardsIDs = playersPistiCardsIDsSerialized.ToDictionary();
        if (playerPiştiPoolTransforms == null || playerPiştiPoolTransforms.Count == 0)
        {
            
            return;
        }

        // Aggregate pişti cards by target poolIndex
        Dictionary<int, List<string>> aggregatedPistiPools = new Dictionary<int, List<string>>();
        foreach (var kvp in playersPistiCardsIDs)
        {
            int relSeat = kvp.Key;
            int poolIndex = GetPoolIndex(relSeat);
            if (!aggregatedPistiPools.ContainsKey(poolIndex))
            {
                aggregatedPistiPools[poolIndex] = new List<string>();
            }
            aggregatedPistiPools[poolIndex].AddRange(kvp.Value);
        }

        foreach (var kvp in aggregatedPistiPools)
        {
            int poolIndex = kvp.Key;
            List<string> cardIDs = kvp.Value;

            Transform pistiTransform = playerPiştiPoolTransforms[poolIndex];

            for (int i = 0; i < cardIDs.Count; i++)
            {
                string cardID = cardIDs[i];
                if (CardInteraction.cardLookup.TryGetValue(cardID, out var cardInteraction))
                {
                    GameObject cardObj = cardInteraction.gameObject;
                    cardObj.transform.SetParent(pistiTransform, false);

                    Vector3 tempRotation = pistiTransform.rotation.eulerAngles;
                    int targetScale = normalScale;
                    cardObj.transform.localScale = new Vector3(targetScale, targetScale, targetScale);

                    // Re-connection fix: Set world position and world rotation directly
                    if (i == cardIDs.Count - 1 && cardIDs.Count > 1)
                    {
                        // Capturing card faces UP
                        cardObj.transform.position = new Vector3(pistiTransform.position.x + 100 + i, pistiTransform.position.y, pistiTransform.position.z - i + 10);
                        cardObj.transform.rotation = Quaternion.Euler(tempRotation.x + 90, tempRotation.y, tempRotation.z + UnityEngine.Random.Range(170f, 190f) + 90);
                    }
                    else
                    {
                        // Base pişti cards face DOWN
                        cardObj.transform.position = pistiTransform.position;
                        cardObj.transform.rotation = Quaternion.Euler(tempRotation.x - 90, tempRotation.y, tempRotation.z + UnityEngine.Random.Range(170f, 190f));
                    }
                }
                else
                {
                    
                }
            }
        }
    }

    /// <summary>
    /// Private helper to synchronize layout counters with rebuilt pool sizes
    /// </summary>
    private void ResetOffsetCounterForPool(int poolIndex, int cardCount)
    {
        int counterVal = cardCount - 1;
        if (poolIndex == 0) offsetCounter0 = counterVal;
        else if (poolIndex == 1) offsetCounter1 = counterVal;
        else if (poolIndex == 2) offsetCounter2 = counterVal;
    }

    public void AssignCardsToPlayerHands(Dictionary<int, List<string>> playersHandCardsIDs)
    {
        if (playerHandTransforms == null || playerHandTransforms.Count == 0)
        {
            
            return;
        }

        foreach (var kvp in playersHandCardsIDs)
        {
            int playerNo = kvp.Key;
            List<string> cardIDs = kvp.Value;

            int handIndex = GetHandIndex(playerNo); // Use your existing logic
            Transform handTransform = playerHandTransforms[handIndex];

            foreach (string cardID in cardIDs)
            {
                // SIMPLIFIED: Just use the cardLookup directly - no mapping needed
                if (CardInteraction.cardLookup.TryGetValue(cardID, out CardInteraction cardInteraction))
                {
                    GameObject cardObj = cardInteraction.gameObject;
                    cardObj.transform.SetParent(handTransform, false);
                }
                else
                {
                    
                }
            }
        }
    }

    private IEnumerator TweenMoveTransform(Transform target, Vector3 endPos, Quaternion endRot, Vector3 endScale, float duration)
    {
        // If instant mode (reconnection), teleport immediately
        if (isInstantMode)
        {
            target.position = endPos;
            target.rotation = endRot;
            target.localScale = endScale;
            yield return null;
        }
        else
        {
            DG.Tweening.Sequence seq = DOTween.Sequence();
            seq.Join(target.DOMove(endPos, duration));
            seq.Join(target.DORotateQuaternion(endRot, duration));
            seq.Join(target.DOScale(endScale, duration));
            yield return seq.WaitForCompletion();
        }
    }

    // ===== DECK MOVEMENT METHODS =====
    
    /// <summary>
    /// Moves the deck from starting position to reach point
    /// </summary>
    public void MoveDeckToReachPoint()
    {
        if (isDeckMoving || deckReachPoint == null) return;
        
        // Get the DeckTransform GameObject that actually needs to move
        Transform deckTransform = GameObject.Find("DeckTransform").transform;
        if (deckTransform == null)
        {
            
            return;
        }
        
        
        
        // Kill any existing movement sequence
        if (deckMoveSequence != null)
            deckMoveSequence.Kill();
        
        isDeckMoving = true;
        isDeckAtReachPoint = true;
        
        // Create movement sequence - move the DeckTransform, not this GameObject
        deckMoveSequence = DOTween.Sequence();
        deckMoveSequence.Append(deckTransform.DOMove(deckReachPoint.position, deckMoveSpeed).SetEase(deckMoveEase));
        deckMoveSequence.OnComplete(() => {
            isDeckMoving = false;
            
        });
    }
    
    /// <summary>
    /// Moves the deck from reach point back to starting position
    /// </summary>
    public void MoveDeckToStartingPosition()
    {
        if (isDeckMoving) return;
        
        // Get the DeckTransform GameObject that actually needs to move
        Transform deckTransform = GameObject.Find("DeckTransform").transform;
        if (deckTransform == null)
        {
            
            return;
        }
        
        
        
        // Kill any existing movement sequence
        if (deckMoveSequence != null)
            deckMoveSequence.Kill();
        
        isDeckMoving = true;
        isDeckAtReachPoint = false;
        
        // Create movement sequence - move the DeckTransform, not this GameObject
        deckMoveSequence = DOTween.Sequence();
        deckMoveSequence.Append(deckTransform.DOMove(deckStartingPosition, deckMoveSpeed).SetEase(deckMoveEase));
        deckMoveSequence.OnComplete(() => {
            isDeckMoving = false;
            
        });
    }
    
    /// <summary>
    /// Public method to check if deck is at reach point
    /// </summary>
    public bool IsDeckAtReachPoint()
    {
        return isDeckAtReachPoint;
    }
    
    /// <summary>
    /// Public method to check if deck is moving
    /// </summary>
    public bool IsDeckMoving()
    {
        return isDeckMoving;
    }
    
    /// <summary>
    /// Public method to manually trigger movement to reach point (for testing)
    /// </summary>
    [ContextMenu("Move Deck to Reach Point")]
    public void TriggerMoveDeckToReachPoint()
    {
        MoveDeckToReachPoint();
    }
    
    /// <summary>
    /// Public method to manually trigger movement to starting position (for testing)
    /// </summary>
    [ContextMenu("Move Deck to Starting Position")]
    public void TriggerMoveDeckToStartingPosition()
    {
        MoveDeckToStartingPosition();
    }
    
    /// <summary>
    /// Destroy all 52 card GameObjects to ensure clean slate for new game.
    /// Call this when leaving game or disconnecting.
    /// </summary>
    public void DestroyAllCards()
    {
        
        
        int destroyedCount = 0;
        
        // Destroy all card GameObjects from cardObjectList
        if (cardObjectList != null)
        {
            foreach (var cardObject in cardObjectList)
            {
                if (cardObject != null)
                {
                    Destroy(cardObject);
                    destroyedCount++;
                }
            }
            cardObjectList.Clear();
            
        }
        
        // Clear all references
        if (cardInteractionList != null)
        {
            cardInteractionList.Clear();
        }
        
        if (activeCards != null)
        {
            activeCards.Clear();
        }
        
        if (deckPool != null)
        {
            deckPool.Clear();
        }
        
        if (orderedCardObjectList != null)
        {
            orderedCardObjectList.Clear();
        }
        
        // Clear CardInteraction static lookup
        if (CardInteraction.cardLookup != null)
        {
            CardInteraction.cardLookup.Clear();
        }
        
        
    }
    
    void OnDestroy()
    {
        // Clean up DOTween sequences
        if (deckMoveSequence != null)
            deckMoveSequence.Kill();
    }
    
    
    private void OnDrawGizmosSelected()
    {
        // Draw the deck reach point in the scene view
        if (deckReachPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(deckReachPoint.position, 0.5f);
            
            // Draw line from current deck position to reach point
            Transform deckTransform = GameObject.Find("DeckTransform").transform;
            if (deckTransform != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(deckTransform.position, deckReachPoint.position);
            }
        }
    }
}

