using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;
using System;

[Serializable]
public struct SerializableDictionary : INetworkSerializable
{
    private List<int> keys;              // List to store dictionary keys
    private List<List<int[]>> values;    // List to store dictionary values (nested lists)

    // Constructor to initialize from a regular dictionary
    public SerializableDictionary(Dictionary<int, List<int[]>> dictionary)
    {
        keys = new List<int>();
        values = new List<List<int[]>>();

        foreach (var kvp in dictionary)
        {
            keys.Add(kvp.Key);

            // Deep copy of the nested list
            List<int[]> deepCopiedList = new List<int[]>();
            foreach (var array in kvp.Value)
            {
                deepCopiedList.Add((int[])array.Clone());
            }
            values.Add(deepCopiedList);
        }
    }

    // Convert back to a regular dictionary
    public Dictionary<int, List<int[]>> ToDictionary()
    {
        Dictionary<int, List<int[]>> dictionary = new Dictionary<int, List<int[]>>();
        for (int i = 0; i < keys.Count; i++)
        {
            dictionary[keys[i]] = values[i];
        }
        return dictionary;
    }

    // Add key-value pair
    public void Add(int key, List<int[]> value)
    {
        keys.Add(key);

        // Deep copy of the nested list
        List<int[]> deepCopiedList = new List<int[]>();
        foreach (var array in value)
        {
            deepCopiedList.Add((int[])array.Clone());
        }
        values.Add(deepCopiedList);
    }

    // Clear the dictionary
    public void Clear()
    {
        keys.Clear();
        values.Clear();
    }

    // Serialize and deserialize the dictionary
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        int keyCount = keys?.Count ?? 0;
        serializer.SerializeValue(ref keyCount);

        if (serializer.IsReader)
        {
            keys = new List<int>(keyCount);
            values = new List<List<int[]>>(keyCount);
        }

        for (int i = 0; i < keyCount; i++)
        {
            int key = i < keys.Count ? keys[i] : 0;
            serializer.SerializeValue(ref key);

            if (serializer.IsReader)
            {
                keys.Add(key);
            }

            int valueCount = i < values.Count ? values[i]?.Count ?? 0 : 0;
            serializer.SerializeValue(ref valueCount);

            if (serializer.IsReader)
            {
                values.Add(new List<int[]>());
            }

            for (int j = 0; j < valueCount; j++)
            {
                int arrayLength = i < values.Count && j < values[i]?.Count ? values[i][j]?.Length ?? 0 : 0;
                serializer.SerializeValue(ref arrayLength);

                if (serializer.IsReader)
                {
                    values[i].Add(new int[arrayLength]);
                }

                for (int k = 0; k < arrayLength; k++)
                {
                    int element = j < values[i].Count && k < values[i][j]?.Length ? values[i][j][k] : 0;
                    serializer.SerializeValue(ref element);

                    if (serializer.IsReader)
                    {
                        values[i][j][k] = element;
                    }
                }
            }
        }
    }

    // Print all contents (debugging)
    public void PrintAll()
    {
        if (keys == null || values == null || keys.Count == 0)
        {
            Debug.Log("SerializableDictionary is empty.");
            return;
        }

        for (int i = 0; i < keys.Count; i++)
        {
            Debug.Log($"Key: {keys[i]}");
            Debug.Log("Values:");
            foreach (var array in values[i])
            {
                Debug.Log($"  [{string.Join(", ", array)}]");
            }
        }
    }
}

[Serializable]
public struct SerializableList : INetworkSerializable
{
    private List<int[]> data; // The list to store the arrays

    // Constructor to initialize from a regular List<int[]>
    public SerializableList(List<int[]> list)
    {
        // Ensure deep copying for immutability
        data = list != null ? new List<int[]>(list.Count) : new List<int[]>();
        if (list != null)
        {
            foreach (var array in list)
            {
                data.Add(array != null ? (int[])array.Clone() : null); // Deep copy each array, handle nulls
            }
        }
    }

    // Convert back to a regular List<int[]>
    public List<int[]> ToList()
    {
        // Ensure deep copying for immutability
        List<int[]> list = new List<int[]>(data.Count);
        foreach (var array in data)
        {
            list.Add(array != null ? (int[])array.Clone() : null); // Deep copy each array when returning
        }
        return list;
    }

    // Add an array to the list
    public void Add(int[] value)
    {
        data.Add(value != null ? (int[])value.Clone() : null); // Deep copy the array, handle nulls
    }

    // Clear the list
    public void Clear()
    {
        data.Clear();
    }

    // Serialize and deserialize the list
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        int count = data?.Count ?? 0;
        serializer.SerializeValue(ref count); // Serialize the number of items in the list

        // Initialize list if reading
        if (serializer.IsReader)
        {
            data = new List<int[]>(count);
        }

        // Serialize each array
        for (int i = 0; i < count; i++)
        {
            int arrayLength = 0;

            if (serializer.IsWriter && data[i] != null)
            {
                arrayLength = data[i].Length;
            }
            serializer.SerializeValue(ref arrayLength); // Serialize the length of the array

            if (serializer.IsReader)
            {
                data.Add(arrayLength > 0 ? new int[arrayLength] : null); // Handle empty or null arrays
            }

            for (int j = 0; j < arrayLength; j++)
            {
                int element = 0;

                if (serializer.IsWriter)
                {
                    element = data[i][j];
                }

                serializer.SerializeValue(ref element); // Serialize an element of the array

                if (serializer.IsReader)
                {
                    data[i][j] = element; // Assign the value to the deserialized array
                }
            }
        }
    }

    // Print all contents (debugging)
    public void PrintAll()
    {
        if (data == null || data.Count == 0)
        {
            Debug.Log("SerializableList is empty.");
            return;
        }

        for (int i = 0; i < data.Count; i++)
        {
            if (data[i] == null)
            {
                Debug.Log($"Array {i}: null");
            }
            else
            {
                Debug.Log($"Array {i}: [{string.Join(", ", data[i])}]");
            }
        }
    }
}


