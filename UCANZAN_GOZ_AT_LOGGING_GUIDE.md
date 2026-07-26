# UcundanGözAt Power - Complete Logging Guide

## Overview
All [UcundanGözAt] logs have been added to track the power execution through the entire system. Use these logs to identify exactly where the power fails if it locks the game.

## Expected Log Sequence (Normal Execution)

### 1. Power Activation (SuperPowerController.cs)
```
[UcundanGözAt] ActivatePower() called
[UcundanGözAt] Calling PowerActivated()
[UcundanGözAt] Delegating to PowerOrchestrator.ExecutePeekOpponentCard()
```

**Failure Points:**
- If you don't see "ActivatePower() called" → Power button not being clicked/registered
- If you don't see "GameManager.LocalInstance is null" → GameManager not initialized
- If you see "PowerOrchestrator is null" → PowerOrchestrator not created

### 2. Power Orchestration (PowerOrchestrator.cs)
```
[UcundanGözAt] PowerOrchestrator.ExecutePeekOpponentCard() called
[UcundanGözAt] PowerOrchestrator: Adapter validation passed
[UcundanGözAt] PowerOrchestrator: Delegating to modeAdapter.ExecutePeekOpponentCard()
[UcundanGözAt] PowerOrchestrator: modeAdapter.ExecutePeekOpponentCard() completed successfully
[UcundanGözAt] PowerOrchestrator: Calling PersistGameStateAfterPower()
[UcundanGözAt] PowerOrchestrator.ExecutePeekOpponentCard() execution complete
```

**Failure Points:**
- If you see "ValidateAdapter() failed - modeAdapter is null" → ModeAdapter not initialized
- If you see an exception in modeAdapter.ExecutePeekOpponentCard() → Check the exception details

### 3. SinglePlayer Implementation (SinglePlayerModeController.cs)
```
[UcundanGözAt] SinglePlayerModeController.ExecutePeekOpponentCard() called
[UcundanGözAt] SinglePlayerModeController: Opponent has X cards in hand
[UcundanGözAt] SinglePlayerModeController: Selected random card index X
[UcundanGözAt] SinglePlayerModeController: Calling GameManager.OnPeekOpponentCardSynced(opponentNo=1, cardIndex=X)
[UcundanGözAt] SinglePlayerModeController: GameManager.OnPeekOpponentCardSynced() returned
[UcundanGözAt] SinglePlayerModeController: Calling ShowcaseSuperPower()
[UcundanGözAt] SinglePlayerModeController: Calling SaveRunState()
[UcundanGözAt] SinglePlayerModeController.ExecutePeekOpponentCard() completed successfully
```

**Failure Points:**
- If you see "Opponent hand is empty" → No opponent cards to peek (shouldn't happen in normal play)
- If you see "GameManager.LocalInstance is null" → GameManager reference lost mid-execution

### 4. Game State Sync (GameManager.cs)
```
[UcundanGözAt] GameManager.OnPeekOpponentCardSynced() called: opponentPlayerNo=1, cardIndex=X
[UcundanGözAt] GameManager: Delegating to deckController.PeekOpponentCard()
[UcundanGözAt] GameManager.OnPeekOpponentCardSynced() completed
```

**Failure Points:**
- If you see "deckController is null" → DeckController not initialized
- If execution stops here → Problem in DeckController.PeekOpponentCard()

### 5. Deck Card Selection (DeckController.cs - PeekOpponentCard)
```
[UcundanGözAt] DeckController.PeekOpponentCard() called: opponentPlayerNo=1, cardIndex=X
[UcundanGözAt] DeckController: thisPlayerNumber=0, playerCount=2
[UcundanGözAt] DeckController: relativeIndex=1, isMine=false
[UcundanGözAt] DeckController: handTransform.childCount=Y
[UcundanGözAt] DeckController: Found card at actualIndex=Z, starting PeekCardAnimation
```

**Failure Points:**
- If you see "GetHandIndex() out of bounds" → Invalid hand transform index
- If you see "Not enough cards in hand" → Hand state inconsistency
- If you see "Card at index X was not found" → Card selection failed

### 6. Card Animation (DeckController.cs - PeekCardAnimation)
```
[UcundanGözAt] DeckController.PeekCardAnimation() started: card=<cardname>, isMine=false
[UcundanGözAt] DeckController: Original state - pos=X, rot=Y, scale=Z
[UcundanGözAt] DeckController: Peek state - peekPos=X, peekRot=Y, peekScale=Z
[UcundanGözAt] DeckController: Starting move to peek position (duration=0.4s)
[UcundanGözAt] DeckController: Move to peek position completed
[UcundanGözAt] DeckController: Pausing for 3s to show card
[UcundanGözAt] DeckController: Pause completed
[UcundanGözAt] DeckController: Starting move back to original position (duration=0.4s)
[UcundanGözAt] DeckController: Move back to original position completed
[UcundanGözAt] DeckController: Restarting auto-rotate for my card
[UcundanGözAt] DeckController: Closing SuperPowerSpawner InfoBox
[UcundanGözAt] DeckController: Calling EndGameplayActionIfTagStartsWith
[UcundanGözAt] DeckController.PeekCardAnimation() completed successfully
```

**Failure Points:**
- If animation starts but doesn't complete → Check TweenMoveTransform or animation system
- If card doesn't return to original position → Animation interrupted
- If InfoBox never closes → SuperPowerSpawner issue
- If you see "SuperPowerSpawner infobox not open or spawner null" → UI state mismatch

## How to Use These Logs

1. **Open Unity Console** while testing in singleplayer mode
2. **Click the UcundanGözAt power button**
3. **Watch the console** for the log sequence above
4. **Identify where logs stop** - that's where the issue occurs
5. **Check the specific failure message** at that point

## Common Lock Scenarios

| Scenario | Look For | Likely Cause |
|----------|----------|--------------|
| Freezes after power click | Logs stop at step 1 or 2 | PowerOrchestrator or adapter null |
| Freezes with card showing | Logs stop at step 5-6 | Animation system issue or incomplete state |
| Card doesn't animate | Logs reach PeekCardAnimation but no animation | TweenMoveTransform problem |
| Card stuck in peek state | Logs show animation started but not completed | WaitForCompletion or yield issue |
| UI doesn't update | Logs show power completed but no UI change | GameManager/ShowcaseSuperPower issue |

## Emergency Debug

If logs are completely missing, check:
1. Are you in singleplayer mode? (multiplayer uses different adapter)
2. Is Debug.Log working? (check other logs in console)
3. Is the power actually being called? (add a breakpoint or check button state)

## Notes
- This logging is specifically for singleplayer mode testing
- The 3-second pause at step 6 is intentional (player sees the card for 3 seconds)
- All logs use the [UcundanGözAt] prefix for easy filtering
- If you see no logs at all, the power button isn't being triggered
