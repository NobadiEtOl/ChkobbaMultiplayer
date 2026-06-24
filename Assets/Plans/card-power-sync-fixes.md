# Project Overview 
- **Game Title**: Chkobba Multiplayer
- **High-Level Concept**: A digital multiplayer implementation of the traditional Tunisian card game "Chkobba", augmented with special power card modifications such as "Kapkaç", "Yandım Anam", and "Kopyala Yapıştır" to create a dynamic, competitive card experience.
- **Players**: Online Multiplayer (Host-Client with Reconnection and Host Migration systems) / Local vs AI
- **Inspiration / Reference Games**: Traditional Chkobba (Scopa) card games
- **Tone / Art Direction**: Stylized 2D visual layout with high-impact visual effects for card power-ups
- **Target Platform**: WebGL
- **Screen Orientation / Resolution**: Landscape (1920x1080)
- **Render Pipeline**: Universal Render Pipeline (URP - Medium_PipelineAsset)

# Game Mechanics 
## Core Gameplay Loop
Players take turns playing cards from their hands to match and capture cards from the center deck. Points are scored based on the number of captured cards, capturing specific high-value cards, and scoring "Pişti" or "Chkobba" (clearing the board). The gameplay loop includes high-stakes "Superpowers" which dynamically alter card values and appearances:
1. **Kapkaç**: Instantly changes a card's value to 11 (Jack), making it a valuable capturer.
2. **Yandım Anam**: Instantly changes a card's value to 0, resetting its scoring value.
3. **Kopyala Yapıştır**: Copies both the visual skin and logical value of another card on the board.

## Controls and Input Methods
The game uses point-and-click / touch-and-drag interactions. Players drag cards from their hands to play them, or select cards to target with superpower actions. Drag-and-drop and tap gestures provide responsive card play.

# UI
In-game HUD displays player hands, center deck, player score piles, and superpower activation buttons. During reconnection, a loading/reconnection overlay is presented to obscure the state reconstruction from the player, and then fades out once synchronization is achieved.

# Key Asset & Context
- **Assets/Scripts/GameManager.cs**: Central manager handling game flow, turn timing, special power activations (`OnYandimAnamCardChanged`, `OnKopyalaYapistir`, `OnKapkacCardChanged`), and client reconnection state reconstruction (`RestoreVisualStatesFromMoveChain`, `ApplyCopiedCardMap`, `ApplyCardPowerEffects`).
- **Assets/Scripts/CardInteraction.cs**: Script component attached to every individual card GameObject. It tracks the card's active power effect (`activePowerEffect`), original unmodified attributes (`originalCardID`, `originalSprite`), and spawned VFX instances (`yandimAnamSpriteInstance`, `yandimAnamEffectInstance`).

---

# Implementation Steps

## Step 1: Implement Parent-Traversal Scoring Pool Helper in GameManager
- **Description**: Add `IsCardInScoringPool(CardInteraction targetCard)` in `GameManager.cs` to dynamically determine if a card has been collected into any player's private scoring pool or Pişti pool. This prevents spawning heavyweight particles on small cards in score pools.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## Step 2: Fix Logic State & Caching in Yandım Anam Instant Reconstruction Branch
- **Description**: Modify `OnYandimAnamCardChanged` in `GameManager.cs` to ensure that when `instant == true` is passed (e.g. during reconnection state reconstruction):
  1. The card value is correctly updated to `0` via `cardInteraction.SetCardValue(0)`.
  2. `cardInteraction.activePowerEffect` is set to `"YandımAnam"`.
  3. `cardPowerEffects[cardUniqueID]` is cached for persistence.
  4. The spawned sprite overlay prefab is assigned to `cardInteraction.yandimAnamSpriteInstance` to prevent leaking visual assets on card resets.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## Step 3: Implement Visual Restoration Guard (Skip Captured Cards)
- **Description**: Update the visual state restorer methods (`RestoreKopyalaYapistirVisual`, `RestoreKapkacVisual`, `RestoreYandimAnamVisual`) within `RestoreVisualStatesFromMoveChain` to call the `IsCardInScoringPool()` helper. If a card is determined to be captured inside a private score pool, skip spawning visual overlays or triggering visual change routines.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

## Step 4: Add Instant Support to CopyEffectVisuals
- **Description**: Update `CopyEffectVisuals(CardInteraction targetCard)` to accept an optional `bool instant = false` parameter and forward it to `OnKapkacCardChanged` and `OnYandimAnamCardChanged`. Update `OnKopyalaYapistir` to pass its own `instant` flag to `CopyEffectVisuals`, bypassing cascading asynchronous coroutine transitions during reconnection.
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

## Step 5: Resolve Transitive Copy Chains and Safe Asset Copying in ApplyCopiedCardMap
- **Description**: Refactor `ApplyCopiedCardMap` in `GameManager.cs` to resolve transitive chains (e.g., A copies B, B copies C) to their ultimate source card. Copy from the ultimate source card using its immutable `originalCardID` and `originalSprite` (backing up if they are null) to avoid rendering desyncs if intermediate source cards have been swept or modified.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

---

# Verification & Testing
1. **Yandım Anam State Integration Check**: Run a test or simulate a client reconnection after "Yandım Anam" is played. Assert that the reconnected client sees the card with the fire overlay, its value is logically synchronized to 0, and playing a match with it matches value 0.
2. **Scoring Pool Visual Check**: Capture a card that has "Kapkaç" or "Yandım Anam" active into player scoring pools. Reconnect a client. Verify that the score pool transforms contain clean, scaled-down cards without full-size particle systems or floating fire overlays.
3. **Transitive Copy Restoration Check**: Perform a "Kopyala Yapıştır" action where Card A copies Card B, which itself has "Kapkaç" active. Perform a sweep/round reset. Reconnect a client. Verify that Card A's visuals correctly restore to resemble Card B's original design and logical effects, with no missing sprites or slow animations during load.
