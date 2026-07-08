# Project Overview
- Game Title: Chkobba Multiplayer
- High-Level Concept: A card game with super power mechanics where players can view and activate powers from an InfoBox menu.
- Players: Multiplayer (Networked)
- Target Platform: Android
- Render Pipeline: URP (Medium_PipelineAsset)

# Game Mechanics
## Core Gameplay Loop
Players interact with cards and super powers. A central InfoBox (InfoBoxBackGroundPanel) displays details about a selected power or a menu of all available powers.
## Controls and Input Methods
- Touch/Mouse interactions for UI.
- Scrollable list for super powers.
- New filter buttons to categorize powers by cost tier.

# UI
## InfoBox Filtering
- 3 new buttons (Tier 1, Tier 2, Tier 3) located on the right side of the InfoBox, stacked vertically.
- Same horizontal position as the existing `ActivateButton`.
- Vertical distribution: The height of the InfoBox will be divided into 3 equal parts for these buttons.
- Visual Feedback: Selected button will have a darkened color tint to indicate it is the active tab.

# Key Asset & Context
- **Assets/Prefabs/UI/InfoBoxBackGroundPanel.prefab**: The main prefab to modify.
- **Assets/Scripts/MenuController.cs**: Manages the token menu and filtering logic.
- **Assets/Scripts/SuperPowerSpawner.cs**: Manages the InfoBox state and animations.
- **Assets/Scripts/UIFrameAnimator.cs**: Handles the sprite-based background animation.

# Implementation Steps

## Step 1: UI Hierarchy Setup
- Open `InfoBoxBackGroundPanel.prefab`.
- Create a new container `FilterButtonContainer` under `InfoBoxCanvas`.
- Match the `RectTransform` of `FilterButtonContainer` to the vertical area of the InfoBox, positioned at the same X as `ActivateButton`.
- Add a `VerticalLayoutGroup` to `FilterButtonContainer`.
    - Control Child Size: Width and Height.
    - Child Force Expand: Width and Height.
- Create 3 buttons (`Tier1Button`, `Tier2Button`, `Tier3Button`) as children of the container.
- Set their labels to "Tier 1", "Tier 2", "Tier 3" (or icons if preferred).
- **Assigned role**: developer
- **Dependencies**: None

## Step 2: Extend MenuController for Filtering
- Add `[SerializeField]` references for the 3 filter buttons.
- Add `Color` variables for `normalColor` and `selectedColor`.
- Add `int currentFilterTier = -1;` (-1 representing All).
- Implement `FilterMenuByTier(int tier)`:
    - Update `currentFilterTier`.
    - Re-run the token creation logic, but filter `tokenDataList` by `rarity == tier`.
    - If `tier == -1`, show all.
- Implement `UpdateFilterButtonVisuals()`:
    - Apply `selectedColor` to the image of the button matching `currentFilterTier`.
- **Assigned role**: developer
- **Dependencies**: Step 1

## Step 3: Integrate Animation and Transition
- Update `MenuController.OnTierButtonClicked(int tier)`:
    - Call `SuperPowerSpawner.LocalInstance.TriggerPageChange()`.
    - Use a Coroutine to:
        1. Fade out current token content (using `CanvasGroup` if available or iterating through tokens).
        2. Wait for fade/animation mid-point.
        3. Clear and Re-create tokens with the new filter.
        4. Fade in the new content.
- Ensure the filter resets to `-1` (Show All) inside `OpenMenuInInfoBox()` so the full list appears every time the menu is freshly opened.
- **Assigned role**: developer
- **Dependencies**: Step 2

## Step 4: Logic Wiring in Prefab
- In the `InfoBoxBackGroundPanel` prefab, assign the new buttons to the `MenuController` inspector fields.
- Set the `OnClick` events for the buttons to call the new filtering methods in `MenuController`.
- **Assigned role**: developer
- **Dependencies**: Step 3

# Verification & Testing
- **Manual Check**: Open the InfoBox Menu. Verify all powers (Tier 1-4) are shown.
- **Filtering Check**: Click Tier 1. Verify background animation plays and only Tier 1 powers are listed.
- **Visual Check**: Verify the Tier 1 button changes color to indicate selection.
- **Persistence Check**: Close InfoBox, open again. Verify it shows the full list (Tier 1-4) and no buttons are "selected".
- **Transition Check**: Verify the fade in/out happens smoothly alongside the background sprite animation.
