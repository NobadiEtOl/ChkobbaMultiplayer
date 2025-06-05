using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;
using Unity.Netcode;
using UnityEngine.Pool;
using UnityEngine.Tilemaps;

public class Server : NetworkBehaviour
{
    public static Server Singleton { get; private set; } // Singleton instance
    [SerializeField] private NetworkRelay networkRelay; // Reference to the NetworkRelay script
    private Dictionary<int, List<string>> playersHandCardsIDs;//Dictionary containing all the players' hands
    private Dictionary<int, List<string>> playersPooledCardsIDs;//Dictionary containing all the players' pools
    private Dictionary<string, int[]> deckCardsDict; // replaces deckCardsIDs
    public Dictionary<string, int[]> centerCardsDict; // replaces centerCardsIDs
    private Dictionary<string, int[]> allCardLookup = new Dictionary<string, int[]>();
    private int playerCount; // Number of players in the game for the game mode
    private int connectedPlayerCount = 0;
    [SerializeField] private int seed;//Seed for the deck suffle
    private int turnCounter = 0;
    public int currentPlayer;//The player that is currently playing
    int[] points;// To store points for each player
    private int[] piştiCounts;
    public int lastPlayerToCapture = -1;
    private int startingPlayerNo = 0;
    private float timer = 0;
    private float turnTime = 15f; // the time player has before turn skips
    private int roundCount = 0;
    private int readyToEndTurnCounter = 0; //Counter to make sure every connected player is ready to end the turn
    private bool singleDebuggingMode;
    public bool winnerPrintFlag = false;
    // Server.cs
    private Dictionary<string, string> copiedCardMap = new Dictionary<string, string>();

    public void ResetAllServerVariables()
    {
        deckCardsDict = null;
        centerCardsDict = null;
        playersHandCardsIDs = null;
        playersPooledCardsIDs = null;
        //seed = 0;
        turnCounter = 0;
        currentPlayer = 0;
        points = new int[2];
        points[0] = 0; points[1] = 0;
        piştiCounts = new int[2];
        piştiCounts[0] = 0; piştiCounts[1] = 0;
        lastPlayerToCapture = -1;
        startingPlayerNo = 0;
        timer = 0f;
        turnTime = 15f;
        //connectedPlayerCount = 0;
        roundCount = 0;
        readyToEndTurnCounter = 0;
        singleDebuggingMode = false;
        winnerPrintFlag = false;
        copiedCardMap.Clear();
    }

    public void ResetForNewRound()
    {
        deckCardsDict = null;
        centerCardsDict = null;
        playersHandCardsIDs = null;
        playersPooledCardsIDs = null;
        //seed = 0; // Optionally keep or randomize for each round
        turnCounter = 0;
        currentPlayer = 0;
        lastPlayerToCapture = -1;
        timer = 0f;
        turnTime = 15f;
        //connectedPlayerCount = 0;
        readyToEndTurnCounter = 0;
        singleDebuggingMode = false;
        winnerPrintFlag = false;
        copiedCardMap.Clear();
        // DO NOT reset: points, piştiCounts, roundCount, startingPlayerNo
    }

    private void OnEnable()
    {
        Singleton = this;
        if (playerCount == 0) playerCount = 2;
    }
    // Start is called before the first frame update
    void Start()
    {
        print("server.cs start");
        ResetAllServerVariables();
        //StartCoroutine(ServerSubsciribe());

    }

    void Update()
    {
        timer += Time.deltaTime;
        if (winnerPrintFlag)
        {
            DecideWinner();
            winnerPrintFlag = false;
        }
    }

    public void StartGame(int tempPlayerCount)
    {
        timer = 0;
        if (roundCount > 0) ResetForNewRound();

        turnCounter = 0;
        playerCount = tempPlayerCount;
        if (connectedPlayerCount == 1) singleDebuggingMode = true;
        else singleDebuggingMode = false;

        Debug.Log("singleDebuggingMode: " + singleDebuggingMode);

        if (!IsServer)
        {
            Debug.LogError("StartGame() called on a non-server instance!");
            return;
        }
        if (networkRelay == null)
        {
            Debug.LogError("Network relay script empty");
        }
        ServerStart();

        // Define current player and update Client
        currentPlayer = startingPlayerNo % playerCount;
        startingPlayerNo++;
        Debug.LogWarning("Current player: " + currentPlayer);
        Debug.LogWarning("Starting player: " + startingPlayerNo);

        // Initialize the deck and shuffle it
        SaveAllCards();
        SuffleCards(seed);

        // Debug before ClientRpc calls
        Debug.Log("About to call ClientRpc functions.");

        GivePlayerCount();

        // Deal the cards
        InitializePlayerPools();
        if (networkRelay != null)
        {
            Invoke("CallUpdateCurrentPlayer", 1);
            networkRelay.InitializeCardPrefabsClientRPC();
        }
    }

