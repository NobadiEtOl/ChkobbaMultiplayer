using System;
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
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button hostTwoPlayerButton;
    [SerializeField] private Button hostFourPlayerButton;
    [SerializeField] private Button startGameTwoPlayerButton;
    [SerializeField] private Button startGameFourPlayerButton;
    [SerializeField] private InputField inputField;
    [SerializeField] private Text joinCodeText;
    private string joinCodeVar;

    void Awake()
    {
        startButton.onClick.AddListener(() => { Server.Singleton.StartGameAfterDelay(); });
        clientButton.onClick.AddListener(async () => { await StartClientWithRelay(); });
        hostTwoPlayerButton.onClick.AddListener(async () => {await StartHostWithRelay(2); });
        hostFourPlayerButton.onClick.AddListener(async () => {await StartHostWithRelay(4); });
        startGameTwoPlayerButton.onClick.AddListener(async () => { await FindLobbiesAndStartHostIfNoneExist(2); });
        startGameFourPlayerButton.onClick.AddListener(async () => { await FindLobbiesAndStartHostIfNoneExist(4); });

        //FindLobbiesAndStartHostIfNoneExist(4);
    }

    bool isListeningFlag = false;
    void Update()
    {
        if (NetworkManager.Singleton.IsListening && isListeningFlag == false)
        {
            Debug.LogWarning("IsListening");
            isListeningFlag = true;
        }
    }

    private string GetUniquePlayerId()
    {
        // Generate a unique ID per tab and store it in PlayerPrefs
        string uniquePlayerId = PlayerPrefs.GetString("UniquePlayerId", null);

        if (string.IsNullOrEmpty(uniquePlayerId))
        {
            uniquePlayerId = Guid.NewGuid().ToString();
            PlayerPrefs.SetString("UniquePlayerId", uniquePlayerId);
        }

        Debug.Log($"Unique Player ID for this tab: {uniquePlayerId}");
        return uniquePlayerId;
    }

    public async Task<string> StartHostWithRelay(int playerCount)
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(playerCount);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "wss"));
        joinCodeVar = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        joinCodeText.text = joinCodeVar;

        CreateLobbyOptions options = new CreateLobbyOptions
        {
            Data = new Dictionary<string, DataObject>
            {
                { "JoinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCodeVar) }
            }
        };

        await Lobbies.Instance.CreateLobbyAsync("MyLobby", playerCount, options);
        Debug.Log($"Host started with join code: {joinCodeVar}");
        Server.Singleton.SetPlayerCount(playerCount);
        return NetworkManager.Singleton.StartHost() ? joinCodeVar : null;
    }

    public async Task<bool> StartClientWithRelay()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        string inputJoinCode = inputField.text;
        Debug.Log("Trying to join with joinCode: " + inputJoinCode);
        joinCodeText.text = inputJoinCode;

        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode: inputJoinCode);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "wss"));

        return !string.IsNullOrEmpty(inputJoinCode) && NetworkManager.Singleton.StartClient();
    }

    public async Task FindLobbiesAndStartHostIfNoneExist(int playerCount)
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        QueryLobbiesOptions queryOptions = new QueryLobbiesOptions
        {
            Filters = new List<QueryFilter>
            {
                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT), // Lobbies with available slots
                new QueryFilter(QueryFilter.FieldOptions.MaxPlayers, playerCount.ToString(), QueryFilter.OpOptions.EQ)
            }
        };

        var lobbies = await Lobbies.Instance.QueryLobbiesAsync(queryOptions);

        if (lobbies.Results.Count > 0)
        {
            Debug.Log($"Found {lobbies.Results.Count} open lobbies for {playerCount} players.");
            foreach (var lobby in lobbies.Results)
            {
                Debug.Log($"Lobby Name: {lobby.Name}, Player Count: {playerCount}, Available Slots: {lobby.AvailableSlots}");
            }

            // Join the first lobby found
            if (lobbies.Results.Count > 0)
            {
                JoinLobby(lobbies.Results[0].Id);
            }
        }
        else
        {
            Debug.Log($"No open lobbies for {playerCount} players found. Creating a new one...");
            await StartHostWithRelay(playerCount);
        }

        Server.Singleton.StartGameAfterDelay();
    }


    public async void JoinLobby(string lobbyId)
    {
        try
        {
            var lobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobbyId);

            if (lobby != null)
            {
                UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport == null)
                {
                    Debug.LogError("UnityTransport component is missing.");
                    return;
                }

                var joinCode = lobby.Data["JoinCode"].Value;
                var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode: joinCode);
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "wss"));

                NetworkManager.Singleton.StartClient();
                Debug.Log("Successfully connected to the lobby.");
            }
            else
            {
                Debug.LogError("Failed to retrieve join allocation.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error joining lobby: {e}");
        }
    }

    public void SendJoinCodeToHTML(string joinCode)
    {
        Debug.Log("Sending Join Code to HTML: " + joinCode);
        Application.ExternalCall("receiveJoinCode", joinCode);
    }
}






/*using System.Collections;
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
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;


public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button serverButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button startGameTwoPlayerButton;
    [SerializeField] private Button startGameFourPlayerButton;
    [SerializeField] private InputField inputField;
    [SerializeField] private Text joinCodeText;
    private string joinCodeVar;

    void Awake()
    {
        serverButton.onClick.AddListener(() => {Server.Singleton.SkipTurn();});
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
        }//!!!!!!!!!!!!!!!!!!!!!
    }

    public void SendJoinCodeToHTML(string joinCode)
    {
        Debug.Log("Sending Join Code to HTML: " + joinCode);
        Application.ExternalCall("receiveJoinCode", joinCode);
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
        joinCodeVar = joinCode;
        joinCodeText.text = joinCode;
        //Server.Singleton.StartGame(2);
        SendJoinCodeToHTML(joinCode);
        return NetworkManager.Singleton.StartHost() ? joinCode : null;
    }

    public async Task<bool> StartClientWithRelay(string tempJoinCode)
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Debug.Log("Trying to join with joinCode: " + tempJoinCode);
        joinCodeText.text = tempJoinCode;
        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode: tempJoinCode);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "wss"));
        return !string.IsNullOrEmpty(tempJoinCode) && NetworkManager.Singleton.StartClient();
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

}*/
