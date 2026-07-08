# Project Overview
- Game Title: Chkobba Multiplayer
- High-Level Concept: A multiplayer card game based on the traditional Chkobba/Pişti mechanics, allowing players to match cards, score points, and communicate using expressive emojis.
- Players: Single player & Multiplayer (1v1, 2v2)
- Inspiration / Reference Games: Traditional Mediterranean card games, Clash Royale (emote sharing UX)
- Tone / Art Direction: Friendly, casual, polished UI
- Target Platform: Mobile (Android) & PC (WebGL/Desktop)
- Screen Orientation / Resolution: Landscape (responsive)
- Render Pipeline: Universal Render Pipeline (URP)

# Game Mechanics
## Core Gameplay Loop
- Players draw cards, play them to match and clear table cards, score points, and use an expressive in-game Emote Bubble system to interact and convey emotions dynamically.

## Controls and Input Methods
- Touch inputs on mobile, click inputs on PC.
- Clicking/tapping the player's own avatar background opens the custom Emoji Keyboard to share an emote.

# UI
## Emoji Keyboard Layout
The custom Emoji Keyboard is an in-game overlay UI that occupies a bottom-centered position or standard popup position.
- **Overlay Background:** Fully covers the viewport with a transparent dark panel (with Raycast Blocker and a script callback to close when clicking outside).
- **Keyboard Panel (Window):** Beautifully styled box with rounded corners.
  - **Vertical Layout Group** structure:
    - **Header:** Title Text ("İfadeler" / "Select Emote") + Close Button.
    - **Middle Section (ScrollRect Viewport):**
      - **Content Container:** Holds a **Grid Layout Group** with cell dimensions optimized for clean visual spacing of emojis.
      - **Content Size Fitter:** Controls vertical size to automatically wrap and scroll depending on the active category list.
    - **Bottom Section (Category Tabs Panel):**
      - **Horizontal Layout Group:** Contains category selection buttons:
        - 🕒 Recent (dynamically loaded from local history)
        - 😊 Smileys
        - 👍 Gestures
        - 🌿 Nature
        - ⚽ Activities
        - 💡 Objects

# Key Asset & Context
### Existing Assets to Modify:
- **`Assets/Scripts/EmoteManager.cs`**:
  - Replace native `TouchScreenKeyboard` workflow with our custom `EmojiKeyboardUI` script callbacks.
  - Expose serialized reference `[SerializeField] private EmojiKeyboardUI emojiKeyboard;` to assign the prefab instance in the scene.
  - Clean up obsolete `TouchScreenKeyboard` checking code in `Update()` to prevent conflicts.

### New Assets to Create:
1. **`Assets/Scripts/EmojiHistoryManager.cs`**:
   - A utility helper class that handles saving and loading the player's recent emojis list using `PlayerPrefs` (maximum 24, saved as a pipe-separated string e.g. "😊|👍|😂").
2. **`Assets/Scripts/EmojiKeyboardUI.cs`**:
   - The runtime controller script for the emoji keyboard.
   - Populates grid buttons dynamically for the selected category.
   - Saves selected emojis to the recent history upon selection.
   - Manages tab state, scroll-to-top on tab switch, and opening/closing state with smooth CanvasGroup alpha fading.
3. **`Assets/Scripts/Editor/EmojiKeyboardPrefabCreator.cs`**:
   - An Editor utility script providing a menu item `Tools / Create Emoji Keyboard Prefab`.
   - Programmatically constructs the entire, flawless uGUI layout hierarchy (ScrollRect, viewport, buttons, layouts) so that layout configurations are pixel-perfect and require no manual designer configuration.
   - Saves it directly as `Assets/Prefabs/UI/EmojiKeyboard.prefab`.

# Implementation Steps

### Step 1: Create the Emoji History Manager
- **Description**: Implement `EmojiHistoryManager.cs` to handle loading/saving emoji lists in `PlayerPrefs` safely and efficiently.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2: Implement the Emoji Keyboard UI Controller
- **Description**: Implement `EmojiKeyboardUI.cs` with categorization logic, grid population, scroll mechanics, and category highlighting.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 3: Implement the Automated Prefab Creator Editor Script
- **Description**: Implement `EmojiKeyboardPrefabCreator.cs` inside an `Editor` folder to programmatically construct, configure, link, and save the `EmojiKeyboard` prefab with exact layout properties.
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

### Step 4: Run Prefab Creator and Modify EmoteManager
- **Description**: Run the menu command to generate the prefab. Modify `EmoteManager.cs` to use our custom `EmojiKeyboardUI` instead of `TouchScreenKeyboard`.
- **Assigned role**: developer
- **Dependencies**: Step 3
- **Parallelizable**: No

### Step 5: Scene Placement and Assignment
- **Description**: Write a short Editor utility or perform a simple, safe scene edit to instantiate the `EmojiKeyboard` prefab under `UICanvas` and link it to the `EmoteManager` component in the `MultiPlayer_Test` scene.
- **Assigned role**: developer
- **Dependencies**: Step 4
- **Parallelizable**: No

# Verification & Testing
1. **Prefab Creation Validation**: Verify `Assets/Prefabs/UI/EmojiKeyboard.prefab` is successfully created and has all layout groups, ScrollRect, and script fields populated.
2. **Spam Protection Verification**: Ensure that emote spam protection (block window / duration) is still fully respected and functions normally.
3. **Recent Tab Dynamic Updates**: Trigger different emojis in succession. Ensure the "Recent" (🕒) tab dynamically displays them in the order of use without duplicates.
4. **Platform & PC Compatibility**: Playtest in the Unity Editor and ensure the UI works flawlessly with mouse-clicks (simulating touch interaction on both PC and mobile devices).
5. **No Native Keyboard Intrusion**: Confirm that native keyboards no longer slide up or block the screen on Android/iOS when the emote system is used.
