using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IMoveProcessor implementation for singleplayer mode.
/// Routes card plays directly to SinglePlayerModeController — no RPC calls.
/// </summary>
public class SingleplayerMoveProcessor : IMoveProcessor
{
    private readonly SinglePlayerModeController controller;

    public bool IsActive => controller != null;
    public string ModeName => "Singleplayer";

    public SingleplayerMoveProcessor(SinglePlayerModeController controller)
    {
        this.controller = controller;
    }

    public void ProcessCardPlay(string cardId, Dictionary<string, int[]> centerCards, int sumValue, int cardValue, int playerNumber)
    {
        Debug.Log($"[SingleplayerMoveProcessor] ProcessCardPlay: card={cardId}, cardValue={cardValue}, sumValue={sumValue}");
        GameManager.AddToDebugLog($"[SingleplayerMoveProcessor] ProcessCardPlay: card={cardId}, cardValue={cardValue}, sumValue={sumValue}");

        controller.ValidateAndProcessPlayerMove(cardId, centerCards, sumValue, cardValue);
    }
}
