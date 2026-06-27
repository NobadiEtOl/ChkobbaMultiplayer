using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public NetworkVariable<PlayerCustomData> CustomData = new NetworkVariable<PlayerCustomData>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> AbsolutePlayerNumber = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    void Start()
    {
        Debug.Log($"[Player] ===== ÖNEMLİ: PLAYER START CALLED =====\n" +
                 $"IsOwner: {IsOwner}\n" +
                 $"LocalClientId: {NetworkManager.Singleton?.LocalClientId ?? 0}\n" +
                 $"GameManager.LocalInstance: {(GameManager.LocalInstance != null ? "FOUND" : "NULL")}");

        CustomData.OnValueChanged += OnCustomDataChanged;
        AbsolutePlayerNumber.OnValueChanged += OnPlayerNumberChanged;

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

            LoadAndSendCustomization();
        }
    }

    void Update()
    {
        if (IsOwner)
        {
            if (DeckController.LocalInstance != null)
            {
                int currentSeat = DeckController.LocalInstance.thisPlayerNumber;
                if (currentSeat != -1 && AbsolutePlayerNumber.Value != currentSeat)
                {
                    Debug.Log($"[Player] Owner detected seat number changed to {currentSeat}. Submitting to Server.");
                    SubmitPlayerNumberServerRpc(currentSeat);
                }
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Trigger visual updates upon spawning in case the variables were already initialized
        UpdateSideVisuals();
    }

    public override void OnDestroy()
    {
        CustomData.OnValueChanged -= OnCustomDataChanged;
        AbsolutePlayerNumber.OnValueChanged -= OnPlayerNumberChanged;
        base.OnDestroy();
    }

    private void LoadAndSendCustomization()
    {
        string pName = PlayerPrefs.GetString("PlayerName", "Player");
        int cardBack = PlayerPrefs.GetInt("CardBackIndex", 0);
        int baseFace = PlayerPrefs.GetInt("AvatarBaseFaceIndex", 0);
        int hair = PlayerPrefs.GetInt("AvatarHairIndex", 0);
        int eyes = PlayerPrefs.GetInt("AvatarEyesIndex", 0);
        int eyebrows = PlayerPrefs.GetInt("AvatarEyebrowsIndex", 0);
        int mouth = PlayerPrefs.GetInt("AvatarMouthIndex", 0);

        PlayerCustomData localData = new PlayerCustomData
        {
            PlayerName = pName,
            CardBackIndex = cardBack,
            BaseFaceIndex = baseFace,
            HairIndex = hair,
            EyesIndex = eyes,
            EyebrowsIndex = eyebrows,
            MouthIndex = mouth
        };

        SubmitCustomizationServerRpc(localData);

        // Map client ID to player seat number (assigned by Server during connection flow)
        if (DeckController.LocalInstance != null)
        {
            int seat = DeckController.LocalInstance.thisPlayerNumber;
            if (seat != -1)
            {
                SubmitPlayerNumberServerRpc(seat);
            }
        }
    }

    [ServerRpc]
    public void SubmitCustomizationServerRpc(PlayerCustomData data)
    {
        CustomData.Value = data;
    }

    [ServerRpc]
    public void SubmitPlayerNumberServerRpc(int playerNum)
    {
        AbsolutePlayerNumber.Value = playerNum;
    }

    private void OnCustomDataChanged(PlayerCustomData oldVal, PlayerCustomData newVal)
    {
        UpdateSideVisuals();
    }

    private void OnPlayerNumberChanged(int oldVal, int newVal)
    {
        UpdateSideVisuals();
    }

    private void UpdateSideVisuals()
    {
        if (SideManager.Instance != null)
        {
            SideManager.Instance.UpdateAllSides();
        }
    }
}

