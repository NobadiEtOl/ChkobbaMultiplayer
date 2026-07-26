# GameManager.cs Power System - Complete Mapping

## ✅ ALL BOOLEAN STATE FLAGS (12 Total)

| Flag Name | Line | Type | Purpose | Reset In |
|-----------|------|------|---------|----------|
| `isKapkacPending` | 379 | bool | Single-card selection mode for Kapkaç | ResetForNewRound() |
| `isYandimAnamPending` | 381 | bool | Single-card selection mode for Yandım Anam | ResetForNewRound() |
| `isBuDahaIyiPending` | 383 | bool | Single-card selection mode for Bu Daha İyi | ResetForNewRound() |
| `isKopyalaSelectingSource` | 2979 | bool | Phase 1: waiting for source card in Kopyala | ResetForNewRound() |
| `isKopyalaActive` | 2978 | bool | Phase 2: waiting for target card in Kopyala | ResetForNewRound() |
| `isSunuDegisTokusActive` | 3999 | bool | Phase 1: hand card selected, waiting for opponent card | ResetForNewRound() |
| `isSunuDegisTokusSelectingOpponent` | 4003 | bool | Phase 2: currently choosing opponent hand card | ResetForNewRound() |
| `isSunuDegisBunuTokusActive` | 4300 | bool | Multi-swap selection mode active | ResetForNewRound() |
| `valeArarActive` | 3689 | bool | Jack visibility indicators ON (entire round) | OnRoundOver() |
| `verZehriActive` | 3685 | bool | Ver Zehri fog effect active | ResetForNewGame() |
| `kutsalDesteActive` | 3687 | bool | Kutsal Deste fog effect active | ResetForNewGame() |
| `oynayamazsinActive` | 3597 | bool | Play block prefab visible above center | ResetForNewGame() |

---

## 📋 ALL POWER METHODS ORGANIZED BY CATEGORY

### Category 1: SINGLE-CARD SELECTION POWERS
These powers require selecting ONE card from opponent(s). Mutually exclusive flags.

#### Kapkaç Power
```csharp
public void StartKapkacSelectionPower()
  Returns: void
  Purpose: Activate single-card selection mode for Kapkaç
  Sets: isKapkacPending = true
  Clears: isYandimAnamPending, isBuDahaIyiPending, currentlySelectedCard, currentSelectedHandCard
  RPC: PauseTurnTimerForPowerServerRPC(), StartPowerDurationTimerServerRPC()
  UI: ShowcaseAllOtherHands(false), ShowDualSelectionStepText("Değiştirmek için bir kart seç")

public void OnKapkacCardChanged(string cardUniqueID, bool instant = false)
  Parameters: 
    - cardUniqueID: string (unique card identifier)
    - instant: bool (true = no animation, false = animated via coroutine)
  Effect: cardID[1] = 11 (changes card to Jack)
  Tracking: MoveChainIntegrator.TrackSuperpowerEffect(), cardPowerEffects[id]="Kapkaç"
  Visual: KapkacCourotine() coroutine OR instant kapkacEffectPrefab
  Saves: kapkacCardsToBeReset list for cleanup

private IEnumerator KapkacCourotine(string cardUniqueID)
  - Animates the Kapkaç effect over time
```

