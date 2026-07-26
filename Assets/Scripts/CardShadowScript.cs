using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardShadowScript : MonoBehaviour
{
    [Header("Shadow Settings")]
    [SerializeField] private Shadow shadowComponent;
    [SerializeField] private float maxShadowDistance = 10f;
    
    [Header("Update Settings")]
    [SerializeField] private bool updateInRealTime = true;
    [SerializeField] private float updateFrequency = 0.05f;
    
    [Header("Jump Prevention")]
    [SerializeField] private float maxOffsetChange = 2f; // Maximum allowed offset change per frame
    [SerializeField] private bool preventJumps = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // Initial values stored at start
    private Vector2 initialShadowOffset;
    private float initialRotationZ;
    private Vector2 initialWorldShadowDirection;
    
    // Runtime values
    private Vector2 lastShadowOffset;
    private float lastUpdateTime;
    private float lastRotationZ; // Track previous rotation to detect jumps
    private Vector2 previousFrameOffset; // Track previous frame's offset

    void Start()
    {
        // Get shadow component if not assigned
        if (shadowComponent == null)
            shadowComponent = GetComponent<Shadow>();
        
        if (shadowComponent == null)
        {
            
            return;
        }
        
        // Store initial values
        StoreInitialValues();
        
        // Initialize previous frame offset
        previousFrameOffset = initialShadowOffset;
        
        // Initial shadow calculation
        UpdateShadow();
    }

    void Update()
    {
        if (!updateInRealTime || shadowComponent == null)
            return;
        
        if (Time.time - lastUpdateTime >= updateFrequency)
        {
            UpdateShadow();
            lastUpdateTime = Time.time;
        }
    }

    void StoreInitialValues()
    {
        // Store initial shadow offset from the Shadow component
        initialShadowOffset = shadowComponent.effectDistance;
        
        // Store initial rotation (normalized)
        initialRotationZ = NormalizeAngle(transform.eulerAngles.z);
        lastRotationZ = initialRotationZ;
        
        // Calculate initial world shadow direction
        initialWorldShadowDirection = LocalToWorldShadowDirection(initialShadowOffset, initialRotationZ);
        
        if (showDebugInfo)
        {
            
        }
    }

    void UpdateShadow()
    {
        if (shadowComponent == null)
            return;

        // Get current rotation and normalize it
        float currentRotationZ = NormalizeAngle(transform.eulerAngles.z);
        
        // Check for angle wrap-around and smooth it
        currentRotationZ = SmoothAngleTransition(lastRotationZ, currentRotationZ);
        lastRotationZ = currentRotationZ;
        
        // Calculate new shadow offset to maintain the same world position
        Vector2 newShadowOffset = WorldToLocalShadowDirection(initialWorldShadowDirection, currentRotationZ);
        
        // Clamp to maximum shadow distance
        if (newShadowOffset.magnitude > maxShadowDistance)
        {
            newShadowOffset = newShadowOffset.normalized * maxShadowDistance;
        }
        
        // Prevent extreme offset changes (jump detection)
        if (preventJumps)
        {
            newShadowOffset = PreventOffsetJumps(newShadowOffset);
        }
        
        // Apply the new shadow offset
        shadowComponent.effectDistance = newShadowOffset;
        lastShadowOffset = newShadowOffset;
        previousFrameOffset = newShadowOffset; // Update previous frame reference
        
        if (showDebugInfo && Mathf.Abs(currentRotationZ - initialRotationZ) > 1f)
        {
            
        }
    }

    // Prevent sudden offset jumps by limiting change per frame
    Vector2 PreventOffsetJumps(Vector2 targetOffset)
    {
        Vector2 offsetDifference = targetOffset - previousFrameOffset;
        float changeDistance = offsetDifference.magnitude;
        
        // If change is too large, limit it
        if (changeDistance > maxOffsetChange)
        {
            if (showDebugInfo)
            {
                
            }
            
            // Limit the change to maxOffsetChange in the same direction
            Vector2 limitedOffset = previousFrameOffset + (offsetDifference.normalized * maxOffsetChange);
            return limitedOffset;
        }
        
        return targetOffset;
    }

    // Normalize angle to -180 to 180 range to avoid 0/360 jumps
    float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    // Smooth angle transitions to prevent sudden jumps
    float SmoothAngleTransition(float previousAngle, float currentAngle)
    {
        float angleDifference = currentAngle - previousAngle;
        
        // If the angle difference is too large, it's likely a wrap-around
        if (angleDifference > 180f)
        {
            currentAngle -= 360f;
        }
        else if (angleDifference < -180f)
        {
            currentAngle += 360f;
        }
        
        return currentAngle;
    }

    Vector2 LocalToWorldShadowDirection(Vector2 localShadowOffset, float rotationZ)
    {
        // Convert local shadow offset to world direction using the given rotation
        float rotationRad = rotationZ * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rotationRad);
        float sin = Mathf.Sin(rotationRad);
        
        Vector2 worldDirection = new Vector2(
            localShadowOffset.x * cos - localShadowOffset.y * sin,
            localShadowOffset.x * sin + localShadowOffset.y * cos
        );
        
        return worldDirection;
    }

    Vector2 WorldToLocalShadowDirection(Vector2 worldShadowDirection, float rotationZ)
    {
        // Convert world shadow direction to local offset using inverse rotation
        float rotationRad = -rotationZ * Mathf.Deg2Rad; // Negative for inverse
        float cos = Mathf.Cos(rotationRad);
        float sin = Mathf.Sin(rotationRad);
        
        Vector2 localOffset = new Vector2(
            worldShadowDirection.x * cos - worldShadowDirection.y * sin,
            worldShadowDirection.x * sin + worldShadowDirection.y * cos
        );
        
        return localOffset;
    }

    // Method to force shadow update
    public void ForceUpdateShadow()
    {
        UpdateShadow();
    }
    
    // Method to reset initial values (useful if you change shadow manually)
    public void ResetInitialValues()
    {
        StoreInitialValues();
        previousFrameOffset = initialShadowOffset;
        UpdateShadow();
    }
    
    // Method to set new initial shadow offset
    public void SetInitialShadowOffset(Vector2 newOffset)
    {
        initialShadowOffset = newOffset;
        shadowComponent.effectDistance = newOffset;
        initialRotationZ = NormalizeAngle(transform.eulerAngles.z);
        lastRotationZ = initialRotationZ;
        initialWorldShadowDirection = LocalToWorldShadowDirection(initialShadowOffset, initialRotationZ);
        previousFrameOffset = newOffset;
        
        if (showDebugInfo)
        {
            
        }
    }
    
    // Method to get current world shadow direction
    public Vector2 GetWorldShadowDirection()
    {
        return initialWorldShadowDirection;
    }

    // Debug visualization in Scene view
    void OnDrawGizmosSelected()
    {
        if (shadowComponent == null) return;
        
        // Draw initial world shadow direction (red)
        Gizmos.color = Color.red;
        Vector3 worldShadowPos = transform.position + new Vector3(initialWorldShadowDirection.x * 3f, initialWorldShadowDirection.y * 3f, 0);
        Gizmos.DrawLine(transform.position, worldShadowPos);
        Gizmos.DrawWireSphere(worldShadowPos, 0.2f);
        
        // Draw current local shadow offset (blue)
        Gizmos.color = Color.blue;
        Vector3 localShadowPos = transform.position + new Vector3(lastShadowOffset.x, lastShadowOffset.y, 0);
        Gizmos.DrawLine(transform.position, localShadowPos);
        Gizmos.DrawWireSphere(localShadowPos, 0.15f);
        
        // Draw card rotation indicator (green)
        Gizmos.color = Color.green;
        Vector3 forward = transform.rotation * Vector3.forward;
        Gizmos.DrawRay(transform.position, forward * 2f);
        
        // Draw initial rotation indicator (yellow)
        Gizmos.color = Color.yellow;
        float initialRotRad = initialRotationZ * Mathf.Deg2Rad;
        Vector3 initialForward = new Vector3(Mathf.Sin(initialRotRad), Mathf.Cos(initialRotRad), 0);
        Gizmos.DrawRay(transform.position, initialForward * 1.5f);
    }
}