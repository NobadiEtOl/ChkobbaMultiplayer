using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
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
