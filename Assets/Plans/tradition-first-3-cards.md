# Project Overview
- Game Title: Chkobba Multiplayer
- High-Level Concept: A multiplayer adaptation of the traditional Tunisian card game Chkobba (similar to Pişti) featuring special powers, host migration, and reconnection synchronization.
- Players: Multiplayer (2 or 4 players, local/online, and VS AI bots)
- Inspiration / Reference Games: Traditional Chkobba / Scopa / Pişti
- Tone / Art Direction: Stylized vintage/retro cartoon with robust UI
- Target Platform: WebGL / PC
- Render Pipeline: URP (Medium Pipeline Asset)

# Game Mechanics
## Core Gameplay Loop
Players are dealt hands of 4 cards. They take turns playing a card from their hand to the center carpet. If the card matches the value of the top card in the center (or sum matching standard Chkobba capture rules), they capture the cards. Captured cards are moved to the player's pool (and pişti pool for double matches). When all cards are played, a new round of hand dealing occurs until the deck is depleted. Scores are calculated based on captured cards, gold values, and piştis.
The first 3 center cards dealt initially in a round are traditionally face down, visible only to the player who eventually captures them.

## Controls and Input Methods
- Touch/Click gestures to drag or select cards from own hand.
- Touch/Click on player/opponent pools to showcase their captured cards list.

# UI
Captured pool showcase displays a spread of captured cards. When showcasing the opponent's captured pool:
- The first 3 dealt center cards must stay face down, positioned at the very beginning of the layout, and remain unsorted.
- All subsequent captured cards are sorted by kind and value, appearing face-up after the face-down cards.
- When showcasing the owner's own pool, all cards (including the first 3) are sorted and face-up as normal.

# Key Asset & Context
- **CardInteraction.cs** (`Assets/Scripts/CardInteraction.cs`): Added `public bool isFirstThreeDealtCard` tracking field.
- **Server.cs** (`Assets/Scripts/Server.cs`): Holds `public List<string> firstThreeDealtCardIds` list of the 3 initially dealt face-down center card IDs. Populated on initial deal and serialized into game state.
- **SerializableGameState.cs** (`Assets/Scripts/SerializableGameState.cs`): Holds `public SerializableStringList firstThreeDealtCardIds` field, synchronized over network to all clients.
- **DeckController.cs** (`Assets/Scripts/DeckController.cs`): Adjusts `ShowcasePlayerPoolCards` to display face-down first three cards at the beginning when viewed by opponents, bypassing standard sorting.
- **GameManager.cs** (`Assets/Scripts/GameManager.cs`): Resolves the reconstruction bug where all non-top center cards were incorrectly assumed to be face-down during reconnection/migration state application.

---

# Detailed Findings & Suspicion Report
Your suspicion is highly accurate! There is indeed an issue in how reconnection center rebuilding treats card rotations.
Currently, during `RebuildCardContainers` (which is executed only during state reconstruction on reconnection/host migration), the code loops through the list of center cards and forces **every card except the top one** to be face down:
```csharp
if (i == centerList.Count - 1) // Last card (top card) should be face up
{
    cardObject.transform.rotation = Quaternion.Euler(centerRotation.x + 180, ...);
}
else
{
    cardObject.transform.rotation = Quaternion.Euler(centerRotation.x, ...);
}
```
If players have played cards to the center during active gameplay (which are naturally face-up), and then a reconnection or host migration occurs, the reconstructed center will incorrectly render those played cards as **face-down**, rather than only the first 3 dealt cards. 
By introducing the `firstThreeDealtCardIds` tracking and syncing it via the game state snapshot, we can determine the exact face-up/face-down rotation state of each center card during reconstruction.

---

# Implementation Steps

### Step 1: Update CardInteraction tracking field
- **Description**: Add `public bool isFirstThreeDealtCard` flag to `CardInteraction.cs`.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2: Update SerializableGameState to support synchronization
- **Description**: 
  - Add `public SerializableStringList firstThreeDealtCardIds` to `SerializableGameState` struct.
  - Update `NetworkSerialize` in `SerializableGameState.cs` to serialize this new field.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 3: Populate and manage tracking list on Server
- **Description**:
  - Add `public List<string> firstThreeDealtCardIds = new List<string>();` to `Server.cs`.
  - In `ResetAllServerVariables`, clear `firstThreeDealtCardIds`.
  - In `DealCardsToCenter()`, populate `firstThreeDealtCardIds` with the first 3 keys of `centerCardsDict`.
  - In `BuildGameStateSnapshot()`, populate `snapshot.firstThreeDealtCardIds` from the server list.
  - In `ApplyGameStateToServer()`, restore `firstThreeDealtCardIds` list from `snapshot.firstThreeDealtCardIds`.
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

### Step 4: Apply tracking state and fix center reconstruction rotation
- **Description**:
  - In `ApplyGameStateCoroutine` or `RebuildCardContainers` of `GameManager.cs`, first clear the `isFirstThreeDealtCard` flag on all cards. Then, set it to `true` for any card ID in `snapshot.firstThreeDealtCardIds`.
  - In `RebuildCardContainers` of `GameManager.cs`, instead of checking `i == centerList.Count - 1` to determine if a card is face-up, check if `cardInteraction.isFirstThreeDealtCard` is `false`. If it is `false` (meaning it's not one of the first 3 dealt cards), set its rotation to face-up (`centerRotation.x + 180`). Otherwise, set it to face-down (`centerRotation.x`).
- **Assigned role**: developer
- **Dependencies**: Step 3
- **Parallelizable**: No

### Step 5: Implement face-down unsorted behavior in Showcase
- **Description**:
  - In `ShowcasePlayerPoolCards(int poolIndex)` of `DeckController.cs`, determine if the player viewing is the owner (`poolIndex == 0`).
  - If `poolIndex == 0` (owner), sort all cards and show them face-up as normal.
  - If `poolIndex != 0` (opponent/other player), filter the pool cards into `firstThreeDealtCards` (where `isFirstThreeDealtCard` is `true`) and `otherCards` (where `isFirstThreeDealtCard` is `false`).
  - Sort only `otherCards` according to kind and value. Keep `firstThreeDealtCards` unsorted.
  - Position `firstThreeDealtCards` at the beginning of the layout (index 0 onwards), and render them **face-down** (`Quaternion.Euler(-90, 0, 0)`). 
  - Position `otherCards` right after them, and render them **face-up** (`Quaternion.Euler(90, 0, 0)`).
- **Assigned role**: developer
- **Dependencies**: Step 4
- **Parallelizable**: No

---

# Verification & Testing
1. **Initial Deal Test**: Start a new game. Verify that the first 3 cards dealt to the center are face down, and the 4th card is face up.
2. **Normal Play Test**: Play cards to the center. Verify they are dealt face up.
3. **Capture & Showcase Test (Owner)**: Capture the center. Showcase your own captured pool. Verify that all cards (including the first 3 center cards) are sorted by suit and value and shown face up.
4. **Capture & Showcase Test (Opponent)**: Capture the center. Let the opponent showcase your captured pool. Verify that the first 3 cards are positioned at the beginning, shown face down, and are not sorted, while the rest are sorted and face up.
5. **Reconnection & State Rebuild Test**: Capture the center cards, play some more cards, then disconnect and reconnect. Verify that the center cards played during gameplay remain face up, and the reconstructed captured pool maintains correct face-down/face-up visibility and positioning.
