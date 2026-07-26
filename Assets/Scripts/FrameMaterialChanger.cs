using UnityEngine;

public class FrameMaterialChanger : MonoBehaviour
{
    [SerializeField] private Material frameShaderMaterial;
    
    [ContextMenu("Apply FrameShader Material to All Frames")]
    public void ApplyFrameShaderMaterialToAllFrames()
    {
        if (frameShaderMaterial == null)
        {
            
            return;
        }

        // Find the ElHolderScript to get the frame objects
        ElHolderScript elHolder = FindObjectOfType<ElHolderScript>();
        if (elHolder == null)
        {
            
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
                    
                }
                else
                {
                    
                }
            }
        }

        
    }

    [ContextMenu("Test Turn Indication")]
    public void TestTurnIndication()
    {
        ElHolderScript elHolder = FindObjectOfType<ElHolderScript>();
        if (elHolder != null)
        {
            // Test with player 0
            elHolder.UpdateCurrentPlayer(0);
            
        }
        else
        {
            
        }
    }

    [ContextMenu("Reset All Frames")]
    public void ResetAllFrames()
    {
        ElHolderScript elHolder = FindObjectOfType<ElHolderScript>();
        if (elHolder != null)
        {
            elHolder.ReturnAllHandsToIdle();
            
        }
        else
        {
            
        }
    }
} 