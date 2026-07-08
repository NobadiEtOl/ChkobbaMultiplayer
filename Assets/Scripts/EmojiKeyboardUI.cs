using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using TMPro;

/// <summary>
/// Runtime controller for the custom Emoji Keyboard UI.
/// Handles category switching, dynamic grid population, and opening/closing with transitions.
/// </summary>
public class EmojiKeyboardUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private GameObject emojiButtonPrefab;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.2f;

    /// <summary>
    /// Event triggered when an emoji is selected.
    /// </summary>
    public Action<string> OnEmojiSelected;

    private static readonly string[] Smileys = { "😊", "😂", "🤣", "😍", "😒", "😁", "😉", "😋", "😎", "😢", "😡", "😱", "😴", "😇", "🥳", "🥺", "🤡", "💩", "👻", "💀", "👽", "🤖", "🎃", "😺" };
    private static readonly string[] Gestures = { "👍", "👎", "👌", "✌️", "🤞", "👋", "👏", "🙌", "👐", "🙏", "💪", "🤙", "🖕", "🖖", "🤝", "💅", "🤳", "✍️", "🖐️", "✋" };
    private static readonly string[] Nature = { "🌿", "🌸", "🌲", "🌞", "🌙", "⭐", "🌊", "🍁", "🌻", "🌵", "🍎", "🍓", "🐶", "🐱", "🦊", "🦁", "🐧", "🦄", "🌈", "🔥", "🍄", "🥥", "🥦", "🥑" };
    private static readonly string[] Activities = { "⚽", "🏀", "🎾", "🏐", "🏈", "🎮", "🎲", "🎯", "🎳", "🎨", "🎭", "🎤", "🎧", "🎷", "🎸", "🎻", "🎬", "🎟️", "🚴", "🏋️", "🏆", "🏅" };
    private static readonly string[] Objects = { "💡", "💎", "📱", "💻", "⏰", "📚", "🖊️", "✉️", "🎁", "🏆", "🚗", "✈️", "⚓", "🔑", "🔨", "🔒", "💰", "🔔", "🔭", "🕯️", "🛠️", "🛡️" };

    private bool isOpen = false;

    private void Awake()
    {
        // Ensure keyboard is closed on start
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    /// <summary>
    /// Opens the keyboard with a fade-in effect and populates the 'Recent' category by default.
    /// </summary>
    public void OpenKeyboard()
    {
        if (isOpen) return;
        isOpen = true;

        StopAllCoroutines();
        StartCoroutine(FadeRoutine(1f, true));

        // Default to Recent
        SwitchCategory("Recent");
    }

    /// <summary>
    /// Closes the keyboard with a fade-out effect.
    /// </summary>
    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;

        StopAllCoroutines();
        StartCoroutine(FadeRoutine(0f, false));
    }

    /// <summary>
    /// Overload for EventTrigger compatibility.
    /// </summary>
    public void Close(UnityEngine.EventSystems.BaseEventData eventData)
    {
        Close();
    }

    private System.Collections.IEnumerator FadeRoutine(float targetAlpha, bool interactable)
    {
        if (canvasGroup == null) yield break;

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0;

        // Set interactivity immediately or at the end based on fade direction
        if (interactable)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        if (!interactable)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    /// <summary>
    /// Switches the active emoji category and repopulates the grid.
    /// </summary>
    /// <param name="category">Category name: Recent, Smileys, Gestures, Nature, Activities, Objects</param>
    public void SwitchCategory(string category)
    {
        PopulateGrid(category);
        
        // Reset scroll position to top
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void PopulateGrid(string category)
    {
        if (content == null || emojiButtonPrefab == null) return;

        // Clear existing buttons
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        IEnumerable<string> emojis;
        switch (category)
        {
            case "Recent":
                List<string> combined = new List<string>(EmojiHistoryManager.GetRecentEmojis());
                combined.AddRange(Smileys);
                combined.AddRange(Gestures);
                combined.AddRange(Nature);
                combined.AddRange(Activities);
                combined.AddRange(Objects);
                emojis = combined;
                break;
            case "Smileys":
                emojis = Smileys;
                break;
            case "Gestures":
                emojis = Gestures;
                break;
            case "Nature":
                emojis = Nature;
                break;
            case "Activities":
                emojis = Activities;
                break;
            case "Objects":
                emojis = Objects;
                break;
            default:
                Debug.LogWarning($"[EmojiKeyboardUI] Unknown category: {category}. Defaulting to Smileys.");
                emojis = Smileys;
                break;
        }

        foreach (string emoji in emojis)
        {
            GameObject btnObj = Instantiate(emojiButtonPrefab, content);
            Button btn = btnObj.GetComponent<Button>();
            
            // Try to find text in children (generic approach)
            TMP_Text tmpText = btnObj.GetComponentInChildren<TMP_Text>();
            if (tmpText != null)
            {
                tmpText.text = emoji;
            }
            else
            {
                Text legacyText = btnObj.GetComponentInChildren<Text>();
                if (legacyText != null)
                {
                    legacyText.text = emoji;
                }
            }

            if (btn != null)
            {
                string capturedEmoji = emoji;
                btn.onClick.AddListener(() => SelectEmoji(capturedEmoji));
            }
        }
    }

    private void SelectEmoji(string emoji)
    {
        OnEmojiSelected?.Invoke(emoji);
        EmojiHistoryManager.AddEmoji(emoji);
        Close();
    }

    /// <summary>
    /// Called by category buttons to switch content.
    /// </summary>
    /// <param name="category">Category name.</param>
    public void OnCategoryButtonClicked(string category)
    {
        SwitchCategory(category);
    }
}
