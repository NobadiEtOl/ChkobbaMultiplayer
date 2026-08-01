using System;
using System.Collections.Generic;
using Unity.Netcode;

/// <summary>
/// One entry in the allCardLookup dictionary (cardId -> {kind, value}).
/// Used only for PlayerPrefs-based snapshot persistence; not sent over the network.
/// </summary>
[Serializable]
public class CardLookupEntry
{
    public string cardId;
    public int kind;
    public int value;
}

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
    public int readyToEndTurnCounter;
    public float turnTimerElapsed;
    public float turnTimeLimit;

    // Card containers (all using uniqueCardID strings)
    public SerializableStringList deck;
    public SerializableStringList center;
    public SerializableStringList firstThreeDealtCardIds;
    public SerializableDictionary hands; // Dictionary<int, List<string>>
    public SerializableDictionary pools; // Dictionary<int, List<string>>
    public SerializableDictionary pistiPools; // Dictionary<int, List<string>>
    public SerializableStringList bombStack;

    // Scores and counts
    public SerializableIntArray points;
    public SerializableIntArray pistiCounts;
    public int p1side_selfFakePointReduction;
    public int p2side_selfFakePointReduction;
    public int p1side_oppFakePointReduction;
    public int p2side_oppFakePointReduction;

    // Active effects and flags
    public SerializableStringDictionary copiedCardMap; // Dictionary<string, string>
    public bool oynayamazsinActive;
    public bool isYapamazsınActive;
    public bool verZehriActive;
    public bool kutsalDesteActive;
    public bool verZehriPending;
    public bool kutsalDestePending;
    public bool oynayamazsinPending;
    public int oynayamazsinActivatedBy;
    public int blockCount;

    // Client-side superpower states that need to persist
    public bool isKapkacPending;
    public bool isYandimAnamPending;
    public bool isDegisTokusPending;
    public bool isKopyalaActive;
    public bool isSunuDegisTokusActive;
    public bool isSunuDegisBunuTokusActive;

    // Card power effect tracking (for Kapkaç, Yandım Anam, etc.)
    public SerializableStringDictionary cardPowerEffects; // Dictionary<string, string> - cardID -> powerEffect

    // Optional: Player gold (if you want gold to be sync-safe)
    public SerializableIntDictionary playerGold; // Dictionary<int, int>

    public SerializableDictionary playerSuperPowers; // Dictionary<int, List<string>>

    public SerializableIntArray botControlledPlayers; // Players currently controlled by bots

    // Card master lookup (cardId -> {kind, value}). Serialized for PlayerPrefs only.
    // Not included in NetworkSerialize so it does not affect RPC bandwidth.
    public CardLookupEntry[] cardLookup;

    // Precomputed sub-host failover chain (slot-descending, host excluded).
    // Set once at game start; survivors read this to determine who should rehost.
    // Stored in PlayerPrefs only - not sent over network.
    public int[] failoverChain;

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
        serializer.SerializeValue(ref readyToEndTurnCounter);
        serializer.SerializeValue(ref turnTimerElapsed);
        serializer.SerializeValue(ref turnTimeLimit);

        serializer.SerializeValue(ref deck);
        serializer.SerializeValue(ref center);
        serializer.SerializeValue(ref firstThreeDealtCardIds);
        serializer.SerializeValue(ref hands);
        serializer.SerializeValue(ref pools);
        serializer.SerializeValue(ref pistiPools);
        serializer.SerializeValue(ref bombStack);

        serializer.SerializeValue(ref points);
        serializer.SerializeValue(ref pistiCounts);
        serializer.SerializeValue(ref p1side_selfFakePointReduction);
        serializer.SerializeValue(ref p2side_selfFakePointReduction);
        serializer.SerializeValue(ref p1side_oppFakePointReduction);
        serializer.SerializeValue(ref p2side_oppFakePointReduction);

        serializer.SerializeValue(ref copiedCardMap);
        serializer.SerializeValue(ref oynayamazsinActive);
        serializer.SerializeValue(ref isYapamazsınActive);
        serializer.SerializeValue(ref verZehriActive);
        serializer.SerializeValue(ref kutsalDesteActive);
        serializer.SerializeValue(ref verZehriPending);
        serializer.SerializeValue(ref kutsalDestePending);
        serializer.SerializeValue(ref oynayamazsinPending);
        serializer.SerializeValue(ref oynayamazsinActivatedBy);
        serializer.SerializeValue(ref blockCount);
        
        // Client-side superpower states
        serializer.SerializeValue(ref isKapkacPending);
        serializer.SerializeValue(ref isYandimAnamPending);
        serializer.SerializeValue(ref isDegisTokusPending);
        serializer.SerializeValue(ref isKopyalaActive);
        serializer.SerializeValue(ref isSunuDegisTokusActive);
        serializer.SerializeValue(ref isSunuDegisBunuTokusActive);

        serializer.SerializeValue(ref cardPowerEffects);

        serializer.SerializeValue(ref playerGold);
        serializer.SerializeValue(ref playerSuperPowers);
        serializer.SerializeValue(ref botControlledPlayers);

        // Card master lookup (cardId -> {kind, value})
        int lookupLength = cardLookup != null ? cardLookup.Length : 0;
        serializer.SerializeValue(ref lookupLength);
        if (serializer.IsReader)
        {
            cardLookup = new CardLookupEntry[lookupLength];
        }
        for (int i = 0; i < lookupLength; i++)
        {
            if (serializer.IsReader) cardLookup[i] = new CardLookupEntry();
            serializer.SerializeValue(ref cardLookup[i].cardId);
            serializer.SerializeValue(ref cardLookup[i].kind);
            serializer.SerializeValue(ref cardLookup[i].value);
        }
    }
}

/// <summary>
/// Helper for serializing Dictionary<int, int>
/// </summary>
[Serializable]
public struct SerializableIntDictionary : INetworkSerializable
{
    public int[] keys;
    public int[] values;

    public SerializableIntDictionary(Dictionary<int, int> dict)
    {
        if (dict != null && dict.Count > 0)
        {
            keys = new int[dict.Count];
            values = new int[dict.Count];
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
            keys = new int[0];
            values = new int[0];
        }
    }

    public Dictionary<int, int> ToDictionary()
    {
        var dict = new Dictionary<int, int>();
        if (keys != null && values != null)
        {
            for (int i = 0; i < Math.Min(keys.Length, values.Length); i++)
            {
                dict[keys[i]] = values[i];
            }
        }
        return dict;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        int length = keys != null ? keys.Length : 0;
        int valuesLength = values != null ? values.Length : 0;

        if (serializer.IsWriter)
        {
            length = Math.Min(length, valuesLength);
        }
        serializer.SerializeValue(ref length);

        if (serializer.IsReader)
        {
            keys = new int[length];
            values = new int[length];
        }

        for (int i = 0; i < length; i++)
        {
            int k = serializer.IsWriter ? keys[i] : 0;
            int v = serializer.IsWriter ? values[i] : 0;
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

/// <summary>
/// Plain-class wrapper around int[] used for PlayerPrefs-only failover chain serialisation.
/// Not INetworkSerializable — never sent over the wire.
/// </summary>
[Serializable]
public class SerializableIntList
{
    public int[] items;

    public SerializableIntList() { items = new int[0]; }

    public SerializableIntList(System.Collections.Generic.List<int> list)
    {
        items = list != null ? list.ToArray() : new int[0];
    }

    public System.Collections.Generic.List<int> ToList()
    {
        return items != null ? new System.Collections.Generic.List<int>(items) : new System.Collections.Generic.List<int>();
    }
}
