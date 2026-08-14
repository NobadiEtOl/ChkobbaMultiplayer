using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Controls the display and interaction of joker selection UI.
/// Manages a prefab with 3 joker display slots (Joker1/2/3Parent).
/// 
/// Responsibilities:
/// - Fetch 3 random jokers and populate the UI
/// - Handle joker selection with button clicks
/// - Animate fade in/out transitions
/// - Animate joker pop-up and jiggle effects using DOTween
/// 
/// Flow:
/// 1. ShowJokers() - Fade in and display 3 random jokers with sprites/text
/// 2. Animate each joker popping up (scale 0→1) and jiggling in sequence
/// 3. WaitForSelection() - Add buttons, wait for player click
/// 4. On selection - Selected joker jiggles to confirm
/// 5. HideJokers() - Fade out and clear visuals
/// </summary>
public class JokerDisplayController : MonoBehaviour
{
    [SerializeField] private List<Transform> jokerParents = new List<Transform>();
    [SerializeField] private List<TextMeshProUGUI> titleTexts = new List<TextMeshProUGUI>();
    [SerializeField] private List<TextMeshProUGUI> descriptionTexts = new List<TextMeshProUGUI>();
    [SerializeField] private List<Image> jokerImages = new List<Image>();
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float popUpDuration = 0.4f;
    [SerializeField] private float wiggleDuration = 0.3f;
    [SerializeField] private float wiggleStrength = 0.15f;
    [SerializeField] private GameObject visualElementsHolder;

    private int selectedJokerId = -1;
    private bool isSelectionComplete = false;
    private List<JokerController.JokerDefinition> displayedJokers = new List<JokerController.JokerDefinition>();
    private List<Button> jokerButtons = new List<Button>();
    private CanvasGroup canvasGroup;
    private List<Sequence> activeSequences = new List<Sequence>();

    /// <summary>
    /// Initialize: Ensure the panel is deactivated at startup.
    /// The panel will be activated only when a new stage starts.
    /// Set initial scale to 0 for all joker parents for pop-up animation.
    /// </summary>
    private void Start()
    {
        // Get or add CanvasGroup for fade animations
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Set initial scale to 0 for all joker parents (they will pop up during ShowJokers)
        foreach (var jokerParent in jokerParents)
        {
            jokerParent.localScale = Vector3.zero;
        }

        // Ensure panel is inactive at game start
        gameObject.SetActive(false);
        canvasGroup.alpha = 0f;
        Debug.Log($"[JokerDisplayController] Initialized - panel deactivated");
    }

    /// <summary>
    /// Activate the joker selection panel.
    /// Called at the start of a new stage before showing jokers.
    /// </summary>
    public void ActivatePanel()
    {
        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        Debug.Log($"[JokerDisplayController] Panel activated");
    }

    /// <summary>
    /// Deactivate the joker selection panel.
    /// Called after the joker selection flow completes.
    /// </summary>
    public void DeactivatePanel()
    {
        gameObject.SetActive(false);
        Debug.Log($"[JokerDisplayController] Panel deactivated");
    }

    /// <summary>
    /// Display 3 random jokers with their sprites and descriptions.
    /// Populates the UI prefab with joker data and fades in.
    /// Each joker pops up and jiggles in sequence (1, 2, 3).
    /// </summary>
    public IEnumerator ShowJokers()
    {
        // Generate 3 random joker options
        var options = JokerController.GenerateRandomJokerOptions(3, Random.state);
        displayedJokers = new List<JokerController.JokerDefinition>(options);

        // Populate UI elements for each joker
        for (int i = 0; i < displayedJokers.Count && i < jokerParents.Count; i++)
        {
            var joker = displayedJokers[i];

            // Set joker image
            if (jokerImages.Count > i && jokerImages[i] != null)
            {
                jokerImages[i].sprite = joker.jokerImage;
            }

            // Set joker title
            if (titleTexts.Count > i && titleTexts[i] != null)
            {
                titleTexts[i].text = joker.jokerName;
            }

            // Set joker description
            if (descriptionTexts.Count > i && descriptionTexts[i] != null)
            {
                descriptionTexts[i].text = joker.description;
            }

            Debug.Log($"[JokerDisplayController] Showing joker {i}: {joker.jokerName} (ID: {joker.jokerID})");
        }

        // Fade in animation
        yield return StartCoroutine(FadeIn());

        // Animate each joker popping up and jiggling in sequence (1, 2, 3)
        for (int i = 0; i < jokerParents.Count && i < displayedJokers.Count; i++)
        {
            yield return StartCoroutine(AnimateJokerPopUpAndJiggle(i));
        }

        Debug.Log($"[JokerDisplayController] All jokers animated - ready for selection");
    }