    public void CallUpdateCurrentPlayer()
    {
        networkRelay.UpdateCurrentPlayerClientRPC(currentPlayer);
    }

    private int initialDealCoroutineCheckCounter = 0;
    public void InitialDealCoroutineCheck()
    {
        initialDealCoroutineCheckCounter++;
        if (initialDealCoroutineCheckCounter == connectedPlayerCount)
        {
            StartCoroutine(InitialDealCoroutine());
            initialDealCoroutineCheckCounter = 0;
        }
    }
    private IEnumerator InitialDealCoroutine()
    {
        // Initialize cardObjects

        AudioManager.Instance.PlayAudio(0, 5, false);

        yield return new WaitForSeconds(3.5f);

        DealCardsToCenter();

        yield return new WaitForSeconds(1.25f);

        DealCardsToPlayerHands();
    }
    private void ServerStart()
    {
        if (!IsServer)
        {
            print("Server no open");
        }
        else
        {
            System.Random random = new System.Random(DateTime.Now.Millisecond);
            if (seed == 0) seed = random.Next();

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

    //Add all cards to the deckCardIDs by creating all necessary IDs.
    private void SaveAllCards()
    {
        deckCardsDict = new Dictionary<string, int[]>();
        int cardIndex = 0;
        // Club cards (kind=1, value=1 to 13)
        for (int value = 1; value <= 13; value++)
        {
            string uniqueID = "card_" + cardIndex++;
            deckCardsDict.Add(uniqueID, new int[] { 1, value });
        }
        // Diamond cards (kind=2, value=1 to 13)
        for (int value = 1; value <= 13; value++)
        {
            string uniqueID = "card_" + cardIndex++;
            deckCardsDict.Add(uniqueID, new int[] { 2, value });
        }
        // Heart cards (kind=3, value=1 to 13)
        for (int value = 1; value <= 13; value++)
        {
            string uniqueID = "card_" + cardIndex++;
            deckCardsDict.Add(uniqueID, new int[] { 3, value });
        }
        // Spade cards (kind=4, value=1 to 13)
        for (int value = 1; value <= 13; value++)
        {
            string uniqueID = "card_" + cardIndex++;
            deckCardsDict.Add(uniqueID, new int[] { 4, value });
        }

        allCardLookup = new Dictionary<string, int[]>(deckCardsDict);
    }

    //Suffle the deck according to the seed
    private void SuffleCards(int seed)
    {
        System.Random rng = new System.Random(seed);
        var deckList = new List<KeyValuePair<string, int[]>>(deckCardsDict);
        int count = deckList.Count;
        for (int i = 0; i < count - 1; i++)
        {
            int r = rng.Next(i, count);
            var temp = deckList[i];
            deckList[i] = deckList[r];
            deckList[r] = temp;
        }
        // Rebuild the dictionary in shuffled order
        deckCardsDict = new Dictionary<string, int[]>();
        foreach (var kvp in deckList)
        {
            deckCardsDict[kvp.Key] = kvp.Value;
        }
    }

    //Add a new List<int[]> to the dictionary for each player representing the player pools.
    private void InitializePlayerPools()
    {
        playersPooledCardsIDs = new Dictionary<int, List<string>>();

        for (int i = 0; i < playerCount; i++)
        {
            playersPooledCardsIDs[i] = new List<string>();
        }
    }

    //Add a new List<int[]> to the dictionary for each player representing the player hands.
    private void InitializePlayersHands()
    {
        Debug.LogWarning("InitializePlayersHands called");
        playersHandCardsIDs = new Dictionary<int, List<string>>();

        for (int i = 0; i < playerCount; i++)
        {
            playersHandCardsIDs[i] = new List<string>();
        }
        Debug.LogWarning("InitializePlayersHands finished");
    }

    //Chooses the cards to be dealth to the players
    private void DealCardsToPlayerHands()
    {
        InitializePlayersHands();//With each new deal players has to start with a fresh hand
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < playerCount; j++)
            {
                // Remove from the deck and add to player's hand
                var lastCard = deckCardsDict.Last();
                string uniqueID = lastCard.Key;
                int[] cardID = lastCard.Value;

                playersHandCardsIDs[j].Add(uniqueID);
                deckCardsDict.Remove(uniqueID);
            }
        }

        //Sends players hand to the gameManger so that card objects be given to the players
        SerializableDictionary playersHandCardsIDsSerialized = new SerializableDictionary(playersHandCardsIDs);
        Delayed_DealCardPrefabsToPlayers(playersHandCardsIDsSerialized);
    }