#### Yandım Anam Power
```csharp
public void ActivateYandimAnamPower()
  Returns: void
  Purpose: Wrapper for StartYandimAnamSelectionPower()

public void StartYandimAnamSelectionPower()
  Returns: void
  Purpose: Activate single-card selection mode for Yandım Anam
  Sets: isYandimAnamPending = true
  Clears: isKapkacPending, isBuDahaIyiPending
  RPC: powerProcessor?.StartYandimAnamSelection()
  UI: ShowcaseAllOtherHands(false), ShowDualSelectionStepText("Değiştirmek için bir kart seç")

public void OnYandimAnamCardChanged(string cardUniqueID, bool instant = false)
  Parameters:
    - cardUniqueID: string
    - instant: bool
  Effect: Sets card value to 0 (removes card)
  Tracking: MoveChainIntegrator.TrackSuperpowerEffect(), cardPowerEffects[id]="YandımAnam"
  Visual: YandimAnamCoroutine() OR instant effect with yandimAnamSpritePrefab + yandimAnamEffectPrefab
  Stores: cardInteraction.yandimAnamEffectInstance, yandimAnamSpriteInstance

private IEnumerator YandimAnamCoroutine(string cardUniqueID)
  - Resets card visuals
  - Instantiates animation prefab (yandimAnamEffectPrefab) above card
  - Instantiates sprite overlay (yandimAnamSpritePrefab) with fade-in
  - Sets card value to 0
  - Sets cardScript.activePowerEffect = "YandımAnam"
```

#### Bu Daha İyi Power
```csharp
public void StartBuDahaIyiSelectionPower()
  Returns: void
  Purpose: Activate single-card selection mode for Bu Daha İyi
  Sets: isBuDahaIyiPending = true
  Clears: isKapkacPending, isYandimAnamPending
  RPC: PauseTurnTimerForPowerServerRPC(), StartPowerDurationTimerServerRPC()
  UI: ShowcaseAllOtherHands(false), ShowDualSelectionStepText("Orta ile değiş tokuş için bir el kartı seç")

public void UseBuDahaIyiPower()
  Returns: void
  Preconditions: currentSelectedHandCard != null, centerCards.Count > 0
  Effect: Gets top center card (last added), calls networkRelay.UseBuDahaIyiServerRPC()

public void OnBuDahaIyiSynced(int activatingPlayerNo, int handCardOwnerPlayerNo, 
                               string handCardID, string centerCardID)
  Parameters:
    - activatingPlayerNo: int (player executing power)
    - handCardOwnerPlayerNo: int (player owning hand card)
    - handCardID: string
    - centerCardID: string
  Effect: 
    - Swaps cards in myCards list (if relevant)
    - Swaps in centerCards dictionary
  Tracking: MoveChainIntegrator.ReportPowerCompletion(), powerProcessor?.OnGameStateMutated()
  Visual: AnimateBuDahaIyiSwap(handCardID, centerCardID, handCardOwnerPlayerNo)

private IEnumerator AnimateBuDahaIyiSwap(string handCardID, string centerCardID, int playerNo)
  - Calls deckController.SwapHandCardWithCenterCard()
  - Updates showcase originals for both cards
  - Exits showcase and returns to normal play
```

---

### Category 2: DUAL-SELECTION POWERS (Two-Step)
Two separate selections required in sequence.

#### Şunu Değiş Tokuş / Değiş Tokuş (Swap Powers)
```csharp
public void StartSunuDegisTokusDualSelection(string myHandCard, 
                                             string powerName = "Şunu Değiş Tokuş")
  Parameters:
    - myHandCard: string (optional, not used in current implementation)
    - powerName: string (default="Şunu Değiş Tokuş", also supports "Değiş Tokuş")
  Sets: 
    - isSunuDegisTokusActive = true
    - sunuDegisTokusFirstCard = null (will be set when opponent card selected)
    - isSunuDegisTokusSelectingOpponent = false (becomes true when hand card selected)
    - activeSunuDegisTokusPowerName = powerName
  RPC: PauseTurnTimerForPowerServerRPC(), StartPowerDurationTimerServerRPC()
  UI: ShowcaseAllOtherHands(), ShowDualSelectionStepText("Adım 1/2: Değiş tokuş için ilk el kartını seç")
  Process: Player selects hand card → state shifts → player selects opponent hand card → ServerRPC → OnSunuDegisTokusSynced()

public bool IsSunuDegisTokusSelectingOpponent()
  Returns: bool (isSunuDegisTokusSelectingOpponent)
  Purpose: Check if in phase 2 (waiting for opponent card)

public IEnumerator OnSunuDegisTokusSynced(int myPlayerNo, int otherPlayerNo, 
                                          string myHandCardID, string otherHandCardID)
  Parameters:
    - myPlayerNo: int
    - otherPlayerNo: int
    - myHandCardID: string
    - otherHandCardID: string
  Effect:
    - Updates myCards list if local player is either player in swap
    - Swaps card IDs in visual representation
  Tracking: MoveChainIntegrator.TrackCardSwap(), ReportPowerCompletion()
  Visual: SwapCardsBetweenPlayersByID(myPlayerNo, myHandCardID, otherPlayerNo, otherHandCardID, true)
  Cleanup: ExitShowcaseAllOtherHands(), TryStopShowcaseCenterCards()
  Returns: IEnumerator (yields animation)

private void TriggerActiveSunuDegisTokusPowerActivated()
  - Creates temporary power instance based on activeSunuDegisTokusPowerName
  - Calls PowerActivated() on it
  - Used for local tracking
```

