using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IMoveProcessor implementation for multiplayer mode.
/// Routes card plays through the network relay RPCs (server-authoritative flow).
/// </summary>
public class MultiplayerMoveProcessor : IMoveProcessor
{
    private readonly GameNetworkRelay networkRelay;

    public bool IsActive => networkRelay != null;
    public string ModeName => "Multiplayer";

    public MultiplayerMoveProcessor(GameNetworkRelay relay)
    {
        this.networkRelay = relay;
    }

    public void ProcessCardPlay(string cardId, Dictionary<string, int[]> centerCards, int sumValue, int cardValue, int playerNumber)
    {
        Debug.Log($"[MultiplayerMoveProcessor] ProcessCardPlay: card={cardId}, cardValue={cardValue}, sumValue={sumValue}");
        GameManager.AddToDebugLog($"[MultiplayerMoveProcessor] ProcessCardPlay: card={cardId}, cardValue={cardValue}, sumValue={sumValue}");

        SerializableCard serializableCard = new SerializableCard(centerCards);
        networkRelay.PauseTurnTimerForPowerServerRPC();
        GameManager.AddToDebugLog($"[MultiplayerMoveProcessor] PauseTurnTimerForPowerServerRPC sent");

        networkRelay.SendMoveToServerRPC(cardId, serializableCard, playerNumber, sumValue);
        GameManager.AddToDebugLog($"[MultiplayerMoveProcessor] SendMoveToServerRPC sent");
    }
}
