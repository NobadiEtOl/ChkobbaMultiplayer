using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button serverButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button objectButton;
    
    private void Awake()
    {
        serverButton.onClick.AddListener(() => {NetworkManager.Singleton.StartServer();});
        clientButton.onClick.AddListener(() => {NetworkManager.Singleton.StartClient();});
        hostButton.onClick.AddListener(() => {NetworkManager.Singleton.StartHost();});
        objectButton.onClick.AddListener(() => {Server.Singleton.StartGame();});
    }
}
