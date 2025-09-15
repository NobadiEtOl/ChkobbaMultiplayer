# Bot System Usage Guide

## Overview
The bot system allows single players to play 1v1 Chkobba games against an AI opponent. The bot is controlled entirely by the server and makes moves automatically when it's the bot's turn.

## How to Enable Bot Mode

### Method 1: Unity Inspector
1. Select the **Server** GameObject in the scene
2. In the Inspector, find the **Server (Script)** component
3. Check the **Is Bot Mode Enabled** checkbox
4. Adjust **Bot Move Delay** (default: 2 seconds) if desired

### Method 2: Context Menu (Runtime)
1. Select the **Server** GameObject in the scene
2. Right-click on the **Server (Script)** component
3. Select **"Toggle Bot Mode"** from the context menu
4. Check the Console for confirmation messages

### Method 3: Manual Start (Testing)
1. Enable bot mode using Method 1 or 2
2. Right-click on the **Server (Script)** component
3. Select **"Start Bot Game"** from the context menu
4. This will immediately start a 1v1 game with the bot

## How It Works

### Automatic Activation
- Bot mode only activates in **1v1 games** (playerCount = 2)
- Bot activates when **only 1 human player** is connected
- Bot plays as **Player 1** (the opponent to the host)
- Host always plays as **Player 0**

### Bot Behavior
- Bot waits for its turn automatically
- Makes moves after a configurable delay (default: 2 seconds)
- Uses intelligent card selection strategy:
  1. **Priority 1**: Cards that can capture center cards
  2. **Priority 2**: Jacks (value 11) - powerful cards
  3. **Priority 3**: Sequential order (first available card)

### Bot Strategy
- **Captures**: Bot looks for exact value matches with center cards
- **Jacks**: Bot recognizes Jacks capture all center cards
- **No Captures**: Bot plays cards to center in sequential order
- **Turn Management**: Bot automatically ends its turn after making a move

## Testing Bot Functionality

### Context Menu Commands
1. **Toggle Bot Mode**: Enable/disable bot mode at runtime
2. **Start Bot Game**: Manually start a 1v1 game with bot (for testing)
3. **Force Bot Move**: Make the bot move immediately (only when bot is active)

### Console Messages
The bot system provides detailed logging:
- `[Server] Bot mode ENABLED/DISABLED`
- `[Server] Bot mode activated! Bot will play as player 1`
- `[Server] It's bot's turn (player 1), scheduling bot move`
- `[Server] Bot selected card: [cardId] [kind, value]`
- `[Server] Bot capturing X cards` or `Bot playing card to center`

## Limitations and Notes

### Current Limitations
- Bot only works in **1v1 games** (not 2v2)
- Bot uses **simple strategy** (no advanced AI)
- Bot **doesn't use superpowers** (only plays cards)
- Bot **doesn't respond to opponent superpowers** specifically

### Network Behavior
- Bot moves are processed server-side using the same `GetMove()` flow as human players
- Bot moves are synchronized to the human player through existing ClientRPC calls
- Bot automatically handles turn transitions and EndTurnCheck calls

### Performance
- Bot moves have a configurable delay to feel natural
- No impact on network performance (server-side only)
- Bot doesn't interfere with normal multiplayer games

## Configuration Options

### Editable in Inspector
- **Is Bot Mode Enabled**: Master toggle for bot functionality
- **Bot Move Delay**: Time (seconds) bot waits before making a move

### Hardcoded Settings
- **Bot Player Number**: 1 (opponent)
- **Human Player Number**: 0 (host)
- **Bot Strategy**: Capture-priority with Jack preference

## Troubleshooting

### Bot Not Moving
1. Check that **Is Bot Mode Enabled** is checked
2. Verify it's a **1v1 game** (playerCount = 2)
3. Verify only **1 human player** is connected
4. Check Console for bot activation messages
5. Try using **"Start Bot Game"** context menu for immediate testing

### Bot Skipping Turns or Recursive Moves
1. **FIXED**: Bot no longer manually calls `EndTurnCheck()`
2. Bot now relies on natural turn flow via client animations
3. Turn ends automatically when animation completes and `TellServerTurnEnded()` is called

### Bot Making Invalid Moves or False Pişti
1. **IMPROVED**: Bot now has better capture logic with detailed debugging
2. Bot only captures cards that exactly match its card value or uses Jack to capture all
3. Pişti detection works correctly with bot moves (only triggers for 2-card captures with same value)

### Bot Interfering with Normal Games
1. Bot only activates when conditions are met (1v1, 1 human)
2. Bot can be disabled anytime via **Toggle Bot Mode**
3. Bot automatically deactivates when disabled or when more players join

### Game Not Starting with Bot
1. **FIXED**: Game now starts properly when bot mode is enabled and only 1 human player is connected
2. Server recognizes bot as virtual second player for game start conditions
3. Card dealing works correctly for 1 human + 1 bot setup

## Future Enhancements
- **Superpower Integration**: Bot could use superpowers strategically
- **Difficulty Levels**: Multiple AI strategies (Easy, Medium, Hard)
- **Random vs Sequential**: Toggle between random and sequential card selection
- **Smart Strategy**: More advanced card selection based on game state
- **4-Player Bot Support**: Extend to 2v2 games with multiple bots
