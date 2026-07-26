using UnityEngine;

/// <summary>
/// Performance controller for StarShader variants
/// Automatically adjusts shader settings based on device performance
/// </summary>
public class StarShaderPerformanceController : MonoBehaviour
{
    [Header("Performance Settings")]
    [SerializeField] private bool autoDetectPerformance = true;
    [SerializeField] private PerformanceLevel targetPerformance = PerformanceLevel.Balanced;
    
    [Header("Shader References")]
    [SerializeField] private Material starShaderMaterial;
    [SerializeField] private Material starShaderOptimizedMaterial;
    [SerializeField] private Material starShaderMobileMaterial;
    
    [Header("Frame Rate Settings")]
    [SerializeField] private bool enableFrameRateLimit = true;
    [SerializeField] private int targetFrameRate = 30;
    [SerializeField] private int animationFrameRate = 24;
    
    [Header("Performance Monitoring")]
    [SerializeField] private bool showPerformanceStats = false;
    [SerializeField] private float performanceCheckInterval = 2.0f;
    
    private float lastPerformanceCheck;
    private float averageFPS;
    private int frameCount;
    private float frameTime;
    
    public enum PerformanceLevel
    {
        High,        // Original shader - best quality, worst performance
        Balanced,    // Optimized shader - good quality, good performance  
        Mobile       // Mobile shader - basic quality, best performance
    }
    
    private void Start()
    {
        if (autoDetectPerformance)
        {
            DetectDevicePerformance();
        }
        
        ApplyPerformanceSettings();
        
        if (enableFrameRateLimit)
        {
            Application.targetFrameRate = targetFrameRate;
        }
    }
    
    private void Update()
    {
        if (showPerformanceStats)
        {
            UpdatePerformanceStats();
        }
        
        if (autoDetectPerformance && Time.time - lastPerformanceCheck > performanceCheckInterval)
        {
            CheckAndAdjustPerformance();
            lastPerformanceCheck = Time.time;
        }
    }
    
    private void DetectDevicePerformance()
    {
        // Simple device performance detection
        int systemMemory = SystemInfo.systemMemorySize;
        int graphicsMemory = SystemInfo.graphicsMemorySize;
        string deviceModel = SystemInfo.deviceModel;
        
        
        
        
        
        
        // Basic performance classification
        if (systemMemory >= 4000 && graphicsMemory >= 1000)
        {
            targetPerformance = PerformanceLevel.High;
            
        }
        else if (systemMemory >= 2000 && graphicsMemory >= 500)
        {
            targetPerformance = PerformanceLevel.Balanced;
            
        }
        else
        {
            targetPerformance = PerformanceLevel.Mobile;
            
        }
    }
    
    private void ApplyPerformanceSettings()
    {
        Material targetMaterial = null;
        
        switch (targetPerformance)
        {
            case PerformanceLevel.High:
                targetMaterial = starShaderMaterial;
                animationFrameRate = 60;
                break;
                
            case PerformanceLevel.Balanced:
                targetMaterial = starShaderOptimizedMaterial;
                animationFrameRate = 30;
                break;
                
            case PerformanceLevel.Mobile:
                targetMaterial = starShaderMobileMaterial;
                animationFrameRate = 20;
                break;
        }
        
        if (targetMaterial != null)
        {
            // Apply the material to all renderers using star shader
            ApplyMaterialToRenderers(targetMaterial);
            
            // Set animation frame rate
            targetMaterial.SetFloat("_AnimationFrameRate", animationFrameRate);
            
            
        }
    }
    
    private void ApplyMaterialToRenderers(Material newMaterial)
    {
        // Find all renderers using star shader and replace the material
        Renderer[] renderers = FindObjectsOfType<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            if (renderer.material != null)
            {
                string shaderName = renderer.material.shader.name;
                if (shaderName.Contains("StarShader"))
                {
                    renderer.material = newMaterial;
                }
            }
        }
    }
    
    private void CheckAndAdjustPerformance()
    {
        if (averageFPS < 25 && targetPerformance != PerformanceLevel.Mobile)
        {
            
            targetPerformance = (PerformanceLevel)((int)targetPerformance + 1);
            ApplyPerformanceSettings();
        }
        else if (averageFPS > 50 && targetPerformance != PerformanceLevel.High)
        {
            
            targetPerformance = (PerformanceLevel)((int)targetPerformance - 1);
            ApplyPerformanceSettings();
        }
    }
    
    private void UpdatePerformanceStats()
    {
        frameCount++;
        frameTime += Time.unscaledDeltaTime;
        
        if (frameTime >= 1.0f)
        {
            averageFPS = frameCount / frameTime;
            frameCount = 0;
            frameTime = 0;
        }
    }
    
    // Public methods for manual control
    public void SetPerformanceLevel(PerformanceLevel level)
    {
        targetPerformance = level;
        autoDetectPerformance = false;
        ApplyPerformanceSettings();
    }
    
    public void SetAnimationFrameRate(int fps)
    {
        animationFrameRate = Mathf.Clamp(fps, 12, 60);
        
        // Apply to current material
        Material currentMaterial = GetCurrentMaterial();
        if (currentMaterial != null)
        {
            currentMaterial.SetFloat("_AnimationFrameRate", animationFrameRate);
        }
    }
    
    public void ToggleEmissionLines(bool enable)
    {
        Material currentMaterial = GetCurrentMaterial();
        if (currentMaterial != null && currentMaterial.HasProperty("_EnableEmission"))
        {
            currentMaterial.SetFloat("_EnableEmission", enable ? 1.0f : 0.0f);
        }
    }
    
    public void ToggleSmokeEffect(bool enable)
    {
        Material currentMaterial = GetCurrentMaterial();
        if (currentMaterial != null && currentMaterial.HasProperty("_EnableSmoke"))
        {
            currentMaterial.SetFloat("_EnableSmoke", enable ? 1.0f : 0.0f);
        }
    }
    
    private Material GetCurrentMaterial()
    {
        Renderer[] renderers = FindObjectsOfType<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer.material != null && renderer.material.shader.name.Contains("StarShader"))
            {
                return renderer.material;
            }
        }
        return null;
    }
    
    // GUI for runtime performance control
    private void OnGUI()
    {
        if (!showPerformanceStats) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label($"FPS: {averageFPS:F1}");
        GUILayout.Label($"Performance Level: {targetPerformance}");
        GUILayout.Label($"Animation FPS: {animationFrameRate}");
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("High Performance"))
            SetPerformanceLevel(PerformanceLevel.High);
        if (GUILayout.Button("Balanced Performance"))
            SetPerformanceLevel(PerformanceLevel.Balanced);
        if (GUILayout.Button("Mobile Performance"))
            SetPerformanceLevel(PerformanceLevel.Mobile);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Toggle Emission Lines"))
        {
            Material currentMaterial = GetCurrentMaterial();
            if (currentMaterial != null && currentMaterial.HasProperty("_EnableEmission"))
            {
                float currentValue = currentMaterial.GetFloat("_EnableEmission");
                ToggleEmissionLines(currentValue < 0.5f);
            }
        }
        
        if (GUILayout.Button("Toggle Smoke Effect"))
        {
            Material currentMaterial = GetCurrentMaterial();
            if (currentMaterial != null && currentMaterial.HasProperty("_EnableSmoke"))
            {
                float currentValue = currentMaterial.GetFloat("_EnableSmoke");
                ToggleSmokeEffect(currentValue < 0.5f);
            }
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
