using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UIElements;

public class ElHolderScript : MonoBehaviour
{
    public static ElHolderScript LocalInstance; // Make it static and public
    List<Animator> animatorHands = new List<Animator>();
    private int currentActivePlayer = -1; // Track currently active player
    [SerializeField] public List<GameObject> powerTransformObjects = new List<GameObject>();
    [SerializeField] public List<GameObject> powerDictionary = new List<GameObject>();
    [SerializeField] private Transform centerTransform; // Center position for showcasing powers
    void Start()
    {
        if (LocalInstance == null)
        {
            LocalInstance = this;
        }
        else
        {
            Debug.LogWarning("ElHolderScript: LocalInstance already set, using existing instance.");
        }

        List<GameObject> handObjects = new List<GameObject>();

        foreach (Transform child in gameObject.transform)
        {
            handObjects.Add(child.gameObject);
            Animator animator = child.GetComponent<Animator>();
            if (animator != null)
            {
                animatorHands.Add(animator);
            }
        }
    }

    // Convert logical player number to hand index based on game mode
    private int GetHandIndex(int playerNumber)
    {
        // Get player count from DeckController to determine game mode
        int playerCount = DeckController.LocalInstance?.playerCount ?? 4;

        if (playerCount == 2) // 1v1 mode
        {
            // In 1v1: player 0 -> hand 0, player 1 -> hand 2 (across from each other)
            return playerNumber == 0 ? 0 : 2;
        }
        else // 2v2 mode (4 players)
        {
            // In 2v2: direct mapping player 0 -> hand 0, player 1 -> hand 1, etc.
            return playerNumber;
        }
    }

    public void UpdateCurrentPlayer(int newPlayerNumber)
    {
        // Stop current player's turn animation if there is one
        if (currentActivePlayer != -1)
        {
            int currentHandIndex = GetHandIndex(currentActivePlayer);
            if (currentHandIndex >= 0 && currentHandIndex < animatorHands.Count)
            {
                // Force immediate stop
                animatorHands[currentHandIndex].SetBool("turnLoop", false);
                animatorHands[currentHandIndex].Play("Idle", 0, 0f);
            }
        }

        // Update current player
        currentActivePlayer = newPlayerNumber;

        // Start new player's turn animation immediately
        int newHandIndex = GetHandIndex(newPlayerNumber);
        if (newHandIndex >= 0 && newHandIndex < animatorHands.Count)
        {
            animatorHands[newHandIndex].SetBool("turnLoop", true);
            // Force immediate transition to turn state
            animatorHands[newHandIndex].Play("ElPlayingAnimationClip", 0, 0f); // Replace with actual turn state name
        }
    }


    void Update()
    {

    }

    [ContextMenu("TokenActionTrue")]
    public void TokenActionTrue()
    {
        TokenActionTrue(0); // Default to player 0 for context menu
    }

    public void TokenActionTrue(int playerNumber)
    {
        if (playerNumber >= 0 && playerNumber < animatorHands.Count)
        {
            animatorHands[playerNumber].SetBool("tokenAction", true);
            // Force immediate transition to token animation
            animatorHands[playerNumber].Play("ElTokenAnimationClip", 0, 0f); // Replace with actual token state name
        }
    }


    public void OnTokenAnimationEnd(int playerNumber)
    {
        TokenActionFalse(playerNumber);
    }

    [ContextMenu("TokenActionFalse")]
    public void TokenActionFalse()
    {
        TokenActionFalse(0); // Default to player 0 for context menu
    }

    public void TokenActionFalse(int playerNumber)
    {
        if (playerNumber >= 0 && playerNumber < animatorHands.Count)
        {
            animatorHands[playerNumber].SetBool("tokenAction", false);
            animatorHands[playerNumber].Play("ElPlayingAnimationClip", 0, 0);
        }
    }

    [ContextMenu("TurnLoopTrue")]
    public void TurnActionTrue()
    {
        TurnActionTrue(0); // Default to player 0 for context menu
    }

    public void TurnActionTrue(int playerNumber)
    {
        if (playerNumber >= 0 && playerNumber < animatorHands.Count)
        {
            animatorHands[playerNumber].SetBool("turnLoop", true);
            // Force immediate transition to turn animation
            animatorHands[playerNumber].Play("ElPlayingAnimationClip", 0, 0f); // Replace with actual turn state name
        }
    }


    public void OnTurnAnimationEnd(int playerNumber)
    {
        TurnActionFalse(playerNumber);
    }

    [ContextMenu("TurnLoopFalse")]
    public void TurnActionFalse()
    {
        TurnActionFalse(0); // Default to player 0 for context menu
    }

