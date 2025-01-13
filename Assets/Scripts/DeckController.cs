using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class DeckController : MonoBehaviour
{   
    public static DeckController LocalInstance;
    public int thisPlayerNumber;
    [SerializeField]private GameManager gameManager;
    [SerializeField] private GameObject addCardPrefab;//Prefab of the add button
    [SerializeField] private List<GameObject> cardPrefabsList;//Prefabs of all the cards.
    [SerializeField] private Dictionary<string, GameObject> cardPrefabs;//A dictionary to keep track of each card prefabs with its ID
    private Dictionary<string, List<GameObject>> cardPools;//A dictionary of card ID and a list of all the instantiated cards
    private List<GameObject> activeCards = new List<GameObject>();
    private List<CardInteraction> cardInteractionList; // List to store CardInteraction references
    private GameObject[] playerParentHands = new GameObject[4];//Array of player hand objects to keep track of where the card objects will be placed
    private GameObject centerParent;//Center object to keep track of where the center cards will be placed

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

        cardPrefabs = new Dictionary<string, GameObject>();
        cardPools = new Dictionary<string, List<GameObject>>();

        if(NetworkManager.Singleton.IsHost)
        {
            thisPlayerNumber = 0;
        }
        else if(NetworkManager.Singleton.IsClient)
        {
            thisPlayerNumber = -1;
            gameManager.AskPlayerNumber();
        }

        StartCoroutine(DelayedFlag());

        GetPlayerHandObjects();
    }

    private bool tempFlag=false;
    void Update()
    {
        if(tempFlag)
        {
            Debug.Log("ThisPlayerNumber: " + thisPlayerNumber);
            tempFlag=false;
        }
    }

    private void GetPlayerHandObjects()
    {
        for(int i = 0; i<4 ; i++)
        {
            string tempTag = "PlayerHand" + (i+1);
            playerParentHands[i] = GameObject.FindGameObjectWithTag(tempTag);
            Debug.Log(playerParentHands[i].name);
        }
        centerParent = GameObject.FindGameObjectWithTag("Center");
        Debug.Log(centerParent.name);
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
            int[] cardID = new int[] { (i / 10) + 1, (i % 10) + 1 };
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
    private void InitializeCardPool()
    {
        cardInteractionList = new List<CardInteraction>();
        GameObject cardPool = new GameObject("CardPool");
        foreach (var cardPrefabEntry in cardPrefabs)
        {
            var cardIDString = cardPrefabEntry.Key;
            var cardPrefab = cardPrefabEntry.Value;

            // Create a pool for this card type
            cardPools[cardIDString] = new List<GameObject>();

            // Instantiate and add a fixed number of cards to the pool
            for (int i = 0; i < 3; i++)
            {
                GameObject card = Instantiate(cardPrefab);
                if(i==0)
                {
                    card.SetActive(true); // Deactivate the card
                    card.transform.position = new Vector3(-3000, -2000, 0); // Place off-screen
                    card.transform.rotation = Quaternion.Euler(0, 180, 0);
                }
                else
                {
                    card.SetActive(false); // Deactivate the card
                    card.transform.position = new Vector3(-2000, -2000, 0); // Place off-screen
                }

                // Get the CardInteraction component and add it to the list
                CardInteraction cardInteraction = card.GetComponent<CardInteraction>();
                if (cardInteraction != null)
                {
                    cardInteractionList.Add(cardInteraction); // Store the reference in the list
                }
                
                card.transform.parent = cardPool.transform;
                cardPools[cardIDString].Add(card);
            }
        }
    }

    //Deals to players according to the playerCount
    public void DealPlayers(int playerCount, Dictionary<int, List<int[]>> playerHands)
    {
        List<GameObject> cardObjects = new List<GameObject>();
        List<Vector3> positions = new List<Vector3>();
        List<Quaternion> rotations = new List<Quaternion>();

        if (playerCount == 2 || playerCount == 4)
        {
            for (int i = 0; i < playerCount; i++)
            {
                // Determine the actual player index, relative to thisPlayerNumber
                int relativeIndex = (i - thisPlayerNumber + playerCount) % playerCount;

                for (int j = 0; j < 3; j++)
                {
                    // Get the card ID for the relative player
                    var cardID = playerHands[i][j];

                    // Get the card object for the cardID
                    GameObject tempCardObject = GetCardFromPool(cardID);
                    if (tempCardObject != null)
                    {
                        cardObjects.Add(tempCardObject);

                        Vector3 position = Vector3.zero;
                        Quaternion rotation = Quaternion.identity;

                        if (playerCount == 2)
                        {
                            // Positions for 2 players
                            if (relativeIndex == 0) // This player (treated as player 0)
                            {
                                position = new Vector3(j * 700, -2000, 0); // Bottom position
                                
                            }
                            else if (relativeIndex == 1) // The other player (treated as player 1)
                            {
                                position = new Vector3(j * 700, 2000, 0); // Top position
                                rotation = Quaternion.Euler(0, 180, 0);
                            }
                        }
                        else if (playerCount == 4)
                        {
                            // Positions for 4 players
                            switch (relativeIndex)
                            {
                                case 0: // This player (treated as player 0)
                                    position = new Vector3(j * 700 + 200, -2000, 0); // Bottom position
                                    break;
                                case 1: // Next player (treated as player 1)
                                    position = new Vector3(5000, j * 700 - 500, 0); // Right position
                                    rotation = Quaternion.Euler(0, 180, 90);
                                    break;
                                case 2: // Opposite player (treated as player 2)
                                    position = new Vector3(j * 700 + 200, 2000, 0); // Top position
                                    rotation = Quaternion.Euler(0, 180, 0);
                                    break;
                                case 3: // Previous player (treated as player 3)
                                    position = new Vector3(-2500, j * 700 - 500, 0); // Left position
                                    rotation = Quaternion.Euler(0, 180, 90);
                                    break;
                            }
                        }

                        positions.Add(position);
                        rotations.Add(rotation);

                        // Set the parent of the card to the correct hand
                        tempCardObject.transform.parent = playerParentHands[i].transform;
                    }
                }
            }

            // Chain movement for all cards
            ChainMoveCards(positions, cardObjects, 10, rotations);
        }
        else
        {
            Debug.LogError("Player number is different from 2 or 4");
        }
    }



    //Deals to center according to the playerCount
    public void DealCenter(List<int[]> centerCardIDs)
    {
        SendCardInteractionsToGameManager();
        List<GameObject> cardObjects = new List<GameObject>();
        List<Vector3> positions = new List<Vector3>();
        List<Quaternion> rotations = new List<Quaternion>();
        for (int i = 0; i < 4; i++)
        {
            var cardID = centerCardIDs[i];
            GameObject tempCenterCard = GetCardFromPool(cardID);
            cardObjects.Add(tempCenterCard);
            if (tempCenterCard != null)
            {
                positions.Add(new Vector3(i * 700, 0, 0));
                tempCenterCard.transform.parent = centerParent.transform;
                gameManager.centerCardsObjects.Add(tempCenterCard);
                gameManager.centerCards.Add(cardID);
                rotations.Add(Quaternion.identity);
            }
        }
        ChainMoveCards(positions, cardObjects, 10, rotations);
    }
    
    private void SendCardInteractionsToGameManager()
    {
        gameManager.GetCardInteractionScripts(cardInteractionList);
    }

    //Adds the card Object of the selected card to the center
    public void PlaceCardToCenter(Vector3 placementLocation, int[] placedCardID)
    {
        Debug.Log("Entered PlaceCardToCenter: " + placedCardID[0] + "_" + placedCardID[1]);
        GameObject placedCard = GetCardFromPool(placedCardID);
        if (placedCard != null)
        {
            placedCard.transform.position = placementLocation;
            placedCard.transform.rotation = Quaternion.identity;
            placedCard.transform.parent = centerParent.transform;

            gameManager.centerCards.Add(placedCardID);
            gameManager.centerCardsObjects.Add(placedCard);
        }
    }

    //Allow to get card object with the cardID
    private GameObject GetCardFromPool(int[] cardID)
    {
        string cardIDString = GameManager.TurnCardIdToString(cardID);
        print("cardIDString: " + cardIDString);

        if (cardPools.ContainsKey(cardIDString) && cardPools[cardIDString].Count > 0)
        {
            var card = cardPools[cardIDString][0];
            cardPools[cardIDString].RemoveAt(0);
            card.SetActive(true);
            activeCards.Add(card);
            return card;
        }

        Debug.LogError($"No cards available in the pool for ID: {cardIDString}");
        return null;
    }

    private void ReturnAllActiveCardsToPool()
    {
        foreach (var card in activeCards.ToArray())
        {
            int[] cardID = ExtractCardID(card); // Define a way to get cardID from the card object
            ReturnCardToPool(card, cardID);
        }
    }
    
    private void ReturnCardToPool(GameObject card, int[] cardID)
    {
        string cardIDString = GameManager.TurnCardIdToString(cardID);

        if (cardPools.ContainsKey(cardIDString))
        {
            card.SetActive(false);
            card.transform.position = new Vector3(-2000, -2000, 0); // Place off-screen
            activeCards.Remove(card);
            cardPools[cardIDString].Add(card);
        }
        else
        {
            Debug.LogError($"No pool found for card ID: {cardIDString}");
        }
    }

    private int[] ExtractCardID(GameObject card)
    {
        // Implement logic to extract cardID from card metadata or name
        throw new NotImplementedException("Add logic to extract cardID from the GameObject");
    }

    public void DiscardHandCard(string cardID)
    {
        GameObject tempCardObject = GameObject.FindWithTag(cardID);
        tempCardObject.transform.position = new Vector3(-10000,-10000,0);
        tempCardObject.transform.parent = null;
        Destroy(tempCardObject);
    }

    public void UpdateCenterCardsLayout()
    {
        // Define the spacing and center of the layout
        float cardSpacing = 600f; // Distance between cards
        Vector3 centerPosition = new Vector3(0, 0, 0); // Center of the layout

        int totalCards = gameManager.centerCardsObjects.Count;
        if (totalCards == 0) return;

        // Calculate the starting position for the layout
        float totalWidth = (totalCards - 1) * cardSpacing; // Total width occupied by the cards
        float startX = centerPosition.x - (totalWidth / 2) + 700; // Leftmost card position

        // Arrange cards
        for (int i = 0; i < totalCards; i++)
        {
            Vector3 targetPosition = new Vector3(startX + (i * cardSpacing), centerPosition.y, centerPosition.z);
            gameManager.centerCardsObjects[i].transform.position = targetPosition;

            // Optionally: Add animation for smooth transition
            // You can use a coroutine or tweening library for this
        }
    }

    public void MoveCard(Vector3 endPos, GameObject cardObject, float speed, Quaternion rotation)
    {
        // Start the coroutine to move the card
        StartCoroutine(MoveCardCoroutine(endPos, cardObject, speed, rotation));
    }

    private IEnumerator MoveCardCoroutine(Vector3 endPos, GameObject cardObject, float speedMultiplier, Quaternion rotation)
    {
        // Set the initial position of the card
        Vector3 startingPos = cardObject.transform.position;

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

            // Increment elapsed time
            timeElapsed += Time.deltaTime;

            // Wait for the next frame
            yield return null;
        }

        // Ensure the card reaches the exact end position
        cardObject.transform.position = endPos;

        // Call RotateCard to apply rotation
        RotateCard(rotation, cardObject, 20);
    }


    public void RotateCard(Quaternion endRotation, GameObject cardObject, float speed)
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

    public void ChainMoveCards(List<Vector3> positions, List<GameObject> cardObject, float speed, List<Quaternion> rotations)
    {
        StartCoroutine(ChainMoveCardsCoroutine(positions, cardObject, speed, rotations));
    }

    private IEnumerator ChainMoveCardsCoroutine(List<Vector3> positions, List<GameObject> cardObject, float speed, List<Quaternion> rotations)
    {
        foreach (Vector3 position in positions)
        {
            // Wait for the current MoveCardCoroutine to finish
            int index = positions.IndexOf(position);
            yield return StartCoroutine(MoveCardCoroutine(position, cardObject[index], speed, rotations[index]));
        }
    }

    public void SetPlayerNumber(int playerNumber)
    {
        thisPlayerNumber = playerNumber;
    }

    public int SendPlayerNumber()
    {
        return thisPlayerNumber;
    }

    private IEnumerator DelayedFlag()
    {
        yield return new WaitForSeconds(1);
        tempFlag=true;
    }
}

