using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;
using Unity.Netcode;
using UnityEngine.Pool;

public class Server : NetworkBehaviour
{
    private List<int[]> deckCardsIDs;//List of all the cardIDs represents the deck  
    private Dictionary<int, List<int[]>> playersHandCardsIDs;//Dictionary containing all the players' hands
    public SerializableDictionary playersHandCardsIDsSerialized = new SerializableDictionary();
    public SerializableList tempSerializableList = new SerializableList();
    private Dictionary<int, List<int[]>> playersPooledCardsIDs;//Dictionary containing all the players' pools
    [SerializeField]public int playerCount=0;
    public List<int[]> centerCardsIDs;//List of all the cards in the center
    [SerializeField]private int seed = 124;//Seed for the deck suffle
    private int turnCounter=0;
    public int currentPlayer;//The player that is currently playing
    //public bool dealFlag = false; //Used to check when the cards should be dealt
    int[] points;  // To store points for each player
    public int lastPlayerToCapture=1;
    public static Server Singleton { get; private set; }
    [SerializeField]private NetworkRelay networkRelay;
    private void OnEnable()
    {
        Singleton = this;
        if(playerCount==0)playerCount=2;
    }
    // Start is called before the first frame update
    void Start()
    {
        print("server.cs start");
    }

    /*[SerializeField]private GameObject multiplayerObjectPrefab;
    public void InstantiateMultiplayerObject()
    {
        GameObject newObject = Instantiate(multiplayerObjectPrefab, new Vector3(960, 540, 0), Quaternion.identity);
        newObject.GetComponent<NetworkObject>().Spawn();
    }*/
    public void StartGame(int tempPlayerCount)
    {
        playerCount = tempPlayerCount;
        if (!IsServer)
        {
            Debug.LogError("StartGame() called on a non-server instance!");
            return;
        }
        if(networkRelay==null)
        {
            Debug.LogError("Network relay script empty");
        }
        ServerStart();

        // Define current player and update Client
        currentPlayer = 0;

        // Initialize the deck and shuffle it
        SaveAllCards();
        SuffleCards(seed);

        // Debug before ClientRpc calls
        Debug.Log("About to call ClientRpc functions.");

        // Initialize cardObjects
        if (networkRelay != null)
        {
            networkRelay.UpdateCurrentPlayerClientRPC(currentPlayer);
            networkRelay.InitializeCardPrefabsClientRPC();
        }
        else
        {
            Debug.LogError("NetworkRelay is null!");
        }

        // Deal the cards
        InitializePlayerPools();
        StartCoroutine(InitialDealCoroutine());
    }

    private IEnumerator InitialDealCoroutine()
    {
        DealCardsToCenter();

        yield return new WaitForSeconds(2);

        DealCardsToPlayerHands();
    }
    private void ServerStart()
    {
        if(!IsServer)
        {
            print("Server no open");
        }
        else
        {
            System.Random random = new System.Random(DateTime.Now.Millisecond);
            seed=random.Next();
            seed=1234;
            points = new int[playerCount];
            Debug.Log("NetworkManager State: " + NetworkManager.Singleton.NetworkConfig.NetworkTransport);
            //Invoke("StartGame",0f);
        }

        if (networkRelay != null)
        {   
            print("networkRelay is not null");
            DelayedMessageSend();
        }
        else
        {
            print("NetworkRelay is null.");
        }
    }

    private void DelayedMessageSend()
    {
        print("DelayedMessageSend");
        networkRelay.PrintMessageServerRPC("message sent");
    }

    public bool winnerPrintFlag = false;
    void Update()
    {
        if(winnerPrintFlag)
        {
            DecideWinner();
            winnerPrintFlag = false;
        }
    }

    //Add all cards to the deckCardIDs by creating all necessary IDs.
    private void SaveAllCards()
    {
        deckCardsIDs = new List<int[]>();
         // Carreau cards (kind=1, value=1 to 10)
        for (int value = 1; value <= 10; value++)
        {
            deckCardsIDs.Add(new int[] { 1, value });
        }

        // Coeur cards (kind=2, value=1 to 10)
        for (int value = 1; value <= 10; value++)
        {
            deckCardsIDs.Add(new int[] { 2, value });
        }

        // Pique cards (kind=3, value=1 to 10)
        for (int value = 1; value <= 10; value++)
        {
            deckCardsIDs.Add(new int[] { 3, value });
        }

        // Trefle cards (kind=4, value=1 to 10)
        for (int value = 1; value <= 10; value++)
        {
            deckCardsIDs.Add(new int[] { 4, value });
        }
    }

