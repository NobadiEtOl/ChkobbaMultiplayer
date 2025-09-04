using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class MainUIScript : MonoBehaviour
{
    [SerializeField]private ProfileScript profileScript;
    [SerializeField] private GameObject startingScreenUI;
    [SerializeField] private GameObject waitingScreenUI;
    [SerializeField] private GameObject quickPlayUI;
    [SerializeField] private GameObject createRoomUI;
    [SerializeField] private GameObject findRoomUI;
    [SerializeField] private GameObject profileUI;
    [SerializeField] private GameObject[] currentMode1v1;
    [SerializeField] private GameObject[] currentMode2v2;
    [SerializeField] private GameObject currentModeYellow;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnQuickPlayButtonClicked()
    {
        quickPlayUI.SetActive(true);
    }
    public void OnCreateRoomButtonClicked()
    {
        createRoomUI.SetActive(true);
    }
    public void OnFindRoomButtonClicked()
    {
        findRoomUI.SetActive(true);
    }

    public void OnProfileButtonClicked()
    {
        profileUI.SetActive(true);
        gameObject.GetComponent<ProfileScript>().UpdateMatchCountText();
    }

    public void OnQuickPlayCloseButtonClicked()
    {
        Debug.Log("Quick Play Close Button Clicked");
        quickPlayUI.SetActive(false);
    }
    public void OnCreateRoomCloseButtonClicked()
    {
        createRoomUI.SetActive(false);
    }
    public void OnFindRoomCloseButtonClicked()
    {
        findRoomUI.SetActive(false);
    }

    public void OnProfileCloseButtonClicked()
    {
        profileUI.SetActive(false);
        profileScript.ResetCardBackShowcase();
        profileScript.ResetProfilePicShowcase();
    }

    public void OpenWaitingScreenUI(string color, string playerCount, string joinCode)
    {
        if(startingScreenUI.activeSelf)startingScreenUI.SetActive(false);
        waitingScreenUI.SetActive(true);

        if(color == "red")
        {
            if(playerCount == "2")
            {
                currentMode1v1[0].SetActive(true);
            }
            else if(playerCount == "4")
            {
                currentMode2v2[0].SetActive(true);
            }
        }
        else if(color == "blue")
        {
            if (playerCount == "2")
            {
                currentMode1v1[1].SetActive(true);
                currentMode1v1[1].transform.GetChild(0).gameObject.GetComponent<Text>().text = joinCode;
            }
            else if (playerCount == "4")
            {
                currentMode2v2[1].SetActive(true);
                currentMode2v2[1].transform.GetChild(0).gameObject.GetComponent<Text>().text = joinCode;
            }
        }
        else if(color == "yellow")
        {
            currentModeYellow.SetActive(true);
            currentModeYellow.transform.GetChild(0).gameObject.GetComponent<Text>().text = joinCode;
        }
    }

    /// <summary>
    /// Context menu method to return to main page while properly closing all network connections
    /// </summary>
    [ContextMenu("Return to Main Page (Close All Connections)")]
    public void ReturnToMainPage()
    {
        Debug.Log("[MainUIScript] Returning to main page and closing all network connections...");
        
        // STEP 1: Close all UI popups/pages
        CloseAllUIPages();
        
        // STEP 2: Disconnect from network if connected
        DisconnectFromNetwork();
        
        // STEP 3: Reset UI state to main page
        ResetToMainPage();
        
        // STEP 4: Clear any pending network operations
        ClearPendingNetworkOperations();
        
        Debug.Log("[MainUIScript] Successfully returned to main page with clean network state");
    }

    /// <summary>
    /// Closes all open UI pages and popups
    /// </summary>
    private void CloseAllUIPages()
    {
        Debug.Log("[MainUIScript] Closing all UI pages...");
        
        // Close all popup UIs
        quickPlayUI.SetActive(false);
        createRoomUI.SetActive(false);
        findRoomUI.SetActive(false);
        profileUI.SetActive(false);
        waitingScreenUI.SetActive(false);
        
        // Close all mode-specific UIs
        foreach (var mode in currentMode1v1)
        {
            if (mode != null) mode.SetActive(false);
        }
        
        foreach (var mode in currentMode2v2)
        {
            if (mode != null) mode.SetActive(false);
        }
        
        if (currentModeYellow != null) currentModeYellow.SetActive(false);
        
        Debug.Log("[MainUIScript] All UI pages closed");
    }

    /// <summary>
    /// Properly disconnects from any active network connections
    /// </summary>
    private void DisconnectFromNetwork()
    {
        Debug.Log("[MainUIScript] Disconnecting from network...");
        
        // STEP 1: Disconnect from lobby services first
        var networkManagerUI = FindObjectOfType<NetworkManagerUI>();
        if (networkManagerUI != null)
        {
            Debug.Log("[MainUIScript] Found NetworkManagerUI, disconnecting from lobby services...");
            networkManagerUI.DisconnectFromLobbyAndNetwork();
        }
        
        // STEP 2: Shutdown NetworkManager if it's running
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsListening)
            {
                Debug.Log("[MainUIScript] NetworkManager is listening, shutting down...");
                
                if (NetworkManager.Singleton.IsHost)
                {
                    Debug.Log("[MainUIScript] Shutting down as Host...");
                    NetworkManager.Singleton.Shutdown();
                }
                else if (NetworkManager.Singleton.IsClient)
                {
                    Debug.Log("[MainUIScript] Shutting down as Client...");
                    NetworkManager.Singleton.Shutdown();
                }
                else if (NetworkManager.Singleton.IsServer)
                {
                    Debug.Log("[MainUIScript] Shutting down as Server...");
                    NetworkManager.Singleton.Shutdown();
                }
            }
            else
            {
                Debug.Log("[MainUIScript] NetworkManager is not listening, no need to shutdown");
            }
        }
        else
        {
            Debug.Log("[MainUIScript] NetworkManager.Singleton is null");
        }
        
        // STEP 3: Also try to find and disconnect from any NetworkRelay
        var networkRelay = FindObjectOfType<NetworkRelay>();
        if (networkRelay != null)
        {
            Debug.Log("[MainUIScript] Found NetworkRelay, ensuring clean state");
            // NetworkRelay will be cleaned up when NetworkManager shuts down
        }
        
        Debug.Log("[MainUIScript] Network disconnection complete");
    }

    /// <summary>
    /// Resets the UI to show only the main starting screen
    /// </summary>
    private void ResetToMainPage()
    {
        Debug.Log("[MainUIScript] Resetting to main page...");
        
        // Show the main starting screen
        startingScreenUI.SetActive(true);
        
        // Ensure all other screens are hidden
        waitingScreenUI.SetActive(false);
        quickPlayUI.SetActive(false);
        createRoomUI.SetActive(false);
        findRoomUI.SetActive(false);
        profileUI.SetActive(false);
        
        Debug.Log("[MainUIScript] UI reset to main page complete");
    }

    /// <summary>
    /// Clears any pending network operations and resets network state
    /// </summary>
    private void ClearPendingNetworkOperations()
    {
        Debug.Log("[MainUIScript] Clearing pending network operations...");
        
        // CRITICAL FIX: Do NOT reset server state during disconnection
        // This was causing connectedPlayerCount to be reset to 0 before OnClientDisconnected() could run
        // The server needs to maintain its state for proper disconnection handling
        if (Server.Singleton != null)
        {
            Debug.Log("[MainUIScript] NOT resetting Server state during disconnection - server handles its own cleanup");
            // Server.Singleton.ResetAllServerVariables(); // COMMENTED OUT - causes double counting bug
        }
        
        // Reset any game manager state if it exists
        if (GameManager.LocalInstance != null)
        {
            Debug.Log("[MainUIScript] Resetting GameManager state...");
            GameManager.LocalInstance.ResetForNewRound();
        }
        
        // Reset move chains if they exist
        try
        {
            MoveChainIntegrator.ResetChains();
            Debug.Log("[MainUIScript] Move chains reset");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[MainUIScript] Could not reset move chains: {e.Message}");
        }
        
        // Also reset the connection count manually to ensure clean state
        if (Server.Singleton != null)
        {
            Debug.Log("[MainUIScript] Manually resetting Server connection count...");
            Server.Singleton.ResetConnectionCount();
        }
        
        Debug.Log("[MainUIScript] Pending network operations cleared");
    }

    /// <summary>
    /// Public method that can be called from UI buttons to return to main page
    /// </summary>
    public void OnReturnToMainPageButtonClicked()
    {
        ReturnToMainPage();
    }

    /// <summary>
    /// Called when a disconnect is detected by NetworkManagerUI
    /// </summary>
    public void OnDisconnectDetected()
    {
        Debug.Log("[MainUIScript] Disconnect detected - returning to main page");
        
        // CRITICAL FIX: Do NOT call ReturnToMainPage() which resets server state
        // Just close the UI and let the server handle the disconnection properly
        CloseAllUIPages();
        ResetToMainPage();
        
        // DO NOT call ClearPendingNetworkOperations() - this resets server state!
        // The server will handle the disconnection in OnClientDisconnected()
    }
}
