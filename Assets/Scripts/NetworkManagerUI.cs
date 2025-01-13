using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Networking.Transport;
using System.Threading.Tasks;


public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button serverButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button startGameTwoPlayerButton;
    [SerializeField] private Button startGameFourPlayerButton;
    [SerializeField] private InputField inputField;
    [SerializeField] private Text joinCodeText;

    void Awake()
    {
        serverButton.onClick.AddListener(async () => {await StartHostWithRelay();});
        clientButton.onClick.AddListener(async () => {await StartClientWithRelay();});
        hostButton.onClick.AddListener(async () => {await StartHostWithRelay();});
        startGameTwoPlayerButton.onClick.AddListener(() => {Server.Singleton.StartGame(2);});
        startGameFourPlayerButton.onClick.AddListener(() => {Server.Singleton.StartGame(4);});
    }

    bool isListeningFlag = false;
    void Update()
    {
        if(NetworkManager.Singleton.IsListening && isListeningFlag==false)
        {
            Debug.LogWarning("IsListening");
            isListeningFlag=true;
        }
        /*else
        {
            Debug.LogWarning("IsNotListening");
        }*/
    }
    public async Task<string> StartHostWithRelay()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "wss"));
        var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        joinCodeText.text = joinCode;
        Debug.Log("JoinCode: " + joinCode);
        return NetworkManager.Singleton.StartHost() ? joinCode : null;
    }

    public async Task<bool> StartClientWithRelay()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Debug.Log("Trying to join with joinCode: " + inputField.text);
        joinCodeText.text = inputField.text;
        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode: inputField.text);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "wss"));
        return !string.IsNullOrEmpty(inputField.text) && NetworkManager.Singleton.StartClient();
    }

    /*private async void CreateRelay()
    {
        try
        {
            Debug.LogWarning("Inside CreateRelay");

            // Initialize Unity Services and authenticate
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("Signed in successfully.");
            }
            else
            {
                Debug.LogError("Authentication failed.");
                return;
            }

            // Create the Relay allocation
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3, "europe-central2");
            if (allocation == null || allocation.RelayServer == null)
            {
                Debug.LogError("Allocation or RelayServer data is null.");
                return;
            }

            Debug.Log($"Relay server IP: {allocation.RelayServer.IpV4}, Port: {allocation.RelayServer.Port}");

            // Get the join code
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            if (string.IsNullOrEmpty(joinCode))
            {
                Debug.LogError("Failed to retrieve the join code.");
                return;
            }

            Debug.Log("JoinCode: " + joinCode);

            // Configure UnityTransport with SetHostRelayData
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("UnityTransport component is missing.");
                return;
            }

            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.ConnectionData,
                allocation.Key,
                true // Secure connection
            );

            Debug.Log("Host is about to start. Verifying Relay connection...");

            // Start the host
            if (NetworkManager.Singleton.StartHost())
            {
                Debug.Log("Host started successfully. Waiting for clients to connect...");
            }
            else
            {
                Debug.LogError("Failed to start host.");
                return;
            }

            // Optional delay to ensure initialization
            await Task.Delay(2000);

            // Verify NetworkManager state
            if (NetworkManager.Singleton.IsListening)
            {
                Debug.Log("NetworkManager is listening. Host successfully connected to the Relay server.");
            }
            else
            {
                Debug.LogError("NetworkManager is not listening. Host connection failed.");
            }

            Debug.LogWarning("Ending CreateRelay");
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("RelayServiceException: " + e.Message);
        }
    }

    private async void JoinRelay()
    {   
        string JoinCodeString = "";
        if(inputField!=null)
        {   
            JoinCodeString = inputField.text;
            Debug.Log("JoinRelay joinCode: " + JoinCodeString);
        }
        else Debug.Log("inputField problem");
        try
        {
            Debug.Log($"Attempting to join relay with join code: {JoinCodeString}");

            // Join the allocation on the relay server using the join code
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(JoinCodeString);

            // Set the client's relay data in UnityTransport
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("UnityTransport component is missing.");
                return;
            }

            transport.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            // Start the client and connect to the host via the relay
            NetworkManager.Singleton.StartClient();

            Debug.Log("Successfully connected to the relay server as a client.");
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"RelayServiceException: {e.Message}");
        }
    }*/

}