    private void Delayed_DealCardPrefabsToPlayers(SerializableDictionary playersHandCardsIDsSerialized)
    {
        if (IsServer) networkRelay.DealCardPrefabsToPlayersClientRPC(playerCount, playersHandCardsIDsSerialized);
    }

    //Chooses the cards to be dealth to the center
    private void DealCardsToCenter()
    {
        centerCardsDict = new Dictionary<string, int[]>();
        var deckEnum = deckCardsDict.GetEnumerator();
        for (int i = 0; i < 4; i++)
        {
            if (!deckEnum.MoveNext()) break;
            var kvp = deckEnum.Current;
            centerCardsDict[kvp.Key] = kvp.Value;
        }
        // Remove from deck
        foreach (var key in centerCardsDict.Keys)
        {
            deckCardsDict.Remove(key);
        }
        // Send to clients
        SerializableCard serializableCard = new SerializableCard(centerCardsDict);
        networkRelay.UpdateCenterCardIDListClientRPC(serializableCard);
        Delayed_DealCardPrefabsToCenter(serializableCard);
    }

    //******Check if the centerCardIDList in the game manager
    //is updated correctly, if so you dont need to send tempSerializableList
    //to the game mananger and you can use centerCardIDList instead
    private void Delayed_DealCardPrefabsToCenter(SerializableCard tempSerializableCard)
    {
        if (IsServer) networkRelay.DealCardPrefabsToCenterClientRPC(tempSerializableCard);
    }

    //Add played cards to the current players pool.
    public void AddDiscardedCardsToPlayerPool(SerializableCard serializableCard, int playerNumber)
    {
        var discardedDict = serializableCard.ToDictionary();
        foreach (var kvp in discardedDict)
        {
            // kvp.Key is uniqueID, kvp.Value is int[] cardID
            playersPooledCardsIDs[playerNumber].Add(kvp.Key);
            Debug.LogWarning("PlayerNumber: " + playerNumber + " discardedCardID: " + kvp.Value[0] + "_" + kvp.Value[1]);
        }

        int piştiPlayer = 5;
        bool jPistiFlag = false;

        if (discardedDict.Count == 2)
        {
            Debug.LogWarning("Inside Pişti");
            var values = new List<int[]>(discardedDict.Values);
            // If the last two cards have the same value, it's a pişti
            if (values[values.Count - 1][1] == values[values.Count - 2][1])
            {
                Debug.LogWarning("Correct Pişti");
                if (values[values.Count - 1][1] == 11)
                {
                    jPistiFlag = true;
                }
                PlayerPişti(currentPlayer, jPistiFlag);
                piştiPlayer = currentPlayer;
            }
        }

        //networkRelay.PrintPlayerPoolsClientRPC(new SerializableDictionary(playersPooledCardsIDs), piştiPlayer);
    }

    public void EndTurnCheck()
    {
        //if(!singleDebuggingMode)
        //{
        readyToEndTurnCounter++;
        if (readyToEndTurnCounter == connectedPlayerCount)
        {
            EndTurn();
            readyToEndTurnCounter = 0;
        }
        //}
    }
    //Called at the end of each turn
    public void EndTurn()
    {
        //Debug.LogWarning("InsideEndTurn");
        if (turnCounter == 47)
        {
            //Round ends and a winner is decided after each card is played
            DecideWinner();
        }
        else if (turnCounter % (playerCount * 4) == (playerCount * 4) - 1)
        {
            //If each player played their 4 cards new cards are dealt
            Invoke("DealCardsToPlayerHands", 1f);
        }
        NextTurn();

        if (verZehriPending)
        {
            verZehriActive = true;
            verZehriPending = false;
            networkRelay.SetVerZehriActiveClientRPC(true); // Notify clients to start effect
        }
        if (kutsalDestePending)
        {
            kutsalDesteActive = true;
            kutsalDestePending = false;
            networkRelay.SetKutsalDesteActiveClientRPC(true); // Notify clients to start effect
        }
    }

