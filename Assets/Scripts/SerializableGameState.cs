using System;
using System.Collections.Generic;
using Unity.Netcode;

/// <summary>
/// Serializable DTO that represents the complete game state for syncing clients
/// </summary>
[Serializable]
public struct SerializableGameState : INetworkSerializable
{
    // Core game info
    public int snapshotVersion;
    public long timestamp;
    public int playerCount;
    public int currentPlayer;
    public int turnCounter;
    public int roundCount;
    public int startingPlayerNo;
    public int seed;
    public int lastPlayerToCapture;

    // Card containers (all using uniqueCardID strings)
    public SerializableStringList deck;
    public SerializableStringList center;
    public SerializableDictionary hands; // Dictionary<int, List<string>>
    public SerializableDictionary pools; // Dictionary<int, List<string>>
    public SerializableDictionary pistiPools; // Dictionary<int, List<string>>
    public SerializableStringList bombStack;

    // Scores and counts
    public SerializableIntArray points;
    public SerializableIntArray pistiCounts;

    // Active effects and flags
    public SerializableStringDictionary copiedCardMap; // Dictionary<string, string>
    public bool oynayamazsinActive;
    public bool isYapamazsınActive;
    public bool verZehriActive;
    public bool kutsalDesteActive;
    public bool verZehriPending;
    public bool kutsalDestePending;
    public bool oynayamazsinPending;
    public int blockCount;

    // Optional: Player gold (if you want gold to be sync-safe)
    public SerializableDictionary playerGold; // Dictionary<int, int>

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref snapshotVersion);
        serializer.SerializeValue(ref timestamp);
        serializer.SerializeValue(ref playerCount);
        serializer.SerializeValue(ref currentPlayer);
        serializer.SerializeValue(ref turnCounter);
        serializer.SerializeValue(ref roundCount);
        serializer.SerializeValue(ref startingPlayerNo);
        serializer.SerializeValue(ref seed);
        serializer.SerializeValue(ref lastPlayerToCapture);

        serializer.SerializeValue(ref deck);
        serializer.SerializeValue(ref center);
        serializer.SerializeValue(ref hands);
        serializer.SerializeValue(ref pools);
        serializer.SerializeValue(ref pistiPools);
        serializer.SerializeValue(ref bombStack);

        serializer.SerializeValue(ref points);
        serializer.SerializeValue(ref pistiCounts);

        serializer.SerializeValue(ref copiedCardMap);
        serializer.SerializeValue(ref oynayamazsinActive);
        serializer.SerializeValue(ref isYapamazsınActive);
        serializer.SerializeValue(ref verZehriActive);
        serializer.SerializeValue(ref kutsalDesteActive);
        serializer.SerializeValue(ref verZehriPending);
        serializer.SerializeValue(ref kutsalDestePending);
        serializer.SerializeValue(ref oynayamazsinPending);
        serializer.SerializeValue(ref blockCount);

        serializer.SerializeValue(ref playerGold);
    }
}

/// <summary>
/// Helper for serializing List<string>
/// </summary>
[Serializable]
public struct SerializableStringList : INetworkSerializable
{
    public string[] items;

    public SerializableStringList(List<string> list)
    {
        items = list?.ToArray() ?? new string[0];
    }

    public List<string> ToList()
    {
        return items != null ? new List<string>(items) : new List<string>();
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        int length = items != null ? items.Length : 0;
        serializer.SerializeValue(ref length);
        if (serializer.IsReader)
        {
            items = new string[length];
        }
        for (int i = 0; i < length; i++)
        {
            string element = serializer.IsWriter ? (items[i] ?? string.Empty) : string.Empty;
            serializer.SerializeValue(ref element);
            if (serializer.IsReader)
            {
                items[i] = element;
            }
        }
    }
}

/// <summary>
/// Helper for serializing int[]
/// </summary>
[Serializable]
public struct SerializableIntArray : INetworkSerializable
{
    public int[] items;

    public SerializableIntArray(int[] array)
    {
        items = array ?? new int[0];
    }

    public int[] ToArray()
    {
        return items ?? new int[0];
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // Keep primitive array handling minimal; write length then elements
        int length = items != null ? items.Length : 0;
        serializer.SerializeValue(ref length);
        if (serializer.IsReader)
        {
            items = new int[length];
        }
        for (int i = 0; i < length; i++)
        {
            int element = serializer.IsWriter ? items[i] : 0;
            serializer.SerializeValue(ref element);
            if (serializer.IsReader)
            {
                items[i] = element;
            }
        }
    }
}

/// <summary>
/// Helper for serializing Dictionary<string, string>
/// </summary>
[Serializable]
public struct SerializableStringDictionary : INetworkSerializable
{
    public string[] keys;
    public string[] values;

    public SerializableStringDictionary(Dictionary<string, string> dict)
    {
        if (dict != null && dict.Count > 0)
        {
            keys = new string[dict.Count];
            values = new string[dict.Count];
            int i = 0;
            foreach (var kvp in dict)
            {
                keys[i] = kvp.Key;
                values[i] = kvp.Value;
                i++;
            }
        }
        else
        {
            keys = new string[0];
            values = new string[0];
        }
    }

    public Dictionary<string, string> ToDictionary()
    {
        var dict = new Dictionary<string, string>();
        if (keys != null && values != null)
        {
            for (int i = 0; i < Math.Min(keys.Length, values.Length); i++)
            {
                if (!string.IsNullOrEmpty(keys[i]))
                {
                    dict[keys[i]] = values[i] ?? string.Empty;
                }
            }
        }
        return dict;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        int length = keys != null ? keys.Length : 0;
        int valuesLength = values != null ? values.Length : 0;

        // Serialize length once; enforce keys and values equal length
        if (serializer.IsWriter)
        {
            length = Math.Min(length, valuesLength);
        }
        serializer.SerializeValue(ref length);

        if (serializer.IsReader)
        {
            keys = new string[length];
            values = new string[length];
        }

        for (int i = 0; i < length; i++)
        {
            string k = serializer.IsWriter ? (keys[i] ?? string.Empty) : string.Empty;
            string v = serializer.IsWriter ? (values[i] ?? string.Empty) : string.Empty;
            serializer.SerializeValue(ref k);
            serializer.SerializeValue(ref v);
            if (serializer.IsReader)
            {
                keys[i] = k;
                values[i] = v;
            }
        }
    }
}
