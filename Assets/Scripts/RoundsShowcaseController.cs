using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Displays opponent information (max health and playstyle) before each round starts.
/// Integrates with SinglePlayerModeController to show/hide the showcase screen and block round progression until player continues.
/// </summary>
public class RoundsShowcaseController : MonoBehaviour
{
    // Default background color for inactive opponents
    [SerializeField] private Color defaultBackgroundColor = Color.white;

    // Easy opponent (difficulty 0) UI references
    [SerializeField] private TextMeshProUGUI easyHealthText;
    [SerializeField] private TextMeshProUGUI easyPlayStyleText;
    [SerializeField] private Image easyBackgroundImage;
    [SerializeField] private Color easyBackgroundColor = Color.white;

    // Normal opponent (difficulty 1) UI references
    [SerializeField] private TextMeshProUGUI normalHealthText;
    [SerializeField] private TextMeshProUGUI normalPlayStyleText;
    [SerializeField] private Image normalBackgroundImage;
    [SerializeField] private Color normalBackgroundColor = Color.white;

    // Hard opponent (difficulty 2) UI references
    [SerializeField] private TextMeshProUGUI hardHealthText;
    [SerializeField] private TextMeshProUGUI hardPlayStyleText;
    [SerializeField] private Image hardBackgroundImage;
    [SerializeField] private Color hardBackgroundColor = Color.white;

    // Continue button
    [SerializeField] private Button continueButton;

    // Callback invoked when player presses Continue
    public System.Action onContinuePressed;

    /// <summary>
    /// Display opponent information for the upcoming round.
    /// Updates the appropriate difficulty panel with health and playstyle text.
    /// Shows the screen and sets up the continue button.
    /// </summary>
    public void ShowRound(int difficulty, int maxHealth, OpponentBehaviorManager.OpponentBehaviorMode mode)
    {
        // Apply default color to all backgrounds first
        if (easyBackgroundImage != null)
            easyBackgroundImage.color = defaultBackgroundColor;
        if (normalBackgroundImage != null)
            normalBackgroundImage.color = defaultBackgroundColor;
        if (hardBackgroundImage != null)
            hardBackgroundImage.color = defaultBackgroundColor;

        // Get the correct text fields and background image based on difficulty
        TextMeshProUGUI healthText = null;
        TextMeshProUGUI playStyleText = null;
        Image backgroundImage = null;
        Color backgroundColor = Color.white;

        switch (difficulty)
        {
            case 0: // Easy
                healthText = easyHealthText;
                playStyleText = easyPlayStyleText;
                backgroundImage = easyBackgroundImage;
                backgroundColor = easyBackgroundColor;
                break;

            case 1: // Normal
                healthText = normalHealthText;
                playStyleText = normalPlayStyleText;
                backgroundImage = normalBackgroundImage;
                backgroundColor = normalBackgroundColor;
                break;

            case 2: // Hard
                healthText = hardHealthText;
                playStyleText = hardPlayStyleText;
                backgroundImage = hardBackgroundImage;
                backgroundColor = hardBackgroundColor;
                break;

            default:
                Debug.LogError($"[RoundsShowcaseController] Invalid difficulty: {difficulty}");
                return;
        }

        // Validate references
        if (healthText == null || playStyleText == null)
        {
            Debug.LogError($"[RoundsShowcaseController] Missing TMP text references for difficulty {difficulty}");
            return;
        }

        // Update text fields
        healthText.text = $"{maxHealth} HP";
        playStyleText.text = OpponentBehaviorManager.GetPlaystyleDescription(mode);

        // Apply active background color to current opponent
        if (backgroundImage != null)
        {
            backgroundImage.color = backgroundColor;
        }

        // Show the showcase screen
        gameObject.SetActive(true);

        // Setup continue button
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueButtonPressed);
        }
        else
        {
            Debug.LogError("[RoundsShowcaseController] Continue button is not assigned!");
        }
    }

    /// <summary>
    /// Display all 3 opponent information for the current stage at once.
    /// Updates all difficulty panels (Easy, Normal, Hard) with their respective health and playstyle.
    /// Only the current opponent's background is colored; the others use the default background color.
    /// Shows the screen and sets up the continue button.
    /// </summary>
    public void ShowAllOpponents(int[] maxHealthValues, OpponentBehaviorManager.OpponentBehaviorMode[] modes, int currentDifficulty)
    {
        if (maxHealthValues == null || modes == null || maxHealthValues.Length != 3 || modes.Length != 3)
        {
            Debug.LogError("[RoundsShowcaseController] ShowAllOpponents requires arrays of exactly 3 elements");
            return;
        }

        // Apply default color to all backgrounds first
        if (easyBackgroundImage != null)
            easyBackgroundImage.color = defaultBackgroundColor;
        if (normalBackgroundImage != null)
            normalBackgroundImage.color = defaultBackgroundColor;
        if (hardBackgroundImage != null)
            hardBackgroundImage.color = defaultBackgroundColor;

        // Update Easy opponent (difficulty 0)
        if (easyHealthText != null && easyPlayStyleText != null)
        {
            easyHealthText.text = $"{maxHealthValues[0]} HP";
            easyPlayStyleText.text = OpponentBehaviorManager.GetPlaystyleDescription(modes[0]);
        }
        else
        {
            Debug.LogError("[RoundsShowcaseController] Missing Easy opponent text references");
        }

        if (easyBackgroundImage != null && currentDifficulty == 0)
        {
            easyBackgroundImage.color = easyBackgroundColor;
        }

        // Update Normal opponent (difficulty 1)
        if (normalHealthText != null && normalPlayStyleText != null)
        {
            normalHealthText.text = $"{maxHealthValues[1]} HP";
            normalPlayStyleText.text = OpponentBehaviorManager.GetPlaystyleDescription(modes[1]);
        }
        else
        {
            Debug.LogError("[RoundsShowcaseController] Missing Normal opponent text references");
        }

        if (normalBackgroundImage != null && currentDifficulty == 1)
        {
            normalBackgroundImage.color = normalBackgroundColor;
        }

        // Update Hard opponent (difficulty 2)
        if (hardHealthText != null && hardPlayStyleText != null)
        {
            hardHealthText.text = $"{maxHealthValues[2]} HP";
            hardPlayStyleText.text = OpponentBehaviorManager.GetPlaystyleDescription(modes[2]);
        }
        else
        {
            Debug.LogError("[RoundsShowcaseController] Missing Hard opponent text references");
        }

        if (hardBackgroundImage != null && currentDifficulty == 2)
        {
            hardBackgroundImage.color = hardBackgroundColor;
        }

        // Show the showcase screen
        gameObject.SetActive(true);

        // Setup continue button
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueButtonPressed);
        }
        else
        {
            Debug.LogError("[RoundsShowcaseController] Continue button is not assigned!");
        }
    }

    /// <summary>
    /// Called when the continue button is pressed.
    /// Hides the showcase screen and invokes the callback to unblock SinglePlayerModeController.
    /// </summary>
    private void OnContinueButtonPressed()
    {
        // Hide the showcase screen
        gameObject.SetActive(false);

        // Invoke the callback to signal SinglePlayerModeController to resume
        onContinuePressed?.Invoke();
    }

    private void Start()
    {
        // Ensure screen starts hidden
        gameObject.SetActive(false);
    }

    private void Update()
    {
        // No-op; all logic is event-driven
    }
}