    //Suffle the deck according to the seed
    private void SuffleCards(int seed)
    {
        System.Random rng = new System.Random(seed);
        int count = deckCardsIDs.Count;
        
        for (int i = 0; i < count - 1; i++)
        {
            int r = rng.Next(i, count);  // Random number from i to count - 1
            int[] temp = deckCardsIDs[i];
            deckCardsIDs[i] = deckCardsIDs[r];
           deckCardsIDs [r] = temp;
        }
        //Debug.Log("Deck shuffled with seed: " + seed);
    }
    
    //Add a new List<int[]> to the dictionary for each player representing the player pools.
    private void InitializePlayerPools()
    {
        playersPooledCardsIDs = new Dictionary<int, List<int[]>>();

        for(int i=0; i<playerCount; i++)
        {
            playersPooledCardsIDs[i] = new List<int[]>();
        }
    }

    //Add a new List<int[]> to the dictionary for each player representing the player hands.
    private void InitializePlayersHands()
    {
        playersHandCardsIDs = new Dictionary<int, List<int[]>>();

        for(int i=0; i<playerCount; i++)
        {
            playersHandCardsIDs[i] = new List<int[]>();
        }
    }

    //Chooses the cards to be dealth to the players
    private void DealCardsToPlayerHands()
    {
        InitializePlayersHands();//With each new deal players has to start with a fresh hand
        for(int i=0; i<3; i++)
        {
            for(int j=0; j<playerCount; j++)
            {
                //Removes from the deck and adds to players hand
                int[] tempCardID = deckCardsIDs[deckCardsIDs.Count-1];
                playersHandCardsIDs[j].Add(tempCardID);
                deckCardsIDs.RemoveAt(deckCardsIDs.Count-1);
                print(tempCardID[0] + "_" + tempCardID[1]);
                //Add functions to run animations
            }
        }

        //Sends players hand to the gameManger so that card objects be given to the players
        playersHandCardsIDsSerialized = new SerializableDictionary(playersHandCardsIDs);
        Delayed_DealCardPrefabsToPlayers();
    }

    private void Delayed_DealCardPrefabsToPlayers()
    {
        if(IsServer)networkRelay.DealCardPrefabsToPlayersClientRPC(playerCount, playersHandCardsIDsSerialized);
    }

    //Chooses the cards to be dealth to the center
    private void DealCardsToCenter()
    {
        centerCardsIDs = new List<int[]>();
        for(int i=0; i<4; i++)
        {
            //Removes from the deck and adds to center
            int[] tempCardID = deckCardsIDs[deckCardsIDs.Count-1];
            centerCardsIDs.Add(tempCardID);
            deckCardsIDs.RemoveAt(deckCardsIDs.Count-1);
        }
        //Sends center cards to the gameManger so that card objects be put to the center
        tempSerializableList = new SerializableList(centerCardsIDs);
        networkRelay.UpdateCenterCardIDListClientRPC(tempSerializableList);
        //tempSerializableList.PrintAll();
        //Invoke(nameof(Delayed_DealCardPrefabsToCenter),0.5f);
        Delayed_DealCardPrefabsToCenter();
        //Instance is the probable cause of my problems
    }

    //******Check if the centerCardIDList in the game manager
    //is updated correctly, if so you dont need to send tempSerializableList
    //to the game mananger and you can use centerCardIDList instead
    private void Delayed_DealCardPrefabsToCenter()
    {
        if(IsServer)networkRelay.DealCardPrefabsToCenterClientRPC(tempSerializableList);
    }

    //Add played cards to the current players pool.
    public void AddDiscardedCardsToPlayerPool(SerializableList serializableList)
    {
        List<int[]> discardedCardIDs = serializableList.ToList(); 
        foreach(int[] discardedCardID in discardedCardIDs)
        {
            playersPooledCardsIDs[currentPlayer].Add(discardedCardID);
        }

        int chkobbaPlayer=5;
        if(centerCardsIDs.Count==0)
        {
            Debug.Log("Chkobba Player " + currentPlayer);
            PlayerChkobba(currentPlayer);
            chkobbaPlayer=currentPlayer;
        }
        networkRelay.PrintPlayerPoolsClientRPC(new SerializableDictionary(playersPooledCardsIDs), chkobbaPlayer);
    }
    
