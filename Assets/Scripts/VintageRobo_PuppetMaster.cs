using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes the puppet master robot's fingers (reach points) follow the puppet's bone movements.
/// Positions the master's hands to follow the average finger positions.
/// Arm IK is handled by Unity's Animation Rigging package (TwoBoneIK constraints).
/// </summary>
public class VintageRobo_PuppetMaster : MonoBehaviour
{
    [System.Serializable]
    public class HandSetup
    {
        [Header("Hand Configuration")]
        [Tooltip("Hand bone (palm) - IK will control this, don't assign if you want script to move it")]
        public Transform hand;
        
        [Tooltip("IK Target transform - this will follow the finger average, IK will make hand follow this")]
        public Transform ikTarget;
        
        [Tooltip("List of reach point bones belonging to this hand")]
        public List<Transform> reachPoints = new List<Transform>();
        
        [Tooltip("Offset from average reach point position to target position")]
        public Vector3 handOffset = new Vector3(0, -0.1f, 0);
        
        [Range(0f, 1f)]
        [Tooltip("How much the target follows the finger average (1 = full follow)")]
        public float followInfluence = 1f;
        
        [Range(0f, 20f)]
        [Tooltip("Speed of target movement")]
        public float smoothSpeed = 10f;
        
        [Header("Hand-Finger Connection Lines")]
        [Tooltip("Show visual connections between hand and fingers")]
        public bool showConnections = true;
        
        [Tooltip("Connection base point (where lines start from). If null, uses hand position")]
        public Transform connectionBasePoint;
        
        [Tooltip("Number of parallel lines per finger connection")]
        [Range(1, 5)]
        public int linesPerConnection = 1;
        
        [Tooltip("Width of connection lines")]
        public float connectionWidth = 0.01f;
        
        [Tooltip("Color of connection lines")]
        public Color connectionColor = new Color(0.3f, 0.8f, 1f, 0.8f); // Cyan/blue glow
        
        [Header("Lightning Effect")]
        [Tooltip("Enable lightning zigzag effect")]
        public bool useLightningEffect = true;
        
        [Tooltip("Number of segments in lightning bolt (more = more detail)")]
        [Range(2, 20)]
        public int lightningSegments = 6;
        
        [Tooltip("Maximum displacement of lightning segments")]
        [Range(0f, 0.5f)]
        public float lightningDisplacement = 0.05f;
        
        [Tooltip("Speed of lightning animation (0 = static)")]
        [Range(0f, 10f)]
        public float lightningSpeed = 2f;
        
        [HideInInspector] public Vector3 originalTargetPosition;
        [HideInInspector] public List<LineRenderer> connectionLines = new List<LineRenderer>();
        [HideInInspector] public List<float> lightningOffsets = new List<float>(); // Random offset for each line's animation
    }

    [Header("String Controller Reference")]
    [SerializeField] private VintageRobo_PuppetStrings puppetStrings;

    [Header("Hand Setup")]
    [SerializeField] private List<HandSetup> hands = new List<HandSetup>();
    
    [Header("Connection Line Settings")]
    [SerializeField] private Material connectionMaterial;
    [Tooltip("If true, creates connection lines in Start()")]
    [SerializeField] private bool autoCreateConnections = true;

    [Header("Finger Follow Settings")]
    [Range(0f, 1f)]
    [Tooltip("How much master fingers follow puppet bones horizontally (1 = perfect follow)")]
    [SerializeField] private float positionInfluence = 0.8f;

    [Range(0f, 1f)]
    [Tooltip("How much master fingers copy puppet bone rotation")]
    [SerializeField] private float rotationInfluence = 0f;

    [Range(0.1f, 20f)]
    [Tooltip("Speed of smooth following (higher = faster response)")]
    [SerializeField] private float smoothSpeed = 10f;

    [Tooltip("Maintain initial distance between master and puppet (keeps string length constant)")]
    [SerializeField] private bool maintainStringLength = true;

    [Tooltip("Keep master finger bones at their original rotation (recommended for proper finger posing)")]
    [SerializeField] private bool preserveOriginalRotation = true;

