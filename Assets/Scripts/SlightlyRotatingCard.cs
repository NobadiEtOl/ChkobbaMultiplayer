using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SlightlyRotatingCards : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationDuration = 2f;
    [SerializeField] private float minRotationSpeed = 0.5f;
    [SerializeField] private float maxRotationSpeed = 3f;
    [SerializeField] private bool randomizeDirection = true;
    [SerializeField] private bool continuousRotation = true;
    
    [Header("Minor Rotation Limits")]
    [SerializeField] private float maxXRotation = 5f;
    [SerializeField] private float maxYRotation = 5f;
    [SerializeField] private float maxZRotation = 5f; // Added Z rotation limit

    [Header("Animation Settings")]
    [SerializeField] private Transform centerTransform;
    [SerializeField] private float animationDuration = 1f;
    [SerializeField] private Ease animationEase = Ease.OutBack;
    [SerializeField] private float animationDelay = 0f;
    
    [Header("Simultaneous Animation")]
    [SerializeField] private bool startRotationWithAnimation = true; // New option to control timing

    private Tween zRotationTween;
    private Tween positionTween;
    private Tween scaleTween;

    // Store initial values
    private Vector3 targetPosition;
    private Vector3 targetScale;
    private Quaternion initialRotation;

    void Start()
    {
        // Store the target values (current position and scale)
        targetPosition = transform.position;
        targetScale = transform.localScale;
        initialRotation = transform.rotation; // Preserve initial rotation

        // Set initial state (at center with 0 scale)
        if (centerTransform != null)
        {
            transform.position = centerTransform.position;
        }
        transform.localScale = Vector3.zero;
        
        // Start the entrance animation
        StartEntranceAnimation();
    }

    void StartEntranceAnimation()
    {
        // Kill any existing tweens
        positionTween?.Kill();
        scaleTween?.Kill();

        // Create position animation
        if (centerTransform != null)
        {
            positionTween = transform.DOMove(targetPosition, animationDuration)
                .SetEase(animationEase)
                .SetDelay(animationDelay);
        }

        // Create scale animation
        scaleTween = transform.DOScale(targetScale, animationDuration)
            .SetEase(animationEase)
            .SetDelay(animationDelay);

        // Start rotations based on the setting
        if (startRotationWithAnimation)
        {
            // Start rotations immediately (simultaneous with movement/scale)
            StartRotations();
        }
        else
        {
            // Start rotations after animation completes (original behavior)
            scaleTween.OnComplete(() => {
                StartRotations();
            });
        }

        // Preserve initial rotation (will be overridden by rotation animation if simultaneous)
        if (!startRotationWithAnimation)
        {
            transform.rotation = initialRotation;
        }
    }

    void StartRotations()
    {
        StartZRotation();
    }

    void StartZRotation()
    {
        // Generate random duration for rotation cycle
        float randomDuration = Random.Range(minRotationSpeed, maxRotationSpeed);

        // Determine rotation direction for Z axis
        float rotationDirection = randomizeDirection ? (Random.Range(0f, 1f) > 0.5f ? 1f : -1f) : 1f;

        // Generate random peak X, Y, and Z values
        float peakX = Random.Range(-maxXRotation, maxXRotation);
        float peakY = Random.Range(-maxYRotation, maxYRotation);
        float peakZ = Random.Range(-maxZRotation, maxZRotation); // Random Z rotation within limits

        // Kill existing rotation
        zRotationTween?.Kill();

        // Create a sequence for the complete rotation cycle
        Sequence rotationSequence = DOTween.Sequence();

        // Use initial rotation as starting point if simultaneous, otherwise use current
        Vector3 startRotation = startRotationWithAnimation ? initialRotation.eulerAngles : transform.eulerAngles;

        // Calculate target rotations for the cycle - now using limited Z rotation
        Vector3 halfwayRotation = new Vector3(
            startRotation.x + peakX,
            startRotation.y + peakY,
            startRotation.z + peakZ // Use limited Z rotation instead of 180°
        );

        Vector3 endRotation = new Vector3(
            startRotation.x, // Back to starting X
            startRotation.y, // Back to starting Y
            startRotation.z  // Back to starting Z rotation
        );

        // If simultaneous, add delay to rotation to match entrance animation
        float rotationDelay = startRotationWithAnimation ? animationDelay : 0f;

        // First half: go to peak X, Y, and Z
        rotationSequence.Append(transform.DORotate(halfwayRotation, randomDuration * 0.5f)
                               .SetEase(Ease.InOutSine) // Changed to smoother easing
                               .SetDelay(rotationDelay))
                        // Second half: return all axes to starting rotation
                        .Append(transform.DORotate(endRotation, randomDuration * 0.5f)
                               .SetEase(Ease.InOutSine)); // Changed to smoother easing

        // Set looping behavior
        if (continuousRotation)
        {
            rotationSequence.SetLoops(-1)
                           .OnComplete(() => {
                               // When sequence completes, restart with new random values
                               StartZRotation();
                           });
        }
        else
        {
            rotationSequence.OnComplete(() => StartZRotation());
        }

        zRotationTween = rotationSequence;
    }

    void OnDestroy()
    {
        // Clean up all tweens
        zRotationTween?.Kill();
        positionTween?.Kill();
        scaleTween?.Kill();
        DOTween.Kill(transform);
    }

    void OnDisable()
    {
        // Pause all tweens
        zRotationTween?.Pause();
        positionTween?.Pause();
        scaleTween?.Pause();
    }

    void OnEnable()
    {
        // Resume all tweens
        zRotationTween?.Play();
        positionTween?.Play();
        scaleTween?.Play();
    }

    // Method to stop all rotations
    public void StopRotation()
    {
        zRotationTween?.Kill();
    }

    // Method to restart all rotations
    public void RestartRotations()
    {
        StopRotation();
        StartRotations();
    }

    // Method to restart entire animation (useful for testing)
    public void RestartAnimation()
    {
        // Kill all tweens
        zRotationTween?.Kill();
        positionTween?.Kill();
        scaleTween?.Kill();

        // Reset to initial state
        if (centerTransform != null)
        {
            transform.position = centerTransform.position;
        }
        transform.localScale = Vector3.zero;
        transform.rotation = initialRotation;

        // Restart animation
        StartEntranceAnimation();
    }

    // Method to skip animation and go directly to final state
    public void SkipToFinalState()
    {
        // Kill all tweens
        positionTween?.Kill();
        scaleTween?.Kill();
        zRotationTween?.Kill();

        // Set final values
        transform.position = targetPosition;
        transform.localScale = targetScale;
        transform.rotation = initialRotation;

        // Start rotations immediately
        StartRotations();
    }

    // Method to toggle simultaneous rotation
    public void SetSimultaneousRotation(bool simultaneous)
    {
        startRotationWithAnimation = simultaneous;
    }

    // Method to set rotation limits at runtime
    public void SetRotationLimits(float maxX, float maxY, float maxZ)
    {
        maxXRotation = maxX;
        maxYRotation = maxY;
        maxZRotation = maxZ;
    }
}