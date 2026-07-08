# Project Overview
- Game Title: Chkobba Multiplayer
- High-Level Concept: Multiplayer card game with expressive emotes.
- Players: Multiplayer & Quickplay (Local) mode.
- Render Pipeline: URP
- Input System: Both

# Game Mechanics
## Emote System Refinement
- **Local Mode Support**: If the network is not connected (e.g., Quickplay), emotes will be displayed locally for the player.
- **Robust Click Detection**: Ensure the avatar click is captured by setting up the button properly and disabling raycast blocking from other elements.
- **Editor Simulation**: Maintain Editor-only shortcuts for faster testing.

# Key Asset & Context
- **Assets/Scripts/EmoteManager.cs**: Update to handle local display and better state logging.
- **Assets/Scripts/SideManager.cs**: Update `SetupLocalEmoteTrigger` for reliable click capturing.

# Implementation Steps
## 1. Update EmoteManager.cs
- **Description**: Add support for local emote display when network is not active and improve logs.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## 2. Update SideManager.cs
- **Description**: Refine button attachment logic and clear raycast targets that might overlap the avatar.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

# Verification & Testing
- **Local Test**: Run in Quickplay, click avatar, verify emoji floats and logs appear.
- **Editor Test**: Press 'E' to verify logic flow.
- **Spam Test**: Verify block logic works in both local and network modes.
