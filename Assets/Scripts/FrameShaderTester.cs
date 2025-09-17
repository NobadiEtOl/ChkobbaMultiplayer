using UnityEngine;

public class FrameShaderTester : MonoBehaviour
{
    [Header("Frame Shader Testing")]
    [SerializeField] private ElHolderScript elHolderScript;
    [SerializeField] private int testPlayerNumber = 0;
    [SerializeField] private float testDuration = 3f;
    
    private bool isTesting = false;
    
    void Start()
    {
        if (elHolderScript == null)
        {
            elHolderScript = FindObjectOfType<ElHolderScript>();
        }
    }
    
    [ContextMenu("Test FrameShader Turn Indication")]
    public void TestFrameShaderTurnIndication()
    {
        if (elHolderScript == null)
        {
            Debug.LogError("ElHolderScript not found!");
            return;
        }
        
        if (isTesting)
        {
            Debug.Log("Test already running, please wait...");
            return;
        }
        
        StartCoroutine(TestTurnIndicationCoroutine());
    }
    
    private System.Collections.IEnumerator TestTurnIndicationCoroutine()
    {
        isTesting = true;
        Debug.Log($"Starting FrameShader turn indication test for player {testPlayerNumber}");
        
        // Test turn indication
        elHolderScript.SetFrameBackgroundToWhite(testPlayerNumber);
        Debug.Log($"Frame {testPlayerNumber} should now be highlighted with green Color3");
        
        yield return new WaitForSeconds(testDuration);
        
        // Reset turn indication
        elHolderScript.ResetFrameBackgroundColor(testPlayerNumber);
        Debug.Log($"Frame {testPlayerNumber} should now be reset to original colors");
        
        yield return new WaitForSeconds(1f);
        
        // Test with different player
        int nextPlayer = (testPlayerNumber + 1) % 4;
        elHolderScript.SetFrameBackgroundToWhite(nextPlayer);
        Debug.Log($"Frame {nextPlayer} should now be highlighted with green Color3");
        
        yield return new WaitForSeconds(testDuration);
        
        elHolderScript.ResetFrameBackgroundColor(nextPlayer);
        Debug.Log($"Frame {nextPlayer} should now be reset to original colors");
        
        isTesting = false;
        Debug.Log("FrameShader turn indication test completed!");
    }
    
    [ContextMenu("Check Frame Materials")]
    public void CheckFrameMaterials()
    {
        if (elHolderScript == null)
        {
            Debug.LogError("ElHolderScript not found!");
            return;
        }
        
        Debug.Log("=== FRAME MATERIAL CHECK ===");
        
        for (int i = 0; i < elHolderScript.frameObjects.Count; i++)
        {
            GameObject frame = elHolderScript.frameObjects[i];
            if (frame != null)
            {
                Renderer renderer = frame.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = renderer.material;
                    Debug.Log($"Frame {i} ({frame.name}):");
                    Debug.Log($"  - Shader: {mat.shader.name}");
                    Debug.Log($"  - Material: {mat.name}");
                    
                    // Check for FrameShader properties
                    if (mat.HasProperty("_Color3Intensity"))
                    {
                        float intensity = mat.GetFloat("_Color3Intensity");
                        Debug.Log($"  - _Color3Intensity: {intensity}");
                    }
                    else
                    {
                        Debug.LogWarning($"  - Missing _Color3Intensity property!");
                    }
                    
                    if (mat.HasProperty("_Color3"))
                    {
                        Color color3 = mat.GetColor("_Color3");
                        Debug.Log($"  - _Color3: {color3}");
                    }
                    else
                    {
                        Debug.LogWarning($"  - Missing _Color3 property!");
                    }
                }
                else
                {
                    Debug.LogWarning($"Frame {i} ({frame.name}) has no Renderer component!");
                }
            }
            else
            {
                Debug.LogWarning($"Frame {i} is null!");
            }
        }
        
        Debug.Log("=== END FRAME MATERIAL CHECK ===");
    }
    
    [ContextMenu("Force Apply FrameShader Material")]
    public void ForceApplyFrameShaderMaterial()
    {
        if (elHolderScript == null)
        {
            Debug.LogError("ElHolderScript not found!");
            return;
        }
        
        // Find the FrameShader material
        Material frameShaderMaterial = Resources.Load<Material>("FrameShader_Material");
        if (frameShaderMaterial == null)
        {
            #if UNITY_EDITOR
            // Try to find it in the project
            string[] guids = UnityEditor.AssetDatabase.FindAssets("FrameShader_Material t:Material");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                frameShaderMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
            }
            #endif
        }
        
        if (frameShaderMaterial == null)
        {
            Debug.LogError("FrameShader_Material not found! Please assign it manually.");
            return;
        }
        
        int appliedCount = 0;
        foreach (GameObject frameObject in elHolderScript.frameObjects)
        {
            if (frameObject != null)
            {
                Renderer renderer = frameObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = frameShaderMaterial;
                    appliedCount++;
                    Debug.Log($"Applied FrameShader material to: {frameObject.name}");
                }
            }
        }
        
        Debug.Log($"Successfully applied FrameShader material to {appliedCount} frame objects.");
    }
}
