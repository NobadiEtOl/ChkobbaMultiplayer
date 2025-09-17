using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MaterialValidator : MonoBehaviour
{
    [ContextMenu("Validate Resources Materials")]
    public void ValidateResourcesMaterials()
    {
        Debug.Log("=== MATERIAL VALIDATION ===");
        
        // Test FrameShader Material
        Material frameMaterial = Resources.Load<Material>("FrameShader_Material");
        if (frameMaterial != null)
        {
            Debug.Log($"✓ FrameShader_Material loaded successfully");
            Debug.Log($"  - Shader: {frameMaterial.shader.name}");
            Debug.Log($"  - Has _Color3Intensity: {frameMaterial.HasProperty("_Color3Intensity")}");
            Debug.Log($"  - Has _Color3: {frameMaterial.HasProperty("_Color3")}");
            Debug.Log($"  - Has _Color3AnimationSpeed: {frameMaterial.HasProperty("_Color3AnimationSpeed")}");
            
            if (frameMaterial.HasProperty("_Color3Intensity"))
            {
                float intensity = frameMaterial.GetFloat("_Color3Intensity");
                Debug.Log($"  - _Color3Intensity value: {intensity}");
            }
        }
        else
        {
            Debug.LogError("✗ FrameShader_Material not found in Resources!");
        }
        
        // Test CRT Material
        Material crtMaterial = Resources.Load<Material>("CRTScreen_Material");
        if (crtMaterial != null)
        {
            Debug.Log($"✓ CRTScreen_Material loaded successfully");
            Debug.Log($"  - Shader: {crtMaterial.shader.name}");
            Debug.Log($"  - Has _ScanlineIntensity: {crtMaterial.HasProperty("_ScanlineIntensity")}");
            Debug.Log($"  - Has _DistortionStrength: {crtMaterial.HasProperty("_DistortionStrength")}");
            Debug.Log($"  - Has _AberrationOffset: {crtMaterial.HasProperty("_AberrationOffset")}");
            
            if (crtMaterial.HasProperty("_ScanlineIntensity"))
            {
                float intensity = crtMaterial.GetFloat("_ScanlineIntensity");
                Debug.Log($"  - _ScanlineIntensity value: {intensity}");
            }
        }
        else
        {
            Debug.LogError("✗ CRTScreen_Material not found in Resources!");
        }
        
        Debug.Log("=== END MATERIAL VALIDATION ===");
    }
    
    [ContextMenu("Test Material Loading in Build")]
    public void TestMaterialLoadingInBuild()
    {
        Debug.Log("=== BUILD MATERIAL TEST ===");
        
        // This simulates what happens in a build
        try
        {
            Material frameMaterial = Resources.Load<Material>("FrameShader_Material");
            if (frameMaterial != null)
            {
                Debug.Log("✓ FrameShader_Material loads successfully in build simulation");
            }
            else
            {
                Debug.LogError("✗ FrameShader_Material failed to load in build simulation");
            }
            
            Material crtMaterial = Resources.Load<Material>("CRTScreen_Material");
            if (crtMaterial != null)
            {
                Debug.Log("✓ CRTScreen_Material loads successfully in build simulation");
            }
            else
            {
                Debug.LogError("✗ CRTScreen_Material failed to load in build simulation");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ Exception during material loading: {e.Message}");
        }
        
        Debug.Log("=== END BUILD MATERIAL TEST ===");
    }
    
    [ContextMenu("Check CRT Post-Process Setup")]
    public void CheckCRTPostProcessSetup()
    {
        Debug.Log("=== CRT POST-PROCESS SETUP CHECK ===");
        
        // Check if CRT post-process feature exists
        var crtFeatures = FindObjectsOfType<MonoBehaviour>();
        bool foundCRTFeature = false;
        
        foreach (var component in crtFeatures)
        {
            if (component.GetType().Name.Contains("CRTPostProcessFeature"))
            {
                foundCRTFeature = true;
                Debug.Log($"✓ Found CRT Post-Process Feature: {component.name}");
                break;
            }
        }
        
        if (!foundCRTFeature)
        {
            Debug.LogWarning("⚠ No CRT Post-Process Feature found in scene");
        }
        
        // Check render pipeline
        var pipeline = GraphicsSettings.currentRenderPipeline;
        if (pipeline != null)
        {
            Debug.Log($"✓ Render Pipeline: {pipeline.GetType().Name}");
        }
        else
        {
            Debug.LogError("✗ No Render Pipeline found! CRT requires URP.");
        }
        
        // Check post-process volume
        var volume = FindObjectOfType<Volume>();
        if (volume != null)
        {
            Debug.Log($"✓ Post-Process Volume found: {volume.name}");
            Debug.Log($"  - Enabled: {volume.enabled}");
            Debug.Log($"  - Weight: {volume.weight}");
        }
        else
        {
            Debug.LogWarning("⚠ No Post-Process Volume found");
        }
        
        Debug.Log("=== END CRT POST-PROCESS SETUP CHECK ===");
    }
}