    /// <summary>
    /// Animate a joker popping up (scaling from 0 to 1) and then jiggling.
    /// Pop-up uses OutBack ease for elastic effect, jiggle uses rotation wobble.
    /// </summary>
    private IEnumerator AnimateJokerPopUpAndJiggle(int jokerIndex)
    {
        if (jokerIndex >= jokerParents.Count)
            yield break;

        Transform jokerParent = jokerParents[jokerIndex];
        Sequence popSequence = DOTween.Sequence();

        // Pop-up: Scale from 0 to 1 with OutBack elastic ease
        popSequence.Append(jokerParent.DOScale(1f, popUpDuration).SetEase(Ease.OutBack));

        // Jiggle: Rotation wobble (left, right, center) to create jiggling effect
        popSequence.Append(jokerParent.DORotate(new Vector3(0, 0, wiggleStrength), wiggleDuration / 3)
            .SetEase(Ease.InOutQuad));
        popSequence.Append(jokerParent.DORotate(new Vector3(0, 0, -wiggleStrength), wiggleDuration / 3)
            .SetEase(Ease.InOutQuad));
        popSequence.Append(jokerParent.DORotate(Vector3.zero, wiggleDuration / 3)
            .SetEase(Ease.InOutQuad));

        activeSequences.Add(popSequence);

        // Wait for the sequence to complete
        yield return popSequence.WaitForCompletion();

        Debug.Log($"[JokerDisplayController] Joker {jokerIndex} pop-up and jiggle complete");
    }

    /// <summary>
    /// Animate a joker jiggling to indicate selection (more pronounced movement).
    /// Used when the player selects a joker.
    /// </summary>
    private IEnumerator JiggleJokerForSelection(int jokerIndex)
    {
        if (jokerIndex >= jokerParents.Count)
            yield break;

        Transform jokerParent = jokerParents[jokerIndex];
        Sequence wiggleSequence = DOTween.Sequence();

        // Jiggle with more pronounced rotation movement
        float jigglePower = wiggleStrength * 1.5f;
        wiggleSequence.Append(jokerParent.DORotate(new Vector3(0, 0, jigglePower), wiggleDuration / 3)
            .SetEase(Ease.InOutQuad));
        wiggleSequence.Append(jokerParent.DORotate(new Vector3(0, 0, -jigglePower), wiggleDuration / 3)
            .SetEase(Ease.InOutQuad));
        wiggleSequence.Append(jokerParent.DORotate(Vector3.zero, wiggleDuration / 3)
            .SetEase(Ease.InOutQuad));

        activeSequences.Add(wiggleSequence);

        yield return wiggleSequence.WaitForCompletion();

        Debug.Log($"[JokerDisplayController] Joker {jokerIndex} selection jiggle complete");
    }

    /// <summary>
    /// Wait for player to select a joker.
    /// Adds button components at runtime to each joker parent.
    /// Blocks until a selection is made and the selection jiggle animation completes.
    /// </summary>
    public IEnumerator WaitForSelection()
    {
        // Clear previous buttons
        jokerButtons.Clear();

        // Add button component to each joker parent and set up listeners
        for (int i = 0; i < jokerParents.Count; i++)
        {
            Transform parent = jokerParents[i];
            Button button = parent.GetComponent<Button>();
            
            // If button doesn't exist, add it
            if (button == null)
            {
                button = parent.gameObject.AddComponent<Button>();
            }

            // Set the target graphic (the image that shows the pressed state)
            Image image = parent.GetComponent<Image>();
            if (image != null)
            {
                button.targetGraphic = image;
            }

            // Store reference
            jokerButtons.Add(button);

            // Create local copy of index to avoid closure issues
            int jokerIndex = i;
            
            // Add listener for this button
            button.onClick.AddListener(() => OnJokerButtonClicked(jokerIndex));
        }

        Debug.Log($"[JokerDisplayController] Waiting for joker selection...");

        // Wait until selection is complete (includes button click and jiggle animation)
        while (!isSelectionComplete)
        {
            yield return null;
        }

        // Disable all buttons after selection
        foreach (var button in jokerButtons)
        {
            button.enabled = false;
        }

        Debug.Log($"[JokerDisplayController] Joker selected! ID: {selectedJokerId}");
    }