#### Kopyala Yapıştır (Two-Phase Copy)
```csharp
public void StartKopyalaYapistirDualSelection(CardInteraction sourceCard)
  Parameters:
    - sourceCard: CardInteraction or null
  If sourceCard == null (Phase 1 - Source Selection):
    Sets: isKopyalaSelectingSource = true, isKopyalaActive = false, kopyalaSourceCard = null
    RPC: PauseTurnTimerForPowerServerRPC(), StartPowerDurationTimerServerRPC()
    UI: ShowcaseAllOtherHands(false)
    Process: Waiting for player to select source card
  If sourceCard != null (Phase 2 - Target Selection):
    Sets: isKopyalaSelectingSource = false, isKopyalaActive = true, kopyalaSourceCard = sourceCard
    UI: ShowcaseAllOtherHands(false)
    Process: Waiting for player to select target card

public void SelectKopyalaSource(CardInteraction sourceCard)
  Parameters: sourceCard - CardInteraction (called from CardInteraction when card tapped in Phase 1)
  Effect: Transitions from Phase 1 to Phase 2
  Sets: isKopyalaSelectingSource = false, isKopyalaActive = true, kopyalaSourceCard = sourceCard
  UI: ShowDualSelectionStepText("Yapıştırmak için bir kart seç")

public void TryKopyalaYapistir(CardInteraction targetCard)
  Parameters: targetCard - CardInteraction (called from CardInteraction when card tapped in Phase 2)
  Validation: 
    - isKopyalaActive == true
    - kopyalaSourceCard != null
    - targetCard != null
    - targetCard != kopyalaSourceCard
  If valid:
    - Calls PowerActivated() on temporary KopyalaYapistir instance
    - RPC: KopyalaYapistirServerRPC(targetCard.uniqueCardInstanceID, kopyalaSourceCard.uniqueCardInstanceID)
    - Stops showcase and closes InfoBox
    - Resets: isKopyalaActive=false, isKopyalaSelectingSource=false, kopyalaSourceCard=null

public void OnKopyalaYapistir(string targetUniqueID, string sourceUniqueID, bool instant = false)
  Parameters:
    - targetUniqueID: string (card being copied TO)
    - sourceUniqueID: string (card being copied FROM)
    - instant: bool
  Effect:
    - newCardID = sourceCard.GetCardID() (copies both kind AND value)
    - newSprite = sourceCard.GetComponent<SpriteRenderer>().sprite
    - targetCard.activePowerEffect = sourceCard.activePowerEffect
    - copiedCardMap[targetUniqueID] = sourceUniqueID (for persistence)
  Tracking: MoveChainIntegrator.TrackSuperpowerEffect(), ReportPowerCompletion()
  Save: powerProcessor?.OnGameStateMutated()
  Visual: Fade sprite change (instant or coroutine with fade duration)
```

---

### Category 3: MULTI-SWAP POWER
Multiple sequential swaps (one power, many selections).

