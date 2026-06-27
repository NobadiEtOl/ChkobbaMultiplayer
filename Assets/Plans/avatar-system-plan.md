# Project Overview
- Game Title: Chkobba Multiplayer
- High-Level Concept: A multiplayer adaptation of the traditional Tunisian card game Chkobba, featuring profile customization with layered, visual avatars and side-specific player views.
- Players: 1v1 (Single Player vs AI or Online Multiplayer), 2v2 (Online Team-based Multiplayer)
- Inspiration / Reference Games: Traditional Chkobba card games
- Tone / Art Direction: Colorful, casual, stylized cartoon-style avatar elements.
- Target Platform: WebGL, PC
- Screen Orientation / Resolution: Landscape (1920x1080)
- Render Pipeline: URP (Medium_PipelineAsset)

# Game Mechanics
## Core Gameplay Loop
Players play cards from their hand to the center table to capture matching cards (by value or combination), aiming to score points through specific card captures (e.g., getting the 7 of Dinari/Gold, capturing the majority of cards/dinari, or achieving "Chkobba" by clearing the table). Points are accrued across multiple rounds until a player/team reaches the winning threshold.

## Controls and Input Methods
- **Mouse/Touch**: Select and drag/click cards to play or capture cards on the table.
- **UI Customization Buttons/Arrow Keys**: Pre-game interface for cycling avatar customization layers (base, hair, eyes, eyebrows, mouth) and entering player name.

# UI
The UI is built using Unity Canvas (uGUI) with `TMPro` for typography.
- **Side Panels**:
  - `PlayerHolderSide1` (Side 0): Friendly/Local Team Side. Displays local player in Slot 1 and their teammate (in 2v2 mode) in Slot 2.
  - `PlayerHolderSide2` (Side 1): Opponent Team Side. Displays Opponent 1 in Slot 1 and Opponent 2 (in 2v2 mode) in Slot 2.
- **Avatar Presentation**:
  - Layered rendering of avatar layers (Base Face -> Hair -> Eyes -> Eyebrows -> Mouth) as overlapping raw image/sprite components.
  - Name plate texts beneath the layered avatar displays.

# Key Asset & Context
1. **`Assets/Scripts/PlayerCustomData.cs`**:
   - Holds customizable profile information (`PlayerName` as a `string` or fixed string, and integer indices for customization layers).
   - Inherits `INetworkSerializable` to define custom binary replication.
2. **`Assets/Scripts/Player.cs`**:
   - Represents a network player entity. Synchronizes `CustomData` and seat number (`AbsolutePlayerNumber`) using `NetworkVariable`s.
   - Submits client-side customization values to the server via ServerRpc.
3. **`Assets/Scripts/SideManager.cs`**:
   - Orchestrates the local visual assignment of player slots based on local seat perspective.
   - Maps seats dynamically so teammate and local players are grouped together on Side 0, while opponents are mapped to Side 1.

# Implementation Steps

### Step 1: Resolve Serialization NullReferenceException Bug
- **Description**: Add safety guards inside `PlayerCustomData.NetworkSerialize<T>()` to prevent `NullReferenceException` when `PlayerName` string is null during network variables' default initialization.
  - Modify `Assets/Scripts/PlayerCustomData.cs` to set `PlayerName` to an empty string `string.Empty` if it is null when starting serialization.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: No

### Step 2: Set Up Pre-Game Customization Flow via PlayerPrefs
- **Description**: Verify that the pre-game customization window is correctly saving selection indices using the existing `AvatarCustomization` script.
  - Verify that `PlayerPrefs` keys match perfectly between `Player.cs` ("AvatarBaseFaceIndex", "AvatarHairIndex", etc.) and `AvatarCustomization.cs` / `SideManager.cs`.
- **Assigned role**: explorer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 3: Connect Client Customization Load & Replication Flow
- **Description**: Ensure the `Player` network script loads saved profile data from the local device and replicates it correctly to the server upon joining.
  - `Player.LoadAndSendCustomization()` gets called when the local player's network object spawns.
  - Ensure the client calls `SubmitCustomizationServerRpc(localData)` immediately to pass their cached `PlayerCustomData` to the server, which updates `CustomData.Value`.
- **Assigned role**: developer
- **Dependencies**: Step 1, Step 2
- **Parallelizable**: No

### Step 4: Validate Side-Specific Perspective Mapping (Seat-to-Side Routing)
- **Description**: Verify and refine the perspective assignment logic inside `SideManager.UpdateAllSides()`.
  - Validate that `localPlayerNo` (retrieved via `DeckController.LocalInstance.thisPlayerNumber`) maps correctly.
  - **Side 0 (Local Team)** must display the local player (`localPlayerNo`) in Slot 1, and the local teammate (`(localPlayerNo + 2) % 4` in 2v2) in Slot 2.
  - **Side 1 (Opponents)** must display opponents (`(localPlayerNo + 1) % 4` and `(localPlayerNo + 3) % 4` in 2v2, or `(localPlayerNo + 1) % 2` in 1v1) in slots.
- **Assigned role**: developer
- **Dependencies**: Step 3
- **Parallelizable**: No

### Step 5: Implement index-based sprite mapping
- **Description**: Confirm that `SideManager.SetSlotData` maps indices to actual sprite assets on the local device, avoiding sending actual image textures over the wire.
  - Confirm the array mappings (`faceBaseSprites`, `hairSprites`, etc.) in `SideManager` match the customization index ranges.
- **Assigned role**: developer
- **Dependencies**: Step 4
- **Parallelizable**: Yes

# Verification & Testing
1. **Single Client Testing**:
   - Run the game in single-player or bot mode. Check that the bottom/left side panel (Side 0 Slot 1) displays the custom avatar and name selected in the profile scene.
2. **Multiplayer Join Synchronization**:
   - Start a Host and connect a Client.
   - Verify that the Host sees the Client's customized avatar and name in the opponent slot (Side 1 Slot 1).
   - Verify that the Client sees the Host's customized avatar and name in their opponent slot (Side 1 Slot 1), and sees their own custom avatar in Side 0 Slot 1.
3. **No Crash Verification**:
   - Ensure that no NullReferenceExceptions are logged in the Console during player connection, spawning, or network scene synchronization.
