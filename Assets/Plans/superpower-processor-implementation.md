# Project Overview
- Game Title: Chkobba Multiplayer
- High-Level Concept: A card game with superpowers and jokers, supporting both singleplayer (vs AI) and multiplayer.
- Players: Single player (vs AI), Multiplayer (using Unity Netcode).
- Render Pipeline: URP (Medium_PipelineAsset).
- Target Platform: Android.
- Input System: Legacy Input Manager.

# Game Mechanics
## Superpower Integration
The goal is to enable the existing superpower logic in singleplayer mode by abstracting the power execution into a processor pattern, similar to `IMoveProcessor`. This allows the same "trigger" (token activation) to work in both modes while keeping the network-heavy multiplayer logic isolated.

# Key Asset & Context
- `IPowerProcessor`: New interface for power execution.
- `MultiplayerPowerProcessor`: Implementation for multiplayer (sends RPCs).
- `SingleplayerPowerProcessor`: Implementation for singleplayer (routes to `SinglePlayerModeController`).
- `GameManager.cs`: Updated to hold and use `IPowerProcessor`.
- `SinglePlayerModeController.cs`: Updated to handle local power execution logic.
- `CardInteraction.cs`: Updated to use `IPowerProcessor` for selection-based powers.

# Implementation Steps

## Phase 1: Abstraction
1. **Create `IPowerProcessor` interface** in `Assets/Scripts/IPowerProcessor.cs`.
   - Methods: `ExecutePeek()`, `ExecuteBayaBayaBak()`, `ExecuteBlockNextPlayer()`, `ExecuteBomba()`, `ExecuteKapkacSelection()`, `ExecuteKapkacOnCard(string cardId)`, `ExecuteSwap(string myCardId, string opponentCardId)`, etc.
   - Assigned role: developer
   - Parallelizable: No

2. **Implement `MultiplayerPowerProcessor`** in `Assets/Scripts/MultiplayerPowerProcessor.cs`.
   - Wrap existing `GameNetworkRelay` RPC calls.
   - Assigned role: developer
   - Parallelizable: Yes (with Step 3)

3. **Implement `SingleplayerPowerProcessor`** in `Assets/Scripts/SingleplayerPowerProcessor.cs`.
   - Route calls to `SinglePlayerModeController`.
   - Assigned role: developer
   - Parallelizable: Yes (with Step 2)

## Phase 2: Integration
4. **Update `GameManager.cs`**:
   - Add `private IPowerProcessor powerProcessor;`.
   - Initialize `powerProcessor` in `InitializeMoveProcessor()` (assigning either Multi or Single based on mode).
   - Refactor superpower activation methods (e.g., `UsePeekOpponentCardPower`, `ActivateBombaPower`) to call `powerProcessor` instead of direct `networkRelay` calls.
   - Assigned role: developer
   - Dependencies: Phase 1

5. **Update `CardInteraction.cs`**:
   - Refactor `OnMouseDown` / power selection logic (Kapkaç, Yandım Anam) to use `GameManager.LocalInstance.PowerProcessor` (or a helper method in GameManager) instead of direct `networkRelay` calls.
   - Assigned role: developer
   - Dependencies: Phase 1

## Phase 3: Singleplayer Logic
6. **Update `SinglePlayerModeController.cs`**:
   - Implement handlers for all superpower methods.
   - **Example (Peek)**: Select random card from `opponentHandList`, trigger `DeckController` reveal animation.
   - **Example (Bomba)**: Clear `localCenterCards`, destroy center card objects.
   - **Example (Swap)**: Exchange card IDs between player hand and opponent hand list.
   - Assigned role: developer
   - Dependencies: Phase 1 & 2

# Verification & Testing
- **Multiplayer Regression**: 
  - Start a multiplayer game.
  - Activate a "Bomba" or "Peek" token.
  - Verify that the correct RPCs are sent and the effect is synchronized across clients.
- **Singleplayer Validation**:
  - Start a singleplayer game.
  - Spawn a superpower (via kind match).
  - Activate the token.
  - Verify that the power effect (e.g., center clearing for Bomba, opponent card reveal for Peek) occurs correctly and updates the local state in `SinglePlayerModeController`.
- **UI Check**: Ensure the `InfoBox` messages and "dual selection" instructions still appear correctly in both modes.
