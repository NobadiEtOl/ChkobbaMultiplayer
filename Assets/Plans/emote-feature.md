# Project Overview
- Game Title: Chkobba Multiplayer
- High-Level Concept: Multiplayer card game with avatar customization and now expressive emotes.
- Players: Single player vs AI, Local/Networking multiplayer.
- Render Pipeline: URP (Medium_PipelineAsset)
- Input System: Both (Legacy + New)
- Unity Version: 6000.2.6f2

# Game Mechanics
## Emote System
- Players can click their own avatar in the `SideManager` UI to trigger an emote.
- Clicking the avatar opens the OS keyboard (Mobile/Tablet). 
    - **Keyboard Configuration**: We will use `TouchScreenKeyboardType.Default` (or `Social`). 
    - **Emoji Extraction**: Since a true "emoji-only lock" is not supported by standard Unity APIs, the system will monitor the input and automatically extract the first valid emoji/character, ignoring any other text.
- Once an emoji is submitted, a speech bubble appears above the avatar's head in the UI.
- The emote is synchronized across the network so all connected players see it in the correct perspective.

## Spam Protection & Blocking
- **Overlap Protection**: A player cannot trigger a new emote while their current one is still visible. The avatar click will be disabled until the bubble vanishes.
- **Spam Cooldown & Block**:
    - **Tracking**: `EmoteManager` will track the timestamp of each emote.
    - **Limit**: If a player attempts to send more than 3 emotes within 5 seconds, the feature is blocked for 15 seconds.
    - **Turkish Notification**: If blocked, a message appears using `UIFeedbackManager`: *"Çok fazla ifade gönderdiğiniz için geçici olarak engellendiniz."*
- **Local Enforcement**: The block is enforced locally to save network bandwidth, but the server will also ignore rapid-fire RPCs from the same client as a secondary security measure.

# UI
## Emote Bubble Prefab
- A UI Prefab containing:
    - **Image Component**: For the bubble sprite (assignable in Inspector).
    - **TextMeshProUGUI**: For displaying the emoji character.
    - **Auto-Destroy/Hide Logic**: The bubble will automatically fade out or destroy itself after 2.5 seconds.
    - **Dynamic Anchoring**: The bubble will be spawned as a child of the `PlayerSlotParent` in `SideManager` and positioned relative to the `BaseFaceImage`.

## Avatar Interaction
- The `SideManager`'s local player avatar image will have a `Button` or `EventTrigger` component added dynamically or via script to detect clicks.

# Key Asset & Context
- **Assets/Scripts/EmoteManager.cs**: New script to manage local logic, keyboard input, spam protection, and bubble spawning.
- **Assets/Scripts/GameNetworkRelay.cs**: Modification to include `SendEmoteServerRPC` and `ShowEmoteClientRPC`.
- **Assets/Scripts/SideManager.cs**: Minor modification to expose references or add click listeners to local avatars.
- **Assets/Prefabs/EmoteBubble.prefab**: To be created for the visual feedback.

# Implementation Steps
## 1. Create Emote Manager & RPCs
- **Description**: Implement `EmoteManager.cs` for local handling and update `GameNetworkRelay.cs` for networking.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## 2. Implement Bubble Prefab and Logic
- **Description**: Create the `EmoteBubble` prefab with TMP support and a script to handle its lifetime.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## 3. UI Integration in SideManager
- **Description**: Update `SideManager.cs` to detect when the local player's avatar is clicked and notify `EmoteManager`.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

## 4. Spam & Block Logic
- **Description**: Implement the `spamCounter` and `blockTimer` in `EmoteManager`, integrating with `UIFeedbackManager` for the Turkish warning.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

# Verification & Testing
- **Manual Test (Local)**: Click your own avatar, verify keyboard opens (mocked in Editor), and bubble appears locally.
- **Multiplayer Test**: Join with two clients, send an emote from P1, verify P2 sees the bubble on P1's avatar in their perspective.
- **Spam Test**: Rapidly send emotes to trigger the block and verify the Turkish warning appears.
- **Cooldown Test**: Verify only one bubble appears at a time per player.
