using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SuperPowerToken : MonoBehaviour
{
    public static SuperPowerToken ActiveInstance { get; set; }
    public SuperPower power; // Assign this in the Inspector
    public string superPowerClassName;

    void Awake()
    {

    }

    [ContextMenu("Activate Power")]
    public void OnTokenClicked()
    {
        Debug.Log("SuperPowerToken clicked: " + power.name);
        
        // Check if it's the player's turn before activating
        if (GameManager.LocalInstance != null && !GameManager.LocalInstance.IsLocalPlayerTurn())
        {
            Debug.LogWarning($"Cannot activate {power.name} - it's not your turn!");
            ShowOutOfTurnFeedback();
            return;
        }
        
        power.ActivatePower();
        
        SuperPowerSpawner.LocalInstance.RemoveSpawnedSuperPower(gameObject);
        SuperPowerSpawner.LocalInstance.UpdateTokenPositions();
        
        // FIXED: Start the close coroutine instead of calling CloseInfoBox directly
        StartCoroutine(SuperPowerSpawner.LocalInstance.CloseInfoBox());
        StartCoroutine(FadeOutSprite()); // Destroy the token after activation
    }
    
    /// <summary>
    /// Show visual feedback when player tries to activate power out of turn
    /// </summary>
    private void ShowOutOfTurnFeedback()
    {
        // Flash the token red briefly to indicate it's not their turn
        StartCoroutine(FlashRedFeedback());
        
        // Show error message in InfoBox if it's open
        if (SuperPowerSpawner.LocalInstance != null && SuperPowerSpawner.LocalInstance.isInfoBoxOpen)
        {
            SuperPowerSpawner.LocalInstance.ShowOutOfTurnErrorMessage();
        }
    }
    
    /// <summary>
    /// Flash the token red to indicate invalid action
    /// </summary>
    private IEnumerator FlashRedFeedback()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        Color originalColor = sr.color;
        Color redColor = Color.red;
        
        // Flash red 3 times
        for (int i = 0; i < 3; i++)
        {
            sr.color = redColor;
            yield return new WaitForSeconds(0.1f);
            sr.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }
    }

    public IEnumerator FadeOutSprite(float duration = 0.5f)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        Color startColor = sr.color;
        startColor.a = 1f;
        Color endColor = sr.color;
        endColor.a = 0f;

        float elapsed = 0f;
        sr.color = startColor;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            sr.color = Color.Lerp(startColor, endColor, elapsed / duration);
            yield return null;
        }
        sr.color = endColor;

        yield return new WaitForSeconds(duration); // Wait before destroying

        Destroy(gameObject); // Destroy the token after fading out
    }

    void Start()
    {
        if (power == null)
        {
            Debug.LogError("SuperPowerToken: Power is not assigned for " + gameObject.name);
        }

        // Fade in the token sprite
        StartCoroutine(FadeInSprite());

        // Play animation on the only child
        if (transform.childCount > 0)
        {

            GameObject smokeEffectChild = transform.GetChild(0).gameObject;
            smokeEffectChild.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(0,360)); // Reset rotation if needed
            Animator animator = smokeEffectChild.GetComponent<Animator>();

            if (animator != null)
            {
                animator.Play("SmokeEffectAnimationClip"); // Use your animation state name here
            }
            else
            {
                Debug.LogWarning("Animator not found on child object.");
            }
        }
        else
        {
            Debug.LogWarning("No child object found to play animation.");
        }
    }
    private IEnumerator FadeInSprite(float duration = 0.5f)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        Color startColor = sr.color;
        startColor.a = 0f;
        Color endColor = sr.color;
        endColor.a = 1f;

        float elapsed = 0f;
        sr.color = startColor;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            sr.color = Color.Lerp(startColor, endColor, elapsed / duration);
            yield return null;
        }
        sr.color = endColor;
    }
}