using UnityEngine;

/// <summary>
/// Central repository for all joker definitions.
/// This is the single source of truth for joker data.
/// Add, modify, or remove jokers here; JokerController will use these definitions.
/// </summary>
public static class JokerDefinitions
{
    /// <summary>
    /// JOKER 0: Flat +2 Damage
    /// Adds +2 to all damage dealt to opponent, regardless of capture type.
    /// Behavior implemented in JokerLogic.OnCapture_JokerFlatPlus2().
    /// </summary>
    public static readonly JokerController.JokerDefinition FlatPlus2 = new JokerController.JokerDefinition
    {
        jokerID = 0,
        jokerName = "Flat +2",
        description = "Adds +2 to all damage dealt to opponent, regardless of capture type",
        jokerImage = null, // Placeholder; art will be added later
        rarity = JokerController.JokerRarity.Common
    };

    /// <summary>
    /// JOKER 1: Jack Capture x2
    /// Doubles the base damage of Jack captures only (not Jack pişti).
    /// Behavior implemented in JokerLogic.OnCapture_JokerJackCaptureDoubler().
    /// </summary>
    public static readonly JokerController.JokerDefinition JackCaptureDoubler = new JokerController.JokerDefinition
    {
        jokerID = 1,
        jokerName = "Jack Capture Doubler",
        description = "Doubles the base damage when capturing a Jack (not Jack pişti)",
        jokerImage = null, // Placeholder; art will be added later
        rarity = JokerController.JokerRarity.Uncommon
    };

    /// <summary>
    /// JOKER 2: Normal Capture x2
    /// Doubles the base damage of normal (non-Jack) captures only (not pişti).
    /// Behavior implemented in JokerLogic.OnCapture_JokerNormalCaptureDoubler().
    /// </summary>
    public static readonly JokerController.JokerDefinition NormalCaptureDoubler = new JokerController.JokerDefinition
    {
        jokerID = 2,
        jokerName = "Normal Capture Doubler",
        description = "Doubles the base damage when capturing non-Jack cards (not pişti)",
        jokerImage = null, // Placeholder; art will be added later
        rarity = JokerController.JokerRarity.Uncommon
    };

    /// <summary>
    /// JOKER 3: Regular Pişti x2
    /// Doubles the base damage when a regular pişti occurs (2 same non-Jack cards).
    /// Behavior implemented in JokerLogic.OnCapture_JokerRegularPiştiDoubler().
    /// </summary>
    public static readonly JokerController.JokerDefinition RegularPiştiDoubler = new JokerController.JokerDefinition
    {
        jokerID = 3,
        jokerName = "Regular Pişti Doubler",
        description = "Doubles the base damage when capturing a regular pişti (2 same non-Jack cards)",
        jokerImage = null, // Placeholder; art will be added later
        rarity = JokerController.JokerRarity.Rare
    };

    /// <summary>
    /// JOKER 4: Jack Pişti x2
    /// Doubles the base damage when a jack pişti occurs (2 Jacks).
    /// Behavior implemented in JokerLogic.OnCapture_JokerJackPiştiDoubler().
    /// </summary>
    public static readonly JokerController.JokerDefinition JackPiştiDoubler = new JokerController.JokerDefinition
    {
        jokerID = 4,
        jokerName = "Jack Pişti Doubler",
        description = "Doubles the base damage when capturing a jack pişti (2 Jacks)",
        jokerImage = null, // Placeholder; art will be added later
        rarity = JokerController.JokerRarity.Rare
    };

    /// <summary>
    /// JOKER 5: Two of Clubs Bonus
    /// Adds +3 bonus damage when 2 of Clubs is captured.
    /// Behavior implemented in JokerLogic.OnCapture_JokerTwoOfClubsBonus().
    /// </summary>
    public static readonly JokerController.JokerDefinition TwoOfClubsBonus = new JokerController.JokerDefinition
    {
        jokerID = 5,
        jokerName = "Two of Clubs Bonus",
        description = "Adds +3 bonus damage when 2 of Clubs is captured",
        jokerImage = null, // Placeholder; art will be added later
        rarity = JokerController.JokerRarity.Uncommon
    };

    /// <summary>
    /// JOKER 6: Two of Clubs Multiplier
    /// Doubles the base damage when 2 of Clubs is captured.
    /// Behavior implemented in JokerLogic.OnCapture_JokerTwoOfClubsMultiplier().
    /// </summary>
    public static readonly JokerController.JokerDefinition TwoOfClubsMultiplier = new JokerController.JokerDefinition
    {
        jokerID = 6,
        jokerName = "Two of Clubs Multiplier",
        description = "Doubles the base damage when capturing 2 of Clubs",
        jokerImage = null, // Placeholder; art will be added later
        rarity = JokerController.JokerRarity.Rare
    };

    /// <summary>
    /// JOKER 7: Ten of Diamonds Bonus
    /// Adds +3 bonus damage when 10 of Diamonds is captured.
    /// Behavior implemented in JokerLogic.OnCapture_JokerTenOfDiamondsBonus().
    /// </summary>
    public static readonly JokerController.JokerDefinition TenOfDiamondsBonus = new JokerController.JokerDefinition
    {
        jokerID = 7,
        jokerName = "Ten of Diamonds Bonus",
        description = "Adds +3 bonus damage when 10 of Diamonds is captured",
        jokerImage = null, // Placeholder; art will be added later
        rarity = JokerController.JokerRarity.Uncommon
    };

    /// <summary>
    /// JOKER 8: Ten of Diamonds Multiplier
    /// Doubles the base damage when 10 of Diamonds is captured.
    /// Behavior implemented in JokerLogic.OnCapture_JokerTenOfDiamondsMultiplier().
    /// </summary>
    public static readonly JokerController.JokerDefinition TenOfDiamondsMultiplier = new JokerController.JokerDefinition
    {
        jokerID = 8,
        jokerName = "Ten of Diamonds Multiplier",
        description = "Doubles the base damage when capturing 10 of Diamonds",
        jokerImage = null, // Placeholder; art will be added later
        rarity = JokerController.JokerRarity.Rare
    };

    /// <summary>
    /// Get all defined jokers as a collection.
    /// Used by JokerController to populate the joker pool.
    /// </summary>
    public static JokerController.JokerDefinition[] GetAllJokerDefinitions()
    {
        return new JokerController.JokerDefinition[]
        {
            FlatPlus2,
            JackCaptureDoubler,
            NormalCaptureDoubler,
            RegularPiştiDoubler,
            JackPiştiDoubler,
            TwoOfClubsBonus,
            TwoOfClubsMultiplier,
            TenOfDiamondsBonus,
            TenOfDiamondsMultiplier,
        };
    }
}
