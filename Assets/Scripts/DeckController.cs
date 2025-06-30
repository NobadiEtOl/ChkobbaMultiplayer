using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class DeckController : MonoBehaviour
{
    public static DeckController LocalInstance;
    public int thisPlayerNumber;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private List<GameObject> cardPrefabsList;//Prefabs of all the cards.
    private Dictionary<string, GameObject> cardPrefabs;//A dictionary to keep track of each card prefabs with its ID
    private Dictionary<string, GameObject> deckPool;//A dictionary of card ID and a list of all the instantiated cards
    private List<GameObject> activeCards = new List<GameObject>();
    private List<CardInteraction> cardInteractionList; // List to store CardInteraction references
    private List<Transform> playerHandTransforms = new List<Transform>();
    private List<Transform> playerPoolTransforms = new List<Transform>();
    private Transform centerTransform;
    private List<Transform> playerPiştiPoolTransforms = new List<Transform>();
    private int relativeIndex = 0;
    public int playerCount = 0;
    int offset = 150;
    int offsetCounter0 = -1;
    int offsetCounter1 = -1;
    int offsetCounter2 = -1;
    private int zOffsetCounter = 0;

    private int initialScale = 600; // Initial scale for the cards
    private int normalScale = 750; // Scale for the normal cards
    private int centerScale = 900; // Scale for the center cards
    private int myCardsScale = 1200; // Scale for the player's cards
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
        InitialDeckSetUp();
    }

    //Called when the deck is ready to start
    public IEnumerator DeckStart()
    {
        yield return StartCoroutine(DefineCardPrefabs());
        yield return StartCoroutine(InitializeCardPool());
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
                //Debug.LogWarning($"Card prefab with ID {cardIDString} already exists. Skipping...");
            }
        }
        yield return null;
    }


    //Initialize and instantiate all the card objects that can be used
    private List<GameObject> cardObjectList = new List<GameObject>();
    private int uniqueCardCounter = 0;
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
                    string uniqueID = "card_" + uniqueCardCounter++;
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
            Debug.LogWarning("cardInteractionList.Count: " + cardInteractionList.Count);
            //deckTransform.rotation = Quaternion.Euler(90, 0, 0);
        }

        else yield return StartCoroutine(ResetCards());


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
                card.transform.parent = deckTransform;
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
        yield return StartCoroutine(LerpForDeckTransform(deckTransform, originalDeckPosition, originalDeckRotation, 0.5f));
    }

    private IEnumerator LerpForDeckTransform(Transform target, Vector3 endPos, Quaternion endRot, float duration = 0.5f)
    {
        Vector3 startPos = target.position;
        Quaternion startRot = target.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            target.position = Vector3.Lerp(startPos, endPos, t);
            target.rotation = Quaternion.Lerp(startRot, endRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        target.position = endPos;
        target.rotation = endRot;
    }

    //Deals to players according to the playerCount
    public void DealPlayers(int playerCount, Dictionary<int, List<string>> playerHands)
    {
        this.playerCount = playerCount;

        if (playerCount != 2 && playerCount != 4)
        {
            //Debug.LogError("Player number is different from 2 or 4");
            return;
        }

        if (playerCount == 2)
        {
            DealTwoPlayers(playerHands);
        }
        else if (playerCount == 4)
        {
            DealFourPlayers(playerHands);
        }
    }

    private void DealTwoPlayers(Dictionary<int, List<string>> playerHands)
    {
        var cardObjects = new List<GameObject>();
        var positions = new List<Vector3>();
        var rotations = new List<Quaternion>();
        var scales = new List<Vector3>(); // List to store scales

        for (int n = 0; n < 2; n++)
        {
            int i = (startingPlayerNoCounter + n) % 2;
            relativeIndex = (i - thisPlayerNumber + playerCount) % playerCount;
            Debug.LogWarning("relativeIndex: " + relativeIndex);

            if (relativeIndex == 0) gameManager.myCards = new List<string>();

            for (int j = 0; j < 4; j++)
            {
                string uniqueCardID = playerHands[i][j];
                if (relativeIndex == 0) gameManager.myCards.Add(uniqueCardID);
                //if (relativeIndex == 1) relativeIndex=2;

                GameObject tempCardObject = CardInteraction.cardLookup[uniqueCardID].gameObject;
                if (tempCardObject == null) continue;

                cardObjects.Add(tempCardObject);

                tempCardObject.transform.parent = playerHandTransforms[GetPoolIndex(relativeIndex)].transform;

                var playerHandTransform = playerHandTransforms[GetPoolIndex(relativeIndex)];
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
                        rotation = Quaternion.Euler(centerRotation.x, centerRotation.y + (randomOffset1 * 3), centerRotation.z);
                        //scales.Add(new Vector3(800, 800, 800));
                        break;
                    case 1: // Top (Player 2)
                        offset = new Vector3(randomOffset1 * randomOffset2, j * 5, randomOffset1 * randomOffset2);
                        rotation = Quaternion.Euler(centerRotation.x, centerRotation.y + (randomOffset1 * 3), centerRotation.z);
                        //scales.Add(new Vector3(600, 600, 600));
                        break;
                }

                positions.Add(basePos + offset);
                rotations.Add(rotation);
                scales.Add(new Vector3(normalScale, normalScale, normalScale));
            }
        }

        StartCoroutine(ChainMoveCards(positions, cardObjects, 10, rotations, scales));
    }

    private void DealFourPlayers(Dictionary<int, List<string>> playerHands)
    {
        var cardObjects = new List<GameObject>();
        var positions = new List<Vector3>();
        var rotations = new List<Quaternion>();
        var scales = new List<Vector3>(); // List to store scales

        for (int n = 0; n < 4; n++)
        {
            int i = (startingPlayerNoCounter + n) % 4;
            relativeIndex = (i - thisPlayerNumber + playerCount) % playerCount;
            if (relativeIndex == 0) gameManager.myCards = new List<string>();

            for (int j = 0; j < 4; j++)
            {
                string uniqueCardID = playerHands[i][j];
                if (relativeIndex == 0) gameManager.myCards.Add(uniqueCardID);

                GameObject tempCardObject = CardInteraction.cardLookup[uniqueCardID].gameObject;
                if (tempCardObject == null) continue;

                cardObjects.Add(tempCardObject);

                var playerHandTransform = playerHandTransforms[GetPoolIndex(relativeIndex)];

                tempCardObject.transform.parent = playerHandTransform.transform;
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
                        rotation = Quaternion.Euler(centerRotation.x, centerRotation.y + (randomOffset1 * 3), centerRotation.z);
                        //scales.Add(new Vector3(800, 800, 800));
                        break;
                    case 1: // Right (Player 1)
                        offset = new Vector3(randomOffset1 * randomOffset2, j * 5, randomOffset1 * randomOffset2);
                        rotation = Quaternion.Euler(centerRotation.x, centerRotation.y + 90 + (randomOffset1 * 3), centerRotation.z);
                        //scales.Add(new Vector3(700, 700, 700));
                        break;
                    case 2: // Top (Player 2)
                        offset = new Vector3(randomOffset1 * randomOffset2, j * 5, randomOffset1 * randomOffset2);
                        rotation = Quaternion.Euler(centerRotation.x, centerRotation.y + (randomOffset1 * 3), centerRotation.z);
                        //scales.Add(new Vector3(600, 600, 600));
                        break;
                    case 3: // Left (Player 3)
                        offset = new Vector3(randomOffset1 * randomOffset2, j * 5, randomOffset1 * randomOffset2);
                        rotation = Quaternion.Euler(centerRotation.x, centerRotation.y + 90 + (randomOffset1 * 3), centerRotation.z);
                        //scales.Add(new Vector3(700, 700, 700));
                        break;
                }

                positions.Add(basePos + offset);
                rotations.Add(rotation);
                scales.Add(new Vector3(normalScale, normalScale, normalScale));
            }
        }

        StartCoroutine(ChainMoveCards(positions, cardObjects, 10, rotations, scales));
    }

    private int startingPlayerNoCounter = -1;
    //Deals to center according to the playerCount
    public void DealCenter(List<string> centerCardIDs)
    {
        startingPlayerNoCounter++;
        SendCardInteractionsToGameManager();
        List<GameObject> cardObjects = new List<GameObject>();
        List<Vector3> positions = new List<Vector3>();
        List<Quaternion> rotations = new List<Quaternion>();
        List<Vector3> scales = new List<Vector3>();

        for (int i = 0; i < 4; i++)
        {
            string uniqueCardID = centerCardIDs[i];
            GameObject tempCenterCard = CardInteraction.cardLookup[uniqueCardID].gameObject;
            cardObjects.Add(tempCenterCard);
            if (tempCenterCard != null)
            {
                tempCenterCard.transform.parent = centerTransform;

                // Calculate position based on centerTransform
                Vector3 centerPosition = centerTransform.position;
                Vector3 centerRotation = centerTransform.rotation.eulerAngles;
                //positions.Add(new Vector3(centerPosition.x, centerPosition.y, centerPosition.z + i * -10));
                //else positions.Add(new Vector3(centerPosition.x, centerPosition.y, centerPosition.z - 10 + i * -10 ));


                gameManager.centerCardsObjects.Add(tempCenterCard);
                gameManager.centerCards.Add(uniqueCardID, CardInteraction.cardLookup[uniqueCardID].GetCardID());

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

        StartCoroutine(ChainMoveCards(positions, cardObjects, 10, rotations, scales));
    }

    private void SendCardInteractionsToGameManager()
    {
        gameManager.GetCardInteractionScripts(cardInteractionList);
    }

    public void DiscardHandCardToCenter(string uniqueCardID, int[] cardID)
    {
        GameObject placedCard = CardInteraction.cardLookup[uniqueCardID].gameObject;
        List<Vector3> positions = new List<Vector3>();
        List<GameObject> cardObjects = new List<GameObject>();
        List<Quaternion> rotations = new List<Quaternion>();
        List<Vector3> scales = new List<Vector3>();

        if (placedCard != null)
        {
            Vector3 centerPosition = centerTransform.position;
            Vector3 centerRotation = centerTransform.rotation.eulerAngles;

            rotations.Add(Quaternion.Euler(centerRotation.x + 180, centerRotation.y, UnityEngine.Random.Range(-12, 12)));
            placedCard.transform.parent = centerTransform;

            // Use centerTransform for positioning
            positions.Add(new Vector3(centerPosition.x, centerPosition.y + 10 * GameManager.LocalInstance.centerCardsObjects.Count, centerPosition.z));
            scales.Add(new Vector3(centerScale, centerScale, centerScale));

            gameManager.centerCards.Add(uniqueCardID, cardID);
            gameManager.centerCardsObjects.Add(placedCard);
            cardObjects.Add(placedCard);
        }

        AudioManager.Instance.PlayAudio(3, 1, false);
        StartCoroutine(ChainMoveCards(positions, cardObjects, 10, rotations, scales, true));
        //UpdateCurrentPlayerHandLayout();
    }

    public void UpdateCurrentPlayerHandLayout()
    {
        if (isShowcaseAllActive)
        {
            ShowcaseAllOtherHandsLayout();
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

        for (int handIdx = 0; handIdx < playerHandTransforms.Count; handIdx++)
        {
            foreach (Transform child in playerHandTransforms[handIdx])
            {
                var ci = child.GetComponent<CardInteraction>();
                if (ci == null) continue;
                if (handIdx == 0)
                {
                    //ci.autoRotateFlag = true;
                }
                else
                {
                    ci.autoRotateFlag = false;
                }
            }
        }
    }

    [ContextMenu("ShowcaseAllOtherHands")]
    private void ShowcaseAllOtherHandsLayout()
    {
        float spacing = 500f;
        int handCount = playerHandTransforms.Count;
        int startIdx = 1; // Skip playerHandTransforms[0] (your hand)

        // Layout for your hand (player 0) - normal layout, autoRotateFlag = true
        foreach (Transform child in playerHandTransforms[0])
        {
            var ci = child.GetComponent<CardInteraction>();
            if (ci != null)
            {
                ci.autoRotateFlag = true;
            }
        }

        // Layout for other hands (showcase)
        for (int handIdx = startIdx; handIdx < handCount; handIdx++)
        {
            Transform hand = playerHandTransforms[handIdx];
            var playerCards = new List<GameObject>();
            int counter = 0;
            foreach (Transform child in hand)
            {
                if (counter > 1) playerCards.Add(child.gameObject);
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
                var ci = card.GetComponent<CardInteraction>();
                if (ci == null) continue;

                // Store original transform and flags if not already stored
                if (!showcaseOriginalTransforms.ContainsKey(card))
                    showcaseOriginalTransforms[card] = (card.transform.position, card.transform.rotation, card.transform.localScale, ci.autoRotateFlag);

                Vector3 offset = Vector3.zero;
                Quaternion rotation = Quaternion.identity;
                Vector3 scale = new Vector3(centerScale, centerScale, centerScale);

                switch (handIdx)
                {
                    case 1: // Right
                    case 3: // Left
                        offset = new Vector3(0, i * 10, spacing * 3f * (i - offsetMult));
                        rotation = Quaternion.Euler(centerRotation.x, centerRotation.y + 90, centerRotation.z);
                        break;
                    case 2: // Top
                        offset = new Vector3(spacing * 3f * (i - offsetMult), i * 10, 0);
                        rotation = Quaternion.Euler(centerRotation.x, centerRotation.y, centerRotation.z);
                        break;
                    default:
                        offset = new Vector3(spacing * 3f * (i - offsetMult), i * 10, 0);
                        rotation = Quaternion.Euler(centerRotation.x, centerRotation.y, centerRotation.z);
                        break;
                }

                Vector3 targetPosition = basePos + offset;
                MoveCard(targetPosition, card, 10, rotation, scale, false);

                ci.autoRotateFlag = false;
            }
        }
    }

    public void ShowcaseAllOtherHands()
    {
        isShowcaseAllActive = true;
        ShowcaseAllOtherHandsLayout();
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
        int counter = 0;
        foreach (Transform child in currentPlayerHand)
        {
            //Each hand has 2 child for normal and pişti pools
            if (counter > 1) playerCards.Add(child.gameObject);
            counter++;
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

            switch (playerNumber)
            {
                case 0: // Bottom (Player 0)
                    offset = new Vector3(spacing * 15f * (i - offsetMult), 1000, 0);
                    rotation = Quaternion.Euler(-centerRotation.x, centerRotation.y, centerRotation.z);
                    currentScale = new Vector3(myCardsScale, myCardsScale, myCardsScale);
                    break;
                case 2: // Top (Player 2)
                    offset = new Vector3(spacing * 3f * (i - offsetMult), 1000 + (i * 10), 0);
                    rotation = Quaternion.Euler(centerRotation.x, centerRotation.y, centerRotation.z);
                    currentScale = new Vector3 (normalScale,normalScale,normalScale);
                    break;
            }

            Vector3 targetPosition = playerHandTransforms[playerNumber].position + offset;
            MoveCard(targetPosition, playerCards[i], 10, rotation, currentScale);
        }
        if (playerNumber == 0) StartCoroutine(SetAutoRotateFlagTrue(playerCards));
        //Debug.LogWarning(playerNumber);
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
        int counter = 0;
        foreach (Transform child in currentPlayerHand)
        {
            //Each hand has 2 child for normal and pişti pools
            if (counter > 1) playerCards.Add(child.gameObject);
            counter++;
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

            switch (playerNumber)
            {
                case 0: // Bottom (Player 0)
                    offset = new Vector3(spacing * 15f * (i - offsetMult), 1000, 0);
                    rotation = Quaternion.Euler(-centerRotation.x, centerRotation.y, centerRotation.z);
                    currentScale = new Vector3(myCardsScale, myCardsScale, myCardsScale);
                    break;
                case 1: // Right (Player 1)
                    offset = new Vector3(0, i * 10, spacing * 3f * (i - offsetMult));
                    rotation = Quaternion.Euler(centerRotation.x, centerRotation.y + 90, centerRotation.z);
                    currentScale = new Vector3 (normalScale,normalScale,normalScale);
                    break;
                case 2: // Top (Player 2)
                    offset = new Vector3(spacing * 3f * (i - offsetMult), i * 10, 0);
                    rotation = Quaternion.Euler(centerRotation.x, centerRotation.y, centerRotation.z);
                    currentScale = new Vector3 (normalScale,normalScale,normalScale);
                    break;
                case 3: // Left (Player 3)
                    offset = new Vector3(0, i * 10, spacing * 3f * (i - offsetMult));
                    rotation = Quaternion.Euler(centerRotation.x, centerRotation.y + 90, centerRotation.z);
                    currentScale = new Vector3 (normalScale,normalScale,normalScale);
                    break;
            }

            Vector3 targetPosition = playerHandTransforms[playerNumber].position + offset;
            MoveCard(targetPosition, playerCards[i], 10, rotation, currentScale);
        }
        if (playerNumber == 0) StartCoroutine(SetAutoRotateFlagTrue(playerCards));
        //Debug.LogWarning(playerNumber);
    }

    public IEnumerator SetAutoRotateFlagTrue(List<GameObject> cardObjects)
    {
        //Debug.LogWarning("CardInteraction not found for the specified GameObject.");
        yield return new WaitForSeconds(0.2f);
        // Search the list for a CardInteraction with the matching GameObject
        foreach (var cardObject in cardObjects)
        {
            foreach (var cardInteraction in cardInteractionList)
            {
                if (cardInteraction.gameObject == cardObject)
                {
                    cardInteraction.autoRotateFlag = true; // Return the matching CardInteraction
                }
            }
        }

    }

    public void SetAutoRotateFlagFalse(GameObject cardObject)
    {
        //Debug.LogWarning("CardInteraction not found for the specified GameObject.");

        foreach (var cardInteraction in cardInteractionList)
        {
            if (cardInteraction.gameObject == cardObject)
            {
                cardInteraction.autoRotateFlag = false; // Return the matching CardInteraction
                return;
            }
        }



        //Debug.LogWarning("CardInteraction not found for the specified GameObject.");

    }

    private Dictionary<GameObject, (Vector3 pos, Quaternion rot, Vector3 scale)> poolCardOriginalTransforms = new Dictionary<GameObject, (Vector3, Quaternion, Vector3)>();
    private bool isShowcasing = false;
    [ContextMenu("Showcase Player Pool Cards")]
    public void ShowcasePlayerPoolCards()
    {
        isShowcasing = true;
        int poolIndex = 0;
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

        // --- Layout Regular Pool Cards (sorted) ---
        if (poolCards.Count > 0)
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
                Quaternion rotation = Quaternion.Euler(90, 0, 0);
                Vector3 targetPosition = new Vector3(spacing * (i - offsetMult), (i * 10) + centerTransform.position.y + 5000, baseZ);
                Vector3 targetScale = new Vector3(myCardsScale, myCardsScale, myCardsScale);
                MoveCard(targetPosition, poolCards[i], 10, rotation, targetScale, false);
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
                    MoveCard(targetPosition, piştiCards[cardIdx], 10, rotation, targetScale, false);
                }
            }
        }
    }

    [ContextMenu("Stop Showcase Player Pool Cards")]
    public void StopShowcasePlayerPoolCards()
    {
        isShowcasing = false;
        int poolIndex = 0;
        var poolTransform = playerPoolTransforms[poolIndex];
        var piştiPoolTransform = playerPiştiPoolTransforms[poolIndex];

        // Restore regular pool cards
        foreach (Transform child in poolTransform)
        {
            var card = child.gameObject;
            if (poolCardOriginalTransforms.TryGetValue(card, out var original))
            {
                MoveCard(original.pos, card, 10, original.rot, original.scale, false);
            }
        }
        // Restore pişti pool cards
        foreach (Transform child in piştiPoolTransform)
        {
            var card = child.gameObject;
            if (poolCardOriginalTransforms.TryGetValue(card, out var original))
            {
                MoveCard(original.pos, card, 10, original.rot, original.scale, false);
            }
        }
        poolCardOriginalTransforms.Clear();
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
                MoveCard(pos, myPiştiCards[i], 10, Quaternion.Euler(90, 0, 0), new Vector3(cardScale, cardScale, cardScale), false);
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
            MoveCard(pos, myPointCards[i], 10, Quaternion.Euler(90, 0, 0), new Vector3(cardScale, cardScale, cardScale), false);
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
                MoveCard(pos, oppPiştiCards[i], 10, Quaternion.Euler(90, 0, 0), new Vector3(cardScale, cardScale, cardScale), false);
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
            MoveCard(pos, oppPointCards[i], 10, Quaternion.Euler(90, 0, 0), new Vector3(cardScale, cardScale, cardScale), false);
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

        // My team: player 0 and 2
        List<GameObject> myTeamPiştiCards = new List<GameObject>();
        List<GameObject> myTeamPointCards = new List<GameObject>();
        foreach (int idx in new int[] { GetPoolIndex(0), GetPoolIndex(2) })
        {
            foreach (Transform t in playerPiştiPoolTransforms[idx]) myTeamPiştiCards.Add(t.gameObject);
            foreach (Transform t in playerPoolTransforms[idx])
                if (IsPointCard(t.gameObject)) myTeamPointCards.Add(t.gameObject);
        }

        // Opponent team: player 1 and 3
        List<GameObject> oppTeamPiştiCards = new List<GameObject>();
        List<GameObject> oppTeamPointCards = new List<GameObject>();
        foreach (int idx in new int[] { GetPoolIndex(1), GetPoolIndex(3) })
        {
            foreach (Transform t in playerPiştiPoolTransforms[idx]) oppTeamPiştiCards.Add(t.gameObject);
            foreach (Transform t in playerPoolTransforms[idx])
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
                MoveCard(pos, myTeamPiştiCards[i], 10, Quaternion.Euler(90, 0, 0), new Vector3(cardScale, cardScale, cardScale), false);
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
            MoveCard(pos, myTeamPointCards[i], 10, Quaternion.Euler(90, 0, 0), new Vector3(cardScale, cardScale, cardScale), false);
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
                MoveCard(pos, oppTeamPiştiCards[i], 10, Quaternion.Euler(90, 0, 0), new Vector3(cardScale, cardScale, cardScale), false);
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
            MoveCard(pos, oppTeamPointCards[i], 10, Quaternion.Euler(90, 0, 0), new Vector3(cardScale, cardScale, cardScale), false);
        }
    }


    public void TryStopShowcasePlayerPoolCards()
    {
        if (isShowcasing)
        {
            StopShowcasePlayerPoolCards();
        }
    }

    public void MoveCard(Vector3 endPos, GameObject cardObject, float speed, Quaternion rotation, Vector3 scales, bool audioFlag = true)
    {
        // Start the coroutine to move the card
        StartCoroutine(MoveCardCoroutine(endPos, cardObject, speed, rotation, scales, audioFlag));
    }

    private IEnumerator MoveCardCoroutine(Vector3 endPos, GameObject cardObject, float speedMultiplier, Quaternion rotation, Vector3 scale, bool audioFlag = true)
    {
        // Set the initial position, rotation, and scale of the card
        Vector3 startingPos = cardObject.transform.position;
        Quaternion startingRotation = cardObject.transform.rotation;
        Vector3 startingScale = cardObject.transform.localScale;

        if (audioFlag) AudioManager.Instance.PlayAudio(3, 1, false);

        // Calculate the journey length and base speed
        float journeyLength = Vector3.Distance(startingPos, endPos);
        float fixedDuration = 2f; // The time (in seconds) it should take for all cards to move
        float movementSpeed = journeyLength / fixedDuration; // Speed to make all cards take the same time
        float finalSpeed = movementSpeed * speedMultiplier; // Apply the multiplier

        // Track progress over time
        float timeElapsed = 0f;

        while (timeElapsed < journeyLength / finalSpeed)
        {
            // Calculate the percentage of the journey completed
            float t = timeElapsed / (journeyLength / finalSpeed);

            // Move the card's position using Lerp
            cardObject.transform.position = Vector3.Lerp(startingPos, endPos, t);

            // Smoothly interpolate the rotation using Lerp
            cardObject.transform.rotation = Quaternion.Lerp(startingRotation, rotation, t);

            // Smoothly interpolate the scale using Lerp
            cardObject.transform.localScale = Vector3.Lerp(startingScale, scale, t);

            // Increment elapsed time
            timeElapsed += Time.deltaTime;

            // Wait for the next frame
            yield return null;
        }

        // Ensure the card reaches the exact end position, rotation, and scale
        cardObject.transform.position = endPos;
        cardObject.transform.rotation = rotation;
        cardObject.transform.localScale = scale;
    }


    public void RotateCard(Quaternion endRotation, GameObject cardObject, float speed = 20)
    {
        // Start the coroutine to rotate the card
        StartCoroutine(RotateCardCoroutine(endRotation, cardObject, speed));
    }

    private IEnumerator RotateCardCoroutine(Quaternion endRotation, GameObject cardObject, float speed = 5f)
    {
        // Set the initial rotation of the card
        Quaternion startingRotation = cardObject.transform.rotation;

        // Track progress over time
        float timeElapsed = 0f;

        // Assume a normalized speed (rotation doesn't have a direct distance like position)
        while (timeElapsed < 1f)
        {
            // Calculate the percentage of the rotation completed
            float t = timeElapsed / (1f / speed);

            // Smoothly interpolate the rotation using Lerp
            cardObject.transform.rotation = Quaternion.Lerp(startingRotation, endRotation, t);

            // Increment elapsed time
            timeElapsed += Time.deltaTime;

            // Wait for the next frame
            yield return null;
        }

        // Ensure the card reaches the exact end rotation
        cardObject.transform.rotation = endRotation;
    }

    public IEnumerator ChainMoveCards(List<Vector3> positions, List<GameObject> cardObject, float speed, List<Quaternion> rotations, List<Vector3> scales, bool endTurnFlag = false, bool lastMove = false, bool updateFlag = true)
    {
        yield return StartCoroutine(ChainMoveCardsCoroutine(positions, cardObject, speed, rotations, scales));
        if (lastMove) AllCardsShowcase();
        if (endTurnFlag) gameManager.TellServerTurnEnded();
        if (updateFlag) UpdateCurrentPlayerHandLayout();
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
            scales.Add(new Vector3(initialScale, initialScale, initialScale)); // Set scale for all cards

            if (piştiFlag)
            {
                if (i == cardObjects.Count - 1)
                {
                    positions[i] = new Vector3(position.x + 100 + GetChkobbaZOffset(), position.y, position.z - GetChkobbaZOffset() + 10);
                    rotations[i] = Quaternion.Euler(tempRotation.x + 90, tempRotation.y, tempRotation.z + UnityEngine.Random.Range(170f, 190f) + 90);
                    //Debug.LogWarning(poolIndex);
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
        //if(cardObjects.Count<20)yield return new WaitForSeconds(2f - (cardObjects.Count*0.1f));
        //else yield return new WaitForSeconds(0.2f);
        for (int i = 0; i < cardObjects.Count; i++)
        {
            yield return StartCoroutine(MoveCardCoroutine(positions[i], cardObjects[i], speed + (i * 0.25f), rotations[i], scales[i]));
        }
    }
    public void SetPlayerNumber(int playerNumber)
    {
        thisPlayerNumber = playerNumber;
        //Debug.LogWarning("thisPlayerNumber: " + thisPlayerNumber);
    }

    public int SendPlayerNumber()
    {
        return thisPlayerNumber;
    }

    [ContextMenu("Print ThisPlayerNumber")]
    private void PrintThisPlayerNumber()
    {
        //Debug.Log("ThisPlayerNumber: " + thisPlayerNumber);
    }

    private List<GameObject> orderedCardObjectList = new List<GameObject>();
    public void AddCardsToPlayerPool(int playerNumber)
    {
        if (playerNumber == -1) return;
        //Debug.Log("inside AddCardsToPlayerPool: " + gameManager.centerCardsObjects.Count);
        List<GameObject> cardObjects = new List<GameObject>();
        int relativePoolIndex = (playerNumber - thisPlayerNumber + playerCount) % playerCount;

        var children = new List<Transform>();
        foreach (Transform child in GameObject.Find("Center").transform) { children.Add(child); }

        foreach (Transform child in children)
        {
            Debug.LogWarning("Child: " + child.name);
            GameObject card = child.gameObject;
            cardObjects.Add(card);
            card.transform.parent = null; // Unparent the card
            card.transform.parent = playerPoolTransforms[GetPoolIndex(relativePoolIndex)];
            gameManager.centerCardsObjects.Remove(card);
            orderedCardObjectList.Add(card);
        }


        BuildPoolMoveListsAndMoveCards(playerPoolTransforms[GetPoolIndex(relativePoolIndex)].position, cardObjects, 10, relativePoolIndex, false, true);
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
            foreach (var card in cardObjects)
            {
                card.transform.parent = null; // Unparent the card
                card.transform.parent = playerPoolTransforms[GetPoolIndex(relativePoolIndex)];
                gameManager.centerCardsObjects.Remove(card);
            }
            BuildPoolMoveListsAndMoveCards(playerPoolTransforms[GetPoolIndex(relativePoolIndex)].position, cardObjects, 10, GetPoolIndex(relativePoolIndex));
        }
    }

    private int GetPoolIndex(int relativePoolIndex)
    {
        int poolIndex = 0;
        if (playerCount == 4)
        {
            poolIndex = relativePoolIndex;
        }
        else if (playerCount == 2)
        {
            if (relativePoolIndex == 0) poolIndex = 0;
            else if (relativePoolIndex == 1) poolIndex = 2;
        }

        return poolIndex;
    }

    public void GetPlayerCount(int playerC)
    {
        playerCount = playerC;
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
        Debug.LogWarning($"PeekOpponentCard called for opponent {opponentPlayerNo} at card index {cardIndex}");
        int myNo = thisPlayerNumber;
        int playerCount = this.playerCount;
        int relativeIndex = (opponentPlayerNo - myNo + playerCount) % playerCount;
        bool isMine = thisPlayerNumber == opponentPlayerNo;

        // Get the hand transform for the opponent in my perspective
        Transform handTransform = playerHandTransforms[GetPoolIndex(relativeIndex)];

        // Get the card GameObject at the specified index
        if (handTransform.childCount <= cardIndex + 2) // +2 for pool/extra children
            return;

        // Skip pool/extra children if needed
        int actualIndex = 0;
        int found = 0;
        foreach (Transform child in handTransform)
        {
            if (actualIndex > 1) // skip pool/extra
            {
                if (found == cardIndex)
                {
                    GameObject card = child.gameObject;
                    StartCoroutine(PeekCardAnimation(card,isMine));
                    break;
                }
                found++;
            }
            actualIndex++;
        }
    }

    public void PeekOpponentCardAll(int opponentPlayerNo)
    {
        int myNo = thisPlayerNumber;
        int playerCount = this.playerCount;
        int relativeIndex = (opponentPlayerNo - myNo + playerCount) % playerCount;
        bool isMine = thisPlayerNumber == opponentPlayerNo;

        // Get the hand transform for the opponent in my perspective
        Transform handTransform = playerHandTransforms[GetPoolIndex(relativeIndex)];


        // Skip pool/extra children if needed
        int actualIndex = 0;
        List<GameObject> cardsToPeek = new List<GameObject>();
        foreach (Transform child in handTransform)
        {
            if (actualIndex > 1) // skip pool/extra
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
        // Store original transform
        Vector3 originalPos = card.transform.position;
        Quaternion originalRot = card.transform.rotation;
        Vector3 originalScale = card.transform.localScale;

        // Calculate target position (midpoint between card and center, lifted above table)
        Vector3 centerPos = centerTransform.position;
        Vector3 peekPos; // 1000 units above table, adjust as needed

        // Target rotation: face up (show card front)
        Quaternion peekRot;

        // Target scale: bigger
        Vector3 peekScale;
        if (!isMine)
        {
            peekPos = originalPos + new Vector3(0, 1000, 0);
            peekScale = originalScale * 2.0f; // 2x bigger, adjust as needed
            peekRot = Quaternion.Euler(90, 0, 0); // Adjust as needed for your card orientation
        }
        else
        {
            peekPos = originalPos + new Vector3(0, 1000, 0);
            card.GetComponent<CardInteraction>().autoRotateFlag = false;
            peekScale = originalScale;
            peekRot = Quaternion.Euler(-90, 0, 0);
        }

        float moveDuration = 0.4f;
        float pauseDuration = 1.2f;
        float t = 0f;

        // Move, scale, and rotate to peek position
        while (t < moveDuration)
        {
            float lerp = t / moveDuration;
            card.transform.position = Vector3.Lerp(originalPos, peekPos, lerp);
            card.transform.rotation = Quaternion.Lerp(originalRot, peekRot, lerp);
            card.transform.localScale = Vector3.Lerp(originalScale, peekScale, lerp);
            t += Time.deltaTime;
            yield return null;
        }
        card.transform.position = peekPos;
        card.transform.rotation = peekRot;
        card.transform.localScale = peekScale;

        // Pause so player can see
        yield return new WaitForSeconds(pauseDuration);

        // Move, scale, and rotate back to original
        t = 0f;
        while (t < moveDuration)
        {
            float lerp = t / moveDuration;
            card.transform.position = Vector3.Lerp(peekPos, originalPos, lerp);
            card.transform.rotation = Quaternion.Lerp(peekRot, originalRot, lerp);
            card.transform.localScale = Vector3.Lerp(peekScale, originalScale, lerp);
            t += Time.deltaTime;
            yield return null;
        }
        card.transform.position = originalPos;
        card.transform.rotation = originalRot;
        card.transform.localScale = originalScale;
        if (isMine) card.GetComponent<CardInteraction>().autoRotateFlag = true;
    }


    private IEnumerator PeekCardAllAnimation(List<GameObject> cards, bool isMine)
    {
        // Store original transforms
        List<Vector3> originalPoss = new List<Vector3>();
        List<Quaternion> originalRots = new List<Quaternion>();
        List<Vector3> originalScales = new List<Vector3>();

        for (int i = 0; i < cards.Count; i++)
        {
            originalPoss.Add(cards[i].transform.position);
            originalRots.Add(cards[i].transform.rotation);
            originalScales.Add(cards[i].transform.localScale);
        }

        // Calculate new positions for spread-out effect
        float spread = 1000f; // Adjust for desired spacing
        float yOffset = 1200f; // How much to bring cards forward (higher Y)
        Vector3 center = Vector3.zero;
        foreach (var pos in originalPoss) center += pos;
        center /= cards.Count;

        List<Vector3> peekPoss = new List<Vector3>();
        List<Quaternion> peekRots = new List<Quaternion>();
        List<Vector3> peekScales = new List<Vector3>();

        for (int i = 0; i < cards.Count; i++)
        {
            if(isMine)cards[i].GetComponent<CardInteraction>().autoRotateFlag = false;
            // Spread cards along X axis, centered
            float offset = (i - (cards.Count - 1) / 2f) * spread;
            Vector3 peekPos;
            if (!isMine) peekPos = originalPoss[i] + new Vector3(offset, yOffset, playerCount == 2 ? -500 : 0);
            else peekPos = originalPoss[i] + new Vector3(offset, yOffset, 0);
            peekPoss.Add(peekPos);

            // Face up
            if (!isMine) peekRots.Add(Quaternion.Euler(90, 0, 0));
            else peekRots.Add(Quaternion.Euler(-90, 0, 0));
            // Scale up
            if (!isMine) peekScales.Add(originalScales[i] * 2.0f);
            else peekScales.Add(originalScales[i]);
        }

        float duration = 0.4f;
        float pause = 3;
        float t = 0f;

        // Animate to peek positions
        while (t < duration)
        {
            float lerp = t / duration;
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].transform.position = Vector3.Lerp(originalPoss[i], peekPoss[i], lerp);
                cards[i].transform.rotation = Quaternion.Lerp(originalRots[i], peekRots[i], lerp);
                cards[i].transform.localScale = Vector3.Lerp(originalScales[i], peekScales[i], lerp);
            }
            t += Time.deltaTime;
            yield return null;
        }
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].transform.position = peekPoss[i];
            cards[i].transform.rotation = peekRots[i];
            cards[i].transform.localScale = peekScales[i];
        }

        // Pause for viewing
        yield return new WaitForSeconds(pause);

        // Animate back to original
        t = 0f;
        while (t < duration)
        {
            float lerp = t / duration;
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].transform.position = Vector3.Lerp(peekPoss[i], originalPoss[i], lerp);
                cards[i].transform.rotation = Quaternion.Lerp(peekRots[i], originalRots[i], lerp);
                cards[i].transform.localScale = Vector3.Lerp(peekScales[i], originalScales[i], lerp);
            }
            t += Time.deltaTime;
            yield return null;
        }
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].transform.position = originalPoss[i];
            cards[i].transform.rotation = originalRots[i];
            cards[i].transform.localScale = originalScales[i];
            if(isMine)cards[i].GetComponent<CardInteraction>().autoRotateFlag = true;
        }
    }


    public int GetRandomHandCardIndex(int absolutePlayerNo)
    {
        // Use relative index logic to get the correct hand transform for this player from my perspective
        int myNo = thisPlayerNumber;
        int playerCount = this.playerCount;
        int relativeIndex = (absolutePlayerNo - myNo + playerCount) % playerCount;

        // Get the hand transform for the opponent in my perspective
        Transform handTransform = playerHandTransforms[GetPoolIndex(relativeIndex)];

        // Count only the actual hand cards (skip pool/extra children if needed)
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
    /// Gets the hand card GameObject at the given index, skipping pool/extra children.
    /// </summary>
    private GameObject GetHandCardByIndex(Transform handTransform, int index)
    {
        int actualIndex = 0;
        foreach (Transform child in handTransform)
        {
            if (actualIndex > 1) // skip pool/extra
            {
                if ((actualIndex - 2) == index)
                    return child.gameObject;
            }
            actualIndex++;
        }
        return null;
    }

    public void SwapHandCardWithCenterCard(string handCardID, string centerCardID, int playerNo)
    {
        // Find hand card object and center card object
        GameObject handCardObj = CardInteraction.cardLookup[handCardID].gameObject;
        GameObject centerCardObj = CardInteraction.cardLookup[centerCardID].gameObject;

        // Find the player's hand transform and the index of the hand card
        int relativeIndex = (playerNo - thisPlayerNumber + playerCount) % playerCount;
        Transform handTransform = playerHandTransforms[GetPoolIndex(relativeIndex)];
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

        // Store center card's position and rotation
        Vector3 centerPos = centerCardObj.transform.position;
        Quaternion centerRot = centerCardObj.transform.rotation;
        Vector3 handCardPos = handCardObj.transform.position;

        // Hand card goes to center: set position/rotation to center card's
        handCardObj.transform.SetParent(centerTransform, true);
        handCardObj.transform.position = centerPos;
        handCardObj.transform.rotation = centerRot;
        handCardObj.transform.localScale = new Vector3(centerScale, centerScale, centerScale);
        handCardObj.GetComponent<CardInteraction>().autoRotateFlag = false;

        // Center card goes to player's hand at the same index
        centerCardObj.transform.SetParent(handTransform, true);
        centerCardObj.GetComponent<CardInteraction>().autoRotateFlag = true;
        centerCardObj.GetComponent<CardInteraction>().OnCardTouched(Input.mousePosition);

        // Insert at the same index in the hand
        // Remove and re-insert at the correct position
        List<Transform> handChildren = new List<Transform>();
        foreach (Transform child in handTransform) handChildren.Add(child);

        handChildren.Remove(centerCardObj.transform);
        if (handCardIndex >= 0 && handCardIndex <= handChildren.Count)
            handChildren.Insert(handCardIndex, centerCardObj.transform);
        else
            handChildren.Add(centerCardObj.transform);

        // Reorder children
        for (int i = 0; i < handChildren.Count; i++)
            handChildren[i].SetSiblingIndex(i);

        // Optionally, update layout
        UpdateCurrentPlayerHandLayout();
        CardInteraction.currentlySelectedCard = null;
        GameManager.LocalInstance.SetCurrentSelectedHandCardNull();
    }

    public void SwapCardsBetweenPlayersByID(int playerANo, string cardAID, int playerBNo, string cardBID)
    {
        int relA = (playerANo - thisPlayerNumber + playerCount) % playerCount;
        int relB = (playerBNo - thisPlayerNumber + playerCount) % playerCount;

        Transform handA = playerHandTransforms[GetPoolIndex(relA)];
        Transform handB = playerHandTransforms[GetPoolIndex(relB)];

        GameObject cardAObj = CardInteraction.cardLookup[cardAID].gameObject;
        GameObject cardBObj = CardInteraction.cardLookup[cardBID].gameObject;
        cardAObj.transform.localScale = new Vector3(centerScale, centerScale, centerScale);

        Vector3 cardARotation = cardAObj.transform.rotation.eulerAngles;
        cardBObj.transform.rotation = Quaternion.Euler(cardARotation.x, cardARotation.y, cardARotation.z);

        // Store world positions before changing parents
        Vector3 cardAOldPos = cardAObj.transform.position;
        Vector3 cardBOldPos = cardBObj.transform.position;

        // Find indexes
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

        // Swap parents
        cardAObj.transform.SetParent(handB, true);
        cardBObj.transform.SetParent(handA, true);

        // --- Set positions to old world positions so animation is visible ---
        cardAObj.transform.position = cardAOldPos;
        cardBObj.transform.position = cardBOldPos;

        // Set autoRotateFlag and call OnCardTouched for the new card in my hand
        if (thisPlayerNumber == playerANo)
        {
            cardBObj.GetComponent<CardInteraction>().autoRotateFlag = true;
            cardBObj.GetComponent<CardInteraction>().OnCardTouched(Input.mousePosition);
            cardAObj.GetComponent<CardInteraction>().autoRotateFlag = false;
        }
        else if (thisPlayerNumber == playerBNo)
        {
            cardAObj.GetComponent<CardInteraction>().autoRotateFlag = true;
            cardAObj.GetComponent<CardInteraction>().OnCardTouched(Input.mousePosition);
            cardBObj.GetComponent<CardInteraction>().autoRotateFlag = false;
        }

        // Swap sibling indexes to preserve hand order
        if (idxA != -1) cardBObj.transform.SetSiblingIndex(idxA);
        if (idxB != -1) cardAObj.transform.SetSiblingIndex(idxB);

        // Now, when UpdateCurrentPlayerHandLayout is called, the cards will animate from their old positions
        UpdateCurrentPlayerHandLayout();
        CardInteraction.currentlySelectedCard = null;
        GameManager.LocalInstance.SetCurrentSelectedHandCardNull();
    }


    //DeğişTokuş
    public List<string> GetOpponentHandCardIDs(int absolutePlayerNo)
    {
        int myNo = thisPlayerNumber;
        int playerCount = this.playerCount;
        int relativeIndex = (absolutePlayerNo - myNo + playerCount) % playerCount;
        Transform handTransform = playerHandTransforms[GetPoolIndex(relativeIndex)];
        List<string> handCardIDs = new List<string>();
        int actualIndex = 0;
        foreach (Transform child in handTransform)
        {
            if (actualIndex > 1) // skip pool/extra
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

    public IEnumerator SwapCardsAndUpdateLayoutCoroutine(int playerANo, string cardAID, int playerBNo, string cardBID, int handIndexA)
    {
        int relA = (playerANo - thisPlayerNumber + playerCount) % playerCount;
        int relB = (playerBNo - thisPlayerNumber + playerCount) % playerCount;

        Transform handA = playerHandTransforms[GetPoolIndex(relA)];
        Transform handB = playerHandTransforms[GetPoolIndex(relB)];

        GameObject cardAObj = CardInteraction.cardLookup[cardAID].gameObject;
        GameObject cardBObj = CardInteraction.cardLookup[cardBID].gameObject;
        cardAObj.transform.localScale = new Vector3(centerScale, centerScale, centerScale);

        Vector3 cardARotation = cardAObj.transform.rotation.eulerAngles;
        cardBObj.transform.rotation = Quaternion.Euler(cardARotation.x, cardARotation.y, cardARotation.z);
        //cardBObj.transform.position = cardAObj.transform.position;

        // Store world positions before changing parents
        Vector3 cardAOldPos = cardAObj.transform.position;
        Vector3 cardBOldPos = cardBObj.transform.position;

        // Find index of cardB in handB (skipping first 2 children)
        int idxB = -1, i = 0, actualIdxB = 0;
        foreach (Transform child in handB)
        {
            if (i > 1) // skip first 2
            {
                if (child.gameObject == cardBObj) { idxB = actualIdxB; break; }
                actualIdxB++;
            }
            i++;
        }

        // Set parents
        cardAObj.transform.SetParent(handB, true);
        cardBObj.transform.SetParent(handA, true);

        // Set positions to old world positions so animation is visible
        cardAObj.transform.position = cardAOldPos;
        cardBObj.transform.position = cardBOldPos;

        // Set autoRotateFlag and call OnCardTouched for the new card in my hand
        if (thisPlayerNumber == playerANo)
        {
            cardBObj.GetComponent<CardInteraction>().autoRotateFlag = true;
            // Suppress input for this frame to prevent accidental play
            cardBObj.GetComponent<CardInteraction>().OnCardTouched(Input.mousePosition);
            cardAObj.GetComponent<CardInteraction>().autoRotateFlag = false;
        }
        else if (thisPlayerNumber == playerBNo)
        {
            cardAObj.GetComponent<CardInteraction>().autoRotateFlag = true;
            cardAObj.GetComponent<CardInteraction>().OnCardTouched(Input.mousePosition);
            cardBObj.GetComponent<CardInteraction>().autoRotateFlag = false;
        }

        // --- Insert cardBObj at the correct index in handA (skipping first 2 children) ---
        List<Transform> handAChildren = new List<Transform>();
        int idx = 0;
        foreach (Transform child in handA)
        {
            if (idx > 1) handAChildren.Add(child);
            idx++;
        }
        handAChildren.Remove(cardBObj.transform);
        if (handIndexA >= 0 && handIndexA <= handAChildren.Count)
            handAChildren.Insert(handIndexA, cardBObj.transform);
        else
            handAChildren.Add(cardBObj.transform);

        // Reorder only the card objects (after the first 2 children)
        idx = 0;
        foreach (Transform child in handA)
        {
            if (idx > 1)
            {
                handAChildren[idx - 2].SetSiblingIndex(idx);
            }
            idx++;
        }

        // --- Insert cardAObj at the correct index in handB (skipping first 2 children) ---
        if (idxB != -1)
        {
            List<Transform> handBChildren = new List<Transform>();
            int idx2 = 0;
            foreach (Transform child in handB)
            {
                if (idx2 > 1) handBChildren.Add(child);
                idx2++;
            }
            handBChildren.Remove(cardAObj.transform);
            if (idxB >= 0 && idxB <= handBChildren.Count)
                handBChildren.Insert(idxB, cardAObj.transform);
            else
                handBChildren.Add(cardAObj.transform);

            // Reorder only the card objects (after the first 2 children)
            idx2 = 0;
            foreach (Transform child in handB)
            {
                if (idx2 > 1)
                {
                    handBChildren[idx2 - 2].SetSiblingIndex(idx2);
                }
                idx2++;
            }
        }

        float moveDuration = 0.5f;
        MoveCard(cardAObj.transform.position, cardBObj, moveDuration * 10, cardAObj.transform.rotation, cardAObj.transform.localScale, false);
        //yield return new WaitForSeconds(moveDuration);

        MoveCard(cardBObj.transform.position, cardAObj, moveDuration * 10, cardBObj.transform.rotation, cardBObj.transform.localScale, false);
        yield return new WaitForSeconds(moveDuration);

        // Now update the layout so both cards are included in the correct hands
        UpdateCurrentPlayerHandLayout();

        // If in showcase mode, also update the showcase originals for both cards
        UpdateShowcaseOriginalsAfterSwap(cardAObj);
        UpdateShowcaseOriginalsAfterSwap(cardBObj);

        if (!GameManager.LocalInstance.isSunuDegisBunuTokusActive) ExitShowcaseAllOtherHands();

        CardInteraction.currentlySelectedCard = null;
        GameManager.LocalInstance.SetCurrentSelectedHandCardNull();
        //asflalkf
    }


    private bool isShowcaseAllActive = false;
    private Dictionary<GameObject, (Vector3 pos, Quaternion rot, Vector3 scale, bool autoRotateFlag)> showcaseOriginalTransforms = new Dictionary<GameObject, (Vector3, Quaternion, Vector3, bool)>();

    [ContextMenu("ExitShowcaseAllOtherHands")]
    public void ExitShowcaseAllOtherHands()
    {
        isShowcaseAllActive = false;
        // Restore all cards to their stored transforms and flags
        foreach (var kvp in showcaseOriginalTransforms)
        {
            GameObject card = kvp.Key;
            var (pos, rot, scale, autoRotateFlag) = kvp.Value;
            MoveCard(pos, card, 10, rot, scale, false);

            var ci = card.GetComponent<CardInteraction>();
            if (ci != null)
            {
                ci.autoRotateFlag = autoRotateFlag;
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
            bool autoRotate = ci != null ? ci.autoRotateFlag : false;
            showcaseOriginalTransforms[card] = (card.transform.position, card.transform.rotation, card.transform.localScale, autoRotate);
        }
    }

    /// <summary>
    /// Assigns card GameObjects to player pools based on a dictionary of playerNo -> List of card IDs.
    /// </summary>
    public void AssignCardsToPlayerPools(SerializableDictionary playersPooledCardsIDsSerialized)
    {
        Dictionary<int, List<string>> playersPooledCardsIDs = playersPooledCardsIDsSerialized.ToDictionary();
        if (playerPoolTransforms == null || playerPoolTransforms.Count == 0)
        {
            Debug.LogError("playerPoolTransforms not set!");
            return;
        }

        foreach (var kvp in playersPooledCardsIDs)
        {
            int playerNo = kvp.Key;
            List<string> cardIDs = kvp.Value;

            // Get the correct pool transform for this player
            int poolIndex = GetPoolIndex(playerNo); // Use your existing logic for 2v2/1v1
            Transform poolTransform = playerPoolTransforms[poolIndex];

            foreach (string cardID in cardIDs)
            {
                if (CardInteraction.cardLookup.TryGetValue(cardID, out var cardInteraction))
                {
                    GameObject cardObj = cardInteraction.gameObject;
                    cardObj.transform.SetParent(poolTransform, false);
                    // Optionally, reset position/rotation/scale here if needed
                }
                else
                {
                    Debug.LogWarning($"CardInteraction.cardLookup does not contain cardID: {cardID}");
                }
            }
        }
    }

    public void AssignCardsToPlayerHands(Dictionary<int, List<string>> playersHandCardsIDs)
    {
        if (playerHandTransforms == null || playerHandTransforms.Count == 0)
        {
            Debug.LogError("playerHandTransforms not set!");
            return;
        }

        foreach (var kvp in playersHandCardsIDs)
        {
            int playerNo = kvp.Key;
            List<string> cardIDs = kvp.Value;

            int handIndex = GetPoolIndex(playerNo); // Use your existing logic
            Transform handTransform = playerHandTransforms[handIndex];

            foreach (string cardID in cardIDs)
            {
                if (CardInteraction.cardLookup.TryGetValue(cardID, out var cardInteraction))
                {
                    GameObject cardObj = cardInteraction.gameObject;
                    cardObj.transform.SetParent(handTransform, false);
                }
                else
                {
                    Debug.LogWarning($"CardInteraction.cardLookup does not contain cardID: {cardID}");
                }
            }
        }
    }



}

