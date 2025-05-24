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
    [SerializeField]private GameManager gameManager;
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
    int offset=150; 
    int offsetCounter0=-1;
    int offsetCounter1=-1;
    int offsetCounter2=-1;
    private int zOffsetCounter=0;
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
                var cardIDString = cardPrefabEntry.Key;
                var cardPrefab = cardPrefabEntry.Value;


                GameObject card = Instantiate(cardPrefab);
                card.SetActive(true); // Deactivate the card
                card.transform.rotation = Quaternion.Euler(deckTransform.rotation.x, deckTransform.rotation.y, deckTransform.rotation.z);


                // Get the CardInteraction component and add it to the list
                CardInteraction cardInteraction = card.GetComponent<CardInteraction>();
                if (cardInteraction != null)
                {
                    cardInteractionList.Add(cardInteraction); // Store the reference in the list
                }

                card.transform.position = new Vector3(deckTransform.transform.position.x, deckTransform.transform.position.y + (counter * 2), deckTransform.transform.position.z);
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
        List<Quaternion> rotations = Enumerable.Repeat(Quaternion.Euler(deckTransform.rotation.x-90, deckTransform.rotation.y, deckTransform.rotation.z), 52).ToList();
        List<Vector3> scales = Enumerable.Repeat(new Vector3(1000, 1000, 1000), 52).ToList();

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
    public void DealPlayers(int playerCount, Dictionary<int, List<int[]>> playerHands)
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

    private void DealTwoPlayers(Dictionary<int, List<int[]>> playerHands)
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

            if (relativeIndex == 0) gameManager.myCards = new List<int[]>();

            for (int j = 0; j < 4; j++)
            {
                var cardID = playerHands[i][j];
                if (relativeIndex == 0) gameManager.myCards.Add(cardID);
                //if (relativeIndex == 1) relativeIndex=2;

                GameObject tempCardObject = GetCardFromPool(cardID);
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
                        rotation = Quaternion.Euler(centerRotation.x,centerRotation.y + (randomOffset1*3),centerRotation.z);
                        //scales.Add(new Vector3(800, 800, 800));
                        break;
                    case 1: // Top (Player 2)
                        offset = new Vector3(randomOffset1 * randomOffset2, j * 5, randomOffset1 * randomOffset2);
                        rotation = Quaternion.Euler(centerRotation.x,centerRotation.y + (randomOffset1*3),centerRotation.z);
                        //scales.Add(new Vector3(600, 600, 600));
                        break;
                }

                positions.Add(basePos + offset);
                rotations.Add(rotation);
                scales.Add(new Vector3(1000, 1000, 1000));
            }
        }

        StartCoroutine(ChainMoveCards(positions, cardObjects, 10, rotations, scales));
    }

    private void DealFourPlayers(Dictionary<int, List<int[]>> playerHands)
    {
        var cardObjects = new List<GameObject>();
        var positions = new List<Vector3>();
        var rotations = new List<Quaternion>();
        var scales = new List<Vector3>(); // List to store scales

        for (int n = 0; n < 4; n++)
        {
            int i = (startingPlayerNoCounter + n) % 4;
            relativeIndex = (i - thisPlayerNumber + playerCount) % playerCount;
            if (relativeIndex == 0) gameManager.myCards = new List<int[]>();

            for (int j = 0; j < 4; j++)
            {
                var cardID = playerHands[i][j];
                if (relativeIndex == 0) gameManager.myCards.Add(cardID);

                GameObject tempCardObject = GetCardFromPool(cardID);
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
                        rotation = Quaternion.Euler(centerRotation.x,centerRotation.y + (randomOffset1*3),centerRotation.z);
                        //scales.Add(new Vector3(800, 800, 800));
                        break;
                    case 1: // Right (Player 1)
                        offset = new Vector3(randomOffset1 * randomOffset2, j * 5, randomOffset1 * randomOffset2);
                        rotation = Quaternion.Euler(centerRotation.x,centerRotation.y + 90 + (randomOffset1*3),centerRotation.z);
                        //scales.Add(new Vector3(700, 700, 700));
                        break;
                    case 2: // Top (Player 2)
                        offset = new Vector3(randomOffset1 * randomOffset2, j * 5, randomOffset1 * randomOffset2);
                        rotation = Quaternion.Euler(centerRotation.x,centerRotation.y + (randomOffset1*3),centerRotation.z);
                        //scales.Add(new Vector3(600, 600, 600));
                        break;
                    case 3: // Left (Player 3)
                        offset = new Vector3(randomOffset1 * randomOffset2, j * 5, randomOffset1 * randomOffset2);
                        rotation = Quaternion.Euler(centerRotation.x,centerRotation.y + 90 + (randomOffset1*3),centerRotation.z);
                        //scales.Add(new Vector3(700, 700, 700));
                        break;
                }

                positions.Add(basePos + offset);
                rotations.Add(rotation);
                scales.Add(new Vector3(1000, 1000, 1000));
            }
        }

        StartCoroutine(ChainMoveCards(positions, cardObjects, 10, rotations, scales));
    }

    private int startingPlayerNoCounter = -1;
    //Deals to center according to the playerCount
    public void DealCenter(List<int[]> centerCardIDs)
    {
        startingPlayerNoCounter++;
        SendCardInteractionsToGameManager();
        List<GameObject> cardObjects = new List<GameObject>();
        List<Vector3> positions = new List<Vector3>();
        List<Quaternion> rotations = new List<Quaternion>();
        List<Vector3> scales = new List<Vector3>();

        for (int i = 0; i < 4; i++)
        {
            var cardID = centerCardIDs[i];
            GameObject tempCenterCard = GetCardFromPool(cardID);
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
                gameManager.centerCards.Add(cardID);

                if (i == 3)
                {
                    rotations.Add(Quaternion.Euler(centerRotation.x+180, centerRotation.y, UnityEngine.Random.Range(-12, 12)));
                    positions.Add(new Vector3(centerPosition.x, centerPosition.y+10, centerPosition.z));
                }
                else
                {
                    rotations.Add(Quaternion.Euler(centerRotation.x, centerRotation.y, UnityEngine.Random.Range(-12, 12)));
                    positions.Add(new Vector3(centerPosition.x, centerPosition.y, centerPosition.z));
                }
                scales.Add(new Vector3(1200, 1200, 1200));
            }
        }

        StartCoroutine(ChainMoveCards(positions, cardObjects, 10, rotations, scales));
    }
    
    private void SendCardInteractionsToGameManager()
    {
        gameManager.GetCardInteractionScripts(cardInteractionList);
    }

    //Allow to get card object with the cardID
    private GameObject GetCardFromPool(int[] cardID)
    {
        string cardIDString = GameManager.TurnCardIdToString(cardID);
        //print("cardIDString: " + cardIDString);

        if (deckPool.ContainsKey(cardIDString) && deckPool[cardIDString] != null)
        {
            var card = deckPool[cardIDString];
            //deckPool[cardIDString] = null;
            card.SetActive(true);
            activeCards.Add(card);
            return card;
        }

        //Debug.LogError($"No cards available in the pool for ID: {cardIDString}");
        return null;
    }

    public void DiscardHandCardToCenter(int[] cardID)
    {
        GameObject placedCard = GameObject.FindGameObjectWithTag(GameManager.TurnCardIdToString(cardID));
        List<Vector3> positions = new List<Vector3>();
        List<GameObject> cardObjects = new List<GameObject>();
        List<Quaternion> rotations = new List<Quaternion>();
        List<Vector3> scales = new List<Vector3>();

        if (placedCard != null)
        {
            Vector3 centerPosition = centerTransform.position;
            Vector3 centerRotation = centerTransform.rotation.eulerAngles;

            rotations.Add(Quaternion.Euler(centerRotation.x+180, centerRotation.y, UnityEngine.Random.Range(-12, 12)));
            placedCard.transform.parent = centerTransform;

            // Use centerTransform for positioning
            positions.Add(new Vector3(centerPosition.x, centerPosition.y + 10 * GameManager.LocalInstance.centerCardsObjects.Count, centerPosition.z));
            scales.Add(new Vector3(1200,1200,1200));

            gameManager.centerCards.Add(cardID);
            gameManager.centerCardsObjects.Add(placedCard);
            cardObjects.Add(placedCard);
        }

        AudioManager.Instance.PlayAudio(3, 1, false);
        StartCoroutine(ChainMoveCards(positions, cardObjects, 10, rotations, scales, true));
        //UpdateCurrentPlayerHandLayout();
    }

    public void UpdateCurrentPlayerHandLayout()
    {
        if (playerCount == 2)
        {
            for(int i = 0; i < 2; i++)
            {
                UpdateCurrentPlayerHandLayoutTwoPlayers(i);
            }
        }
        else if (playerCount == 4)
        {
            for(int i = 0; i < 4; i++)
            {
                UpdateCurrentPlayerHandLayoutFourPlayers(i);
            }
        }
    }

    private void UpdateCurrentPlayerHandLayoutTwoPlayers(int playerNumber = -1)
    {
        if(playerNumber == 1) playerNumber = 2;

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


        if(currentPlayerHand.childCount == 1)
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
                    offset = new Vector3(spacing * 15f * (i - offsetMult), 1000, 100);
                    rotation = Quaternion.Euler(-centerRotation.x,centerRotation.y,centerRotation.z);
                    currentScale = new Vector3(1250, 1250, 1250);
                    break;
                case 2: // Top (Player 2)
                    offset = new Vector3(spacing * 3f * (i - offsetMult), 1000+(i*10), 0);
                    rotation = Quaternion.Euler(-centerRotation.x,centerRotation.y,centerRotation.z);
                    break;
            }

            Vector3 targetPosition = playerHandTransforms[playerNumber].position + offset;
            MoveCard(targetPosition, playerCards[i], 10, rotation, currentScale);
        }
        if(playerNumber == 0) StartCoroutine(SetAutoRotateFlagTrue(playerCards));
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


        if(currentPlayerHand.childCount == 1)
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
                    rotation = Quaternion.Euler(-centerRotation.x,centerRotation.y,centerRotation.z);
                    currentScale = new Vector3(1250, 1250, 1250);
                    break;
                case 1: // Right (Player 1)
                    offset = new Vector3(0, i*10, spacing * 3f * (i - offsetMult));
                    rotation = Quaternion.Euler(-centerRotation.x,centerRotation.y+90,centerRotation.z);
                    break;
                case 2: // Top (Player 2)
                    offset = new Vector3(spacing * 3f * (i - offsetMult), i*10, 0);
                    rotation = Quaternion.Euler(-centerRotation.x,centerRotation.y,centerRotation.z);
                    break;
                case 3: // Left (Player 3)
                    offset = new Vector3(0, i*10, spacing * 3f * (i - offsetMult));
                    rotation = Quaternion.Euler(-centerRotation.x,centerRotation.y+90,centerRotation.z);
                    break;
            }

            Vector3 targetPosition = playerHandTransforms[playerNumber].position + offset;
            MoveCard(targetPosition, playerCards[i], 10, rotation, currentScale);
        }
        if(playerNumber == 0) StartCoroutine(SetAutoRotateFlagTrue(playerCards));
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
                Vector3 targetPosition = new Vector3(spacing * (i - offsetMult), (i*10)+centerTransform.position.y+5000, baseZ);
                Vector3 targetScale = new Vector3(1250, 1250, 1250);
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
                    if(j == 1)rotation = Quaternion.Euler(90, 0, 0);
                    else if(j == 0)rotation = Quaternion.Euler(90, 90, 0);
                    Vector3 targetPosition = new Vector3(
                        pairSpacing * (pair - offsetMult) + (j - 10f) * cardSpacingInPair,
                        (j*10)+centerTransform.position.y+5000,
                        baseZ
                    );
                    Vector3 targetScale = new Vector3(1250, 1250, 1250);
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

    [ContextMenu("Showcase All Cards")]
    public void AllCardsShowcase()
    {
        if (playerCount == 2)
        {
            ShowcasePools1v1();
        }
        else if (playerCount == 4)
        {
            ShowcasePools2v2();
        }
    }

    private void ShowcasePools1v1()
    {
        int cardsPerRow = 7;
        float cardSpacing = 420f;
        float rowSpacing = 1250f;
        float cardScale = 1250f;
        float piştiGap = 1.5f * rowSpacing;

        // Get screen positions for left (my side) and right (opponent)
        Vector3 leftScreen = new Vector3(Screen.width * 0.25f, Screen.height * 0.75f, 3000f);
        Vector3 rightScreen = new Vector3(Screen.width * 0.75f, Screen.height * 0.75f, 3000f);

        Vector3 leftWorld = Camera.main.ScreenToWorldPoint(leftScreen);
        Vector3 rightWorld = Camera.main.ScreenToWorldPoint(rightScreen);

        // Determine my and opponent indices
        int myPoolIndex = GetPoolIndex(0);
        int oppPoolIndex = GetPoolIndex(1);

        // --- My side ---
        List<GameObject> myPiştiCards = new List<GameObject>();
        foreach (Transform t in playerPiştiPoolTransforms[myPoolIndex]) myPiştiCards.Add(t.gameObject);

        List<GameObject> myPoolCards = new List<GameObject>();
        foreach (Transform t in playerPoolTransforms[myPoolIndex]) myPoolCards.Add(t.gameObject);

        // Layout my pişti cards (top)
        int myPiştiRows = Mathf.CeilToInt(myPiştiCards.Count / (float)cardsPerRow);
        if (myPiştiCards.Count > 0)
        {
            for (int i = 0; i < myPiştiCards.Count; i++)
            {
                int row = i / cardsPerRow;
                int col = i % cardsPerRow;
                Vector3 pos = leftWorld + new Vector3((col - (cardsPerRow-1)/2f) * cardSpacing, 100 + i, -row * rowSpacing);
                MoveCard(pos, myPiştiCards[i], 10, Quaternion.Euler(90, 0, 0), new Vector3(cardScale, cardScale, cardScale), false);
            }
        }

        // Layout my pool cards (below pişti if present, else at top)
        float myPoolZOffset = (myPiştiCards.Count > 0) ? (-myPiştiRows * rowSpacing - piştiGap) : 0f;
        for (int i = 0; i < myPoolCards.Count; i++)
        {
            int row = i / cardsPerRow;
            int col = i % cardsPerRow;
            Vector3 pos = leftWorld + new Vector3(
                (col - (cardsPerRow-1)/2f) * cardSpacing,
                100 + i,
                myPoolZOffset - row * rowSpacing
            );
            MoveCard(pos, myPoolCards[i], 10, Quaternion.Euler(90, 0, 0), new Vector3(cardScale, cardScale, cardScale), false);
        }

        // --- Opponent side ---
        List<GameObject> oppPiştiCards = new List<GameObject>();
        foreach (Transform t in playerPiştiPoolTransforms[oppPoolIndex]) oppPiştiCards.Add(t.gameObject);

        List<GameObject> oppPoolCards = new List<GameObject>();
        foreach (Transform t in playerPoolTransforms[oppPoolIndex]) oppPoolCards.Add(t.gameObject);

        int oppPiştiRows = Mathf.CeilToInt(oppPiştiCards.Count / (float)cardsPerRow);
        if (oppPiştiCards.Count > 0)
        {
            for (int i = 0; i < oppPiştiCards.Count; i++)
            {
                int row = i / cardsPerRow;
                int col = i % cardsPerRow;
                Vector3 pos = rightWorld + new Vector3((col - (cardsPerRow-1)/2f) * cardSpacing, 100 + i, -row * rowSpacing);
                MoveCard(pos, oppPiştiCards[i], 10, Quaternion.Euler(90, 0, 0), new Vector3(cardScale, cardScale, cardScale), false);
            }
        }

        float oppPoolZOffset = (oppPiştiCards.Count > 0) ? (-oppPiştiRows * rowSpacing - piştiGap) : 0f;
        for (int i = 0; i < oppPoolCards.Count; i++)
        {
            int row = i / cardsPerRow;
            int col = i % cardsPerRow;
            Vector3 pos = rightWorld + new Vector3(
                (col - (cardsPerRow-1)/2f) * cardSpacing,
                100 + i,
                oppPoolZOffset - row * rowSpacing
            );
            MoveCard(pos, oppPoolCards[i], 10, Quaternion.Euler(90, 0, 0), new Vector3(cardScale, cardScale, cardScale), false);
        }
    }

    private void ShowcasePools2v2()
    {
        int cardsPerRow = 7;
        float cardSpacing = 420f;
        float rowSpacing = 1250f;
        float cardScale = 1250f;
        float piştiGap = 1.5f * rowSpacing;

        // Get screen positions for left (my team) and right (opponent team)
        Vector3 leftScreen = new Vector3(Screen.width * 0.25f, Screen.height * 0.75f, 3000f);
        Vector3 rightScreen = new Vector3(Screen.width * 0.75f, Screen.height * 0.75f, 3000f);

        Vector3 leftWorld = Camera.main.ScreenToWorldPoint(leftScreen);
        Vector3 rightWorld = Camera.main.ScreenToWorldPoint(rightScreen);

        // My team: player 0 and 2
        List<GameObject> myTeamPiştiCards = new List<GameObject>();
        List<GameObject> myTeamPoolCards = new List<GameObject>();
        foreach (int idx in new int[] { GetPoolIndex(0), GetPoolIndex(2) })
        {
            foreach (Transform t in playerPiştiPoolTransforms[idx]) myTeamPiştiCards.Add(t.gameObject);
            foreach (Transform t in playerPoolTransforms[idx]) myTeamPoolCards.Add(t.gameObject);
        }

        // Opponent team: player 1 and 3
        List<GameObject> oppTeamPiştiCards = new List<GameObject>();
        List<GameObject> oppTeamPoolCards = new List<GameObject>();
        foreach (int idx in new int[] { GetPoolIndex(1), GetPoolIndex(3) })
        {
            foreach (Transform t in playerPiştiPoolTransforms[idx]) oppTeamPiştiCards.Add(t.gameObject);
            foreach (Transform t in playerPoolTransforms[idx]) oppTeamPoolCards.Add(t.gameObject);
        }

        // Layout my team pişti cards (top)
        int myTeamPiştiRows = Mathf.CeilToInt(myTeamPiştiCards.Count / (float)cardsPerRow);
        if (myTeamPiştiCards.Count > 0)
        {
            for (int i = 0; i < myTeamPiştiCards.Count; i++)
            {
                int row = i / cardsPerRow;
                int col = i % cardsPerRow;
                Vector3 pos = leftWorld + new Vector3((col - (cardsPerRow-1)/2f) * cardSpacing, 100 + i, -row * rowSpacing);
                MoveCard(pos, myTeamPiştiCards[i], 10, Quaternion.Euler(90, 0, 0), new Vector3(cardScale, cardScale, cardScale), false);
            }
        }

        // Layout my team pool cards (below pişti if present, else at top)
        float myTeamPoolZOffset = (myTeamPiştiCards.Count > 0) ? (-myTeamPiştiRows * rowSpacing - piştiGap) : 0f;
        for (int i = 0; i < myTeamPoolCards.Count; i++)
        {
            int row = i / cardsPerRow;
            int col = i % cardsPerRow;
            Vector3 pos = leftWorld + new Vector3(
                (col - (cardsPerRow-1)/2f) * cardSpacing,
                100 + i,
                myTeamPoolZOffset - row * rowSpacing
            );
            MoveCard(pos, myTeamPoolCards[i], 10, Quaternion.Euler(90, 0, 0), new Vector3(cardScale, cardScale, cardScale), false);
        }

        // Layout opponent team pişti cards (top)
        int oppTeamPiştiRows = Mathf.CeilToInt(oppTeamPiştiCards.Count / (float)cardsPerRow);
        if (oppTeamPiştiCards.Count > 0)
        {
            for (int i = 0; i < oppTeamPiştiCards.Count; i++)
            {
                int row = i / cardsPerRow;
                int col = i % cardsPerRow;
                Vector3 pos = rightWorld + new Vector3((col - (cardsPerRow-1)/2f) * cardSpacing, 100 + i, -row * rowSpacing);
                MoveCard(pos, oppTeamPiştiCards[i], 10, Quaternion.Euler(90, 0, 0), new Vector3(cardScale, cardScale, cardScale), false);
            }
        }

        // Layout opponent team pool cards (below pişti if present, else at top)
        float oppTeamPoolZOffset = (oppTeamPiştiCards.Count > 0) ? (-oppTeamPiştiRows * rowSpacing - piştiGap) : 0f;
        for (int i = 0; i < oppTeamPoolCards.Count; i++)
        {
            int row = i / cardsPerRow;
            int col = i % cardsPerRow;
            Vector3 pos = rightWorld + new Vector3(
                (col - (cardsPerRow-1)/2f) * cardSpacing,
                100 + i,
                oppTeamPoolZOffset - row * rowSpacing
            );
            MoveCard(pos, oppTeamPoolCards[i], 10, Quaternion.Euler(90, 0, 0), new Vector3(cardScale, cardScale, cardScale), false);
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


    public void RotateCard(Quaternion endRotation, GameObject cardObject, float speed=20)
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

    public IEnumerator ChainMoveCards(List<Vector3> positions, List<GameObject> cardObject, float speed, List<Quaternion> rotations, List<Vector3> scales,bool endTurnFlag=false, bool lastMove=false)
    {
        yield return StartCoroutine(ChainMoveCardsCoroutine(positions, cardObject, speed, rotations, scales));
        if(lastMove)AllCardsShowcase();
        if (endTurnFlag) gameManager.TellServerTurnEnded();
    }
    
    public void BuildPoolMoveListsAndMoveCards(Vector3 position, List<GameObject> cardObjects, float speed, int poolIndex, bool piştiFlag = false, bool lastMove=false)
    {
        List<Vector3> positions = new List<Vector3>();
        List<Quaternion> rotations = new List<Quaternion>();
        List<Vector3> scales = new List<Vector3>();
        for (int i = 0; i < cardObjects.Count; i++)
        {
            Vector3 tempRotation = playerPoolTransforms[poolIndex].transform.rotation.eulerAngles;
            positions.Add(position);
            Quaternion rotation = Quaternion.Euler(tempRotation.x-90, tempRotation.y, tempRotation.z+UnityEngine.Random.Range(170f, 190f));
            rotations.Add(rotation);
            scales.Add(new Vector3(1000, 1000, 1000)); // Set scale for all cards

            if (piştiFlag)
            {
                if (i == cardObjects.Count - 1)
                {
                    positions[i] = new Vector3(position.x + 100 + GetChkobbaZOffset(), position.y, position.z - GetChkobbaZOffset() + 10);
                    rotations[i] = Quaternion.Euler(tempRotation.x+90, tempRotation.y, tempRotation.z+UnityEngine.Random.Range(170f, 190f)+90);
                    //Debug.LogWarning(poolIndex);
                }
                StartCoroutine(ChkobbaCoroutine());
            }
        }

        StartCoroutine(ChainMoveCards(positions, cardObjects, speed, rotations, scales, true,lastMove));
    }

    private IEnumerator ChkobbaCoroutine()
    {
        //chkobbaText.SetActive(true);

        yield return new WaitForSeconds(2);

        //chkobbaText.SetActive(false);
    }

    private int GetChkobbaYOffset(int poolIndex)
    {
        if(poolIndex==0)
        {
            offsetCounter0++;
            return (offset*offsetCounter0)-200;

        }
        else if(poolIndex==1)
        {
            offsetCounter1++;
            return -(offset*offsetCounter1)+200;
        }
        else if(poolIndex==2)
        {
            offsetCounter2++;
            return -(offset*offsetCounter2)+200;
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
        for(int i = 0; i < cardObjects.Count; i++)
        {
            yield return StartCoroutine(MoveCardCoroutine(positions[i], cardObjects[i], speed + (i*0.25f), rotations[i],scales[i]));
        }
        UpdateCurrentPlayerHandLayout();
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


        BuildPoolMoveListsAndMoveCards(playerPoolTransforms[GetPoolIndex(relativePoolIndex)].position, cardObjects, 10, relativePoolIndex,false,true);
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
            BuildPoolMoveListsAndMoveCards(playerPiştiPoolTransforms[GetPoolIndex(relativePoolIndex)].position, cardObjects, 10, GetPoolIndex(relativePoolIndex),piştiFlag);
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
        int poolIndex=0;
        if(playerCount==4)
        {
            poolIndex=relativePoolIndex;
        }
        else if(playerCount==2)
        {
            if(relativePoolIndex==0)poolIndex=0;
            else if(relativePoolIndex==1)poolIndex=2;
        }

        return poolIndex;
    }

    public void GetPlayerCount(int playerC)
    {
        playerCount=playerC;
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
}

