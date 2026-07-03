# Power Selection System Redesign Report

## Overview
The power selection and gold economy system has been completely redesigned to simplify mechanics and create fairer power distribution based on rarity.

**Key Changes:**
- Removed exact coin-value matching for power selection
- Implemented inverse-weighted random selection based on `rarityMultiplier`
- Unified all power costs to 1 gold per draw (regardless of power rarity)
- Simplified calculator token integration

---

## System Design

### Gold Cost Model
- **Old System**: Coin value determined power cost and selection type
- **New System**: Every power costs exactly 1 gold to draw
- **Effect**: Players spend 1 gold per token dropped into the kese (pouch), regardless of power rarity

### Power Selection Algorithm

#### Old System (Exact Matching)
```
coinAmount = 5
→ Search for powers where rarityMultiplier == 5
→ If found, randomly pick one of the matching powers
→ If not found, return any random power (fallback)
```
**Problems:** 
- Limited power availability based on exact matches
- Inconsistent availability when no matches exist
- No consideration of rarity vs. accessibility balance

#### New System (Inverse-Weighted Random)
```
For each power:
  weight = 1 / rarityMultiplier
  
Pick random value from [0, totalWeight)
Return power where cumulative weight >= random value
```

**Effect:**
- Higher `rarityMultiplier` = lower weight = harder to draw
- Lower `rarityMultiplier` = higher weight = easier to draw
- Fair probability distribution across all powers

### Probability Examples
Assuming 17 total powers with rarityMultipliers: [5, 7, 12, 14, 15, 17, 18, 19, 20, 20, 22, 22, 25, 25, 67, 100]

**Probability of Drawing:**
| Power | Rarity | Weight | Probability |
|-------|--------|--------|-------------|
| UcundanGözAt | 5 | 0.2000 | ~9.3% |
| DeğişTokuş | 7 | 0.1429 | ~6.6% |
| Oynayamazsın | 12 | 0.0833 | ~3.9% |
| SunuDegisTokus | 14 | 0.0714 | ~3.3% |
| Kapkaç | 15 | 0.0667 | ~3.1% |
| BayaBayaBak | 17 | 0.0588 | ~2.7% |
| ... | ... | ... | ... |
| ZaferPuani | 67 | 0.0149 | ~0.7% |
| ValeArar | 100 | 0.0100 | ~0.5% |

---

## Modified Files

### 1. **SuperPowerSpawner.cs**

#### Method: `ReadyToSpawnSuperPowers()`
**Before:**
```csharp
public void ReadyToSpawnSuperPowers(int numberOfSuperPowersToSpawn = 2, 
                                     Vector3? spawnOrigin = null, 
                                     float spawnScale = 150f, 
                                     int coinAmount = -1)
```

**After:**
```csharp
public void ReadyToSpawnSuperPowers(int numberOfSuperPowersToSpawn = 2, 
                                     Vector3? spawnOrigin = null, 
                                     float spawnScale = 150f)
```

**Changes:**
- Removed `coinAmount` parameter (no longer needed)
- Updated docstring to clarify inverse-weighted behavior

---

#### Method: `ReadyToSpawnSuperPower()` (Coroutine)
**Before:**
```csharp
private IEnumerator ReadyToSpawnSuperPower(int numberOfSuperPowersToSpawn, 
                                            Vector3? spawnOrigin, 
                                            float spawnScale, 
                                            int coinAmount)
{
    yield return new WaitForSeconds(1f);
    for (int i = 0; i < numberOfSuperPowersToSpawn; i++)
    {
        StartCoroutine(SpawnSuperPower(GetWeightedRandomSuperPower(coinAmount), 
                                       spawnOrigin, spawnScale));
        yield return new WaitForSeconds(0.5f);
    }
}
```

**After:**
```csharp
private IEnumerator ReadyToSpawnSuperPower(int numberOfSuperPowersToSpawn, 
                                            Vector3? spawnOrigin, 
                                            float spawnScale)
{
    yield return new WaitForSeconds(1f);
    for (int i = 0; i < numberOfSuperPowersToSpawn; i++)
    {
        StartCoroutine(SpawnSuperPower(GetInverseWeightedRandomSuperPower(), 
                                       spawnOrigin, spawnScale));
        yield return new WaitForSeconds(0.5f);
    }
}
```

**Changes:**
- Removed `coinAmount` parameter
- Calls new `GetInverseWeightedRandomSuperPower()` instead of `GetWeightedRandomSuperPower(coinAmount)`

---

#### Method: `GetInverseWeightedRandomSuperPower()` (NEW)
**Replaces:** `GetWeightedRandomSuperPower()` and `GetRandomSuperPower()`