    [Tooltip("Lock fingers directly above puppet bones on Y axis (keeps vertical alignment)")]
    [SerializeField] private bool lockVerticalAlignment = true;

    [Header("Control")]
    [SerializeField] private bool enableControl = true;

    private Dictionary<Transform, Vector3> originalPositions = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, Quaternion> originalRotations = new Dictionary<Transform, Quaternion>();
    private Dictionary<Transform, float> initialDistances = new Dictionary<Transform, float>();
    private Dictionary<Transform, float> verticalOffsets = new Dictionary<Transform, float>(); // Stores Y offset between connection point and puppet bone
    private Dictionary<Transform, Transform> reachPointOriginalParents = new Dictionary<Transform, Transform>(); // Stores original parent of reach points

    private void Start()
    {
        if (puppetStrings == null)
        {
            puppetStrings = GetComponent<VintageRobo_PuppetStrings>();
        }

        if (puppetStrings == null)
        {
            
            enabled = false;
            return;
        }

        // First, position reach points above their target bones
        PositionReachPointsAboveTargets();

        // Calculate hand offset BEFORE unparenting (while fingers are still in hierarchy)
        CalculateTargetOffsets();

        // Unparent reach points from skeleton to prevent IK feedback
        UnparentReachPoints();

        // Then cache the transforms for future reference
        CacheOriginalTransforms();
        CacheTargetOriginalTransforms();
        
        // Create visual connections between hands and fingers
        if (autoCreateConnections)
        {
            CreateHandFingerConnections();
        }
    }

    private void PositionReachPointsAboveTargets()
    {
        var strings = GetPuppetStrings();

        foreach (var puppetString in strings)
        {
            if (puppetString.reachPoint == null || puppetString.boneTarget == null)
                continue;

            Transform masterFinger = puppetString.reachPoint;
            Transform puppetBone = puppetString.boneTarget;

            // Calculate where the connection point currently is
            Vector3 currentConnectionPoint = CalculateConnectionPoint(masterFinger, puppetString.connectionPoint);
            
            // Calculate the offset from bone base to connection point
            Vector3 offsetToConnectionPoint = currentConnectionPoint - masterFinger.position;

            // Calculate initial vertical distance (offset) between connection point and puppet bone
            float initialVerticalOffset = currentConnectionPoint.y - puppetBone.position.y;

            // Calculate target position for the connection point (above puppet bone with fixed offset)
            Vector3 targetConnectionPoint = new Vector3(
                puppetBone.position.x,                      // Match puppet's X
                puppetBone.position.y + initialVerticalOffset, // Maintain vertical offset
                puppetBone.position.z                       // Match puppet's Z
            );

            // Position the bone base so that its connection point ends up at the target
            Vector3 targetBonePosition = targetConnectionPoint - offsetToConnectionPoint;
            masterFinger.position = targetBonePosition;

        }

        
    }

    private void CalculateTargetOffsets()
    {
        foreach (var hand in hands)
        {
            // Skip if no IK target assigned
            if (hand.ikTarget == null)
            {
                
                continue;
            }
            
            if (hand.reachPoints == null || hand.reachPoints.Count == 0)
                continue;

            // Calculate average position of reach points
            Vector3 averageReachPosition = CalculateAverageReachPosition(hand.reachPoints);
            
            // Calculate offset from average to current IK target position
            Vector3 calculatedOffset = hand.ikTarget.position - averageReachPosition;
            
            // Store this as the hand offset
            hand.handOffset = calculatedOffset;
            
            
        }
    }

    private void UnparentReachPoints()
    {
        reachPointOriginalParents.Clear();
        
        foreach (var hand in hands)
        {
            if (hand.reachPoints == null) continue;
            
            foreach (var reachPoint in hand.reachPoints)
            {
                if (reachPoint == null) continue;
                
                // Store original parent
                reachPointOriginalParents[reachPoint] = reachPoint.parent;
                
                // Unparent from skeleton (set parent to null or to this component's transform)
                reachPoint.SetParent(transform, true); // worldPositionStays = true
                
                
            }
        }
        
        
    }

