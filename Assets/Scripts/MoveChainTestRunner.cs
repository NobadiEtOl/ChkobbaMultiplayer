using UnityEngine;
using System.Linq;

/// <summary>
/// Test runner for the move chain system
/// Use this to test desync detection in various scenarios
/// </summary>
public class MoveChainTestRunner : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool runTestsOnStart = false;
    [SerializeField] private float testDelay = 2f;
    
    [Header("Test Results")]
    [SerializeField] private bool lastTestPassed = false;
    [SerializeField] private string lastTestMessage = "";
    
    void Start()
    {
        if (runTestsOnStart)
        {
            Invoke(nameof(RunAllTests), testDelay);
        }
    }
    
    /// <summary>
    /// Runs all available tests
    /// </summary>
    [ContextMenu("Run All Tests")]
    public void RunAllTests()
    {
        Debug.Log("[MoveChainTestRunner] === STARTING MOVE CHAIN TESTS ===");
        
        // Test 1: Check if system is initialized
        TestSystemInitialization();
        
        // Test 2: Test basic move recording
        TestBasicMoveRecording();
        
        // Test 3: Test desync detection
        TestDesyncDetection();
        
        Debug.Log("[MoveChainTestRunner] === TESTS COMPLETED ===");
    }
    
    /// <summary>
    /// Test 1: Check if move chain system is properly initialized
    /// </summary>
    [ContextMenu("Test 1: System Initialization")]
    public void TestSystemInitialization()
    {
        Debug.Log("[MoveChainTestRunner] Test 1: Checking system initialization...");
        
        bool serverTrackerExists = MoveChainTracker.ServerInstance != null;
        bool clientTrackerExists = MoveChainTracker.ClientInstance != null;
        bool integratorExists = FindObjectOfType<MoveChainIntegrator>() != null;
        
        lastTestPassed = serverTrackerExists && clientTrackerExists && integratorExists;
        lastTestMessage = $"Server: {serverTrackerExists}, Client: {clientTrackerExists}, Integrator: {integratorExists}";
        
        if (lastTestPassed)
        {
            Debug.Log("[MoveChainTestRunner] ✅ Test 1 PASSED: All components initialized");
        }
        else
        {
            Debug.LogError($"[MoveChainTestRunner] ❌ Test 1 FAILED: {lastTestMessage}");
        }
    }
    
    /// <summary>
    /// Test 2: Test basic move recording functionality
    /// </summary>
    [ContextMenu("Test 2: Basic Move Recording")]
    public void TestBasicMoveRecording()
    {
        Debug.Log("[MoveChainTestRunner] Test 2: Testing basic move recording...");
        
        if (MoveChainTracker.ClientInstance == null)
        {
            Debug.LogError("[MoveChainTestRunner] ❌ Test 2 FAILED: Client tracker not found");
            lastTestPassed = false;
            lastTestMessage = "Client tracker not found";
            return;
        }
        
        // Record a test move
        var testMove = GameMove.CreateCardPlayMove(0, 0, "test_card_0", new int[] { 1, 5 }, new string[0], 0);
        MoveChainTracker.ClientInstance.RecordCardPlay(0, "test_card_0", new int[] { 1, 5 }, new string[0], 0);
        
        var chain = MoveChainTracker.ClientInstance.GetCurrentChain();
        lastTestPassed = chain.chainVersion == 1;
        lastTestMessage = $"Chain version: {chain.chainVersion}";
        
        if (lastTestPassed)
        {
            Debug.Log("[MoveChainTestRunner] ✅ Test 2 PASSED: Move recorded successfully");
        }
        else
        {
            Debug.LogError($"[MoveChainTestRunner] ❌ Test 2 FAILED: {lastTestMessage}");
        }
    }
    
    /// <summary>
    /// Test 3: Test desync detection
    /// </summary>
    [ContextMenu("Test 3: Desync Detection")]
    public void TestDesyncDetection()
    {
        Debug.Log("[MoveChainTestRunner] Test 3: Testing desync detection...");
        
        if (MoveChainTracker.ClientInstance == null || MoveChainTracker.ServerInstance == null)
        {
            Debug.LogError("[MoveChainTestRunner] ❌ Test 3 FAILED: Trackers not found");
            lastTestPassed = false;
            lastTestMessage = "Trackers not found";
            return;
        }
        
        // Create a desync by adding a move only to client
        MoveChainTracker.ClientInstance.RecordCardPlay(0, "desync_test_card", new int[] { 2, 7 }, new string[0], 0);
        
        // Get chains
        var clientChain = MoveChainTracker.ClientInstance.GetCurrentChain();
        var serverChain = MoveChainTracker.ServerInstance.GetCurrentChain();
        
        // Validate (this should detect the desync)
        var validationResult = clientChain.ValidateAgainst(serverChain, out int mismatchIndex);
        bool isValid = validationResult == MoveChain.ValidationResult.Valid;
        
        lastTestPassed = !isValid; // We expect this to fail (desync detected)
        lastTestMessage = $"Desync detected: {!isValid}, Mismatch at: {mismatchIndex}, Result: {validationResult}";
        
        if (lastTestPassed)
        {
            Debug.Log("[MoveChainTestRunner] ✅ Test 3 PASSED: Desync correctly detected");
        }
        else
        {
            Debug.LogWarning($"[MoveChainTestRunner] ⚠️ Test 3: Desync not detected as expected: {lastTestMessage}");
        }
    }
    
    /// <summary>
    /// Test 4: Test superpower effect tracking
    /// </summary>
    [ContextMenu("Test 4: Superpower Effect Tracking")]
    public void TestSuperpowerEffectTracking()
    {
        Debug.Log("[MoveChainTestRunner] Test 4: Testing superpower effect tracking...");
        
        if (MoveChainTracker.ClientInstance == null)
        {
            Debug.LogError("[MoveChainTestRunner] ❌ Test 4 FAILED: Client tracker not found");
            lastTestPassed = false;
            lastTestMessage = "Client tracker not found";
            return;
        }
        
        // Record a superpower effect
        var effectData = new System.Collections.Generic.Dictionary<string, string>
        {
            ["oldValue"] = "3",
            ["newValue"] = "11",
            ["effectType"] = "cardValueChange"
        };
        
        MoveChainTracker.ClientInstance.RecordSuperpowerEffect(0, "Test Kapkaç", new[] { "test_card_effect" }, effectData);
        
        var chain = MoveChainTracker.ClientInstance.GetCurrentChain();
        var superpowerEffects = chain.ToList().Count(m => m.moveType == GameMove.MoveType.SuperPower_Effect);
        
        lastTestPassed = superpowerEffects > 0;
        lastTestMessage = $"Superpower effects recorded: {superpowerEffects}";
        
        if (lastTestPassed)
        {
            Debug.Log("[MoveChainTestRunner] ✅ Test 4 PASSED: Superpower effect tracked successfully");
        }
        else
        {
            Debug.LogError($"[MoveChainTestRunner] ❌ Test 4 FAILED: {lastTestMessage}");
        }
    }
    
    /// <summary>
    /// Cleans up test data
    /// </summary>
    [ContextMenu("Cleanup Test Data")]
    public void CleanupTestData()
    {
        Debug.Log("[MoveChainTestRunner] Cleaning up test data...");
        
        if (MoveChainTracker.ClientInstance != null)
        {
            MoveChainTracker.ClientInstance.ResetChain();
        }
        
        if (MoveChainTracker.ServerInstance != null)
        {
            MoveChainTracker.ServerInstance.ResetChain();
        }
        
        Debug.Log("[MoveChainTestRunner] Test data cleaned up");
    }
    
    /// <summary>
    /// Shows current test status
    /// </summary>
    [ContextMenu("Show Test Status")]
    public void ShowTestStatus()
    {
        Debug.Log($"[MoveChainTestRunner] === TEST STATUS ===");
        Debug.Log($"[MoveChainTestRunner] Last Test Passed: {lastTestPassed}");
        Debug.Log($"[MoveChainTestRunner] Last Test Message: {lastTestMessage}");
        
        if (MoveChainTracker.ClientInstance != null)
        {
            var clientChain = MoveChainTracker.ClientInstance.GetCurrentChain();
            Debug.Log($"[MoveChainTestRunner] Client Chain Version: {clientChain.chainVersion}");
        }
        
        if (MoveChainTracker.ServerInstance != null)
        {
            var serverChain = MoveChainTracker.ServerInstance.GetCurrentChain();
            Debug.Log($"[MoveChainTestRunner] Server Chain Version: {serverChain.chainVersion}");
        }
    }
}
