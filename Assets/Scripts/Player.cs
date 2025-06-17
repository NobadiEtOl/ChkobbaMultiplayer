using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    void Start()
    {
        GameManager.LocalInstance.NotifyConnection();
    }
}