#### Şunu Değiş Bunu Tokuş (Multi-Swap)
```csharp
public void ActivateSunuDegisBunuTokusPower()
  Returns: void
  Process:
    1. Gets local hand snapshot: GetCurrentLocalHandCardIdsForSwap() → List<string>
    2. Counts opponent cards: GetAvailableOpponentCardCount() → int
    3. Caps totalSwaps = min(myHandSnapshot.Count, opponentCardCount)
    4. Initializes state variables:
       - isSunuDegisBunuTokusActive = true
       - sunuDegisBunuTokusSwapIndex = 0
       - sunuDegisBunuTokusMyHandSnapshot = localHandSnapshot
       - sunuDegisBunuTokusTotalSwaps = totalSwaps
       - sunuDegisBunuTokusSelectedOpponentCards = []
  RPC: PauseTurnTimerForPowerServerRPC(), StartPowerDurationTimerServerRPC()
  UI: ShowcaseAllOtherHands(false), TemporarilySetOwnHandToNormalScale()
       ShowDualSelectionStepText(GetSunuDegisBunuTokusSelectionText(0, totalSwaps))
       StartHandShowcaseForDualSelection("Şunu Değiş Bunu Tokuş")
  Process: Players select opponent cards sequentially; each selection added to list

public void EnqueueSunuDegisBunuTokusSwap(int myPlayerNo, int otherPlayerNo, 
                                          string myHandCardID, string otherHandCardID, 
                                          int myHandIndex, bool readyToExit)
  Parameters:
    - myPlayerNo: int
    - otherPlayerNo: int
    - myHandCardID: string
    - otherHandCardID: string
    - myHandIndex: int (position in myHand)
    - readyToExit: bool (true only on last swap)
  Effect: Enqueues IEnumerator into sunuDegisBunuTokusSwapQueue
  Starts: ProcessSunuDegisBunuTokusSwapQueue() if not running

private IEnumerator ProcessSunuDegisBunuTokusSwapQueue()
  - Sets: sunuDegisBunuTokusSwapProcessing = true
  - While queue not empty:
    - Dequeues and executes OnSunuDegisBunuTokusSynced() coroutine
  - Sets: sunuDegisBunuTokusSwapProcessing = false
  - Prevents concurrent showcase restoration during animations

private void CompleteSunuDegisBunuTokusActivation()
  - Called when player finishes selecting all opponent cards
  - Validates: pairCount > 0
  - Calls: PowerActivated() on temporary SunuDegisBunuTokus instance
  - Sets: sunuDegisBunuTokusActivationPending = true, sunuDegisBunuTokusExpectedSyncs = pairCount
  - Loops: Sends UseSunuDegisBunuTokusServerRPC() for each swap pair (last marked with readyToExit=true)

public IEnumerator OnSunuDegisBunuTokusSynced(int myPlayerNo, int otherPlayerNo, 
                                              string myHandCardID, string otherHandCardID, 
                                              int myHandIndex, bool readyToExit = false)
  Parameters:
    - myPlayerNo: int
    - otherPlayerNo: int
    - myHandCardID: string
    - otherHandCardID: string
    - myHandIndex: int
    - readyToExit: bool (true only on last swap in sequence)
  Effect:
    - Updates myCards if relevant to local player
    - Swaps card visuals: SwapCardsBetweenPlayersByID()
    - Increments sunuDegisBunuTokusCompletedSyncs
    - Updates UI: ShowDualSelectionStepText(progress)
  If readyToExit && completedSyncs >= expectedSyncs:
    - Calls: FinalizeSunuDegisBunuTokusActivation()

private void FinalizeSunuDegisBunuTokusActivation()
  - ExitShowcaseAllOtherHands(), TryStopShowcaseCenterCards()
  - StopHandShowcase(), CloseInfoBox()
  - ResetSunuDegisBunuTokusState(false)
  - powerProcessor?.OnGameStateMutated()

private void ResetSunuDegisBunuTokusState(bool clearQueue)
  - Clears all ŞDBT state variables
  - If clearQueue: clears queue and resets sunuDegisBunuTokusSwapProcessing

private List<string> GetCurrentLocalHandCardIdsForSwap()
  Returns: List<string> of card IDs from local hand (indices > 1 in hand transform children)

private int GetAvailableOpponentCardCount()
  Returns: int (total cards in all opponent hands, hand transforms indices 1+)

private string GetSunuDegisBunuTokusSelectionText(int index, int totalSwaps)
  Parameters: index (int), totalSwaps (int)
  Returns: string (ordinal + fraction, e.g., "İlk kartın ile değiştirmek için bir kart seç (1/3)")
```

