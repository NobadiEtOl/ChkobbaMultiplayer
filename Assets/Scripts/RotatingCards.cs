using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class RotatingCards : MonoBehaviour
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

    private Tween zRotationTween;

    void Start()
    {
        StartRotations();
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

        // Generate random peak X and Y values (will peak at 180° in the cycle)
        float peakX = Random.Range(-maxXRotation, maxXRotation);
        float peakY = Random.Range(-maxYRotation, maxYRotation);

        // Kill existing rotation
        zRotationTween?.Kill();

        // Create a sequence for the complete rotation cycle
        Sequence rotationSequence = DOTween.Sequence();

        // Current starting rotation
        Vector3 startRotation = transform.eulerAngles;

        // Calculate target rotations for the cycle
        Vector3 halfwayRotation = new Vector3(
            startRotation.x + peakX,
            startRotation.y + peakY,
            startRotation.z + (180f * rotationDirection)
        );

        Vector3 endRotation = new Vector3(
            startRotation.x, // Back to starting X
            startRotation.y, // Back to starting Y
            startRotation.z + (360f * rotationDirection) // Full Z rotation
        );

        // First half: go to peak X,Y and 180° Z
        rotationSequence.Append(transform.DORotate(halfwayRotation, randomDuration * 0.5f).SetEase(Ease.Linear))
                        // Second half: return X,Y to start and complete 360° Z
                        .Append(transform.DORotate(endRotation, randomDuration * 0.5f).SetEase(Ease.Linear));

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
        DOTween.Kill(transform);
    }

    void OnDisable()
    {
        // Pause all tweens
        zRotationTween?.Pause();
    }

    void OnEnable()
    {
        // Resume all tweens
        zRotationTween?.Play();
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
}