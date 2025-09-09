using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
    void Start()
    {
        Debug.Log($"[Player] ===== ÖNEMLİ: PLAYER START CALLED =====\n" +
                 $"IsOwner: {IsOwner}\n" +
                 $"LocalClientId: {NetworkManager.Singleton?.LocalClientId ?? 0}\n" +
                 $"GameManager.LocalInstance: {(GameManager.LocalInstance != null ? "FOUND" : "NULL")}");
        
        if (IsOwner)
        {
            if (GameManager.LocalInstance != null)
            {
                Debug.Log("[Player] Calling GameManager.NotifyConnection()");
                GameManager.LocalInstance.NotifyConnection();
            }
            else 
            {
                Debug.LogError("[Player] GameManager.LocalInstance is null in Player.Start");
            }
        }
        else
        {
            Debug.Log("[Player] Not owner - skipping NotifyConnection");
        }
    }
}
