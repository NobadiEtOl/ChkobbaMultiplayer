using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class EmoteManager : MonoBehaviour
{
    public static EmoteManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private GameObject emoteBubblePrefab;
    [SerializeField] private float spamWindow = 5f;
    [SerializeField] private int maxEmotesInWindow = 3;
    [SerializeField] private float blockDuration = 15f;
    [SerializeField] private EmojiKeyboardUI emojiKeyboard;

    private List<float> emoteTimestamps = new List<float>();
    private bool isBlocked = false;
    private float blockEndTime = 0f;

    private Dictionary<int, GameObject> activeBubbles = new Dictionary<int, GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (emojiKeyboard != null)
            {
                emojiKeyboard.OnEmojiSelected += OnEmojiSelected;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (isBlocked && Time.time > blockEndTime)
        {
            isBlocked = false;
        }
    }

    public void RequestEmote()
    {
        Debug.Log("[EmoteManager] RequestEmote() called.");
        if (isBlocked)
        {
            Debug.Log("[EmoteManager] Request blocked due to spam protection.");
            ShowBlockWarning();
            return;
        }

        int localPlayerNo = DeckController.LocalInstance != null ? DeckController.LocalInstance.thisPlayerNumber : -1;
        
        // Default to 0 universally when seat is not assigned yet so player can use the emote system locally instantly
        if (localPlayerNo == -1) 
        {
            Debug.Log("[EmoteManager] Local seat not assigned yet. Defaulting playerNo to 0.");
            localPlayerNo = 0;
        }

        if (activeBubbles.ContainsKey(localPlayerNo) && activeBubbles[localPlayerNo] != null)
        {
            Debug.Log("[EmoteManager] Request ignored: an emote is already active for this player.");
            return;
        }

        // Open keyboard
        if (emojiKeyboard != null)
        {
            Debug.Log("[EmoteManager] Opening custom emoji keyboard...");
            emojiKeyboard.OpenKeyboard();
        }
        else
        {
            Debug.LogError("[EmoteManager] emojiKeyboard reference is missing!");
        }
    }

    private void OnEmojiSelected(string input)
    {
        Debug.Log($"[EmoteManager] OnEmojiSelected(input: '{input}')");
        if (string.IsNullOrEmpty(input)) 
        {
            Debug.Log("[EmoteManager] Input is null or empty. Aborting.");
            return;
        }

        // Extract first emoji/character
        string emoji = ExtractFirstEmoji(input);
        Debug.Log($"[EmoteManager] Extracted emoji: '{emoji}'");
        if (string.IsNullOrEmpty(emoji)) 
        {
            Debug.Log("[EmoteManager] Extracted emoji is empty. Aborting.");
            return;
        }

        // Check Spam
        if (CheckSpam())
        {
            Debug.Log("[EmoteManager] Spam detected! Blocking feature.");
            BlockFeature();
            return;
        }

        // Handle Send
        int localPlayerNo = DeckController.LocalInstance != null ? DeckController.LocalInstance.thisPlayerNumber : -1;
        int targetPlayerNo = localPlayerNo == -1 ? 0 : localPlayerNo;

        // 1. ALWAYS show our own emote locally immediately for instant feedback
        Debug.Log($"[EmoteManager] Showing emote '{emoji}' locally for player {targetPlayerNo}.");
        ShowEmote(targetPlayerNo, emoji);

        // 2. If we are in a networked session and our seat is assigned, sync it with other players
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && localPlayerNo != -1)
        {
            if (GameNetworkRelay.Instance != null)
            {
                Debug.Log($"[EmoteManager] Sending emoji '{emoji}' to network for player {localPlayerNo}.");
                GameNetworkRelay.Instance.SendEmoteServerRPC(localPlayerNo, emoji);
            }
            else
            {
                Debug.LogError("[EmoteManager] Network relay instance is missing!");
            }
        }
    }

    private string ExtractFirstEmoji(string input)
    {
        // Simple extraction: take the first element (could be a surrogate pair)
        if (string.IsNullOrEmpty(input)) return "";
        
        // Use StringInfo to handle surrogate pairs (emojis)
        System.Globalization.TextElementEnumerator enumerator = System.Globalization.StringInfo.GetTextElementEnumerator(input);
        if (enumerator.MoveNext())
        {
            return enumerator.GetTextElement();
        }
        return "";
    }

    private bool CheckSpam()
    {
        float now = Time.time;
        emoteTimestamps.Add(now);

        // Remove old timestamps
        emoteTimestamps.RemoveAll(t => now - t > spamWindow);

        return emoteTimestamps.Count > maxEmotesInWindow;
    }

    private void BlockFeature()
    {
        Debug.Log($"[EmoteManager] Blocking emote feature for {blockDuration} seconds.");
        isBlocked = true;
        blockEndTime = Time.time + blockDuration;
        ShowBlockWarning();
    }

    private void ShowBlockWarning()
    {
        if (UIFeedbackManager.Instance != null)
        {
            UIFeedbackManager.Instance.ShowFeedback("Çok fazla ifade gönderdiğiniz için geçici olarak engellendiniz.");
        }
    }

    public void ShowEmote(int playerNo, string emoji)
    {
        Debug.Log($"[EmoteManager] ShowEmote(playerNo: {playerNo}, emoji: '{emoji}')");
        // Find Slot Parent in SideManager
        GameObject slotParent = FindSlotParentForPlayer(playerNo);
        if (slotParent == null) 
        {
            Debug.LogWarning($"[EmoteManager] Could not find UI slot for player {playerNo}.");
            return;
        }

        // If bubble already exists for this player, destroy it to avoid overlap (though logic should prevent it)
        if (activeBubbles.ContainsKey(playerNo) && activeBubbles[playerNo] != null)
        {
            Destroy(activeBubbles[playerNo]);
        }

        // Spawn bubble
        GameObject bubble = Instantiate(emoteBubblePrefab, slotParent.transform);
        activeBubbles[playerNo] = bubble;

        // Position it (over the base image)
        RectTransform rt = bubble.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero; 

        // Set Emoji
        EmoteBubble eb = bubble.GetComponent<EmoteBubble>();
        if (eb != null)
        {
            eb.SetEmote(emoji);
        }
    }

    private GameObject FindSlotParentForPlayer(int playerNo)
    {
        if (SideManager.Instance == null) return null;

        // Fallback: If seat is not assigned yet (-1) or DeckController doesn't exist,
        // map playerNo (0 or -1) to the local slot (Side 0, Slot 1).
        int localPlayerNo = -1;
        if (DeckController.LocalInstance != null)
        {
            localPlayerNo = DeckController.LocalInstance.thisPlayerNumber;
        }

        if (localPlayerNo == -1)
        {
            if (playerNo == -1 || playerNo == 0)
            {
                return GetSideManagerSlotParent(0, 1); // Side 0, Slot 1 is ALWAYS the local player!
            }
            return null;
        }

        int playerCount = DeckController.LocalInstance.playerCount;
        bool is2v2 = (playerCount == 4);

        // Side 0: My Team (Slot 1 is always the local player)
        if (playerNo == localPlayerNo) return GetSideManagerSlotParent(0, 1);
        
        if (is2v2)
        {
            int teammateNo = (localPlayerNo + 2) % 4;
            if (playerNo == teammateNo) return GetSideManagerSlotParent(0, 2);

            int opp1No = (localPlayerNo + 1) % 4;
            int opp2No = (localPlayerNo + 3) % 4;
            if (playerNo == opp1No) return GetSideManagerSlotParent(1, 1);
            if (playerNo == opp2No) return GetSideManagerSlotParent(1, 2);
        }
        else
        {
            int oppNo = (localPlayerNo + 1) % 2;
            if (playerNo == oppNo) return GetSideManagerSlotParent(1, 1);
        }

        return null;
    }

    private GameObject GetSideManagerSlotParent(int sideIndex, int slotIndex)
    {
        // Use reflection to access private side fields if necessary, or I'll modify SideManager to expose them.
        // For now, I'll assume I'll add a helper method to SideManager.
        return SideManager.Instance.GetPlayerSlotParent(sideIndex, slotIndex);
    }
}
