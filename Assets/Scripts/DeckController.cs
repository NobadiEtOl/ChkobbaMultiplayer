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
    [SerializeField] private GameObject addCardPrefab;//Prefab of the add button
    [SerializeField] private List<GameObject> cardPrefabsList;//Prefabs of all the cards.
    private Dictionary<string, GameObject> cardPrefabs;//A dictionary to keep track of each card prefabs with its ID
    private Dictionary<string, GameObject> deckPool;//A dictionary of card ID and a list of all the instantiated cards
    private List<Transform> cardPoolTransforms = new List<Transform>();
    private List<GameObject> activeCards = new List<GameObject>();
    private List<CardInteraction> cardInteractionList; // List to store CardInteraction references
    private GameObject[] playerParentHands = new GameObject[4];//Array of player hand objects to keep track of where the card objects will be placed
    private List<Vector3> playerPools = new List<Vector3>();
    private int relativeIndex = 0;
    public int playerCount = 0;
    //private GameObject cardPool;
    private GameObject chkobbaText;

    void Start()
    {   
        if(LocalInstance == null)
        {
            LocalInstance = this;
        }
        else 
        {
            Debug.LogWarning("Duplicate GameManager detected. Destroying extra instance.");
            //Destroy(gameObject);
        }

        InitialDeckSetUp();
        
    }

    //Called when the deck is ready to start
    public void DeckStart()
    {
        DefineCardPrefabs();
        InitializeCardPool();
    }

    public void DefineCardPrefabs()
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
                Debug.LogWarning($"Card prefab with ID {cardIDString} already exists. Skipping...");
            }
        }
    }


    //Initialize and instantiate all the card objects that can be used
    private List<GameObject> cardObjectList = new List<GameObject>();
    private void InitializeCardPool()
    {
        if(deckPool.Count==0)
        {
            cardInteractionList = new List<CardInteraction>();
            Transform deckTransform = GameObject.Find("DeckTransform").transform;

            int counter=0;
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
                
                card.transform.position = new Vector3(deckTransform.transform.position.x, deckTransform.transform.position.y + (counter*2), deckTransform.transform.position.z);
                card.transform.parent = deckTransform.transform;
                deckPool[cardIDString] = card;
                cardObjectList.Add(card);

                counter++;
            }

            //deckTransform.rotation = Quaternion.Euler(90, 0, 0);
        }

        else ResetCards();
        
        
    }

    [ContextMenu("Reset Cards")]
    public void ResetCards()
    {
        Transform deckTransform = GameObject.Find("DeckTransform").transform;


        foreach (var card in cardObjectList)
        {
            if (card != null)
            {
                SetAutoRotateFlagFalse(card);
                card.transform.parent = null; // Unparent the card
                card.transform.parent = deckTransform;
                card.transform.rotation = Quaternion.Euler(deckTransform.rotation.x-90, deckTransform.rotation.y, deckTransform.rotation.z);
            }
        }
        
        activeCards.Clear();
        gameManager.centerCards.Clear();
        gameManager.centerCardsObjects.Clear();
        offsetCounter0=0;
        offsetCounter1=0;
        offsetCounter2=0;

        List<Vector3> positions = Enumerable.Repeat(deckTransform.position, 52).ToList();
        List<Quaternion> rotations = Enumerable.Repeat(Quaternion.Euler(deckTransform.rotation.x-90, deckTransform.rotation.y, deckTransform.rotation.z), 52).ToList();
        List<Vector3> scales = Enumerable.Repeat(new Vector3(1000,1000,1000), 52).ToList();

        ChainMoveCards(positions,cardObjectList, 40, rotations, scales);

        Debug.Log("All cards have been reset to the deck position.");
    }

    //Deals to players according to the playerCount
    public void DealPlayers(int playerCount, Dictionary<int, List<int[]>> playerHands)
    {
        this.playerCount = playerCount;

        if (playerCount != 2 && playerCount != 4)
        {
            Debug.LogError("Player number is different from 2 or 4");
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
        
        float spacing = 200;

        for (int i = 0; i < 2; i++)
        {
            relativeIndex = (i - thisPlayerNumber + playerCount) % playerCount;
            if (relativeIndex == 0) gameManager.myCards = new List<int[]>();

            for (int j = 0; j < 4; j++)
            {
                var cardID = playerHands[i][j];
                if (relativeIndex == 0) gameManager.myCards.Add(cardID);
                if (relativeIndex == 1) relativeIndex=2;

                GameObject tempCardObject = GetCardFromPool(cardID);
                if (tempCardObject == null) continue;

                cardObjects.Add(tempCardObject);
                tempCardObject.transform.parent = playerHandTransforms[relativeIndex].transform;

                Vector3 basePos = playerHandTransforms[relativeIndex].position;
                Vector3 centerRotation = playerHandTransforms[relativeIndex].rotation.eulerAngles;
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
                    case 2: // Top (Player 2)
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

        StartCoroutine(ChainMoveCardsPlayersCoroutine(positions, cardObjects, 10, rotations, scales));
    }

    private void DealFourPlayers(Dictionary<int, List<int[]>> playerHands)
    {
        var cardObjects = new List<GameObject>();
        var positions = new List<Vector3>();
        var rotations = new List<Quaternion>();
        var scales = new List<Vector3>(); // List to store scales
        
        float spacing = 200;

        for (int i = 0; i < 4; i++)
        {
            relativeIndex = (i - thisPlayerNumber + playerCount) % playerCount;
            if (relativeIndex == 0) gameManager.myCards = new List<int[]>();

            for (int j = 0; j < 4; j++)
            {
                var cardID = playerHands[i][j];
                if (relativeIndex == 0) gameManager.myCards.Add(cardID);

                GameObject tempCardObject = GetCardFromPool(cardID);
                if (tempCardObject == null) continue;

                cardObjects.Add(tempCardObject);
                tempCardObject.transform.parent = playerHandTransforms[relativeIndex].transform;

                Vector3 basePos = playerHandTransforms[relativeIndex].position;
                Vector3 centerRotation = playerHandTransforms[relativeIndex].rotation.eulerAngles;
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

        StartCoroutine(ChainMoveCardsPlayersCoroutine(positions, cardObjects, 10, rotations, scales));
    }


    //Deals to center according to the playerCount
    public void DealCenter(List<int[]> centerCardIDs)
    {
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

        ChainMoveCards(positions, cardObjects, 10, rotations, scales);
    }

    private IEnumerator ChainMoveCardsPlayersCoroutine(List<Vector3 >positions, List<GameObject> cardObjects, int speed,List<Quaternion> rotations, List<Vector3> scales)
    {
        yield return new WaitForSeconds(0.5f);
        ChainMoveCards(positions, cardObjects, 10, rotations, scales);

        //yield return new WaitForSeconds(3f);
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

        Debug.LogError($"No cards available in the pool for ID: {cardIDString}");
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
        StartCoroutine(ChainMoveCardsPlayersCoroutine(positions, cardObjects, 10, rotations, scales));
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
        float spacing = 200;

        // Get the parent object of the current player's hand
        GameObject currentPlayerHand;
        if (playerNumber != -1)
        {
            currentPlayerHand = playerParentHands[playerNumber];
        }
        else
        {
            currentPlayerHand = playerParentHands[GameManager.currentPlayerNo];
            playerNumber = GameManager.currentPlayerNo;
        }


        if(currentPlayerHand.transform.childCount == 1)
        {
            return;
        }

        // Get all the cards that are children of the current player's hand
        var playerCards = new List<GameObject>();
        int counter = 0;
        foreach (Transform child in currentPlayerHand.transform)
        {
            if (counter > 0) playerCards.Add(child.gameObject);
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
                    currentScale = new Vector3(1750, 1750, 1750);
                    break;
                case 2: // Top (Player 2)
                    offset = new Vector3(spacing * 3f * (i - offsetMult), 1000+(i*10), 0);
                    rotation = Quaternion.Euler(centerRotation.x,centerRotation.y,centerRotation.z);
                    break;
            }

            Vector3 targetPosition = playerHandTransforms[playerNumber].position + offset;
            MoveCard(targetPosition, playerCards[i], 10, rotation, currentScale);
        }
        if(playerNumber == 0) StartCoroutine(SetAutoRotateFlagTrue(playerCards));
        Debug.LogWarning(playerNumber);
    }

    private void UpdateCurrentPlayerHandLayoutFourPlayers(int playerNumber = -1)
    {
        // Define the spacing for the layout
        float spacing = 200;

        // Get the parent object of the current player's hand
        GameObject currentPlayerHand;
        if (playerNumber != -1)
        {
            currentPlayerHand = playerParentHands[playerNumber];
        }
        else
        {
            currentPlayerHand = playerParentHands[GameManager.currentPlayerNo];
            playerNumber = GameManager.currentPlayerNo;
        }


        if(currentPlayerHand.transform.childCount == 1)
        {
            return;
        }

        // Get all the cards that are children of the current player's hand
        var playerCards = new List<GameObject>();
        int counter = 0;
        foreach (Transform child in currentPlayerHand.transform)
        {
            if (counter > 0) playerCards.Add(child.gameObject);
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

            float yRotationDegrees = 75f;
            if (playerNumber == 1) yRotationDegrees *= -1;

            float radians = yRotationDegrees * Mathf.Deg2Rad;

            float d = 200f; // Desired local offset (e.g., spacing between cards)

            // Offset along local X (left-right in card space)
            float offsetX = d * Mathf.Cos(radians); // world-space X
            float offsetZ = d * Mathf.Sin(radians); // world-space Z
            Vector3 currentScale = playerCards[i].transform.localScale; // Use the current scale of the card

            switch (playerNumber)
            {
                case 0: // Bottom (Player 0)
                    offset = new Vector3(spacing * 15f * (i - offsetMult), 1000, 0);
                    rotation = Quaternion.Euler(-centerRotation.x,centerRotation.y,centerRotation.z);
                    currentScale = new Vector3(1750, 1750, 1750);
                    break;
                case 1: // Right (Player 1)
                    offset = new Vector3(0, i*10, spacing * 3f * (i - offsetMult));
                    rotation = Quaternion.Euler(centerRotation.x,centerRotation.y+90,centerRotation.z);
                    break;
                case 2: // Top (Player 2)
                    offset = new Vector3(spacing * 3f * (i - offsetMult), i*10, 0);
                    rotation = Quaternion.Euler(centerRotation.x,centerRotation.y,centerRotation.z);
                    break;
                case 3: // Left (Player 3)
                    offset = new Vector3(0, i*10, spacing * 3f * (i - offsetMult));
                    rotation = Quaternion.Euler(centerRotation.x,centerRotation.y+90,centerRotation.z);
                    break;
            }

            Vector3 targetPosition = playerHandTransforms[playerNumber].position + offset;
            MoveCard(targetPosition, playerCards[i], 10, rotation, currentScale);
        }
        if(playerNumber == 0) StartCoroutine(SetAutoRotateFlagTrue(playerCards));
        Debug.LogWarning(playerNumber);
    }
    public IEnumerator SetAutoRotateFlagTrue(List<GameObject> cardObjects)
    {
        Debug.LogWarning("CardInteraction not found for the specified GameObject.");
        yield return new WaitForSeconds(0.5f);
        // Search the list for a CardInteraction with the matching GameObject
        foreach (var cardObject in cardObjects)
        {
            foreach (var cardInteraction in cardInteractionList)
            {
                if (cardInteraction.gameObject == cardObject)
                {
                    cardInteraction.autoRotateFlag=true; // Return the matching CardInteraction
                }
            }
        }
        

        Debug.LogWarning("CardInteraction not found for the specified GameObject.");
        
    }

    public void SetAutoRotateFlagFalse(GameObject cardObject)
    {
        Debug.LogWarning("CardInteraction not found for the specified GameObject.");
        
        foreach (var cardInteraction in cardInteractionList)
        {
            if (cardInteraction.gameObject == cardObject)
            {
                cardInteraction.autoRotateFlag=false; // Return the matching CardInteraction
            }
        }
        
        

        Debug.LogWarning("CardInteraction not found for the specified GameObject.");
        
    }

    public void MoveCard(Vector3 endPos, GameObject cardObject, float speed, Quaternion rotation, Vector3 scales,bool audioFlag=true)
    {
        // Start the coroutine to move the card
        StartCoroutine(MoveCardCoroutine(endPos, cardObject, speed, rotation, scales,audioFlag));
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

    public void ChainMoveCards(List<Vector3> positions, List<GameObject> cardObject, float speed, List<Quaternion> rotations, List<Vector3> scales)
    {
        StartCoroutine(ChainMoveCardsCoroutine(positions, cardObject, speed, rotations, scales));
    }

    int offset=150; 
    int offsetCounter0=-1;
    int offsetCounter1=-1;
    int offsetCounter2=0-1;
    public void ChainMoveCardsToPool(Vector3 position, List<GameObject> cardObjects, float speed, int poolIndex)
    {   
        List<Vector3> positions = new List<Vector3>();
        List<Quaternion> rotations = new List<Quaternion>();
        List<Vector3> scales = new List<Vector3>();
        for (int i = 0; i < cardObjects.Count; i++)
        {
            positions.Add(position);
            Quaternion rotation = Quaternion.Euler(-90, 0, UnityEngine.Random.Range(170f, 190f));
            rotations.Add(rotation);
            scales.Add(new Vector3(1000, 1000, 1000)); // Set scale for all cards

            if (cardObjects.Count == 2)
            {
                if (i == cardObjects.Count - 1)
                {
                    rotations[i] = Quaternion.Euler(0, 0, 90);
                    Debug.LogWarning(poolIndex);
                    positions[i] = new Vector3(position.x + 100, position.y + GetChkobbaYOffset(poolIndex), position.z - GetChkobbaZOffset() + 10);
                }
                StartCoroutine(ChkobbaCoroutine());
            }
        }

        StartCoroutine(ChainMoveCardsCoroutine(positions, cardObjects, speed, rotations, scales, true));
    }

    private IEnumerator ChkobbaCoroutine()
    {
        chkobbaText.SetActive(true);

        yield return new WaitForSeconds(2);

        chkobbaText.SetActive(false);
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

    private int zOffsetCounter=0;
    private int GetChkobbaZOffset()
    {   
        zOffsetCounter++;
        return zOffsetCounter;
    }


    private IEnumerator ChainMoveCardsCoroutine(List<Vector3> positions, List<GameObject> cardObjects, float speed, List<Quaternion> rotations, List<Vector3> scales, bool playFlag=false)
    {
        if(cardObjects.Count<20)yield return new WaitForSeconds(2f - (cardObjects.Count*0.1f));
        else yield return new WaitForSeconds(0.2f);
        for(int i = 0; i < cardObjects.Count; i++)
        {
            yield return StartCoroutine(MoveCardCoroutine(positions[i], cardObjects[i], speed + (i*0.25f), rotations[i],scales[i]));
        }
        UpdateCurrentPlayerHandLayout();
    
    }
    public void SetPlayerNumber(int playerNumber)
    {
        thisPlayerNumber = playerNumber;
        Debug.LogWarning("thisPlayerNumber: " + thisPlayerNumber);
    }

    public int SendPlayerNumber()
    {
        return thisPlayerNumber;
    }

    [ContextMenu("Print ThisPlayerNumber")]
    private void PrintThisPlayerNumber()
    {
        Debug.Log("ThisPlayerNumber: " + thisPlayerNumber);
    }

    public void AddCardsToPlayerPool(int playerNumber)
    {
        Debug.Log("inside AddCardsToPlayerPool: " + gameManager.centerCardsObjects.Count);
        List<GameObject> centerObjects = new List<GameObject>();
        int relativePoolIndex = (playerNumber - thisPlayerNumber + playerCount) % playerCount;
        foreach(Transform child in GameObject.Find("Center").transform)
        {
            GameObject gameObject = child.gameObject;
            centerObjects.Add(gameObject);
        }


        ChainMoveCardsToPool(playerPoolTransforms[GetPoolIndex(relativePoolIndex)].position, centerObjects, 10, relativePoolIndex);
    }

    public void MoveCardsToPlayerPool(List<GameObject> cardObjects, int playerNumber)
    {   
        int relativePoolIndex = (playerNumber - thisPlayerNumber + playerCount) % playerCount;
        foreach(var card in cardObjects)
        {   
            card.transform.parent = null; // Unparent the card
            //card.transform.parent = playerPoolTransforms[0];
            gameManager.centerCardsObjects.Remove(card);  
        }
        ChainMoveCardsToPool(playerPoolTransforms[GetPoolIndex(relativePoolIndex)].position, cardObjects, 10, GetPoolIndex(relativePoolIndex));
    }

    private int GetPoolIndex(int relativePoolIndex)
    {
        int poolIndex=0;
        if(playerCount==4)
        {
            if(relativePoolIndex==0 || relativePoolIndex==2)poolIndex=0;
            else if(relativePoolIndex==1 || relativePoolIndex==3)poolIndex=1;
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
        chkobbaText = GameObject.Find("ChkobbaText");
        chkobbaText.SetActive(false);

        cardPrefabs = new Dictionary<string, GameObject>();
        deckPool = new Dictionary<string, GameObject>();

        if(NetworkManager.Singleton.IsHost)
        {
            thisPlayerNumber = WebGLCommunication.LocalInstance.playerNumber;
        }
        else if(NetworkManager.Singleton.IsClient)
        {
            thisPlayerNumber = WebGLCommunication.LocalInstance.playerNumber;
            //gameManager.AskPlayerNumber();
        }

        //StartCoroutine(DelayedFlag());
        GetPools();
        GetPlayerHandObjects();
    }

    private void GetPools()
    {
        for(int i = 0; i<4; i++)
        {
            string tempTag = "PlayerPool" + (i+1);
            playerPools.Add(GameObject.Find(tempTag).GetComponent<Transform>().position);
            //Debug.Log(tempTag);
        }
    }

    private void GetPlayerHandObjects()
    {
        for(int i = 0; i<4 ; i++)
        {
            string tempTag = "PlayerHand" + (i+1);
            playerParentHands[i] = GameObject.FindGameObjectWithTag(tempTag);
            //Debug.Log(playerParentHands[i].name);
        }
    }

    private List<Transform> playerHandTransforms = new List<Transform>();
    private List<Transform> playerPoolTransforms = new List<Transform>();
    private Transform centerTransform;
    public void getPlayerHandTransforms(List<Transform> playerHTransforms, List<Transform> playerPTransforms, Transform cTransform)
    {
        foreach(var playerH in playerHTransforms)
        {
            playerHandTransforms.Add(playerH);
        }

        foreach(var playerP in playerPTransforms)
        {
            playerPoolTransforms.Add(playerP);
        }

        centerTransform = cTransform;
    }
}

