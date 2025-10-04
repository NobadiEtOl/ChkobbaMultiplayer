using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SuperPowerToken : MonoBehaviour
{
    public static SuperPowerToken ActiveInstance { get; set; }
    public SuperPower power; // Assign this in the Inspector
    public string superPowerClassName;

    private float activationDistanceThreshold = 1000f; // Distance to trigger activation

    private Vector3 originalPosition;
    private bool isDragging = false;
    private Vector3 offset;

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
        
        // Set active instance for peek power detection
        SuperPowerToken.ActiveInstance = this;
        
        power.ActivatePower();
        
        SuperPowerSpawner.LocalInstance.RemoveSpawnedSuperPower(gameObject);
        SuperPowerSpawner.LocalInstance.UpdateTokenPositions();
        
        // CRITICAL FIX: For peek powers, delay InfoBox closure to allow animation to complete
        if (superPowerClassName == "UcundanGözAt" || superPowerClassName == "BayaBayaBak")
        {
            Debug.Log($"[SuperPowerToken] Peek power detected ({superPowerClassName}) - delaying InfoBox closure");
            StartCoroutine(DelayedCloseInfoBoxForPeekPower());
        }
        else
        {
            // FIXED: Start the close coroutine instead of calling CloseInfoBox directly
            StartCoroutine(SuperPowerSpawner.LocalInstance.CloseInfoBox());
        }
        
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
    /// Delayed InfoBox closure for peek powers to allow animations to complete
    /// </summary>
    private IEnumerator DelayedCloseInfoBoxForPeekPower()
    {
        Debug.Log($"[SuperPowerToken] Starting delayed closure for peek power: {superPowerClassName}");
        
        // Wait for peek animation to complete
        // Ucundan Göz At: ~2.0 seconds (0.4 move + 1.2 pause + 0.4 move)
        // Baya Baya Bak: ~3.8 seconds (0.4 move + 3.0 pause + 0.4 move)
        // Add extra buffer time to ensure animation completes
        float delayTime = superPowerClassName == "UcundanGözAt" ? 3.0f : 5.0f;
        
        Debug.Log($"[SuperPowerToken] Waiting {delayTime} seconds before closing InfoBox");
        yield return new WaitForSeconds(delayTime);
        
        // Clear the active instance before closing
        SuperPowerToken.ActiveInstance = null;
        
        Debug.Log($"[SuperPowerToken] Delayed closure complete - closing InfoBox now");
        StartCoroutine(SuperPowerSpawner.LocalInstance.CloseInfoBox());
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

        // Ensure a Collider2D is present for drag detection
        if (GetComponent<Collider2D>() == null)
        {
            gameObject.AddComponent<BoxCollider2D>();
        }

        // Store the original position for snap-back
        originalPosition = transform.position;

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

    void OnMouseDown()
    {
        isDragging = true;
        originalPosition = transform.position; // Update original position on drag start
        offset = transform.position - GetMouseWorldPosition();
        // Open InfoBox for this token when dragging starts using queue system
        if (SuperPowerSpawner.LocalInstance != null)
        {
            SuperPowerSpawner.LocalInstance.EnqueueOpenInfoBox(this);
        }

        if(SuperPowerSpawner.LocalInstance != null && SuperPowerSpawner.LocalInstance.restirictedPowersName_CardNeedToBeSelected.Contains(power.name))
        {
            DeckController.LocalInstance.ShowcaseAllOtherHands();
        }
    }

    void OnMouseDrag()
    {
        if (isDragging)
        {
            transform.position = GetMouseWorldPosition() + offset;
        }
    }

    void OnMouseUp()
    {
        isDragging = false;
        float distance = Vector3.Distance(transform.position, originalPosition);
        // Always close InfoBox when dragging ends using queue system
        if (SuperPowerSpawner.LocalInstance != null)
        {
            SuperPowerSpawner.LocalInstance.EnqueueCloseInfoBox();
        }

        // Detect cards directly beneath the token using a box
        Vector3 boxCenter = transform.position + Vector3.down * 250f; // Move box down from token
        Vector3 boxHalfExtents = new Vector3(250f, 10f, 250f); // Wide and shallow box
        Quaternion boxOrientation = Quaternion.identity;
        Collider[] colliders = Physics.OverlapBox(boxCenter, boxHalfExtents, boxOrientation);
        CardInteraction detectedCard = null;
        foreach (var collider in colliders)
        {
            CardInteraction card = collider.GetComponent<CardInteraction>();
            if (card != null)
            {
                Debug.Log($"[Card] {card.gameObject.name}");
                detectedCard = card;
                break; // Only select the first detected card
            }
        }


        // If a card is detected, set it as the currently selected card
        if (detectedCard != null)
        {
            CardInteraction.currentlySelectedCard = detectedCard;
            Debug.Log($"[SuperPowerToken] Set currentlySelectedCard to {detectedCard.gameObject.name}");

            // Special logic for Bu Daha İyi: set GameManager.currentSelectedHandCard to this card's unique ID
            if (power != null && power.name == "Bu Daha İyi")
            {
                if (GameManager.LocalInstance != null)
                {
                    GameManager.LocalInstance.currentSelectedHandCard = detectedCard.uniqueCardInstanceID;
                    Debug.Log($"[SuperPowerToken] Bu Daha İyi: Set currentSelectedHandCard to {detectedCard.uniqueCardInstanceID}");
                }
            }
        }

        if (power.name == "Şunu Değiş Bunu Tokuş")
        {
            DeckController.LocalInstance.ShowcaseAllOtherHands();
        }

        if (power == null || SuperPowerSpawner.LocalInstance.restirictedPowersName_WaitForSwap.Contains(power.name) == false)
        {
            Debug.Log($"[SuperPowerToken] Exiting showcase for other hands - power: {(power != null ? power.name : "null")}");
            DeckController.LocalInstance.ExitShowcaseAllOtherHands();
        }

        if (distance >= activationDistanceThreshold)
        {
            // Activate power if moved far enough
            OnTokenClicked();
        }
        else
        {
            // Snap back to original position
            transform.position = originalPosition;
            DeckController.LocalInstance.ExitShowcaseAllOtherHands();
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Camera.main.WorldToScreenPoint(transform.position).z;
        return Camera.main.ScreenToWorldPoint(mouseScreenPos);
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

    void OnDrawGizmosSelected()
    {
    // Draw detection box for card detection in blue
    Vector3 boxCenter = transform.position + Vector3.down * 250f;
    Vector3 boxSize = new Vector3(500f, 20f, 500f); // Full size (2x half extents)
    Gizmos.color = Color.blue;
    Gizmos.DrawWireCube(boxCenter, boxSize);
    }
}