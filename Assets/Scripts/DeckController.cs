using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeckController : MonoBehaviour
{
    [SerializeField]private GameManager gameManager;
    [SerializeField] private GameObject[] playerParentHands = new GameObject[4];//Array of player hand objects to keep track of where the card objects will be placed
    [SerializeField] private GameObject centerParent;//Center object to keep track of where the center cards will be placed
    [SerializeField] private GameObject addCardPrefab;//Prefab of the add button
    [SerializeField] private List<GameObject> cardPrefabsList;//Prefabs of all the cards.
    [SerializeField] private Dictionary<string, GameObject> cardPrefabs;//A dictionary to keep track of each card prefabs with its ID
    private Dictionary<string, List<GameObject>> cardPools;//A dictionary of card ID and a list of all the instantiated cards
    private List<GameObject> activeCards = new List<GameObject>();
    private List<CardInteraction> cardInteractionList; // List to store CardInteraction references

    void Start()
    {
        cardPrefabs = new Dictionary<string, GameObject>();
        cardPools = new Dictionary<string, List<GameObject>>();
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
                card.SetActive(false); // Deactivate the card
                card.transform.position = new Vector3(-2000, -2000, 0); // Place off-screen

                // Get the CardInteraction component and add it to the list
                CardInteraction cardInteraction = card.GetComponent<CardInteraction>();
                if (cardInteraction != null)
                {
                    cardInteractionList.Add(cardInteraction); // Store the reference in the list
                }
                
                cardPools[cardIDString].Add(card);
            }
        }
    }

    //Deals to players according to the playerCount
    public void DealPlayers(int playerCount, Dictionary<int, List<int[]>> playerHands)
    {
        if (playerCount == 2 || playerCount == 4)
        {
            for (int i = 0; i < playerCount; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    var cardID = playerHands[i][j];
                    //Gets the card object to be dealt according to the cardID
                    GameObject tempCardObject = GetCardFromPool(cardID);
                    if (tempCardObject != null)
                    {
                        Vector3 position = Vector3.zero;
                        Quaternion rotation = Quaternion.identity;

                        if (playerCount == 2)
                        {
                            position = new Vector3(j * 700, (2*i-1) * 2000, 0);
                        }
                        else
                        {
                            switch (i)
                            {
                                case 0:
                                    position = new Vector3(j * 700 + 200, -2000, 0);
                                    break;
                                case 1:
                                    position = new Vector3(5000, j * 700 - 500, 0);
                                    rotation = Quaternion.Euler(0, 0, 90);
                                    break;
                                case 2:
                                    position = new Vector3(j * 700 + 200, 2000, 0);
                                    break;
                                case 3:
                                    position = new Vector3(-2500, j * 700 - 500, 0);
                                    rotation = Quaternion.Euler(0, 0, 90);
                                    break;
                            }
                        }

                        tempCardObject.transform.position = position;
                        tempCardObject.transform.rotation = rotation;
                        //Set parent of the card
                        tempCardObject.transform.parent = playerParentHands[i].transform;
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Player number is different from 2 or 4");
        }
    }

    //Deals to center according to the playerCount
    public void DealCenter(List<int[]> centerCardIDs)
    {
        //Adds the add butoon before adding the center cards.
        GameObject addCardObject = Instantiate(addCardPrefab, new Vector3(-700, 0, 0), Quaternion.identity);
        cardInteractionList.Add(addCardObject.GetComponent<CardInteraction>());
        SendCardInteractionsToGameManager();
        addCardObject.transform.parent = centerParent.transform;

        for (int i = 0; i < 4; i++)
        {
            var cardID = centerCardIDs[i];
            GameObject tempCenterCard = GetCardFromPool(cardID);
            if (tempCenterCard != null)
            {
                tempCenterCard.transform.position = new Vector3(i * 700, 0, 0);
                tempCenterCard.transform.rotation = Quaternion.identity;
                tempCenterCard.transform.parent = centerParent.transform;
                gameManager.centerCardsObjects.Add(tempCenterCard);
                gameManager.centerCards.Add(cardID);
            }
        }
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
}


/*public class IntArrayComparer : IEqualityComparer<int[]>
{
    public bool Equals(int[] x, int[] y)
    {
        if (x == null || y == null) return false;
        if (x.Length != y.Length) return false;
        for (int i = 0; i < x.Length; i++)
        {
            if (x[i] != y[i]) return false;
        }
        return true;
    }

    public int GetHashCode(int[] obj)
    {
        if (obj == null) return 0;
        int hash = 17;
        foreach (var item in obj)
        {
            hash = hash * 31 + item;
        }
        return hash;
    }
}*/
