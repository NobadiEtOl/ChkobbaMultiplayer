# Plan - Fix 2v2 Host Migration Perspective Corruption

This plan fixes the perspective and hand-rebuild issues when host migration occurs in 2v2 (4-player) mode.

## Steps

1. **Restore Player Identity Before Snapshot Application**
   In `Assets/Scripts/GameManager.cs`, call `RestorePlayerNumberFromPrefs()` at the very beginning of the `ApplyCoreGameState` method. This ensures that when the snapshot is applied and the scene is rebuilt, `thisPlayerNumber` is already restored.

2. **Wait for Local Seat Assignment in `RebuildCardContainers`**
   In `Assets/Scripts/GameManager.cs` inside `RebuildCardContainers(SerializableGameState snapshot)`, if `myNo` is not yet assigned (i.e. `-1`), add a short wait loop to let it assign instead of immediately aborting. This prevents "Invisible Hands" when the seat assignment hasn't fully propagated in NGO on first frames.

3. **Verify/Audit Seat-to-Transform mapping**
   Ensure `GetTransformIndexForSeat` correctly maps seats for 4-player games, and that no modulo loop collisions happen during hand reconstructions or visual snapshots.
