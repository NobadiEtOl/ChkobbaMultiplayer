using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System;

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

    // Pop the last array from the list
    public int[] Pop()
    {
        if (data == null || data.Count == 0)
        {
            return null;
        }
        int lastIndex = data.Count - 1;
        int[] value = data[lastIndex];
        data.RemoveAt(lastIndex);
        return value;
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
