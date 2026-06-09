using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VintageRobo_PuppetStrings : MonoBehaviour
{
    [System.Serializable]
    public class PuppetString
    {
        [Header("String Configuration")]
        public string stringName = "String";
        public Transform boneTarget; // The bone this string attaches to (bottom end)
        public Transform reachPoint; // The point where string reaches (top end)
        public bool useOffsetIfNoReachPoint = true; // Fallback to offset if reachPoint is null
        public Vector3 anchorOffset = new Vector3(0, 2, 0); // Used only if reachPoint is null
        
        [Header("String Connection Point")]
        [Range(0f, 1f)]
        [Tooltip("Where on the reach point bone the string connects (0 = base, 0.5 = middle, 1 = tip)")]
        public float connectionPoint = 1f; // 1 = tip of finger by default
        
        [Header("Visual Settings")]
        public float stringWidth = 0.02f; // Increased default width for better visibility
        public Color stringColor = Color.white;
        public Material stringMaterial;
        
        [HideInInspector] public LineRenderer lineRenderer;
    }

    [Header("Puppet Strings")]
    [SerializeField] private List<PuppetString> strings = new List<PuppetString>();

    [Header("Global String Settings")]
    [SerializeField] private Material defaultStringMaterial;
    [SerializeField] private bool updateInEditor = false;

    private void Start()
    {
        InitializeStrings();
    }

    private void Update()
    {
        UpdateStringPositions();
    }

    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (updateInEditor && Application.isPlaying)
        {
            UpdateStringVisuals();
        }
    }
    #endif

    private void InitializeStrings()
    {
        Debug.Log($"Initializing {strings.Count} puppet strings...");
        
        foreach (PuppetString puppetString in strings)
        {
            if (puppetString.boneTarget == null)
            {
                Debug.LogWarning($"String '{puppetString.stringName}' has no bone target assigned! Please drag a bone into the Bone Target field.");
                continue;
            }

            Debug.Log($"Setting up string: {puppetString.stringName} attached to {puppetString.boneTarget.name}");

            // Create LineRenderer directly on this GameObject if it doesn't exist
            if (puppetString.lineRenderer == null)
            {
                GameObject stringObject = new GameObject($"{puppetString.stringName}_LineRenderer");
                stringObject.transform.SetParent(transform);
                stringObject.transform.rotation = Quaternion.identity; // Lock rotation
                puppetString.lineRenderer = stringObject.AddComponent<LineRenderer>();
                Debug.Log($"Created LineRenderer for {puppetString.stringName}");
            }

            // Configure LineRenderer
            SetupLineRenderer(puppetString);
            
            Debug.Log($"String '{puppetString.stringName}' initialized successfully!");
        }
        
        Debug.Log("Puppet strings initialization complete!");
    }

    private void SetupLineRenderer(PuppetString puppetString)
    {
        LineRenderer lr = puppetString.lineRenderer;
        
        lr.positionCount = 2;
        lr.startWidth = puppetString.stringWidth;
        lr.endWidth = puppetString.stringWidth;
        
        // Use custom material or default
        Material mat = puppetString.stringMaterial != null ? puppetString.stringMaterial : defaultStringMaterial;
        if (mat != null)
        {
            lr.material = mat;
        }
        else
        {
            // Create a simple unlit material for 3D rendering
            Shader unlitShader = Shader.Find("Unlit/Color");
            if (unlitShader == null)
                unlitShader = Shader.Find("Standard");
            
            Material newMat = new Material(unlitShader);
            newMat.color = puppetString.stringColor;
            lr.material = newMat;
            
            Debug.Log($"Created material with shader: {unlitShader.name} for {puppetString.stringName}");
        }
        
        lr.startColor = puppetString.stringColor;
        lr.endColor = puppetString.stringColor;
        
        // Disable shadows for strings
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        
        // Important: Use world space for 3D positioning
        lr.useWorldSpace = true;
        
        // Set alignment to view for better visibility
        lr.alignment = LineAlignment.View;
        
        Debug.Log($"LineRenderer configured for {puppetString.stringName}: width={puppetString.stringWidth}, color={puppetString.stringColor}");
    }

    private void UpdateStringPositions()
    {
        foreach (PuppetString puppetString in strings)
        {
            if (puppetString.boneTarget == null || puppetString.lineRenderer == null)
                continue;

            // Keep string GameObject rotation locked
            if (puppetString.lineRenderer.transform.rotation != Quaternion.identity)
            {
                puppetString.lineRenderer.transform.rotation = Quaternion.identity;
            }

            Vector3 bonePos = puppetString.boneTarget.position;
            Vector3 topPos;

            // Use reach point if assigned, otherwise use offset
            if (puppetString.reachPoint != null)
            {
                topPos = CalculateBoneConnectionPoint(puppetString.reachPoint, puppetString.connectionPoint);
            }
            else if (puppetString.useOffsetIfNoReachPoint)
            {
                topPos = bonePos + puppetString.anchorOffset;
            }
            else
            {
                continue; // Skip if no reach point and offset disabled
            }

            // Update line positions in world space (only positions, not rotation)
            puppetString.lineRenderer.SetPosition(0, topPos);   // Top (reach point or offset)
            puppetString.lineRenderer.SetPosition(1, bonePos);  // Bottom (bone)
        }
    }

    private Vector3 CalculateBoneConnectionPoint(Transform bone, float connectionPoint)
    {
        // Start at bone base (pivot)
        Vector3 basePosition = bone.position;

        // If bone has a child, calculate tip position
        if (bone.childCount > 0)
        {
            Transform childBone = bone.GetChild(0);
            Vector3 tipPosition = childBone.position;
            
            // Interpolate between base and tip based on connection point
            return Vector3.Lerp(basePosition, tipPosition, connectionPoint);
        }
        else
        {
            // No child bone, estimate tip using bone's forward direction
            // Assume bone length based on scale or a default length
            float estimatedLength = bone.lossyScale.magnitude * 0.1f; // Adjust multiplier as needed
            Vector3 tipPosition = basePosition + bone.TransformDirection(Vector3.up) * estimatedLength;
            
            return Vector3.Lerp(basePosition, tipPosition, connectionPoint);
        }
    }

    private void UpdateStringVisuals()
    {
        foreach (PuppetString puppetString in strings)
        {
            if (puppetString.lineRenderer != null)
            {
                puppetString.lineRenderer.startWidth = puppetString.stringWidth;
                puppetString.lineRenderer.endWidth = puppetString.stringWidth;
                puppetString.lineRenderer.startColor = puppetString.stringColor;
                puppetString.lineRenderer.endColor = puppetString.stringColor;
            }
        }
    }

    // Public methods to add strings at runtime
    public void AddString(string name, Transform bone, Vector3 offset, Color color, float width = 0.01f)
    {
        PuppetString newString = new PuppetString
        {
            stringName = name,
            boneTarget = bone,
            anchorOffset = offset,
            stringColor = color,
            stringWidth = width
        };
        
        strings.Add(newString);
        
        if (Application.isPlaying)
        {
            InitializeStrings();
        }
    }

    public void RemoveString(string name)
    {
        PuppetString toRemove = strings.Find(s => s.stringName == name);
        if (toRemove != null)
        {
            if (toRemove.lineRenderer != null && toRemove.lineRenderer.gameObject != null)
                Destroy(toRemove.lineRenderer.gameObject);
            
            strings.Remove(toRemove);
        }
    }

    public void SetStringVisibility(bool visible)
    {
        Debug.Log($"Setting string visibility to: {visible}");
        int count = 0;
        
        foreach (PuppetString puppetString in strings)
        {
            if (puppetString.lineRenderer != null)
            {
                puppetString.lineRenderer.enabled = visible;
                count++;
            }
        }
        
        Debug.Log($"Updated visibility for {count} strings");
    }

    // Editor helper methods
    #if UNITY_EDITOR
    [ContextMenu("Initialize Strings")]
    private void EditorInitializeStrings()
    {
        InitializeStrings();
    }

    [ContextMenu("Clear All Strings")]
    private void ClearAllStrings()
    {
        foreach (PuppetString puppetString in strings)
        {
            if (puppetString.lineRenderer != null && puppetString.lineRenderer.gameObject != null)
            {
                DestroyImmediate(puppetString.lineRenderer.gameObject);
            }
        }
        strings.Clear();
    }
    #endif
}
