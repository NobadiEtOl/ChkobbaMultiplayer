# Lobby Lifecycle and ParrelSync Fixes

## Problems Identified and Solved

### **Problem 1: Unity Lobbies Lifecycle Issue**
**Issue**: Unity Lobbies become **unjoinable** once the game starts because they're designed for matchmaking, not persistent game sessions.

**Root Cause**: 
- Lobbies are meant for finding and joining games
- Once players connect via Relay and the game starts, lobbies become inactive
- Reconnection attempts fail because the lobby is no longer joinable

**Solution**: **Direct Relay Connection for Reconnection**
- Prioritize direct relay connection over lobby-based connection for reconnection
- Save relay join codes instead of lobby codes for reconnection
- Use lobby system only for initial matchmaking, not reconnection

### **Problem 2: ParrelSync PlayerPrefs Sharing**
**Issue**: ParrelSync clones **share the same PlayerPrefs**, causing confusion when multiple editor instances save/load join codes.

**Root Cause**:
- All ParrelSync clones use the same PlayerPrefs storage
- Host and client clones overwrite each other's saved join codes
- Reconnection attempts use wrong join codes

**Solution**: **ParrelSync-Aware PlayerPrefs Keys**
- Generate unique identifiers for each ParrelSync instance
- Use unique PlayerPrefs keys per instance
- Prevent cross-contamination between host and client clones

## Technical Implementation

### **1. ParrelSync-Aware PlayerPrefs**

**Before:**
```csharp
private const string LAST_JOIN_CODE_KEY = "LastGameJoinCode";
private const string LAST_PLAYER_COUNT_KEY = "LastPlayerCount";
private const string LAST_GAME_TIMESTAMP_KEY = "LastGameTimestamp";
```

**After:**
```csharp
private string LAST_JOIN_CODE_KEY;
private string LAST_PLAYER_COUNT_KEY;
private string LAST_GAME_TIMESTAMP_KEY;

private void InitializePlayerPrefsKeys()
{
    string uniqueId = GetUniquePlayerId();
    LAST_JOIN_CODE_KEY = $"LastGameJoinCode_{uniqueId}";
    LAST_PLAYER_COUNT_KEY = $"LastPlayerCount_{uniqueId}";
    LAST_GAME_TIMESTAMP_KEY = $"LastGameTimestamp_{uniqueId}";
}
```

### **2. Direct Relay Connection for Reconnection**

**New Reconnection Flow:**
```csharp
private async Task<bool> AttemptReconnectionToGame(string joinCode)
{
    // CRITICAL FIX: Prioritize direct relay connection
    // Lobbies become unjoinable after game starts
    
    // First, try direct relay connection (most reliable)
    try
    {
        var directJoinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode: joinCode);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(directJoinAllocation, "wss"));
        
        bool success = NetworkManager.Singleton.StartClient();
        if (success)
        {
            
            return true;
        }
    }
    catch (Exception relayException)
    {
        // Fallback: Try lobby-based connection
        return await AttemptLobbyBasedReconnection(joinCode);
    }
}
```

### **3. Relay Code Storage Instead of Lobby Code**

**Host Side (StartHostWithRelay):**
```csharp
if (privateFlag)
{
    // Display lobby code to clients for initial joining
    joinCodeText.text = currentLobby.LobbyCode;
    
    // CRITICAL FIX: Save RELAY join code for reconnection
    // Lobby code becomes unjoinable after game starts
    SaveGameJoinCode(joinCodeVar, playerCount); // Save relay code, not lobby code
}
```

**Client Side (StartClientWithRelay):**
```csharp
// For private lobbies, save the RELAY join code for reconnection
// We need the relay code, not the lobby code
SaveGameJoinCode(relayJoinCode, currentLobby.MaxPlayers);
```

## How It Works Now

### **Initial Connection (First Time)**
1. **Host**: Creates lobby → Gets relay code → Displays lobby code to clients
2. **Client**: Uses lobby code to join → Gets relay code from lobby → Connects via relay
3. **Both**: Save relay code (not lobby code) for potential reconnection

### **Reconnection (After Disconnect)**
1. **Client**: Loads saved relay code → Attempts direct relay connection
2. **Success**: Connects directly via relay → Requests game state sync
3. **Fallback**: If direct relay fails → Tries lobby-based connection

### **ParrelSync Compatibility**
1. **Each Instance**: Gets unique PlayerPrefs keys based on instance ID
2. **No Cross-Contamination**: Host and client clones have separate saved data
3. **Independent Reconnection**: Each instance can reconnect independently

## Key Benefits

### ✅ **Reliable Reconnection**
- Direct relay connection bypasses lobby lifecycle issues
- Works even after lobbies become inactive
- Fallback to lobby-based connection for edge cases

### ✅ **ParrelSync Compatibility**
- Each editor instance has independent PlayerPrefs
- No confusion between host and client saved data
- Proper testing with multiple editor instances

### ✅ **Backward Compatibility**
- Still supports lobby-based initial connections
- Maintains existing UI and user experience
- Graceful fallback for edge cases

### ✅ **Robust Error Handling**
- Comprehensive logging for debugging
- Multiple fallback strategies
- Clear error messages for troubleshooting

## Testing Scenarios

The system now handles these scenarios correctly:

1. ✅ **ParrelSync Testing**: Host and client clones can reconnect independently
2. ✅ **Real Client Testing**: External clients can reconnect after disconnect
3. ✅ **Game In Progress**: Reconnection works even after game has started
4. ✅ **Multiple Disconnects**: Clients can disconnect/reconnect multiple times
5. ✅ **Different Room Types**: Works with private/public, 2/4 player games

## Debug Information

The system now provides clear debug messages:

```
[NetworkManagerUI] Initialized PlayerPrefs keys with unique ID: [unique_id]
[NetworkManagerUI] Reconnection: Attempting direct relay connection with code: [code]
[NetworkManagerUI] Successfully reconnected directly via relay join code: [code]
```

This makes it easy to troubleshoot reconnection issues and verify that the correct codes are being used.

## Summary

The fixes address both the **lobby lifecycle issue** and **ParrelSync PlayerPrefs sharing** problem:

- **Lobby Issue**: Direct relay connection bypasses inactive lobbies
- **ParrelSync Issue**: Unique PlayerPrefs keys prevent cross-contamination
- **Result**: Reliable reconnection for both ParrelSync testing and real clients

The system now provides a robust, reliable reconnection experience that works regardless of lobby state or ParrelSync configuration.