---

### Category 4: CLIENT-LOCAL POWERS (No State Mutation)
These powers only affect UI/visualization for the activating player.

#### Vale Arar (Jack Visibility)
```csharp
public void ActivateValeArarPower()
  Returns: void
  Sets: valeArarActive = true
  Effect: 
    - Loops through cardInteractionsScripts
    - Finds all Jacks (cardID[1] == 11)
    - Activates SelectedCardIndicator on each Jack (red, 50% alpha)
  Scope: CLIENT-LOCAL ONLY - no server sync, no state mutation
  Duration: ENTIRE ROUND (until DeactivateValeArarPower or end of round)

public void DeactivateValeArarPower()
  Returns: void
  Sets: valeArarActive = false
  Effect: Hides SelectedCardIndicator on all Jacks

public void RefreshValeArarIndicators()
  Returns: void
  Condition: Only runs if valeArarActive == true
  Purpose: Re-shows indicators after card pool rebuild (e.g., new round cards created)
```

---

### Category 5: BLOCKING/EFFECT POWERS
Powers that apply state/visual effects.

#### Oynayamazsin (Play Block)
```csharp
public void SetOynayamazsinActive(bool isActive)
  Parameters: isActive - bool
  If isActive == true:
    - Instantiates oynayamazsinBlockPrefab as child of center
    - Stores in oynayamazsinBlockInstance
    - Fades in sprite via FadeInSprite()
  If isActive == false:
    - Destroys oynayamazsinBlockInstance
    - Sets to null
  Purpose: Visual block above center to indicate "play blocked"
```

#### Ver Zehri (Fog Effect)
```csharp
public void SetVerZehriActive(bool isActive)
  Parameters: isActive - bool
  Sets: verZehriActive = isActive
  If true: StartVerZehriEffect()
  If false: StopVerZehriEffect()

private void StartVerZehriEffect()
  Effect: Loops through verZehriObject children, calls child.GetComponent<FogController>().StartFog()

private void StopVerZehriEffect()
  Effect: Loops through verZehriObject children, calls child.GetComponent<FogController>().StopFog()

public void ShowVerZehriEffect(int playerNumber, int points)
  - Stub method for showing notification/feedback
```

#### Kutsal Deste (Fog Effect)
```csharp
public void SetKutsalDesteActive(bool isActive)
  Parameters: isActive - bool
  Sets: kutsalDesteActive = isActive
  If true: StartKutsalDesteEffect()
  If false: StopKutsalDesteEffect()

private void StartKutsalDesteEffect()
  Effect: Loops through kutsalDesteObject children, calls child.GetComponent<FogController>().StartFog()

private void StopKutsalDesteEffect()
  Effect: Loops through kutsalDesteObject children, calls child.GetComponent<FogController>().StopFog()

public void ShowKutsalDesteEffect(int playerNumber, int points)
  - Stub method for showing notification/feedback
```

---

### Category 6: POWER VALIDATION & CHECKS
Utility methods for checking power state.