    //Called at the end of each turn
    public void EndTurn()
    {
        if(turnCounter == 35)
        {   
            //Round ends and a winner is decided after each card is played
            DecideWinner();
        }
        else if(turnCounter%(playerCount*3)==(playerCount*3)-1)
        {
            //If each player played their 3 cards new cards are dealt
            DealCardsToPlayerHands();
        }
        NextTurn();
    }

    private void NextTurn()
    {
        turnCounter++;

        int nextPlayer = (turnCounter%playerCount);
        currentPlayer = nextPlayer;
        networkRelay.UpdateCurrentPlayerClientRPC(currentPlayer);
        print("TurnCounter:"  + turnCounter);
        //PrintCenterCards();
    }

    private void DecideWinner()
    {
        AddRemainingCardsToPlayerPool();
        string roundOverText = "";

        // Variables to track rule comparisons
        int maxCardCount = 0;
        int maxDiamonds = 0;
        int maxSevens = 0;

        // Temporary variables to track the player or team with most cards/diamonds/sevens
        List<int> playerWithMostCards = new List<int>();
        List<int> playerWithMostDiamonds = new List<int>();
        List<int> playerWithMostSevens = new List<int>();
        List<int> playerWithSevenOfDiamonds = new List<int>();

        Dictionary<int, List<int[]>> pooledCards;

        if (playerCount == 4)
        {
            // Combine card pools for teams
            pooledCards = new Dictionary<int, List<int[]>>
            {
                { 0, playersPooledCardsIDs[0].Concat(playersPooledCardsIDs[2]).ToList() },
                { 1, playersPooledCardsIDs[1].Concat(playersPooledCardsIDs[3]).ToList() }
            };
        }
        else
        {
            pooledCards = playersPooledCardsIDs;
        }

        // Iterate through each player's or team's pooled cards
        foreach (var kvp in pooledCards)
        {
            int playerID = kvp.Key;
            List<int[]> cardList = kvp.Value;

            // Rule 1: Count cards
            int cardCount = cardList.Count;
            if (cardCount >= maxCardCount)
            {
                if (cardCount == maxCardCount)
                {
                    playerWithMostCards.Add(playerID);
                }
                else
                {
                    playerWithMostCards.Clear();
                    playerWithMostCards.Add(playerID);
                    maxCardCount = cardCount;
                }
                print(playerID + " has the most Cards");
            }

            // Rule 2: Count diamonds and sevens
            int diamondsCount = 0;
            int sevensCount = 0;
            bool hasSevenOfDiamonds = false;

            foreach (var card in cardList)
            {
                int kind = card[0];
                int value = card[1];

                if (kind == 1) // Diamonds (kind = 1)
                {
                    diamondsCount++;
                    if (value == 7)
                    {
                        hasSevenOfDiamonds = true;
                        print(playerID + " has the seven of diamonds");
                    }
                }

                if (value == 7) // Count Sevens (any kind of seven)
                {
                    sevensCount++;
                }
            }

            print(playerID + " has " + diamondsCount + " diamonds");
            print(playerID + " has " + sevensCount + " sevens");

            // Track most diamonds
            if (diamondsCount >= maxDiamonds)
            {
                if (diamondsCount == maxDiamonds)
                {
                    playerWithMostDiamonds.Add(playerID);
                }
                else
                {
                    playerWithMostDiamonds.Clear();
                    playerWithMostDiamonds.Add(playerID);
                    maxDiamonds = diamondsCount;
                }
            }

            // Track most sevens
            if (sevensCount >= maxSevens)
            {
                if (sevensCount == maxSevens)
                {
                    playerWithMostSevens.Add(playerID);
                }
                else
                {
                    playerWithMostSevens.Clear();
                    playerWithMostSevens.Add(playerID);
                    maxSevens = sevensCount;
                }
            }

            // Track seven of diamonds
            if (hasSevenOfDiamonds)
            {
                playerWithSevenOfDiamonds.Add(playerID);
            }
        }

        if (playerWithMostCards.Count == 1)
        {
            points[playerWithMostCards[0]]++;
        }
        if (playerWithMostDiamonds.Count == 1)
        {
            points[playerWithMostDiamonds[0]]++;
        }
        if (playerWithSevenOfDiamonds.Count == 1)
        {
            points[playerWithSevenOfDiamonds[0]]++;
        }
        if (playerWithMostSevens.Count == 1)
        {
            points[playerWithMostSevens[0]]++;
        }

        // Determine the winner (max points)
        int maxPoints = -1;
        List<int> winnerIDs = new List<int>();

        for (int i = 0; i < (playerCount == 4 ? 2 : playerCount); i++)
        {
            if (points[i] >= maxPoints)
            {
                if (points[i] == maxPoints)
                {
                    winnerIDs.Add(i);
                }
                else
                {
                    winnerIDs.Clear();
                    winnerIDs.Add(i);
                    maxPoints = points[i];
                }
            }
            if(playerCount==4)
            {
                roundOverText += $"Team {i+1} has {points[i]} points!";
                roundOverText += "\n";
            }
            else
            {
                roundOverText += $"Player {i+1} has {points[i]} points!";
                roundOverText += "\n";
            }
        }
        roundOverText += "\n";
        roundOverText += "\n";
        roundOverText += "\n";
        roundOverText += "\n";
        roundOverText += "\n";
        roundOverText += "\n";

        if (playerCount == 4)
        {
            roundOverText += $"Team {winnerIDs[0]+1} wins with {maxPoints} points!";
        }
        else
        {
            roundOverText += $"Player {winnerIDs[0]+1} wins with {maxPoints} points!";
        }

        SendWinScreen(roundOverText);
    }

