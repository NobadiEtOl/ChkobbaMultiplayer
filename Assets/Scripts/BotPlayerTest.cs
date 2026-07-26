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
        
        
        // Test 1: Check if BotPlayer instance exists
        BotPlayer botPlayer = BotPlayer.Instance;
        if (botPlayer == null)
        {
            
            return;
        }
        
        
        // Test 2: Check if Server can find BotPlayer
        Server server = Server.Singleton;
        if (server == null)
        {
            
            return;
        }
        
        
        // Test 3: Check if GameManager exists
        GameManager gameManager = GameManager.LocalInstance;
        if (gameManager == null)
        {
            
            return;
        }
        
        
        // Test 4: Check if PlayerHand3 exists
        GameObject playerHand3 = GameObject.Find("PlayerHand3");
        if (playerHand3 == null)
        {
            
            return;
        }
        
        
        // Test 5: Check if BotPlayer can find cards in PlayerHand3
        CardInteraction firstCard = botPlayer.GetFirstCardFromPlayerHand3();
        if (firstCard == null)
        {
            
        }
        else
        {
            
        }
        
        
    }
    
    /// <summary>
    /// Tests bot activation/deactivation
    /// </summary>
    [ContextMenu("Test Bot Activation")]
    public void TestBotActivation()
    {
        
        
        BotPlayer botPlayer = BotPlayer.Instance;
        if (botPlayer == null)
        {
            
            return;
        }
        
        // Test activation
        botPlayer.ActivateBot();
        if (botPlayer.IsActive())
        {
            
        }
        else
        {
            
        }
        
        // Test deactivation
        botPlayer.DeactivateBot();
        if (!botPlayer.IsActive())
        {
            
        }
        else
        {
            
        }
    }
}
