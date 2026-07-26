using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System;

[Serializable]
public struct SerializableCard : INetworkSerializable
{
    private List<string> keys;      // List to store dictionary keys
    private List<int[]> values;     // List to store dictionary values

    // Constructor to initialize from a regular dictionary
    public SerializableCard(Dictionary<string, int[]> dictionary)
    {
        keys = new List<string>();
        values = new List<int[]>();

        foreach (var kvp in dictionary)
        {
            keys.Add(kvp.Key);
            values.Add((int[])kvp.Value.Clone());
        }
    }

    // Convert back to a regular dictionary
    public Dictionary<string, int[]> ToDictionary()
    {
        Dictionary<string, int[]> dictionary = new Dictionary<string, int[]>();
        for (int i = 0; i < keys.Count; i++)
        {
            dictionary[keys[i]] = values[i];
        }
        return dictionary;
    }

    // Add key-value pair
    public void Add(string key, int[] value)
    {
        keys.Add(key);
        values.Add((int[])value.Clone());
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
            keys = new List<string>(keyCount);
            values = new List<int[]>(keyCount);
        }

        for (int i = 0; i < keyCount; i++)
        {
            string key = i < keys.Count ? keys[i] : string.Empty;
            serializer.SerializeValue(ref key);

            if (serializer.IsReader)
            {
                keys.Add(key);
            }

            int arrayLength = i < values.Count ? values[i]?.Length ?? 0 : 0;
            serializer.SerializeValue(ref arrayLength);

            if (serializer.IsReader)
            {
                values.Add(new int[arrayLength]);
            }

            for (int k = 0; k < arrayLength; k++)
            {
                int element = k < values[i]?.Length ? values[i][k] : 0;
                serializer.SerializeValue(ref element);

                if (serializer.IsReader)
                {
                    values[i][k] = element;
                }
            }
        }
    }

    // Print all contents (debugging)
    public void PrintAll()
    {
        if (keys == null || values == null || keys.Count == 0)
        {
            
            return;
        }

        for (int i = 0; i < keys.Count; i++)
        {
            
            
        }
    }
}