    public void TurnActionFalse(int playerNumber)
    {
        if (playerNumber >= 0 && playerNumber < animatorHands.Count)
        {
            animatorHands[playerNumber].SetBool("turnLoop", false);
            // Force immediate transition to idle state
            animatorHands[playerNumber].Play("Idle", 0, 0f); // Replace "Idle" with your actual idle state name
        }
    }


    // Helper method to control all hands at once (if needed)
    public void TokenActionTrueAll()
    {
        for (int i = 0; i < animatorHands.Count; i++)
        {
            TokenActionTrue(i);
        }
    }

    public void TurnActionTrueAll()
    {
        for (int i = 0; i < animatorHands.Count; i++)
        {
            TurnActionTrue(i);
        }
    }

    public void ReturnAllHandsToIdle()
    {
        for (int i = 0; i < animatorHands.Count; i++)
        {
            TurnActionFalse(i);
            TokenActionFalse(i);
        }
        currentActivePlayer = -1; // Reset current active player
    }

    public void SetHandMode(int playerCount)
    {
        if (playerCount == 2)
        {
            // In 1v1 mode, deactivate hands at index 1 and 3
            if (animatorHands.Count > 1)
                animatorHands[1].gameObject.SetActive(false);
            if (animatorHands.Count > 3)
                animatorHands[3].gameObject.SetActive(false);
        }
        else if (playerCount == 4)
        {
            // In 2v2 mode, make sure all hands are active
            for (int i = 0; i < animatorHands.Count; i++)
            {
                animatorHands[i].gameObject.SetActive(true);
            }
        }
    }

    public void ShowcasePower(int playerNumber, string powerName)
    {
        Debug.Log($"Showcasing power: {powerName} for player {playerNumber}");
        int handIndex = GetHandIndex(playerNumber);
        if (playerNumber == DeckController.LocalInstance.thisPlayerNumber) return;
        powerName = powerName.Replace(" ", ""); // Remove spaces for matching

        // Bounds check for hand index
        if (handIndex < 0 || handIndex >= powerTransformObjects.Count)
        {
            Debug.LogWarning($"Invalid hand index {handIndex} for player {playerNumber}");
            return;
        }

        // Check if the hand is active (important for 1v1 mode)
        if (!powerTransformObjects[handIndex].activeInHierarchy)
        {
            Debug.LogWarning($"Hand {handIndex} is inactive, skipping power showcase");
            return;
        }

        // Find the correct power prefab by name
        GameObject powerPrefab = null;
        string targetName = powerName + "_Token";

        foreach (GameObject prefab in powerDictionary)
        {
            if (prefab != null && prefab.name.Contains(powerName))
            {
                powerPrefab = prefab;
                break;
            }
        }

        if (powerPrefab == null)
        {
            Debug.LogWarning($"Power prefab not found for: {targetName}");
            return;
        }

        StartCoroutine(ShowcasePowerCoroutine(handIndex, powerName, powerPrefab));
    }

