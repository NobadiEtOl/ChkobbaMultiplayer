using Unity.Netcode;
using UnityEngine;

public class NetworkButtons : MonoBehaviour
{
    public void StartServer()
    {
        NetworkManager.Singleton.StartServer();
    }

    public void JoinClient()
    {
        NetworkManager.Singleton.StartClient();
    }
}
