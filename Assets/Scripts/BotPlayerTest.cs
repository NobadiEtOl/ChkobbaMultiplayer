using UnityEngine;

/// <summary>
/// Test script to verify BotPlayer integration
/// </summary>
public class BotPlayerTest : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private bool runTestOnStart = false;
    
    private void Start()
    {
        if (runTestOnStart)
        {
            TestBotPlayerIntegration();
        }
    }
    
    /// <summary>
    /// Tests the BotPlayer integration
    /// </summary>
    [ContextMenu("Test BotPlayer Integration")]
    public void TestBotPlayerIntegration()
    {
        Debug.Log("[BotPlayerTest] Starting BotPlayer integration test...");
        
        // Test 1: Check if BotPlayer instance exists
        BotPlayer botPlayer = BotPlayer.Instance;
        if (botPlayer == null)
        {
            Debug.LogError("[BotPlayerTest] FAILED: BotPlayer.Instance is null!");
            return;
        }
        Debug.Log("[BotPlayerTest] PASSED: BotPlayer instance found");
        
        // Test 2: Check if Server can find BotPlayer
        Server server = Server.Singleton;
        if (server == null)
        {
            Debug.LogError("[BotPlayerTest] FAILED: Server.Singleton is null!");
            return;
        }
        Debug.Log("[BotPlayerTest] PASSED: Server instance found");
        
        // Test 3: Check if GameManager exists
        GameManager gameManager = GameManager.LocalInstance;
        if (gameManager == null)
        {
            Debug.LogError("[BotPlayerTest] FAILED: GameManager.LocalInstance is null!");
            return;
        }
        Debug.Log("[BotPlayerTest] PASSED: GameManager instance found");
        
        // Test 4: Check if PlayerHand3 exists
        GameObject playerHand3 = GameObject.Find("PlayerHand3");
        if (playerHand3 == null)
        {
            Debug.LogError("[BotPlayerTest] FAILED: PlayerHand3 GameObject not found!");
            return;
        }
        Debug.Log("[BotPlayerTest] PASSED: PlayerHand3 GameObject found");
        
        // Test 5: Check if BotPlayer can find cards in PlayerHand3
        CardInteraction firstCard = botPlayer.GetFirstCardFromPlayerHand3();
        if (firstCard == null)
        {
            Debug.LogWarning("[BotPlayerTest] WARNING: No cards found in PlayerHand3 (this is normal if no cards are dealt yet)");
        }
        else
        {
            Debug.Log($"[BotPlayerTest] PASSED: Found first card in PlayerHand3: {firstCard.uniqueCardInstanceID}");
        }
        
        Debug.Log("[BotPlayerTest] Integration test completed successfully!");
    }
    
    /// <summary>
    /// Tests bot activation/deactivation
    /// </summary>
    [ContextMenu("Test Bot Activation")]
    public void TestBotActivation()
    {
        Debug.Log("[BotPlayerTest] Testing bot activation...");
        
        BotPlayer botPlayer = BotPlayer.Instance;
        if (botPlayer == null)
        {
            Debug.LogError("[BotPlayerTest] BotPlayer not found!");
            return;
        }
        
        // Test activation
        botPlayer.ActivateBot();
        if (botPlayer.IsActive())
        {
            Debug.Log("[BotPlayerTest] PASSED: Bot activated successfully");
        }
        else
        {
            Debug.LogError("[BotPlayerTest] FAILED: Bot activation failed");
        }
        
        // Test deactivation
        botPlayer.DeactivateBot();
        if (!botPlayer.IsActive())
        {
            Debug.Log("[BotPlayerTest] PASSED: Bot deactivated successfully");
        }
        else
        {
            Debug.LogError("[BotPlayerTest] FAILED: Bot deactivation failed");
        }
    }
}
