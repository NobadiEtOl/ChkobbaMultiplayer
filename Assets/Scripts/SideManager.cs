using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections.Generic;
using TMPro;

public class SideManager : MonoBehaviour
{
    public static SideManager Instance { get; private set; }

    [System.Serializable]
    public struct LayeredAvatarUI
    {
        public GameObject PlayerSlotParent; // Parent object for the player slot (contains name and avatar image stack)
        public TextMeshProUGUI PlayerNameText; // The text displaying the player's name

        [Header("Layered Image Stack (Same order as Customization Window)")]
        public Image BaseFaceImage;
        public Image HairImage;
        public Image EyesImage;
        public Image EyebrowsImage;
        public Image MouthImage;
    }

    [System.Serializable]
    public struct SideUI
    {
        public GameObject SidePanelParent; // Parent container for the entire side panel
        public Text SidePointsText;         // Points text for this team/side
        public LayeredAvatarUI PlayerSlot1; // Slot for player 1 (always active)
        public LayeredAvatarUI PlayerSlot2; // Slot for player 2 (active only in 2v2 mode)
        public GameObject PlayerBackground1; // Background parent for player 1 (just in case)
        public GameObject PlayerBackground2; // Background parent for player 2 (active only in 2v2 mode)
    }

    [Header("Side Specific UI Panels")]
    [SerializeField] private SideUI side0LocalTeam; // Left or Bottom panel displaying local/friendly team details
    [SerializeField] private SideUI side1OpponentTeam; // Right or Top panel displaying opponent team details

