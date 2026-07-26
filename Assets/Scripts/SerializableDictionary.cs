using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System;

[Serializable]
public struct SerializableDictionary : INetworkSerializable
{
    private List<int> keys;              // List to store player numbers
    private List<List<string>> values;   // List to store uniqueIDs

    // Constructor to initialize from a regular dictionary
    public SerializableDictionary(Dictionary<int, List<string>> dictionary)
    {
        keys = new List<int>();
        values = new List<List<string>>();

        foreach (var kvp in dictionary)
        {
            keys.Add(kvp.Key);

            // Deep copy of the list
            List<string> deepCopiedList = new List<string>(kvp.Value);
            values.Add(deepCopiedList);
        }
    }

    // Convert back to a regular dictionary
    public Dictionary<int, List<string>> ToDictionary()
    {
        Dictionary<int, List<string>> dictionary = new Dictionary<int, List<string>>();
        
        if (keys == null || values == null) return dictionary;

        for (int i = 0; i < keys.Count; i++)
        {
            if (i < values.Count)
            {
                List<string> list = values[i] ?? new List<string>();
                dictionary[keys[i]] = new List<string>(list);
            }
            else
            {
                dictionary[keys[i]] = new List<string>();
            }
        }
        return dictionary;
    }

    // Add key-value pair
    public void Add(int key, List<string> value)
    {
        keys.Add(key);
        values.Add(new List<string>(value));
    }

    // Clear the dictionary
    public void Clear()
    {
        keys.Clear();
        values.Clear();
    }

    // Get the count of items in the dictionary
    public int Count
    {
        get { return keys?.Count ?? 0; }
    }

    // Serialize and deserialize the dictionary
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        int keyCount = keys?.Count ?? 0;
        serializer.SerializeValue(ref keyCount);

        if (serializer.IsReader)
        {
            keys = new List<int>(keyCount);
            values = new List<List<string>>(keyCount);
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
                values.Add(new List<string>());
            }

            for (int j = 0; j < valueCount; j++)
            {
                string element = i < values.Count && j < values[i]?.Count ? values[i][j] : string.Empty;
                serializer.SerializeValue(ref element);

                if (serializer.IsReader)
                {
                    values[i].Add(element);
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
            
            
            foreach (var uniqueID in values[i])
            {
                
            }
        }
    }
}