    private void NextTurn()
    {
        turnCounter++;

        currentPlayer = (currentPlayer + 1) % playerCount;

        networkRelay.UpdateCurrentPlayerClientRPC(currentPlayer);
    }

    private void GivePlayerCount()
    {
        networkRelay.GivePlayerCountClientRPC(playerCount);
    }

    public void SkipTurn()
    {
        networkRelay.SkipTurnClientRPC();
    }

    private void DecideWinner()
    {
        AddRemainingCardsToPlayerPool();
        networkRelay.AddRemainingCardsToPoolClientRPC(lastPlayerToCapture);
        roundCount++;
        string roundOverText = "";

        // Variables to track rule comparisons
        int maxCardCount = 0;
        List<int> playerWithMostCards = new List<int>();

        Dictionary<int, List<string>> pooledCards;

        if (playerCount == 4)
        {
            // Combine card pools for teams
            pooledCards = new Dictionary<int, List<string>>
            {
                { 0, playersPooledCardsIDs[0].Concat(playersPooledCardsIDs[2]).ToList() },
                { 1, playersPooledCardsIDs[1].Concat(playersPooledCardsIDs[3]).ToList() }
            };
        }
        else
        {
            pooledCards = playersPooledCardsIDs;
        }

        foreach (var kvp in pooledCards)
        {
            int playerID = kvp.Key;
            List<string> cardList = kvp.Value;
            Debug.Log($"Player {playerID} pooled cards: {string.Join(", ", cardList)}");
            foreach (var card in cardList)
            {
                int[] cardID = allCardLookup[card];
                Debug.Log($"Player {playerID} card: {card} ({cardID[0]}, {cardID[1]})");
            }
        }



        // Iterate through each player's or team's pooled cards
        foreach (var kvp in pooledCards)
        {
            int playerID = kvp.Key;
            List<string> cardList = kvp.Value;

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
            }

            List<string> controlCardList = new List<string>();
            // Calculate points based on card values
            foreach (var card in cardList)
            {
                if (controlCardList.Contains(card))
                {
                    Debug.LogError("Duplicate card found in player's pool: " + card);
                    continue; // Skip duplicate cards
                }
                else
                {
                    controlCardList.Add(card);
                }

                int[] cardID;
                if (copiedCardMap.ContainsKey(card))
                    cardID = allCardLookup[copiedCardMap[card]];
                else
                    cardID = allCardLookup[card];
                int kind = cardID[0];
                int value = cardID[1];

                if (value == 1) // Ace
                {
                    Debug.LogWarning("Player " + playerID + " has an Ace");
                    points[playerID]++;
                }
                else if (value == 11) // Jack
                {
                    Debug.LogWarning("Player " + playerID + " has a Jack");
                    points[playerID]++;
                }
                else if (kind == 1 && value == 2) // 2 of Clubs
                {
                    Debug.LogWarning("Player " + playerID + " has a 2 of Clubs");
                    points[playerID] += 2;
                }
                else if (kind == 2 && value == 10) // 10 of Diamonds
                {
                    Debug.LogWarning("Player " + playerID + " has a 10 of Diamonds");
                    points[playerID] += 3;
                }
            }
        }

        // Add 3-point bonus for most cards
        if (playerWithMostCards.Count == 1)
        {
            Debug.LogWarning("Player with most cards: " + playerWithMostCards[0]);
            points[playerWithMostCards[0]] += 3;
        }