    private Vector3 CalculateConnectionPoint(Transform bone, float connectionPoint)
    {
        Vector3 basePosition = bone.position;

        if (bone.childCount > 0)
        {
            Transform childBone = bone.GetChild(0);
            Vector3 tipPosition = childBone.position;
            return Vector3.Lerp(basePosition, tipPosition, connectionPoint);
        }
        else
        {
            float estimatedLength = bone.lossyScale.magnitude * 0.1f;
            Vector3 tipPosition = basePosition + bone.TransformDirection(Vector3.up) * estimatedLength;
            return Vector3.Lerp(basePosition, tipPosition, connectionPoint);
        }
    }

    private void LateUpdate()
    {
        if (!enableControl || puppetStrings == null) return;

        UpdateMasterFingers();
        UpdateIKTargets();
        UpdateHandFingerConnections();
    }

    private void CacheOriginalTransforms()
    {
        originalPositions.Clear();
        originalRotations.Clear();
        initialDistances.Clear();
        verticalOffsets.Clear();

        // Cache original transforms of all reach points (master fingers)
        var strings = GetPuppetStrings();
        foreach (var puppetString in strings)
        {
            if (puppetString.reachPoint == null || puppetString.boneTarget == null) continue;

            if (!originalPositions.ContainsKey(puppetString.reachPoint))
            {
                originalPositions[puppetString.reachPoint] = puppetString.reachPoint.position;
                originalRotations[puppetString.reachPoint] = puppetString.reachPoint.localRotation;
                
                // Cache initial distance between master finger and puppet bone
                float initialDistance = Vector3.Distance(
                    puppetString.reachPoint.position,
                    puppetString.boneTarget.position
                );
                initialDistances[puppetString.reachPoint] = initialDistance;

                // Cache initial vertical offset between connection point and puppet bone
                Vector3 connectionPoint = CalculateConnectionPoint(puppetString.reachPoint, puppetString.connectionPoint);
                float verticalOffset = connectionPoint.y - puppetString.boneTarget.position.y;
                verticalOffsets[puppetString.reachPoint] = verticalOffset;
            }
        }

        
    }

    private void CacheTargetOriginalTransforms()
    {
        foreach (var hand in hands)
        {
            if (hand.ikTarget != null)
            {
                hand.originalTargetPosition = hand.ikTarget.position;
            }
        }
        
        
    }

    private void UpdateMasterFingers()
    {
        var strings = GetPuppetStrings();

        foreach (var puppetString in strings)
        {
            // Skip if no proper mapping
            if (puppetString.reachPoint == null || puppetString.boneTarget == null)
                continue;

            Transform masterFinger = puppetString.reachPoint;  // Master's finger bone
            Transform puppetBone = puppetString.boneTarget;     // Puppet's bone to follow

            // Follow position with influence while maintaining vertical offset
            if (positionInfluence > 0f)
            {
                Vector3 originalPos = originalPositions.ContainsKey(masterFinger) 
                    ? originalPositions[masterFinger] 
                    : masterFinger.position;

                Vector3 puppetPos = puppetBone.position;
                
                // Calculate connection point offset from bone base
                Vector3 currentConnectionPoint = CalculateConnectionPoint(masterFinger, puppetString.connectionPoint);
                Vector3 offsetToConnectionPoint = currentConnectionPoint - masterFinger.position;
                
                // Get the cached vertical offset
                float cachedVerticalOffset = verticalOffsets.ContainsKey(masterFinger) 
                    ? verticalOffsets[masterFinger] 
                    : (currentConnectionPoint.y - puppetPos.y);
                
                Vector3 targetPosition;

                if (lockVerticalAlignment)
                {
                    // Position connection point above puppet bone, maintaining constant vertical offset
                    Vector3 targetConnectionPoint = new Vector3(
                        puppetPos.x,                          // Match puppet's X
                        puppetPos.y + cachedVerticalOffset,   // Maintain vertical offset (follows Y movement)
                        puppetPos.z                           // Match puppet's Z
                    );
                    
                    // Calculate bone base position from target connection point
                    targetPosition = targetConnectionPoint - offsetToConnectionPoint;
                }
                else
                {
                    // Follow puppet position normally
                    targetPosition = puppetPos;
                }
                
                // Blend with original position based on influence
                targetPosition = Vector3.Lerp(originalPos, targetPosition, positionInfluence);

                // Smooth movement
                masterFinger.position = Vector3.Lerp(
                    masterFinger.position,
                    targetPosition,
                    Time.deltaTime * smoothSpeed
                );
            }

            // Follow rotation with influence (only if not preserving original rotation)
            if (rotationInfluence > 0f && !preserveOriginalRotation)
            {
                Quaternion originalRot = originalRotations.ContainsKey(masterFinger)
                    ? originalRotations[masterFinger]
                    : masterFinger.localRotation;

                // Convert puppet bone world rotation to master finger's local space
                Quaternion puppetRotLocal = masterFinger.parent != null
                    ? Quaternion.Inverse(masterFinger.parent.rotation) * puppetBone.rotation
                    : puppetBone.localRotation;

                Quaternion targetRotation = Quaternion.Slerp(
                    originalRot,
                    puppetRotLocal,
                    rotationInfluence
                );

                masterFinger.localRotation = Quaternion.Slerp(
                    masterFinger.localRotation,
                    targetRotation,
                    Time.deltaTime * smoothSpeed
                );
            }
            else if (preserveOriginalRotation)
            {
                // Keep original rotation locked
                Quaternion originalRot = originalRotations.ContainsKey(masterFinger)
                    ? originalRotations[masterFinger]
                    : masterFinger.localRotation;
                
                masterFinger.localRotation = originalRot;
            }
        }
    }

