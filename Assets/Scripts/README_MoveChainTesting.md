# Move Chain System Testing Guide

## Overview
The Move Chain System provides reliable desync detection by tracking all game moves and superpower effects in a chain that can be validated between server and clients.

## How to Test

### 1. Setup the System
1. **Add MoveChainInitializer** to your scene (any GameObject)
2. **Add MoveChainTestRunner** to your scene for automated testing
3. The system will automatically initialize when the scene starts

### 2. Manual Testing via Context Menus

#### In GameManager (Right-click on GameManager component):
- **"Test Desync - Play Card Locally Only"** - Plays first card locally without server sync (creates desync)
- **"Test Desync - Superpower Effect Locally Only"** - Applies Kapkaç effect locally only (creates desync)
- **"Test Chain Validation"** - Validates client vs server chains

#### In MoveChainIntegrator (Right-click on component):
- **"Trigger Periodic Validation Now"** - Forces immediate chain validation
- **"Force Desync Test"** - Adds fake move to client chain (creates desync)
- **"Log All Chain Stats"** - Shows statistics for all chains

#### In MoveChainTestRunner (Right-click on component):
- **"Run All Tests"** - Runs complete test suite
- **"Test 1: System Initialization"** - Checks if all components are ready
- **"Test 2: Basic Move Recording"** - Tests move recording functionality
- **"Test 3: Desync Detection"** - Tests desync detection
- **"Test 4: Superpower Effect Tracking"** - Tests superpower tracking
- **"Cleanup Test Data"** - Resets all chains
- **"Show Test Status"** - Shows current test results

### 3. Testing Scenarios

#### Scenario 1: Local Card Play Desync
1. Start a game and get some cards in hand
2. Use **"Test Desync - Play Card Locally Only"** in GameManager
3. This will:
   - Record a card play locally
   - Remove the card from local hand
   - NOT send the move to server
4. The periodic validation (every 5 seconds) will detect this desync
5. Check console for desync detection messages

#### Scenario 2: Superpower Effect Desync
1. Start a game and get some cards in hand
2. Use **"Test Desync - Superpower Effect Locally Only"** in GameManager
3. This will:
   - Record a Kapkaç effect locally
   - NOT sync the effect with server
4. The system will detect this desync when chains are compared

#### Scenario 3: Forced Desync Test
1. Use **"Force Desync Test"** in MoveChainIntegrator
2. This adds a fake move (ID 999) to the client chain
3. Immediate validation will detect the desync

### 4. What to Look For

#### Console Messages:
- ✅ **Chains are in sync** - System working correctly
- ❌ **DESYNC DETECTED** - Desync found and reported
- 🔄 **Periodic validation** - Automatic checks running
- 📊 **Chain statistics** - Move counts and types

#### Desync Detection:
- **Move Mismatch**: Different moves at same position
- **Version Mismatch**: Different number of moves
- **Hash Mismatch**: Integrity check failed

### 5. Integration Points

The system automatically hooks into:
- **Server.GetMove()** - Card plays and captures
- **Superpower methods** - Kapkaç, Yandım Anam, Kopyala Yapıştır, Bomba
- **SuperPowerController.PowerActivated()** - Superpower activations

### 6. Troubleshooting

#### "MoveChainTracker not found" errors:
- Make sure MoveChainInitializer is in the scene
- Check that Server and GameManager components exist
- Verify the system initialized in Start()

#### No desync detection:
- Check if periodic validation is enabled (default: every 5 seconds)
- Verify both client and server trackers exist
- Look for console messages about validation

#### Performance issues:
- Adjust `chainValidationInterval` in MoveChainIntegrator
- Consider disabling periodic validation in production
- Monitor chain sizes (reset chains between rounds)

### 7. Production Use

For production:
1. **Disable periodic validation** (set `enableDesyncDetection = false`)
2. **Use manual validation** when needed
3. **Reset chains** at round start
4. **Monitor chain sizes** to prevent memory issues

### 8. Custom Testing

To create custom test scenarios:
1. Use `MoveChainTracker.ClientInstance.RecordCardPlay()` for local moves
2. Use `MoveChainTracker.ClientInstance.RecordSuperpowerEffect()` for local effects
3. Compare chains using `clientChain.ValidateAgainst(serverChain, out mismatchIndex)`
4. Check `mismatchIndex` to see where desync occurred

## Example Test Flow

```
1. Start game → System auto-initializes
2. Play some cards normally → Chains stay in sync
3. Use "Test Desync - Play Card Locally Only" → Creates desync
4. Wait for periodic validation (5 seconds) → Desync detected
5. Check console → See desync details
6. Use "Test Chain Validation" → Manual validation
7. Use "Cleanup Test Data" → Reset for next test
```

This system provides comprehensive desync detection while maintaining minimal impact on existing game logic.