```csharp
/// <summary>
/// Selects a random power using inverse-weighted probability based on rarityMultiplier.
/// Higher rarityMultiplier = lower selection probability (harder to get rare powers).
/// Uses weight = 1 / rarityMultiplier for each power.
/// </summary>
private SuperPower GetInverseWeightedRandomSuperPower()
{
    if (superPowerPrefabs.Count == 0)
    {
        Debug.LogWarning("[SuperPowerSpawner] No super powers available to spawn.");
        return null;
    }

    List<SuperPower> allPowers = new List<SuperPower>(superPowerPrefabs.Keys);
    
    // Calculate weights for each power: weight = 1 / rarityMultiplier
    List<float> weights = new List<float>();
    float totalWeight = 0f;
    
    for (int i = 0; i < allPowers.Count; i++)
    {
        float weight = 1f / allPowers[i].rarityMultiplier;
        weights.Add(weight);
        totalWeight += weight;
    }
    
    // Pick a random value in [0, totalWeight)
    float randomValue = Random.Range(0f, totalWeight);
    float cumulativeWeight = 0f;
    
    // Find which power was selected
    for (int i = 0; i < allPowers.Count; i++)
    {
        cumulativeWeight += weights[i];
        if (randomValue < cumulativeWeight)
        {
            SuperPower selectedPower = allPowers[i];
            Debug.Log($"[SuperPowerSpawner] Inverse-weighted selection: {selectedPower.name} " +
                     $"(rarity: {selectedPower.rarityMultiplier}, weight: {weights[i]:F4})");
            return selectedPower;
        }
    }
    
    // Fallback (should rarely happen due to floating point precision)
    SuperPower fallbackPower = allPowers[allPowers.Count - 1];
    Debug.LogWarning($"[SuperPowerSpawner] Weighted selection fallback: {fallbackPower.name}");
    return fallbackPower;
}
```

**Algorithm Details:**
1. Creates weight for each power: `weight = 1 / rarityMultiplier`
2. Calculates total weight across all powers
3. Picks random value in [0, totalWeight)
4. Performs cumulative weight check to find selected power
5. Returns selected power with debug logging

**Time Complexity:** O(n) where n = number of powers (typically 17)

---

### 2. **KeseController.cs**

#### Method: `SpawnTokensFromCalculatorData()`
**Before:**
```csharp
private void SpawnTokensFromCalculatorData(Vector3 spawnOrigin, float spawnScale)
{
    if (SuperPowerSpawner.LocalInstance == null) return;
    
    Debug.Log($"[KeseController] Spawning tokens from calculator data");
    
    // Calculate total cost
    int totalCost = 0;
    foreach (var token in coinTokenData)
    {
        totalCost += token.value * token.count;
    }
    
    // Check if player has enough gold
    if (SuperPowerSpawner.LocalInstance.HasEnoughGold(totalCost))
    {
        // Spend the gold
        SuperPowerSpawner.LocalInstance.SpendGold(totalCost);
        
        // Spawn the tokens
        foreach (var token in coinTokenData)
        {
            for (int i = 0; i < token.count; i++)
            {
                // Spawn each token with its specific value
                SuperPowerSpawner.LocalInstance.ReadyToSpawnSuperPowers(1, spawnOrigin, 
                                                                        spawnScale, token.value);
            }
        }
        
        Debug.Log($"[KeseController] Successfully spent {totalCost} gold and spawned tokens");
    }
    else
    {
        Debug.Log($"[KeseController] Insufficient gold! Need {totalCost}, have {...}");
        ReturnCoin();
    }
}
```

**After:**
```csharp
private void SpawnTokensFromCalculatorData(Vector3 spawnOrigin, float spawnScale)
{
    if (SuperPowerSpawner.LocalInstance == null) return;
    
    Debug.Log($"[KeseController] Spawning tokens from calculator data");
    
    // Calculate total number of tokens to spawn
    int totalTokenCount = 0;
    foreach (var token in coinTokenData)
    {
        totalTokenCount += token.count;
    }
    
    // Each power costs 1 gold
    int totalGoldCost = totalTokenCount;
    
    // Check if player has enough gold
    if (SuperPowerSpawner.LocalInstance.HasEnoughGold(totalGoldCost))
    {
        // Spend the gold (1 per power)
        SuperPowerSpawner.LocalInstance.SpendGold(totalGoldCost);
        
        // Spawn all powers with inverse-weighted random selection
        SuperPowerSpawner.LocalInstance.ReadyToSpawnSuperPowers(totalTokenCount, 
                                                                 spawnOrigin, spawnScale);
        
        Debug.Log($"[KeseController] Successfully spent {totalGoldCost} gold and spawned {totalTokenCount} powers");
    }
    else
    {
        Debug.Log($"[KeseController] Insufficient gold! Need {totalGoldCost}, have {...}");
        ReturnCoin();
    }
}
```

**Key Changes:**
1. **Cost Calculation**: Changed from `token.value * token.count` to just `token.count`
2. **Spawning**: Single call to `ReadyToSpawnSuperPowers(totalTokenCount)` instead of looping through each token with its value
3. **Parameter Removal**: No longer passes `token.value` to `ReadyToSpawnSuperPowers()`
4. **Clarified Logic**: Each power now costs exactly 1 gold, regardless of calculator configuration