    private void UpdateIKTargets()
    {
        foreach (var hand in hands)
        {
            if (!ValidateHandSetup(hand)) continue;

            // Calculate average position of reach points (fingers)
            Vector3 averageReachPosition = CalculateAverageReachPosition(hand.reachPoints);
            
            // Apply hand offset to get target position
            Vector3 targetPosition = averageReachPosition + hand.handOffset;
            
            // Blend with original position based on influence
            targetPosition = Vector3.Lerp(
                hand.originalTargetPosition,
                targetPosition,
                hand.followInfluence
            );
            
            // Smooth the target movement
            hand.ikTarget.position = Vector3.Lerp(
                hand.ikTarget.position,
                targetPosition,
                Time.deltaTime * hand.smoothSpeed
            );
            
            // IK Target should NOT rotate - let IK solver handle hand rotation completely
            // The target just defines WHERE the hand should be, not HOW it should rotate
        }
    }

    private Vector3 CalculateAverageReachPosition(List<Transform> reachPoints)
    {
        if (reachPoints == null || reachPoints.Count == 0)
            return Vector3.zero;

        Vector3 sum = Vector3.zero;
        int validCount = 0;

        foreach (var reachPoint in reachPoints)
        {
            if (reachPoint != null)
            {
                sum += reachPoint.position;
                validCount++;
            }
        }

        return validCount > 0 ? sum / validCount : Vector3.zero;
    }

    private bool ValidateHandSetup(HandSetup hand)
    {
        if (hand.ikTarget == null)
        {
            return false;
        }

        if (hand.reachPoints == null || hand.reachPoints.Count == 0)
        {
            return false;
        }

        return true;
    }

