# Project Overview
- Game Title: Chkobba Multiplayer
- High-Level Concept: Multiplayer card game based on the traditional Tunisian game Chkobba with superpower mechanics.
- Players: Single player, multiplayer 1vs1, and multiplayer 2vs2.
- Render Pipeline: URP

# Game Mechanics
## Side-Specific Pools (2vs2 Mode)
In 2vs2 mode, teammate player absolute seats are grouped into Side 0 and Side 1:
- **Side 0 (Friendly)**: Local Player (Seat 0 relative) and Teammate (Seat 2 relative)
- **Side 1 (Opponent)**: Opponents (Seat 1 and 3 relative)

Instead of maintaining 4 distinct physical and visual player pools (`PlayerPool1` to `PlayerPool4` and `PlayerPiştiPool1` to `PlayerPiştiPool4`), the 2vs2 mode will now use:
- **Side 0 Pool (`PlayerPool1` & `PlayerPiştiPool1`)**: Captures from Seat 0 relative (Local Player) and Seat 2 relative (Teammate) are routed here.
- **Side 1 Pool (`PlayerPool2` & `PlayerPiştiPool2`)**: Captures from Seat 1 relative and Seat 3 relative (Opponents) are routed here.

This transition ensures that teammates visually share their captured cards and made piştis on local client screens, matching the cooperative side-specific gameplay loop.

# UI / Scene Setup
- Physically delete or deactivate `PlayerPool3`, `PlayerPool4`, `PlayerPiştiPool3`, and `PlayerPiştiPool4` GameObjects from the game scene/prefabs.
- Update `GameManager` script's collection of pool transforms so it only registers and uses `PlayerPool1`/`PlayerPool2` and `PlayerPiştiPool1`/`PlayerPiştiPool2` transforms.

# Key Asset & Context
- `Assets/Scripts/GameManager.cs`: Hand and pool initialization, client state capturing (`CaptureVisualSnapshot`), seat index helpers.
- `Assets/Scripts/DeckController.cs`: Card movement animations (`MoveCardsToPlayerPool`), pool index resolution (`GetPoolIndex`), hand index resolution, hand layout card iterations, and end-of-round score showcases.

# Implementation Steps

## Step 1: Hand Index vs Pool Index Separation
- **Description**: Add `GetHandIndex` separate from `GetPoolIndex` inside `DeckController.cs` so player hand transforms are still indexed individually (0 to 3 in 2vs2) while player pool transforms are mapped to Sides (0 and 1).
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: No

## Step 2: Redirect Pools to Sides
- **Description**: Modify `GetPoolIndex(int relativeIndex)` inside `DeckController.cs` to return:
  - `0` (Side 0) for relative index 0 or 2.
  - `1` (Side 1) for relative index 1 or 3.
  This automatically redirects card movements, parenting, and animations to Side 1 & Side 2 pool transforms.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

## Step 3: Scene Cleanup and GameManager Initializations
- **Description**:
  - Remove/deactivate `PlayerPool3`, `PlayerPool4`, `PlayerPiştiPool3`, `PlayerPiştiPool4` GameObjects in the scene.
  - In `GameManager.cs` (around lines 2694-2710), delete/remove the lines attempting to find and add pool/pisti pool indices 3 and 4 to `playerPoolTransforms` and `playerPiştiPoolTransforms`.
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: Yes

## Step 4: Handle Aggregated Card Rebuilding
- **Description**: Update `AssignCardsToPlayerPools` and `AssignCardsToPlayerPistiPools` inside `DeckController.cs` to aggregate cards by pool index before resetting offsets and instantiating positions. This prevents overlapping layouts or resetting counters multiple times when teammates both have cards in a shared side pool.
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

## Step 5: Secure Hand Card Layout Calculations
- **Description**: Update hand card counts in `DeckController.cs` (around lines 1172, 1219, 1289) to skip pool transforms using name checks (`child.name.StartsWith("PlayerPool")`) rather than hardcoded index counts (`counter <= 1`), avoiding layout shifting or hidden cards in hands 3 and 4.
- **Assigned role**: developer
- **Dependencies**: Step 3
- **Parallelizable**: Yes

## Step 6: Fix Visual Snapshot Audits
- **Description**: Update `CaptureVisualSnapshot()` in `GameManager.cs` to audit pools and pişti pools precisely for side-specific routing. Avoid duplicate card mappings by assigning combined pool items to representative relative indices (relative 0 for Side 0, relative 1 for Side 1), ensuring 100% accurate client state serialization for reconnection and host migration.
- **Assigned role**: developer
- **Dependencies**: Step 4
- **Parallelizable**: No

## Step 7: Update Score Showcases
- **Description**: Adjust `ShowcasePiştisAndPoints2v2()` inside `DeckController.cs` to fetch Side 0 and Side 1 cards directly from pool transforms at index 0 and 1, avoiding duplicate card parsing loops.
- **Assigned role**: developer
- **Dependencies**: Step 4
- **Parallelizable**: Yes

# Verification & Testing
1. **Local Test (1vs1 and 2vs2 Layout)**: Launch game and ensure cards are animated correctly to Side 1 / Side 2.
2. **Reconnection & State Audit**: Reconnect a client in 2vs2 mode and verify their captured side pool renders exactly identical to other clients.
3. **Host Migration**: Disconnect the host mid-game to trigger host migration, verifying state serialization and restoration completes without loss of pooled cards.
