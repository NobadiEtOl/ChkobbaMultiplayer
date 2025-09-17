using UnityEngine;
using UnityEngine.Rendering;
using System.Reflection;

public class ShaderVariantHelper : MonoBehaviour
{
    [ContextMenu("Show Shader Variant Instructions")]
    public void ShowShaderVariantInstructions()
    {
        Debug.Log("=== SHADER VARIANT PROTECTION INSTRUCTIONS ===");
        Debug.Log("To prevent your shaders from being stripped in mobile builds:");
        Debug.Log("");
        Debug.Log("1. Go to: Edit > Project Settings > Graphics");
        Debug.Log("2. Find 'Always Included Shaders' section");
        Debug.Log("3. Add these shaders to the list:");
        Debug.Log("   - Custom/CRTScreenURP");
        Debug.Log("   - Custom/SimpleTwoColorLines");
        Debug.Log("");
        Debug.Log("4. Click the '+' button to add new shader slots");
        Debug.Log("5. Drag your shaders from the Project window to the slots");
        Debug.Log("6. Save the project");
        Debug.Log("");
        Debug.Log("This ensures your shaders are never stripped from builds!");
        Debug.Log("=== END INSTRUCTIONS ===");
    }
    
    [ContextMenu("Check Shader Status")]
    public void CheckShaderStatus()
    {
        Debug.Log("=== SHADER STATUS CHECK ===");
        
        // Check if shaders can be found
        Shader crtShader = Shader.Find("Custom/CRTScreenURP");
        if (crtShader != null)
        {
            Debug.Log($"✓ CRT Shader found: {crtShader.name}");
            Debug.Log($"  - Is Supported: {crtShader.isSupported}");
            Debug.Log($"  - Render Queue: {crtShader.renderQueue}");
        }
        else
        {
            Debug.LogError("✗ CRT Shader not found! Make sure it's compiled correctly.");
        }
        
        Shader frameShader = Shader.Find("Custom/SimpleTwoColorLines");
        if (frameShader != null)
        {
            Debug.Log($"✓ FrameShader found: {frameShader.name}");
            Debug.Log($"  - Is Supported: {frameShader.isSupported}");
            Debug.Log($"  - Render Queue: {frameShader.renderQueue}");
        }
        else
        {
            Debug.LogError("✗ FrameShader not found! Make sure it's compiled correctly.");
        }
        
        // Check render pipeline
        var pipeline = GraphicsSettings.currentRenderPipeline;
        if (pipeline != null)
        {
            Debug.Log($"✓ Render Pipeline: {pipeline.GetType().Name}");
        }
        else
        {
            Debug.LogError("✗ No Render Pipeline found! Please set up URP.");
        }
        
        Debug.Log("=== END SHADER STATUS CHECK ===");
        Debug.Log("Note: To check Always Included Shaders, go to Project Settings > Graphics manually.");
    }
    
    [ContextMenu("Test Shader Compilation")]
    public void TestShaderCompilation()
    {
        Debug.Log("=== SHADER COMPILATION TEST ===");
        
        // Test CRT Shader
        Shader crtShader = Shader.Find("Custom/CRTScreenURP");
        if (crtShader != null)
        {
            Debug.Log($"✓ CRT Shader found: {crtShader.name}");
            Debug.Log($"  - Is Supported: {crtShader.isSupported}");
            Debug.Log($"  - Render Queue: {crtShader.renderQueue}");
            Debug.Log($"  - LOD: {crtShader.maximumLOD}");
        }
        else
        {
            Debug.LogError("✗ CRT Shader not found!");
        }
        
        // Test FrameShader
        Shader frameShader = Shader.Find("Custom/SimpleTwoColorLines");
        if (frameShader != null)
        {
            Debug.Log($"✓ FrameShader found: {frameShader.name}");
            Debug.Log($"  - Is Supported: {frameShader.isSupported}");
            Debug.Log($"  - Render Queue: {frameShader.renderQueue}");
            Debug.Log($"  - LOD: {frameShader.maximumLOD}");
        }
        else
        {
            Debug.LogError("✗ FrameShader not found!");
        }
        
        Debug.Log("=== END SHADER COMPILATION TEST ===");
    }
}