    private void CreateHandFingerConnections()
    {
        foreach (var hand in hands)
        {
            if (!hand.showConnections || hand.hand == null || hand.reachPoints == null)
                continue;

            // Clear existing connections
            foreach (var line in hand.connectionLines)
            {
                if (line != null && line.gameObject != null)
                    Destroy(line.gameObject);
            }
            hand.connectionLines.Clear();
            hand.lightningOffsets.Clear();

            // Create multiple lines for each finger
            foreach (var finger in hand.reachPoints)
            {
                if (finger == null) continue;

                // Create multiple parallel lines for this finger
                for (int lineNum = 0; lineNum < hand.linesPerConnection; lineNum++)
                {
                    // Create line renderer object
                    GameObject lineObj = new GameObject($"HandConnection_{hand.hand.name}_to_{finger.name}_Line{lineNum}");
                    lineObj.transform.SetParent(transform);
                    lineObj.transform.rotation = Quaternion.identity;

                    LineRenderer lr = lineObj.AddComponent<LineRenderer>();
                    
                    // Configure line renderer
                    int segments = hand.useLightningEffect ? hand.lightningSegments + 2 : 2; // +2 for start and end points
                    lr.positionCount = segments;
                    lr.startWidth = hand.connectionWidth;
                    lr.endWidth = hand.connectionWidth;
                    lr.useWorldSpace = true;
                    lr.alignment = LineAlignment.View;
                    
                    // Set material
                    if (connectionMaterial != null)
                    {
                        lr.material = connectionMaterial;
                    }
                    else
                    {
                        // Create default glowing material
                        Shader unlitShader = Shader.Find("Unlit/Color");
                        if (unlitShader == null) unlitShader = Shader.Find("Standard");
                        
                        Material mat = new Material(unlitShader);
                        mat.color = hand.connectionColor;
                        lr.material = mat;
                    }
                    
                    lr.startColor = hand.connectionColor;
                    lr.endColor = hand.connectionColor;
                    
                    // Disable shadows
                    lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    lr.receiveShadows = false;
                    
                    // Add to list
                    hand.connectionLines.Add(lr);
                    
                    // Add random offset for animation variety
                    hand.lightningOffsets.Add(Random.Range(0f, 100f));
                }
            }

            
        }
    }

    private void UpdateHandFingerConnections()
    {
        foreach (var hand in hands)
        {
            if (!hand.showConnections || hand.hand == null || hand.reachPoints == null)
                continue;

            // Determine base point for connections
            Vector3 basePosition = hand.connectionBasePoint != null 
                ? hand.connectionBasePoint.position 
                : hand.hand.position;

            // Update each connection line (multiple lines per finger)
            int lineIndex = 0;
            for (int fingerIndex = 0; fingerIndex < hand.reachPoints.Count; fingerIndex++)
            {
                Transform finger = hand.reachPoints[fingerIndex];
                if (finger == null)
                {
                    // Skip lines for this missing finger
                    lineIndex += hand.linesPerConnection;
                    continue;
                }

                Vector3 fingerPos = finger.position;

                // Update all lines for this finger
                for (int lineNum = 0; lineNum < hand.linesPerConnection && lineIndex < hand.connectionLines.Count; lineNum++)
                {
                    LineRenderer lr = hand.connectionLines[lineIndex];
                    if (lr == null)
                    {
                        lineIndex++;
                        continue;
                    }

                    // Keep line rotation locked
                    if (lr.transform.rotation != Quaternion.identity)
                    {
                        lr.transform.rotation = Quaternion.identity;
                    }

                    if (hand.useLightningEffect)
                    {
                        // Generate lightning bolt
                        UpdateLightningLine(lr, basePosition, fingerPos, hand, lineIndex);
                    }
                    else
                    {
                        // Simple straight line
                        lr.positionCount = 2;
                        lr.SetPosition(0, basePosition);
                        lr.SetPosition(1, fingerPos);
                    }
                    
                    // Update visibility
                    lr.enabled = hand.showConnections;
                    
                    lineIndex++;
                }
            }
        }
    }

