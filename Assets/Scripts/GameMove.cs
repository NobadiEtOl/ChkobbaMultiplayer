using System;
using System.Collections.Generic;
using Unity.Netcode;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Represents a single game move/action that can be chained for sync validation
/// </summary>
[Serializable]
public struct GameMove : INetworkSerializable
{
    public int moveId;           // Sequential ID for ordering
    public int playerNumber;     // Who made the move
    public long timestamp;       // When it happened
    public MoveType moveType;    // What type of move
    public string cardId;        // Primary card involved
    public int[] cardData;       // Card kind/value at time of move
    public string[] capturedCardIds; // For captures
    public int sumValue;         // Sum value for captures
    public string moveHash;      // Hash for integrity checking

    // Superpower-specific data
    public string superPowerName;           // Name of superpower used
    public string[] affectedCardIds;        // All cards affected by superpower
    public SerializableStringDictionary superPowerData; // Power-specific data

    public enum MoveType
    {
        PlayToCenter,
        Capture,
        SuperPower_Activation,    // Superpower was activated
        SuperPower_Effect,        // Superpower effect applied to card(s)
        CardMovement,             // Card moved between locations
        CardSwap,                 // Cards swapped between players
        CardParentingChange,      // Card parenting changed in Unity hierarchy
        TurnEnd,
        RoundStart
    }



    /// <summary>
    /// Creates a basic card play move
    /// </summary>
    public static GameMove CreateCardPlayMove(int moveId, int playerNumber, string cardId, int[] cardData, 
        string[] capturedCardIds, int sumValue)
    {
        var move = new GameMove
        {
            moveId = moveId,
            playerNumber = playerNumber,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            moveType = capturedCardIds != null && capturedCardIds.Length > 0 ? MoveType.Capture : MoveType.PlayToCenter,
            cardId = cardId,
            cardData = cardData ?? new int[0],
            capturedCardIds = capturedCardIds ?? new string[0],
            sumValue = sumValue,
            superPowerName = "",
            affectedCardIds = new string[0],
            superPowerData = new SerializableStringDictionary(new Dictionary<string, string>())
        };
        
        move.moveHash = CalculateMoveHash(move);
        return move;
    }

    /// <summary>
    /// Creates a superpower activation move
    /// </summary>
    public static GameMove CreateSuperpowerActivationMove(int moveId, int playerNumber, string superPowerName)
    {
        var move = new GameMove
        {
            moveId = moveId,
            playerNumber = playerNumber,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            moveType = MoveType.SuperPower_Activation,
            cardId = "",
            cardData = new int[0],
            capturedCardIds = new string[0],
            sumValue = 0,
            superPowerName = superPowerName,
            affectedCardIds = new string[0],
            superPowerData = new SerializableStringDictionary(new Dictionary<string, string>())
        };
        
        move.moveHash = CalculateMoveHash(move);
        return move;
    }

    /// <summary>
    /// Creates a superpower effect move (when power affects cards)
    /// </summary>
    public static GameMove CreateSuperpowerEffectMove(int moveId, int playerNumber, string superPowerName, 
        string[] affectedCardIds, Dictionary<string, string> effectData)
    {
        var move = new GameMove
        {
            moveId = moveId,
            playerNumber = playerNumber,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            moveType = MoveType.SuperPower_Effect,
            cardId = affectedCardIds != null && affectedCardIds.Length > 0 ? affectedCardIds[0] : "",
            cardData = new int[0],
            capturedCardIds = new string[0],
            sumValue = 0,
            superPowerName = superPowerName,
            affectedCardIds = affectedCardIds ?? new string[0],
            superPowerData = new SerializableStringDictionary(effectData ?? new Dictionary<string, string>())
        };
        
        move.moveHash = CalculateMoveHash(move);
        return move;
    }
    
    /// <summary>
    /// Creates a card movement move
    /// </summary>
    public static GameMove CreateCardMovementMove(int moveId, int playerNumber, string cardId, string fromLocation, string toLocation, string reason)
    {
        var move = new GameMove
        {
            moveId = moveId,
            playerNumber = playerNumber,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            moveType = MoveType.CardMovement,
            cardId = cardId,
            cardData = new int[0],
            capturedCardIds = new string[0],
            sumValue = 0,
            superPowerName = "",
            affectedCardIds = new string[0],
            superPowerData = new SerializableStringDictionary(new Dictionary<string, string> 
            { 
                { "fromLocation", fromLocation },
                { "toLocation", toLocation },
                { "reason", reason }
            })
        };
        
        move.moveHash = CalculateMoveHash(move);
        return move;
    }
    
