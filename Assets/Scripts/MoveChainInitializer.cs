using UnityEngine;

/// <summary>
/// Simple component to initialize the move chain system
/// Add this to your scene to automatically set up move chain tracking
/// </summary>
public class MoveChainInitializer : MonoBehaviour
{
    [Header("Move Chain Settings")]
    [SerializeField] private bool autoInitialize = true;
    [SerializeField] private bool enableLogging = true;
    
    void Start()
    {
        if (autoInitialize)
        {
            InitializeMoveChainSystem();
        }
    }
    
    /// <summary>
    /// Initializes the move chain system by adding integrator components
    /// </summary>
    public void InitializeMoveChainSystem()
    {
        // Add MoveChainIntegrator to this GameObject if it doesn't exist
        var integrator = GetComponent<MoveChainIntegrator>();
        if (integrator == null)
        {
            integrator = gameObject.AddComponent<MoveChainIntegrator>();
            if (enableLogging)
            {
                Debug.Log("[MoveChainInitializer] Added MoveChainIntegrator to scene");
            }
        }
        
        // The integrator will automatically set up the tracking components on Server and GameManager
        if (enableLogging)
        {
            Debug.Log("[MoveChainInitializer] Move chain system initialized successfully");
        }
    }
    
    [ContextMenu("Initialize Move Chain System")]
    public void ManualInitialize()
    {
        InitializeMoveChainSystem();
    }
}