    private void UpdateLightningLine(LineRenderer lr, Vector3 start, Vector3 end, HandSetup hand, int lineIndex)
    {
        int segments = hand.lightningSegments;
        lr.positionCount = segments + 2; // +2 for start and end

        // Set start and end points
        lr.SetPosition(0, start);
        lr.SetPosition(segments + 1, end);

        // Calculate direction and perpendicular vectors for displacement
        Vector3 direction = end - start;
        float totalDistance = direction.magnitude;
        direction.Normalize();

        // Find perpendicular vectors for random displacement
        Vector3 perpendicular1 = Vector3.Cross(direction, Vector3.up);
        if (perpendicular1.magnitude < 0.1f) // If direction is parallel to up, use forward
            perpendicular1 = Vector3.Cross(direction, Vector3.forward);
        perpendicular1.Normalize();

        Vector3 perpendicular2 = Vector3.Cross(direction, perpendicular1).normalized;

        // Animation time with per-line offset for variety
        float timeOffset = hand.lightningOffsets[lineIndex];
        float animTime = Time.time * hand.lightningSpeed + timeOffset;

        // Generate intermediate points with random displacement
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / (segments + 1);
            Vector3 basePoint = Vector3.Lerp(start, end, t);

            // Generate random displacement with Perlin noise for smooth animation
            float noiseX = Mathf.PerlinNoise(animTime + i * 0.5f, timeOffset) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(animTime + i * 0.5f + 100f, timeOffset + 50f) * 2f - 1f;

            // Apply displacement (less at endpoints for natural look)
            float displacementAmount = hand.lightningDisplacement * totalDistance;
            float falloff = Mathf.Sin(t * Mathf.PI); // Reduces displacement near ends
            
            Vector3 displacement = (perpendicular1 * noiseX + perpendicular2 * noiseY) * displacementAmount * falloff;
            
            lr.SetPosition(i, basePoint + displacement);
        }
    }

    public void SetConnectionsVisible(bool visible)
    {
        foreach (var hand in hands)
        {
            hand.showConnections = visible;
            
            foreach (var line in hand.connectionLines)
            {
                if (line != null)
                    line.enabled = visible;
            }
        }
    }

    public void SetConnectionColor(int handIndex, Color color)
    {
        if (handIndex < 0 || handIndex >= hands.Count) return;
        
        var hand = hands[handIndex];
        hand.connectionColor = color;
        
        foreach (var line in hand.connectionLines)
        {
            if (line != null)
            {
                line.startColor = color;
                line.endColor = color;
            }
        }
    }

    // Helper to get strings from the puppet strings component
    private List<VintageRobo_PuppetStrings.PuppetString> GetPuppetStrings()
    {
        // Use reflection to access the private strings list
        var field = typeof(VintageRobo_PuppetStrings).GetField("strings", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            return field.GetValue(puppetStrings) as List<VintageRobo_PuppetStrings.PuppetString>;
        }

        
        return new List<VintageRobo_PuppetStrings.PuppetString>();
    }

    // Public control methods
    public void SetControlEnabled(bool enabled)
    {
        enableControl = enabled;
    }

    public void SetFollowInfluence(float position, float rotation)
    {
        positionInfluence = Mathf.Clamp01(position);
        rotationInfluence = Mathf.Clamp01(rotation);
    }

    public void ResetToOriginalPose()
    {
        foreach (var kvp in originalPositions)
        {
            if (kvp.Key != null)
            {
                kvp.Key.position = kvp.Value;
            }
        }

        foreach (var kvp in originalRotations)
        {
            if (kvp.Key != null)
            {
                kvp.Key.localRotation = kvp.Value;
            }
        }

        foreach (var hand in hands)
        {
            if (hand.ikTarget != null)
            {
                hand.ikTarget.position = hand.originalTargetPosition;
            }
        }
    }

    #if UNITY_EDITOR
    [ContextMenu("Recache Original Transforms")]
    private void EditorRecacheTransforms()
    {
        if (puppetStrings == null)
        {
            puppetStrings = GetComponent<VintageRobo_PuppetStrings>();
        }
        CacheOriginalTransforms();
        CacheTargetOriginalTransforms();
    }

    [ContextMenu("Reset to Original Pose")]
    private void EditorResetPose()
    {
        ResetToOriginalPose();
    }
    
    [ContextMenu("Create Hand-Finger Connections")]
    private void EditorCreateConnections()
    {
        if (Application.isPlaying)
        {
            CreateHandFingerConnections();
        }
        else
        {
            
        }
    }
    
    [ContextMenu("Show Connections")]
    private void EditorShowConnections()
    {
        SetConnectionsVisible(true);
    }
    
    [ContextMenu("Hide Connections")]
    private void EditorHideConnections()
    {
        SetConnectionsVisible(false);
    }
    #endif
}
