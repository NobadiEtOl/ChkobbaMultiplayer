/// <summary>
/// Provides initialization state visibility for the current game mode.
/// Both Server (multiplayer) and SinglePlayerModeController (singleplayer) implement this.
///
/// Purpose: Shared logic can validate that prerequisites are in place before running,
/// so failures are caught early with a clear diagnostic message instead of a cryptic
/// NullReferenceException several call-frames deep.
/// </summary>
public interface IGameModeInitState
{
    /// <summary>True when the mode has completed core initialization (deck built, hands assigned).</summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Returns a human-readable checklist of what is and isn't initialized.
    /// Call this when IsInitialized is false to understand what's missing.
    /// </summary>
    string GetInitializationStatus();
}
