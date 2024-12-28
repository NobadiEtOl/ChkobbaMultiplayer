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
    public List<KeyValuePair<int, List<int[]>>> data;

    public SerializableDictionary(Dictionary<int, List<int[]>> dictionary)
    {
        data = new List<KeyValuePair<int, List<int[]>>>(dictionary);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        foreach (var kvp in data)
        {
            // Serialize key
            int key = kvp.Key;
            serializer.SerializeValue(ref key);

            // Serialize the list of int[] (arrays inside the list)
            List<int[]> value = kvp.Value;
            int listCount = value.Count;
            serializer.SerializeValue(ref listCount); // Serialize the list size

            for (int i = 0; i < value.Count; i++)  // Use a for loop instead of foreach
            {
                int[] array = value[i];
                int arrayLength = array.Length;
                serializer.SerializeValue(ref arrayLength); // Serialize array length

                for (int j = 0; j < array.Length; j++)  // Serialize each element in the array
                {
                    int element = array[j];  // Access the element with a regular for loop
                    serializer.SerializeValue(ref element); // Serialize the element
                }
            }
        }
    }

    public Dictionary<int, List<int[]>> ToDictionary()
    {
        var dictionary = new Dictionary<int, List<int[]>>();

        // Loop through the data and convert to dictionary format
        foreach (var kvp in data)
        {
            dictionary.Add(kvp.Key, kvp.Value);
        }

        return dictionary;
    }

}

[Serializable]
public struct SerializableList : INetworkSerializable
{
    public List<int[]> data;

    // Constructor to initialize from a regular List<int[]>
    public SerializableList(List<int[]> list)
    {
        data = new List<int[]>(list);
    }

    // Network serialization method
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // Serialize the list size
        int listCount = data.Count;
        serializer.SerializeValue(ref listCount);  // Serialize the list size

        // Serialize each int[] in the list
        for (int i = 0; i < data.Count; i++)
        {
            int[] array = data[i];
            int arrayLength = array.Length;
            serializer.SerializeValue(ref arrayLength);  // Serialize array length

            // Serialize each element in the array
            for (int j = 0; j < array.Length; j++)
            {
                int element = array[j];
                serializer.SerializeValue(ref element);  // Serialize each element
            }
        }
    }

    // Convert the SerializableList back to a regular List<int[]>
    public List<int[]> ToList()
    {
        return new List<int[]>(data);
    }
}

public class NetworkRelay : NetworkBehaviour
{
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
    [ClientRpc]
    public void UpdateCurrentPlayerClientRPC(int currentPlayer)
    {
        gameManagers[0].UpdateCurrentPlayer(currentPlayer);
        print("trying");
    }
    [ClientRpc]
    public void InitializeCardPrefabsClientRPC()
    {
        gameManagers[0].InitializeCardPrefabs();
    }
    [ClientRpc]
    public void DealCardPrefabsToPlayersClientRPC(int playerCount, SerializableDictionary serializableDictionary)
    {
        gameManagers[0].DealCardPrefabsToPlayers(playerCount,serializableDictionary);
    }
    [ClientRpc]
    public void DealCardPrefabsToCenterClientRPC(SerializableList serializableList)
    {
        gameManagers[0].DealCardPrefabsToCenter(serializableList);
    }

    //ServerRPC
    [ServerRpc]
    public void PlayerChkobbaServerRPC(int playerNumber)
    {
        Server.Singleton.PlayerChkobba(playerNumber);
    }

    [ServerRpc]//Called after player plays a move
    public void EndTurnAfterPlayServerRPC(int playerNumber,SerializableList serializableList)
    {
        Server.Singleton.lastPlayerToCapture = playerNumber;
        Server.Singleton.AddDiscardedCardsToPlayerPool(serializableList);
        Server.Singleton.EndTurn();//End turn
    }

    [ServerRpc]//Called after player adds a card to the center
    public void EndTurnAfterCenterServerRPC(int[] currentSelectedHandCard)
    {
        Server.Singleton.centerCardsIDs.Add(currentSelectedHandCard);
        Server.Singleton.EndTurn();
    }

    [ServerRpc]
    public void RemoveCenterCardsServerRPC(SerializableList serializableList)
    {
        
        Server.Singleton.RemoveCardsFromCenter(serializableList);
        
    }
    [ServerRpc(RequireOwnership = false)]
    public void PrintMessageServerRPC(string message)
    {
        Debug.Log("PrintMessageServerRPC called with message: " + message);
        Server.Singleton.PrintMessage(message);
    }
}