```csharp
public bool IsSpecialPowerAllowingPlay()
  Returns: bool
  Purpose: Check if any active power allows out-of-turn card play
  Checks:
    - isKapkacPending → true (can select opponent cards)
    - isYandimAnamPending → true
    - isBuDahaIyiPending → true
    - isKopyalaActive → true (selecting target)
    - (Add other powers as needed)
  Returns: false if no special powers active

public bool CanPlayerPlay(GameObject cardObj)
  Returns: bool
  Purpose: Determine if card can be played by current player
  Checks:
    1. Is it player's turn? (currentPlayerNo == deckController.thisPlayerNumber)
    2. Is card in correct hand location?
    3. If not player's turn, check IsSpecialPowerAllowingPlay()

public bool IsAnySwapPowerActive()
  Returns: bool
  Purpose: Check if ANY swap power is currently active
  Returns: isSunuDegisTokusActive || isSunuDegisBunuTokusActive
```

---

### Category 7: UNIVERSAL TIMEOUT & CANCELLATION

```csharp
public void CancelDualSelectionPower()
  Returns: void
  Called: When server power duration timer expires (client receives RPC)
  Checks each power type in order:
    1. isBuDahaIyiPending → Resets to normal play
    2. isKapkacPending/isYandimAnamPending → Resets to normal play
    3. isKopyalaActive/isKopyalaSelectingSource → Resets to normal play
    4. isSunuDegisBunuTokusActive → Resets to normal play
    5. isSunuDegisTokusActive → Resets to normal play
  Common actions for all:
    - Clears: CardInteraction.currentlySelectedCard, currentSelectedHandCard
    - UI: ExitShowcaseAllOtherHands(), CloseInfoBox()
    - Logging: Logs which power was cancelled
    - Returns to normal turn play with same player still active
```

---

## 📊 POWER CALL FLOW PATTERNS

### Pattern 1: Single-Selection (Kapkaç, Yandım Anam, Bu Daha İyi)
```
SuperPowerController (Button Press)
  ↓
StartKapkacSelectionPower() / StartYandimAnamSelectionPower() / StartBuDahaIyiSelectionPower()
  ├─ Sets pending flag (isKapkacPending/isYandimAnamPending/isBuDahaIyiPending)
  ├─ RPC: PauseTurnTimerForPowerServerRPC(), StartPowerDurationTimerServerRPC()
  └─ UI: ShowcaseAllOtherHands(false)
     ↓
     CardInteraction (Opponent's card tapped)
     ├─ RPC to Server: "I selected this opponent card for Kapkaç"
     ├─ Server validates and executes power
     └─ Server broadcasts: OnKapkacCardChanged(cardID)
        ↓
        OnKapkacCardChanged(cardID, instant=false)
        ├─ Updates: cardID[1] = 11, cardPowerEffects[id] = "Kapkaç"
        ├─ Animation: KapkacCourotine() or instant effect
        └─ Save: powerProcessor?.OnGameStateMutated()
```

### Pattern 2: Dual-Selection Swap (Şunu Değiş Tokuş)
```
SuperPowerController (Button Press)
  ↓
StartSunuDegisTokusDualSelection(myHandCard, "Şunu Değiş Tokuş")
  ├─ Sets: isSunuDegisTokusActive = true, isSunuDegisTokusSelectingOpponent = false
  ├─ RPC: PauseTurnTimerForPowerServerRPC(), StartPowerDurationTimerServerRPC()
  └─ UI: ShowcaseAllOtherHands()
     ↓ (Phase 1 complete - hand card already selected from CardInteraction)
     ↓
     CardInteraction (Opponent hand card tapped)
     ├─ Validates: isSunuDegisTokusActive == true
     ├─ Sets: isSunuDegisTokusSelectingOpponent = true (phase 2)
     ├─ RPC to Server: UseSunuDegisTokusServerRPC(myHandCard, oppHandCard)
     └─ Server validates and executes power
        ↓
        Server broadcasts: OnSunuDegisTokusSynced(myPlayerNo, oppPlayerNo, myCardID, oppCardID)
        ↓
        OnSunuDegisTokusSynced()
        ├─ Updates: myCards if relevant, swaps card references
        ├─ Tracking: MoveChainIntegrator.TrackCardSwap(), ReportPowerCompletion()
        ├─ Visual: SwapCardsBetweenPlayersByID()
        └─ Cleanup: ExitShowcaseAllOtherHands(), TryStopShowcaseCenterCards()
```

