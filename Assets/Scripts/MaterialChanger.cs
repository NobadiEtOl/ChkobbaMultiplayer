using UnityEngine;

public class MaterialChanger : MonoBehaviour
{
    [Header("Material Assignment")]
    [SerializeField] private Material targetMaterial;
    
    [Header("Object References")]
    [SerializeField] private GameObject[] cubeObjects;
    
    [Header("Scale-Aware Settings")]
    [SerializeField] private bool enableScaleAwareness = true;
    [SerializeField] private string scalePropertyName = "_ObjectScale";
    [SerializeField] private bool logScaleInfo = true;
    
    [Header("Rotation-Aware Settings")]
    [SerializeField] private bool enableRotationAwareness = true;
    [SerializeField] private string rotationPropertyName = "_ObjectRotation";
    [SerializeField] private bool logRotationInfo = true;
    
    [ContextMenu("Load Unlit_WoodenFrame Material")]
    public void LoadUnlitWoodenFrameMaterial()
    {
        // Load the Unlit_WoodenFrame material from Resources or direct path
        targetMaterial = Resources.Load<Material>("Shader/SceneDecor/Unlit_WoodenFrame");
        
        if (targetMaterial == null)
        {
            // Try alternative loading method
            targetMaterial = Resources.Load<Material>("Unlit_WoodenFrame");
        }
        
        if (targetMaterial == null)
        {
            Debug.LogError("Could not load Unlit_WoodenFrame material. Please assign it manually in the inspector.");
            return;
        }
        
        Debug.Log("Unlit_WoodenFrame material loaded successfully!");
    }
    
    [ContextMenu("Find and Change Cube Materials (Scale & Rotation Aware)")]
    public void FindAndChangeCubeMaterials()
    {
        // First, try to load the material if not already assigned
        if (targetMaterial == null)
        {
            LoadUnlitWoodenFrameMaterial();
        }
        
        if (targetMaterial == null)
        {
            Debug.LogError("No target material assigned. Please assign Unlit_WoodenFrame material manually.");
            return;
        }
        
        // Find the MesaMantelRandom object
        GameObject mesaMantelRandom = GameObject.Find("MesaMantelRandom");
        
        if (mesaMantelRandom == null)
        {
            Debug.LogError("Could not find 'MesaMantelRandom' object in the scene.");
            return;
        }
        
        // Find the Frame object under MesaMantelRandom
        Transform frameTransform = mesaMantelRandom.transform.Find("Frame");
        
        if (frameTransform == null)
        {
            Debug.LogError("Could not find 'Frame' object under MesaMantelRandom.");
            return;
        }
        
        // Find all Cube objects under Frame
        Transform[] cubeTransforms = frameTransform.GetComponentsInChildren<Transform>();
        int cubeCount = 0;
        
        foreach (Transform child in cubeTransforms)
        {
            if (child.name.ToLower().Contains("cube"))
            {
                Renderer renderer = child.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Create a new material instance for each object to avoid sharing
                    Material instanceMaterial = new Material(targetMaterial);
                    
                                         // Apply scale-aware properties if enabled
                     if (enableScaleAwareness)
                     {
                         ApplyScaleAwareProperties(instanceMaterial, child);
                     }
                     
                     // Apply rotation-aware properties if enabled
                     if (enableRotationAwareness)
                     {
                         ApplyRotationAwareProperties(instanceMaterial, child);
                     }
                    
                    renderer.material = instanceMaterial;
                    cubeCount++;
                    
                                         if (logScaleInfo || logRotationInfo)
                     {
                         Vector3 scale = child.lossyScale;
                         Vector3 rotation = child.eulerAngles;
                         string logMessage = $"Changed material for cube: {child.name}";
                         
                         if (logScaleInfo)
                         {
                             logMessage += $" (Scale: {scale.x:F2}, {scale.y:F2}, {scale.z:F2})";
                         }
                         
                         if (logRotationInfo)
                         {
                             logMessage += $" (Rotation: {rotation.x:F1}°, {rotation.y:F1}°, {rotation.z:F1}°)";
                         }
                         
                         Debug.Log(logMessage);
                     }
                     else
                     {
                         Debug.Log($"Changed material for cube: {child.name}");
                     }
                }
            }
        }
        