    [Header("Customization Sprite Assets (Must match AvatarCustomization arrays exactly)")]
    [SerializeField] private Sprite[] faceBaseSprites;
    [SerializeField] private Sprite[] hairSprites;
    [SerializeField] private Sprite[] eyesSprites;
    [SerializeField] private Sprite[] eyebrowsSprites;
    [SerializeField] private Sprite[] mouthSprites;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("SideManager: Another instance already exists! Destroying duplicate on " + gameObject.name);
            Destroy(this);
        }
    }

    private void Start()
    {
        UpdateAllSides();
    }

    /// <summary>
    /// Synchronizes point display on both side panels dynamically.
    /// </summary>
    public void UpdatePoints(int point0, int point1)
    {
        if (DeckController.LocalInstance == null) return;
        int localPlayerNo = DeckController.LocalInstance.thisPlayerNumber;

        // Perspective-aware team point mapping
        if (localPlayerNo == 0 || localPlayerNo == 2)
        {
            if (side0LocalTeam.SidePointsText != null) side0LocalTeam.SidePointsText.text = point0.ToString();
            if (side1OpponentTeam.SidePointsText != null) side1OpponentTeam.SidePointsText.text = point1.ToString();
        }
        else if (localPlayerNo == 1 || localPlayerNo == 3)
        {
            if (side0LocalTeam.SidePointsText != null) side0LocalTeam.SidePointsText.text = point1.ToString();
            if (side1OpponentTeam.SidePointsText != null) side1OpponentTeam.SidePointsText.text = point0.ToString();
        }
    }

    /// <summary>
    /// Updates all nameplates, avatars, and slots for both sides.
    /// This is called reactively whenever a player joins, reconnects, or updates their profile.
    /// </summary>
    public void UpdateAllSides()
    {
        if (DeckController.LocalInstance == null) return;

        int localPlayerNo = DeckController.LocalInstance.thisPlayerNumber;
        int playerCount = DeckController.LocalInstance.playerCount;

        // If local seat hasn't been assigned yet, wait until assigned
        if (localPlayerNo == -1) return;

        // Find all active networked Player objects spawned in the game
        Player[] allNetworkPlayers = Object.FindObjectsByType<Player>(FindObjectsSortMode.None);
        Dictionary<int, Player> playerMap = new Dictionary<int, Player>();

        foreach (Player p in allNetworkPlayers)
        {
            int seat = p.AbsolutePlayerNumber.Value;
            if (seat != -1)
            {
                playerMap[seat] = p;
            }
        }

        // Determine current mode (1v1 or 2v2)
        bool is2v2 = (playerCount == 4);

        // Configure Side 0 (My Team Side)
        ConfigureSide0(localPlayerNo, is2v2, playerMap);

        // Configure Side 1 (Opponent Team Side)
        ConfigureSide1(localPlayerNo, is2v2, playerMap);
    }

    private void ConfigureSide0(int localPlayerNo, bool is2v2, Dictionary<int, Player> playerMap)
    {
        // Handle backgrounds activation
        if (side0LocalTeam.PlayerBackground1 != null)
        {
            side0LocalTeam.PlayerBackground1.SetActive(true);
        }

        // Slot 1 is always the local player
        if (playerMap.TryGetValue(localPlayerNo, out Player localPlayer))
        {
            SetSlotData(side0LocalTeam.PlayerSlot1, localPlayer.CustomData.Value);
        }
        else
        {
            // Show local player's cached data from PlayerPrefs if the NetworkVariable isn't ready
            SetSlotDataFromPlayerPrefs(side0LocalTeam.PlayerSlot1);
        }
        side0LocalTeam.PlayerSlot1.PlayerSlotParent.SetActive(true);

        // Slot 2 is active only in 2v2 (My teammate is (localPlayerNo + 2) % 4)
        if (is2v2)
        {
            if (side0LocalTeam.PlayerBackground2 != null)
            {
                side0LocalTeam.PlayerBackground2.SetActive(true);
            }

            int teammateNo = (localPlayerNo + 2) % 4;
            if (playerMap.TryGetValue(teammateNo, out Player teammate))
            {
                SetSlotData(side0LocalTeam.PlayerSlot2, teammate.CustomData.Value);
                side0LocalTeam.PlayerSlot2.PlayerSlotParent.SetActive(true);
            }
            else
            {
                // Teammate not connected/not spawned yet - display empty/disconnected slot or bot
                SetSlotToEmpty(side0LocalTeam.PlayerSlot2, teammateNo);
            }
        }
        else
        {
            if (side0LocalTeam.PlayerBackground2 != null)
            {
                side0LocalTeam.PlayerBackground2.SetActive(false);
            }

            // Disable second slot in 1v1 mode
            side0LocalTeam.PlayerSlot2.PlayerSlotParent.SetActive(false);
        }
    }

    private void ConfigureSide1(int localPlayerNo, bool is2v2, Dictionary<int, Player> playerMap)
    {
        // Handle backgrounds activation
        if (side1OpponentTeam.PlayerBackground1 != null)
        {
            side1OpponentTeam.PlayerBackground1.SetActive(true);
        }

        if (is2v2)
        {
            if (side1OpponentTeam.PlayerBackground2 != null)
            {
                side1OpponentTeam.PlayerBackground2.SetActive(true);
            }

            // 2v2 opponents are (localPlayerNo + 1) % 4 and (localPlayerNo + 3) % 4
            int opp1No = (localPlayerNo + 1) % 4;
            int opp2No = (localPlayerNo + 3) % 4;

            // Opponent 1
            if (playerMap.TryGetValue(opp1No, out Player opp1))
            {
                SetSlotData(side1OpponentTeam.PlayerSlot1, opp1.CustomData.Value);
            }
            else
            {
                SetSlotToEmpty(side1OpponentTeam.PlayerSlot1, opp1No);
            }
            side1OpponentTeam.PlayerSlot1.PlayerSlotParent.SetActive(true);

            // Opponent 2
            if (playerMap.TryGetValue(opp2No, out Player opp2))
            {
                SetSlotData(side1OpponentTeam.PlayerSlot2, opp2.CustomData.Value);
            }
            else
            {
                SetSlotToEmpty(side1OpponentTeam.PlayerSlot2, opp2No);
            }
            side1OpponentTeam.PlayerSlot2.PlayerSlotParent.SetActive(true);
        }
        else
        {
            if (side1OpponentTeam.PlayerBackground2 != null)
            {
                side1OpponentTeam.PlayerBackground2.SetActive(false);
            }

            // 1v1 opponent is (localPlayerNo + 1) % 2
            int oppNo = (localPlayerNo + 1) % 2;
            if (playerMap.TryGetValue(oppNo, out Player opp))
            {
                SetSlotData(side1OpponentTeam.PlayerSlot1, opp.CustomData.Value);
            }
            else
            {
                SetSlotToEmpty(side1OpponentTeam.PlayerSlot1, oppNo);
            }
            side1OpponentTeam.PlayerSlot1.PlayerSlotParent.SetActive(true);

            // Disable second slot in 1v1 mode
            side1OpponentTeam.PlayerSlot2.PlayerSlotParent.SetActive(false);
        }
    }

    private void SetSlotData(LayeredAvatarUI slot, PlayerCustomData data)
    {
        if (slot.PlayerNameText != null)
        {
            slot.PlayerNameText.text = string.IsNullOrEmpty(data.PlayerName.ToString()) ? "Player" : data.PlayerName.ToString();
        }

        UpdateImage(slot.BaseFaceImage, faceBaseSprites, data.BaseFaceIndex);
        UpdateImage(slot.HairImage, hairSprites, data.HairIndex);
        UpdateImage(slot.EyesImage, eyesSprites, data.EyesIndex);
        UpdateImage(slot.EyebrowsImage, eyebrowsSprites, data.EyebrowsIndex);
        UpdateImage(slot.MouthImage, mouthSprites, data.MouthIndex);
    }

    private void SetSlotDataFromPlayerPrefs(LayeredAvatarUI slot)
    {
        if (slot.PlayerNameText != null)
        {
            slot.PlayerNameText.text = PlayerPrefs.GetString("PlayerName", "Player");
        }

        int baseFace = PlayerPrefs.GetInt("AvatarBaseFaceIndex", 0);
        int hair = PlayerPrefs.GetInt("AvatarHairIndex", 0);
        int eyes = PlayerPrefs.GetInt("AvatarEyesIndex", 0);
        int eyebrows = PlayerPrefs.GetInt("AvatarEyebrowsIndex", 0);
        int mouth = PlayerPrefs.GetInt("AvatarMouthIndex", 0);

        UpdateImage(slot.BaseFaceImage, faceBaseSprites, baseFace);
        UpdateImage(slot.HairImage, hairSprites, hair);
        UpdateImage(slot.EyesImage, eyesSprites, eyes);
        UpdateImage(slot.EyebrowsImage, eyebrowsSprites, eyebrows);
        UpdateImage(slot.MouthImage, mouthSprites, mouth);
    }

    private void SetSlotToEmpty(LayeredAvatarUI slot, int absoluteSeatIndex)
    {
        // If a server or controller designates this seat as a bot, display a bot face & name
        if (Server.Singleton != null && Server.Singleton.IsBotControlled(absoluteSeatIndex))
        {
            if (slot.PlayerNameText != null)
            {
                slot.PlayerNameText.text = $"CPU Player {absoluteSeatIndex + 1}";
            }

            // Procedural, deterministic bot avatar
            int baseFace = absoluteSeatIndex % Mathf.Max(1, faceBaseSprites.Length);
            int hair = (absoluteSeatIndex * 2) % Mathf.Max(1, hairSprites.Length);
            int eyes = (absoluteSeatIndex * 3) % Mathf.Max(1, eyesSprites.Length);
            int eyebrows = (absoluteSeatIndex * 4) % Mathf.Max(1, eyebrowsSprites.Length);
            int mouth = (absoluteSeatIndex * 5) % Mathf.Max(1, mouthSprites.Length);

            UpdateImage(slot.BaseFaceImage, faceBaseSprites, baseFace);
            UpdateImage(slot.HairImage, hairSprites, hair);
            UpdateImage(slot.EyesImage, eyesSprites, eyes);
            UpdateImage(slot.EyebrowsImage, eyebrowsSprites, eyebrows);
            UpdateImage(slot.MouthImage, mouthSprites, mouth);
        }
        else
        {
            if (slot.PlayerNameText != null)
            {
                slot.PlayerNameText.text = "Waiting...";
            }

            // Hide avatar layers when seat is completely empty and no bot is active
            if (slot.BaseFaceImage != null) slot.BaseFaceImage.enabled = false;
            if (slot.HairImage != null) slot.HairImage.enabled = false;
            if (slot.EyesImage != null) slot.EyesImage.enabled = false;
            if (slot.EyebrowsImage != null) slot.EyebrowsImage.enabled = false;
            if (slot.MouthImage != null) slot.MouthImage.enabled = false;
        }
    }

    private void UpdateImage(Image image, Sprite[] sprites, int index)
    {
        if (image == null) return;

        if (sprites == null || sprites.Length == 0)
        {
            image.enabled = false;
            return;
        }

        int safeIndex = Mathf.Clamp(index, 0, sprites.Length - 1);
        Sprite s = sprites[safeIndex];

        if (s != null)
        {
            image.sprite = s;
            image.enabled = true;
        }
        else
        {
            image.enabled = false;
        }
    }
}
