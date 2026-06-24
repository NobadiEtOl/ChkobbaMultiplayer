using UnityEngine;
using TMPro;
using System.Collections;

public class UIFeedbackManager : MonoBehaviour
{
    public static UIFeedbackManager Instance { get; private set; }

    [SerializeField] private CanvasGroup feedbackCanvasGroup;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float displayDuration = 1.2f;

    private Coroutine currentCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Usually we want this on a UI Canvas in the scene, so no DontDestroyOnLoad needed if it's scene-specific
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (feedbackCanvasGroup != null)
        {
            feedbackCanvasGroup.alpha = 0;
            feedbackCanvasGroup.gameObject.SetActive(false);
        }
    }

    public void ShowFeedback(string message)
    {
        if (feedbackCanvasGroup == null || feedbackText == null)
        {
            Debug.LogWarning("[UIFeedbackManager] References not set.");
            return;
        }

        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(FeedbackSequence(message));
    }

    private IEnumerator FeedbackSequence(string message)
    {
        feedbackText.text = message;
        feedbackCanvasGroup.gameObject.SetActive(true);
        
        // Fade in
        float elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            feedbackCanvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / fadeDuration);
            yield return null;
        }
        feedbackCanvasGroup.alpha = 1;

        yield return new WaitForSeconds(displayDuration);

        // Fade out
        elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            feedbackCanvasGroup.alpha = Mathf.Lerp(1, 0, elapsed / fadeDuration);
            yield return null;
        }
        feedbackCanvasGroup.alpha = 0;
        feedbackCanvasGroup.gameObject.SetActive(false);
        currentCoroutine = null;
    }
}