### Pattern 3: Two-Phase Copy (Kopyala Yapıştır)
```
SuperPowerController (Button Press)
  ↓
StartKopyalaYapistirDualSelection(null)  [Phase 1]
  ├─ Sets: isKopyalaSelectingSource = true, isKopyalaActive = false
  ├─ RPC: PauseTurnTimerForPowerServerRPC(), StartPowerDurationTimerServerRPC()
  └─ UI: ShowcaseAllOtherHands(false)
     ↓
     CardInteraction (Any card tapped in Phase 1)
     ├─ RPC not sent (Phase 1 is local only)
     ├─ SelectKopyalaSource(sourceCard)
     ├─ Sets: isKopyalaSelectingSource = false, isKopyalaActive = true, kopyalaSourceCard = sourceCard
     └─ UI: ShowDualSelectionStepText("Yapıştırmak için bir kart seç")
        ↓ (Phase 2 active - waiting for target)
        ↓
        CardInteraction (Target card tapped in Phase 2)
        ├─ Validates: isKopyalaActive && kopyalaSourceCard != null && targetCard != sourceCard
        ├─ TryKopyalaYapistir(targetCard)
        ├─ Calls: PowerActivated() on temporary KopyalaYapistir
        ├─ RPC: KopyalaYapistirServerRPC(targetCardID, sourceCardID)
        └─ Server validates and executes power
           ↓
           Server broadcasts: OnKopyalaYapistir(targetCardID, sourceCardID, instant=false)
           ↓
           OnKopyalaYapistir()
           ├─ Updates: targetCard.cardID = sourceCard.cardID
           ├─ Copies: sprite, activePowerEffect
           ├─ Tracking: copiedCardMap[targetID] = sourceID
           ├─ Save: powerProcessor?.OnGameStateMutated()
           └─ Visual: Fade sprite change
```

### Pattern 4: Multi-Swap (Şunu Değiş Bunu Tokuş)
```
SuperPowerController (Button Press)
  ↓
ActivateSunuDegisBunuTokusPower()
  ├─ Gets: localHandSnapshot, availableOpponentCards
  ├─ Calcs: totalSwaps = min(myHand.Count, oppCards.Count)
  ├─ Sets: isSunuDegisBunuTokusActive = true, sunuDegisBunuTokusTotalSwaps = totalSwaps
  ├─ RPC: PauseTurnTimerForPowerServerRPC(), StartPowerDurationTimerServerRPC()
  └─ UI: ShowcaseAllOtherHands(false), StartHandShowcaseForDualSelection()
     ↓
     [Player selects opponent cards sequentially]
     CardInteraction 1 (1st opponent card tapped) → Added to sunuDegisBunuTokusSelectedOpponentCards
     CardInteraction 2 (2nd opponent card tapped) → Added to sunuDegisBunuTokusSelectedOpponentCards
     CardInteraction N (Nth opponent card tapped) → Added to sunuDegisBunuTokusSelectedOpponentCards
     ↓
     CompleteSunuDegisBunuTokusActivation()
     ├─ Calls: PowerActivated() on temporary SunuDegisBunuTokus
     ├─ Sets: sunuDegisBunuTokusActivationPending = true, expectedSyncs = pairCount
     └─ Sends: Multiple UseSunuDegisBunuTokusServerRPC() calls (one per swap pair)
        - For swap 1-N: readyToExit = false
        - For swap N: readyToExit = true
        ↓
        Server executes swaps and broadcasts synced events
        ↓
        EnqueueSunuDegisBunuTokusSwap() × N  [Each enqueued as IEnumerator]
        ↓
        ProcessSunuDegisBunuTokusSwapQueue()
        ├─ Dequeues and executes OnSunuDegisBunuTokusSynced() for each swap
        ├─ Updates: myCards, visual swaps
        ├─ Increments: sunuDegisBunuTokusCompletedSyncs
        └─ On last swap (readyToExit=true AND completedSyncs >= expectedSyncs):
           FinalizeSunuDegisBunuTokusActivation()
           ├─ ExitShowcaseAllOtherHands(), StopHandShowcase()
           ├─ CloseInfoBox()
           └─ ResetSunuDegisBunuTokusState(false)
```

