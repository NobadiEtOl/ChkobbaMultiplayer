# Project Overview
- **Game Title**: Chkobba Multiplayer
- **High-Level Concept**: A modern digital, multiplayer-focused card game based on the traditional Tunisian card game Chkobba, enhanced with high-impact gameplay-modifying superpowers.
- **Players**: Online Multiplayer (Host-Client with reconnect, dynamic bot filler and takeover, and automatic Host Migration) / VS AI
- **Inspiration / Reference Games**: Traditional Chkobba (Scopa)
- **Tone / Art Direction**: Polished 2D visual style with modern UI, satisfying card physical interactions, and distinct particle/overlay effects for superpowers.
- **Target Platform**: WebGL
- **Screen Orientation / Resolution**: Landscape / 1920x1080
- **Render Pipeline**: URP (Universal Render Pipeline using Medium_PipelineAsset)

---

# Game Mechanics
## Core Gameplay Loop
Players take turns playing cards from their hand to the center table, attempting to capture cards with matching values or mathematical sums. Standard card gameplay is enhanced by shop-purchasable Superpower Tokens (like Kapkaç, Yandım Anam, Kopyala Yapıştır) which players can play to modify card attributes. 
Kapkaç turns a card value to 11 (Jack), Yandım Anam turns a card value to 0, and Kopyala Yapıştır copies another card's value and visual style. The game persists these states across rounds, reconnects, and host migrations.

## Controls and Input Methods
Point-and-click or drag-and-drop mouse/touch interactions to play cards or cast superpowers on cards.

---

# UI
Dynamic hand layout, central playing grid, and captured pools for each player seat. Visual superpower overlays (such as a fire sprite for Yandım Anam, or a Jack overlay/highlight for Kapkaç) are spawned directly on card GameObjects to clearly communicate their modified state to players. On reconnection or host migration, the UI is reconstructed instantly behind a loading/sync curtain to maintain seamless visual parity.

---

# Key Asset & Context
The visual states of modified cards need to be rebuilt in two distinct scenarios during reconnection/host migration:
1. When a card directly undergoes **Kapkaç** or **Yandım Anam**.
2. When a card copies another card using **Kopyala Yapıştır**, which itself might have an active Kapkaç or Yandım Anam effect.

### Key Files & Methods:
- **Assets/Scripts/GameManager.cs**:
  - `ApplyCardPowerEffects()`: Applies stored card power modifications from the state snapshot.
  - `ApplyCopiedCardMap()`: Reconstructs copied cards from the snapshot.
  - `IsCardInScoringPool()`: Helper method to determine if a card is currently captured.
  - `OnKapkacCardChanged()`, `OnYandimAnamCardChanged()`: Reconstructs card visual/logic values instantly if `instant = true`.
  - `CopyEffectVisuals()`: Handles instant copying of visuals.

---

# Implementation Steps

### Step 1: Update `ApplyCardPowerEffects` in GameManager
- **Description**: Refactor `ApplyCardPowerEffects` in `GameManager.cs` to fully apply both state AND instant visuals during reconnection. Instead of merely modifying card ID values and leaving a comment, call `OnKapkacCardChanged(cardUniqueID, true)` and `OnYandimAnamCardChanged(cardUniqueID, true)`. Include a safety check using `IsCardInScoringPool(cardInteraction)` to apply only the mathematical attributes (no visual overlay/prefab instantiation) if the card is in a player's captured score pool.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2: Update `ApplyCopiedCardMap` to Reconstruct Copy Overlays
- **Description**: Update `ApplyCopiedCardMap` in `GameManager.cs` to trigger visual restoration for copied cards. After setting the `activePowerEffect` string on the target card to match the source card, verify if the effect is not `"none"` and the target card is not in a scoring pool. If so, call `CopyEffectVisuals(targetCard, true)` to instantly instantiate the visual overlay (Kapkaç or Yandım Anam) on the copied card.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

---

# Verification & Testing

### Test Case 1: Kapkaç Reconnection Visual Check
1. Start a multiplayer game session.
2. Draw and play the "Kapkaç" superpower token onto a card in hand or on the center board. Verify the card turns into a Jack with the Kapkaç visual overlay.
3. Close/disconnect the client browser tab.
4. Reconnect the player.
5. Verify that once state synchronization completes, the target card in the player's hand/center has the Kapkaç visual overlay properly rendered and its value remains a Jack.

### Test Case 2: Yandım Anam Reconnection Visual Check
1. Play the "Yandım Anam" superpower token onto a card. Verify the card displays the fire/overlay visual.
2. Trigger client disconnection.
3. Reconnect the player.
4. Verify that the fire visual overlay is correctly restored on the card in its proper hand/center container, and its numerical value is 0.

### Test Case 3: Kopyala Yapıştır Reconnection Visual Check
1. Play "Kapkaç" or "Yandım Anam" on Card A.
2. Play "Kopyala Yapıştır", selecting Card B (target) and Card A (source). Verify Card B successfully copies the look and value of Card A, including the power overlay.
3. Disconnect and reconnect.
4. Verify that Card B retains its copied visuals and active power overlays on reconnection.

### Test Case 4: Scoring Pool Visual Exclusion Check
1. Play a power (e.g. Kapkaç) on a card, then capture it into your score pool.
2. Disconnect and reconnect.
3. Verify that the captured card in the score pool does NOT render any giant visual highlights or particle systems, keeping the pool tidy, but its modified state remains structurally correct for final end-of-round score calculation.