    /// <summary>
    /// Hide/clear the joker display.
    /// Fades out, removes button components, clears visuals, and resets transforms.
    /// </summary>
    public IEnumerator HideJokers()
    {
        Debug.Log($"[JokerDisplayController] Hiding jokers...");

        // Fade out animation
        yield return StartCoroutine(FadeOut());

        // Clear all visuals and reset transforms
        for (int i = 0; i < jokerParents.Count; i++)
        {
            if (jokerImages.Count > i && jokerImages[i] != null)
            {
                jokerImages[i].sprite = null;
            }

            if (titleTexts.Count > i && titleTexts[i] != null)
            {
                titleTexts[i].text = "";
            }

            if (descriptionTexts.Count > i && descriptionTexts[i] != null)
            {
                descriptionTexts[i].text = "";
            }

            // Reset scale and rotation
            jokerParents[i].localScale = Vector3.zero;
            jokerParents[i].localRotation = Quaternion.identity;
        }

        // Remove button components
        foreach (var button in jokerButtons)
        {
            if (button != null)
            {
                Destroy(button);
            }
        }

        jokerButtons.Clear();
        displayedJokers.Clear();
    }

    /// <summary>
    /// Called when a joker button is clicked.
    /// Stores the selected joker ID, triggers jiggle animation, and marks selection as complete.
    /// </summary>
    private void OnJokerButtonClicked(int jokerIndex)
    {
        // Prevent multiple selections
        if (isSelectionComplete) return;

        if (jokerIndex < 0 || jokerIndex >= displayedJokers.Count)
        {
            Debug.LogWarning($"[JokerDisplayController] Invalid joker index: {jokerIndex}");
            return;
        }

        var selectedJoker = displayedJokers[jokerIndex];
        selectedJokerId = selectedJoker.jokerID;

        Debug.Log($"[JokerDisplayController] Joker button clicked! Index: {jokerIndex}, ID: {selectedJokerId}");

        // Trigger jiggle animation for the selected joker
        // After jiggle completes, mark selection as complete
        StartCoroutine(JiggleAndCompleteSelection(jokerIndex));
    }

    /// <summary>
    /// Jiggle the selected joker and then mark selection as complete.
    /// </summary>
    private IEnumerator JiggleAndCompleteSelection(int jokerIndex)
    {
        yield return StartCoroutine(JiggleJokerForSelection(jokerIndex));
        isSelectionComplete = true;
        Debug.Log($"[JokerDisplayController] Selection animation complete");
    }

    /// <summary>
    /// Get the selected joker ID.
    /// Called by SinglePlayerModeController after WaitForSelection() completes.
    /// </summary>
    public int GetSelectedJokerId()
    {
        return selectedJokerId;
    }

    /// <summary>
    /// Reset the selected joker ID for the next stage.
    /// Called by SinglePlayerModeController after processing the selection.
    /// </summary>
    public void ResetSelection()
    {
        selectedJokerId = -1;
        isSelectionComplete = false;
        Debug.Log($"[JokerDisplayController] Selection reset");
    }

    /// <summary>
    /// Fade in the panel over fadeDuration seconds.
    /// </summary>
    private IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        Debug.Log($"[JokerDisplayController] Fade in complete");
    }

    /// <summary>
    /// Fade out the panel over fadeDuration seconds.
    /// </summary>
    private IEnumerator FadeOut()
    {
        if (canvasGroup == null) yield break;

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1f - (elapsedTime / fadeDuration));
            yield return null;
        }

        canvasGroup.alpha = 0f;
        Debug.Log($"[JokerDisplayController] Fade out complete");
    }

    /// <summary>
    /// Clean up DOTween sequences when the object is destroyed.
    /// Prevents animation errors and memory leaks.
    /// </summary>
    private void OnDestroy()
    {
        foreach (var sequence in activeSequences)
        {
            if (sequence != null && sequence.IsActive())
            {
                sequence.Kill();
            }
        }
        activeSequences.Clear();
    }
}
