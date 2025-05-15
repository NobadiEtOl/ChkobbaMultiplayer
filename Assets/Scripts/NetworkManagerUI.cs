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
#if UNITY_EDITOR
using ParrelSync;
#endif

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField]private MainUIScript mainUIScript;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button hostTwoPlayerButton;
    [SerializeField] private Button hostFourPlayerButton;
    [SerializeField] private Button quickPlayTwoPlayerButton;
    [SerializeField] private Button quickPlayFourPlayerButton;
    [SerializeField] private InputField inputField;
    [SerializeField] private Text joinCodeText;
    private string joinCodeVar;

    void Awake()
    {
        clientButton.onClick.AddListener(async () => { await StartClientWithRelay(); });
        hostTwoPlayerButton.onClick.AddListener(async () => {await StartHostWithRelay(2,true); });
        hostFourPlayerButton.onClick.AddListener(async () => {await StartHostWithRelay(4,true); });
        quickPlayTwoPlayerButton.onClick.AddListener(async () => { await FindLobbiesAndStartHostIfNoneExist(2); });
        quickPlayFourPlayerButton.onClick.AddListener(async () => { await FindLobbiesAndStartHostIfNoneExist(4); });

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

    public async Task EnsureFreshAnonymousSignIn()
    {
        if (AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut(); // Use synchronous version
        }
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
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

    public async Task<string> StartHostWithRelay(int playerCount,bool privateFlag)
    {
        if(privateFlag)mainUIScript.OpenWaitingScreenUI("blue", playerCount.ToString());

        var initialOptions = new InitializationOptions();

        #if UNITY_EDITOR
        if (ClonesManager.IsClone())
        {
            string parrelArgument = ClonesManager.GetArgument();
            initialOptions.SetProfile(parrelArgument);
            Debug.Log("ParrelSync argument: " + parrelArgument);
        }
        #endif

        await UnityServices.InitializeAsync(initialOptions);

        await EnsureFreshAnonymousSignIn();

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(playerCount);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "wss"));
        joinCodeVar = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        joinCodeText.text = joinCodeVar;

        CreateLobbyOptions options = new CreateLobbyOptions
        {
            IsPrivate = privateFlag, // <--- This makes the lobby joinable only by code
            Data = new Dictionary<string, DataObject>
            {
                { "JoinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCodeVar) }
            }
        };

        currentLobby = await Lobbies.Instance.CreateLobbyAsync("MyLobby", playerCount, options);
        Debug.Log($"Host started with join code: {joinCodeVar}");
        Server.Singleton.SetPlayerCount(playerCount);
        return NetworkManager.Singleton.StartHost() ? joinCodeVar : null;
    }

    public async Task<bool> StartClientWithRelay()
    {
        string inputJoinCode = inputField.text;
        mainUIScript.OpenWaitingScreenUI("yellow", inputJoinCode);

        var initialOptions = new InitializationOptions();

        #if UNITY_EDITOR
        if (ClonesManager.IsClone())
        {
            string parrelArgument = ClonesManager.GetArgument();
            initialOptions.SetProfile(parrelArgument);
            Debug.Log("ParrelSync argument: " + parrelArgument);
        }
        #endif

        await UnityServices.InitializeAsync(initialOptions);

        await EnsureFreshAnonymousSignIn();

        Debug.Log("Trying to join with joinCode: " + inputJoinCode);
        joinCodeText.text = inputJoinCode;

        // --- Join the Lobby using the join code ---
        // Find the lobby that matches the join code
        var queryOptions = new QueryLobbiesOptions
        {
            Filters = new List<QueryFilter>
            {
                new QueryFilter(QueryFilter.FieldOptions.S1, inputJoinCode, QueryFilter.OpOptions.EQ)
            }
        };
        var lobbies = await Lobbies.Instance.QueryLobbiesAsync(queryOptions);
        Lobby foundLobby = null;
        foreach (var lobby in lobbies.Results)
        {
            if (lobby.Data != null && lobby.Data.ContainsKey("JoinCode") && lobby.Data["JoinCode"].Value == inputJoinCode)
            {
                foundLobby = lobby;
                break;
            }
        }
        if (foundLobby != null)
        {
            currentLobby = await Lobbies.Instance.JoinLobbyByIdAsync(foundLobby.Id);
        }
        else
        {
            Debug.LogError("No lobby found with the provided join code.");
            return false;
        }
        // --- End join lobby section ---

        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode: inputJoinCode);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "wss"));

        return !string.IsNullOrEmpty(inputJoinCode) && NetworkManager.Singleton.StartClient();
    }

    public async Task FindLobbiesAndStartHostIfNoneExist(int playerCount)
    {
        mainUIScript.OpenWaitingScreenUI("red", playerCount.ToString());

        var initialOptions = new InitializationOptions();

        #if UNITY_EDITOR
        if (ClonesManager.IsClone())
        {
            string parrelArgument = ClonesManager.GetArgument();
            initialOptions.SetProfile(parrelArgument);
            Debug.Log("ParrelSync argument: " + parrelArgument);
        }
        #endif

        await UnityServices.InitializeAsync(initialOptions);

        await EnsureFreshAnonymousSignIn();

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
            await StartHostWithRelay(playerCount,false);
        }

        //if(playerCount==2)Server.Singleton.StartGameAfterDelayTwoPlayer();
        //if(playerCount==4)Server.Singleton.StartGameAfterDelayFourPlayer();
    }


    public async void JoinLobby(string lobbyId)
    {
        try
        {
            var lobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobbyId);
            currentLobby = lobby; // Store the current lobby

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

    private Lobby currentLobby; // Store the current lobby when you join/create it

    private async void OnApplicationQuit()
    {
        if (currentLobby != null && AuthenticationService.Instance.IsSignedIn)
        {
            try
            {
                await Lobbies.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to leave lobby on quit: " + e);
            }
        }
    }
}
