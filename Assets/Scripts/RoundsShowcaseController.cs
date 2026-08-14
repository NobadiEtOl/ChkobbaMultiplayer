using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Displays opponent information (max health and playstyle) before each round starts.
/// Integrates with SinglePlayerModeController to show/hide the showcase screen and block round progression until player continues.
/// Animates fade in and fade out transitions.
/// Animates moving element holder sliding up/down during transitions.
/// </summary>
public class RoundsShowcaseController : MonoBehaviour
{
    // Default background color for inactive opponents
    [SerializeField] private Color defaultBackgroundColor = Color.white;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float slideDuration = 0.6f;
    [SerializeField] private float slideDistance = 200f; // How far down the moving element starts

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

    [SerializeField] private GameObject movingElementHolder;

    // Callback invoked when player presses Continue
    public System.Action onContinuePressed;

    private CanvasGroup canvasGroup;
    private RectTransform movingElementRectTransform;
    private Vector2 movingElementOriginalPosition;
    private Sequence currentAnimationSequence;

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
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

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

        // Fade in the showcase screen
        StartCoroutine(FadeIn());
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
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

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

        // Fade in the showcase screen
        StartCoroutine(FadeIn());
    }

    /// <summary>
    /// Called when the continue button is pressed.
    /// Fades out, hides the showcase screen and invokes the callback to unblock SinglePlayerModeController.
    /// </summary>
    private void OnContinueButtonPressed()
    {
        StartCoroutine(FadeOutAndHide());
    }

    /// <summary>
    /// Fade out the panel and hide it, then invoke the callback.
    /// </summary>
    private IEnumerator FadeOutAndHide()
    {
        yield return StartCoroutine(FadeOut());

        // Hide the showcase screen
        gameObject.SetActive(false);

        // Invoke the callback to signal SinglePlayerModeController to resume
        onContinuePressed?.Invoke();
    }

    /// <summary>
    /// Fade in the panel and slide the moving element up into place.
    /// Both animations happen in parallel for a smooth entrance.
    /// </summary>
    private IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;

        // Kill any existing animation sequence
        if (currentAnimationSequence != null && currentAnimationSequence.IsActive())
        {
            currentAnimationSequence.Kill();
        }

        currentAnimationSequence = DOTween.Sequence();

        // Fade in the main panel
        currentAnimationSequence.Append(DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1f, fadeDuration)
            .SetEase(Ease.InOutQuad));

        // Slide the moving element up in parallel (at the same time as fade in)
        if (movingElementRectTransform != null)
        {
            currentAnimationSequence.Insert(0, DOTween.To(() => movingElementRectTransform.anchoredPosition, x => movingElementRectTransform.anchoredPosition = x, movingElementOriginalPosition, slideDuration)
                .SetEase(Ease.OutCubic));
        }

        yield return currentAnimationSequence.WaitForCompletion();
        Debug.Log($"[RoundsShowcaseController] Fade in and slide up complete");
    }

    /// <summary>
    /// Fade out the panel and slide the moving element down in parallel.
    /// </summary>
    private IEnumerator FadeOut()
    {
        if (canvasGroup == null) yield break;

        // Kill any existing animation sequence
        if (currentAnimationSequence != null && currentAnimationSequence.IsActive())
        {
            currentAnimationSequence.Kill();
        }

        currentAnimationSequence = DOTween.Sequence();

        // Fade out the main panel
        currentAnimationSequence.Append(DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0f, fadeDuration)
            .SetEase(Ease.InOutQuad));

        // Slide the moving element down in parallel (at the same time as fade out)
        if (movingElementRectTransform != null)
        {
            currentAnimationSequence.Insert(0, DOTween.To(() => movingElementRectTransform.anchoredPosition, x => movingElementRectTransform.anchoredPosition = x, 
                movingElementOriginalPosition - new Vector2(0, slideDistance), slideDuration)
                .SetEase(Ease.InCubic));
        }

        yield return currentAnimationSequence.WaitForCompletion();
        Debug.Log($"[RoundsShowcaseController] Fade out and slide down complete");
    }

    private void Start()
    {
        // Get or add CanvasGroup for fade animations
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Get the RectTransform of the moving element
        if (movingElementHolder != null)
        {
            movingElementRectTransform = movingElementHolder.GetComponent<RectTransform>();
            if (movingElementRectTransform != null)
            {
                // Store the original position
                movingElementOriginalPosition = movingElementRectTransform.anchoredPosition;
                
                // Start with the element positioned lower (hidden below)
                movingElementRectTransform.anchoredPosition = movingElementOriginalPosition - new Vector2(0, slideDistance);
            }
        }

        // Ensure screen starts hidden
        gameObject.SetActive(false);
        canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Clean up DOTween sequences when the object is destroyed.
    /// Prevents animation errors and memory leaks.
    /// </summary>
    private void OnDestroy()
    {
        if (currentAnimationSequence != null && currentAnimationSequence.IsActive())
        {
            currentAnimationSequence.Kill();
        }
    }
}
