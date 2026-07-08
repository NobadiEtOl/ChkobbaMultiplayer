using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Handles saving and loading the player's recent emojis list using PlayerPrefs.
/// Maximum 24 emojis are stored in a pipe-separated string.
/// </summary>
public static class EmojiHistoryManager
{
    private const string PrefKey = "RecentEmojis";
    private const int MaxHistory = 24;

    /// <summary>
    /// Retrieves the list of recently used emojis from PlayerPrefs.
    /// </summary>
    /// <returns>A list of emoji strings.</returns>
    public static List<string> GetRecentEmojis()
    {
        string saved = PlayerPrefs.GetString(PrefKey, "");
        if (string.IsNullOrEmpty(saved))
        {
            return new List<string>();
        }
        return saved.Split(new[] { '|' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    /// <summary>
    /// Adds an emoji to the recent history. 
    /// If it already exists, it moves to the front. 
    /// If not, it's added to the front. 
    /// The list is capped at 24 items.
    /// </summary>
    /// <param name="emoji">The emoji string to add.</param>
    public static void AddEmoji(string emoji)
    {
        if (string.IsNullOrEmpty(emoji)) return;

        List<string> history = GetRecentEmojis();
        
        // Remove if already exists to move it to the front
        if (history.Contains(emoji))
        {
            history.Remove(emoji);
        }
        
        // Insert at the beginning
        history.Insert(0, emoji);

        // Trim to max history
        if (history.Count > MaxHistory)
        {
            history = history.Take(MaxHistory).ToList();
        }

        // Save
        PlayerPrefs.SetString(PrefKey, string.Join("|", history));
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Clears the emoji history (utility method).
    /// </summary>
    public static void ClearHistory()
    {
        PlayerPrefs.DeleteKey(PrefKey);
        PlayerPrefs.Save();
    }
}
