# Project Overview
- Game Title: Chkobba Multiplayer (with Single Player mode)
- High-Level Concept: Card game with superpowers and damage mechanics.
- Players: Single player (vs AI) and Multiplayer.
- Unity Version: 6000.2.6f2
- Input System: Legacy Input Manager
- Render Pipeline: URP (Medium_PipelineAsset)

# Game Mechanics
## Core Gameplay Loop
- Players capture cards from the center.
- Captures deal damage to opponents.
- Captures now grant gold, using the same logic as multiplayer.
- Gold is used to activate superpowers.
## Controls and Input Methods
- Card selection and playing via touch/mouse.

# UI
- Gold display in the `SuperPowerSpawner` (`goldDisplayText`).
- Gold amount reflected in the `HesapMakinesiController`.
- Animated gold popups when cards are captured.

# Key Asset & Context
- `SinglePlayerModeController.cs`: Main logic for single-player.
- `SuperPowerSpawner.cs`: Manages gold and superpowers.
- `RunManager.cs`: Handles single-player persistence.
- `HesapMakinesiController.cs`: Updates the gold UI.

# Implementation Steps
## 1. Update Run Persistence
- **Description**: Add gold storage to the single-player run persistence system.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes
- **Details**:
    - Modify `RunManager.RunProgressData` class to include `public int currentGold`.
    - Update `RunManager.SaveRunProgress` to store the gold value in `PlayerPrefs`.
    - Update `RunManager.LoadRunProgress` to retrieve the gold value from `PlayerPrefs`.

## 2. Implement Gold Gain in Single Player
- **Description**: Trigger gold gain logic when a capture happens in single player.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No
- **Details**:
    - Modify `SinglePlayerModeController.ProcessPlayerMoveCoroutine`.
    - After updating `GameManager.LocalInstance.centerCards` with the captured cards, call `SuperPowerSpawner.LocalInstance.OnLocalCapture(cardId)`.
    - This will calculate the gold, update the UI, and show the popup using the existing multiplayer logic.

## 3. Synchronize Gold with Save System
- **Description**: Ensure the current gold amount is included when the game saves progress.
- **Assigned role**: developer
- **Dependencies**: Step 1, Step 2
- **Parallelizable**: No
- **Details**:
    - Add `public int GetCurrentGold() => currentGold;` to `SuperPowerSpawner.cs`.
    - Update `SinglePlayerModeController.BuildRunProgressData` to read the current gold from `SuperPowerSpawner.LocalInstance.GetCurrentGold()`.
    - This ensures that every time `CheckRoundEndConditions` or `OnStageCleared` is called, the gold is saved.

## 4. Restore Gold on Run Resume
- **Description**: Restore the player's gold amount when a single-player run is resumed.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No
- **Details**:
    - In `SinglePlayerModeController.StartNewRun` (or the logic that handles run restoration), read the gold from the loaded `RunProgressData`.
    - Call `SuperPowerSpawner.LocalInstance.SetGold(savedGold)` to initialize the system with the correct amount.

# Verification & Testing
- **Manual Test**: Start a new single-player run. Play cards and make a capture. Verify:
    - Gold amount increases.
    - Gold popup appears.
    - Gold text in `SuperPowerSpawner` and `HesapMakinesiController` updates.
- **Persistence Test**: Play until you have some gold, then exit the game (or stop Play Mode). Restart the run and verify the gold amount is restored correctly.
- **Edge Case**: Verify gold does not exceed the `maxGold` cap (if applicable).
