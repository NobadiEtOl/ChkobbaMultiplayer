using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
    void Start()
    {
        Debug.Log("Player started");
        if (IsOwner)
        {
            if (GameManager.LocalInstance != null)
                GameManager.LocalInstance.NotifyConnection();
            else Debug.LogError("GameManager.LocalInstance is null in Player.Start");
        }
    }
}