    /// <summary>
    /// Creates a card swap move
    /// </summary>
    public static GameMove CreateCardSwapMove(int moveId, int playerANumber, int playerBNumber, string cardAId, string cardBId, string reason)
    {
        var move = new GameMove
        {
            moveId = moveId,
            playerNumber = playerANumber, // Primary player
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            moveType = MoveType.CardSwap,
            cardId = cardAId,
            cardData = new int[0],
            capturedCardIds = new string[0],
            sumValue = 0,
            superPowerName = "",
            affectedCardIds = new string[] { cardAId, cardBId },
            superPowerData = new SerializableStringDictionary(new Dictionary<string, string> 
            { 
                { "playerB", playerBNumber.ToString() },
                { "cardB", cardBId },
                { "reason", reason }
            })
        };
        
        move.moveHash = CalculateMoveHash(move);
        return move;
    }
    
    /// <summary>
    /// Creates a card parenting change move
    /// </summary>
    public static GameMove CreateCardParentingChangeMove(int moveId, int playerNumber, string cardId, string oldParent, string newParent, string reason)
    {
        var move = new GameMove
        {
            moveId = moveId,
            playerNumber = playerNumber,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            moveType = MoveType.CardParentingChange,
            cardId = cardId,
            cardData = new int[0],
            capturedCardIds = new string[0],
            sumValue = 0,
            superPowerName = "",
            affectedCardIds = new string[0],
            superPowerData = new SerializableStringDictionary(new Dictionary<string, string> 
            { 
                { "oldParent", oldParent },
                { "newParent", newParent },
                { "reason", reason }
            })
        };
        
        move.moveHash = CalculateMoveHash(move);
        return move;
    }

    /// <summary>
    /// Calculates hash for move integrity validation
    /// </summary>
    private static string CalculateMoveHash(GameMove move)
    {
        var sb = new StringBuilder();
        sb.Append($"{move.moveId}|{move.playerNumber}|{move.timestamp}|{move.moveType}|{move.cardId}|{move.sumValue}|{move.superPowerName}");
        
        if (move.cardData != null)
        {
            foreach (var data in move.cardData)
                sb.Append($"|{data}");
        }
        
        if (move.capturedCardIds != null)
        {
            foreach (var captured in move.capturedCardIds)
                sb.Append($"|{captured}");
        }
        
        if (move.affectedCardIds != null)
        {
            foreach (var affected in move.affectedCardIds)
                sb.Append($"|{affected}");
        }
        
        // Add superpower data to hash
        if (move.superPowerData.keys != null && move.superPowerData.values != null)
        {
            for (int i = 0; i < move.superPowerData.keys.Length; i++)
            {
                sb.Append($"|{move.superPowerData.keys[i]}:{move.superPowerData.values[i]}");
            }
        }
        
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }

    /// <summary>
    /// Validates if this move matches another move (for desync detection)
    /// </summary>
    public bool MatchesMove(GameMove other)
    {
        return moveId == other.moveId &&
               playerNumber == other.playerNumber &&
               moveType == other.moveType &&
               cardId == other.cardId &&
               sumValue == other.sumValue &&
               superPowerName == other.superPowerName &&
               moveHash == other.moveHash;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref moveId);
        serializer.SerializeValue(ref playerNumber);
        serializer.SerializeValue(ref timestamp);
        serializer.SerializeValue(ref moveType);
        serializer.SerializeValue(ref cardId);
        
        // Handle nullable arrays for NetworkSerializer compatibility
        int cardDataLength = cardData?.Length ?? 0;
        serializer.SerializeValue(ref cardDataLength);
        if (serializer.IsReader)
        {
            cardData = new int[cardDataLength];
        }
        for (int i = 0; i < cardDataLength; i++)
        {
            int element = serializer.IsWriter ? cardData[i] : 0;
            serializer.SerializeValue(ref element);
            if (serializer.IsReader)
            {
                cardData[i] = element;
            }
        }
        
        int capturedLength = capturedCardIds?.Length ?? 0;
        serializer.SerializeValue(ref capturedLength);
        if (serializer.IsReader)
        {
            capturedCardIds = new string[capturedLength];
        }
        for (int i = 0; i < capturedLength; i++)
        {
            string element = serializer.IsWriter ? (capturedCardIds[i] ?? "") : "";
            serializer.SerializeValue(ref element);
            if (serializer.IsReader)
            {
                capturedCardIds[i] = element;
            }
        }
        
        serializer.SerializeValue(ref sumValue);
        serializer.SerializeValue(ref moveHash);
        serializer.SerializeValue(ref superPowerName);
        
        int affectedLength = affectedCardIds?.Length ?? 0;
        serializer.SerializeValue(ref affectedLength);
        if (serializer.IsReader)
        {
            affectedCardIds = new string[affectedLength];
        }
        for (int i = 0; i < affectedLength; i++)
        {
            string element = serializer.IsWriter ? (affectedCardIds[i] ?? "") : "";
            serializer.SerializeValue(ref element);
            if (serializer.IsReader)
            {
                affectedCardIds[i] = element;
            }
        }
        
        serializer.SerializeValue(ref superPowerData);
    }
}
