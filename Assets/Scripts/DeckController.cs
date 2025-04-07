using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
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
                card.transform.rotation = Quaternion.Euler(270, 0, 0);


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
    private void ResetCards()
    {
        Transform deckTransform = GameObject.Find("DeckTransform").transform;

        foreach (var card in cardObjectList)
        {
            if (card != null)
            {
                card.transform.parent = null; // Unparent the card
                card.transform.parent = deckTransform;
            }
        }

        activeCards.Clear();
        gameManager.centerCards.Clear();
        gameManager.centerCardsObjects.Clear();
        offsetCounter0=0;
        offsetCounter1=0;
        offsetCounter2=0;

        List<Vector3> positions = Enumerable.Repeat(deckTransform.position, 52).ToList();
        List<Quaternion> rotations = Enumerable.Repeat(Quaternion.Euler(0, 180, 0), 52).ToList();

        ChainMoveCards(positions,cardObjectList, 10, rotations);

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

        float archRadius = 1000f; // Radius of the arch for the cards
        float angleStep = 30f; // Angle between cards in degrees

        for (int i = 0; i < 2; i++)
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
                tempCardObject.transform.parent = playerHandTransforms[relativeIndex == 0 ? 0 : 2].transform;

                Vector3 handTransformPosition = playerHandTransforms[relativeIndex == 0 ? 0 : 2].position;

                // Calculate the center of the arch
                Vector3 archCenter = handTransformPosition + new Vector3(0, relativeIndex == 0 ? -archRadius : -archRadius, 0);

                // Calculate the starting angle for the layout
                float startAngle = -angleStep * (4 - 1) / 2; // Center the arch
                float angle = startAngle + j * angleStep;

                // Calculate the position for the card
                Vector3 position = archCenter + Quaternion.Euler(0, 0, angle) * (handTransformPosition - archCenter);
                position.z -= j * 10; // Decrease z value from left to right
                positions.Add(position);

                // Calculate the rotation for the card
                Quaternion rotation = Quaternion.Euler(0, 0, angle);
                rotations.Add(rotation);
            }
        }

        StartCoroutine(ChainMoveCardsPlayersCoroutine(positions, cardObjects, 10, rotations));
    }

    private void DealFourPlayers(Dictionary<int, List<int[]>> playerHands)
    {
        var cardObjects = new List<GameObject>();
        var positions = new List<Vector3>();
        var rotations = new List<Quaternion>();

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
                Vector3 offset = Vector3.zero;
                Quaternion rotation = Quaternion.identity;

                float yRotationDegrees = 50f;
                if(relativeIndex==1) yRotationDegrees *= -1;

                float radians = yRotationDegrees * Mathf.Deg2Rad;

                float d = 200f; // This is your desired local offset (e.g., spacing between cards)

                // Offset along local X (left-right in card space)
                float offsetX = d * Mathf.Cos(radians); // world-space X
                float offsetZ = d * Mathf.Sin(radians); // world-space Z


                switch (relativeIndex)
                {
                    case 0: // Bottom (Player 0)
                        offset = new Vector3(j * spacing, 0, -j * 10);
                        rotation = Quaternion.identity;
                        break;
                    case 1: // Right (Player 1)
                        offset = new Vector3((offsetX*j)-(1.5f*offsetX), 0, (-j * 10)+(j * offsetZ)-(1.5f*offsetZ));
                        rotation = Quaternion.Euler(0, -130, 0);
                        break;
                    case 2: // Top (Player 2)
                        offset = new Vector3((j * spacing)-(1.5f*spacing), 0, -j * 10);
                        rotation = Quaternion.Euler(0, 180, 0);
                        break;
                    case 3: // Left (Player 3)
                        offset = new Vector3((j * offsetX)-(1.5f*offsetX), 0, (-j * 10)+(offsetZ*j)-(1.5f*offsetZ));
                        rotation = Quaternion.Euler(0, 130, 0);
                        break;
                }

                positions.Add(basePos + offset);
                rotations.Add(rotation);
            }
        }

        StartCoroutine(ChainMoveCardsPlayersCoroutine(positions, cardObjects, 10, rotations));
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
                // Calculate position based on centerTransform
                Vector3 centerPosition = centerTransform.position;
                positions.Add(new Vector3(centerPosition.x, centerPosition.y, centerPosition.z + i * -10));
                
                tempCenterCard.transform.parent = centerTransform;
                gameManager.centerCardsObjects.Add(tempCenterCard);
                gameManager.centerCards.Add(cardID);

                if (i == 3)
                    rotations.Add(Quaternion.Euler(60, 0, UnityEngine.Random.Range(-12, 12)));
                else
                    rotations.Add(Quaternion.Euler(300, 180, UnityEngine.Random.Range(-12, 12)));
            }
        }

        ChainMoveCards(positions, cardObjects, 10, rotations);
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

        if (deckPool.ContainsKey(cardIDString) && deckPool[cardIDString] != null)
        {
            var card = deckPool[cardIDString];
            deckPool[cardIDString] = null;
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
        if (placedCard != null)
        {
            placedCard.transform.rotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-12, 12));
            placedCard.transform.parent = centerTransform;

            // Use centerTransform for positioning
            Vector3 centerPosition = centerTransform.position;
            placedCard.transform.position = new Vector3(centerPosition.x, centerPosition.y, centerPosition.z - 10 * GameManager.LocalInstance.centerCardsObjects.Count);

            gameManager.centerCards.Add(cardID);
            gameManager.centerCardsObjects.Add(placedCard);
        }

        AudioManager.Instance.PlayAudio(3, 1, false);
        UpdateCurrentPlayerHandLayout();
    }

    public void UpdateCurrentPlayerHandLayout()
    {
        if (playerCount == 2)
        {
            UpdateCurrentPlayerHandLayoutTwoPlayers();
        }
        else if (playerCount == 4)
        {
            UpdateCurrentPlayerHandLayoutFourPlayers();
        }
    }

    private void UpdateCurrentPlayerHandLayoutTwoPlayers()
    {
        // Define the spacing for the layout
        float archRadius = 1000f; // Radius of the arch for the cards
        float angleStep = 30f; // Angle between cards in degrees

        // Map player numbers to hand indices for two players
        int handIndex = GameManager.currentPlayerNo == 0 ? 0 : 2;

        // Get the parent object of the current player's hand
        GameObject currentPlayerHand = playerParentHands[handIndex];

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

        // Get the center position from the playerHandTransforms based on the hand index
        Transform playerHandTransform = playerHandTransforms[handIndex];
        Vector3 handTransformPosition = playerHandTransform.position;

        // Calculate the center of the arch
        Vector3 archCenter = handTransformPosition + new Vector3(0, handIndex == 0 ? -archRadius : archRadius, 0);

        // Calculate the starting angle for the layout
        float startAngle = -angleStep * (totalCards - 1) / 2; // Center the arch

        // Arrange cards
        for (int i = 0; i < totalCards; i++)
        {
            float angle = startAngle + i * angleStep;

            // Calculate the position for the card
            Vector3 targetPosition = archCenter + Quaternion.Euler(0, 0, angle) * (handTransformPosition - archCenter);
            targetPosition.z -= i * 10; // Decrease z value from left to right
            Quaternion targetRotation = Quaternion.Euler(0, 0, angle);

            MoveCard(targetPosition, playerCards[i], 10, targetRotation, false);
        }
    }

    private void UpdateCurrentPlayerHandLayoutFourPlayers()
    {
        // Define the spacing for the layout
        float archRadius = 1000f; // Radius of the arch for the cards
        float angleStep = 30f; // Angle between cards in degrees

        // Get the parent object of the current player's hand
        GameObject currentPlayerHand = playerParentHands[GameManager.currentPlayerNo];

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

        // Get the center position from the playerHandTransforms based on the player number
        Transform playerHandTransform = playerHandTransforms[GameManager.currentPlayerNo];
        Vector3 handTransformPosition = playerHandTransform.position;

        // Calculate the center of the arch
        Vector3 archCenter = handTransformPosition;
        switch (GameManager.currentPlayerNo)
        {
            case 0: // Bottom
                archCenter += new Vector3(0, -archRadius, 0);
                break;
            case 1: // Right
                archCenter += new Vector3(archRadius, 0, 0);
                break;
            case 2: // Top
                archCenter += new Vector3(0, archRadius, 0);
                break;
            case 3: // Left
                archCenter += new Vector3(-archRadius, 0, 0);
                break;
        }

        // Calculate the starting angle for the layout
        float startAngle = -angleStep * (totalCards - 1) / 2; // Center the arch

        // Arrange cards
        for (int i = 0; i < totalCards; i++)
        {
            float angle = startAngle + i * angleStep;

            // Calculate the position for the card
            Vector3 targetPosition = archCenter + Quaternion.Euler(0, 0, angle) * (handTransformPosition - archCenter);
            targetPosition.z -= i * 10; // Decrease z value from left to right
            Quaternion targetRotation;

            if (GameManager.currentPlayerNo == 0 || GameManager.currentPlayerNo == 2)
            {
                targetRotation = Quaternion.Euler(0, 0, angle);
            }
            else if (GameManager.currentPlayerNo == 1 || GameManager.currentPlayerNo == 3)
            {
                targetRotation = Quaternion.Euler(0, 0, 90 + angle);
            }
            else
            {
                targetRotation = Quaternion.identity;
            }

            MoveCard(targetPosition, playerCards[i], 10, targetRotation, false);
        }
    }

    public void MoveCard(Vector3 endPos, GameObject cardObject, float speed, Quaternion rotation, bool audioFlag=true)
    {
        // Start the coroutine to move the card
        StartCoroutine(MoveCardCoroutine(endPos, cardObject, speed, rotation, audioFlag));
    }

    private IEnumerator MoveCardCoroutine(Vector3 endPos, GameObject cardObject, float speedMultiplier, Quaternion rotation, bool audioFlag = true)
    {
        // Set the initial position and rotation of the card
        Vector3 startingPos = cardObject.transform.position;
        Quaternion startingRotation = cardObject.transform.rotation;

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

            // Increment elapsed time
            timeElapsed += Time.deltaTime;

            // Wait for the next frame
            yield return null;
        }

        // Ensure the card reaches the exact end position and rotation
        cardObject.transform.position = endPos;
        cardObject.transform.rotation = rotation;
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
        Debug.LogWarning("ChainMoveCardsToPool: " + poolIndex);
        List<Vector3> positions = new List<Vector3>();
        List<Quaternion> rotations = new List<Quaternion>();
        for (int i = 0; i<cardObjects.Count; i++)
        {
            positions.Add(position);
            Quaternion rotation = Quaternion.Euler(0, 180, UnityEngine.Random.Range(170f, 190f));
            rotations.Add(rotation);

            if(cardObjects.Count == 2)
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
            yield return StartCoroutine(MoveCardCoroutine(positions[i], cardObjects[i], speed + (i*0.25f), rotations[i]));
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
            Debug.Log("Also inside the loop");
            card.transform.parent = null; // Unparent the card
            //card.transform.parent = playerPoolTransforms[0];
            gameManager.centerCardsObjects.Remove(card);  
        }

        Debug.Log("Also outside the loop");
        ChainMoveCardsToPool(playerPoolTransforms[GetPoolIndex(relativePoolIndex)].position, cardObjects, 10, GetPoolIndex(relativePoolIndex));
        Debug.Log("Also outside the function"); 
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
            Debug.Log(playerParentHands[i].name);
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

