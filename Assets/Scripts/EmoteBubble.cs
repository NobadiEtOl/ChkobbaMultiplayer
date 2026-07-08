using UnityEngine;
using TMPro;
using System.Collections;

public class EmoteBubble : MonoBehaviour
{
    [SerializeField] private TMP_Text emoteText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float displayDuration = 1.5f;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float moveDistance = 100f;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void SetEmote(string text)
    {
        if (emoteText != null)
        {
            emoteText.text = text;
        }
        Debug.Log($"[EmoteBubble] Starting animation for '{text}'.");
        StartCoroutine(EmoteSequence());
    }

    private IEnumerator EmoteSequence()
    {
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 endPos = startPos + Vector2.up * moveDistance;
        float totalDuration = fadeDuration * 2 + displayDuration;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            // Fade In & Start Moving
            float elapsed = 0;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                canvasGroup.alpha = Mathf.Lerp(0, 1, t);
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, Vector2.Lerp(startPos, endPos, elapsed / totalDuration), t);
                yield return null;
            }
            canvasGroup.alpha = 1;
        }

        // Display & Continue Moving
        float displayElapsed = 0;
        float startDisplayPosFactor = fadeDuration / totalDuration;
        float endDisplayPosFactor = (fadeDuration + displayDuration) / totalDuration;
        
        while (displayElapsed < displayDuration)
        {
            displayElapsed += Time.deltaTime;
            float t = displayElapsed / displayDuration;
            float posT = Mathf.Lerp(startDisplayPosFactor, endDisplayPosFactor, t);
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, posT);
            yield return null;
        }

        if (canvasGroup != null)
        {
            // Fade Out & Finish Moving
            float fadeOutElapsed = 0;
            while (fadeOutElapsed < fadeDuration)
            {
                fadeOutElapsed += Time.deltaTime;
                float t = fadeOutElapsed / fadeDuration;
                canvasGroup.alpha = Mathf.Lerp(1, 0, t);
                float posT = Mathf.Lerp(endDisplayPosFactor, 1f, t);
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, posT);
                yield return null;
            }
            canvasGroup.alpha = 0;
        }

        Debug.Log("[EmoteBubble] Animation complete. Destroying object.");
        Destroy(gameObject);
    }
}
