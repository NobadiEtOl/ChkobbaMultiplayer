using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the display and interaction of joker selection UI.
/// Manages a prefab with 3 joker display slots (Joker1/2/3Parent).
/// 
/// Responsibilities:
/// - Fetch 3 random jokers and populate the UI
/// - Handle joker selection with button clicks
/// - Support future animations via IEnumerators
/// 
/// Flow:
/// 1. ShowJokers() - Display 3 random jokers with sprites/text
/// 2. WaitForSelection() - Add buttons, wait for player click
/// 3. HideJokers() - Clear visuals and clean up
/// </summary>
public class JokerDisplayController : MonoBehaviour
{
    [SerializeField] private List<Transform> jokerParents = new List<Transform>();
    [SerializeField] private List<TextMeshProUGUI> titleTexts = new List<TextMeshProUGUI>();
    [SerializeField] private List<TextMeshProUGUI> descriptionTexts = new List<TextMeshProUGUI>();
    [SerializeField] private List<Image> jokerImages = new List<Image>();

    private int selectedJokerId = -1;
    private List<JokerController.JokerDefinition> displayedJokers = new List<JokerController.JokerDefinition>();
    private List<Button> jokerButtons = new List<Button>();

    /// <summary>
    /// Initialize: Ensure the panel is deactivated at startup.
    /// The panel will be activated only when a new stage starts.
    /// </summary>
    private void Start()
    {
        // Ensure panel is inactive at game start
        gameObject.SetActive(false);
        Debug.Log($"[JokerDisplayController] Initialized - panel deactivated");
    }

    /// <summary>
    /// Activate the joker selection panel.
    /// Called at the start of a new stage before showing jokers.
    /// </summary>
    public void ActivatePanel()
    {
        gameObject.SetActive(true);
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
    /// Populates the UI prefab with joker data.
    /// Ready for animation: yield instructions can be added here later.
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

        // Placeholder for animation: can yield here later for fade-in effects
        yield break;
    }

    /// <summary>
    /// Wait for player to select a joker.
    /// Adds button components at runtime to each joker parent.
    /// Blocks until a selection is made (selectedJokerId != -1).
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

        // Wait until a joker is selected
        while (selectedJokerId == -1)
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
    /// Removes button components and clears visuals.
    /// Ready for animation: can yield here later for fade-out effects.
    /// </summary>
    public IEnumerator HideJokers()
    {
        Debug.Log($"[JokerDisplayController] Hiding jokers...");

        // Clear all visuals
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

        // Placeholder for animation: can yield here later for fade-out effects
        yield break;
    }

    /// <summary>
    /// Called when a joker button is clicked.
    /// Stores the selected joker ID and disables buttons.
    /// </summary>
    private void OnJokerButtonClicked(int jokerIndex)
    {
        if (jokerIndex < 0 || jokerIndex >= displayedJokers.Count)
        {
            Debug.LogWarning($"[JokerDisplayController] Invalid joker index: {jokerIndex}");
            return;
        }

        var selectedJoker = displayedJokers[jokerIndex];
        selectedJokerId = selectedJoker.jokerID;

        Debug.Log($"[JokerDisplayController] Joker button clicked! Index: {jokerIndex}, ID: {selectedJokerId}");
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
        Debug.Log($"[JokerDisplayController] Selection reset");
    }
}