public class NetworkRelay : NetworkBehaviour
{
    [SerializeField]private Server server;
    private List<GameManager> gameManagers;
    public static NetworkRelay Instance { get; private set; }
    // Start is called before the first frame update
    void Awake()
    {
        if(IsServer)
        {
            Instance = this;
            gameManagers = new List<GameManager>();
        }

        Instance = this;
        gameManagers = new List<GameManager>();
    }

    public void AddGameManager(GameManager gameManager)
    {
        gameManagers.Add(gameManager);
        print("gameManager added");
    }
    // Update is called once per frame
    void Update()
    {
        
    }

    //ClientRpc
    [ClientRpc(RequireOwnership = false)]
    public void UpdateCurrentPlayerClientRPC(int currentPlayer)
    {
        Debug.Log("ClientRpc called: Updating current player");
        if (GameManager.LocalInstance != null)
        {
            GameManager.LocalInstance.UpdateCurrentPlayer(currentPlayer);
        }
        else
        {
            Debug.LogError("GameManagers list is null or empty!");
        }
        Debug.Log("ClientRpc Updating current player execution complete.");
    }
    [ClientRpc(RequireOwnership = false)]
    public void UpdateCenterCardIDListClientRPC(SerializableList serializableList)
    {
        Debug.Log("ClientRpc called: Updating current player");
        if (GameManager.LocalInstance != null)
        {
            GameManager.LocalInstance.UpdateCenterCardIDList(serializableList);
        }
        else
        {
            Debug.LogError("GameManagers list is null or empty!");
        }
        Debug.Log("ClientRpc Updating current player execution complete.");
    }
    [ClientRpc(RequireOwnership = false)]
    public void InitializeCardPrefabsClientRPC()
    {
        Debug.Log("ClientRpc called: Initializing card prefabs.");
        GameManager.LocalInstance.InitializeCardPrefabs();
        Debug.Log("ClientRpc called: Initializing card prefabs ended.");
    }

    [ClientRpc(RequireOwnership = false)]
    public void DealCardPrefabsToPlayersClientRPC(int playerCount, SerializableDictionary serializableDictionary)
    {
        Debug.Log("ClientRpc called: Dealing card prefabs to players.");
        //Debug.Log("PlayerCount: " + playerCount);
        //serializableDictionary.PrintAll();
        GameManager.LocalInstance.CardPrefabsToPlayers(playerCount,serializableDictionary);
        Debug.Log("ClientRpc called: Dealing card prefabs to players ended.");
    }

    [ClientRpc(RequireOwnership = false)]
    public void DealCardPrefabsToCenterClientRPC(SerializableList serializableList)
    {
        Debug.Log("ClientRpc called: Dealing card prefabs to center.");
        serializableList.PrintAll();
        GameManager.LocalInstance.CardPrefabsToCenter(serializableList);
        Debug.Log("ClientRpc called: Dealing card prefabs to center ended.");
    }

    [ClientRpc(RequireOwnership = false)]
    public void SendMoveToClientRPC(int[] selectedHandCard, SerializableList selectedCenterCards, int playerNumber)
    {
        Debug.Log("ClientRpc called: SendMoveToClientRPC.");
        GameManager.LocalInstance.DiscardPlayedCards(selectedHandCard, selectedCenterCards, playerNumber);
        Debug.Log("ClientRpc called: SendMoveToClientRPC ended.");
    }

    [ClientRpc(RequireOwnership = false)]
    public void SendCardAddedToCenterClientRPC(int[] cardID)
    {
        GameManager.LocalInstance.GetCardAddedToCenter(cardID);
    }
    //ServerRPC
    [ServerRpc(RequireOwnership = false)]
    public void PlayerChkobbaServerRPC(int playerNumber)
    {
        server.PlayerChkobba(playerNumber);
    }

    /*[ServerRpc(RequireOwnership = false)]//Called after player plays a move
    public void EndTurnAfterPlayServerRPC(int playerNumber,SerializableList serializableList)
    {
        server.lastPlayerToCapture = playerNumber;
        server.AddDiscardedCardsToPlayerPool(serializableList);
        server.EndTurn();//End turn
    }*/

    [ServerRpc(RequireOwnership = false)]
    public void RemoveCenterCardsServerRPC(SerializableList serializableList)
    {
        
        server.RemoveCardsFromCenter(serializableList);
        
    }
    [ServerRpc(RequireOwnership = false)]
    public void PrintMessageServerRPC(string message)
    {
        Debug.Log("PrintMessageServerRPC called with message: " + message);
        server.PrintMessage(message);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendMoveToServerRPC(int[] selectedHandCard, SerializableList serializableList, int playerNumber)
    {
        server.GetMove(selectedHandCard,serializableList,playerNumber);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddCenterCardServerRPC(int[] cardID)
    {
        server.AddCardIDToCenter(cardID);
    }
    
}
