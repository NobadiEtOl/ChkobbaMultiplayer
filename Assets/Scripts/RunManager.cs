using System;
using UnityEngine;

/// <summary>
/// Manages run state persistence to PlayerPrefs.
/// Stores and retrieves run progress for mid-run save/load and run history.
/// </summary>
public static class RunManager
{
    [System.Serializable]
    public class RunProgressData
    {
        public int currentStage;              // 0-indexed
        public int currentOpponentDifficulty; // 0, 1, 2
        public int currentRound;              // Within the current opponent round
        public int totalDamageDealt;          // Cumulative opponent damage
        public int activeJokerId;             // Selected joker for this run
        public int currentGold;               // Player's gold amount
        public int seed;                      // Deck seed
public string deckClassName;          // "Balanced", "Aggressive", etc.
        public long timestamp;                // Run start time
    }

    private const string PLAYERPREFS_PREFIX = "RunProgress_";
    private const string KEY_STAGE = PLAYERPREFS_PREFIX + "CurrentStage";
    private const string KEY_OPP_DIFFICULTY = PLAYERPREFS_PREFIX + "CurrentOppDifficulty";
    private const string KEY_ROUND = PLAYERPREFS_PREFIX + "CurrentRound";
    private const string KEY_TOTAL_DAMAGE = PLAYERPREFS_PREFIX + "TotalDamageDealt";
    private const string KEY_JOKER_ID = PLAYERPREFS_PREFIX + "ActiveJokerId";
    private const string KEY_GOLD = PLAYERPREFS_PREFIX + "CurrentGold";
    private const string KEY_SEED = PLAYERPREFS_PREFIX + "Seed";
private const string KEY_DECK_CLASS = PLAYERPREFS_PREFIX + "DeckClass";
    private const string KEY_TIMESTAMP = PLAYERPREFS_PREFIX + "Timestamp";

    /// <summary>
    /// Save current run progress to PlayerPrefs.
    /// </summary>
    public static void SaveRunProgress(RunProgressData data)
    {
        if (data == null)
        {
            
            return;
        }

        Debug.Log($"[RunManager] Saving run progress: Stage {data.currentStage}, " +
                  $"Opponent {data.currentOpponentDifficulty}, Damage {data.totalDamageDealt}");

        PlayerPrefs.SetInt(KEY_STAGE, data.currentStage);
        PlayerPrefs.SetInt(KEY_OPP_DIFFICULTY, data.currentOpponentDifficulty);
        PlayerPrefs.SetInt(KEY_ROUND, data.currentRound);
        PlayerPrefs.SetInt(KEY_TOTAL_DAMAGE, data.totalDamageDealt);
        PlayerPrefs.SetInt(KEY_JOKER_ID, data.activeJokerId);
        PlayerPrefs.SetInt(KEY_GOLD, data.currentGold);
        PlayerPrefs.SetInt(KEY_SEED, data.seed);
PlayerPrefs.SetString(KEY_DECK_CLASS, data.deckClassName ?? "Balanced");
        PlayerPrefs.SetString(KEY_TIMESTAMP, data.timestamp.ToString());

        PlayerPrefs.Save();
        
    }

    /// <summary>
    /// Load current run progress from PlayerPrefs.
    /// Returns null if no run progress exists.
    /// </summary>
    public static RunProgressData LoadRunProgress()
    {
        // Check if any run progress exists
        if (!PlayerPrefs.HasKey(KEY_STAGE))
        {
            
            return null;
        }

        try
        {
            var data = new RunProgressData
            {
                currentStage = PlayerPrefs.GetInt(KEY_STAGE, 0),
                currentOpponentDifficulty = PlayerPrefs.GetInt(KEY_OPP_DIFFICULTY, 0),
                currentRound = PlayerPrefs.GetInt(KEY_ROUND, 0),
                totalDamageDealt = PlayerPrefs.GetInt(KEY_TOTAL_DAMAGE, 0),
                activeJokerId = PlayerPrefs.GetInt(KEY_JOKER_ID, -1),
                currentGold = PlayerPrefs.GetInt(KEY_GOLD, 0),
                seed = PlayerPrefs.GetInt(KEY_SEED, 0),
                deckClassName = PlayerPrefs.GetString(KEY_DECK_CLASS, "Balanced"),
                timestamp = long.Parse(PlayerPrefs.GetString(KEY_TIMESTAMP, "0"))
            };

            Debug.Log($"[RunManager] Loaded run progress: Stage {data.currentStage}, " +
                      $"Opponent {data.currentOpponentDifficulty}, Damage {data.totalDamageDealt}");

            return data;
        }
        catch (Exception e)
        {
            
            return null;
        }
    }

    /// <summary>
    /// Clear all saved run progress.
    /// Called when player loses or completes a run.
    /// </summary>
    public static void ClearRunProgress()
    {
        

        PlayerPrefs.DeleteKey(KEY_STAGE);
        PlayerPrefs.DeleteKey(KEY_OPP_DIFFICULTY);
        PlayerPrefs.DeleteKey(KEY_ROUND);
        PlayerPrefs.DeleteKey(KEY_TOTAL_DAMAGE);
        PlayerPrefs.DeleteKey(KEY_JOKER_ID);
        PlayerPrefs.DeleteKey(KEY_GOLD);
        PlayerPrefs.DeleteKey(KEY_SEED);
PlayerPrefs.DeleteKey(KEY_DECK_CLASS);
        PlayerPrefs.DeleteKey(KEY_TIMESTAMP);

        PlayerPrefs.Save();
        
    }

    /// <summary>
    /// Check if there is a saved run in progress.
    /// </summary>
    public static bool HasSavedRunProgress()
    {
        return PlayerPrefs.HasKey(KEY_STAGE);
    }

    /// <summary>
    /// Debug method to print current saved run progress.
    /// </summary>
    [ContextMenu("Print Saved Run Progress")]
    public static void PrintSavedRunProgress()
    {
        var data = LoadRunProgress();
        if (data != null)
        {
            Debug.Log($"=== SAVED RUN PROGRESS ===\n" +
                      $"Stage: {data.currentStage}\n" +
                      $"Opponent: {data.currentOpponentDifficulty}\n" +
                      $"Round: {data.currentRound}\n" +
                      $"Total Damage: {data.totalDamageDealt}\n" +
                      $"Joker ID: {data.activeJokerId}\n" +
                      $"Seed: {data.seed}\n" +
                      $"Deck Class: {data.deckClassName}\n" +
                      $"Timestamp: {data.timestamp}");
        }
        else
        {
            
        }
    }
}
