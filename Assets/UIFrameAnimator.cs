using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIFrameAnimator : MonoBehaviour
{
    [Header("Idle Animation")]
    public Sprite[] animationFrames;
    public float frameRate = 10f;

    [Header("Page Change Animation")]
    public Sprite[] pageChangeFrames;
    public float pageChangeFrameRate = 15f;

    private SpriteRenderer spriteRenderer;
    private int currentFrame;
    private float timer;
    private bool isPlayingPageChange = false;
    private bool isIdleAnimationEnabled = true;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentFrame = 0;
        timer = 0f;
    }

    void Update()
    {
        // Don't play idle animation if page change is playing or if it's disabled
        if (isPlayingPageChange || !isIdleAnimationEnabled) return;
        
        // Play idle animation
        PlayIdleAnimation();
    }

    private void PlayIdleAnimation()
    {
        if (animationFrames.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= 1f / frameRate)
        {
            currentFrame = (currentFrame + 1) % animationFrames.Length;
            spriteRenderer.sprite = animationFrames[currentFrame];
            timer = 0f;
        }
    }

    [ContextMenu("Play Page Change Animation")]
    public void PlayPageChangeAnimation()
    {
        if (pageChangeFrames.Length == 0)
        {
            Debug.LogWarning("No page change frames assigned!");
            return;
        }

        StartCoroutine(PlayPageChangeCoroutine());
    }

    private IEnumerator PlayPageChangeCoroutine()
    {
        // Stop idle animation
        isPlayingPageChange = true;
        
        // Play page change animation
        for (int i = 0; i < pageChangeFrames.Length; i++)
        {
            spriteRenderer.sprite = pageChangeFrames[i];
            yield return new WaitForSeconds(1f / pageChangeFrameRate);
        }

        // Animation finished, resume idle animation
        isPlayingPageChange = false;
        currentFrame = 0; // Reset idle animation to start
        timer = 0f;
    }

    // Public methods for external control
    public void SetIdleAnimationEnabled(bool enabled)
    {
        isIdleAnimationEnabled = enabled;
        if (!enabled && !isPlayingPageChange)
        {
            // If disabling idle and no page change is playing, show first idle frame
            if (animationFrames.Length > 0)
            {
                spriteRenderer.sprite = animationFrames[0];
            }
        }
    }

    public bool IsPlayingPageChange()
    {
        return isPlayingPageChange;
    }

    public void StopAllAnimations()
    {
        StopAllCoroutines();
        isPlayingPageChange = false;
        isIdleAnimationEnabled = false;
    }

    public void ResumeIdleAnimation()
    {
        if (!isPlayingPageChange)
        {
            isIdleAnimationEnabled = true;
            currentFrame = 0;
            timer = 0f;
        }
    }

    // Method to play page change animation from code
    public void TriggerPageChange()
    {
        PlayPageChangeAnimation();
    }
}