    private IEnumerator ShowcasePowerCoroutine(int handIndex, string powerName, GameObject powerPrefab)
    {
        yield return new WaitForSeconds(0.5f);
        Debug.Log($"Showcasing power: {powerName} for hand index: {handIndex}");

        // Get the starting transform
        Transform startTransform = powerTransformObjects[handIndex].transform;

        // Create the power sprite instance
        GameObject powerInstance = Instantiate(powerPrefab);
        powerInstance.transform.SetParent(centerTransform.parent, true);
        SpriteRenderer sr = powerInstance.GetComponent<SpriteRenderer>();

        // Set initial position and scale
        powerInstance.transform.position = startTransform.position;
        powerInstance.transform.localScale = Vector3.one * 50f; // Start small

        // Get center position
        Vector3 centerPosition = centerTransform.position;
        //centerPosition.z = powerInstance.transform.position.z; // Keep same Z

        // Animation duration
        float moveDuration = 0.1f; // 70% of 1.5f
        float fadeDuration = 0.25f; // 30% of 1.5f

        Vector3 endScale = new Vector3(200, 200, 200); // End bigger

        // Create a sequence for the animations 
        Sequence powerSequence = DOTween.Sequence();

        powerSequence.AppendInterval(0.35f);

        // Move to center and scale up simultaneously
        powerSequence.Append(powerInstance.transform.DOMove(centerPosition, moveDuration));
        powerSequence.Join(powerInstance.transform.DOScale(endScale, moveDuration));

        powerSequence.AppendInterval(0);

        powerSequence.Append(powerInstance.transform.DORotate(powerInstance.transform.rotation.eulerAngles + new Vector3(0, 0, 15), 0.025f).SetLoops(2, LoopType.Yoyo));
        powerSequence.Append(powerInstance.transform.DORotate(powerInstance.transform.rotation.eulerAngles + new Vector3(0, 0, -12), 0.025f).SetLoops(2, LoopType.Yoyo));
        powerSequence.Join(powerInstance.transform.DOMove(centerPosition + new Vector3(0, 0, 170), 0.15f).SetLoops(2, LoopType.Yoyo));
        powerSequence.Append(powerInstance.transform.DORotate(powerInstance.transform.rotation.eulerAngles + new Vector3(0, 0, 13), 0.025f).SetLoops(2, LoopType.Yoyo));
        powerSequence.Append(powerInstance.transform.DORotate(powerInstance.transform.rotation.eulerAngles + new Vector3(0, 0, -16), 0.025f).SetLoops(2, LoopType.Yoyo));
        powerSequence.Join(powerInstance.transform.DOMove(centerPosition + new Vector3(0, 0, 100), 0.15f).SetLoops(2, LoopType.Yoyo));
        powerSequence.Append(powerInstance.transform.DORotate(powerInstance.transform.rotation.eulerAngles + new Vector3(0, 0, 17), 0.025f).SetLoops(2, LoopType.Yoyo));
        powerSequence.Append(powerInstance.transform.DORotate(powerInstance.transform.rotation.eulerAngles + new Vector3(0, 0, -17), 0.025f).SetLoops(2, LoopType.Yoyo));
        powerSequence.Join(powerInstance.transform.DOMove(centerPosition + new Vector3(0, 0, 50), 0.15f).SetLoops(2, LoopType.Yoyo));

        powerSequence.AppendInterval(0f);


        // Fade out
        if (sr != null)
        {
            //powerSequence.Append(sr.DOFade(0f, fadeDuration).SetEase(Ease.InQuad));
        }

        // Wait for the sequence to complete
        yield return powerSequence.WaitForCompletion();

        // Destroy the power instance
        Debug.LogError($"Destroying power instance: {powerInstance.name}");
        Destroy(powerInstance);
    }
    
    public void PlayTokenAnimationOnce(int playerNumber)
    {
        int handIndex = GetHandIndex(playerNumber);
        
        // Bounds check for hand index
        if (handIndex < 0 || handIndex >= animatorHands.Count)
        {
            Debug.LogWarning($"Invalid hand index {handIndex} for player {playerNumber}");
            return;
        }
        
        // Check if the hand is active (important for 1v1 mode)
        if (!animatorHands[handIndex].gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"Hand {handIndex} is inactive, skipping token animation");
            return;
        }
        
        // Play token animation once
        StartCoroutine(TokenAnimationOnceCoroutine(handIndex));
    }

    private IEnumerator TokenAnimationOnceCoroutine(int handIndex)
    {
        // Force immediate start of token animation
        animatorHands[handIndex].SetBool("tokenAction", true);
        animatorHands[handIndex].Play("ElTokenAnimationClip", 0, 0f); // Replace with actual token state name

        AnimatorStateInfo stateInfo = animatorHands[handIndex].GetCurrentAnimatorStateInfo(0);
        float animationDuration = stateInfo.length;

        Debug.Log($"Token animation duration: {animationDuration} seconds");

        yield return new WaitForSeconds(animationDuration);

        // Force immediate transition back to turn animation
        animatorHands[handIndex].SetBool("tokenAction", false);

        // Check if this hand belongs to the current active player
        int playerNumber = GetPlayerNumberFromHandIndex(handIndex);
        if (playerNumber == currentActivePlayer)
        {
            animatorHands[handIndex].SetBool("turnLoop", true);
            animatorHands[handIndex].Play("ElPlayingAnimationClip", 0, 0f); // Force immediate turn animation
        }
        else
        {
            animatorHands[handIndex].Play("ElDrumRollAnimationClip", 0, 0f); // Force to idle
        }
    }

    private int GetPlayerNumberFromHandIndex(int handIndex)
    {
        int playerCount = DeckController.LocalInstance?.playerCount ?? 4;

        if (playerCount == 2) // 1v1 mode
        {
            // In 1v1: hand 0 -> player 0, hand 2 -> player 1
            return handIndex == 0 ? 0 : 1;
        }
        else // 2v2 mode (4 players)
        {
            // In 2v2: direct mapping hand index -> player number
            return handIndex;
        }
    }


}