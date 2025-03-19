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
    private List<Vector3> playerPools = new List<Vector3>();
    private int relativeIndex = 0;
    public int playerCount = 0;
    private GameObject cardPool;
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

        chkobbaText = GameObject.Find("ChkobbaText");
        chkobbaText.SetActive(false);

        cardPrefabs = new Dictionary<string, GameObject>();
        cardPools = new Dictionary<string, List<GameObject>>();

        if(NetworkManager.Singleton.IsHost)
        {
            thisPlayerNumber = WebGLCommunication.LocalInstance.playerNumber;
        }
        else if(NetworkManager.Singleton.IsClient)
        {
            thisPlayerNumber = WebGLCommunication.LocalInstance.playerNumber;
            //gameManager.AskPlayerNumber();
        }

        StartCoroutine(DelayedFlag());
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
    private void InitializeCardPool()
    {
        cardInteractionList = new List<CardInteraction>();
        if(cardPool!=null)DestroyCards();
        cardPool = new GameObject("CardPool");
        foreach (var cardPrefabEntry in cardPrefabs)
        {
            var cardIDString = cardPrefabEntry.Key;
            var cardPrefab = cardPrefabEntry.Value;

            // Create a pool for this card type
            cardPools[cardIDString] = new List<GameObject>();

            // Instantiate and add a fixed number of cards to the pool
            for (int i = 0; i < 1; i++)
            {
                GameObject card = Instantiate(cardPrefab);
                if(i==0)
                {
                    card.SetActive(true); // Deactivate the card
                    card.transform.position = transform.position; // Place off-screen
                    card.transform.rotation = Quaternion.Euler(0, 180, 0);
                }
                else
                {
                    card.SetActive(false); // Deactivate the card
                    card.transform.position = transform.position; // Place off-screen
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

        

        cardPool.transform.position = GameObject.Find("CardPoolObject").transform.position;
    }

    private void DestroyCards()
    {
        // Get the layer index for the "Cards" layer
        int cardsLayer = LayerMask.NameToLayer("Cards");

        if (cardsLayer == -1)
        {
            Debug.LogError("Layer 'Cards' does not exist.");
            return;
        }

        // Find all GameObjects in the scene
        GameObject[] allGameObjects = FindObjectsOfType<GameObject>();

        // Iterate through all GameObjects and destroy those on the "Cards" layer
        foreach (GameObject obj in allGameObjects)
        {
            if (obj.layer == cardsLayer)
            {
                Destroy(obj);
            }
        }

        gameManager.centerCards.Clear();
        gameManager.centerCardsObjects.Clear();
        offsetCounter0=0;
        offsetCounter1=0;
        offsetCounter2=0;


        Debug.Log("All objects on the 'Cards' layer have been destroyed.");
        Destroy(cardPool);
    }


    //Deals to players according to the playerCount
    public void DealPlayers(int playerCount, Dictionary<int, List<int[]>> playerHands)
    {
        this.playerCount = playerCount;
        List<GameObject> cardObjects = new List<GameObject>();
        List<Vector3> positions = new List<Vector3>();
        List<Quaternion> rotations = new List<Quaternion>();
        int turnAround = 180;

        if (playerCount == 2 || playerCount == 4)
        {
            for (int i = 0; i < playerCount; i++)
            {
                // Determine the actual player index, relative to thisPlayerNumber
                relativeIndex = (i - thisPlayerNumber + playerCount) % playerCount;
                if(relativeIndex == 0)
                {
                    gameManager.myCards = new List<int[]>();
                }

                for (int j = 0; j < 3; j++)
                {
                    // Get the card ID for the relative player
                    var cardID = playerHands[i][j];
                    if(relativeIndex == 0)
                    {
                        gameManager.myCards.Add(cardID);
                    }
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
                                position = new Vector3((j * 700)+275, -1600, 0); // Bottom position
                                
                            }
                            else if (relativeIndex == 1) // The other player (treated as player 1)
                            {
                                position = new Vector3((j * 700)+275, 1600, 0); // Top position
                                rotation = Quaternion.Euler(0, turnAround, 0);
                            }
                        }
                        else if (playerCount == 4)
                        {
                            // Positions for 4 players
                            switch (relativeIndex)
                            {
                                case 0: // This player (treated as player 0)
                                    position = new Vector3((j * 700)+275, -1600, 0); // Bottom position
                                    break;
                                case 1: // Next player (treated as player 1)
                                    position = new Vector3(4655, j * 700 - 700, 0); // Right position
                                    rotation = Quaternion.Euler(0, turnAround, 90);
                                    break;
                                case 2: // Opposite player (treated as player 2)
                                    position = new Vector3((j * 700)+275, 1600, 0); // Top position
                                    rotation = Quaternion.Euler(0, turnAround, 0);
                                    break;
                                case 3: // Previous player (treated as player 3)
                                    position = new Vector3(-2700, j * 700 - 700, 0); // Left position
                                    rotation = Quaternion.Euler(0, turnAround, 90);
                                    break;
                            }
                        }

                        positions.Add(position);
                        rotations.Add(rotation);

                        // Set the parent of the card to the correct hand
                        tempCardObject.transform.parent = playerParentHands[i].transform;
                        if(relativeIndex == 0) Debug.Log("playerParentHand: " + i);
                    }
                }
            }

            // Chain movement for all cards
            StartCoroutine(ChainMoveCardsPlayersCoroutine(positions, cardObjects, 10, rotations));
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
                positions.Add(new Vector3(980, 0, i*-10));
                tempCenterCard.transform.parent = centerParent.transform;
                gameManager.centerCardsObjects.Add(tempCenterCard);
                gameManager.centerCards.Add(cardID);
                if(i==3)rotations.Add(Quaternion.Euler(0, 0, UnityEngine.Random.Range(-12,12)));
                else rotations.Add(Quaternion.Euler(0, 180, UnityEngine.Random.Range(-12,12)));
            }
        }
        ChainMoveCards(positions, cardObjects, 10, rotations);
        //Invoke("UpdateCenterCardsLayout", 0.8f);
    }

    private IEnumerator ChainMoveCardsPlayersCoroutine(List<Vector3 >positions, List<GameObject> cardObjects, int speead,List<Quaternion> rotations)
    {
        yield return new WaitForSeconds(1.5f);
        ChainMoveCards(positions, cardObjects, 10, rotations);
    }
    
    private void SendCardInteractionsToGameManager()
    {
        gameManager.GetCardInteractionScripts(cardInteractionList);
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

    public void DiscardHandCardToCenter(int[] cardID)
    {
        /*GameObject tempCardObject = GameObject.FindWithTag(GameManager.TurnCardIdToString(cardID));
        tempCardObject.transform.position = new Vector3(-10000,-10000,0);
        tempCardObject.transform.parent = null;*/
        PlaceCardToCenter(cardID);
    }

    //Adds the card Object of the selected card to the center
    public void PlaceCardToCenter(int[] placedCardID)
    {
        GameObject placedCard = GameObject.FindGameObjectWithTag(GameManager.TurnCardIdToString(placedCardID) );
        if (placedCard != null)
        {
            placedCard.transform.rotation = Quaternion.Euler(0,0,UnityEngine.Random.Range(-12,12));
            placedCard.transform.parent = centerParent.transform;
            placedCard.transform.position = new Vector3(980,0,-10*GameManager.LocalInstance.centerCardsObjects.Count);

            gameManager.centerCards.Add(placedCardID);
            gameManager.centerCardsObjects.Add(placedCard);
        }

        AudioManager.Instance.PlayAudio(3,1,false);
        //UpdateCenterCardsLayout();
    }

    public void UpdateCenterCardsLayout()
    {
        // Define the spacing and center of the layout
        float cardSpacing = 600f; // Distance between cards
        Vector3 centerPosition = new Vector3(0, 0, 0); // Center of the layout

        int totalCards = gameManager.centerCardsObjects.Count;
        Debug.Log("Gamemanager.centerCardsObjects.Count: " + gameManager.centerCardsObjects.Count);
        if (totalCards == 0) return;

        // Calculate the starting position for the layout
        float totalWidth = (totalCards - 1) * cardSpacing; // Total width occupied by the cards
        float startX = centerPosition.x - (totalWidth / 2) + 975; // Leftmost card position

        // Arrange cards
        for (int i = 0; i < totalCards; i++)
        {
            Vector3 targetPosition = new Vector3(startX + (i * cardSpacing), centerPosition.y, centerPosition.z);

            MoveCard(targetPosition, gameManager.centerCardsObjects[i], 10, Quaternion.identity,false);
            //gameManager.centerCardsObjects[i].transform.position = targetPosition;

            // Optionally: Add animation for smooth transition
            // You can use a coroutine or tweening library for this
        }
    }

    public void MoveCard(Vector3 endPos, GameObject cardObject, float speed, Quaternion rotation, bool audioFlag=true)
    {
        // Start the coroutine to move the card
        StartCoroutine(MoveCardCoroutine(endPos, cardObject, speed, rotation, audioFlag));
    }

    private IEnumerator MoveCardCoroutine(Vector3 endPos, GameObject cardObject, float speedMultiplier, Quaternion rotation, bool audioFlag=true)
    {
        // Set the initial position of the card
        Vector3 startingPos = cardObject.transform.position;

        if(audioFlag)AudioManager.Instance.PlayAudio(3,1,false);

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

    public void ChainMoveCards(List<Vector3> positions, List<GameObject> cardObject, float speed, List<Quaternion> rotations)
    {
        StartCoroutine(ChainMoveCardsCoroutine(positions, cardObject, speed, rotations));
    }

    int offset=150; 
    int offsetCounter0=-1;
    int offsetCounter1=-1;
    int offsetCounter2=0-1;
    public void ChainMoveCardsToPool(Vector3 position, List<GameObject> cardObjects, float speed, int poolIndex)
    {   
        List<Vector3> positions = new List<Vector3>();
        List<Quaternion> rotations = new List<Quaternion>();
        for (int i = 0; i<cardObjects.Count; i++)
        {
            positions.Add(position);
            Quaternion rotation = Quaternion.Euler(0, 180, UnityEngine.Random.Range(170f, 190f));
            rotations.Add(rotation);

            if(gameManager.centerCardsObjects.Count == 0)
            {
                if(i==cardObjects.Count-1)
                {
                    rotations[i] = Quaternion.Euler(0, 0, 90);
                    Debug.LogWarning(poolIndex);
                    positions[i] = new Vector3(position.x+100, position.y+GetChkobbaYOffset(poolIndex),position.z-GetChkobbaZOffset()+10);
                }
                StartCoroutine(ChkobbaCoroutine());
            }
        }

        StartCoroutine(ChainMoveCardsCoroutine(positions, cardObjects, speed, rotations,true));
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


    private IEnumerator ChainMoveCardsCoroutine(List<Vector3> positions, List<GameObject> cardObjects, float speed, List<Quaternion> rotations, bool playFlag=false)
    {
        if(playFlag)
        {
            RotateCard(Quaternion.identity,cardObjects[cardObjects.Count-1]);
        }
        for(int i = 0; i < cardObjects.Count; i++)
        {
            yield return StartCoroutine(MoveCardCoroutine(positions[i], cardObjects[i], speed, rotations[i]));
        }
    
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

    private IEnumerator DelayedFlag()
    {
        yield return new WaitForSeconds(1);
        tempFlag=true;
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


        ChainMoveCardsToPool(playerPools[GetPoolIndex(relativePoolIndex)], centerObjects, 10, relativePoolIndex);
    }

    public void MoveCardsToPlayerPool(List<GameObject> cardObjects, int playerNumber)
    {   
        int relativePoolIndex = (playerNumber - thisPlayerNumber + playerCount) % playerCount;
        foreach(var card in cardObjects)
        {   
            Debug.Log("Also inside the loop");
            card.transform.parent = cardPool.transform;
            gameManager.centerCardsObjects.Remove(card);    
        }

        
        ChainMoveCardsToPool(playerPools[GetPoolIndex(relativePoolIndex)], cardObjects, 10, GetPoolIndex(relativePoolIndex)); 
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
}