        // Collect all pooled cards across all players
        var allPooledCards = pooledCards.SelectMany(kvp => kvp.Value).ToList();
        var duplicateCards = allPooledCards.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicateCards.Count > 0)
            Debug.LogError("DUPLICATE CARDS ACROSS POOLS: " + string.Join(", ", duplicateCards));

        // Determine the winner (max points)
        int maxPoints = -1;
        List<int> winnerIDs = new List<int>();

        for (int i = 0; i < 2; i++)
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
            if (playerCount == 4)
            {
                roundOverText += $"Team {i + 1} has {points[i]} points!";
                roundOverText += "\n";
            }
            else
            {
                roundOverText += $"Player {i + 1} has {points[i]} points!";
                roundOverText += "\n";
            }
        }
        roundOverText += "\n";

        int winnerSide = -1;

        if (points[0] >= 100 || points[1] >= 100)
        {
            if (playerCount == 4)
            {
                if (points[0] == points[1])
                {
                    roundOverText += $"Both teams win!!";
                    winnerSide = 3;
                }
                else
                {
                    roundOverText += $"Team {winnerIDs[0] + 1} wins";
                    winnerSide = winnerIDs[0];
                }
            }
            else
            {
                if (points[0] == points[1])
                {
                    roundOverText += $"Both players win!!";
                    winnerSide = 3;
                }
                else
                {
                    roundOverText += $"Player {winnerIDs[0] + 1} wins";
                    winnerSide = winnerIDs[0];
                }
            }
        }
        else
        {
            roundOverText += $"\n\nWaiting for another round to start";
        }

        Debug.LogWarning(points[0] + "_" + points[1]);

        networkRelay.PrintPlayerPoolsClientRPC(new SerializableDictionary(playersPooledCardsIDs), 5);

        SendWinScreen(roundOverText, winnerSide, points[0], points[1]);

        if (winnerSide == -1)
        {
            Invoke("StartGameAutomatic", 10f);
        }
    }

    private void StartGameAutomatic()
    {
        if (timer >= 11)
        {
            StartGame(playerCount);
            Debug.LogWarning("StartGameAutomatic called");
        }

    }

    private void SendWinScreen(string message, int winnerSide, int point0, int point1)
    {
        networkRelay.ShowWinScreenClientRPC(message, winnerSide, point0, point1);
    }

    //Add remaining cards in the center to the pool of the player who last captured a card.
    public void AddRemainingCardsToPlayerPool()
    {
        if (centerCardsDict == null) return;
        foreach (var kvp in centerCardsDict)
        {
            Debug.LogWarning("Adding remaining card to player pool: " + kvp.Key);
            if (playersPooledCardsIDs[lastPlayerToCapture].Contains(kvp.Key))
                Debug.LogError("DUPLICATE ADD TO POOL: " + kvp.Key);
            playersPooledCardsIDs[lastPlayerToCapture].Add(kvp.Key);
        }
    }

    public void PrintCenterCards()
    {
        print("CenterCards:");
        if (centerCardsDict != null)
        {
            foreach (var kvp in centerCardsDict)
            {
                print($"{kvp.Key}: {kvp.Value[0]}_{kvp.Value[1]}");
            }
        }
    }

    public void PlayerPişti(int playerID, bool jPiştiFlag)
    {
        Debug.LogWarning("Player " + playerID + " Pişti");
        if (playerCount == 2)
        {
            if (jPiştiFlag) points[playerID] += 20;
            else points[playerID] += 10;
            piştiCounts[playerID]++;
        }

        if (playerCount == 4)
        {
            if (playerID == 0 || playerID == 2)
            {
                if (jPiştiFlag) points[0] += 20;
                else points[0] += 10;
                piştiCounts[0]++;
            }
            else if (playerID == 1 || playerID == 3)
            {
                if (jPiştiFlag) points[1] += 20;
                else points[1] += 10;
                piştiCounts[1]++;
            }
        }
    }
    public void RemoveCardsFromCenter(SerializableCard serializableCard)
    {
        var cardsToRemove = serializableCard.ToDictionary();
        foreach (var key in cardsToRemove.Keys)
        {
            centerCardsDict.Remove(key);
        }
        networkRelay.UpdateCenterCardIDListClientRPC(new SerializableCard(centerCardsDict));
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
        if (list == null || list.Count == 0)
        {
            //Debug.Log("List is empty");
            return;
        }
        int counter = 0;
        foreach (var array in list)
        {
            //Debug.Log("---------------------"); 
            //Debug.Log("Array[" + counter + "]: [" + array[0] + "," + array[1] + "]");
            counter++;
        }
    }

    public void GetMove(string selectedHandCardUniqueID, SerializableCard serializableCard, int playerNumber, int sumValue)
    {
        // selectedHandCardUniqueID is the uniqueID of the played card
        Debug.LogWarning("GetMove called with selectedHandCardUniqueID: " + selectedHandCardUniqueID);

        int[] selectedHandCard = allCardLookup[selectedHandCardUniqueID];
        // Kapkaç: force this card to capture (as if it was a Jack)
        if (kapkacCount > 0)
        {
            Debug.LogWarning("Kapkac active, forcing capture with Jack");
            sumValue = selectedHandCard[1]; // Jack value
            kapkacCount = 0;
            networkRelay.SetKapkacActiveClientRPC(false);
        }
        // Oynayamazsın: force this card to be blocked (add to center, no capture)
        else if (blockCount > 0)
        {
            Debug.LogWarning("Oynayamazsın active, blocking card");
            sumValue = 0;
            blockCount = 0;
            networkRelay.SetOynayamazsinActiveClientRPC(false);
        }

        // Get the cardID for rules
        Debug.LogWarning("Selected hand card: " + selectedHandCard[0] + "_" + selectedHandCard[1]);
        Debug.LogWarning("Sum value: " + sumValue);

        if (selectedHandCard[1] == sumValue || (selectedHandCard[1] == 11 && sumValue != 0))
        {
            RemoveCardsFromCenter(serializableCard);
            // Add the played card to the serializableCard for pool addition
            var updatedDict = serializableCard.ToDictionary();
            updatedDict[selectedHandCardUniqueID] = selectedHandCard;
            AddDiscardedCardsToPlayerPool(new SerializableCard(updatedDict), playerNumber);
            updatedDict.Remove(selectedHandCardUniqueID); // Remove again if needed
            networkRelay.SendMoveToClientRPC(selectedHandCardUniqueID, new SerializableCard(updatedDict), playerNumber);
            lastPlayerToCapture = playerNumber;

            if (verZehriActive)
            {
                int team = (playerNumber % 2);
                points[team] -= 5;
                networkRelay.ShowVerZehriEffectClientRPC(playerNumber, -5);
                verZehriActive = false;
                networkRelay.SetVerZehriActiveClientRPC(false); // Notify clients to stop effect
            }
            if (kutsalDesteActive)
            {
                int team = (playerNumber % 2);
                points[team] += 5;
                networkRelay.ShowKutsalDesteEffectClientRPC(playerNumber, 5);
                kutsalDesteActive = false;
                networkRelay.SetKutsalDesteActiveClientRPC(false); // Notify clients to stop effect
            }
        }
        else
        {
            AddCardIDToCenter(selectedHandCardUniqueID, selectedHandCard);
        }
        //if(singleDebuggingMode)EndTurn();
    }

    public void AddCardIDToCenter(string uniqueID, int[] cardID)
    {
        Debug.LogWarning("AddCardIDToCenter called with cardID: " + cardID[0] + "_" + cardID[1]);
        centerCardsDict[uniqueID] = cardID;
        networkRelay.UpdateCenterCardIDListClientRPC(new SerializableCard(centerCardsDict));
        networkRelay.SendCardAddedToCenterClientRPC(uniqueID, cardID);
        //EndTurn();
    }

    public int SendPlayerNumber()
    {
        return NetworkManager.Singleton.ConnectedClients.Count - 1;
    }

    public void AnotherPlayerConnected(ulong clientId)
    {
        Debug.Log("Inside AnotherPlayerConnected");
        networkRelay.GetPlayerNumberClientRPC(clientId, connectedPlayerCount);
        connectedPlayerCount++;
        Debug.Log("connectedPlayerCount: " + connectedPlayerCount);
        Debug.Log("playerCount: " + playerCount);

        if (playerCount == connectedPlayerCount)
        {
            if (playerCount == 2)
            {
                StartGameAfterDelayTwoPlayer();
            }
            else if (playerCount == 4)
            {
                StartGameAfterDelayFourPlayer();
            }
        }
    }
    public void StartGameAfterDelayFourPlayer()
    {
        Invoke("StartGameDelayedFourPlayer", 3f);
    }
    [ContextMenu("StartGameDelayedFourPlayer")]
    private void StartGameDelayedFourPlayer()
    {
        StartGame(4);
    }
    public void StartGameAfterDelayTwoPlayer()
    {
        Invoke("StartGameDelayedTwoPlayer", 3f);
    }
    [ContextMenu("StartGameDelayedTwoPlayer")]
    private void StartGameDelayedTwoPlayer()
    {
        StartGame(2);
    }

    public void SetPlayerCount(int playerCountVar)
    {
        playerCount = playerCountVar;
    }

    [ContextMenu("PrintPlayerPools")]
    public void CallPrintPlayerPools()
    {
        networkRelay.PrintPlayerPoolsClientRPC(new SerializableDictionary(playersPooledCardsIDs), 5);
    }

    /// <summary>
    /// Swaps cards between two players in the server's hand data.
    /// </summary>
    public void SwapCardsBetweenPlayersOnServer(int playerANo, int cardAIndex, int playerBNo, int cardBIndex)
    {
        if (playersHandCardsIDs == null) return;
        if (!playersHandCardsIDs.ContainsKey(playerANo) || !playersHandCardsIDs.ContainsKey(playerBNo)) return;

        var handA = playersHandCardsIDs[playerANo];
        var handB = playersHandCardsIDs[playerBNo];

        if (handA.Count <= cardAIndex || handB.Count <= cardBIndex) return;

        // Swap the cards in the server's hand data
        string temp = handA[cardAIndex];
        handA[cardAIndex] = handB[cardBIndex];
        handB[cardBIndex] = temp;

        Debug.LogWarning($"Server swapped card {cardAIndex} of player {playerANo} with card {cardBIndex} of player {playerBNo}");
    }

    public void RegisterCopiedCard(string targetUniqueID, string sourceUniqueID)
    {
        copiedCardMap[targetUniqueID] = sourceUniqueID;
    }

    public List<string> GetPlayerHand(int playerNo)
    {
        if (playersHandCardsIDs != null && playersHandCardsIDs.ContainsKey(playerNo))
            return new List<string>(playersHandCardsIDs[playerNo]);
        return new List<string>();
    }

    public void BombaCenter()
    {
        // Remove all cards from the center (do NOT add to any player's pool)
        if (centerCardsDict != null)
            centerCardsDict.Clear();

        // Notify all clients to update their view
        networkRelay.BombaClientRPC();
    }

    private bool isYapamazsınActive = false;
    public void ActivateYapamazsın()
    {
        isYapamazsınActive = true;
        networkRelay.SetYapamazsınActiveClientRPC(true);
    }
    public bool TryBlockPower()
    {
        if (isYapamazsınActive)
        {
            isYapamazsınActive = false;
            networkRelay.SetYapamazsınActiveClientRPC(false);
            return true; // Blocked
        }
        return false; // Not blocked
    }

    private int kapkacCount = 0;
    private int blockCount = 0;

    public void ActivateKapkac()
    {
        kapkacCount = 1;
        networkRelay.SetKapkacActiveClientRPC(true);
    }

    public void ActivateOynayamazsin()
    {
        blockCount = 1;
        networkRelay.SetOynayamazsinActiveClientRPC(true);
    }

    private bool verZehriActive = false;
    private bool kutsalDesteActive = false;
    private bool verZehriPending = false;
    private bool kutsalDestePending = false;

    public void ActivateVerZehri()
    {
        verZehriPending = true;
    }

    public void ActivateKutsalDeste()
    {
        kutsalDestePending = true;
    }

    public void BuDahaIyiSwap(int playerNo, string handCardID, string centerCardID)
    {
        // Swap in player's hand
        if (playersHandCardsIDs[playerNo].Contains(handCardID))
        {
            playersHandCardsIDs[playerNo].Remove(handCardID);
            playersHandCardsIDs[playerNo].Add(centerCardID);
        }
        // Swap in center
        if (centerCardsDict.ContainsKey(centerCardID))
        {
            int[] temp = centerCardsDict[centerCardID];
            centerCardsDict.Remove(centerCardID);
            centerCardsDict[handCardID] = allCardLookup[handCardID];
        }
        else if (centerCardsDict.ContainsKey(handCardID))
        {
            int[] temp = centerCardsDict[handCardID];
            centerCardsDict.Remove(handCardID);
            centerCardsDict[centerCardID] = allCardLookup[centerCardID];
        }
        else
        {
            // Fallback: just swap the last card
            var lastKey = centerCardsDict.Keys.Last();
            int[] temp = centerCardsDict[lastKey];
            centerCardsDict.Remove(lastKey);
            centerCardsDict[handCardID] = allCardLookup[handCardID];
        }
    }

}   
