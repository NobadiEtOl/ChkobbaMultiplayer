using UnityEngine;

public class FrameMaterialChanger : MonoBehaviour
{
    [SerializeField] private Material frameShaderMaterial;
    
    [ContextMenu("Apply FrameShader Material to All Frames")]
    public void ApplyFrameShaderMaterialToAllFrames()
    {
        if (frameShaderMaterial == null)
        {
            Debug.LogError("FrameShader Material is not assigned!");
            return;
        }

        // Find the ElHolderScript to get the frame objects
        ElHolderScript elHolder = FindObjectOfType<ElHolderScript>();
        if (elHolder == null)
        {
            Debug.LogError("ElHolderScript not found in scene!");
            return;
        }

        int appliedCount = 0;
        foreach (GameObject frameObject in elHolder.frameObjects)
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
                else
                {
                    Debug.LogWarning($"No Renderer component found on: {frameObject.name}");
                }
            }
        }

        Debug.Log($"Successfully applied FrameShader material to {appliedCount} frame objects.");
    }

    [ContextMenu("Test Turn Indication")]
    public void TestTurnIndication()
    {
        ElHolderScript elHolder = FindObjectOfType<ElHolderScript>();
        if (elHolder != null)
        {
            // Test with player 0
            elHolder.UpdateCurrentPlayer(0);
            Debug.Log("Testing turn indication for player 0");
        }
        else
        {
            Debug.LogError("ElHolderScript not found!");
        }
    }

    [ContextMenu("Reset All Frames")]
    public void ResetAllFrames()
    {
        ElHolderScript elHolder = FindObjectOfType<ElHolderScript>();
        if (elHolder != null)
        {
            elHolder.ReturnAllHandsToIdle();
            Debug.Log("Reset all frames to idle state");
        }
        else
        {
            Debug.LogError("ElHolderScript not found!");
        }
    }
} 