        Debug.Log($"Successfully changed materials for {cubeCount} cube objects with scale and rotation awareness.");
    }
    
    [ContextMenu("Change Materials for Assigned Cubes (Scale & Rotation Aware)")]
    public void ChangeMaterialsForAssignedCubes()
    {
        if (targetMaterial == null)
        {
            Debug.LogError("No target material assigned. Please assign Unlit_WoodenFrame material.");
            return;
        }
        
        if (cubeObjects == null || cubeObjects.Length == 0)
        {
            Debug.LogError("No cube objects assigned. Please assign them in the inspector or use 'Find and Change Cube Materials'.");
            return;
        }
        
        int changedCount = 0;
        foreach (GameObject cube in cubeObjects)
        {
            if (cube != null)
            {
                Renderer renderer = cube.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Create a new material instance for each object
                    Material instanceMaterial = new Material(targetMaterial);
                    
                                         // Apply scale-aware properties if enabled
                     if (enableScaleAwareness)
                     {
                         ApplyScaleAwareProperties(instanceMaterial, cube.transform);
                     }
                     
                     // Apply rotation-aware properties if enabled
                     if (enableRotationAwareness)
                     {
                         ApplyRotationAwareProperties(instanceMaterial, cube.transform);
                     }
                    
                    renderer.material = instanceMaterial;
                    changedCount++;
                    
                                         if (logScaleInfo || logRotationInfo)
                     {
                         Vector3 scale = cube.transform.lossyScale;
                         Vector3 rotation = cube.transform.eulerAngles;
                         string logMessage = $"Changed material for cube: {cube.name}";
                         
                         if (logScaleInfo)
                         {
                             logMessage += $" (Scale: {scale.x:F2}, {scale.y:F2}, {scale.z:F2})";
                         }
                         
                         if (logRotationInfo)
                         {
                             logMessage += $" (Rotation: {rotation.x:F1}°, {rotation.y:F1}°, {rotation.z:F1}°)";
                         }
                         
                         Debug.Log(logMessage);
                     }
                     else
                     {
                         Debug.Log($"Changed material for cube: {cube.name}");
                     }
                }
            }
        }
        
        Debug.Log($"Successfully changed materials for {changedCount} assigned cube objects with scale and rotation awareness.");
    }
    
    private void ApplyScaleAwareProperties(Material material, Transform targetTransform)
    {
        Vector3 worldScale = targetTransform.lossyScale;
        
        // Handle extreme scale differences (like 0.001 vs 1.0)
        Vector3 normalizedScale = NormalizeExtremeScales(worldScale);
        Vector3 logScale = new Vector3(
            Mathf.Log10(Mathf.Max(worldScale.x, 0.0001f)),
            Mathf.Log10(Mathf.Max(worldScale.y, 0.0001f)),
            Mathf.Log10(Mathf.Max(worldScale.z, 0.0001f))
        );
        
        // Set the scale property if it exists in the shader
        if (material.HasProperty(scalePropertyName))
        {
            material.SetVector(scalePropertyName, worldScale);
        }
        
        // Set common scale-related properties that might exist in the shader
        if (material.HasProperty("_Scale"))
        {
            material.SetVector("_Scale", worldScale);
        }
        
        if (material.HasProperty("_ObjectScale"))
        {
            material.SetVector("_ObjectScale", worldScale);
        }
        
        if (material.HasProperty("_WorldScale"))
        {
            material.SetVector("_WorldScale", worldScale);
        }
        
        // Set normalized scale for better shader handling
        if (material.HasProperty("_NormalizedScale"))
        {
            material.SetVector("_NormalizedScale", normalizedScale);
        }
        
        // Set logarithmic scale for extreme differences
        if (material.HasProperty("_LogScale"))
        {
            material.SetVector("_LogScale", logScale);
        }
        
        // Calculate and set tiling based on scale with extreme scale handling
        if (material.HasProperty("_MainTex"))
        {
            Vector2 tiling = CalculateTilingForExtremeScales(worldScale);
            material.SetTextureScale("_MainTex", tiling);
        }
        
        // Set scale factor for shader calculations
        if (material.HasProperty("_ScaleFactor"))
        {
            float scaleFactor = CalculateScaleFactor(worldScale);
            material.SetFloat("_ScaleFactor", scaleFactor);
        }
        
        // Set individual scale components
        if (material.HasProperty("_ScaleX"))
        {
            material.SetFloat("_ScaleX", worldScale.x);
        }
        
        if (material.HasProperty("_ScaleY"))
        {
            material.SetFloat("_ScaleY", worldScale.y);
        }
        
        if (material.HasProperty("_ScaleZ"))
        {
            material.SetFloat("_ScaleZ", worldScale.z);
        }
        
        // Set aspect ratio for thin objects
        if (material.HasProperty("_AspectRatio"))
        {
            float aspectRatio = worldScale.y / Mathf.Max(worldScale.x, 0.0001f);
            material.SetFloat("_AspectRatio", aspectRatio);
        }
        
                 // Set thickness factor for very thin objects
         if (material.HasProperty("_ThicknessFactor"))
         {
             float thicknessFactor = Mathf.Min(worldScale.x, worldScale.z) / Mathf.Max(worldScale.y, 0.0001f);
             material.SetFloat("_ThicknessFactor", thicknessFactor);
         }
     }
     
     private void ApplyRotationAwareProperties(Material material, Transform targetTransform)
     {
         Vector3 eulerAngles = targetTransform.eulerAngles;
         Quaternion rotation = targetTransform.rotation;
         
         // Convert euler angles to radians for shader calculations
         Vector3 rotationRadians = new Vector3(
             eulerAngles.x * Mathf.Deg2Rad,
             eulerAngles.y * Mathf.Deg2Rad,
             eulerAngles.z * Mathf.Deg2Rad
         );
         
         // Set the rotation property if it exists in the shader
         if (material.HasProperty(rotationPropertyName))
         {
             material.SetVector(rotationPropertyName, eulerAngles);
         }
         
         // Set common rotation-related properties that might exist in the shader
         if (material.HasProperty("_Rotation"))
         {
             material.SetVector("_Rotation", eulerAngles);
         }
         
         if (material.HasProperty("_ObjectRotation"))
         {
             material.SetVector("_ObjectRotation", eulerAngles);
         }
         
         if (material.HasProperty("_WorldRotation"))
         {
             material.SetVector("_WorldRotation", eulerAngles);
         }
         
         // Set rotation in radians for shader calculations
         if (material.HasProperty("_RotationRadians"))
         {
             material.SetVector("_RotationRadians", rotationRadians);
         }
         
         // Set individual rotation components
         if (material.HasProperty("_RotationX"))
         {
             material.SetFloat("_RotationX", eulerAngles.x);
         }
         
         if (material.HasProperty("_RotationY"))
         {
             material.SetFloat("_RotationY", eulerAngles.y);
         }
         
         if (material.HasProperty("_RotationZ"))
         {
             material.SetFloat("_RotationZ", eulerAngles.z);
         }
         
         // Set rotation components in radians
         if (material.HasProperty("_RotationXRad"))
         {
             material.SetFloat("_RotationXRad", rotationRadians.x);
         }
         
         if (material.HasProperty("_RotationYRad"))
         {
             material.SetFloat("_RotationYRad", rotationRadians.y);
         }
         
         if (material.HasProperty("_RotationZRad"))
         {
             material.SetFloat("_RotationZRad", rotationRadians.z);
         }
         
         // Set quaternion rotation
         if (material.HasProperty("_RotationQuaternion"))
         {
             material.SetVector("_RotationQuaternion", new Vector4(rotation.x, rotation.y, rotation.z, rotation.w));
         }
         
         // Set normalized rotation (0-1 range)
         if (material.HasProperty("_RotationNormalized"))
         {
             Vector3 normalizedRotation = new Vector3(
                 eulerAngles.x / 360f,
                 eulerAngles.y / 360f,
                 eulerAngles.z / 360f
             );
             material.SetVector("_RotationNormalized", normalizedRotation);
         }
         
         // Set rotation direction vectors
         if (material.HasProperty("_Forward"))
         {
             material.SetVector("_Forward", targetTransform.forward);
         }
         
         if (material.HasProperty("_Right"))
         {
             material.SetVector("_Right", targetTransform.right);
         }
         
         if (material.HasProperty("_Up"))
         {
             material.SetVector("_Up", targetTransform.up);
         }
         
         // Set rotation matrix
         if (material.HasProperty("_RotationMatrix"))
         {
             Matrix4x4 rotationMatrix = targetTransform.localToWorldMatrix;
             material.SetMatrix("_RotationMatrix", rotationMatrix);
         }
         
         // Set rotation for texture mapping
         if (material.HasProperty("_TextureRotation"))
         {
             float textureRotation = eulerAngles.z; // Use Z rotation for texture rotation
             material.SetFloat("_TextureRotation", textureRotation);
         }
         
         // Set rotation offset for shader calculations
         if (material.HasProperty("_RotationOffset"))
         {
             Vector3 rotationOffset = new Vector3(
                 Mathf.Sin(rotationRadians.x),
                 Mathf.Sin(rotationRadians.y),
                 Mathf.Sin(rotationRadians.z)
             );
             material.SetVector("_RotationOffset", rotationOffset);
         }
     }
    
    private Vector3 NormalizeExtremeScales(Vector3 scale)
    {
        // Normalize extreme scales to a more manageable range
        float maxScale = Mathf.Max(scale.x, scale.y, scale.z);
        float minScale = Mathf.Min(scale.x, scale.y, scale.z);
        
        // If the difference is extreme (more than 1000x), normalize
        if (maxScale / Mathf.Max(minScale, 0.0001f) > 1000f)
        {
            return new Vector3(
                Mathf.Clamp(scale.x / maxScale, 0.001f, 1f),
                Mathf.Clamp(scale.y / maxScale, 0.001f, 1f),
                Mathf.Clamp(scale.z / maxScale, 0.001f, 1f)
            );
        }
        
        return scale;
    }
    
    private Vector2 CalculateTilingForExtremeScales(Vector3 scale)
    {
        // For objects with extreme scale differences, use a more balanced tiling
        float maxScale = Mathf.Max(scale.x, scale.y, scale.z);
        
        // If X and Z are very small compared to Y, use Y for both dimensions
        if (scale.x < 0.01f && scale.z < 0.01f && scale.y > 0.1f)
        {
            return new Vector2(scale.y, scale.y);
        }
        
        // Otherwise, use the larger of X or Z for the first dimension
        float firstDimension = Mathf.Max(scale.x, scale.z);
        return new Vector2(firstDimension, scale.y);
    }
    
    private float CalculateScaleFactor(Vector3 scale)
    {
        // For extreme scale differences, use the dominant scale
        float maxScale = Mathf.Max(scale.x, scale.y, scale.z);
        float minScale = Mathf.Min(scale.x, scale.y, scale.z);
        
        // If there's an extreme difference, use the larger scale
        if (maxScale / Mathf.Max(minScale, 0.0001f) > 100f)
        {
            return maxScale;
        }
        
        // Otherwise, use the average of the two larger scales
        float[] scales = { scale.x, scale.y, scale.z };
        System.Array.Sort(scales);
        return (scales[1] + scales[2]) / 2f;
    }
    
    [ContextMenu("Manual Material Assignment")]
    public void ManualMaterialAssignment()
    {
        Debug.Log("To manually assign materials:");
        Debug.Log("1. Select this GameObject in the inspector");
        Debug.Log("2. Drag the 'Unlit_WoodenFrame' material from Assets/Shader/SceneDecor/ to the 'Target Material' field");
        Debug.Log("3. Drag the 4 Cube objects to the 'Cube Objects' array");
        Debug.Log("4. Enable 'Scale Awareness' if your material supports it");
        Debug.Log("5. Click 'Change Materials for Assigned Cubes' in the context menu");
    }
    
    [ContextMenu("Test Scale and Rotation Awareness")]
    public void TestScaleAndRotationAwareness()
    {
        if (targetMaterial == null)
        {
            Debug.LogError("No target material assigned.");
            return;
        }
        
        Debug.Log("=== TESTING SCALE AND ROTATION AWARENESS ===");
        
        Debug.Log("\n--- SCALE PROPERTIES ---");
        Debug.Log($"Material has _ObjectScale property: {targetMaterial.HasProperty(scalePropertyName)}");
        Debug.Log($"Material has _Scale property: {targetMaterial.HasProperty("_Scale")}");
        Debug.Log($"Material has _MainTex property: {targetMaterial.HasProperty("_MainTex")}");
        Debug.Log($"Material has _ScaleFactor property: {targetMaterial.HasProperty("_ScaleFactor")}");
        Debug.Log($"Material has _NormalizedScale property: {targetMaterial.HasProperty("_NormalizedScale")}");
        Debug.Log($"Material has _LogScale property: {targetMaterial.HasProperty("_LogScale")}");
        Debug.Log($"Material has _AspectRatio property: {targetMaterial.HasProperty("_AspectRatio")}");
        Debug.Log($"Material has _ThicknessFactor property: {targetMaterial.HasProperty("_ThicknessFactor")}");
        Debug.Log($"Material has _ScaleX property: {targetMaterial.HasProperty("_ScaleX")}");
        Debug.Log($"Material has _ScaleY property: {targetMaterial.HasProperty("_ScaleY")}");
        Debug.Log($"Material has _ScaleZ property: {targetMaterial.HasProperty("_ScaleZ")}");
        
        Debug.Log("\n--- ROTATION PROPERTIES ---");
        Debug.Log($"Material has _ObjectRotation property: {targetMaterial.HasProperty(rotationPropertyName)}");
        Debug.Log($"Material has _Rotation property: {targetMaterial.HasProperty("_Rotation")}");
        Debug.Log($"Material has _RotationRadians property: {targetMaterial.HasProperty("_RotationRadians")}");
        Debug.Log($"Material has _RotationX property: {targetMaterial.HasProperty("_RotationX")}");
        Debug.Log($"Material has _RotationY property: {targetMaterial.HasProperty("_RotationY")}");
        Debug.Log($"Material has _RotationZ property: {targetMaterial.HasProperty("_RotationZ")}");
        Debug.Log($"Material has _RotationXRad property: {targetMaterial.HasProperty("_RotationXRad")}");
        Debug.Log($"Material has _RotationYRad property: {targetMaterial.HasProperty("_RotationYRad")}");
        Debug.Log($"Material has _RotationZRad property: {targetMaterial.HasProperty("_RotationZRad")}");
        Debug.Log($"Material has _RotationQuaternion property: {targetMaterial.HasProperty("_RotationQuaternion")}");
        Debug.Log($"Material has _RotationNormalized property: {targetMaterial.HasProperty("_RotationNormalized")}");
        Debug.Log($"Material has _Forward property: {targetMaterial.HasProperty("_Forward")}");
        Debug.Log($"Material has _Right property: {targetMaterial.HasProperty("_Right")}");
        Debug.Log($"Material has _Up property: {targetMaterial.HasProperty("_Up")}");
        Debug.Log($"Material has _RotationMatrix property: {targetMaterial.HasProperty("_RotationMatrix")}");
        Debug.Log($"Material has _TextureRotation property: {targetMaterial.HasProperty("_TextureRotation")}");
        Debug.Log($"Material has _RotationOffset property: {targetMaterial.HasProperty("_RotationOffset")}");
        
        Debug.Log("\n=== TEST COMPLETE ===");
    }
    
    [ContextMenu("Analyze Extreme Scales")]
    public void AnalyzeExtremeScales()
    {
        // Find the MesaMantelRandom object
        GameObject mesaMantelRandom = GameObject.Find("MesaMantelRandom");
        
        if (mesaMantelRandom == null)
        {
            Debug.LogError("Could not find 'MesaMantelRandom' object in the scene.");
            return;
        }
        
        // Find the Frame object under MesaMantelRandom
        Transform frameTransform = mesaMantelRandom.transform.Find("Frame");
        
        if (frameTransform == null)
        {
            Debug.LogError("Could not find 'Frame' object under MesaMantelRandom.");
            return;
        }
        
        Debug.Log("=== EXTREME SCALE ANALYSIS ===");
        
        // Find all Cube objects under Frame
        Transform[] cubeTransforms = frameTransform.GetComponentsInChildren<Transform>();
        
        foreach (Transform child in cubeTransforms)
        {
            if (child.name.ToLower().Contains("cube"))
            {
                Vector3 scale = child.lossyScale;
                Vector3 normalizedScale = NormalizeExtremeScales(scale);
                Vector2 tiling = CalculateTilingForExtremeScales(scale);
                float scaleFactor = CalculateScaleFactor(scale);
                float aspectRatio = scale.y / Mathf.Max(scale.x, 0.0001f);
                float thicknessFactor = Mathf.Min(scale.x, scale.z) / Mathf.Max(scale.y, 0.0001f);
                
                                 Vector3 rotation = child.eulerAngles;
                 Vector3 rotationRadians = new Vector3(
                     rotation.x * Mathf.Deg2Rad,
                     rotation.y * Mathf.Deg2Rad,
                     rotation.z * Mathf.Deg2Rad
                 );
                 
                 Debug.Log($"\nCube: {child.name}");
                 Debug.Log($"  Raw Scale: ({scale.x:F6}, {scale.y:F6}, {scale.z:F6})");
                 Debug.Log($"  Normalized Scale: ({normalizedScale.x:F3}, {normalizedScale.y:F3}, {normalizedScale.z:F3})");
                 Debug.Log($"  Tiling: ({tiling.x:F3}, {tiling.y:F3})");
                 Debug.Log($"  Scale Factor: {scaleFactor:F3}");
                 Debug.Log($"  Aspect Ratio: {aspectRatio:F3}");
                 Debug.Log($"  Thickness Factor: {thicknessFactor:F6}");
                 Debug.Log($"  Rotation (Euler): ({rotation.x:F1}°, {rotation.y:F1}°, {rotation.z:F1}°)");
                 Debug.Log($"  Rotation (Radians): ({rotationRadians.x:F3}, {rotationRadians.y:F3}, {rotationRadians.z:F3})");
                 Debug.Log($"  Forward Vector: ({child.forward.x:F3}, {child.forward.y:F3}, {child.forward.z:F3})");
                 Debug.Log($"  Right Vector: ({child.right.x:F3}, {child.right.y:F3}, {child.right.z:F3})");
                 Debug.Log($"  Up Vector: ({child.up.x:F3}, {child.up.y:F3}, {child.up.z:F3})");
                 
                 // Check if this is an extreme scale case
                 float maxScale = Mathf.Max(scale.x, scale.y, scale.z);
                 float minScale = Mathf.Min(scale.x, scale.y, scale.z);
                 float scaleRatio = maxScale / Mathf.Max(minScale, 0.0001f);
                 
                 if (scaleRatio > 100f)
                 {
                     Debug.Log($"  ⚠️ EXTREME SCALE DETECTED: {scaleRatio:F0}x difference");
                 }
                 
                 // Check for significant rotation
                 if (Mathf.Abs(rotation.x) > 5f || Mathf.Abs(rotation.y) > 5f || Mathf.Abs(rotation.z) > 5f)
                 {
                     Debug.Log($"  🔄 SIGNIFICANT ROTATION DETECTED: {rotation.magnitude:F1}° total rotation");
                 }
            }
        }
        
        Debug.Log("\n=== ANALYSIS COMPLETE ===");
    }
} 