using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CRTSetupGuide : MonoBehaviour
{
    [Header("CRT Setup Instructions")]
    [TextArea(10, 20)]
    public string setupInstructions = @"CRT EFFECT SETUP GUIDE:

1. CREATE MATERIAL:
   - Right-click in Project → Create → Material
   - Name it 'CRT_Material'
   - Set Shader to 'Custom/CRTScreenURP'

2. CONFIGURE URP PIPELINE:
   - Go to Project Settings → Graphics
   - Set Scriptable Render Pipeline Settings to your URP Asset
   - Open your URP Asset (e.g., High_PipelineAsset)
   - Go to Renderer List
   - Add your Forward Renderer (e.g., High_PipelineAsset_ForwardRenderer)

3. ADD CRT FEATURE TO RENDERER:
   - Select your Forward Renderer asset
   - In Inspector, find 'Renderer Features'
   - Click '+' to add new feature
   - Select 'CRT Full Screen Feature' or 'CRT Post Process Feature'
   - Assign your CRT_Material to the feature

4. TEST THE EFFECT:
   - Play the scene
   - You should see CRT scanlines and distortion
   - Adjust material properties to fine-tune the effect

5. MOBILE OPTIMIZATION:
   - The effect automatically reduces resolution on mobile
   - Adjust Scanline Intensity for better visibility
   - Test on actual mobile device for best results

TROUBLESHOOTING:
- If no effect: Check material is assigned to feature
- If performance issues: Reduce Scanline Intensity
- If build errors: Add shader to Always Included Shaders";

    [ContextMenu("Open Project Settings")]
    public void OpenProjectSettings()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.ExecuteMenuItem("Edit/Project Settings...");
        #endif
    }

    [ContextMenu("Check CRT Setup")]
    public void CheckCRTSetup()
    {
        Debug.Log("=== CRT SETUP CHECK ===");
        
        // Check render pipeline
        var pipeline = GraphicsSettings.currentRenderPipeline;
        if (pipeline != null)
        {
            Debug.Log($"✓ Render Pipeline: {pipeline.GetType().Name}");
        }
        else
        {
            Debug.LogError("✗ No Render Pipeline found! Please set up URP in Project Settings > Graphics");
            return;
        }

        // Check if CRT shader exists
        Shader crtShader = Shader.Find("Custom/CRTScreenURP");
        if (crtShader != null)
        {
            Debug.Log($"✓ CRT Shader found: {crtShader.name}");
        }
        else
        {
            Debug.LogError("✗ CRT Shader not found! Make sure it's compiled correctly.");
        }

        // Check for CRT materials
        Material[] allMaterials = Resources.FindObjectsOfTypeAll<Material>();
        bool foundCRTMaterial = false;
        
        foreach (var mat in allMaterials)
        {
            if (mat.shader != null && mat.shader.name == "Custom/CRTScreenURP")
            {
                Debug.Log($"✓ CRT Material found: {mat.name}");
                foundCRTMaterial = true;
            }
        }
        
        if (!foundCRTMaterial)
        {
            Debug.LogWarning("⚠ No CRT Material found! Create a material using Custom/CRTScreenURP shader.");
        }

        // Check for CRT features in renderers
        var renderers = Resources.FindObjectsOfTypeAll<UniversalRendererData>();
        bool foundCRTFeature = false;
        
        foreach (var renderer in renderers)
        {
            foreach (var feature in renderer.rendererFeatures)
            {
                if (feature != null && (feature.GetType().Name.Contains("CRT")))
                {
                    Debug.Log($"✓ CRT Feature found in renderer: {renderer.name}");
                    foundCRTFeature = true;
                }
            }
        }
        
        if (!foundCRTFeature)
        {
            Debug.LogWarning("⚠ No CRT Feature found in renderers! Add CRT Full Screen Feature to your Forward Renderer.");
        }

        Debug.Log("=== END CRT SETUP CHECK ===");
    }

    [ContextMenu("Create CRT Material")]
    public void CreateCRTMaterial()
    {
        #if UNITY_EDITOR
        // Find the CRT shader
        Shader crtShader = Shader.Find("Custom/CRTScreenURP");
        if (crtShader == null)
        {
            Debug.LogError("CRT Shader not found! Make sure it's compiled correctly.");
            return;
        }

        // Create material
        Material crtMaterial = new Material(crtShader);
        crtMaterial.name = "CRT_Material";
        
        // Set default values
        crtMaterial.SetFloat("_ScanlineIntensity", 0.5f);
        crtMaterial.SetFloat("_DistortionStrength", 0.1f);
        crtMaterial.SetFloat("_AberrationOffset", 0.002f);

        // Save to Assets folder
        string path = "Assets/CRT_Material.mat";
        UnityEditor.AssetDatabase.CreateAsset(crtMaterial, path);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();

        Debug.Log($"✓ Created CRT Material at: {path}");
        #else
        Debug.LogWarning("This function only works in the Unity Editor!");
        #endif
    }
}