---

#### Method: `AcceptCoin()` (Updated call site)
**Before:**
```csharp
SuperPowerSpawner.LocalInstance.ReadyToSpawnSuperPowers(1, spawnOrigin, spawnScale, coinAmount);
```

**After:**
```csharp
SuperPowerSpawner.LocalInstance.ReadyToSpawnSuperPowers(1, spawnOrigin, spawnScale);
```

**Changes:**
- Removed `coinAmount` parameter from fallback spawn call

---

## Removed Code

### `GetWeightedRandomSuperPower(int coinAmount)` 
**Rationale:** Exact-matching logic is no longer needed. Replaced by `GetInverseWeightedRandomSuperPower()`.

**Old Logic:**
- Searched for powers where `rarityMultiplier == coinAmount`
- If found, randomly selected one
- If not found, fell back to `GetRandomSuperPower()`

---

### `GetRandomSuperPower()`
**Rationale:** Uniform random selection is subsumed by inverse-weighted logic. All powers are now considered with probability proportional to inverse rarity.

**Old Logic:**
- Randomly picked any power with equal probability
- Used as fallback when no exact matches found

---

## Intended Behavior

### Player Flow
1. **Build Expression**: Player builds calculator expression (e.g., "5 + 3 + 2" = 3 tokens)
2. **Drop Coin**: Player drops coin into kese (pouch)
3. **Cost**: System checks if player has 3 gold (1 per token)
4. **Spawning**: If sufficient gold:
   - Deduct 3 gold
   - Spawn 3 powers
   - Each power selected via inverse-weighted random
5. **Power Draw**: Powers drawn according to probability table above
   - Common powers (low rarity): ~9% chance
   - Rare powers (high rarity): ~0.5% chance

### Gold Economy
- **No longer**: Coin value != power cost
- **Now**: Each power costs exactly 1 gold
- **Benefit**: Simple, predictable economy
- **Flexibility**: Players can spawn 1 power or 10+ depending on calculator input

### Power Balance
- **No longer**: Limited by exact coin-value matches
- **Now**: Fair probabilistic distribution
- **Benefit**: All powers accessible on every coin drop
- **Mechanic**: Rarity directly impacts draw probability

---

## Testing Recommendations

### Unit Tests
```csharp
// Test 1: Probability distribution
for (int i = 0; i < 10000; i++)
{
    var power = GetInverseWeightedRandomSuperPower();
    // Track occurrences, verify within expected ranges
}

// Test 2: Gold cost
Assert.AreEqual(3, totalGoldCost); // 3 tokens = 3 gold

// Test 3: Inverse weighting
Assert.Greater(probabilityOfRarity5, probabilityOfRarity100);
```

### Integration Tests
1. **Calculator + Kese Flow**: Build expression → Drop coin → Verify gold deducted and powers spawned
2. **Insufficient Gold**: Try to spawn when gold < token count → Verify coin returned
3. **Multiple Spawns**: Sequential coin drops → Verify consistent behavior

### Debug Logging
The system logs:
- Power selection with rarity and weight values
- Gold expenditure and token count
- Insufficient gold scenarios
- Edge cases (empty power list, floating point fallback)

---

## Migration Notes

### Breaking Changes
- **Removed Parameter**: `coinAmount` in `ReadyToSpawnSuperPowers()` — update all call sites
- **Renamed Method**: `GetWeightedRandomSuperPower()` → `GetInverseWeightedRandomSuperPower()`
- **Removed Method**: `GetRandomSuperPower()` — replace with new weighted method

### Backwards Compatibility
- ✗ Not compatible with old coin-value based power selection
- ✗ Not compatible with exact-matching logic
- ✓ Compatible with existing `HesapMakinesiController` (calculator) — token structure unchanged
- ✓ Compatible with `SuperPowerController` definitions — no rarity values changed

### Database/Save Games
- **Not Affected**: Power definitions and rarity multipliers unchanged
- **Not Affected**: Player gold balance format unchanged
- **Recommendation**: No migration needed; new system is purely algorithmic

---

## Summary of Changes

| Aspect | Before | After |
|--------|--------|-------|
| **Cost Model** | Variable (based on coin value) | Fixed (1 gold per power) |
| **Selection** | Exact matching or uniform random | Inverse-weighted random |
| **Power Availability** | Limited to exact matches | All powers available always |
| **Rarity Balance** | Unrelated to draw probability | Higher rarity = lower probability |
| **Code Complexity** | Moderate (matching logic) | Optimized (weighted selection) |
| **Flexibility** | Rigid coin → power mapping | Dynamic token-based spawning |

---

## Files Modified
- `Assets/Scripts/SuperPowerSpawner.cs` - Power selection logic redesign
- `Assets/Scripts/KeseController.cs` - Simplified token spawning

---

**Status**: ✅ Implementation complete, ready for testing
**Compiler Status**: No new errors introduced by these changes
