using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Linq;

public class LobbyHostMitigator : MonoBehaviour
{
    public static LobbyHostMitigator Instance { get; private set; }

    [Header("Lobby Info")]
    public Lobby currentLobby;
    public string myLobbyPlayerId;
    public string lastHostPlayerId;
    public string lastJoinCode;

    private Coroutine lobbyPollCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[LobbyHostMitigator] Instance created and set to DontDestroyOnLoad.");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Initialize(Lobby lobby)
    {
        currentLobby = lobby;
        myLobbyPlayerId = AuthenticationService.Instance.PlayerId;
        lastHostPlayerId = lobby.Data["HostPlayerId"].Value;
        lastJoinCode = lobby.Data["JoinCode"].Value;

        Debug.Log($"[LobbyHostMitigator] Initialized. MyLobbyPlayerId: {myLobbyPlayerId}, LastHostPlayerId: {lastHostPlayerId}, LastJoinCode: {lastJoinCode}");

        if (lobbyPollCoroutine != null)
            StopCoroutine(lobbyPollCoroutine);
        lobbyPollCoroutine = StartCoroutine(PollLobbyForHostMigration());
    }

    private IEnumerator PollLobbyForHostMigration()
    {
        Debug.Log("[LobbyHostMitigator] Started polling lobby for host migration.");
        while (true)
        {
            var lobbyTask = Lobbies.Instance.GetLobbyAsync(currentLobby.Id);
            while (!lobbyTask.IsCompleted) yield return null;
            var lobby = lobbyTask.Result;

            // Detect if host left (host not in player list)
            if (!lobby.Players.Any(p => p.Id == lastHostPlayerId))
            {
                Debug.Log("[LobbyHostMitigator] Host has left the lobby! Electing new host...");
                ElectNewHostAndUpdateLobby(lobby);
            }

            string hostPlayerId = lobby.Data["HostPlayerId"].Value;
            string joinCode = lobby.Data["JoinCode"].Value;

            // If host or join code changed, act accordingly
            if (hostPlayerId != lastHostPlayerId || joinCode != lastJoinCode)
            {
                Debug.Log($"[LobbyHostMitigator] Detected host or join code change. NewHostPlayerId: {hostPlayerId}, NewJoinCode: {joinCode}");

                if (hostPlayerId == myLobbyPlayerId)
                {
                    Debug.Log("[LobbyHostMitigator] I am the new host! Starting host logic...");
                    if (!IsCurrentlyHost())
                        StartCoroutine(StartHostWithRelayCoroutine());
                }
                else if (hostPlayerId != myLobbyPlayerId && joinCode != lastJoinCode)
                {
                    Debug.Log("[LobbyHostMitigator] Host changed, reconnecting as client...");
                    StartCoroutine(ReconnectToNewHost(joinCode));
                }
                lastHostPlayerId = hostPlayerId;
                lastJoinCode = joinCode;
            }
            yield return new WaitForSeconds(2f);
        }
    }

    private void ElectNewHostAndUpdateLobby(Lobby lobby)
    {
        // Elect new host: player at index 0 in the lobby.Players list
        string newHostPlayerId = lobby.Players[0].Id;
        Debug.Log($"[LobbyHostMitigator] Electing new host. NewHostPlayerId: {newHostPlayerId}");

        if (myLobbyPlayerId == newHostPlayerId)
        {
            Debug.Log("[LobbyHostMitigator] I am the elected new host. Starting host setup...");
            StartCoroutine(StartHostWithRelayCoroutine((newJoinCode) =>
            {
                Debug.Log($"[LobbyHostMitigator] Updating lobby with new host and join code: {newJoinCode}");
                var updateOptions = new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { "JoinCode", new DataObject(DataObject.VisibilityOptions.Public, newJoinCode) },
                        { "HostPlayerId", new DataObject(DataObject.VisibilityOptions.Public, myLobbyPlayerId) }
                    }
                };
                _ = Lobbies.Instance.UpdateLobbyAsync(currentLobby.Id, updateOptions);
            }));
        }
    }

    // Dummy check, replace with your own logic
    private bool IsCurrentlyHost()
    {
        // Implement a check to see if this client is already the host
        // For example, check if NetworkManager.Singleton.IsHost (if available)
        if (NetworkManager.Singleton != null)
            return NetworkManager.Singleton.IsHost;
        return false;
    }

    // Replace with your actual relay allocation and host start logic
    private IEnumerator StartHostWithRelayCoroutine(System.Action<string> onJoinCode = null)
    {
        Debug.Log("[LobbyHostMitigator] Allocating Relay for new host...");
        // 1. Allocate Relay
        var allocationTask = RelayService.Instance.CreateAllocationAsync(currentLobby.MaxPlayers);
        while (!allocationTask.IsCompleted) yield return null;
        var allocation = allocationTask.Result;

        // 2. Get join code
        var joinCodeTask = RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        while (!joinCodeTask.IsCompleted) yield return null;
        string joinCode = joinCodeTask.Result;

        Debug.Log($"[LobbyHostMitigator] Relay allocated. Join code: {joinCode}");

        // 3. Set up transport
        NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>()
            .SetRelayServerData(new Unity.Networking.Transport.Relay.RelayServerData(allocation, "wss"));

        // 4. Start host
        Debug.Log("[LobbyHostMitigator] Starting host...");
        NetworkManager.Singleton.StartHost();

        // 5. Callback
        onJoinCode?.Invoke(joinCode);
    }

    // Replace with your actual client reconnect logic
    private IEnumerator ReconnectToNewHost(string joinCode)
    {
        Debug.Log("[LobbyHostMitigator] Reconnecting to new host...");
        // 1. Shutdown old session
        if (NetworkManager.Singleton.IsListening)
        {
            Debug.Log("[LobbyHostMitigator] Shutting down old network session...");
            NetworkManager.Singleton.Shutdown();
        }
        yield return new WaitForSeconds(1f);

        // 2. Join Relay allocation
        Debug.Log($"[LobbyHostMitigator] Joining Relay allocation with join code: {joinCode}");
        var joinTask = RelayService.Instance.JoinAllocationAsync(joinCode: joinCode);
        while (!joinTask.IsCompleted) yield return null;
        var joinAllocation = joinTask.Result;

        // 3. Set up transport
        NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>()
            .SetRelayServerData(new Unity.Networking.Transport.Relay.RelayServerData(joinAllocation, "wss"));

        // 4. Start client
        Debug.Log("[LobbyHostMitigator] Starting client...");
        NetworkManager.Singleton.StartClient();
    }
}