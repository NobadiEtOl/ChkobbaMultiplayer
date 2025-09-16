# BotPlayer System Usage Guide

## Overview
The BotPlayer system allows single players to play 1v1 Chkobba games against an AI opponent. The bot runs on the host client using GameManager logic, making it work offline and ensuring proper game flow.

## Key Features
- **Client-Side Bot**: Runs on host's GameManager, not server
- **Offline Capable**: Works without internet connection
- **Uses PlayerHand3**: Bot cards are managed in PlayerHand3 transform
- **Natural Game Flow**: Uses same CheckIfLegal path as human players
- **Proper Captures**: Evaluates moves like humans (captures, pişti)

## How to Enable Bot Mode

### Method 1: Unity Inspector
1. Select the **Server** GameObject in the scene
2. Find the **Server (Script)** component
3. Check the **Is Bot Mode Enabled** checkbox
4. Select the **GameManager** GameObject in the scene
5. Find the **BotPlayer (Script)** component
6. Check the **Is Bot Mode Enabled** checkbox
7. Adjust **Bot Move Delay** (default: 2 seconds) if desired

### Method 2: Context Menu (Runtime)
1. Select the **Server** GameObject in the scene
2. Right-click on the **Server (Script)** component
3. Select **"Toggle Bot Mode"** from the context menu
4. Select the **GameManager** GameObject in the scene
5. Right-click on the **BotPlayer (Script)** component
6. Select **"Toggle Bot Mode"** from the context menu
7. Check the Console for confirmation messages

## How It Works

### Dual System Architecture
- **Server Side**: Detects bot mode and starts game with 1 human player
- **Client Side**: BotPlayer component manages bot behavior on host
- **Game Start**: Server recognizes bot mode and starts 1v1 game immediately
- **Bot Behavior**: BotPlayer runs on host's GameManager using PlayerHand3

### Automatic Activation
- **Server**: Detects when `isBotModeEnabled = true` and `playerCount = 2`
- **Server**: Starts game when only 1 human player connects (instead of waiting for 2)
- **Client**: BotPlayer activates when host is player 0 and bot mode is enabled
- **Bot**: Plays as **Player 1** using **PlayerHand3** cards
- **Host**: Plays as **Player 0** using **PlayerHand1**

### Bot Behavior
- Bot detects when it's its turn (`currentPlayerNo == 1`)
- Waits for configurable delay (default: 2 seconds)
- Selects first card from **PlayerHand3**
- Evaluates possible captures from center cards
- Calls `CheckIfLegal()` like human players

### Bot Strategy
- **Simple Selection**: Always plays first card in PlayerHand3
- **Capture Logic**: 
  - **Jack (value 11)**: Captures all center cards
  - **Normal cards**: Captures cards with matching value
  - **No matches**: Plays card to center
- **Game Flow**: Uses same path as humans (selection → evaluation → CheckIfLegal → RPC)

## Architecture Benefits

### Client-Side Design
- **No Server Dependencies**: Bot runs entirely on host's GameManager
- **Offline Play**: Works without network connection
- **Proper Game Logic**: Uses existing GameManager functions
- **Visual Consistency**: Bot moves trigger same animations as human moves

### Network Integration
- **Same RPC Flow**: Bot calls `SendMoveToServerRPC` like humans
- **Synchronized**: Bot moves are properly sent to server and back to client
- **Turn Management**: Natural turn ending through `TellServerTurnEnded()`
- **Pişti Detection**: Works correctly with existing server logic

## Testing Bot Functionality

### Context Menu Commands

#### Server (Script) Commands
1. **Toggle Bot Mode**: Enable/disable bot mode on server
2. **Start Bot Game**: Manually start a 1v1 game with bot (for testing)

#### BotPlayer (Script) Commands
1. **Toggle Bot Mode**: Enable/disable bot behavior on client
2. **Force Bot Move**: Make the bot move immediately (only when active)
3. **Check Bot Status**: Display detailed bot state information

### Console Messages
The bot provides detailed logging:
```
[BotPlayer] Bot system initialized
[BotPlayer] Bot activated! Bot plays as player 1
[BotPlayer] It's bot's turn, scheduling move
[BotPlayer] Bot selected card: [cardName]
[BotPlayer] Looking for captures for card value X
[BotPlayer] Bot will capture X center cards
[BotPlayer] Playing card via CheckIfLegal
```

### Status Information
Use **"Check Bot Status"** to see:
- Mode enabled/disabled
- Bot active state
- Server status
- Player numbers
- PlayerHand3 card count
- Current game state

## Game Flow Comparison

### Human Player Flow
1. Player selects card from their hand
2. Player selects center cards (if capturing)
3. Player action triggers `CheckIfLegal(playerNumber)`
4. GameManager calls `SendMoveToServerRPC()`
5. Server processes move and responds
6. Client animations play, turn ends

### Bot Player Flow
1. Bot detects its turn in `Update()`
2. Bot selects first card from PlayerHand3
3. Bot evaluates center cards for captures
4. Bot calls `CheckIfLegal(botPlayerNumber)`
5. **Same as human from step 4 onwards**

## Configuration Options

### Editable in Inspector
- **Is Bot Mode Enabled**: Toggle bot on/off
- **Bot Move Delay**: Delay before bot moves (seconds)
- **Bot Player Number**: Which player slot bot uses (default: 1)

### Automatic Detection
- **Player Count**: Auto-detects 1v1 games
- **Host Status**: Auto-detects if running on host
- **Hand Cards**: Auto-finds PlayerHand3 transform

## Troubleshooting

### Bot Not Activating
1. Check **Is Bot Mode Enabled** is checked
2. Verify it's a **1v1 game** (playerCount = 2)
3. Ensure you're the **host** (not client)
4. Use **"Check Bot Status"** for detailed info

### Bot Not Making Moves
1. Check if **PlayerHand3** has cards
2. Verify bot's turn: `currentPlayerNo == 1`
3. Look for errors in Console
4. Try **"Force Bot Move"** for testing

### Cards Not Found
1. Ensure **PlayerHand3** GameObject exists in scene
2. Check that cards are properly dealt to PlayerHand3
3. Verify CardInteraction scripts on cards
4. Check card lookup tables are populated

### Offline Play Issues
1. Bot system works entirely offline on host
2. No network connection required for bot moves
3. Host manages both human and bot players locally
4. Only human player moves need network sync

## Advantages Over Server-Side Bot

### Better Game Integration
- **Uses GameManager**: Same logic as human players
- **Proper Captures**: Correctly evaluates moves before sending
- **Visual Feedback**: Bot moves show in host's view naturally
- **Turn Management**: Natural turn ending flow

### Network Efficiency
- **Fewer RPCs**: Bot moves use standard player flow
- **Client Processing**: Move evaluation happens on client
- **Offline Capable**: No server dependency for bot logic
- **Scalable**: Can easily add multiple bots

### Development Benefits
- **Testable**: Easy to test bot behavior in isolation
- **Debuggable**: Clear separation of bot logic
- **Maintainable**: Bot logic in dedicated component
- **Extensible**: Easy to add more sophisticated AI

## Future Enhancements
- **Multiple Difficulty Levels**: Easy, Medium, Hard AI
- **Smart Strategy**: Better card selection algorithms
- **Superpower Integration**: Bot could use superpowers
- **4-Player Support**: Multiple bots for team games
- **Machine Learning**: Train bot on real games