---

## 🔄 DATA PERSISTENCE

### Tracked In Dictionaries (Survive Save/Load):
```csharp
cardPowerEffects: Dictionary<string, string>
  Maps: cardID → PowerName ("Kapkaç", "YandımAnam", etc.)
  Cleared: ResetForNewGame()
  Used: Reconstructing power visuals on reconnect

copiedCardMap: Dictionary<string, string>
  Maps: copiedCardID → sourceCardID
  Cleared: ResetForNewRound(), ResetForNewGame()
  Used: Tracking which cards were copied from which source

sunuDegisBunuTokusSelectedOpponentCards: List<string>
  Stores: Card IDs selected during ŞDBT activation
  Cleared: ResetSunuDegisBunuTokusState()
  Scope: Active only during ŞDBT selection phase
```

### State Snapshots (For Reconnection):
```csharp
SerializableGameState.isKapkacPending: bool
SerializableGameState.isYandimAnamPending: bool
SerializableGameState.isSunuDegisTokusActive: bool
SerializableGameState.isSunuDegisBunuTokusActive: bool
SerializableGameState.isKopyalaActive: bool
```

---

## 🎯 VALIDATION GATES

### Before Single Selection:
- Check: Power flag not already set
- Check: No other power active (mutually exclusive)
- Check: currentPlayerNo is valid

### Before Dual Selection Phase 1:
- Check: Power flag not already set
- Check: targetCard != null
- Check: Card is selectable (not own hand for swap, etc.)

### Before Dual Selection Phase 2:
- Check: Phase 1 flag set (e.g., isSunuDegisTokusActive)
- Check: Phase 2 flag clear (e.g., !isSunuDegisTokusSelectingOpponent)
- Check: targetCard is valid and different from source

### Before Kopyala Phase 2:
- Check: isKopyalaActive == true
- Check: kopyalaSourceCard != null
- Check: targetCard != kopyalaSourceCard

### Before Multi-Swap Selection:
- Check: isSunuDegisBunuTokusActive == true
- Check: selectedCount < totalSwaps
- Check: Already-selected cards not selected again

---

## ⚠️ CRITICAL NOTES

1. **Mutual Exclusivity**: Only ONE pending flag active at a time
   - isKapkacPending, isYandimAnamPending, isBuDahaIyiPending are mutually exclusive

2. **Phase Transitions**: Dual/two-phase powers transition flags:
   - Kopyala: isKopyalaSelectingSource (Phase 1) → isKopyalaActive (Phase 2)
   - SunuDegisTokuş: isSunuDegisTokusActive (Phase 1) → isSunuDegisTokusSelectingOpponent (Phase 2)

3. **Server-Authoritative**: All card effects applied by server RPC responses:
   - Client initiates selection
   - Server validates and executes
   - Server broadcasts OnXxxXxxSynced() to all clients
   - All clients apply effect from sync RPC

4. **Queue Processing**: ŞDBT uses queue to ensure sequential animation:
   - Multiple swaps queued but executed one-by-one
   - Prevents showcase restoration mid-animation

5. **Save/Load**: Power effects and state persisted:
   - cardPowerEffects saved in snapshot
   - copiedCardMap saved in snapshot
   - Used in ReconstructPowerVisuals() on reconnection

6. **Timeout Mechanism**: Universal timeout via CancelDualSelectionPower()
   - Called by server when power duration timer expires
   - Works for ALL power types
   - Returns player to normal turn play
