using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Linq;

public class CRTTester : MonoBehaviour
{
    [Header("CRT Testing")]
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private Material crtMaterial;
    [SerializeField] private bool enableCRT = true;
    
    void Start()
    {
        // Find the post-process volume if not assigned
        if (postProcessVolume == null)
        {
            postProcessVolume = FindObjectOfType<Volume>();
        }
        
        // Find the CRT material if not assigned
        if (crtMaterial == null)
        {
            #if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("CRTScreen_Material t:Material");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                crtMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
            }
            #else
            // In builds, try to find the material by name
            crtMaterial = Resources.Load<Material>("CRTScreen_Material");
            #endif
        }
        
        // Log system info for debugging
        LogSystemInfo();
    }
    
    [ContextMenu("Log System Info")]
    public void LogSystemInfo()
    {
        Debug.Log("=== SYSTEM INFO ===");
        Debug.Log($"Platform: {Application.platform}");
        Debug.Log($"Graphics Device: {SystemInfo.graphicsDeviceName}");
        Debug.Log($"Graphics Memory: {SystemInfo.graphicsMemorySize} MB");
        Debug.Log($"Graphics API: {SystemInfo.graphicsDeviceType}");
        Debug.Log($"Shader Level: {SystemInfo.graphicsShaderLevel}");
        Debug.Log($"Max Texture Size: {SystemInfo.maxTextureSize}");
        Debug.Log($"Supports Instancing: {SystemInfo.supportsInstancing}");
        Debug.Log($"Supports Compute Shaders: {SystemInfo.supportsComputeShaders}");
        Debug.Log($"Rendering Pipeline: {GraphicsSettings.currentRenderPipeline?.GetType().Name ?? "Built-in"}");
        Debug.Log("=== END SYSTEM INFO ===");
    }
    
    [ContextMenu("Check CRT Post-Process")]
    public void CheckCRTPostProcess()
    {
        Debug.Log("=== CRT POST-PROCESS CHECK ===");
        
        // Check if we have a render pipeline asset
        var pipelineAsset = GraphicsSettings.currentRenderPipeline;
        if (pipelineAsset == null)
        {
            Debug.LogError("No render pipeline asset found! CRT effects require URP.");
            return;
        }
        
        Debug.Log($"Render Pipeline: {pipelineAsset.GetType().Name}");
        
        // Check for CRT post-process feature using a simpler approach
        var urpAsset = pipelineAsset as UniversalRenderPipelineAsset;
        if (urpAsset != null)
        {
            Debug.Log("URP Asset found!");
            
            // Look for CRT post-process feature in the scene
            var crtFeatures = FindObjectsOfType<MonoBehaviour>().Where(mb => mb.GetType().Name.Contains("CRT"));
            if (crtFeatures.Any())
            {
                Debug.Log($"Found {crtFeatures.Count()} CRT-related components in scene");
                foreach (var feature in crtFeatures)
                {
                    Debug.Log($"  - CRT Component: {feature.name} (Type: {feature.GetType().Name})");
                }
            }
            else
            {
                Debug.LogWarning("No CRT post-process features found in scene!");
            }
        }
        
        // Check post-process volume
        if (postProcessVolume != null)
        {
            Debug.Log($"Post-Process Volume: {postProcessVolume.name}");
            Debug.Log($"Post-Process Volume Enabled: {postProcessVolume.enabled}");
            Debug.Log($"Post-Process Volume Weight: {postProcessVolume.weight}");
            Debug.Log($"Post-Process Volume Priority: {postProcessVolume.priority}");
        }
        else
        {
            Debug.LogWarning("No Post-Process Volume found!");
        }
        
        // Check CRT material
        if (crtMaterial != null)
        {
            Debug.Log($"CRT Material: {crtMaterial.name}");
            Debug.Log($"CRT Shader: {crtMaterial.shader.name}");
            Debug.Log($"CRT Material Shader Enabled: {crtMaterial.shader.isSupported}");
        }
        else
        {
            Debug.LogError("CRT Material not found!");
        }
        
        Debug.Log("=== END CRT POST-PROCESS CHECK ===");
    }
    
    [ContextMenu("Toggle CRT Effect")]
    public void ToggleCRTEffect()
    {
        enableCRT = !enableCRT;
        
        if (postProcessVolume != null)
        {
            postProcessVolume.enabled = enableCRT;
            Debug.Log($"CRT Effect {(enableCRT ? "enabled" : "disabled")}");
        }
        else
        {
            Debug.LogWarning("No Post-Process Volume found to toggle!");
        }
    }
    
    [ContextMenu("Test CRT Material Properties")]
    public void TestCRTMaterialProperties()
    {
        if (crtMaterial == null)
        {
            Debug.LogError("CRT Material not assigned!");
            return;
        }
        
        Debug.Log("=== CRT MATERIAL PROPERTIES ===");
        Debug.Log($"Material: {crtMaterial.name}");
        Debug.Log($"Shader: {crtMaterial.shader.name}");
        
        // Test all properties
        if (crtMaterial.HasProperty("_ScanlineIntensity"))
        {
            float scanlineIntensity = crtMaterial.GetFloat("_ScanlineIntensity");
            Debug.Log($"Scanline Intensity: {scanlineIntensity}");
        }
        
        if (crtMaterial.HasProperty("_DistortionStrength"))
        {
            float distortionStrength = crtMaterial.GetFloat("_DistortionStrength");
            Debug.Log($"Distortion Strength: {distortionStrength}");
        }
        
        if (crtMaterial.HasProperty("_AberrationOffset"))
        {
            float aberrationOffset = crtMaterial.GetFloat("_AberrationOffset");
            Debug.Log($"Aberration Offset: {aberrationOffset}");
        }
        
        Debug.Log("=== END CRT MATERIAL PROPERTIES ===");
    }
    
    [ContextMenu("Increase CRT Intensity")]
    public void IncreaseCRTIntensity()
    {
        if (crtMaterial != null && crtMaterial.HasProperty("_ScanlineIntensity"))
        {
            float currentIntensity = crtMaterial.GetFloat("_ScanlineIntensity");
            float newIntensity = Mathf.Min(1.0f, currentIntensity + 0.1f);
            crtMaterial.SetFloat("_ScanlineIntensity", newIntensity);
            Debug.Log($"Increased CRT scanline intensity to: {newIntensity}");
        }
    }
    
    [ContextMenu("Decrease CRT Intensity")]
    public void DecreaseCRTIntensity()
    {
        if (crtMaterial != null && crtMaterial.HasProperty("_ScanlineIntensity"))
        {
            float currentIntensity = crtMaterial.GetFloat("_ScanlineIntensity");
            float newIntensity = Mathf.Max(0.0f, currentIntensity - 0.1f);
            crtMaterial.SetFloat("_ScanlineIntensity", newIntensity);
            Debug.Log($"Decreased CRT scanline intensity to: {newIntensity}");
        }
    }
}
