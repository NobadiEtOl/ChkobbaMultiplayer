using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;

#if TMPRO_PRESENT || true // Direct reference to TMPro since it is present in the project
using TMPro;
#endif

public class ButtonTextCopier : MonoBehaviour
{
    [Header("Copy Settings")]
    [Tooltip("The TextMeshPro component containing the string to copy.")]
    [SerializeField] private TMP_Text targetTextTMP;

    [Tooltip("Alternative legacy Text component containing the string to copy.")]
    [SerializeField] private Text targetTextLegacy;

    #if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void CopyToClipboard(string text);
    #endif

    /// <summary>
    /// Copy the text of the assigned text component to the system clipboard.
    /// This is designed to be called from a Button's onClick event.
    /// </summary>
    public void CopyText()
    {
        string textToCopy = "";

        if (targetTextTMP != null)
        {
            textToCopy = targetTextTMP.text;
        }
        else if (targetTextLegacy != null)
        {
            textToCopy = targetTextLegacy.text;
        }

        if (string.IsNullOrEmpty(textToCopy))
        {
            Debug.LogWarning($"[ButtonTextCopier] No text component assigned or the text is empty on GameObject: {gameObject.name}");
            return;
        }

        CopyStringToClipboard(textToCopy);

        // Show feedback message
        if (UIFeedbackManager.Instance != null)
        {
            UIFeedbackManager.Instance.ShowFeedback("Kopyalandı");
        }
    }

    /// <summary>
    /// Copies a given string to the system clipboard across PC, Mobile, and WebGL.
    /// </summary>
    public static void CopyStringToClipboard(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        #if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            CopyToClipboard(text);
            Debug.Log($"[Clipboard] WebGL copying: '{text}' requested");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[Clipboard] WebGL JSLIB failed, falling back to systemCopyBuffer. Error: {ex.Message}");
            GUIUtility.systemCopyBuffer = text;
        }
        #else
        // Works out-of-the-box on PC (Windows/macOS/Linux) and Mobile (Android/iOS)
        GUIUtility.systemCopyBuffer = text;
        Debug.Log($"[Clipboard] Copied via GUIUtility.systemCopyBuffer: {text}");
        #endif
    }
}
