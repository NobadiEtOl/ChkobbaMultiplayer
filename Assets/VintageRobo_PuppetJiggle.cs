using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simulates the characteristic jiggle and wobble of a puppet on strings.
/// Adds random movement in all directions for realistic puppet motion.
/// </summary>
public class VintageRobo_PuppetJiggle : MonoBehaviour
{
    [Header("Jiggle Settings")]
    [Tooltip("Enable/disable jiggle effect")]
    public bool enableJiggle = true;
    
    [Header("Position Jiggle")]
    [Tooltip("Amount of position jiggle")]
    [Range(0f, 0.5f)]
    public float jiggleAmount = 0.02f;
    
    [Tooltip("Speed of jiggle movement")]
    [Range(0f, 10f)]
    public float jiggleSpeed = 3f;
    
    [Tooltip("Scale jiggle per axis (X, Y, Z)")]
    public Vector3 jiggleScale = new Vector3(1f, 1f, 1f);
    
    [Header("Rotation Jiggle")]
    [Tooltip("Enable rotation wobble")]
    public bool enableRotationJiggle = true;
    
    [Tooltip("Amount of rotation wobble in degrees")]
    [Range(0f, 45f)]
    public float rotationJiggleAmount = 5f;
    
    [Tooltip("Speed of rotation wobble")]
    [Range(0f, 10f)]
    public float rotationJiggleSpeed = 2f;
    
    [Header("Advanced")]
    [Tooltip("Use Perlin noise for smooth jiggle (recommended)")]
    public bool useSmoothNoise = true;
    
    [Tooltip("Random frequency variation (makes movement less predictable)")]
    [Range(0f, 2f)]
    public float frequencyVariation = 0.5f;
    
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 noiseOffset; // Random offset for Perlin noise to make each object unique
    private float timeOffset; // Random time offset for variation

    void Start()
    {
        // Store original transform
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
        
        // Generate random offsets so each puppet jiggles differently
        noiseOffset = new Vector3(
            Random.Range(0f, 100f),
            Random.Range(0f, 100f),
            Random.Range(0f, 100f)
        );
        
        timeOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (!enableJiggle) return;
        
        // Apply position jiggle
        ApplyPositionJiggle();
        
        // Apply rotation jiggle
        if (enableRotationJiggle)
        {
            ApplyRotationJiggle();
        }
    }
    
    private void ApplyPositionJiggle()
    {
        float time = Time.time * jiggleSpeed + timeOffset;
        Vector3 jiggle = Vector3.zero;
        
        if (useSmoothNoise)
        {
            // Smooth Perlin noise for realistic puppet sway
            jiggle.x = (Mathf.PerlinNoise(time + noiseOffset.x, 0f) * 2f - 1f) * jiggleAmount * jiggleScale.x;
            jiggle.y = (Mathf.PerlinNoise(time + noiseOffset.y, 10f) * 2f - 1f) * jiggleAmount * jiggleScale.y;
            jiggle.z = (Mathf.PerlinNoise(time + noiseOffset.z, 20f) * 2f - 1f) * jiggleAmount * jiggleScale.z;
            
            // Add some frequency variation for less predictable movement
            if (frequencyVariation > 0f)
            {
                float fastTime = time * (1f + frequencyVariation);
                jiggle.x += (Mathf.PerlinNoise(fastTime + noiseOffset.x + 50f, 5f) * 2f - 1f) * jiggleAmount * 0.3f * jiggleScale.x;
                jiggle.y += (Mathf.PerlinNoise(fastTime + noiseOffset.y + 50f, 15f) * 2f - 1f) * jiggleAmount * 0.3f * jiggleScale.y;
                jiggle.z += (Mathf.PerlinNoise(fastTime + noiseOffset.z + 50f, 25f) * 2f - 1f) * jiggleAmount * 0.3f * jiggleScale.z;
            }
        }
        else
        {
            // Simple sine wave jiggle (more predictable but cheaper)
            jiggle.x = Mathf.Sin(time + noiseOffset.x) * jiggleAmount * jiggleScale.x;
            jiggle.y = Mathf.Sin(time * 0.8f + noiseOffset.y) * jiggleAmount * jiggleScale.y;
            jiggle.z = Mathf.Sin(time * 1.2f + noiseOffset.z) * jiggleAmount * jiggleScale.z;
        }
        
        transform.localPosition = originalPosition + jiggle;
    }
    
    private void ApplyRotationJiggle()
    {
        float time = Time.time * rotationJiggleSpeed + timeOffset;
        Vector3 rotationJiggle = Vector3.zero;
        
        if (useSmoothNoise)
        {
            // Smooth rotation wobble
            rotationJiggle.x = (Mathf.PerlinNoise(time + noiseOffset.x + 30f, 0f) * 2f - 1f) * rotationJiggleAmount;
            rotationJiggle.y = (Mathf.PerlinNoise(time + noiseOffset.y + 30f, 10f) * 2f - 1f) * rotationJiggleAmount;
            rotationJiggle.z = (Mathf.PerlinNoise(time + noiseOffset.z + 30f, 20f) * 2f - 1f) * rotationJiggleAmount;
        }
        else
        {
            // Simple sine wave rotation
            rotationJiggle.x = Mathf.Sin(time + noiseOffset.x + 30f) * rotationJiggleAmount;
            rotationJiggle.y = Mathf.Sin(time * 0.7f + noiseOffset.y + 30f) * rotationJiggleAmount;
            rotationJiggle.z = Mathf.Sin(time * 1.3f + noiseOffset.z + 30f) * rotationJiggleAmount;
        }
        
        Quaternion jiggleRotation = Quaternion.Euler(rotationJiggle);
        transform.localRotation = originalRotation * jiggleRotation;
    }
    
    /// <summary>
    /// Reset to original position and rotation
    /// </summary>
    [ContextMenu("Reset to Original")]
    public void ResetToOriginal()
    {
        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;
    }
    
    /// <summary>
    /// Set current position/rotation as new original
    /// </summary>
    [ContextMenu("Set Current as Original")]
    public void SetCurrentAsOriginal()
    {
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
    }
}