    private void SendWinScreen(string message)
    {
        networkRelay.ShowWinScreenClientRPC(message);
    }

    //Add remaining cards in the center to the pool of the player who last captured a card.
    public void AddRemainingCardsToPlayerPool()
    {
        foreach(int[] remainingCardsID in centerCardsIDs)
        {
            playersPooledCardsIDs[lastPlayerToCapture].Add(remainingCardsID);
        }

        networkRelay.PrintPlayerPoolsClientRPC(new SerializableDictionary(playersPooledCardsIDs),5);
    }

    public void PrintCenterCards()
    {
        print("CenterCards:");
        foreach(int[] cardID in centerCardsIDs)
        {
            print(cardID[0] + "_" + cardID[1]);
        }
    }

    public void PlayerChkobba(int playerID)
    {
        Debug.LogWarning("Player " + playerID + " Chkobba");
        if(playerCount==2)
        {
            points[playerID]++;
        }

        if(playerCount==4)
        {
            if(playerID==0 || playerID==2)
            {
                points[0]++;
            }
            else if(playerID==1 || playerID==3)
            {
                points[1]++;
            }
        }
    }

    public void RemoveCardsFromCenter(SerializableList serializableList)
    {
        List<int[]> cardsToRemove = serializableList.ToList();
        foreach (int[] cardToBeRemoved in cardsToRemove)
        {
            centerCardsIDs.RemoveAll(card => card.SequenceEqual(cardToBeRemoved));
        }
        networkRelay.UpdateCenterCardIDListClientRPC(new SerializableList(centerCardsIDs));
    }

    public void PrintMessage(string message)
    {
        print(message);
    }

    public static void PrintDictionary(Dictionary<int, List<int[]>> dictionary)
    {
        if (dictionary == null || dictionary.Count == 0)
        {
            Debug.Log("Dictionary is empty.");
            return;
        }

        foreach (var kvp in dictionary)
        {
            Debug.Log($"Key: {kvp.Key}");
            Debug.Log("Values:");
            foreach (var array in kvp.Value)
            {
                string arrayContents = string.Join(", ", array);
                Debug.Log($"  [{arrayContents}]");
            }
        }
    }

    public static void PrintList(List<int[]> list)
    {
        if(list == null || list.Count == 0)
        {
            Debug.Log("List is empty");
            return;
        }
        int counter=0;
        foreach(var array in list)
        {
            Debug.Log("---------------------"); 
            Debug.Log("Array[" + counter + "]: [" + array[0] + "," + array[1] + "]");
            counter++;
        }
    }

    public void GetMove(int[] selectedHandCard, SerializableList serializableList, int playerNumber)
    {
        networkRelay.SendMoveToClientRPC(selectedHandCard, serializableList, playerNumber);
        lastPlayerToCapture = playerNumber;
        serializableList.Add(selectedHandCard);
        AddDiscardedCardsToPlayerPool(serializableList);
        EndTurn();
    }

    public void AddCardIDToCenter(int[] cardID)
    {
        centerCardsIDs.Add(cardID);
        networkRelay.UpdateCenterCardIDListClientRPC(new SerializableList(centerCardsIDs));
        networkRelay.SendCardAddedToCenterClientRPC(cardID);
        EndTurn();
    }

    public int SendPlayerNumber()
    {
        return NetworkManager.Singleton.ConnectedClients.Count-1;
    }

}
