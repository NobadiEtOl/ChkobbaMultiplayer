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
    public bool RunsPostMoveBackgroundCheck => false;

    public SingleplayerMoveProcessor(SinglePlayerModeController controller)
    {
        this.controller = controller;
    }

    public void ProcessCardPlay(string cardId, Dictionary<string, int[]> centerCards, int sumValue, int cardValue, int playerNumber)
    {
        
        GameManager.AddToDebugLog($"[SingleplayerMoveProcessor] ProcessCardPlay: card={cardId}, cardValue={cardValue}, sumValue={sumValue}");

        controller.ValidateAndProcessPlayerMove(cardId, centerCards, sumValue, cardValue);
    }

    public void ProcessAddToCenter(string cardId, int[] cardKindValue)
    {
        
        GameManager.AddToDebugLog($"[SingleplayerMoveProcessor] ProcessAddToCenter: card={cardId}");
        controller.ProcessPlayerAddToCenter(cardId, cardKindValue);
    }
}
